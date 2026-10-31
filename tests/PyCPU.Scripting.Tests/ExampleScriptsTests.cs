using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace PyCPU.Scripting.Tests
{
    /// <summary>Ukázkové skripty z <c>mods\PyCPU\mod\examples</c>: načtou se a dávají očekávané výstupy.</summary>
    public class ExampleScriptsTests : IClassFixture<BackendFixture>
    {
        private readonly PythonBackend backend;

        public ExampleScriptsTests(BackendFixture fixture) => backend = fixture.Backend;

        /// <summary>Názvy všech příkladů zkopírovaných do výstupu testů.</summary>
        public static IEnumerable<object[]> ExampleNames() =>
            Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "examples"), "*.py")
                .Select(p => new object[] { Path.GetFileNameWithoutExtension(p) });

        /// <summary>Načte příklad a zavolá start() (jako hra před prvním tickem).</summary>
        private IScriptInstance LoadExample(string name)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "examples", name + ".py");
            IScriptInstance script = backend.Load(name, File.ReadAllText(path), null);
            Assert.Null(script.Error);
            script.Start(new int[4], new int[4]);
            Assert.Null(script.Error);
            return script;
        }

        /// <summary>Zavolá tick s daným číslem ticku a vstupy, vrátí výstupy.</summary>
        private static int[] Tick(IScriptInstance script, int tick, params int[] inputs)
        {
            var all = new int[4];
            inputs.CopyTo(all, 0);
            var outputs = new int[4];
            script.TickCount = tick;
            script.Tick(all, outputs);
            Assert.Null(script.Error);
            return outputs;
        }

        [Theory]
        [MemberData(nameof(ExampleNames))]
        public void Example_RunsWithoutError(string name)
        {
            IScriptInstance script = LoadExample(name);
            for (int t = 0; t < 50; t++)
                Tick(script, t, t % 2, 0);
        }

        [Fact]
        public void Blikac_TogglesEverySecond()
        {
            IScriptInstance script = LoadExample("priklad_blikac");
            Assert.Equal(0, Tick(script, 4)[0]);
            Assert.Equal(1, Tick(script, 5)[0]);
            Assert.Equal(0, Tick(script, 10)[0]);
        }

        [Fact]
        public void Piny_EachBitHasOwnPeriod()
        {
            IScriptInstance script = LoadExample("priklad_piny");
            Assert.Equal(0b0000, Tick(script, 0)[0]);
            Assert.Equal(0b0001, Tick(script, 1)[0]);
            Assert.Equal(0b0011, Tick(script, 3)[0]);
            Assert.Equal(0b0101, Tick(script, 5)[0]);
        }

        [Fact]
        public void Citac_CountsRisingEdgesAndResets()
        {
            IScriptInstance script = LoadExample("priklad_citac");
            int[] outputs = null;
            for (int i = 0; i < 10; i++)
            {
                Tick(script, 2 * i, 1, 0);
                outputs = Tick(script, 2 * i + 1, 0, 0);
            }
            Assert.Equal(10 & 0b1111, outputs[0]);
            Assert.Equal(1, outputs[1]);
            Assert.Equal(new[] { 0, 0 }, Tick(script, 100, 0, 1).Take(2));
        }
    }
}
