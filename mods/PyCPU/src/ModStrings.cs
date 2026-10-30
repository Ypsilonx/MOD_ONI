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

        /// <summary>Řádek stavu v detailu budovy: {0} název skriptu, {1} stav.</summary>
        public const string StateLine = "Skript {0}: {1}";
        /// <summary>Stav: běží.</summary>
        public const string StateRunning = "běží";
        /// <summary>Stav: spí po io.sleep, {0} zbývající ticky.</summary>
        public const string StateSleeping = "čeká (io.sleep, ještě {0} ticků)";
        /// <summary>Stav: RUN je červený.</summary>
        public const string StateStopped = "zastaven (RUN červený)";
        /// <summary>Stav: chyba skriptu.</summary>
        public const string StateError = "CHYBA";
        /// <summary>Nadpis logu v tooltipu.</summary>
        public const string LogHeader = "Log (io.log, print):";
        /// <summary>Prázdný log.</summary>
        public const string LogEmpty = "(prázdný)";

        /// <summary>Plovoucí text po úspěšném načtení skriptu.</summary>
        public const string PopLoaded = "Skript načten";
        /// <summary>Plovoucí text při chybě skriptu.</summary>
        public const string PopError = "Chyba skriptu";

        /// <summary>Tlačítko otevření skriptu v editoru.</summary>
        public const string ButtonEdit = "Upravit ve VS Code";
        /// <summary>Tooltip tlačítka editoru.</summary>
        public const string ButtonEditTooltip =
            "Otevře soubor skriptu ve VS Code (bez VS Code v programu přiřazeném k .py). Po uložení se skript do 1 s načte.";
        /// <summary>Tlačítko restartu.</summary>
        public const string ButtonRestart = "Restart";
        /// <summary>Tooltip tlačítka restartu.</summary>
        public const string ButtonRestartTooltip =
            "Načte skript znovu, VYMAŽE storage a zavolá start(). Obnoví i CPU zastavené chybou.";

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
