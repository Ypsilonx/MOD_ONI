using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using IronPython.Runtime;
using IronPython.Runtime.Types;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace PyCPU.Scripting
{
    /// <summary>
    /// Jeden načtený skript: vlastní scope, objekt <c>io</c>, slovník <c>storage</c> a log.
    /// Po první chybě se zastaví (Error != null) a další volání ignoruje.
    /// </summary>
    internal sealed class PythonScript : IScriptInstance
    {
        /// <summary>Kolik posledních řádků logu se drží.</summary>
        public const int MaxLogLines = 20;

        private readonly PythonBackend backend;
        private readonly Queue<string> logs = new Queue<string>();
        private readonly object io;
        private readonly object tick, start, stop;
        private PythonDictionary storage;
        private string lastStorageJson;

        /// <inheritdoc />
        public string Error { get; private set; }

        /// <inheritdoc />
        public string[] Logs => logs.ToArray();

        /// <inheritdoc />
        public int SleepTicks { get; private set; }

        /// <inheritdoc />
        public int TickCount { get; set; }

        /// <summary>Načte skript; chyby zapíše do <see cref="Error"/>.</summary>
        public PythonScript(PythonBackend backend, string name, string source, string storageJson)
        {
            this.backend = backend;
            ScriptEngine engine = backend.Engine;
            PythonDictionary builtins = backend.CreateBuiltins(AddLog, out object log);
            io = backend.CreateIo(log);
            try
            {
                storage = StorageConverter.FromJson(storageJson);
                lastStorageJson = StorageConverter.ToJson(storage);

                ScriptScope scope = engine.CreateScope();
                scope.SetVariable("__builtins__", builtins);
                ScriptSource src = engine.CreateScriptSourceFromString(source, name + ".py", SourceCodeKind.File);
                if (backend.RunLimited(() => src.Execute(scope)))
                {
                    FailStepLimit();
                    return;
                }

                scope.TryGetVariable("tick", out tick);
                scope.TryGetVariable("start", out start);
                scope.TryGetVariable("stop", out stop);
                if (tick == null)
                    Error = "Skript nemá funkci tick(io, storage).";
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        /// <inheritdoc />
        public string StorageJson
        {
            get
            {
                if (Error == null)
                {
                    try
                    {
                        lastStorageJson = StorageConverter.ToJson(storage);
                    }
                    catch (Exception e)
                    {
                        Fail(e);
                    }
                }
                return lastStorageJson;
            }
        }

        /// <inheritdoc />
        public void Start(int[] inputs, int[] outputs) => Call(start, inputs, outputs);

        /// <inheritdoc />
        public void Tick(int[] inputs, int[] outputs) => Call(tick, inputs, outputs);

        /// <inheritdoc />
        public void Stop(int[] inputs, int[] outputs) => Call(stop, inputs, outputs);

        /// <summary>Zavolá funkci skriptu s io a storage a přepíše výstupy hodnotami z io.out.</summary>
        private void Call(object function, int[] inputs, int[] outputs)
        {
            if (Error != null || function == null)
                return;
            ObjectOperations ops = backend.Engine.Operations;
            try
            {
                ops.SetMember(io, "inp", ToList(inputs));
                ops.SetMember(io, "out", ToList(outputs));
                ops.SetMember(io, "skip", 0);
                ops.SetMember(io, "tick", TickCount);
                SleepTicks = 0;
                if (backend.RunLimited(() => ops.Invoke(function, io, storage)))
                {
                    FailStepLimit();
                    return;
                }

                int[] result = ReadOutputs(ops.GetMember<object>(io, "out"), outputs.Length);
                Array.Copy(result, outputs, outputs.Length);
                SleepTicks = ops.GetMember<object>(io, "skip") is int skip ? skip : int.MaxValue;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }

        /// <summary>Převede pole hodnot pinů na Python list.</summary>
        private static PythonList ToList(int[] values)
        {
            var list = new PythonList();
            foreach (int v in values)
                list.append(v);
            return list;
        }

        /// <summary>Přečte io.out a převede hodnoty na 32bitová čísla.</summary>
        /// <exception cref="ScriptValueException">io.out není seznam čísel dané délky.</exception>
        private static int[] ReadOutputs(object outValue, int count)
        {
            if (!(outValue is IList list) || list.Count < count)
                throw new ScriptValueException($"io.out musí být seznam alespoň {count} čísel.");
            var result = new int[count];
            for (int i = 0; i < count; i++)
                result[i] = ToPin(list[i], i);
            return result;
        }

        /// <summary>
        /// Převede hodnotu výstupu na int. Velká čísla se ořežou na spodních 32 bitů (jako v ONICPU),
        /// bool je 0/1, float se ořízne k nule.
        /// </summary>
        private static int ToPin(object value, int index)
        {
            switch (value)
            {
                case int i:
                    return i;
                case bool b:
                    return b ? 1 : 0;
                case BigInteger big:
                    return unchecked((int)(uint)(big & uint.MaxValue));
                case double d when d > int.MinValue - 1.0 && d < int.MaxValue + 1.0:
                    return (int)d;
                default:
                    throw new ScriptValueException(
                        $"io.out[{index}] musí být celé číslo (32 bitů), je {TypeName(value)}.");
            }
        }

        /// <summary>Název Python typu hodnoty pro chybové hlášky.</summary>
        internal static string TypeName(object value) =>
            value == null ? "None" : PythonType.Get__name__(DynamicHelpers.GetPythonType(value));

        /// <summary>Přidá řádek do logu, nejstarší řádky zahazuje.</summary>
        private void AddLog(string line)
        {
            logs.Enqueue(line);
            while (logs.Count > MaxLogLines)
                logs.Dequeue();
        }

        /// <summary>Zastaví skript s chybou z výjimky (Python traceback nebo vlastní hláška).</summary>
        private void Fail(Exception e)
        {
            Error = e is ScriptValueException
                ? e.Message
                : backend.Engine.GetService<ExceptionOperations>().FormatException(e).TrimEnd();
        }

        /// <summary>Zastaví skript s chybou překročení limitu kroků.</summary>
        private void FailStepLimit()
        {
            Error = $"Skript překročil limit {PythonBackend.MaxSteps} kroků na jedno volání (nekonečná smyčka?).";
        }
    }
}
