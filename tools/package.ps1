<#
.SYNOPSIS
    Sestaví mody v Release a zabalí je do zipů pro GitHub Releases.
.DESCRIPTION
    Build nasazuje do dočasné složky (ModsDir přesměrovaný do dist\stage), ne do hry, takže
    lokální kopie ve hře zůstane nedotčená. Každý zip obsahuje složku <ModName>\ k rozbalení
    do mods\Local\. Výsledek: dist\<ModName>-<verze>.zip.
.PARAMETER Mod
    Názvy modů k zabalení (složky v mods\ s mod\mod.yaml). Bez parametru všechny.
.OUTPUTS
    Cesty k vytvořeným zipům.
.EXAMPLE
    pwsh tools\package.ps1
    pwsh tools\package.ps1 -Mod PyCPU
#>
[CmdletBinding()]
param(
    [string[]]$Mod
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $root 'dist'
$stage = Join-Path $dist 'stage'

$mods = Get-ChildItem (Join-Path $root 'mods') -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName 'mod\mod.yaml') } |
    Where-Object { -not $Mod -or $_.Name -in $Mod }
if (-not $mods) { throw "Žádný mod k zabalení (zadáno: $Mod)." }

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null

# Celé řešení najednou: PyCPU.Scripting se nasazuje do lib\ modu PyCPU (ModLibOf), samostatně by chyběl.
dotnet build (Join-Path $root 'MOD_ONI.sln') -c Release "-p:ModsDir=$stage" --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw "dotnet build selhal (kód $LASTEXITCODE)." }

foreach ($m in $mods) {
    $info = Get-Content (Join-Path $m.FullName 'mod\mod_info.yaml') -Encoding UTF8
    $version = ($info | Select-String '^version:\s*(\S+)').Matches[0].Groups[1].Value
    $modDir = Join-Path $stage "Dev\$($m.Name)"
    $zip = Join-Path $dist "$($m.Name)-$version.zip"

    Copy-Item (Join-Path $root 'LICENSE') $modDir
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $modDir -DestinationPath $zip
    $zip
}

Remove-Item $stage -Recurse -Force
