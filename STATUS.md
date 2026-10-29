# Stav projektu

Aktualizováno: 2026-10-29

## Cíl

PyCPU: budova s 32bitovými ribbon porty řízená Python skriptem (`tick()` 5×/s), skripty jako `.py`
vedle savu, editace ve VS Code. Spec: `docs/specs/pycpu.md`.

## Hotovo

- Audit modů, složka modů jako junction na `<složka mimo OneDrive>`, mody jen ze Steamu.
- Repo pro víc modů (`mods/<Mod>/`), společný build s nasazením (`Directory.Build.props/.targets`).
- InfiniteGenerator 1.2.0: hotový, odložený (hra má vlastní DEV generátor).
- **Spike PyCPU úspěšný ve hře:** IronPython běží, limit kroků funguje, tick ~1–5 µs.
  Vyřešené podmínky: knihovny v `lib\`, `PyCPU.dll` bez reference na IronPython, `DllStripper` na Microsoft.Dynamic.

## Rozpracováno

- Spike kód (`SpikePatches`, `PythonBackend.SelfTest`) se nahradí skutečnou implementací.

## Další krok

1. Plán PyCPU do `docs/plans/` (budova + porty, script host se sandboxem, soubory a ukládání, boční panel).
2. Malý mod, který při startu hry odstraní atribut jen pro čtení ze složek modů (viz níže).
3. Nástroje do `tools/`: filtr Player.log a kontrola kompatibility z auditu.
4. Skill `oni-mod`.

## Otevřené otázky / slepé uličky

- Atribut jen pro čtení na složkách modů: nastavuje ho hra při rozbalování modu ze Steamu (potvrzeno na
  modu 3244925649). Brání pak smazání nebo aktualizaci modu. Ručně odstraněn, u nových modů se vrací.
- ❌ IronPython knihovny v kořeni složky modu: hra na nich volá `GetTypes()` → pád na System.Xaml.
- ❌ IronPython typy kdekoli v `PyCPU.dll` (i pole lambdy): Mono je načítá už při `GetTypes`, dřív než `OnLoad`.
- ❌ Reflexe na `Generator.wattageRating` / `animSpeed`: v Unity 6 verzi hry neexistují.
- ❌ Lokální kopie PeterHanových modů: samy se neaktualizují. Brát ze Steamu.
