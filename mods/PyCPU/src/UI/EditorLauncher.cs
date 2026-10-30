using System;
using System.Diagnostics;
using System.IO;

namespace PyCPU
{
    /// <summary>Otevření skriptu v editoru: VS Code se složkou skriptů, jinak aplikace přiřazená k .py.</summary>
    public static class EditorLauncher
    {
        /// <summary>
        /// Otevře soubor. VS Code dostane i složku skriptů kolonie (okno s ní se znovu použije a funguje
        /// v něm hledání napříč skripty). Chyby jen zaloguje, hra kvůli editoru spadnout nesmí.
        /// </summary>
        /// <param name="file">Plná cesta ke skriptu.</param>
        /// <param name="folder">Složka skriptů kolonie.</param>
        public static void Open(string file, string folder)
        {
            try
            {
                string code = FindVsCode();
                if (code != null)
                    Process.Start(new ProcessStartInfo(code, $"\"{folder}\" --goto \"{file}\"") { UseShellExecute = false });
                else
                    Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{PyCpuMod.LogPrefix} Editor nejde spustit: {e.Message}");
            }
        }

        /// <summary>
        /// Najde Code.exe: obvyklé instalace (uživatelská, systémová), pak <c>bin\code.cmd</c> v PATH.
        /// Code.exe se spouští přímo, ne přes code.cmd, aby neproblikla konzole.
        /// </summary>
        /// <returns>Cesta ke Code.exe, nebo null, když VS Code není nainstalované.</returns>
        private static string FindVsCode()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\Microsoft VS Code\Code.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft VS Code\Code.exe"),
            };
            foreach (string path in candidates)
                if (File.Exists(path))
                    return path;

            foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            {
                try
                {
                    if (File.Exists(Path.Combine(dir, "code.cmd")))
                    {
                        string exe = Path.GetFullPath(Path.Combine(dir, @"..\Code.exe"));
                        if (File.Exists(exe))
                            return exe;
                    }
                }
                catch (ArgumentException)
                {
                    // Neplatná položka v PATH, přeskočit.
                }
            }
            return null;
        }
    }
}
