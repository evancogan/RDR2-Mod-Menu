# Turn AI Playground mods on and off by moving their DLLs between the game's scripts and scripts_disabled folders.
#
#   .\mods.ps1                      list every mod and whether it's enabled
#   .\mods.ps1 enable  <Mod>
#   .\mods.ps1 disable <Mod>
#   .\mods.ps1 only    <Mod>        enable this mod, disable all the others
#
# Only DLLs named after folders in Mods\ are touched. Press Insert in-game afterwards to reload scripts.
param(
    [ValidateSet('list', 'enable', 'disable', 'only')]
    [string]$Action = 'list',
    [string]$Mod
)

$ErrorActionPreference = 'Stop'

$props = [xml](Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props'))
$rdr2Dir = @($props.Project.PropertyGroup | ForEach-Object { $_.RDR2Dir } | Where-Object { $_ })[0]
$enabledDir = Join-Path $rdr2Dir 'scripts'
$disabledDir = Join-Path $rdr2Dir 'scripts_disabled'
$mods = @(Get-ChildItem (Join-Path $PSScriptRoot 'Mods') -Directory | ForEach-Object Name)

function Get-ModState($name) {
    if (Test-Path (Join-Path $enabledDir "$name.dll")) { 'enabled' }
    elseif (Test-Path (Join-Path $disabledDir "$name.dll")) { 'disabled' }
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
