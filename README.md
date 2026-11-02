# Oxygen Not Included – mody od Ypsilonx

[![Licence: MIT](https://img.shields.io/badge/licence-MIT-green.svg)](LICENSE)
[![Release](https://img.shields.io/github/v/release/Ypsilonx/MOD_ONI?include_prereleases&label=release)](https://github.com/Ypsilonx/MOD_ONI/releases)
[![Oxygen Not Included](https://img.shields.io/badge/Oxygen%20Not%20Included-Spaced%20Out%20%2B%20z%C3%A1klad-orange.svg)](https://www.klei.com/games/oxygen-not-included)
[![C#](https://img.shields.io/badge/C%23-.NET%20Framework%204.8-512BD4.svg?logo=dotnet)](https://dotnet.microsoft.com/)
[![Harmony](https://img.shields.io/badge/Harmony-2-blue.svg)](https://github.com/pardeike/Harmony)
[![IronPython](https://img.shields.io/badge/IronPython-3.4-3776AB.svg?logo=python&logoColor=white)](https://ironpython.net/)
[![Platforma](https://img.shields.io/badge/testov%C3%A1no-Windows-0078D6.svg?logo=windows)](#instalace)

Dva mody do hry [Oxygen Not Included](https://www.klei.com/games/oxygen-not-included).
Mody nejsou na Steam Workshopu, instalují se ručně ze zipu z [Releases](https://github.com/Ypsilonx/MOD_ONI/releases).
Fungují v základní hře i v DLC Spaced Out. Texty ve hře jsou česky.

| Mod | Verze | Co dělá |
|---|---|---|
| [**PyCPU**](#pycpu) | 0.9.0 (pre-release) | automatizační budova řízená vlastním skriptem v Pythonu |
| [**Infinite Generator**](#infinite-generator) | 1.2.0 | generátor elektřiny bez vstupů s nastavitelným výkonem |

---

## PyCPU

Programovatelná automatizační budova 3×4 (záložka **Automatizace → Logická hradla**). Má 4 vstupy
**IN0–IN3**, 4 výstupy **OUT0–OUT3** (ribbon porty) a řídicí vstup **RUN**. Chování určuje skript
v Pythonu, který hra spouští 5× za sekundu:

```python
def tick(io, storage):
    # blikač: výstup 0 se přepne každou sekundu
    if io.tick % 5 == 0:
        io.out[0] = 1 - io.out[0]
```

- **Skripty jsou obyčejné `.py` soubory** vedle savu (`save_files\<kolonie>\pycpu\<název budovy>.py`).
  Tlačítko **Upravit ve VS Code** otevře skript v editoru (VS Code, jinak výchozí program pro `.py`).
  Po uložení se skript do 1 s načte znovu, hru není třeba restartovat.
- **`storage`**: slovník, který se ukládá do savu (čítače, stavy automatů…).
- **Knihovna skriptů** sdílená mezi koloniemi a tři ukázkové skripty (blikač, čítač, práce s piny).
- **Sandbox**: skript nemá přístup k souborům, `os`, `sys` ani .NET. Nekonečnou smyčku zastaví
  limit kroků a chyba je vidět v detailu budovy.

Kompletní nápověda (API `io`, signály a ribbony, omezení) je v [mods/PyCPU/mod/README.md](mods/PyCPU/mod/README.md).
Ve hře se kopíruje jako `NAPOVEDA.md` vedle skriptů. Návrh modu: [docs/specs/pycpu.md](docs/specs/pycpu.md).

> **Stav 0.9:** všechny funkce jsou ověřené ve hře, verze 1.0 vyjde po revizi kódu.
> Chyby a nápady prosím hlaste v [Issues](https://github.com/Ypsilonx/MOD_ONI/issues).

## Infinite Generator

Generátor elektřiny 1×1 bez paliva a bez vstupů (záložka **Elektřina → Generátory**, technologie
*Power Regulation*). Výkon se nastavuje posuvníkem v detailu budovy (100–10 000 W, výchozí 5 000 W),
každá budova má svůj. Hodí se na testování staveb a sandbox. V normální hře je to podvod.

---

## Instalace

1. Stáhněte zip modu z [Releases](https://github.com/Ypsilonx/MOD_ONI/releases).
2. Rozbalte ho do složky lokálních modů tak, aby vznikla `...\mods\Local\<NázevModu>\` s `mod.yaml` uvnitř:
   - Windows: `Dokumenty\Klei\OxygenNotIncluded\mods\Local\` (bývá i v `OneDrive\Dokumenty\…`)
   - Linux: `~/.config/unity3d/Klei/Oxygen Not Included/mods/Local/`
   - macOS: `~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/`
3. Spusťte hru, v menu **Mods** mod zapněte a nechte hru restartovat.

Testováno na Windows. Na Linuxu a macOS by mody měly fungovat také, PyCPU tam ale neumí otevřít VS Code
tlačítkem (skript upravíte v libovolném editoru, hra ho načte sama).

**Aktualizace:** smažte starou složku modu a rozbalte novou verzi. Savy i skripty PyCPU zůstanou.

## Sestavení ze zdrojáků

Potřebujete .NET SDK (testováno s verzí 10) a nainstalovanou hru (DLL hry se berou z její instalace, v repu nejsou).

```powershell
git clone https://github.com/Ypsilonx/MOD_ONI.git
cd MOD_ONI
dotnet build              # sestaví oba mody a nasadí je do mods\Dev\<Mod>\
dotnet test tests\PyCPU.Scripting.Tests   # testy Python hostu (bez hry)
```

IronPython se stáhne z NuGetu sám. Build mody kopíruje do `mods\Dev\`. Když hra běží, DLL je
zamčená a build skončí chybou.

Pokud máte hru jinde než ve výchozí instalaci Steamu, vytvořte vedle `Directory.Build.props`
soubor `Directory.Build.user.props` (je v `.gitignore`):

```xml
<Project>
  <PropertyGroup>
    <GameDir>D:\SteamLibrary\steamapps\common\OxygenNotIncluded</GameDir>
    <!-- nepovinné, jinak se odvodí z Dokumentů: -->
    <!-- <ModsDir>C:\Users\Já\Documents\Klei\OxygenNotIncluded\mods</ModsDir> -->
  </PropertyGroup>
</Project>
```

### Struktura repa

```
mods/InfiniteGenerator/   mod generátoru
mods/PyCPU/               herní část PyCPU (budova, porty, panel, soubory skriptů)
mods/PyCPU.Scripting/     IronPython host, nasazuje se do PyCPU\lib\
tests/                    xUnit testy Python hostu
tools/DllStripper/        ořezání Microsoft.Dynamic.dll o typy, které by hru shodily
docs/                     návrh a plán PyCPU
```

## Licence a poděkování

- Kód je pod licencí [MIT](LICENSE).
- PyCPU přibaluje **IronPython 3.4.2** a DLR (© .NET Foundation, [Apache 2.0](mods/PyCPU/mod/LICENSE-IronPython.txt)).
- Animace budovy PyCPU pochází z modu [ONICPU](https://github.com/imengyu/ONICPU) od imengyu
  ([MIT](mods/PyCPU/mod/LICENSE-ONICPU.txt)).
- Oxygen Not Included je ochranná známka Klei Entertainment. Mody jsou neoficiální a s Klei nesouvisí.
  Repo neobsahuje žádné soubory hry.
