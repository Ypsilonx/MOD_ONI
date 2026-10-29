using KSerialization;

namespace PyCPU
{
    /// <summary>
    /// Logika budovy PyCPU. Task 1: bez Pythonu, výstupy jen kopírují vstupy (ověření portů).
    /// Typ se ukládá do savu pod jménem PyCPU.PyCpu – po vydání nepřejmenovávat.
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public class PyCpu : KMonoBehaviour, ISim200ms
    {
#pragma warning disable CS0649 // Pole plní hra přes [MyCmpGet] za běhu.
        [MyCmpGet]
        private LogicPorts ports;

        [MyCmpGet]
        private KBatchedAnimController animController;
#pragma warning restore CS0649

        private readonly int[] inputs = new int[PyCpuConfig.PinCount];
        private readonly int[] outputs = new int[PyCpuConfig.PinCount];
        private bool? wasRunning;

        /// <summary>Běží, když je RUN zelený, nebo když není zapojený.</summary>
        public bool IsRunning =>
            !ports.IsPortConnected(PyCpuConfig.RunPortId) || ports.GetInputValue(PyCpuConfig.RunPortId) != 0;

        /// <summary>5× za sekundu: přečte vstupy, spočítá výstupy a pošle změněné hodnoty do sítě.</summary>
        /// <param name="dt">Uplynulý čas v sekundách.</param>
        public void Sim200ms(float dt)
        {
            bool running = IsRunning;
            for (int i = 0; i < PyCpuConfig.PinCount; i++)
                inputs[i] = ports.GetInputValue(PyCpuConfig.InputPortIds[i]);

            for (int i = 0; i < PyCpuConfig.PinCount; i++)
                SetOutput(i, running ? inputs[i] : 0);

            if (running != wasRunning)
            {
                wasRunning = running;
                animController.Play(running ? "on" : "off", running ? KAnim.PlayMode.Loop : KAnim.PlayMode.Once);
            }
        }

        /// <summary>Nastaví výstup; signál do sítě posílá jen při změně hodnoty.</summary>
        private void SetOutput(int index, int value)
        {
            if (outputs[index] == value)
                return;
            outputs[index] = value;
            ports.SendSignal(PyCpuConfig.OutputPortIds[index], value);
        }
    }
}
