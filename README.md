# RDR2 Mod Menu

![C#](https://img.shields.io/badge/C%23-.NET-512BD4?logo=dotnet&logoColor=white) ![ScriptHookRDR2](https://img.shields.io/badge/ScriptHookRDR2-.NET%20V2-8B0000)

Single-player mods for Red Dead Redemption 2 with an in-game menu to toggle them. Built on ScriptHookRDR2 .NET V2.

## Setup

1. Install [ScriptHookRDR2](https://www.dev-c.com/rdr2/scripthookrdr2/) and ScriptHookRDR2 .NET V2 into your RDR2 folder.
2. Install Visual Studio 2022+ or the .NET SDK.
3. Build `RDR2-Mod-Menu.slnx`. Each mod's DLL is copied into the game's `scripts\` folder.

The build finds the game automatically (Rockstar, Steam, Epic). To override it, set an `RDR2Dir` environment variable or add a git-ignored `Directory.Build.local.props` next to `Directory.Build.props`:

```xml
<Project><PropertyGroup><RDR2Dir>E:\Games\Red Dead Redemption 2</RDR2Dir></PropertyGroup></Project>
```

## Menu controls

| Key | Action |
|---|---|
| `F9` | Open / close the menu |
| `Up` / `Down` | Move |
| `Enter` | Open section, toggle mod, use button |
| `Left` / `Right` | Change a setting |
| `Backspace` | Back to sections |
| `Insert` | Reload scripts |
| `F8` | .NET console |

## What's in the menu

| Section | Entry | Type | What it does |
|---|---|---|---|
| Player | Super Speed | mod | Arthur moves 5x faster on foot, jumps much higher and can run on water. He sinks when he stops, and won't ragdoll while it's on. |
| Player | God Mode | mod | Arthur can't be hurt or knocked down, and his health, stamina and Dead Eye stay full. |
| Player | Body Type `< Skinny / Medium / Fat >` | setting | Uses the game's own body-weight outfits, so his clothes refit. |
| Player | Set Honor | setting | Shows Arthur's honor and rolls it up or down by 5, within the range the story allows. The on-screen honor meter only catches up at the game's next honor change. |
| Player | Refill Dead Eye / Health / Stamina `< Once / Always >` | setting | On Once, Enter refills it now. On Always, it's kept full. |
| Player | Give $1000 | button | Adds $1000 to Arthur's cash. |
| Weapons | Dynamite Gun | mod | Every bullet explodes like dynamite where it lands, except within 5 m of Arthur. |
| Weapons | Never Reload | mod | The loaded clip never runs down, so guns never need reloading. |
| Weapons | Clean Weapons | button | Resets wear, dirt, soot and damage on every weapon Arthur carries. Weapons on the horse aren't included, and rust stays. |
| Weapons | Refill Ammo | button | Fills ammo for every gun and bow, tops up special ammo he already carries, and reloads the gun in his hand. |
| Horse | Flying Horse (`F7` take off / land, `Space` climb, `Q` descend) | mod | Ride as normal in the air. The higher it flies, the faster it goes. |
| Horse | Horse God Mode | mod | The horse can't be hurt or knocked down, and its health and stamina stay full. |
| Horse | Refill Horse Health / Stamina `< Once / Always >` | setting | The same Once / Always rows as Arthur's. |
| Crime | Never Wanted | mod | The law never comes after Arthur and nobody witnesses his crimes. Bounties aren't touched. |
| Crime | Clear Bounty | button | Wipes his bounty and his record of past crimes. RDR2 keeps bounties per region, and it isn't confirmed yet that this clears every region. |
| Crime | Clear Wanted Level | button | Ends the law's current pursuit. The bounty stays. |
| Speech | Skip Banned Lines (game subtitles must be on) | mod | Cuts off a banned line as soon as its subtitle shows. |
| Speech | Ban a Line | setting | Goes back through the last few subtitles. Enter bans the chosen one, or unbans it. |
| Speech | Banned Lines | setting | Goes through the banned list. Enter unbans a line. |
| Debug | Honor Watch | mod | Read only. Shows Arthur's honor at the top right and logs every change. It's how the honor value was checked against the in-game meter before Set Honor wrote to it. |

## How it works

Each menu section is its own DLL (`Player.dll`, `Weapons.dll` and so on), and the menu is another one (`ModMenu.dll`). That's why `mods` can load and unload sections one at a time.

Every DLL compiles in its own copy of `Common/`, so a normal static list of mods wouldn't be shared between them. All scripts run in the same AppDomain, though, so `Common/ModRegistry.cs` keeps the list in AppDomain data. Each entry is a dictionary of framework types only (strings, delegates, arrays), which every DLL sees as the same types. Each mod registers itself when it loads, and the menu reads the list.

## Function keys

| Key | Used by |
|---|---|
| `F1` | Game: feed message |
| `F4` | Game: wheels |
| `F5` | Native Trainer |
| `F6` | Game: Photo Mode |
| `F7` | Flying Horse |
| `F8` | .NET console |
| `F9` | Mod menu |
| `F12` | Steam screenshot |
| `Insert` | Reload scripts |

Free: `F2`, `F3`, `F10`, `F11`.

## Files (next to `RDR2.exe`)

| File | Contents |
|---|---|
| `RDR2ModMenu.log` | Debug output |
| `RDR2ModMenu.ini` | Which mods are on |
| `RDR2ModMenu.banned.txt` | Banned speech lines, one per line |

## Loading and unloading DLLs

Run from the repo root, then press `Insert` in-game:

```
mods                    list section DLLs and whether each is loaded
mods only Horse         load one, unload the rest
mods enable Debug
mods disable Debug
```

## Notes

See [LESSONS-LEARNED.md](LESSONS-LEARNED.md) for research notes on natives, workarounds and dead ends. It also covers Bleed Out, a mod that was built and then shelved because the game's artery bleed can't be started cleanly.

## How it was built

I built this with Claude as a pair programmer. I planned the mods, wrote parts of the code, directed the rest, and tested every change in-game. The findings in LESSONS-LEARNED.md came from that testing.
