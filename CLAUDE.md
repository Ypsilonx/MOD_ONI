# MOD_ONI

Vlastní mody do hry Oxygen Not Included (C#, Harmony), každý v `mods/<ModName>/`:
- **InfiniteGenerator**: generátor elektřiny bez vstupů, hotový a odložený; vzor jednoduchého modu.
- **PyCPU**: programovatelné IO zařízení se skripty v Pythonu (IronPython), ve vývoji, viz `docs/specs/pycpu.md`.

Velikost: M. Doména: skill `oni-mod` (struktura, ověřování ve hře, body napojení, pasti). Tento projekt je
pro skill referenční implementace: nový obecný poznatek o ONI zapsat do skillu, sem jen projektové věci.

## Spuštění a ověření

- Build + nasazení: `dotnet build` (celé řešení) nebo `dotnet build mods\<ModName>`. Kopíruje do
  `%USERPROFILE%\OneDrive\Dokumenty\Klei\OxygenNotIncluded\mods\Dev\<ModName>\`. Když hra běží, build skončí chybou.
- **Po každém commitu spustit `dotnet build`**, ať je kopie ve hře aktuální. Hra čte jen tuhle kopii.
- Testy (Python host PyCPU, bez hry): `dotnet test tests\PyCPU.Scripting.Tests`.
- Ověření ve hře a čtení `Player.log`: viz skill `oni-mod` → Ověřování.

## Mapa projektu

- `Directory.Build.props`: cesty (hra, `ModsDir`) a společné nastavení (net48). **Cesty upravit tady.**
- `Directory.Build.targets`: reference na DLL hry, `StripModDlls`, nasazení `DeployMod`
  (druhy projektů `ModName` / `ModLibOf`, popsáno v hlavičce souboru).
- `mods/InfiniteGenerator/`: budova, posuvník výkonu, patch `Generator.WattageRating`, tech strom.
- `mods/PyCPU/`: herní část (budova, porty, boční panel, soubory skriptů). Na IronPython neodkazuje.
- `mods/PyCPU.Scripting/` → `lib\`: IronPython host, sandbox, limit kroků, `storage`.
- `tests/PyCPU.Scripting.Tests/`: xUnit testy hostu. `tools/DllStripper/`: ořezání `Microsoft.Dynamic.dll`.
- `animace_názvy.txt`: výpis animací generátorů ze hry (podklad pro volbu `anim`).
- `docs/specs/pycpu.md`: návrh PyCPU. `docs/plans/`: aktuální plán.

## Rozhodnutí a pravidla tohoto projektu

- Hra: Steam, `<Steam>\steamapps\common\OxygenNotIncluded`, Unity 6, DLC Spaced Out + další.
- Složka modů je **skutečná složka v OneDrive**, build do ní kopíruje. Junction na `<složka mimo OneDrive>` byl zrušen
  2026-10-31, protože ho OneDrive nesynchronizoval. `<složka mimo OneDrive>` je stará kopie, nic ji nepoužívá.
- ID budov `InfiniteGenerator` a `PyCPU` a typy v savu (`InfiniteGeneratorController` v globálním namespace,
  `PyCPU.PyCpu`) neměnit.
- PyCPU: skripty `save_files\<kolonie>\pycpu\<název budovy>.py`, knihovna `Klei\OxygenNotIncluded\pycpu_library`.

## Body napojení (navíc ke skillu)

| Vstupní bod | Co dělá | Na co si dát pozor |
|---|---|---|
| `LibResolver.Register()` v `OnLoad` | `AssemblyResolve` na `lib\` | musí proběhnout před prvním použitím `ScriptBackendLoader` |
| `ScriptBackendLoader.Get()` | načte `lib\PyCPU.Scripting.dll` | nikdy před `Assets.OnPrefabInit`; první vytvoření enginu ~0,6 s |
| `IScriptBackend` / `IScriptInstance` | jediné rozhraní mezi `PyCPU.dll` a Pythonem | Python volat jen ze `Sim200ms` (hlavní vlákno), engine není thread-safe |

Stav práce: viz `STATUS.md`.
