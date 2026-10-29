using System;
using HarmonyLib;

namespace PyCPU
{
    /// <summary>
    /// Spike: v hlavním menu (hra už je plně načtená) načte Python backend a spustí jeho testy.
    /// Dočasné – po spiku se odstraní.
    /// </summary>
    [HarmonyPatch(typeof(MainMenu), "OnSpawn")]
    public static class MainMenu_OnSpawn_SpikePatch
    {
        private static bool done;

        /// <summary>Jednou za běh hry spustí testy a výsledky zapíše do Player.log.</summary>
        public static void Postfix()
        {
            if (done)
                return;
            done = true;
            try
            {
                foreach (string line in ScriptBackendLoader.Get().SelfTest())
                    Debug.Log($"{PyCpuMod.LogPrefix} {line}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{PyCpuMod.LogPrefix} SPIKE načtení backendu selhalo: {e}");
            }
        }
    }
}
