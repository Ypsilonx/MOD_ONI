using HarmonyLib;

namespace InfiniteGenerator
{
    /// <summary>Zařazení budovy do stromu výzkumu.</summary>
    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    public static class DbInitialize_Patch
    {
        /// <summary>Po inicializaci databáze přidá budovu do technologie PowerRegulation (jinak do první).</summary>
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
}
