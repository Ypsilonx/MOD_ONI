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
            "# io.sleep(n): přeskočí dalších n ticků (tick běží 5× za s, io.sleep(4) = jednou za sekundu).\n" +
            "# io.tick: počet ticků od start(); např. if io.tick % 5 == 0: jen jednou za sekundu.\n" +
            "# Celá nápověda: NAPOVEDA.md v této složce.\n" +
            "\n" +
            "def start(io, storage):\n" +
            "    pass\n" +
            "\n" +
            "def tick(io, storage):\n" +
            "    io.out[0] = io.inp[0]\n";

        /// <summary>Název souboru nápovědy, který se kopíruje z modu vedle skriptů kolonie.</summary>
        public const string HelpFileName = "NAPOVEDA.md";

        /// <summary>Složka nainstalovaného modu (nastaví <see cref="PyCpuMod.OnLoad"/>).</summary>
        public static string ModFolder { get; internal set; }

        /// <summary>
        /// Nakopíruje ukázkové skripty z <c>examples\</c> modu do knihovny. Existující soubory nepřepisuje,
        /// aby úpravy hráče zůstaly; smazaný příklad se tak po dalším startu hry vrátí.
        /// </summary>
        public static void InstallExamples()
        {
            string source = Path.Combine(ModFolder, "examples");
            string target = LibraryFolder();
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source, "*.py"))
            {
                string dest = Path.Combine(target, Path.GetFileName(file));
                if (!File.Exists(dest))
                    File.Copy(file, dest);
            }
        }

        /// <summary>Zkopíruje nápovědu (README modu) do složky skriptů; vždy přepíše, ať odpovídá verzi modu.</summary>
        /// <param name="folder">Složka skriptů kolonie.</param>
        public static void InstallHelp(string folder)
        {
            Directory.CreateDirectory(folder);
            File.Copy(Path.Combine(ModFolder, "README.md"), Path.Combine(folder, HelpFileName), overwrite: true);
        }

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

        /// <summary>
        /// Knihovna skriptů sdílená všemi koloniemi: <c>Klei\OxygenNotIncluded\pycpu_library\</c>.
        /// Skript z ní se do CPU nahraje výběrem v panelu (přepíše skript CPU, název CPU zůstane).
        /// </summary>
        public static string LibraryFolder() => Path.Combine(Util.RootFolder(), "pycpu_library");

        /// <summary>Plná cesta ke skriptu v knihovně pro daný název budovy.</summary>
        public static string LibraryPathFor(string buildingName) =>
            Path.Combine(LibraryFolder(), SanitizeName(buildingName) + ".py");

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

        /// <summary>Podsložka koše (ve složce skriptů kolonie) pro skripty rozebraných CPU.</summary>
        public const string TrashFolderName = "_smazane";

        /// <summary>
        /// Uklidí skript, který už žádné CPU nepoužívá: nezměněnou šablonu smaže, upravený skript přesune do koše
        /// (při kolizi názvu s časovou značkou), aby rozebrání omylem nesmazalo kód.
        /// </summary>
        /// <param name="path">Plná cesta ke skriptu.</param>
        /// <returns>Popis, co se se souborem stalo (pro log).</returns>
        public static string Retire(string path)
        {
            if (!File.Exists(path))
                return "soubor neexistuje";
            if (IsTemplate(path))
            {
                File.Delete(path);
                return "šablona smazána";
            }
            string target = TrashPathFor(path);
            File.Move(path, target);
            return "přesunuto do " + target;
        }

        /// <summary>Zkopíruje upravený skript do koše (před přepsáním z knihovny); šablonu nezálohuje.</summary>
        /// <param name="path">Plná cesta ke skriptu.</param>
        public static void Backup(string path)
        {
            if (File.Exists(path) && !IsTemplate(path))
                File.Copy(path, TrashPathFor(path));
        }

        /// <summary>True, když soubor obsahuje nezměněnou šablonu.</summary>
        private static bool IsTemplate(string path) => File.ReadAllText(path).Replace("\r\n", "\n") == Template;

        /// <summary>Volná cesta v koši pro daný skript (při kolizi s časovou značkou); koš vytvoří.</summary>
        private static string TrashPathFor(string path)
        {
            string trash = Path.Combine(Path.GetDirectoryName(path), TrashFolderName);
            Directory.CreateDirectory(trash);
            string target = Path.Combine(trash, Path.GetFileName(path));
            if (File.Exists(target))
                target = Path.Combine(trash,
                    $"{Path.GetFileNameWithoutExtension(path)}_{System.DateTime.Now:yyyyMMdd_HHmmss}.py");
            return target;
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
