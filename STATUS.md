# Stav projektu

Aktualizováno: 2026-10-29

## Cíl

Vybrat a rozjet další mod: buď nový, nebo úpravu modu ze Steamu. InfiniteGenerator je odložený.

## Hotovo

- Audit modů ve hře (`docs/mods-audit.md`). Pády způsoboval OneDrive, ne nekompatibilita.
  Složka modů je junction na `<složka mimo OneDrive>`, mody se instalují jen ze Steamu.
- Šablona projektu: git, build s automatickým nasazením do `mods\Dev\<ModName>` (net48, Harmony ze hry),
  `mod/` s yaml soubory, kód rozdělený do `src/`.
- InfiniteGenerator 1.1.0: zkompilovaný v nové struktuře, **odložený**. Hra má teď vlastní DEV budovy
  v sandboxu, které dělají totéž. Ve hře vypnutý.

## Rozpracováno

- Uživatel postupně přidává mody ze Steamu, po každé dávce zkontrolovat Player.log.

## Další krok

1. Rozhodnout, jaký mod dál (nový / úprava cizího), a sepsat krátký spec do `docs/specs/`.
2. Nástroje do `tools/`: `check-log.ps1` (filtr Player.log) a kontrola kompatibility z auditu
   (`patches.py`, `gamemethods.ps1`, zatím ve scratchpadu session 2026-10-29).
3. Skill `oni-mod` z nasbíraných poznatků.

## Otevřené otázky / slepé uličky

- InfiniteGenerator: známé neopravené chyby (dvojitá registrace budovy, výkon ve sdíleném `BuildingDef`,
  výkon se neukládá do savu). Opravit jen pokud se k modu vrátíme.
- ❌ Lokální kopie PeterHanových modů: samy se neaktualizují a po updatu hry zastarají. Brát ze Steamu.
- ❌ Data hry v OneDrive bez junctionu: OneDrive nastavuje složkám jen pro čtení, hra pak nemůže mody
  přeinstalovat a mody zůstanou napůl smazané. Nevracet se.
