# RDR2 Mod Menu

> **Work in progress.** Full write-up coming, check back later.

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

| Section | Entry | Type |
|---|---|---|
| Player | Super Speed | mod |
| Player | God Mode | mod |
| Player | Body Type `< Skinny / Medium / Fat >` | setting |
| Player | Set Honor | setting |
| Player | Refill Dead Eye / Health / Stamina `< Once / Always >` | setting |
| Player | Give $1000 | button |
| Weapons | Dynamite Gun | mod |
| Weapons | Never Reload | mod |
| Weapons | Clean Weapons | button |
| Weapons | Refill Ammo | button |
| Horse | Flying Horse (`F7` take off / land, `Space` climb, `Q` descend) | mod |
| Horse | Horse God Mode | mod |
| Horse | Refill Horse Health / Stamina `< Once / Always >` | setting |
| Crime | Never Wanted | mod |
| Crime | Clear Bounty | button |
| Crime | Clear Wanted Level | button |
| Speech | Skip Banned Lines (game subtitles must be on) | mod |
| Speech | Ban a Line | setting |
| Speech | Banned Lines | setting |
| Debug | Honor Watch | mod |

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

See [LESSONS-LEARNED.md](LESSONS-LEARNED.md) for research notes on natives, workarounds and dead ends.
