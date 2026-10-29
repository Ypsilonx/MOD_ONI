using System;
using System.IO;
using System.Reflection;

namespace PyCPU
{
    /// <summary>
    /// Načítá knihovny modu (PyCPU.Scripting, IronPython, DLR) z podsložky lib\. Hra je tam nevidí
    /// a nevolá na nich GetTypes() při načítání modu. Registruje se v OnLoad, před prvním použitím knihoven.
    /// </summary>
    public static class LibResolver
    {
        /// <summary>Cesta k lib\ vedle PyCPU.dll.</summary>
        public static string LibDir { get; private set; }

        /// <summary>Zaregistruje AssemblyResolve pro lib\. Volat jednou v OnLoad.</summary>
        public static void Register()
        {
            LibDir = Path.Combine(Path.GetDirectoryName(typeof(LibResolver).Assembly.Location), "lib");
            AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
        }

        /// <summary>Najde požadovanou knihovnu podle jména v lib\, jinak nechá hledání na dalších.</summary>
        private static Assembly OnResolve(object sender, ResolveEventArgs args)
        {
            string path = Path.Combine(LibDir, new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }
    }
}
