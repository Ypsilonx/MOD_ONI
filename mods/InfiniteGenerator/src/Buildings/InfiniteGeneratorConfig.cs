using TUNING;
using UnityEngine;

namespace InfiniteGenerator
{
    /// <summary>
    /// Definice budovy nekonečného generátoru: rozměry, materiály, elektrický výstup a komponenty.
    /// </summary>
    public class InfiniteGeneratorConfig : IBuildingConfig
    {
        /// <summary>ID budovy. Neměnit, je uložené v savech.</summary>
        public const string ID = "InfiniteGenerator";

        /// <summary>Vytvoří BuildingDef budovy 1×1 s elektrickým výstupem a vstupem automatizace.</summary>
        /// <returns>Definice budovy pro hru.</returns>
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

            // Výkon v defu jen pro náhled v menu stavby; skutečný výkon má každá budova
            // v InfiniteGeneratorController (viz Generator_WattageRating_Patch).
            buildingDef.GeneratorWattageRating = InfiniteGeneratorController.DefaultWattage;
            // Kapacita bufferu v J; musí pokrýt max. výkon za jeden tick (10 kW × 0,2 s = 2 kJ).
            buildingDef.GeneratorBaseCapacity = 5000f;
            buildingDef.RequiresPowerOutput = true;
            buildingDef.PowerOutputOffset = new CellOffset(0, 0);
            buildingDef.ViewMode = OverlayModes.Power.ID;

            buildingDef.RequiresPowerInput = false;
            buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));

            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.SelfHeatKilowattsWhenActive = 0f;
            buildingDef.AudioCategory = "HollowMetal";
            buildingDef.AudioSize = "large";

            return buildingDef;
        }

        /// <summary>Společné nastavení šablony budovy (tag průmyslové místnosti, zvuky).</summary>
        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            go.AddOrGet<LoopingSounds>();
        }

        /// <summary>Přidá komponenty hotové budovy: generátor, řízení výkonu, automatizaci.</summary>
        public override void DoPostConfigureComplete(GameObject go)
        {
            // Generator místo EnergyGenerator: EnergyGenerator si v EnergySim200ms spotřebovává
            // vstupy a energii by počítal sám; my JoulesAvailable nastavujeme ručně v controlleru.
            Generator generator = go.AddOrGet<Generator>();
            generator.powerDistributionOrder = 9;

            go.AddOrGet<InfiniteGeneratorController>();
            go.AddOrGet<LogicOperationalController>();

            go.AddOrGet<BuildingEnabledButton>();
            go.AddOrGet<LoopingSounds>();
            Prioritizable.AddRef(go);
        }
    }
}
