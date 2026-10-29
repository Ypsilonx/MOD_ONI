using HarmonyLib;

namespace InfiniteGenerator
{
    /// <summary>
    /// Texty a zařazení budovy do menu Power. Samotný InfiniteGeneratorConfig registruje hra
    /// sama (najde IBuildingConfig v DLL modu); ruční RegisterBuilding vedl k duplicitě.
    /// </summary>
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class LoadGeneratedBuildings_Patch
    {
        /// <summary>Před načtením budov zapíše texty a přidá budovu do menu Power.</summary>
        public static void Prefix()
        {
            ModStrings.Register();
            // Podkategorie "generators" (jako DevGenerator); bez ní hra v logu hlásí
            // "has not been added to plan screen subcategory organization".
            ModUtil.AddBuildingToPlanScreen("Power", InfiniteGeneratorConfig.ID, "generators");
        }
    }
}
