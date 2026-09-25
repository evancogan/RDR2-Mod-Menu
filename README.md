# RDR2 Mod Menu

A handful of single-player mods for Red Dead Redemption 2, plus an in-game menu for switching them on and off. Everything is built on ScriptHookRDR2 .NET V2, and each mod is its own small project that builds to its own DLL.

## Getting set up

1. Install [ScriptHookRDR2](https://www.dev-c.com/rdr2/scripthookrdr2/) and ScriptHookRDR2 .NET V2 into your RDR2 folder.
2. Make sure you have Visual Studio 2022 or newer, or at least the .NET SDK.
3. Clone the repo and build `RDR2-Mod-Menu.slnx`. The build copies each mod's DLL into the game's `scripts\` folder for you.

You don't need to tell the build where RDR2 lives. It reads the install location the game leaves in the Windows registry (this works for the Rockstar launcher, Steam and Epic), and falls back to the usual install folders. If your setup is unusual, or you want to point at a different copy of the game, add a `Directory.Build.local.props` file next to `Directory.Build.props`. Git ignores it, so it stays on your machine:

```xml
<Project><PropertyGroup><RDR2Dir>E:\Games\Red Dead Redemption 2</RDR2Dir></PropertyGroup></Project>
```

Setting an `RDR2Dir` environment variable does the same thing. If the build can't find the game, or finds it but ScriptHookRDR2 .NET isn't installed there, it stops and tells you which.

## Using it in-game

Press `F9` to open the menu. You'll see a list of sections (Player, Needs, Weapons, Horse, Crime), and each one shows how many of its mods are currently on. Use `Up` and `Down` to move, `Enter` to open a section, and `F9` again to close the menu.

Inside a section you'll find three kinds of entries:

- **Mods** show ON or OFF. Press `Enter` to flip them.
- **Buttons** show USE. Press `Enter` and they do their thing once.
- **Settings** show `< value >`. Press `Left` or `Right` to change them.

`Backspace` takes you back to the section list.

A few other things worth knowing:

- Every mod starts off. Turning one on pops up a subtitle with a quick reminder of how to use it.
- A few seconds after the scripts load, a subtitle tells you how many mods and actions loaded and how many are on. If you see it, everything loaded fine.
- `Insert` reloads all scripts, which is how you pick up a fresh build without restarting the game. Mods come back exactly how you left them.
- `F8` opens the .NET console.
- Every mod writes its debug output to `RDR2ModMenu.log`, next to `RDR2.exe`.

## What's in the menu

| Section | Entry | Type | What it does |
|---|---|---|---|
| Player | **Super Speed** | mod | Arthur moves five times faster on foot. Walk, run and sprint like normal. He sticks to the ground, jumps about 10 m high without losing speed (and can't get hurt until he lands), runs across water and sinks when he stops, and swims like a jetski at around 15 m/s, or 25 m/s if you sprint. He won't ragdoll while it's on. |
| Player | **Body Type** `< Skinny / Medium / Fat >` | setting | Switches between the game's own body-weight outfits, the same ones cutscenes use, so his clothes refit properly. |
| Player | **Give $1000** | button | Adds $1000 to Arthur's cash. |
| Player | **Check Honor (test)** | button | Temporary. Shows the honor value the mod can see, so we can confirm it before building an honor changer. Changes nothing. |
| Needs | **Keep Needs Filled** | mod | Keeps health, stamina and Dead Eye topped up, both the bars and the cores. |
| Needs | **Refill All Needs** | button | Fills health, stamina and Dead Eye, bars and cores, once. |
| Needs | **Refill Dead Eye / Health / Stamina** | button | Fills just that one bar and its core. |
| Weapons | **Dynamite Gun** | mod | Every bullet explodes like dynamite where it lands. Hits closer than 5 m to you are skipped so you don't blow yourself up. |
| Weapons | **Clean Weapons** | button | Cleans every weapon Arthur is carrying: wear, dirt, soot and damage. Weapons stowed on the horse aren't included, and there's no way to remove rust yet. |
| Weapons | **Refill Ammo** | button | Fills ammo for every gun and bow, tops up any special ammo you're already carrying, and reloads the gun in your hand. Throwables are left alone. |
| Horse | **Flying Horse** | mod | On a horse, press `F7` to take off or land. In the air you ride like normal (`W` to walk, `Shift` to go faster, `A` and `D` to steer). `Space` climbs, `Q` descends, and if you do neither the horse holds its height. You and the horse can't get hurt until you're back on the ground. |
| Crime | **Never Wanted** | mod | The law never comes after you while it's on. Bounties aren't touched. |
| Crime | **Clear Bounty** | button | Wipes your bounty and the law's record of your past crimes. |
| Crime | **Clear Wanted Level** | button | Ends the law's current chase. Your bounty doesn't change. |

## Keys

Rockstar doesn't publish an official list of default keys, so the in-game Key Bindings screen (Settings > Controls) has the final word. Here's what the function keys are used for by default, pieced together from community lists and the Script Hook readme:

| Key | Used by |
|---|---|
| `F1` | Game: feed message |
| `F4` | Game: satchel, journal, weapon and item wheels |
| `F5` | Native Trainer (comes with ScriptHookRDR2) |
| `F6` | Game: Photo Mode |
| `F7` | Flying Horse: take off or land |
| `F8` | .NET console |
| `F9` | Mod menu |
| `F12` | Steam screenshot |
| `Insert` | Reload scripts |

`F2`, `F3`, `F10` and `F11` are free for new mods. While you're on a horse, the game also uses `Ctrl` (stop), `C` (turn the camera around) and `X`.

## How the project is laid out

- `Mods/<Name>/` holds one mod project each. It builds to `<Name>.dll`, which gets copied into the game's `scripts\` folder. One project can hold several entries; `Needs`, for example, has Keep Needs Filled and all the refill buttons.
- `Core/ModMenu/` is the `F9` menu itself. It's always loaded.
- `Common/` is shared code that gets compiled into every DLL, so each one works on its own. It has `ModScript` (mods), `ModAction` (buttons), `ModChoice` (settings), `ModRegistry` (the list the menu reads from), `ModSettings` (remembers what's on), `ScreenText` (menu text that doesn't flicker), `Log` and `Natives`.
- `Directory.Build.props` has the shared build settings: finding the game folder, .NET Framework 4.8, x64 and the Script Hook API reference.
- `Directory.Build.targets` stops the build with a clear message if RDR2 or ScriptHookRDR2 .NET is missing, and copies each built DLL into the game.

## Adding a mod

1. Create `Mods/<Name>/<Name>.csproj` with just `<Project Sdk="Microsoft.NET.Sdk" />` in it.
2. In that folder, add a class that derives from `ModScript` for a mod, `ModAction` for a button, or `ModChoice` for a setting.
3. Add the project to `RDR2-Mod-Menu.slnx` under the `/Mods/` folder.
4. If it needs its own hotkeys, pick free ones from the Keys table and add them there.

### Mods

A mod derives from `ModScript`. That base class puts it in the menu, handles turning it on and off (with the ON/OFF subtitle and a log line) and remembers its state. You fill in:

- `Description`: one line for the menu and the ON subtitle. Required.
- `Category`: which menu section it goes in, like `"Player"`. Required.
- `OnEnabledTick()`: what it does every frame while it's on. Required.
- `OnEnable()`: any setup when it's turned on. Return `false` to refuse.
- `OnDisable()`: cleanup when it's turned off.
- `OnDisabledTick()`: anything that has to keep running while it's off, like finishing a landing.
- `OnAborted()`: undo anything lasting when scripts reload. By default it calls `OnDisable()` if the mod was on.

A mod's own hotkeys should only do anything while `IsEnabled` is true, and a mod can switch itself off by calling `Disable("reason")`.

Which mods are on gets saved to `RDR2ModMenu.ini` next to `RDR2.exe`, one `ModName=on` or `ModName=off` line each. You can edit it by hand, and deleting a line resets that mod to off.

### Buttons and settings

For a button, derive from `ModAction`. It shows up as USE, and pressing `Enter` runs it once. Fill in `Category`, `Description` and `Run()`. `Run()` does the work and returns the subtitle to show afterwards.

For a setting, derive from `ModChoice`. Fill in `Category`, `Description`, `Choices` and `Apply(index)`, and `InitialChoice` if you want it to start somewhere other than the first option. `Left` and `Right` step through the choices and apply each one straight away.

### Sections

An entry's `Category` decides which section it lands in, so mods, buttons and settings about the same thing end up together. Inside a section, mods are listed first, then settings, then buttons. The section order is set by `SectionOrder` in `ModMenu.cs`, and any new sections go after those in alphabetical order.

## Loading and unloading mod DLLs

Normally every mod stays loaded and you use the menu to turn them on and off. If you want the game to skip loading a mod project's DLL entirely, run `mods` from the repo root and then press `Insert` in-game:

```
mods                      list mod projects and whether each DLL is loaded
mods only FlyingHorse     load one, unload the rest
mods enable DynamiteGun
mods disable DynamiteGun
```

It just moves DLLs between the game's `scripts\` and `scripts_disabled\` folders. Builds put a mod back wherever it currently is, and the menu itself (anything under `Core`) is never moved.

## Things we learned the hard way

- V2's `Ped.GetLastWeaponImpactCoords` gives back scrambled coordinates, because the game pads each number to 8 bytes and V2 doesn't expect that. Use `Natives.TryGetLastWeaponImpact` instead, and watch for the same problem anywhere else V2 hands back a `Vector3` through a pointer.
- The game's "is in the air" check (`IsInAir`) can't be trusted for horses. It said false for most of a flight.
- To find water, `TEST_VERTICAL_PROBE_AGAINST_ALL_WATER` works on rivers. `GET_WATER_HEIGHT` and `GET_WATER_HEIGHT_NO_WAVES` didn't find anything.
- When Arthur or a horse is on the ground, the game ignores any speed you set, because the animation is what moves them. Moving them by position with "keep tasks" works instead. The comments in `FlyingHorse.cs` and `SuperSpeed.cs` go into more detail.
