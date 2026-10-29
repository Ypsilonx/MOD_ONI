using System;
using System.Collections.Generic;
using System.Threading;
using IronPython.Hosting;
using IronPython.Runtime;
using IronPython.Runtime.Exceptions;
using Microsoft.Scripting.Hosting;

namespace PyCPU.Scripting
{
    /// <summary>
    /// IronPython backend PyCPU: jeden sdílený engine pro všechna CPU, každý skript má vlastní scope
    /// s omezenými builtins (sandbox) a každé volání do Pythonu hlídá limit kroků.
    /// Vytváří ho ScriptBackendLoader v PyCPU.dll přes reflexi (jméno typu nepřejmenovávat).
    /// </summary>
    public class PythonBackend : IScriptBackend
    {
        /// <summary>Maximum trace událostí (řádků, volání) na jedno volání skriptu. 10 000 kroků ≈ 0,14 s.</summary>
        public const int MaxSteps = 10000;

        /// <summary>Moduly, které smí skript importovat (vestavěné v IronPythonu, bez přístupu k systému).</summary>
        private static readonly HashSet<string> AllowedModules = new HashSet<string> { "math", "cmath", "itertools" };

        /// <summary>Builtins, které skript nedostane: přístup k souborům, konzoli a ukončení procesu.</summary>
        private static readonly HashSet<string> BlockedBuiltins = new HashSet<string>
        {
            "open", "input", "exit", "quit", "help", "breakpoint", "copyright", "credits", "license",
        };

        /// <summary>Pomocný kód v Pythonu: hlídaný import, logovací funkce a objekt io.</summary>
        private const string Prelude = @"
def make_import(real_import, allowed):
    def __import__(name, globals=None, locals=None, fromlist=(), level=0):
        if level != 0 or name.partition('.')[0] not in allowed:
            raise ImportError(""import '%s' is not allowed in PyCPU"" % name)
        return real_import(name, globals, locals, fromlist, level)
    return __import__

def make_log(sink):
    def log(*args, sep=' ', **kwargs):
        sink(sep.join(str(a) for a in args))
    return log

class IO:
    __slots__ = ('inp', 'out', 'log')
    def __init__(self, log):
        self.inp = []
        self.out = []
        self.log = log
";

        /// <summary>Označení abortu vlákna, kterým trace funkce přeruší skript po překročení limitu kroků.</summary>
        private static readonly object AbortMarker = new object();

        private readonly Dictionary<string, object> safeBuiltins = new Dictionary<string, object>();
        private readonly object makeLog;
        private readonly object ioClass;
        private readonly TracebackDelegate trace;
        private int steps;

        /// <summary>Sdílený engine (kvůli testům a chybovým výpisům přístupný i instancím skriptů).</summary>
        internal ScriptEngine Engine { get; }

        /// <summary>Vytvoří engine (~0,6 s při prvním volání v procesu) a připraví sandbox.</summary>
        public PythonBackend()
        {
            Engine = Python.CreateEngine();
            ScriptScope prelude = Engine.CreateScope();
            Engine.Execute(Prelude, prelude);
            makeLog = prelude.GetVariable<object>("make_log");
            ioClass = prelude.GetVariable<object>("IO");

            ScriptScope builtins = Python.GetBuiltinModule(Engine);
            foreach (KeyValuePair<string, object> item in builtins.GetItems())
                if (!BlockedBuiltins.Contains(item.Key))
                    safeBuiltins[item.Key] = item.Value;
            var allowed = new PythonList();
            foreach (string module in AllowedModules)
                allowed.append(module);
            safeBuiltins["__import__"] = Engine.Operations.Invoke(
                prelude.GetVariable<object>("make_import"), builtins.GetVariable<object>("__import__"), allowed);

            trace = (frame, evt, payload) =>
            {
                // Běžnou výjimku z trace by skript spolkl v try/except a IronPython by pak trace vypnul
                // (smyčka by běžela dál bez hlídání). ThreadAbortException CLR znovu vyhodí na konci
                // každého catch, takže projde až do RunLimited, kde se abort zruší.
                if (++steps > MaxSteps)
                    Thread.CurrentThread.Abort(AbortMarker);
                return trace;
            };
        }

        /// <inheritdoc />
        public IScriptInstance Load(string name, string source, string storageJson)
        {
            return new PythonScript(this, name, source, storageJson);
        }

        /// <summary>Vytvoří builtins pro jeden skript; <c>print</c> zapisuje do jeho logu.</summary>
        /// <param name="logSink">Kam jdou řádky z <c>print</c> a <c>io.log</c>.</param>
        /// <param name="log">Vrací logovací funkci, kterou dostane i objekt io.</param>
        internal PythonDictionary CreateBuiltins(Action<string> logSink, out object log)
        {
            log = Engine.Operations.Invoke(makeLog, logSink);
            var dict = new PythonDictionary();
            foreach (KeyValuePair<string, object> item in safeBuiltins)
                dict[item.Key] = item.Value;
            dict["print"] = log;
            return dict;
        }

        /// <summary>Vytvoří objekt <c>io</c> předávaný skriptu.</summary>
        internal object CreateIo(object log) => Engine.Operations.Invoke(ioClass, log);

        /// <summary>Spustí akci s hlídáním limitu kroků.</summary>
        /// <returns>True, když akce překročila limit (výjimku ze skriptu v tom případě zahodí).</returns>
        internal bool RunLimited(System.Action action)
        {
            steps = 0;
            // Trace je v IronPythonu vázaný na vlákno, proto se zapíná pro každé volání zvlášť.
            Python.SetTrace(Engine, trace);
            try
            {
                action();
            }
            catch (Exception) when (steps > MaxSteps)
            {
                if ((Thread.CurrentThread.ThreadState & System.Threading.ThreadState.AbortRequested) != 0)
                    Thread.ResetAbort();
            }
            finally
            {
                Python.SetTrace(Engine, null);
            }
            return steps > MaxSteps;
        }
    }
}
