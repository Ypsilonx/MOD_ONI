namespace PyCPU
{
    /// <summary>
    /// Rozhraní mezi herní částí (PyCPU.dll) a Pythonem (lib\PyCPU.Scripting.dll).
    /// Herní část smí s Pythonem mluvit jen přes ně, viz komentář v PyCPU.csproj.
    /// </summary>
    public interface IScriptBackend
    {
        /// <summary>Spike: ověří běh IronPythonu ve hře a vrátí řádky s výsledky testů.</summary>
        string[] SelfTest();
    }
}
