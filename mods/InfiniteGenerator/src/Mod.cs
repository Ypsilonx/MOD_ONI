using HarmonyLib;
using KMod;

namespace InfiniteGenerator
{
    /// <summary>
    /// Vstupní bod modu. Hra ho najde v DLL a zavolá OnLoad; base.OnLoad aplikuje všechny
    /// [HarmonyPatch] třídy z této assembly.
    /// </summary>
    public class InfiniteGeneratorMod : UserMod2
    {
        /// <summary>Prefix řádků modu v Player.log, ať jdou snadno vyfiltrovat.</summary>
        public const string LogPrefix = "[InfiniteGenerator]";

        /// <summary>Načte mod a zaregistruje Harmony patche.</summary>
        /// <param name="harmony">Instance Harmony, kterou hra pro mod vytvořila.</param>
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log($"{LogPrefix} Mod loaded!");
        }
    }
}
