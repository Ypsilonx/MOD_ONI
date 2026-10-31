using HarmonyLib;
using KMod;

namespace PyCPU
{
    /// <summary>Vstupní bod modu PyCPU.</summary>
    public class PyCpuMod : UserMod2
    {
        /// <summary>Prefix řádků modu v Player.log.</summary>
        public const string LogPrefix = "[PyCPU]";

        /// <summary>
        /// Zaregistruje načítání knihoven z lib\ a Harmony patche, nakopíruje ukázkové skripty do knihovny.
        /// Python se tu ještě nenačítá.
        /// </summary>
        /// <param name="harmony">Instance Harmony, kterou hra pro mod vytvořila.</param>
        public override void OnLoad(Harmony harmony)
        {
            LibResolver.Register();
            base.OnLoad(harmony);
            ScriptFiles.ModFolder = path;
            try
            {
                ScriptFiles.InstallExamples();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"{LogPrefix} Příklady do knihovny nejde nakopírovat: {e.Message}");
            }
            Debug.Log($"{LogPrefix} Mod loaded!");
        }
    }
}
