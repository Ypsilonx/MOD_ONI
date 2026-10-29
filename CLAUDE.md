# MOD_ONI

Vlastní mody do hry Oxygen Not Included (C#, Harmony). Teď obsahuje jeden mod, **InfiniteGenerator**:
generátor elektřiny bez vstupů s nastavitelným výkonem (testovací/sandbox budova). Velikost: M.

Doména: zatím bez L1 skillu. Skill `oni-mod` vznikne ve fázi 3 (viz `STATUS.md`), do té doby
platí pravidla níže.

## Spuštění a ověření

- Build: `dotnet build` (hra musí být vypnutá, jinak je DLL ve složce modu zamčená).
- Nasazení: zatím ručně (`bin\` → `mods\Dev\InfiniteGenerator\`). Task 2 plánu to zautomatizuje.
- Ověření ve hře: spustit hru, pak projít `%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log`
  (řádky `[MOD_ONI]`, `Loading MOD dll`, výjimky). Mod, který shodí hru, hra sama vypne
  (`crash_count` v `mods\mods.json`).

## Mapa projektu

- `MOD_ONI.cs`: celý mod v jednom souboru (UserMod2, config budovy, komponenta, Harmony patche).
  Task 3 plánu ho rozdělí.
- `mod_info.yaml`: metadata pro hru (build, APIVersion).
- `animace_názvy.txt`: výpis dostupných animací generátorů ze hry (podklad pro volbu `anim`).
- `docs/mods-audit.md`: audit modů nainstalovaných ve hře a příčina pádů (OneDrive).
- `docs/plans/`: aktuální plán.

## Rozhodnutí a pravidla tohoto projektu

- Hra: Steam, `<Steam>\steamapps\common\OxygenNotIncluded`, Unity 6, DLC Spaced Out + další.
- Složka modů hry `Dokumenty\Klei\OxygenNotIncluded\mods` (Dokumenty jsou v OneDrive) je **junction
  na `<složka mimo OneDrive>`**. Důvod: OneDrive zamykal složky modů a hra nemohla mody aktualizovat (viz audit).
- Vlastní mody jdou do `mods\Dev\<ModName>\`, cizí mody jen ze Steam Workshopu (žádné lokální kopie).
- Harmony brát ze hry (`Managed\0Harmony.dll`), ne z NuGetu. Cílit na framework hry (net48).
- ID budovy `InfiniteGenerator` neměnit, rozbily by se savy.
- Po každém updatu hry: přebuildovat a zkontrolovat log.

## Body napojení

Viz tabulka v `docs/plans/2026-10-29-standardizace-modu.md` (UserMod2.OnLoad, LoadGeneratedBuildings,
IBuildingConfig, Db.Initialize, serializace, Sim200ms, mod_info.yaml). Po dokončení plánu se přesune sem.

Stav práce: viz `STATUS.md`.
