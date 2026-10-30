using System;
using System.Text;

namespace PyCPU
{
    /// <summary>
    /// Řádky stavu v detailu budovy a v tooltipu při najetí myší: stav skriptu a živé hodnoty IN/OUT.
    /// Hra texty přepočítává sama při každém obnovení panelu, proto stačí callbacky nad komponentou PyCpu.
    /// </summary>
    public static class PyCpuStatusItems
    {
        private static StatusItem state;
        private static StatusItem inputs;
        private static StatusItem outputs;

        /// <summary>Přidá status itemy k budově (definice se vytvoří při prvním volání, kdy už existuje Db).</summary>
        public static void Add(KSelectable selectable, PyCpu cpu)
        {
            if (state == null)
                Create();
            selectable.AddStatusItem(state, cpu);
            selectable.AddStatusItem(inputs, cpu);
            selectable.AddStatusItem(outputs, cpu);
        }

        /// <summary>Vytvoří definice status itemů.</summary>
        private static void Create()
        {
            state = Item("PYCPU_STATE", (_, data) => StateText((PyCpu)data), (_, data) => StateTooltip((PyCpu)data));
            inputs = Item("PYCPU_INPUTS",
                (_, data) => PinsText("IN", i => ((PyCpu)data).Inputs[i]),
                (_, data) => PinsTooltip("IN", i => ((PyCpu)data).Inputs[i]));
            outputs = Item("PYCPU_OUTPUTS",
                (_, data) => PinsText("OUT", ((PyCpu)data).GetOutput),
                (_, data) => PinsTooltip("OUT", ((PyCpu)data).GetOutput));
        }

        /// <summary>Status item s textem i tooltipem z callbacků.</summary>
        private static StatusItem Item(string id, Func<string, object, string> text, Func<string, object, string> tooltip)
        {
            return new StatusItem(id, id, "", "", StatusItem.IconType.Info, NotificationType.Neutral,
                allow_multiples: false, OverlayModes.None.ID, resolve_string_callback: text)
            {
                resolveTooltipCallback = tooltip,
            };
        }

        /// <summary>Krátký text stavu, např. „Skript test: běží“.</summary>
        private static string StateText(PyCpu cpu)
        {
            string status;
            switch (cpu.State)
            {
                case PyCpuState.Error:
                    status = STRINGS.UI.FormatAsKeyWord(ModStrings.StateError);
                    break;
                case PyCpuState.Stopped:
                    status = ModStrings.StateStopped;
                    break;
                case PyCpuState.Sleeping:
                    status = string.Format(ModStrings.StateSleeping, cpu.SleepLeft);
                    break;
                default:
                    status = ModStrings.StateRunning;
                    break;
            }
            return string.Format(ModStrings.StateLine, cpu.ScriptName, status);
        }

        /// <summary>Tooltip stavu: soubor, chyba a poslední řádky logu.</summary>
        private static string StateTooltip(PyCpu cpu)
        {
            var sb = new StringBuilder();
            sb.Append(cpu.ScriptPath);
            int shared = cpu.ShareCount;
            if (shared > 1)
                sb.Append('\n').Append(string.Format(ModStrings.SharedInfo, shared));
            if (cpu.Error != null)
                sb.Append("\n\n").Append(STRINGS.UI.FormatAsKeyWord(ModStrings.StateError)).Append('\n').Append(cpu.Error);
            string[] logs = cpu.Logs;
            sb.Append("\n\n").Append(ModStrings.LogHeader);
            if (logs.Length == 0)
                sb.Append('\n').Append(ModStrings.LogEmpty);
            foreach (string line in logs)
                sb.Append('\n').Append(line);
            return sb.ToString();
        }

        /// <summary>Jeden řádek s hodnotami pinů, např. „IN: 12 | 0 | 5 | 1“.</summary>
        private static string PinsText(string prefix, Func<int, int> value)
        {
            var sb = new StringBuilder(prefix).Append(": ");
            for (int i = 0; i < PyCpuConfig.PinCount; i++)
                sb.Append(i == 0 ? "" : " | ").Append(value(i));
            return sb.ToString();
        }

        /// <summary>Tooltip pinů: každý pin dekadicky, hexadecimálně a binárně.</summary>
        private static string PinsTooltip(string prefix, Func<int, int> value)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < PyCpuConfig.PinCount; i++)
            {
                int v = value(i);
                if (i > 0)
                    sb.Append('\n');
                sb.Append($"{prefix}{i} = {v}   0x{v:X}   0b{Convert.ToString(v, 2)}");
            }
            return sb.ToString();
        }
    }
}
