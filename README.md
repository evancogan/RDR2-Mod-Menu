# AI Playground

RDR2 mods for experimenting, built on ScriptHookRDR2 .NET V2. Each mod is its own project and DLL.

## Layout

- `Mods/<Name>/` — one folder per mod. Builds to `<Name>.dll` and is copied into the game's `scripts\` folder.
- `Common/` — shared helpers (`Log`, `Natives`) compiled into every mod, so each DLL stands alone.
- `Directory.Build.props` — shared settings: game path (`RDR2Dir`), .NET Framework 4.8, x64, API reference.
- `Directory.Build.targets` — copies each built DLL into `<RDR2Dir>\scripts\`.

## Adding a mod

1. Create `Mods/<Name>/<Name>.csproj` containing just `<Project Sdk="Microsoft.NET.Sdk" />`.
2. Add a class deriving from `ModScript` (in `Common/`) in that folder.
3. Add the project to `AIPlayground.slnx` under the `/Mods/` folder.
4. Pick a toggle key that isn't already taken (see the Mods list below).

## The ModScript standard

Every mod derives from `ModScript`, which gives it a toggle hotkey: pressing it turns the mod on or off, shows `<Mod Name>: ON/OFF` as a subtitle, and logs the change. A mod only fills in:

- `ToggleKey` — its hotkey (required).
- `EnabledOnStart` — whether it starts on (default off).
- `OnEnabledTick()` — per-frame work while on (required).
- `OnEnable()` — setup when turned on; return `false` to refuse (e.g. "get on a horse first").
- `OnDisable()` — cleanup when turned off.
- `OnDisabledTick()` — anything that must keep running while off.
- `OnAborted()` — undo lasting effects when scripts reload; defaults to `OnDisable()` if on.

A mod can call `Disable("reason")` to switch itself off, e.g. when the player dismounts.

## Turning mods on and off

Run `mods` from the repo root, then press `Insert` in-game:

```
mods                      list mods and whether each is enabled
mods only FlyingHorse     enable one mod, disable the rest
mods enable DynamiteGun
mods disable DynamiteGun
```

It moves each mod's DLL between the game's `scripts\` and `scripts_disabled\` folders. Builds copy a mod into whichever folder it's currently in, so rebuilding never re-enables a disabled mod.

## In-game

- `Insert` reloads scripts after a rebuild.
- `F8` opens the .NET console.
- Debug output from every mod goes to `AIPlayground.log` next to `RDR2.exe`.

## Mods

| Key | Mod |
|---|---|
| `F7` | FlyingHorse |
| `F10` | DynamiteGun |

- **DynamiteGun** — on at start. Every bullet explodes like dynamite where it lands (skips hits within 5 m of the player).
- **FlyingHorse** — turn on while mounted to take off, off to land. Flies where the camera looks: `W`/`S` forward/back, `Shift` faster, `Space` climb, no input hovers. Rider and horse are invincible until touching down.

## Notes

- V2's `Ped.GetLastWeaponImpactCoords` returns scrambled coordinates (the game pads each float to 8 bytes). Use `Natives.TryGetLastWeaponImpact` instead, and expect the same bug in other V2 wrappers that return a `Vector3` through a pointer.
