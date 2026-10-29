# Plán: standardizace vlastního modu (fáze 2)

Cíl: `dotnet build` sám sestaví mod proti aktuální hře a nasadí ho do `mods\Dev\<ModName>\`.
Hra ho načte bez chyb v `Player.log` a generátor funguje včetně uložení a načtení savu.

## Klíčová rozhodnutí

- **Umístění ve hře:** `mods\Dev\<ModName>\` (= `<složka mimo OneDrive>\Dev\…`). Hra tuto složku bere jako
  lokální vývoj, ze Steamu se do ní nic nepřepisuje.
- **Framework net48** (hra je na Unity 6) místo net472. **Harmony ze hry** (`Managed\0Harmony.dll`, 2.4.2).
  Žádné NuGet balíčky, které by se kopírovaly do modu.
- **Cesty do `Directory.Build.props`**: `GameDir`, `ModsDir`, `ModName`. Žádné natvrdo zapsané
  `<Steam>\…` v csproj.
- **Struktura podle témat:** `src/Mod.cs`, `src/Buildings/`, `src/Components/`, `src/Patches/`,
  `mod/` (`mod.yaml`, `mod_info.yaml`, později `anim/`).

## Tasky

1. **Základ repa:** `git init`, `.gitignore` (bin/obj), projektový `CLAUDE.md` a `STATUS.md`
   (skill project-standards). *Ověření:* `git status` ukazuje jen zdrojové soubory.
2. **Build a nasazení:** `Directory.Build.props`; csproj na net48 s referencemi ze hry
   (`Private=false`); NuGet Harmony pryč; target po buildu kopíruje DLL + `mod/*` do Dev složky.
   Pokud hra běží (DLL je zamčená), build skončí srozumitelnou chybou. Přidat `mod.yaml`
   (title, description, staticID). *Ověření:* `dotnet build` → soubory v `<složka mimo OneDrive>\Dev\<ModName>`,
   v logu `Loading MOD dll` bez výjimek.
3. **Rozdělení kódu do souborů** + české docstringy, beze změny chování.
   *Ověření:* build + generátor se ve hře postaví a vyrábí.
4. **Opravy chyb generátoru:** výkon pro každou instanci zvlášť (`[Serialize]`), nepřepisovat sdílený
   `BuildingDef`, reflexi cachovat; prověřit, jestli ruční `RegisterBuilding` nezpůsobuje
   dvojitou registraci (hra `IBuildingConfig` z modů registruje sama).
   *Ověření:* dva generátory s různým výkonem, po uložení a načtení savu výkon zůstane.
5. **Nástroje:** `tools/check-log.ps1` (z `Player.log` vytáhne řádky našeho modu, výjimky a pády);
   do `tools/compat/` přesunout `patches.py` + `gamemethods.ps1` z auditu. *Ověření:* jedno spuštění
   nad aktuálním logem.
6. *(fáze 3)* Skill `oni-mod` z poznatků fází 1–2.

## Body napojení na hru

| Vstupní bod | Co dělá | Na co si dát pozor |
|---|---|---|
| `KMod.UserMod2.OnLoad(Harmony)` | start modu, Harmony patche se aplikují automaticky (`base.OnLoad`) | běží velmi brzy – `Db`, `Assets` ještě neexistují |
| `GeneratedBuildings.LoadGeneratedBuildings` (prefix) | `Strings.Add`, `ModUtil.AddBuildingToPlanScreen` | stringy musí být před registrací budovy |
| `IBuildingConfig` (`CreateBuildingDef`, `ConfigureBuildingTemplate`, `DoPostConfigureComplete`) | definice budovy | hra konfigurace hledá sama; ID budovy neměnit, jinak se rozbijí savy |
| `Db.Initialize` (postfix) | zařazení do tech stromu | ID technologií se mezi updaty mění |
| `[SerializationConfig(OptIn)]` + `[Serialize]` | ukládání stavu komponenty do savu | bez toho se hodnota po načtení vrátí na výchozí |
| `ISim200ms.Sim200ms(dt)` | periodická logika | volá se pro každou instanci, nedělat v něm reflexi bez cache |
| `mod_info.yaml` | `minimumSupportedBuild`, `APIVersion: 2`, DLC pole | při `APIVersion` ≠ 2 hra DLL nenačte („not using the correct API version“) |

Obecné pasti: po každém updatu hry přebuildovat; za běhu hry nejde DLL přepsat; když mod
shodí hru, hra ho sama vypne (`crash_count` v `mods.json`); log je v
`%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log`.

## Otevřené otázky

- **Název modu / staticID:** `power_gen_ypsix`, `MOD_ONI`, nebo nový (např. `InfiniteGenerator`)?
  ID budovy `InfiniteGenerator` zůstává, takže staré savy fungovat budou.
- Publikovat později na Steam Workshop? Teď ne, jen připravit `mod.yaml` tak, aby to šlo.
