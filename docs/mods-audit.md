# Audit nainstalovaných modů ONI

> **Aktualizace 2026-10-29:** pády hry nezpůsobovala nekompatibilita modů, ale OneDrive. Složky modů
> měl OneDrive označené jen pro čtení, takže hra po aktualizaci ze Steamu nemohla smazat starou kopii
> a mody (True Tiles, Stairs…) zůstaly napůl odinstalované. Všechny mody jsme proto smazali a složka
> `Dokumenty\Klei\OxygenNotIncluded\mods` je teď **junction na `<složka mimo OneDrive>`**, tedy mimo OneDrive
> (savy v OneDrive zůstaly). Hra cestu `Dokumenty\Klei` nastavit neumí, má ji natvrdo v kódu
> (`Util.GetKleiRootPath`). Mody se budou přidávat znovu a postupně. Tabulky níže popisují stav
> **před** vyčištěním.

Stav k 2026-10-29. Hra: build **744825**, Unity 6 (6000.3.5), DLC: Spaced Out + dlc2–dlc5.
Složka modů: `%USERPROFILE%\OneDrive\Dokumenty\Klei\OxygenNotIncluded\mods\` (Dokumenty jsou v OneDrive).
Pořadí načítání a zapnutí = `mods\mods.json` (pořadí v poli `mods`, zapnutí přes `enabledForDlc`).

Metoda: metadata (`mod.yaml`, `mod_info.yaml`, `mods.json`), dekompilace všech 52 DLL (ilspycmd),
extrakce cílů Harmony patchů a jejich porovnání s metadaty aktuální `Assembly-CSharp.dll`.
**Neověřeno za běhu** – poslední `Player.log` je z prosince 2025 (build 704096), před updatem na Unity 6.

## Zapnuté mody (16) – Spaced Out

| # | Mod | Zdroj | Verze / min. build | Hodnocení |
|---|-----|-------|--------------------|-----------|
| 1 | power_gen_ypsix (tvůj) | Local | 1.0.0 / 469300 | DLL z 2025-11, zkompilováno proti staré hře → přebuildovat (fáze 2) |
| 3 | Airlock Door | Local (PeterHan) | 3.14 / 699077 | OK, existuje novější balík |
| 13 | Falling Sand | Local (PeterHan) | 3.2 / 493472 | DLL z 2022 – stará, aktualizovat |
| 26 | Queue For Sinks | Local (PeterHan) | 3.14 / 493472 | DLL z 2024 – aktualizovat |
| 27 | Research Queue | Local (PeterHan) | 3.5 / — | OK |
| 29 | Show Building Ranges | Local (PeterHan) | 3.6 / — | DLL z 2024 – aktualizovat |
| 42 | Underground Conduit | Steam | 2.2.9 / 469369 | nese vlastní **PLib 4.17.1** (viz pořadí) |
| 43 | Advanced Coolers | Steam | 2025.08.11 | nese vlastní **PLib 4.9** (starší) |
| 44 | Dumping Sign | Steam | 2024.09.07 | OK |
| 45 | Geyser Control | Steam | 1.0.6 / 674504 | OK |
| 47 | Freezer | Steam | 1.0.10 | OK |
| 48 | Insulated Door | Steam | 1.1.4 | OK |
| 49 | True Tiles | Steam | 1.3.1 | 3 transpilery na `BlockTileRenderer` – citlivé na update hry |
| 50 | Better Air Filters | Steam | 1.2.0 | OK |
| 51 | Bigger Camera Zoom Out | Steam | 2022.10.7 | transpiler na `ClusterMapScreen.OnKeyDown` – citlivé na update |
| 52 | Steel Ladder | Steam | 2022.10.10 | OK |

Všechny cílové metody patchů zapnutých modů v buildu 744825 **existují** (kontrola podle jména,
ne podle signatury – změněné parametry nebo vnitřek metody u transpilerů se tím neodhalí).

## Kolize patchů mezi zapnutými mody

Žádná skutečná. Překryvy jsou jen na registračních hácích, kde všechny mody dělají postfix/prefix
bez přeskočení originálu – nevadí si a pořadí je jedno:
`Db.Initialize`, `Localization.Initialize`, `GeneratedBuildings.LoadGeneratedBuildings`,
`CameraController.OnPrefabInit` (Bigger Camera Zoom Out + Show Building Ranges).

Ostatní nalezené kolize (17 celkem) jsou mezi vypnutými mody nebo zapnutým × vypnutým – zatím irelevantní.

## Pořadí – kde na něm záleží

1. **Underground Conduit (PLib 4.17.1) musí být před Advanced Coolers (PLib 4.9).** Oba nesou
   samostatné `PLib.dll` se stejným jménem assembly; Mono použije tu, která se načte první.
   Opačně by Underground Conduit dostal starší PLib → pravděpodobně `MissingMethodException`.
   **Aktuální pořadí je správné (42 → 43), neměnit.**
2. True Tiles Material Pack (vypnutý) – pokud ho zapneš, dej ho **za** True Tiles (doplněk za základ;
   preventivně, ne prokázaná nutnost).
3. PeterHanovy mody mají PLib vmergovaný a sdílené části si verzují samy – pořadí je jedno.

## Vypnuté mody – co s nimi

| Mod | Problém | Doporučení |
|-----|---------|------------|
| Stairs (2012810337) | poškozené stažení – v kořeni chybí DLL i yaml, v logu `IOException` | ve Steamu odhlásit + znovu přihlásit odběr |
| Half Door (1766168866) | starý API (Harmony 1), `lastWorkingBuild 447598` | mrtvý mod → odhlásit |
| Traits Reworked | existuje jen `archived_versions/vanilla` → v SO se nenačte | smazat |
| One Hundred K Challenge, Turn Back The Clock | jen `VANILLA_ID` | pro SO nepoužitelné → smazat, nebo nechat |
| ostatní PeterHan Local mody (24) | vypnuté, bez problému | nechat / smazat dle chuti |
| True Tiles Material Pack, Tiny Door Updated | vypnuté, bez problému | — |

## Doporučené kroky

1. **Ověřit za běhu:** spustit hru do hlavního menu (+ načíst save), ukončit, projít `Player.log`
   (`%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log`).
2. **Aktualizovat PeterHanovy Local mody** z `LocalMods.zip` (release *ModsLatest*, 2026-10-26,
   github.com/peterhaneve/ONIMods/releases). Tvoje kopie jsou z 2022–2025, hra mezitím přešla na Unity 6.
3. Opravit Stairs, odhlásit Half Door, uklidit vanilla-only mody.
4. OneDrive: složku `Klei` nastavit na „Vždy ponechat na tomto zařízení“.

## Poznatky pro vlastní mod (fáze 2)

- Hra je teď **Unity 6**, PLib od 4.24 vychází pro **net48** → cílit tvůj mod na `net48` místo `net472`.
- Harmony brát ze hry (`Managed\0Harmony.dll`, 2.4.2), ne z NuGetu.
- Nenosit sdílené knihovny jako samostatné DLL (problém PLib výše) – buď vmergovat, nebo nepoužívat.
