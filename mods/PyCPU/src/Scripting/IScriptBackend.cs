namespace PyCPU
{
    /// <summary>
    /// Rozhraní mezi herní částí (PyCPU.dll) a Pythonem (lib\PyCPU.Scripting.dll).
    /// Herní část smí s Pythonem mluvit jen přes ně, viz komentář v PyCPU.csproj.
    /// Volat jen z hlavního vlákna, Python engine není thread-safe.
    /// </summary>
    public interface IScriptBackend
    {
        /// <summary>
        /// Zkompiluje skript a spustí jeho kód na úrovni modulu. Chyby skriptu nevyhazuje,
        /// vrátí instanci s vyplněným <see cref="IScriptInstance.Error"/>.
        /// </summary>
        /// <param name="name">Název skriptu (zobrazí se v tracebacku).</param>
        /// <param name="source">Zdrojový kód v Pythonu.</param>
        /// <param name="storageJson">Uložený obsah <c>storage</c> (JSON objekt), nebo null pro prázdný.</param>
        /// <returns>Instance skriptu s vlastním prostorem jmen.</returns>
        IScriptInstance Load(string name, string source, string storageJson);
    }

    /// <summary>
    /// Jeden načtený skript (jedno CPU). Po první chybě se další volání ignorují, dokud se skript nenačte znovu.
    /// Pole <c>inputs</c> a <c>outputs</c> mají délku PyCpuConfig.PinCount; <c>outputs</c> nese aktuální
    /// hodnoty výstupů a skript je přepíše (při chybě zůstanou beze změny).
    /// </summary>
    public interface IScriptInstance
    {
        /// <summary>Chyba skriptu (traceback), nebo null, když je skript v pořádku.</summary>
        string Error { get; }

        /// <summary>Posledních několik řádků z <c>io.log</c> a <c>print</c>, nejstarší první.</summary>
        string[] Logs { get; }

        /// <summary>Aktuální obsah <c>storage</c> jako JSON (pro uložení do savu).</summary>
        string StorageJson { get; }

        /// <summary>Zavolá volitelnou funkci <c>start(io, storage)</c>.</summary>
        void Start(int[] inputs, int[] outputs);

        /// <summary>Zavolá povinnou funkci <c>tick(io, storage)</c>.</summary>
        void Tick(int[] inputs, int[] outputs);

        /// <summary>Zavolá volitelnou funkci <c>stop(io, storage)</c>.</summary>
        void Stop(int[] inputs, int[] outputs);
    }
}
