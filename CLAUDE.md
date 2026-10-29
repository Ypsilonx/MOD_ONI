# MOD_ONI

Vlastní mody do hry Oxygen Not Included (C#, Harmony), každý v `mods/<ModName>/`:
- **InfiniteGenerator**: generátor elektřiny bez vstupů, hotový a odložený; vzor jednoduchého modu.
- **PyCPU**: programovatelné IO zařízení se skripty v Pythonu (IronPython), ve vývoji, viz `docs/specs/pycpu.md`.

Velikost: M.

Doména: zatím bez L1 skillu. Skill `oni-mod` vznikne ve fázi 3 (viz `STATUS.md`), do té doby
platí pravidla níže.

## Spuštění a ověření

- Build + nasazení: `dotnet build` (celé řešení) nebo `dotnet build mods\<ModName>`. Target `DeployMod`
  zkopíruje DLL a obsah `mod/` do `<složka mimo OneDrive>\Dev\<ModName>\`. Když hra běží, build skončí chybou.
- Ověření ve hře: mod z `Dev\` je po prvním nasazení vypnutý, zapnout v menu Mods. Pak projít
  `%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log` (řádky `[<ModName>]`, `Loading MOD dll`, výjimky). Mod, který shodí hru, hra sama vypne
  (`crash_count` v `mods\mods.json`).

## Mapa projektu

- `Directory.Build.props`: cesty (hra, složka modů) a společné nastavení (net48). Uprav tady.
- `Directory.Build.targets`: reference na DLL hry, nasazení `DeployMod`. Nepřibaluje DLL, které hra už má
  (`System.Memory`…). Mod může vyřadit další DLL přes `<ModDllExclude Include="X.dll" />`.
- `mods/<ModName>/<ModName>.csproj`: jen `ModName` a vlastní závislosti.
- Struktura modu: `src/Mod.cs` (`UserMod2`), `src/Buildings/` (`IBuildingConfig`), `src/Components/`,
  `src/Patches/` (Harmony), `mod/` (`mod.yaml` s názvem a staticID, `mod_info.yaml` s buildem a APIVersion;
  DLC přes `requiredDlcIds`/`forbiddenDlcIds`, `supportedContent` je zastaralé).
- Komponenty, které se ukládají do savu (`InfiniteGeneratorController`), nepřejmenovávat a neměnit jim namespace,
  protože hra je ukládá pod plným jménem typu.
- `animace_názvy.txt`: výpis dostupných animací generátorů ze hry (podklad pro volbu `anim`).
- `docs/mods-audit.md`: audit modů nainstalovaných ve hře a příčina pádů (OneDrive).
- `docs/specs/`: návrhy (spec PyCPU). `docs/plans/`: aktuální plán.

## Rozhodnutí a pravidla tohoto projektu

- Hra: Steam, `<Steam>\steamapps\common\OxygenNotIncluded`, Unity 6, DLC Spaced Out + další.
- Složka modů hry `Dokumenty\Klei\OxygenNotIncluded\mods` (Dokumenty jsou v OneDrive) je **junction
  na `<složka mimo OneDrive>`**. Důvod: OneDrive zamykal složky modů a hra nemohla mody aktualizovat (viz audit).
- Vlastní mody jdou do `mods\Dev\<ModName>\`, cizí mody jen ze Steam Workshopu (žádné lokální kopie).
- Harmony brát ze hry (`Managed\0Harmony.dll`), ne z NuGetu. Cílit na framework hry (net48).
- ID budovy `InfiniteGenerator` neměnit, rozbily by se savy.
- Po každém updatu hry: přebuildovat a zkontrolovat log.

## Body napojení

| Vstupní bod | Co dělá | Na co si dát pozor |
|---|---|---|
| `KMod.UserMod2.OnLoad(Harmony)` | start modu, `base.OnLoad` aplikuje `[HarmonyPatch]` třídy | běží velmi brzy, `Db` ani `Assets` ještě neexistují |
| `GeneratedBuildings.LoadGeneratedBuildings` (prefix) | `Strings.Add`, `ModUtil.AddBuildingToPlanScreen` | hra `IBuildingConfig` z modů registruje **sama**, ruční `RegisterBuilding` vede k duplicitě (`duplicate tag` v logu) |
| `IBuildingConfig` | definice budovy | ID budovy neměnit (savy) |
| `Db.Initialize` (postfix) | tech strom | ID technologií se mezi updaty mění |
| `[SerializationConfig(OptIn)]` + `[Serialize]` | stav komponenty v savu | bez toho se hodnota po načtení vrátí na výchozí; typ se ukládá pod plným jménem |
| `ISim200ms.Sim200ms(dt)` | periodická logika | volá se pro každou instanci, reflexi cachovat |
| `mod_info.yaml` | `minimumSupportedBuild`, `APIVersion: 2`, `requiredDlcIds`/`forbiddenDlcIds` | `supportedContent` je zastaralé; `APIVersion` ≠ 2 → DLL se nenačte |

Testování ve hře: sandbox (`Shift+S`), debug režim přes prázdný `OxygenNotIncluded_Data\debug_enable.txt`
(`Backspace` zap/vyp, `Ctrl+F4` okamžité stavění, `Ctrl+F2` spawn dupliňáka). Konzole s příkazy ve hře není.

Stav práce: viz `STATUS.md`.
