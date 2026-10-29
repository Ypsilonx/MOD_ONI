using HarmonyLib;

namespace PyCPU
{
    /// <summary>Texty a zařazení budovy do menu Automation (podkategorie logických bran).</summary>
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class LoadGeneratedBuildings_Patch
    {
        /// <summary>Před načtením budov zapíše texty a přidá budovu do menu.</summary>
        public static void Prefix()
        {
            ModStrings.Register();
            ModUtil.AddBuildingToPlanScreen("Automation", PyCpuConfig.ID, "logicgates");
        }
    }

    /// <summary>Zařazení budovy do výzkumu Advanced Automation (ID technologie LogicCircuits).</summary>
    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    public static class DbInitialize_Patch
    {
        /// <summary>Po inicializaci databáze přidá budovu k technologii LogicCircuits.</summary>
        public static void Postfix()
        {
            Db.Get().Techs.TryGet("LogicCircuits")?.unlockedItemIDs.Add(PyCpuConfig.ID);
        }
    }
}
