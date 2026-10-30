using System.Linq;
using Xunit;

namespace PyCPU.Scripting.Tests
{
    /// <summary>Sdílený backend pro všechny testy (vytvoření enginu je pomalé).</summary>
    public sealed class BackendFixture
    {
        public PythonBackend Backend { get; } = new PythonBackend();
    }

    /// <summary>Testy Python hostu: API skriptu, limit kroků, sandbox, storage a chyby.</summary>
    public class PythonBackendTests : IClassFixture<BackendFixture>
    {
        private readonly PythonBackend backend;

        public PythonBackendTests(BackendFixture fixture) => backend = fixture.Backend;

        /// <summary>Načte skript a ověří, že se načetl bez chyby.</summary>
        private IScriptInstance Load(string source, string storageJson = null)
        {
            IScriptInstance script = backend.Load("test", source, storageJson);
            Assert.Null(script.Error);
            return script;
        }

        /// <summary>Zavolá tick a vrátí výstupy.</summary>
        private static int[] Tick(IScriptInstance script, params int[] inputs)
        {
            var outputs = new int[4];
            script.Tick(inputs, outputs);
            return outputs;
        }

        [Fact]
        public void Tick_ComputesOutputsFromInputs()
        {
            var script = Load(@"
def tick(io, storage):
    io.out[0] = io.inp[0] + io.inp[1]
    io.out[1] = io.inp[2] > 5
");
            Assert.Equal(new[] { 3, 1, 0, 0 }, Tick(script, 1, 2, 7, 0));
        }

        [Fact]
        public void Tick_KeepsOutputsNotWrittenByScript()
        {
            var script = Load("def tick(io, storage):\n    io.out[0] = 1\n");
            var outputs = new[] { 0, 5, 6, 7 };
            script.Tick(new int[4], outputs);
            Assert.Equal(new[] { 1, 5, 6, 7 }, outputs);
        }

        [Fact]
        public void Outputs_AreTruncatedTo32Bits()
        {
            var script = Load("def tick(io, storage):\n    io.out[0] = 2**32 + 5\n    io.out[1] = -1\n    io.out[2] = 2.9\n");
            Assert.Equal(new[] { 5, -1, 2, 0 }, Tick(script, 0, 0, 0, 0));
        }

        [Fact]
        public void Outputs_InvalidTypeStopsScript()
        {
            var script = Load("def tick(io, storage):\n    io.out[0] = 'x'\n");
            Assert.Equal(new int[4], Tick(script, 0, 0, 0, 0));
            Assert.Contains("io.out[0]", script.Error);
        }

        [Fact]
        public void StartAndStop_AreOptionalAndCalled()
        {
            var script = Load(@"
def start(io, storage):
    storage['started'] = True
def tick(io, storage):
    pass
def stop(io, storage):
    io.out[3] = 9
");
            var outputs = new int[4];
            script.Start(new int[4], outputs);
            script.Stop(new int[4], outputs);
            Assert.Equal(9, outputs[3]);
            Assert.Contains("\"started\":true", script.StorageJson);

            var minimal = Load("def tick(io, storage): pass");
            minimal.Start(new int[4], outputs);
            minimal.Stop(new int[4], outputs);
            Assert.Null(minimal.Error);
        }

        [Fact]
        public void Sleep_ReportsTicksOnlyForCallThatRequestedIt()
        {
            var script = Load("def tick(io, storage):\n    if io.inp[0]:\n        io.sleep(io.inp[0])\n");
            Tick(script, 4, 0, 0, 0);
            Assert.Equal(4, script.SleepTicks);
            Tick(script, 0, 0, 0, 0);
            Assert.Equal(0, script.SleepTicks);
        }

        [Fact]
        public void TickCount_IsVisibleAsIoTick()
        {
            var script = Load("def tick(io, storage):\n    if io.tick % 5 == 0:\n        io.out[1] = io.tick\n");
            var outputs = new int[4];
            for (int n = 0; n < 12; n++)
            {
                script.TickCount = n;
                script.Tick(new int[4], outputs);
            }
            Assert.Equal(10, outputs[1]);
        }

        [Fact]
        public void Sleep_InvalidArgumentIsError()
        {
            var script = Load("def tick(io, storage):\n    io.sleep(-1)\n");
            Tick(script, 0, 0, 0, 0);
            Assert.Contains("io.sleep", script.Error);
        }

        [Fact]
        public void MissingTick_IsError()
        {
            Assert.Contains("tick", backend.Load("test", "x = 1", null).Error);
        }

        [Fact]
        public void SyntaxError_IsReportedWithLine()
        {
            string error = backend.Load("test", "def tick(io, storage)\n    pass\n", null).Error;
            Assert.NotNull(error);
            Assert.Contains("line 1", error);
        }

        [Fact]
        public void RuntimeError_StopsScriptAndKeepsOutputs()
        {
            var script = Load("def tick(io, storage):\n    io.out[0] = 1\n    raise ValueError('boom')\n");
            Assert.Equal(new int[4], Tick(script, 0, 0, 0, 0));
            Assert.Contains("boom", script.Error);
            Assert.Contains("line 3", script.Error);
        }

        [Fact]
        public void StepLimit_StopsEndlessLoopInTick()
        {
            var script = Load("def tick(io, storage):\n    while True:\n        pass\n");
            Tick(script, 0, 0, 0, 0);
            Assert.Contains("limit", script.Error);
        }

        [Fact]
        public void StepLimit_CannotBeSwallowedByExcept()
        {
            var script = Load(@"
def tick(io, storage):
    while True:
        try:
            while True:
                pass
        except:
            pass
");
            Tick(script, 0, 0, 0, 0);
            Assert.Contains("limit", script.Error);
        }

        [Fact]
        public void StepLimit_StopsEndlessLoopAtModuleLevel()
        {
            Assert.Contains("limit", backend.Load("test", "while True:\n    pass\n", null).Error);
        }

        [Fact]
        public void StepLimit_ResetsBetweenTicks()
        {
            var script = Load("def tick(io, storage):\n    for i in range(3000):\n        pass\n");
            for (int i = 0; i < 10; i++)
                Tick(script, 0, 0, 0, 0);
            Assert.Null(script.Error);
        }

        [Theory]
        [InlineData("import clr")]
        [InlineData("import System")]
        [InlineData("import os")]
        [InlineData("import sys")]
        [InlineData("from io import open")]
        [InlineData("__import__('clr')")]
        [InlineData("open('x.txt', 'w')")]
        [InlineData("exec('import clr')")]
        public void Sandbox_BlocksSystemAccess(string code)
        {
            Assert.NotNull(backend.Load("test", code + "\ndef tick(io, storage): pass\n", null).Error);
        }

        [Fact]
        public void Sandbox_AllowsMath()
        {
            var script = Load("import math\ndef tick(io, storage):\n    io.out[0] = math.floor(math.sqrt(io.inp[0]))\n");
            Assert.Equal(4, Tick(script, 17, 0, 0, 0)[0]);
        }

        [Fact]
        public void Storage_RoundTripsThroughJson()
        {
            var script = Load(@"
def tick(io, storage):
    storage['n'] = storage.get('n', 0) + 1
    storage['items'] = [1, 'a', None, 2.5, {'x': False}]
    storage['big'] = 2**40
");
            Tick(script, 0, 0, 0, 0);
            Tick(script, 0, 0, 0, 0);
            string json = script.StorageJson;
            Assert.True(script.Error == null, script.Error);

            var restored = Load("def tick(io, storage):\n    io.out[0] = storage['n']\n    io.out[1] = storage['big'] == 2**40\n", json);
            int[] outputs = Tick(restored, 0, 0, 0, 0);
            Assert.True(restored.Error == null, restored.Error);
            Assert.Equal(new[] { 2, 1, 0, 0 }, outputs);
            Assert.Equal(json, restored.StorageJson);
        }

        [Fact]
        public void Storage_UnsupportedTypeIsError()
        {
            var script = Load("def tick(io, storage):\n    storage['s'] = {1, 2}\n");
            Tick(script, 0, 0, 0, 0);
            Assert.Equal("{}", script.StorageJson);
            Assert.Contains("storage['s']", script.Error);
        }

        [Fact]
        public void Logs_KeepLastLinesFromLogAndPrint()
        {
            var script = Load("def tick(io, storage):\n    io.log('in', io.inp[0])\n    print('p', io.inp[0])\n");
            for (int i = 0; i < 15; i++)
                Tick(script, i, 0, 0, 0);
            string[] logs = script.Logs;
            Assert.Equal(20, logs.Length); // PythonScript.MaxLogLines
            Assert.Equal("p 14", logs.Last());
            Assert.Equal("in 5", logs.First());
        }

        [Fact]
        public void Scripts_HaveSeparateScopes()
        {
            var a = Load("counter = 0\ndef tick(io, storage):\n    global counter\n    counter += 1\n    io.out[0] = counter\n");
            var b = Load("counter = 100\ndef tick(io, storage):\n    io.out[0] = counter\n");
            Tick(a, 0, 0, 0, 0);
            Assert.Equal(2, Tick(a, 0, 0, 0, 0)[0]);
            Assert.Equal(100, Tick(b, 0, 0, 0, 0)[0]);
        }
    }
}
