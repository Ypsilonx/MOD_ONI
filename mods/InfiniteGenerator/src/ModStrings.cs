namespace InfiniteGenerator
{
    /// <summary>Texty budovy zobrazované ve hře (název, popis, posuvník).</summary>
    public static class ModStrings
    {
        /// <summary>Kořen klíčů pro budovu v tabulce STRINGS hry.</summary>
        private const string Root = "STRINGS.BUILDINGS.PREFABS.INFINITEGENERATOR.";

        /// <summary>Klíč nadpisu posuvníku výkonu.</summary>
        public const string SliderTitleKey = Root + "SLIDER.TITLE";

        /// <summary>Klíč tooltipu posuvníku výkonu.</summary>
        public const string SliderTooltipKey = Root + "SLIDER.TOOLTIP";

        /// <summary>Text tooltipu posuvníku (posuvník ho chce i přímo, nejen přes klíč).</summary>
        public const string SliderTooltip = "Nastavuje aktuální výkon generátoru v [W].";

        /// <summary>Zapíše texty do tabulky STRINGS. Musí proběhnout před registrací budovy.</summary>
        public static void Register()
        {
            Strings.Add(Root + "NAME", "Infinite power GEN");
            Strings.Add(Root + "DESC", "Generuje neomezenou elektrickou energii pro testovací účely.");
            Strings.Add(Root + "EFFECT", "Produkuje elektřinu bez jakýchkoliv vstupů.");
            Strings.Add(SliderTitleKey, "Výkon generátoru");
            Strings.Add(SliderTooltipKey, SliderTooltip);
        }
    }
}
