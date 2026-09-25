# AI Playground

RDR2 mods for experimenting, built on ScriptHookRDR2 .NET V2. Each mod is its own project and DLL.

## In-game

- `F9` opens the **mod menu**. It doesn't turn anything on by itself: inside the menu, `Up`/`Down` selects a mod, `Enter` turns it on or off, and `F9` or `Backspace` closes the menu.
- Every mod starts **off** until you turn it on in the menu. Turning one on shows `<Mod Name>: ON` with a one-line how-to.
- A few seconds after scripts load, a subtitle says how many mods loaded and how many are on. That's the smoke test that everything loaded.
- `Insert` reloads scripts after a rebuild. Mods come back on or off the way you left them.
- `F8` opens the .NET console.
- Debug output from every mod goes to `AIPlayground.log` next to `RDR2.exe`.

## Mods

| Mod | How to use it once it's on |
|---|---|
| **DynamiteGun** | Every bullet explodes like dynamite where it lands (skips hits within 5 m of you). |
| **FlyingHorse** | On a horse, `F7` takes off or lands. Flies where the camera looks: `W`/`S` forward/back, `Shift` faster, `Space` climb, no input hovers. Stays above the ground but passes through buildings and trees. Rider and horse are invincible until touching down. |

## Keys

Rockstar doesn't publish an official list; the in-game Settings → Controls → Key Bindings menu is the source of truth. Default PC bindings on function keys, from community lists and the Script Hook readme:

| Key | Used by |
|---|---|
| `F1` | Game: feed message |
| `F4` | Game: satchel / journal / weapon and item wheels |
| `F5` | Native Trainer (ships with ScriptHookRDR2) |
| `F6` | Game: Photo Mode |
| `F7` | FlyingHorse: take off / land |
| `F8` | .NET console |
| `F9` | Mod menu |
| `F12` | Steam screenshot |
| `Insert` | Reload scripts |

Free for new mods: `F2`, `F3`, `F10`, `F11`.

## Layout

- `Mods/<Name>/` — one folder per mod. Builds to `<Name>.dll` and is copied into the game's `scripts\` folder.
- `Core/ModMenu/` — the F9 mod menu. Always loaded.
- `Common/` — shared code compiled into every DLL, so each one stands alone: `ModScript` (the mod base class), `ModRegistry` (the list of loaded mods the menu reads), `ModSettings` (saved on/off state), `Log`, `Natives`.
- `Directory.Build.props` — shared settings: game path (`RDR2Dir`), .NET Framework 4.8, x64, API reference.
- `Directory.Build.targets` — copies each built DLL into the game folder.

## Adding a mod

1. Create `Mods/<Name>/<Name>.csproj` containing just `<Project Sdk="Microsoft.NET.Sdk" />`.
2. Add a class deriving from `ModScript` in that folder.
3. Add the project to `AIPlayground.slnx` under the `/Mods/` folder.
4. If it has its own hotkeys, pick free ones from the Keys table above and add them to it.

## The ModScript standard

Every mod derives from `ModScript`, which registers it with the mod menu, turns it on and off from there (with an `ON`/`OFF` subtitle and a log entry), and saves its state. A mod fills in:

- `Description` — one line shown in the menu and in the `ON` subtitle (required).
- `OnEnabledTick()` — per-frame work while on (required).
- `OnEnable()` — setup when turned on; return `false` to refuse.
- `OnDisable()` — cleanup when turned off.
- `OnDisabledTick()` — anything that must keep running while off (e.g. finishing a landing).
- `OnAborted()` — undo lasting effects when scripts reload; defaults to `OnDisable()` if on.

A mod's own hotkeys should only do something while `IsEnabled`. A mod can call `Disable("reason")` to switch itself off.

On/off states are saved to `AIPlayground.ini` next to `RDR2.exe` (`ModName=on|off`, one per line). You can edit it by hand; delete a line to reset that mod to off.

## Loading and unloading mod DLLs (developer tool)

Normally every mod stays loaded and the menu turns them on and off. To stop the game from loading a mod's DLL at all, run `mods` from the repo root, then press `Insert` in-game:

```
mods                      list mods and whether each DLL is loaded
mods only FlyingHorse     load one mod, unload the rest
mods enable DynamiteGun
mods disable DynamiteGun
```

It moves each mod's DLL between the game's `scripts\` and `scripts_disabled\` folders. Builds copy a mod into whichever folder it's currently in. `Core` DLLs like the mod menu are never moved.

## Notes

- V2's `Ped.GetLastWeaponImpactCoords` returns scrambled coordinates (the game pads each float to 8 bytes). Use `Natives.TryGetLastWeaponImpact` instead, and expect the same bug in other V2 wrappers that return a `Vector3` through a pointer.
- The game's "is in the air" check (`IsInAir`) is unreliable for horses; it reported false for most of a flight.
