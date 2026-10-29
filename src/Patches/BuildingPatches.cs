using HarmonyLib;

namespace InfiniteGenerator
{
    /// <summary>Registrace budovy: texty, zařazení do menu Power a registrace configu.</summary>
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class LoadGeneratedBuildings_Patch
    {
        /// <summary>Před načtením budov zapíše texty a přidá budovu do menu Power.</summary>
        public static void Prefix()
        {
            ModStrings.Register();
            ModUtil.AddBuildingToPlanScreen("Power", InfiniteGeneratorConfig.ID);
        }

        /// <summary>Po načtení budov ručně zaregistruje config.</summary>
        public static void Postfix()
        {
            BuildingConfigManager.Instance.RegisterBuilding(new InfiniteGeneratorConfig());
        }
    }
}
