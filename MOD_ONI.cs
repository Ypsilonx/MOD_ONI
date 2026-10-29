using HarmonyLib;
using KMod;
using TUNING;
using UnityEngine;

// Hlavní třída modu
public class MOD_ONI : UserMod2
{
    public override void OnLoad(Harmony harmony)
    {
        base.OnLoad(harmony);
        Debug.Log("[MOD_ONI] DEV Buildings Mod loaded!");
    }
}

// Konfigurace nekonečného generátoru
public class InfiniteGeneratorConfig : IBuildingConfig
{
    public const string ID = "InfiniteGenerator";
    
    public override BuildingDef CreateBuildingDef()
    {
        string[] materials = new string[] { "Metal" };
        float[] constructionMass = new float[] { 100f };
        EffectorValues noiseLevel = NOISE_POLLUTION.NOISY.TIER2;
        
        BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
            id: ID,
            width: 1,
            height: 1,
            anim: "dev_generator_kanim",
            hitpoints: 100,
            construction_time: 30f,
            construction_mass: constructionMass,
            construction_materials: materials,
            melting_point: 2400f,
            build_location_rule: BuildLocationRule.OnFloor,
            decor: BUILDINGS.DECOR.PENALTY.TIER2,
            noise: noiseLevel
        );
        
        // KLÍČOVÉ nastavení pro generátor podle PeterHan vzoru
        buildingDef.GeneratorWattageRating = 5000f;
        buildingDef.GeneratorBaseCapacity = 5000f;
        buildingDef.RequiresPowerOutput = true;
        buildingDef.PowerOutputOffset = new CellOffset(0, 0);
        buildingDef.ViewMode = OverlayModes.Power.ID;
        
        // Automation support
        buildingDef.RequiresPowerInput = false;
        buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
        
        buildingDef.ExhaustKilowattsWhenActive = 0f;
        buildingDef.SelfHeatKilowattsWhenActive = 0f;
        buildingDef.AudioCategory = "HollowMetal";
        buildingDef.AudioSize = "large";
        
        return buildingDef;
    }
    
    public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
    {
        go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
        go.AddOrGet<LoopingSounds>();
    }
    
    public override void DoPostConfigureComplete(GameObject go)
    {
        // Použijeme Generator místo EnergyGenerator - Generator nevolá EnergySim200ms!
        Generator generator = go.AddOrGet<Generator>();
        generator.powerDistributionOrder = 9;
        
        // Naše komponenta pro generování
        go.AddOrGet<InfiniteGeneratorController>();
        
        // Automation support - umožní ovládat ON/OFF pomocí automation
        go.AddOrGet<LogicOperationalController>();
        
        // Standardní komponenty
        go.AddOrGet<BuildingEnabledButton>();
        go.AddOrGet<LoopingSounds>();
        Prioritizable.AddRef(go);
    }
}

// Komponenta pro řízení generátoru - nastavuje JoulesAvailable na Generatoru
public class InfiniteGeneratorController : KMonoBehaviour, ISim200ms, ISingleSliderControl
{
#pragma warning disable CS0649
    [MyCmpGet]
    private KBatchedAnimController animController;
#pragma warning restore CS0649
#pragma warning disable CS0649 // Field is assigned by [MyCmpGet] at runtime
    [MyCmpGet]
    private Generator generator;
    
    [MyCmpGet]
    private Operational operational;
#pragma warning restore CS0649
    

    private float wattageRating = 5000f; // Výchozí výkon 5 kW
    private const float MinWattage = 100f;
    private const float MaxWattage = 10000f;
    
    protected override void OnSpawn()
    {
        base.OnSpawn();
        
        // Nastavíme výkon generátoru pomocí reflexe
        if (generator != null)
        {
            var wattageField = typeof(Generator).GetField("wattageRating",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (wattageField != null)
            {
                wattageField.SetValue(generator, wattageRating);
            }
        }
        
        Debug.Log($"[MOD_ONI] InfiniteGenerator spawned with {wattageRating}W");
    }
    
    // KLÍČOVÁ METODA - udržujeme generator vždy aktivní s plnou energií
    public void Sim200ms(float dt)
    {
        if (generator == null || operational == null)
            return;
        
        // Pokud je generátor operativní, nastavíme JoulesAvailable podle výkonu a dt
        if (operational.IsOperational)
        {
            var joulesField = typeof(Generator).GetField("joulesAvailable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (joulesField != null)
            {
                // Nastavíme energii na maximum, které může být za 200 ms vyrobeno
                float maxJoules = wattageRating * dt; // W * s = J
                joulesField.SetValue(generator, maxJoules);
            }
            operational.SetActive(true);

            // Ovládání animace: běží jen při výrobě, rychlost podle wattáže
            if (animController != null)
            {
                float speed = Mathf.Lerp(0.2f, 2.0f, (wattageRating - MinWattage) / (MaxWattage - MinWattage));
                var animSpeedField = typeof(KBatchedAnimController).GetField("animSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (animSpeedField != null)
                    animSpeedField.SetValue(animController, speed);
                if (animController.currentAnim.ToString() != "on")
                    animController.Play("on", KAnim.PlayMode.Loop);
            }
        }
        else
        {
            operational.SetActive(false);
            // Zastavíme animaci
            if (animController != null)
            {
                var animSpeedField = typeof(KBatchedAnimController).GetField("animSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (animSpeedField != null)
                    animSpeedField.SetValue(animController, 1f);
                if (animController.currentAnim.ToString() != "off")
                    animController.Play("off", KAnim.PlayMode.Loop);
            }
        }

        // Aktualizace statusu pro správné zobrazení výkonu v infoboxu
        if (generator != null)
        {
            var def = generator.GetComponent<Building>().Def;
            if (def != null)
            {
                def.GeneratorWattageRating = wattageRating;
            }
        }
    }
    // ISingleSliderControl implementace
    public string SliderTitleKey => "STRINGS.BUILDINGS.PREFABS.INFINITEGENERATOR.SLIDER.TITLE";
    public string SliderUnits => "W";
    public int SliderDecimalPlaces(int index) => 0;
    public float GetSliderMin(int index) => MinWattage;
    public float GetSliderMax(int index) => MaxWattage;
    public float GetSliderValue(int index) => wattageRating;
    public void SetSliderValue(float value, int index)
    {
        wattageRating = value;
        // Okamžitě aktualizujeme výkon generátoru
        if (generator != null)
        {
            var wattageField = typeof(Generator).GetField("wattageRating",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (wattageField != null)
            {
                wattageField.SetValue(generator, wattageRating);
            }

            // Opravíme i hodnotu v BuildingDef pro správné zobrazení v infoboxu
            var def = generator.GetComponent<Building>().Def;
            if (def != null)
            {
                def.GeneratorWattageRating = wattageRating;
            }
        }
    }
    public string GetSliderTooltipKey(int index) => "STRINGS.BUILDINGS.PREFABS.INFINITEGENERATOR.SLIDER.TOOLTIP";
    public string GetSliderTooltip(int index) => "Nastavuje aktuální výkon generátoru v [W].";
    public int SliderRows => 1;
    public int SliderColCount => 1;
    public bool IsSliderInteractable(int index) => true;
    public float GetSliderMinValue(int index) => MinWattage;
    public float GetSliderMaxValue(int index) => MaxWattage;
    public string GetSliderLabel(int index) => null;
}

// Patch pro registraci budovy
[HarmonyPatch(typeof(GeneratedBuildings))]
[HarmonyPatch(nameof(GeneratedBuildings.LoadGeneratedBuildings))]
public class InfiniteGenerator_Buildings_Patch
{
    public static void Prefix()
    {
        Strings.Add("STRINGS.BUILDINGS.PREFABS.INFINITEGENERATOR.NAME", "Infinite power GEN");
        Strings.Add("STRINGS.BUILDINGS.PREFABS.INFINITEGENERATOR.DESC", "Generuje neomezenou elektrickou energii pro testovací účely.");
        Strings.Add("STRINGS.BUILDINGS.PREFABS.INFINITEGENERATOR.EFFECT", "Produkuje elektřinu bez jakýchkoliv vstupů.");
        Strings.Add("STRINGS.BUILDINGS.PREFABS.INFINITEGENERATOR.SLIDER.TITLE", "Výkon generátoru");
        Strings.Add("STRINGS.BUILDINGS.PREFABS.INFINITEGENERATOR.SLIDER.TOOLTIP", "Nastavuje aktuální výkon generátoru v [W].");
        ModUtil.AddBuildingToPlanScreen("Power", InfiniteGeneratorConfig.ID);
    }
    
    public static void Postfix()
    {
        BuildingConfigManager.Instance.RegisterBuilding(new InfiniteGeneratorConfig());
    }
}

// Patch pro přidání do tech tree
[HarmonyPatch(typeof(Db))]
[HarmonyPatch("Initialize")]
public class InfiniteGenerator_Tech_Patch
{
    public static void Postfix()
    {
        Tech tech = Db.Get().Techs.TryGet("PowerRegulation");
        if (tech != null)
        {
            tech.unlockedItemIDs.Add(InfiniteGeneratorConfig.ID);
        }
        else if (Db.Get().Techs.resources.Count > 0)
        {
            Db.Get().Techs.resources[0].unlockedItemIDs.Add(InfiniteGeneratorConfig.ID);
        }
    }
}

// Patch pro zakázání slider UI u EnergyGenerator
