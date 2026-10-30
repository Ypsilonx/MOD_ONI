namespace PyCPU
{
    /// <summary>Texty budovy PyCPU zobrazované ve hře.</summary>
    public static class ModStrings
    {
        /// <summary>Kořen klíčů pro budovu v tabulce STRINGS hry.</summary>
        private const string Root = "STRINGS.BUILDINGS.PREFABS.PYCPU.";

        /// <summary>Název budovy; zároveň výchozí název z UserNameable, který PyCpu nahradí za cpu_&lt;id&gt;.</summary>
        public const string Name = "PyCPU";

        /// <summary>Popis vstupního portu (kolečko v overlay automatizace).</summary>
        public const string InputPort = "Vstup IN{0}";
        /// <summary>Popis výstupního portu.</summary>
        public const string OutputPort = "Výstup OUT{0}";
        /// <summary>Popis kontrolního portu RUN.</summary>
        public const string RunPort = "RUN";

        /// <summary>Zapíše texty do tabulky STRINGS. Musí proběhnout před registrací budovy.</summary>
        public static void Register()
        {
            Strings.Add(Root + "NAME", Name);
            Strings.Add(Root + "DESC", "Programovatelná jednotka řízená skriptem v Pythonu.");
            Strings.Add(Root + "EFFECT",
                "Čte 4 vstupy a nastavuje 4 výstupy (32bitová čísla na ribbon kabelech) podle Python skriptu. " +
                "Běží, dokud je RUN zelený nebo nezapojený.");
        }
    }
}
