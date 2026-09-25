# Developer tool: control whether the game loads a mod's DLL at all, by moving it between the game's
# scripts and scripts_disabled folders. Turning loaded mods on and off is done in-game with the F6 menu.
#
#   .\mods.ps1                      list every mod and whether its DLL is loaded
#   .\mods.ps1 enable  <Mod>        load it
#   .\mods.ps1 disable <Mod>        unload it
#   .\mods.ps1 only    <Mod>        load this mod, unload all the others
#
# Only DLLs named after folders in Mods\ are touched. Press Insert in-game afterwards to reload scripts.
param(
    [ValidateSet('list', 'enable', 'disable', 'only')]
    [string]$Action = 'list',
    [string]$Mod
)

$ErrorActionPreference = 'Stop'

# Ask the build where RDR2 is, so this finds the game exactly the way builds do (see Directory.Build.props).
$anyMod = Get-ChildItem (Join-Path $PSScriptRoot 'Mods') -Filter *.csproj -Recurse | Select-Object -First 1
$rdr2Dir = (dotnet msbuild $anyMod.FullName -getProperty:RDR2Dir -nologo).Trim()
if (-not $rdr2Dir) { throw "Couldn't find Red Dead Redemption 2. See Directory.Build.props for how to set its folder." }
$enabledDir = Join-Path $rdr2Dir 'scripts'
$disabledDir = Join-Path $rdr2Dir 'scripts_disabled'
$mods = @(Get-ChildItem (Join-Path $PSScriptRoot 'Mods') -Directory | ForEach-Object Name)

function Get-ModState($name) {
    if (Test-Path (Join-Path $enabledDir "$name.dll")) { 'loaded' }
    elseif (Test-Path (Join-Path $disabledDir "$name.dll")) { 'unloaded' }
    else { 'not built' }
}

function Move-Mod($name, $toDir) {
    New-Item -ItemType Directory -Force $toDir | Out-Null
    foreach ($dir in $enabledDir, $disabledDir) {
        $dll = Join-Path $dir "$name.dll"
        if ($dir -ne $toDir -and (Test-Path $dll)) {
            Move-Item $dll $toDir -Force
        }
    }
}

if ($Action -ne 'list') {
    if (-not $Mod) { throw "Name a mod. Mods: $($mods -join ', ')" }
    if ($mods -notcontains $Mod) { throw "Unknown mod '$Mod'. Mods: $($mods -join ', ')" }
}

switch ($Action) {
    'enable'  { Move-Mod $Mod $enabledDir }
    'disable' { Move-Mod $Mod $disabledDir }
    'only'    { foreach ($m in $mods) { Move-Mod $m $(if ($m -eq $Mod) { $enabledDir } else { $disabledDir }) } }
}

foreach ($m in $mods) { '{0,-16} {1}' -f $m, (Get-ModState $m) }
if ($Action -ne 'list') { 'Press Insert in-game to reload scripts.' }
