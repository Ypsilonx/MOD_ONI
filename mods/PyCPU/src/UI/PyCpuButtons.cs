namespace PyCPU
{
    /// <summary>
    /// Společný základ tlačítek v bočním panelu. Hra vytvoří jedno tlačítko za každou komponentu
    /// s <see cref="ISidescreenButtonControl"/>, proto má každé tlačítko vlastní třídu.
    /// </summary>
    public abstract class PyCpuButton : KMonoBehaviour, ISidescreenButtonControl
    {
        /// <summary>Skupina, ve které jsou tlačítka vedle sebe na jednom řádku.</summary>
        private const int ButtonRow = 7310;

#pragma warning disable CS0649 // Pole plní hra přes [MyCmpGet] za běhu.
        [MyCmpGet]
        protected PyCpu cpu;
#pragma warning restore CS0649

        /// <inheritdoc />
        public abstract string SidescreenButtonText { get; }

        /// <inheritdoc />
        public abstract string SidescreenButtonTooltip { get; }

        /// <inheritdoc />
        public abstract void OnSidescreenButtonPressed();

        /// <summary>Bez vlastního nadpisu. Rozhraní má výchozí implementaci, kterou net48 neumí zdědit.</summary>
        public string SidescreenTitle => null;

        /// <inheritdoc />
        public void SetButtonTextOverride(ButtonMenuTextOverride textOverride) { }

        /// <inheritdoc />
        public bool SidescreenEnabled() => true;

        /// <inheritdoc />
        public bool SidescreenButtonInteractable() => true;

        /// <inheritdoc />
        public int HorizontalGroupID() => ButtonRow;

        /// <inheritdoc />
        public int ButtonSideScreenSortOrder() => 20;
    }

    /// <summary>Tlačítko „Upravit ve VS Code“.</summary>
    public class PyCpuEditButton : PyCpuButton
    {
        /// <inheritdoc />
        public override string SidescreenButtonText => ModStrings.ButtonEdit;

        /// <inheritdoc />
        public override string SidescreenButtonTooltip => ModStrings.ButtonEditTooltip;

        /// <inheritdoc />
        public override void OnSidescreenButtonPressed() => cpu.OpenInEditor();
    }

    /// <summary>Tlačítko „Do knihovny“.</summary>
    public class PyCpuLibraryButton : PyCpuButton
    {
        /// <inheritdoc />
        public override string SidescreenButtonText => ModStrings.ButtonLibrary;

        /// <inheritdoc />
        public override string SidescreenButtonTooltip => ModStrings.ButtonLibraryTooltip;

        /// <inheritdoc />
        public override void OnSidescreenButtonPressed() => cpu.SaveToLibrary();
    }

    /// <summary>Tlačítko „Restart“.</summary>
    public class PyCpuRestartButton : PyCpuButton
    {
        /// <inheritdoc />
        public override string SidescreenButtonText => ModStrings.ButtonRestart;

        /// <inheritdoc />
        public override string SidescreenButtonTooltip => ModStrings.ButtonRestartTooltip;

        /// <inheritdoc />
        public override void OnSidescreenButtonPressed() => cpu.Restart();
    }
}
