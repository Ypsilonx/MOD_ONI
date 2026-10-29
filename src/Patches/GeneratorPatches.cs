using HarmonyLib;

namespace InfiniteGenerator
{
    /// <summary>
    /// Výkon generátoru pro každou budovu zvlášť. Hra bere Generator.WattageRating ze sdíleného
    /// BuildingDef (stejný pro všechny instance), tady ho u našich budov nahradíme hodnotou z controlleru.
    /// Getter čte UI (infobox, přehled okruhu) i CircuitManager.
    /// </summary>
    [HarmonyPatch(typeof(Generator), nameof(Generator.WattageRating), MethodType.Getter)]
    public static class Generator_WattageRating_Patch
    {
        /// <summary>U budovy s InfiniteGeneratorController vrátí její nastavený výkon.</summary>
        public static void Postfix(Generator __instance, ref float __result)
        {
            if (__instance.TryGetComponent(out InfiniteGeneratorController controller))
                __result = controller.WattageRating;
        }
    }
}
