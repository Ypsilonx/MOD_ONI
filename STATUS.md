# Stav projektu

Aktualizováno: 2026-10-31

## Cíl

PyCPU: budova s 32bitovými ribbon porty řízená Python skriptem (`tick()` 5×/s), skripty jako `.py`
vedle savu, editace ve VS Code. Spec: `docs/specs/pycpu.md`.

## Hotovo

- Audit modů, mody jen ze Steamu. Složka modů je od 2026-10-31 skutečná složka v OneDrive,
  build nasazuje přímo tam.
- Skill `oni-mod` (2026-10-31): obecná pravidla a pasti ONI přesunuté z tohoto projektu.
- Repo pro víc modů (`mods/<Mod>/`), společný build s nasazením (`Directory.Build.props/.targets`).
- InfiniteGenerator 1.2.0: hotový, odložený (hra má vlastní DEV generátor).
- **Spike PyCPU úspěšný ve hře:** IronPython běží, limit kroků funguje, tick ~1–5 µs.
  Vyřešené podmínky: knihovny v `lib\`, `PyCPU.dll` bez reference na IronPython, `DllStripper` na Microsoft.Dynamic.
- PyCPU task 1 (budova 3×4, porty, echo) ověřen ve hře; task 2 (script host, sandbox, limit kroků, storage)
  s testy `dotnet test tests\PyCPU.Scripting.Tests` (25 testů).
- PyCPU task 3 (skripty jako `.py` v `save_files\<kolonie>\pycpu\`, přenačtení do 1 s, storage v savu)
  ověřen ve hře včetně limitu kroků (`Thread.Abort` na Unity Mono funguje). Textura: popisky IN/OUT/RUN, „Python“.
- PyCPU task 4a ověřen ve hře: stav a živé IN/OUT v detailu budovy, potvrzení po načtení, tečky PROGRAM/ERROR,
  tlačítka VS Code a Restart, `io.sleep`, `io.tick` (28 testů).
- PyCPU task 4b ověřen ve hře: `cpu_<id>.py` se přejmenuje spolu s CPU, stejný název = sdílený skript,
  knihovna `Klei\OxygenNotIncluded\pycpu_library` (tlačítko „Do knihovny“, výběr v panelu), koš `pycpu\_smazane`.
- PyCPU task 5 přijat (doladí se podle používání): `mod/README.md` (nápověda, kopíruje se jako `NAPOVEDA.md` do `pycpu\`
  při „Upravit ve VS Code“), 3 příklady `mod/examples/priklad_*.py` → knihovna při startu (nepřepisuje), 34 testů.

## Rozpracováno

- PyCPU podle `docs/plans/2026-10-29-pycpu.md`: zbývá task 6 (nezávislá revize kódu), pak 1.0.

## Další krok

1. PyCPU task 6: nezávislá revize kódu. Varování „not added to plan screen subcategory“ (BuildingTuning) zatím neřešeno.
2. Malý mod, který při startu hry odstraní atribut jen pro čtení ze složek modů (viz níže).
3. Nástroje do `tools/`: filtr Player.log a kontrola kompatibility modů (dekompilace přes ilspycmd,
   porovnání cílů Harmony patchů s aktuální `Assembly-CSharp.dll`).

## Otevřené otázky / slepé uličky

Obecné slepé uličky ONI (junction v OneDrive, `GetTypes` na knihovnách, `BigInteger`, průhledné textury,
lokální kopie cizích modů…) jsou ve skillu `oni-mod` → Pasti. Tady jen projektové:

- Atribut jen pro čtení na složkách modů ze Steamu se u nových modů vrací (potvrzeno na modu 3244925649),
  zatím ručně. Řešením by byl bod 2 výše.
- ❌ Limit kroků výjimkou z trace funkce: `try/except` ve skriptu ji spolkne a IronPython pak trace vypne
  (smyčka běží dál). Řešeno `Thread.CurrentThread.Abort` + `ResetAbort` v `PythonBackend.RunLimited`.
- ❌ Reflexe na `Generator.wattageRating` / `animSpeed`: v Unity 6 verzi hry neexistují.
