# AI Playground

RDR2 mods for experimenting, built on ScriptHookRDR2 .NET V2. Each mod is its own project and DLL.

## Layout

- `Mods/<Name>/` — one folder per mod. Builds to `<Name>.dll` and is copied into the game's `scripts\` folder.
- `Common/` — shared helpers (`Log`, `Natives`) compiled into every mod, so each DLL stands alone.
- `Directory.Build.props` — shared settings: game path (`RDR2Dir`), .NET Framework 4.8, x64, API reference.
- `Directory.Build.targets` — copies each built DLL into `<RDR2Dir>\scripts\`.

## Adding a mod

1. Create `Mods/<Name>/<Name>.csproj` containing just `<Project Sdk="Microsoft.NET.Sdk" />`.
2. Add a class deriving from `RDR2.Script` in that folder.
3. Add the project to `AIPlayground.slnx` under the `/Mods/` folder.

## Turning mods on and off

Move a mod's DLL between `scripts\` and `scripts_disabled\` in the game folder, then press `Insert` in-game.

## In-game

- `Insert` reloads scripts after a rebuild.
- `F8` opens the .NET console.
- Debug output from every mod goes to `AIPlayground.log` next to `RDR2.exe`.

## Mods

- **DynamiteGun** — every bullet explodes like dynamite where it lands (skips hits within 5 m of the player). `F10` toggles it.

## Notes

- V2's `Ped.GetLastWeaponImpactCoords` returns scrambled coordinates (the game pads each float to 8 bytes). Use `Natives.TryGetLastWeaponImpact` instead, and expect the same bug in other V2 wrappers that return a `Vector3` through a pointer.
