using System;
using System.IO;
using System.Text;

namespace PyCPU
{
    /// <summary>Umístění a šablona skriptů: <c>save_files\&lt;kolonie&gt;\pycpu\&lt;název budovy&gt;.py</c>.</summary>
    public static class ScriptFiles
    {
        /// <summary>Podsložka se skripty ve složce kolonie.</summary>
        public const string FolderName = "pycpu";

        /// <summary>Obsah nového skriptu.</summary>
        public const string Template =
            "# PyCPU skript. Po uložení ho hra do 1 s načte znovu.\n" +
            "# io.inp[0..3]: vstupy, io.out[0..3]: výstupy (32bitová čísla), io.log(...): log.\n" +
            "# storage: dict, který se ukládá do savu (jen str/int/float/bool/None/list/dict).\n" +
            "\n" +
            "def start(io, storage):\n" +
            "    pass\n" +
            "\n" +
            "def tick(io, storage):\n" +
            "    io.out[0] = io.inp[0]\n";

        /// <summary>Kódování zapisovaných souborů (UTF-8 bez BOM, jak ho čeká VS Code).</summary>
        internal static readonly Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>Složka se skripty aktivní kolonie.</summary>
        public static string Folder()
        {
            string colony = SaveLoader.GetActiveSaveColonyFolder().TrimEnd('\\', '/');
            // Po načtení autosavu může cesta vést do auto_save; skripty patří celé kolonii.
            if (string.Equals(Path.GetFileName(colony), "auto_save", StringComparison.OrdinalIgnoreCase))
                colony = Path.GetDirectoryName(colony);
            return Path.Combine(colony, FolderName);
        }

        /// <summary>Plná cesta ke skriptu pro daný název budovy.</summary>
        public static string PathFor(string buildingName) => Path.Combine(Folder(), SanitizeName(buildingName) + ".py");

        /// <summary>Upraví název budovy na platný název souboru (zakázané znaky → podtržítko).</summary>
        public static string SanitizeName(string name)
        {
            var sb = new StringBuilder(name.Trim());
            foreach (char c in Path.GetInvalidFileNameChars())
                sb.Replace(c, '_');
            string result = sb.ToString().Trim('.', ' ');
            return result.Length == 0 ? "cpu" : result;
        }
    }

    /// <summary>Jeden soubor skriptu a čas jeho poslední známé změny (pro kontrolu úprav v editoru).</summary>
    internal sealed class ScriptFile
    {
        private System.DateTime lastWrite;

        /// <summary>Plná cesta k souboru.</summary>
        public string Path { get; }

        public ScriptFile(string path) => Path = path;

        /// <summary>True, když soubor existuje.</summary>
        public bool Exists => File.Exists(Path);

        /// <summary>True, když se soubor od posledního čtení nebo zápisu změnil.</summary>
        public bool ChangedOnDisk => File.GetLastWriteTimeUtc(Path) != lastWrite;

        /// <summary>Přečte soubor. Když ho editor právě zapisuje, vyhodí IOException (zkusit znovu později).</summary>
        public string Read()
        {
            System.DateTime time = File.GetLastWriteTimeUtc(Path);
            string text = File.ReadAllText(Path);
            lastWrite = time;
            return text;
        }

        /// <summary>Zapíše soubor (a vytvoří složku).</summary>
        public void Write(string text)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            File.WriteAllText(Path, text, ScriptFiles.Utf8);
            lastWrite = File.GetLastWriteTimeUtc(Path);
        }
    }
}
