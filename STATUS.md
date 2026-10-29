# Stav projektu

Aktualizováno: 2026-10-29

## Cíl

Standardizovat mod InfiniteGenerator. Hotovo bude, až `dotnet build` sám nasadí mod do `mods\Dev\InfiniteGenerator\`,
hra ho načte bez chyb a generátor si po uložení a načtení savu pamatuje výkon.

## Hotovo

- Audit modů ve hře (`docs/mods-audit.md`). Pády způsoboval OneDrive, ne nekompatibilita.
- Všechny staré mody smazány, složka modů přesměrována junctionem na `<složka mimo OneDrive>`.
- Task 1 plánu: git, CLAUDE.md, STATUS.md.

## Rozpracováno

- Plán `docs/plans/2026-10-29-standardizace-modu.md` (tasky 2–5).
- Uživatel postupně přidává mody ze Steamu, po každé dávce zkontrolovat Player.log.

## Další krok

1. Task 2: build a automatické nasazení (net48, Harmony ze hry, Directory.Build.props, mod.yaml).
   Hotovo, až po `dotnet build` hra mod z Dev složky načte bez výjimek.

## Otevřené otázky / slepé uličky

- Publikovat mod na Steam Workshop? Zatím ne.
- ❌ Lokální kopie PeterHanových modů: samy se neaktualizují a po updatu hry zastarají. Brát ze Steamu.
- ❌ Data hry v OneDrive bez junctionu: OneDrive nastavuje složkám jen pro čtení, hra pak nemůže mody
  přeinstalovat a mody zůstanou napůl smazané. Nevracet se.
