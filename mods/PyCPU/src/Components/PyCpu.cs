using System;
using System.IO;
using System.Runtime.Serialization;
using KSerialization;
using UnityEngine;

namespace PyCPU
{
    /// <summary>Stav CPU zobrazovaný ve hře.</summary>
    public enum PyCpuState
    {
        /// <summary>Skript běží (tick každých 200 ms).</summary>
        Running,
        /// <summary>Skript běží, ale přeskakuje ticky po <c>io.sleep(n)</c>.</summary>
        Sleeping,
        /// <summary>RUN je červený.</summary>
        Stopped,
        /// <summary>Chyba skriptu nebo jeho načtení; výstupy jsou 0.</summary>
        Error,
    }

    /// <summary>
    /// Logika budovy PyCPU: 5× za sekundu předá vstupy Python skriptu a jeho výstupy pošle do sítě.
    /// Skript je soubor podle názvu budovy (<see cref="ScriptFiles"/>), kopie zdrojáku a storage jsou v savu.
    /// Typ se ukládá do savu pod jménem PyCPU.PyCpu – nepřejmenovávat.
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public class PyCpu : KMonoBehaviour, ISim200ms
    {
        /// <summary>Jak často (s) kontrolovat změnu souboru skriptu.</summary>
        private const float FileCheckInterval = 1f;

        /// <summary>Událost, kterou vyvolá UserNameable.SetName (parametr: nový název).</summary>
        private const int NameChangedEvent = 1102426921;

        /// <summary>Symboly teček PROGRAM a ERROR v animaci (převzaté z ONICPU).</summary>
        private static readonly KAnimHashedString ProgramDot = "dot_prog";
        private static readonly KAnimHashedString ErrorDot = "dot_err";

        /// <summary>Barvy teček: rozsvícená zelená, rozsvícená červená, zhasnutá (barvy jako v ONICPU).</summary>
        private static readonly Color DotGreen = new Color(0.4f, 1f, 0.4f);
        private static readonly Color DotRed = new Color(1f, 0.4f, 0.4f);
        private static readonly Color DotOff = new Color(40 / 255f, 58 / 255f, 46 / 255f);

#pragma warning disable CS0649 // Pole plní hra přes [MyCmpGet] za běhu.
        [MyCmpGet]
        private LogicPorts ports;

        [MyCmpGet]
        private KBatchedAnimController animController;

        [MyCmpGet]
        private UserNameable nameable;

        [MyCmpGet]
        private KPrefabID prefabId;

        [MyCmpGet]
        private KSelectable selectable;
#pragma warning restore CS0649

        /// <summary>Kopie zdrojáku: save funguje i bez souboru a smazaný soubor se z ní obnoví.</summary>
        [Serialize]
        private string source;

        /// <summary>Obsah <c>storage</c> skriptu jako JSON.</summary>
        [Serialize]
        private string storageJson;

        /// <summary>Už proběhl <c>start()</c>; po načtení savu se proto nevolá znovu.</summary>
        [Serialize]
        private bool started;

        /// <summary>Kolik ticků ještě přeskočit po <c>io.sleep(n)</c>.</summary>
        [Serialize]
        private int sleepLeft;

        /// <summary>Počet ticků od start() včetně přeskočených (<c>io.tick</c>).</summary>
        [Serialize]
        private int tickCount;

        private readonly int[] inputs = new int[PyCpuConfig.PinCount];
        private readonly int[] outputs = new int[PyCpuConfig.PinCount];
        private IScriptInstance script;
        private ScriptFile file;
        private string hostError;
        private string reportedError;
        private float fileCheckTimer;
        private bool? wasActive;
        private bool? wasError;

        /// <summary>Běží, když je RUN zelený, nebo když není zapojený.</summary>
        public bool IsRunning =>
            !ports.IsPortConnected(PyCpuConfig.RunPortId) || ports.GetInputValue(PyCpuConfig.RunPortId) != 0;

        /// <summary>Název budovy, a tím i skriptu.</summary>
        public string ScriptName => nameable.savedName;

        /// <summary>Plná cesta k souboru skriptu, null než se soubor poprvé připojí.</summary>
        public string ScriptPath => file?.Path;

        /// <summary>Chyba skriptu nebo jeho načtení, null když je vše v pořádku.</summary>
        public string Error => hostError ?? script?.Error;

        /// <summary>Posledních několik řádků z <c>io.log</c> a <c>print</c>.</summary>
        public string[] Logs => script?.Logs ?? new string[0];

        /// <summary>Hodnoty vstupů z posledního ticku (jen ke čtení).</summary>
        public int[] Inputs => inputs;

        /// <summary>Hodnota, kterou výstup právě posílá do sítě (u zastaveného nebo chybného CPU 0).</summary>
        public int GetOutput(int index) => ports.GetOutputValue(PyCpuConfig.OutputPortIds[index]);

        /// <summary>Kolik ticků ještě skript spí po <c>io.sleep(n)</c>.</summary>
        public int SleepLeft => sleepLeft;

        /// <summary>Aktuální stav pro zobrazení ve hře.</summary>
        public PyCpuState State =>
            Error != null ? PyCpuState.Error
            : !IsRunning ? PyCpuState.Stopped
            : sleepLeft > 0 ? PyCpuState.Sleeping
            : PyCpuState.Running;

        /// <summary>Výchozí název, připojení souboru skriptu, hlídání přejmenování a status itemy.</summary>
        protected override void OnSpawn()
        {
            base.OnSpawn();
            // Animace (převzatá z ONICPU) má dva štítky jazyka přes sebe; oba jsou „Python“, druhý stačí skrýt.
            animController.SetSymbolVisiblity("lang_1", false);
            if (string.IsNullOrEmpty(nameable.savedName) || nameable.savedName == ModStrings.Name)
                nameable.SetName("cpu_" + prefabId.InstanceID);
            // Hodnoty výstupů ukládá LogicPorts sám; skript na ně po načtení navazuje.
            for (int i = 0; i < PyCpuConfig.PinCount; i++)
                outputs[i] = ports.GetOutputValue(PyCpuConfig.OutputPortIds[i]);
            PyCpuStatusItems.Add(selectable, this);
            Subscribe(NameChangedEvent, _ => OpenFile(announce: true));
            OpenFile(announce: false);
        }

        /// <summary>Před uložením savu převezme aktuální storage ze skriptu.</summary>
        [OnSerializing]
        private void OnSerializing()
        {
            if (script != null)
                storageJson = script.StorageJson;
        }

        /// <summary>5× za sekundu: kontrola souboru, volání skriptu a odeslání výstupů.</summary>
        /// <param name="dt">Uplynulý čas v sekundách.</param>
        public void Sim200ms(float dt)
        {
            fileCheckTimer += dt;
            if (fileCheckTimer >= FileCheckInterval)
            {
                fileCheckTimer = 0;
                CheckFile();
            }

            for (int i = 0; i < PyCpuConfig.PinCount; i++)
                inputs[i] = ports.GetInputValue(PyCpuConfig.InputPortIds[i]);

            bool running = IsRunning;
            if (Error == null)
            {
                if (running)
                    RunTick();
                else if (started)
                {
                    started = false;
                    sleepLeft = 0;
                    script.Stop(inputs, outputs);
                }
                ReportError();
            }

            // Zastavené nebo chybné CPU drží výstupy na 0 (bezpečný stav).
            bool active = running && Error == null;
            for (int i = 0; i < PyCpuConfig.PinCount; i++)
                SetOutput(i, active ? outputs[i] : 0);
            UpdateVisuals(active);
        }

        /// <summary>
        /// Jeden tick běžícího CPU: případně start(), pak tick(), pokud skript zrovna nespí.
        /// První tick po start() má io.tick = 0.
        /// </summary>
        private void RunTick()
        {
            if (!started)
            {
                started = true;
                tickCount = 0;
                script.TickCount = 0;
                script.Start(inputs, outputs);
                sleepLeft = script.SleepTicks;
            }
            else
            {
                tickCount++;
            }
            if (sleepLeft > 0)
            {
                sleepLeft--;
                return;
            }
            script.TickCount = tickCount;
            script.Tick(inputs, outputs);
            sleepLeft = script.SleepTicks;
        }

        /// <summary>
        /// Načte soubor znovu, vymaže storage a při dalším ticku zavolá start(). Slouží i k obnovení po chybě.
        /// </summary>
        public void Restart()
        {
            script = null;
            storageJson = null;
            started = false;
            sleepLeft = 0;
            OpenFile(announce: true);
        }

        /// <summary>Otevře soubor skriptu v editoru (VS Code se složkou skriptů kolonie).</summary>
        public void OpenInEditor()
        {
            if (file == null)
                return;
            if (!file.Exists)
                CheckFile();
            EditorLauncher.Open(file.Path, ScriptFiles.Folder());
        }

        /// <summary>
        /// Připojí soubor podle aktuálního názvu budovy. Existující soubor má přednost (tak lze víc CPU
        /// pustit na jeden skript); chybějící se vytvoří z kopie v savu, u nové budovy ze šablony.
        /// </summary>
        /// <param name="announce">Ukázat nad budovou potvrzení „skript načten“ (ne při načítání savu).</param>
        private void OpenFile(bool announce)
        {
            try
            {
                file = new ScriptFile(ScriptFiles.PathFor(ScriptName));
                if (file.Exists)
                {
                    LoadScript(file.Read(), announce);
                }
                else
                {
                    string text = string.IsNullOrEmpty(source) ? ScriptFiles.Template : source;
                    file.Write(text);
                    LoadScript(text, announce);
                }
            }
            catch (Exception e)
            {
                FailHost($"Soubor skriptu nejde otevřít: {e.Message}");
            }
        }

        /// <summary>Jednou za sekundu: změněný soubor načte znovu, smazaný obnoví z kopie.</summary>
        private void CheckFile()
        {
            if (file == null)
                return;
            try
            {
                if (!file.Exists)
                    file.Write(source ?? ScriptFiles.Template);
                else if (file.ChangedOnDisk)
                    LoadScript(file.Read(), announce: true);
            }
            catch (IOException)
            {
                // Editor soubor právě zapisuje; zkusí se to znovu za sekundu.
            }
            catch (Exception e)
            {
                FailHost($"Soubor skriptu nejde přečíst: {e.Message}");
            }
        }

        /// <summary>
        /// Načte skript. Stejný zdroják už běžícího skriptu nenačítá znovu. Při změně zdrojáku převezme
        /// storage a zavolá znovu start().
        /// </summary>
        /// <param name="text">Zdrojový kód.</param>
        /// <param name="announce">Ukázat nad budovou potvrzení „skript načten“, když se načte bez chyby.</param>
        private void LoadScript(string text, bool announce)
        {
            if (script != null)
            {
                if (text == source && hostError == null)
                    return;
                storageJson = script.StorageJson;
                started = false;
                sleepLeft = 0;
            }
            source = text;
            hostError = null;
            try
            {
                script = ScriptBackendLoader.Get().Load(ScriptName, text, storageJson);
                Debug.Log($"{PyCpuMod.LogPrefix} {ScriptName}: skript načten ({file?.Path})");
            }
            catch (Exception e)
            {
                script = null;
                FailHost($"Python se nepodařilo načíst: {e}");
            }
            if (announce && Error == null)
                PopText(PopFXManager.Instance.sprite_Plus, ModStrings.PopLoaded);
            ReportError();
        }

        /// <summary>Zastaví CPU s chybou hostitele (soubor, načtení Pythonu).</summary>
        private void FailHost(string message)
        {
            hostError = message;
            ReportError();
        }

        /// <summary>Novou chybu ukáže nad budovou a zapíše jednou do Player.log.</summary>
        private void ReportError()
        {
            string error = Error;
            if (error == reportedError)
                return;
            reportedError = error;
            if (error == null)
                return;
            Debug.LogWarning($"{PyCpuMod.LogPrefix} {ScriptName}: {error}");
            PopText(PopFXManager.Instance.sprite_Negative, ModStrings.PopError);
        }

        /// <summary>Krátce zobrazí plovoucí text nad budovou.</summary>
        private void PopText(Sprite icon, string text)
        {
            // Při načítání savu PopFXManager ještě nemusí existovat.
            if (PopFXManager.Instance != null)
                PopFXManager.Instance.SpawnFX(icon, text, transform);
        }

        /// <summary>Animace on/off a tečky PROGRAM (skript v pořádku) a ERROR (chyba) podle stavu.</summary>
        private void UpdateVisuals(bool active)
        {
            if (active != wasActive)
            {
                wasActive = active;
                animController.Play(active ? "on" : "off", active ? KAnim.PlayMode.Loop : KAnim.PlayMode.Once);
            }
            bool error = Error != null;
            if (error != wasError)
            {
                wasError = error;
                animController.SetSymbolTint(ProgramDot, error ? DotOff : DotGreen);
                animController.SetSymbolTint(ErrorDot, error ? DotRed : DotOff);
            }
        }

        /// <summary>Nastaví výstup; signál do sítě posílá jen při změně hodnoty.</summary>
        private void SetOutput(int index, int value)
        {
            HashedString port = PyCpuConfig.OutputPortIds[index];
            if (ports.GetOutputValue(port) != value)
                ports.SendSignal(port, value);
        }
    }
}
