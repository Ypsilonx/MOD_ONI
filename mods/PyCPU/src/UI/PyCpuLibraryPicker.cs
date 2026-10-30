using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PyCPU
{
    /// <summary>
    /// Výběr skriptu z knihovny v bočním panelu (hotový herní panel se seznamem voleb, NToggleSideScreen).
    /// Volba přepíše skript CPU skriptem z knihovny (<see cref="PyCpu.LoadFromLibrary"/>), název CPU zůstane.
    /// </summary>
    public class PyCpuLibraryPicker : KMonoBehaviour, INToggleSideScreenControl
    {
        /// <summary>Jak dlouho (s) platí načtený seznam knihovny. Panel čte volby mnohokrát za sebou.</summary>
        private const float RescanInterval = 2f;

#pragma warning disable CS0649 // Pole plní hra přes [MyCmpGet] za běhu.
        [MyCmpGet]
        private PyCpu cpu;
#pragma warning restore CS0649

        private readonly List<string> names = new List<string>();
        private readonly List<LocString> options = new List<LocString>();
        private readonly List<LocString> tooltips = new List<LocString>();
        private float scannedAt = float.NegativeInfinity;

        /// <inheritdoc />
        public string SidescreenTitleKey => ModStrings.LibraryTitleKey;

        /// <inheritdoc />
        public List<LocString> Options
        {
            get
            {
                Rescan();
                return options;
            }
        }

        /// <inheritdoc />
        public List<LocString> Tooltips
        {
            get
            {
                Rescan();
                return tooltips;
            }
        }

        /// <inheritdoc />
        public string Description => names.Count == 0 ? ModStrings.LibraryEmpty : ModStrings.LibraryDescription;

        /// <summary>Žádná volba není „vybraná“: kliknutí je jednorázová akce (nahrání), ne přepínač.</summary>
        public int SelectedOption => -1;

        /// <inheritdoc />
        public int QueuedOption => SelectedOption;

        /// <inheritdoc />
        public void QueueSelectedOption(int option)
        {
            if (option >= 0 && option < names.Count)
                cpu.LoadFromLibrary(names[option]);
        }

        /// <summary>Načte seznam skriptů z knihovny, nejvýš jednou za <see cref="RescanInterval"/>.</summary>
        private void Rescan()
        {
            if (Time.unscaledTime - scannedAt < RescanInterval)
                return;
            scannedAt = Time.unscaledTime;
            names.Clear();
            options.Clear();
            tooltips.Clear();
            try
            {
                string folder = ScriptFiles.LibraryFolder();
                if (!Directory.Exists(folder))
                    return;
                foreach (string path in Directory.GetFiles(folder, "*.py").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    names.Add(name);
                    options.Add(new LocString(name));
                    tooltips.Add(new LocString(string.Format(ModStrings.LibraryOptionTooltip, name)));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{PyCpuMod.LogPrefix} Knihovnu nejde načíst: {e.Message}");
            }
        }
    }
}
