# PyCPU – nápověda

> Tento soubor kopíruje mod při každém otevření editoru, vlastní úpravy se přepíšou.

PyCPU je automatizační budova (3×4) se 4 vstupy **IN0–IN3** (vlevo), 4 výstupy **OUT0–OUT3** (vpravo)
a řídicím vstupem **RUN** (uprostřed). Řídí ji skript v Pythonu (IronPython 3).

## Rychlý start

1. Postav PyCPU (záložka Automatizace). V detailu budovy klikni na **Upravit ve VS Code**.
2. Otevře se skript `<název budovy>.py` ze složky `save_files\<kolonie>\pycpu\`.
3. Uprav ho a ulož. Hra ho do 1 s načte znovu (nad budovou se ukáže „skript načten“ nebo chyba).
4. Hotový skript z knihovny nahraješ výběrem v panelu **Knihovna** (např. `priklad_blikac`).

Název budovy = název skriptu. Přejmenováním budovy se přejmenuje i soubor. Dvě CPU se stejným
názvem sdílí jeden skript (každé má ale vlastní `storage`).

## Skript

```python
def start(io, storage):   # nepovinné: po spuštění, po úpravě souboru a po načtení savu
    pass

def tick(io, storage):    # povinné: 5× za sekundu, když CPU běží
    io.out[0] = io.inp[0]

def stop(io, storage):    # nepovinné: když RUN zhasne
    pass
```

| Co | Význam |
|---|---|
| `io.inp[0..3]` | hodnoty vstupů, celé 32bitové číslo |
| `io.out[0..3]` | výstupy; co sem zapíšeš, odešle se po skončení `tick`. Hodnota drží, dokud ji nezměníš |
| `io.tick` | počet ticků od `start()` (první `tick` má 0). `io.tick % 5 == 0` = jednou za sekundu |
| `io.sleep(n)` | přeskočí dalších `n` ticků (`io.sleep(4)` = `tick` jednou za sekundu) |
| `io.log(...)`, `print(...)` | řádek do logu (posledních 20 řádků je v tooltipu budovy) |
| `storage` | dict, který se ukládá do savu; jen `str`, `int`, `float`, `bool`, `None`, `list`, `dict` |

## Signály a ribbon

- Jednoduchý drát nese 0 nebo 1. Ribbon (pásek) nese **4 bity**: bit 0–3 = drát 0–3.
- PyCPU čte a posílá celé 32bitové číslo, ale **budovy ze hry (Ribbon Reader/Writer) pracují jen se
  4 bity**. Vyšší bity přečte jen jiné PyCPU nebo budovy z modů (např. ONICPU).
- Bit čteš `(io.inp[0] >> 2) & 1`, nastavíš `io.out[0] |= 1 << 2`.
- Vstup je hodnota z předchozího ticku logické sítě (zpoždění 1 tick).

## Běh a chyby

- **RUN** nezapojený nebo zelený = běží. Červený = zavolá se `stop()` a výstupy jdou na 0.
- Chyba ve skriptu nebo nekonečná smyčka (limit 10 000 kroků na jedno volání) CPU zastaví,
  výstupy jdou na 0 a chyba je vidět v detailu budovy. Po opravě souboru se skript rozběhne sám.
- **Restart** v panelu načte soubor znovu, **vymaže `storage`** a zavolá `start()`.
  Úprava souboru `storage` zachová.
- Tick je pevně 5 Hz. `io.sleep` ho nezrychlí ani nezpomalí, jen některé ticky vynechá.

## Omezení (sandbox)

- `import` jen `math`, `cmath`, `itertools`. Soubory, `os`, `sys` ani .NET (`clr`) skript nemá.
- Žádné `open`, `input`, `exit`.

## Soubory

| Kde | Co |
|---|---|
| `save_files\<kolonie>\pycpu\*.py` | skripty CPU této kolonie |
| `save_files\<kolonie>\pycpu\_smazane\` | skripty rozebraných CPU a zálohy před nahráním z knihovny |
| `Klei\OxygenNotIncluded\pycpu_library\*.py` | knihovna sdílená všemi koloniemi (tlačítko **Do knihovny**) |

Skript je navíc uložený i v savu: když soubor zmizí, po načtení savu se obnoví.
Příklady `priklad_*.py` mod do knihovny nakopíruje, jen když tam ještě nejsou (smazané se po
restartu hry vrátí, upravené zůstanou).
