using System;
using System.Collections.Generic;
using System.Diagnostics;
using IronPython.Hosting;
using IronPython.Runtime.Exceptions;

namespace PyCPU.Scripting
{
    /// <summary>
    /// IronPython backend PyCPU. Zatím jen spike testy: běh IronPythonu v Unity 6 Mono, limit kroků
    /// proti nekonečné smyčce, cena jednoho ticku a dosah skriptu na .NET.
    /// </summary>
    public class PythonBackend : IScriptBackend
    {
        /// <summary>Výjimka, kterou trace funkce přeruší skript po překročení limitu kroků.</summary>
        private sealed class StepLimitExceeded : Exception
        {
            public StepLimitExceeded() : base("step limit exceeded") { }
        }

        private const string Script = @"
def tick(x):
    return x * 2

def endless():
    while True:
        pass
";

        /// <summary>Spustí všechny testy; výjimky zachytí a vrátí jako výsledek testu.</summary>
        /// <returns>Řádek „SPIKE …: OK/FAIL …“ za každý test.</returns>
        public string[] SelfTest()
        {
            var results = new List<string>();
            Run(results, "1 základní běh", BasicRun);
            Run(results, "2 limit kroků", StepLimit);
            Run(results, "3 výkon", Performance);
            Run(results, "4 import clr", ImportClr);
            return results.ToArray();
        }

        /// <summary>Obalí test měřením času a zachycením výjimky.</summary>
        private static void Run(List<string> results, string name, Func<string> test)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                string result = test();
                results.Add($"SPIKE {name}: OK – {result} ({sw.ElapsedMilliseconds} ms)");
            }
            catch (Exception e)
            {
                results.Add($"SPIKE {name}: FAIL ({sw.ElapsedMilliseconds} ms) – {e}");
            }
        }

        /// <summary>Vytvoří engine, nadefinuje funkce a zavolá tick(21).</summary>
        private static string BasicRun()
        {
            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();
            engine.Execute(Script, scope);
            object result = engine.Operations.Invoke(scope.GetVariable<object>("tick"), 21);
            return $"tick(21) = {result}";
        }

        /// <summary>Nekonečnou smyčku musí trace funkce přerušit po 10 000 krocích.</summary>
        private static string StepLimit()
        {
            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();
            engine.Execute(Script, scope);

            int steps = 0;
            TracebackDelegate trace = null;
            trace = (frame, evt, payload) =>
            {
                if (++steps > 10000)
                    throw new StepLimitExceeded();
                return trace;
            };
            Python.SetTrace(engine, trace);
            try
            {
                engine.Operations.Invoke(scope.GetVariable<object>("endless"));
                return "smyčka skončila sama?!";
            }
            catch (Exception e)
            {
                return $"přerušeno po {steps} krocích výjimkou {e.GetType().Name}: {e.Message}";
            }
            finally
            {
                Python.SetTrace(engine, null);
            }
        }

        /// <summary>Změří 1 000 volání tick bez trace a s trace (počítadlem).</summary>
        private static string Performance()
        {
            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();
            engine.Execute(Script, scope);
            object tick = scope.GetVariable<object>("tick");
            var ops = engine.Operations;

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++) ops.Invoke(tick, i);
            long plain = sw.ElapsedMilliseconds;

            int steps = 0;
            TracebackDelegate trace = null;
            trace = (frame, evt, payload) => { steps++; return trace; };
            Python.SetTrace(engine, trace);
            sw.Restart();
            for (int i = 0; i < 1000; i++) ops.Invoke(tick, i);
            long traced = sw.ElapsedMilliseconds;
            Python.SetTrace(engine, null);

            return $"1000× tick: bez trace {plain} ms, s trace {traced} ms ({steps} událostí)";
        }

        /// <summary>Zjistí, jestli skript dosáhne na .NET přes import clr (podklad pro sandbox).</summary>
        private static string ImportClr()
        {
            var engine = Python.CreateEngine();
            object r = engine.Execute("import clr\nimport System\nSystem.Environment.MachineName", engine.CreateScope());
            return $"import clr funguje (MachineName={r}) – sandbox bude nutné řešit";
        }
    }
}
