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
- PyCPU task 1 (budova 3×4, porty, echo) ověřen ve hře; task 2 (script host, sandbox, limit kroků, storage)
  s testy `dotnet test tests\PyCPU.Scripting.Tests` (25 testů).

## Rozpracováno

- PyCPU podle `docs/plans/2026-10-29-pycpu.md`, další je task 3 (napojení na hru a soubory).

## Další krok

1. PyCPU task 3; ve hře ověřit i limit kroků (`Thread.Abort` na hlavním vlákně Unity Mono).
2. Malý mod, který při startu hry odstraní atribut jen pro čtení ze složek modů (viz níže).
3. Nástroje do `tools/`: filtr Player.log a kontrola kompatibility z auditu.
4. Skill `oni-mod`.

## Otevřené otázky / slepé uličky

- Atribut jen pro čtení na složkách modů: nastavuje ho hra při rozbalování modu ze Steamu (potvrzeno na
  modu 3244925649). Brání pak smazání nebo aktualizaci modu. Ručně odstraněn, u nových modů se vrací.
- ❌ IronPython knihovny v kořeni složky modu: hra na nich volá `GetTypes()` → pád na System.Xaml.
- ❌ IronPython typy kdekoli v `PyCPU.dll` (i pole lambdy): Mono je načítá už při `GetTypes`, dřív než `OnLoad`.
- ❌ Limit kroků výjimkou z trace funkce: `try/except` ve skriptu ji spolkne a IronPython pak trace vypne
  (smyčka běží dál). Řešeno `Thread.CurrentThread.Abort` + `ResetAbort` v `PythonBackend.RunLimited`.
- ❌ `BigInteger` v JSON: Newtonsoft ve hře ho nepodporuje, storage je omezené na 64bitová čísla.
- ❌ Reflexe na `Generator.wattageRating` / `animSpeed`: v Unity 6 verzi hry neexistují.
- ❌ Lokální kopie PeterHanových modů: samy se neaktualizují. Brát ze Steamu.
