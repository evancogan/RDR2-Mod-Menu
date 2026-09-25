# RDR2 Mod Menu

RDR2 mods for experimenting, built on ScriptHookRDR2 .NET V2, with an in-game menu to turn them on and off. Each mod is its own project and DLL.

## Setup

1. Install [ScriptHookRDR2](https://www.dev-c.com/rdr2/scripthookrdr2/) and ScriptHookRDR2 .NET V2 into the RDR2 folder.
2. Have Visual Studio 2022 or later (or the .NET SDK) installed.
3. Clone and build `RDR2-Mod-Menu.slnx`. Each mod's DLL is copied into the game's `scripts\` folder automatically.

The build finds the RDR2 folder by itself, from the registry entry the game writes (Rockstar launcher, Steam or Epic) or the standard install folders, so there's nothing to configure. If it can't, or you want a different copy of the game, create `Directory.Build.local.props` next to `Directory.Build.props` (it's git-ignored):

```xml
<Project><PropertyGroup><RDR2Dir>E:\Games\Red Dead Redemption 2</RDR2Dir></PropertyGroup></Project>
```

An `RDR2Dir` environment variable works too. If RDR2 or ScriptHookRDR2 .NET can't be found, the build stops with a message saying which.

## In-game

- `F9` opens the **mod menu**: a list of sections (Player, Needs, Weapons, Horse, Crime), each showing how many of its mods are on. `Up`/`Down` selects, `Enter` opens a section, `F9` closes.
- A section holds everything on that topic, whatever kind it is: on/off mods (`Enter` toggles, shows ON/OFF), one-press buttons (`Enter` uses, shows USE) and scrolling settings (`Left`/`Right` change, shows `< value >`). `Backspace` goes back.
- Every mod starts **off** until you turn it on in the menu. Turning one on shows `<Mod Name>: ON` with a one-line how-to.
- A few seconds after scripts load, a subtitle says how many mods and actions loaded and how many mods are on. That's the smoke test that everything loaded.
- `Insert` reloads scripts after a rebuild. Mods come back on or off the way you left them.
- `F8` opens the .NET console.
- Debug output from every mod goes to `RDR2ModMenu.log` next to `RDR2.exe`.

## Menu sections

| Section | Entry | Kind | What it does |
|---|---|---|---|
| Player | **Super Speed** | on/off | Arthur moves 5× faster on foot; walk, run and sprint as normal. Follows the ground, jumps about 10 m high keeping his speed (invincible until he lands), runs across water (sinks when he stops), and swims like a jetski (about 15 m/s, 25 m/s sprinting). He won't ragdoll while it's on. |
| Player | **Body Type** `< Skinny / Medium / Fat >` | setting | Uses the game's own body-weight outfits (the ones cutscenes switch between), so clothes refit. |
| Needs | **Keep Needs Filled** | on/off | Keeps health, stamina and Dead Eye full, bars and cores. |
| Needs | **Refill All Needs** | button | Fills health, stamina and Dead Eye, bars and cores, once. |
| Needs | **Refill Dead Eye / Health / Stamina** | button | Fills that one bar and its core. |
| Weapons | **Dynamite Gun** | on/off | Every bullet explodes like dynamite where it lands (skips hits within 5 m of you). |
| Weapons | **Refill Ammo** | button | Fills ammo for every gun and bow, tops up special ammo you already carry, and reloads your gun. Skips throwables. |
| Horse | **Flying Horse** | on/off | On a horse, `F7` takes off or lands. Ride as normal in the air (`W` walk, `Shift` faster, `A`/`D` steer); `Space` rises, `Q` descends, otherwise it holds altitude. Rider and horse are invincible until touching down. |
| Crime | **Clear Wanted Level** | button | Ends the law's current pursuit. Your bounty stays as it is. |

## Keys

Rockstar doesn't publish an official list; the in-game Settings → Controls → Key Bindings menu is the source of truth. Default PC bindings on function keys, from community lists and the Script Hook readme:

| Key | Used by |
|---|---|
| `F1` | Game: feed message |
| `F4` | Game: satchel / journal / weapon and item wheels |
| `F5` | Native Trainer (ships with ScriptHookRDR2) |
| `F6` | Game: Photo Mode |
| `F7` | Flying Horse: take off / land |
| `F8` | .NET console |
| `F9` | Mod menu |
| `F12` | Steam screenshot |
| `Insert` | Reload scripts |

Free for new mods: `F2`, `F3`, `F10`, `F11`. While riding, `Ctrl` (stop), `C` (turn the camera around) and `X` are taken by the game too.

## Layout

- `Mods/<Name>/` — one folder per mod project. Builds to `<Name>.dll` and is copied into the game's `scripts\` folder. A project can hold several mods and actions (`Needs` holds Keep Needs Filled and the refill actions).
- `Core/ModMenu/` — the F9 mod menu. Always loaded.
- `Common/` — shared code compiled into every DLL, so each one stands alone: `ModScript` (on/off mods), `ModAction` (buttons), `ModChoice` (scrolling settings), `ModRegistry` (the list the menu reads), `ModSettings` (saved on/off state), `ScreenText` (flicker-free menu text), `Log`, `Natives`.
- `Directory.Build.props` — shared settings: finding the game folder (`RDR2Dir`), .NET Framework 4.8, x64, API reference.
- `Directory.Build.targets` — stops the build with a clear message if RDR2 or ScriptHookRDR2 .NET is missing, and copies each built DLL into the game folder.
## Adding a mod

1. Create `Mods/<Name>/<Name>.csproj` containing just `<Project Sdk="Microsoft.NET.Sdk" />`.
2. Add a class deriving from `ModScript` (an on/off mod), `ModAction` (a button) or `ModChoice` (a scrolling setting) in that folder.
3. Add the project to `RDR2-Mod-Menu.slnx` under the `/Mods/` folder.
4. If it has its own hotkeys, pick free ones from the Keys table above and add them to it.

## The ModScript standard

Every mod derives from `ModScript`, which registers it with the mod menu, turns it on and off from there (with an `ON`/`OFF` subtitle and a log entry), and saves its state. A mod fills in:

- `Description` — one line shown in the menu and in the `ON` subtitle (required).
- `Category` — the menu section it's listed in, e.g. `"Player"` (required).
- `OnEnabledTick()` — per-frame work while on (required).
- `OnEnable()` — setup when turned on; return `false` to refuse.
- `OnDisable()` — cleanup when turned off.
- `OnDisabledTick()` — anything that must keep running while off (e.g. finishing a landing).
- `OnAborted()` — undo lasting effects when scripts reload; defaults to `OnDisable()` if on.

A mod's own hotkeys should only do something while `IsEnabled`. A mod can call `Disable("reason")` to switch itself off.

On/off states are saved to `RDR2ModMenu.ini` next to `RDR2.exe` (`ModName=on|off`, one per line). You can edit it by hand; delete a line to reset that mod to off.

### Buttons and settings

For a one-press button, derive from `ModAction`. It shows as `USE`; pressing `Enter` runs it once. Fill in `Category`, `Description` and `Run()`, which does the work and returns the subtitle to show.

For a scrolling setting, derive from `ModChoice`: fill in `Category`, `Description`, `Choices` and `Apply(index)`, and optionally `InitialChoice`. `Left`/`Right` move through the choices and apply each one straight away.

### Sections

Every entry's `Category` is its menu section; mods, buttons and settings on the same topic share one. Within a section, on/off mods come first, then settings, then buttons. The order of sections is set by `SectionOrder` in `ModMenu.cs`; new sections follow alphabetically.

## Loading and unloading mod DLLs (developer tool)

Normally every mod stays loaded and the menu turns them on and off. To stop the game from loading a mod project's DLL at all, run `mods` from the repo root, then press `Insert` in-game:

```
mods                      list mod projects and whether each DLL is loaded
mods only FlyingHorse     load one, unload the rest
mods enable DynamiteGun
mods disable DynamiteGun
```

It moves each DLL between the game's `scripts\` and `scripts_disabled\` folders. Builds copy a mod into whichever folder it's currently in. `Core` DLLs like the mod menu are never moved.

## Notes

- V2's `Ped.GetLastWeaponImpactCoords` returns scrambled coordinates (the game pads each float to 8 bytes). Use `Natives.TryGetLastWeaponImpact` instead, and expect the same bug in other V2 wrappers that return a `Vector3` through a pointer.
- The game's "is in the air" check (`IsInAir`) is unreliable for horses; it reported false for most of a flight.
- For water, `TEST_VERTICAL_PROBE_AGAINST_ALL_WATER` finds rivers; `GET_WATER_HEIGHT` and `GET_WATER_HEIGHT_NO_WAVES` didn't.
- On foot and on horseback, the game ignores speed set while on the ground (animation moves them); placing by position with "keep tasks" works. See the comments in `FlyingHorse.cs` and `SuperSpeed.cs`.
