# Spec: PyCPU – programovatelné IO zařízení s Pythonem

## Cíl

Budova s logickými vstupy a výstupy, kterou řídí krátký skript v Pythonu (`tick()` 5× za sekundu).
Musí fungovat na aktuální verzi hry (U59, Unity 6) a ovládat se pohodlněji než ONICPU.

## Rozhodnutí: vlastní mod, ne fork ONICPU

Analýza ONICPU (MIT, imengyu, poslední commit 2026-01):
- **Dobré a převezmeme ideu:** abstrakce executoru (`Start`/`Stop`/`ExecuteTick`/`Save`/`Restore`),
  model pinů `io.P0…`, `storage` ukládané do savu, 32bitové hodnoty na ribbon portech.
- **Důvod nepřebírat celé:** editor je 707 řádků ručně pozicovaného UI s pevnými pixely
  (`anchoredPosition = new Vector2(width - 84, 0)`). Odtud pochází i nahlášený překryv paměti
  v editoru. K tomu 3 jazyky, ILMerge a NiL.JS. Fork by znamenal udržovat cizí kód, který
  stejně z velké části přepíšeme.
- **Soužití:** ONICPU může zůstat nainstalované kvůli digitálním senzorům a displejům. Na logické síti
  jsou to jen celá čísla, takže PyCPU jejich hodnoty přečte. Kód z ONICPU přebíráme jen výjimečně,
  vždy s uvedením licence MIT.

## Klíčová rozhodnutí

1. **Engine: IronPython 3.4** (čistý .NET, syntaxe Pythonu 3.4, běží na Mono). Riziko: kompatibilita
   s Unity 6 Mono (Reflection.Emit, závislosti DLR). → **Ověřit jako úplně první task (spike).**
2. **Ochrana proti zamrznutí:** limit kroků na jeden `tick` (trace s počítadlem). Po překročení se skript
   zastaví s chybou a hra neztuhne. Mechanismus ověřit ve spiku.
3. **Skripty jako externí `.py` soubory, editace ve VS Code** (rozhodnuto 2026-10-29):
   - Skripty kolonie leží vedle savu: `save_files\<kolonie>\pycpu\<název>.py` (zálohuje se s OneDrive).
     Každá budova odkazuje na skript názvem. Změnu souboru mod pozná a načte ji automaticky.
   - Do savu se ukládá i kopie zdrojáku: save funguje i bez souborů, při načtení se soubor obnoví.
   - Tlačítko „Upravit ve VS Code“ spustí `code <soubor>`. Hra běží v režimu okna bez rámečku, takže se
     editor objeví nad ní.
   - Knihovna pro další hry: `Klei\OxygenNotIncluded\pycpu_library\`, tlačítka „Uložit do knihovny“ a
     „Načíst z knihovny“.
   - UI ve hře je malé: skript, stav, log, hodnoty pinů a tlačítka výše. Editor ve hře (jako ONICPU)
     zamítnut: velký a křehký kus UI, psaní kódu v něm je nepohodlné.
4. **API skriptu (návrh):**
   ```python
   def tick(io, storage):      # povinné, 5× za s
       io.out[0] = io.inp[0] and not io.inp[1]
       io.log(f"P0={io.inp[0]}")
   def start(io, storage): ...  # volitelné
   def stop(io, storage): ...   # volitelné
   ```
   `storage` je dict uložený do savu (jen JSON typy). Přístup k .NET (`import clr`) zakázat.
5. **Hardware:** jedna budova 2×2, 4 ribbon vstupy + 4 ribbon výstupy (32bit) a kontrolní port
   „běží/stop“. Další velikosti až podle potřeby.

## Výsledek spiku (2026-10-29, U59-744825)

IronPython 3.4.2 ve hře běží. První vytvoření enginu ~0,6 s (jednorázově), 1 000 volání `tick` ~1 ms,
s hlídáním kroků (`Python.SetTrace`) ~5 ms. Nekonečná smyčka se zastaví po limitu kroků (10 000 kroků ≈ 0,14 s).
`import clr` funguje, takže skript dosáhne na celé .NET a sandbox je nutný.
Podmínky běhu ve hře (zjištěné pádem):
- Hra volá `GetTypes()` na každé DLL v kořeni modu a při `Assets.OnPrefabInit` na **všech** assembly
  v procesu, bez ošetření chyb. `Microsoft.Dynamic` proto při buildu prochází přes `tools/DllStripper`
  (odstraní typy závislé na System.Xaml/Remoting).
- `PyCPU.dll` nesmí na IronPython odkazovat vůbec (Mono načítá typy polí už při `GetTypes`).
  Python žije v `lib\PyCPU.Scripting.dll` za rozhraním `IScriptBackend` a načítá se až po startu hry.

## Struktura

- `mods/PyCPU/` (kořen složky modu, bez IronPythonu): `src/Mod.cs`, `src/LibResolver.cs`,
  `src/Scripting/` (rozhraní `IScriptBackend`, loader), `src/Buildings/`, `src/Components/` (ticky, porty,
  ukládání), `src/UI/` (boční panel), `mod/`.
- `mods/PyCPU.Scripting/` → `lib\`: IronPython host, limit kroků, sandbox, API `io`/`storage`.
- `tools/DllStripper/`: úprava přibalených DLL při buildu.

## Rozhodnuto (2026-10-29)

Název `PyCPU`; porty přenášejí 32bitová čísla (kompatibilní s ONICPU senzory); velikost IronPythonu nevadí.
