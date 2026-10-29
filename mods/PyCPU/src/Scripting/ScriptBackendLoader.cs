using System;
using System.IO;
using System.Reflection;

namespace PyCPU
{
    /// <summary>
    /// Načte lib\PyCPU.Scripting.dll a vytvoří implementaci IScriptBackend. Volat až po startu hry
    /// (po Assets.OnPrefabInit, kdy hra prochází typy všech assembly v procesu), nejdřív v hlavním menu.
    /// </summary>
    public static class ScriptBackendLoader
    {
        private const string AssemblyFile = "PyCPU.Scripting.dll";
        private const string BackendType = "PyCPU.Scripting.PythonBackend";

        private static IScriptBackend backend;

        /// <summary>Vrátí backend; při prvním volání ho načte. Chyby načtení propadnou volajícímu.</summary>
        public static IScriptBackend Get()
        {
            if (backend == null)
            {
                string path = Path.Combine(LibResolver.LibDir, AssemblyFile);
                Assembly asm = Assembly.LoadFrom(path);
                backend = (IScriptBackend)Activator.CreateInstance(asm.GetType(BackendType, throwOnError: true));
            }
            return backend;
        }
    }
}
