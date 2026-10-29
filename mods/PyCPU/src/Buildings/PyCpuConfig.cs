using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace PyCPU
{
    /// <summary>
    /// Definice budovy PyCPU: 3×4 na vrstvě logických bran (jako LogicRibbonWriter ze hry).
    /// Rozložení portů (x = sloupec vůči středu, y = řádek odspodu):
    /// levý sloupec IN0–IN3 shora dolů, pravý sloupec OUT0–OUT3 shora dolů, dole uprostřed RUN.
    /// </summary>
    public class PyCpuConfig : IBuildingConfig
    {
        /// <summary>ID budovy. Neměnit, je uložené v savech.</summary>
        public const string ID = "PyCPU";

        /// <summary>Počet vstupů i výstupů.</summary>
        public const int PinCount = 4;

        /// <summary>ID vstupních portů IN0–IN3.</summary>
        public static readonly HashedString[] InputPortIds = PortIds("PyCpuIn");
        /// <summary>ID výstupních portů OUT0–OUT3.</summary>
        public static readonly HashedString[] OutputPortIds = PortIds("PyCpuOut");
        /// <summary>ID kontrolního portu RUN.</summary>
        public static readonly HashedString RunPortId = new HashedString("PyCpuRun");

        /// <summary>Vytvoří ID portů prefix0…prefix3.</summary>
        private static HashedString[] PortIds(string prefix)
        {
            var ids = new HashedString[PinCount];
            for (int i = 0; i < PinCount; i++)
                ids[i] = new HashedString(prefix + i);
            return ids;
        }

        /// <summary>Vytvoří BuildingDef s ribbon porty a portem RUN.</summary>
        /// <returns>Definice budovy pro hru.</returns>
        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 3,
                height: 4,
                anim: "pycpu_kanim",
                hitpoints: 30,
                construction_time: 30f,
                construction_mass: new[] { 50f },
                construction_materials: MATERIALS.REFINED_METALS,
                melting_point: 1600f,
                build_location_rule: BuildLocationRule.Anywhere,
                decor: BUILDINGS.DECOR.PENALTY.TIER0,
                noise: NOISE_POLLUTION.NONE);

            def.ViewMode = OverlayModes.Logic.ID;
            def.ObjectLayer = ObjectLayer.LogicGate;
            def.SceneLayer = Grid.SceneLayer.LogicGates;
            def.AlwaysOperational = true;
            def.Floodable = false;
            def.Overheatable = false;
            def.Entombable = false;
            def.AudioCategory = "Metal";

            def.LogicInputPorts = new List<LogicPorts.Port>();
            def.LogicOutputPorts = new List<LogicPorts.Port>();
            for (int i = 0; i < PinCount; i++)
            {
                int y = PinCount - 1 - i; // IN0/OUT0 nahoře
                string inName = string.Format(ModStrings.InputPort, i);
                string outName = string.Format(ModStrings.OutputPort, i);
                def.LogicInputPorts.Add(LogicPorts.Port.RibbonInputPort(
                    InputPortIds[i], new CellOffset(-1, y), inName, inName, inName));
                def.LogicOutputPorts.Add(LogicPorts.Port.RibbonOutputPort(
                    OutputPortIds[i], new CellOffset(1, y), outName, outName, outName));
            }
            def.LogicInputPorts.Add(LogicPorts.Port.InputPort(
                RunPortId, new CellOffset(0, 0), ModStrings.RunPort,
                "Zelená: skript běží", "Červená: skript stojí"));

            return def;
        }

        /// <summary>Společné nastavení šablony budovy.</summary>
        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(GameTags.OverlayInFrontOfConduits);
        }

        /// <summary>Přidá komponentu PyCpu k hotové budově.</summary>
        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<PyCpu>();
        }
    }
}
