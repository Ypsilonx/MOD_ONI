using System;
using System.IO;
using System.Runtime.Serialization;
using KSerialization;

namespace PyCPU
{
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

#pragma warning disable CS0649 // Pole plní hra přes [MyCmpGet] za běhu.
        [MyCmpGet]
        private LogicPorts ports;

        [MyCmpGet]
        private KBatchedAnimController animController;

        [MyCmpGet]
        private UserNameable nameable;

        [MyCmpGet]
        private KPrefabID prefabId;
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

        private readonly int[] inputs = new int[PyCpuConfig.PinCount];
        private readonly int[] outputs = new int[PyCpuConfig.PinCount];
        private IScriptInstance script;
        private ScriptFile file;
        private string hostError;
        private string reportedError;
        private float fileCheckTimer;
        private bool? wasActive;

        /// <summary>Běží, když je RUN zelený, nebo když není zapojený.</summary>
        public bool IsRunning =>
            !ports.IsPortConnected(PyCpuConfig.RunPortId) || ports.GetInputValue(PyCpuConfig.RunPortId) != 0;

        /// <summary>Název budovy, a tím i skriptu.</summary>
        public string ScriptName => nameable.savedName;

        /// <summary>Chyba skriptu nebo jeho načtení, null když je vše v pořádku.</summary>
        public string Error => hostError ?? script?.Error;

        /// <summary>Výchozí název, připojení souboru skriptu a hlídání přejmenování.</summary>
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
            Subscribe(NameChangedEvent, _ => OpenFile());
            OpenFile();
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
                {
                    if (!started)
                    {
                        started = true;
                        script.Start(inputs, outputs);
                    }
                    script.Tick(inputs, outputs);
                }
                else if (started)
                {
                    started = false;
                    script.Stop(inputs, outputs);
                }
                ReportError();
            }

            // Zastavené nebo chybné CPU drží výstupy na 0 (bezpečný stav).
            bool active = running && Error == null;
            for (int i = 0; i < PyCpuConfig.PinCount; i++)
                SetOutput(i, active ? outputs[i] : 0);

            if (active != wasActive)
            {
                wasActive = active;
                animController.Play(active ? "on" : "off", active ? KAnim.PlayMode.Loop : KAnim.PlayMode.Once);
            }
        }

        /// <summary>
        /// Připojí soubor podle aktuálního názvu budovy. Existující soubor má přednost (tak lze víc CPU
        /// pustit na jeden skript); chybějící se vytvoří z kopie v savu, u nové budovy ze šablony.
        /// </summary>
        private void OpenFile()
        {
            try
            {
                file = new ScriptFile(ScriptFiles.PathFor(ScriptName));
                if (file.Exists)
                {
                    LoadScript(file.Read());
                }
                else
                {
                    string text = string.IsNullOrEmpty(source) ? ScriptFiles.Template : source;
                    file.Write(text);
                    LoadScript(text);
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
                    LoadScript(file.Read());
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
        private void LoadScript(string text)
        {
            if (script != null)
            {
                if (text == source && hostError == null)
                    return;
                storageJson = script.StorageJson;
                started = false;
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
            ReportError();
        }

        /// <summary>Zastaví CPU s chybou hostitele (soubor, načtení Pythonu).</summary>
        private void FailHost(string message)
        {
            hostError = message;
            ReportError();
        }

        /// <summary>Novou chybu zapíše jednou do Player.log (dokud ji neukáže UI).</summary>
        private void ReportError()
        {
            string error = Error;
            if (error == reportedError)
                return;
            reportedError = error;
            if (error != null)
                Debug.LogWarning($"{PyCpuMod.LogPrefix} {ScriptName}: {error}");
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
