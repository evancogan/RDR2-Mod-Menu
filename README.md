# RDR2 Mod Menu

A handful of single-player mods for Red Dead Redemption 2, plus an in-game menu for switching them on and off. Everything is built on ScriptHookRDR2 .NET V2. The mods are grouped into one project per menu section, and each project builds to its own DLL.

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

Press `F9` to open the menu. At the top is **Turn All Mods Off**, which switches off every mod that's on and puts rows like Refill Health back to Once. Below it are the sections: Player, Weapons, World, Game, Horse, Crime, Speech and Debug. Each shows how many of its mods are on, and a section with nothing in it yet says so. Use `Up` and `Down` to move, `Enter` to open a section, and `F9` again to close the menu. It opens again right where you left it. You can keep moving, looking around and acting while it's open. Only what its own keys would also do in the game (like scrolling the weapon wheel) is blocked.

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
| Player | **God Mode** | mod | Arthur can't be hurt or knocked down, and his health, stamina and Dead Eye stay full. |
| Player | **Body Type** `< Skinny / Medium / Fat >` | setting | Switches between the game's own body-weight outfits, the same ones cutscenes use, so his clothes refit properly. |
| Player | **Set Honor** `< -35 >` | setting | Shows Arthur's honor as it is now. `Left` and `Right` roll it down or up by 5; hold to keep rolling. It stops at the range the story allows so far (-240 to 240 early on, -320 to 320 later), which the help text shows. The on-screen honor meter catches up at the game's next honor change. |
| Player | **Refill Dead Eye / Health / Stamina** `< Once / Always >` | setting | On Once, press `Enter` to fill that bar and its core right now. On Always, it stays full. Each row remembers its setting. |
| Player | **Give $1000** | button | Adds $1000 to Arthur's cash. |
| Weapons | **Dynamite Gun** | mod | Every bullet explodes like dynamite where it lands. Hits closer than 5 m to you are skipped so you don't blow yourself up. |
| Weapons | **Never Reload** | mod | Your guns never need reloading: the loaded clip never runs down. |
| Weapons | **Clean Weapons** | button | Cleans every weapon Arthur is carrying: wear, dirt, soot and damage. Weapons stowed on the horse aren't included, and there's no way to remove rust yet. |
| Weapons | **Refill Ammo** | button | Fills ammo for every gun and bow, tops up any special ammo you're already carrying, and reloads the gun in your hand. Throwables are left alone. |
| Horse | **Flying Horse** | mod | On a horse, press `F7` to take off or land. In the air you ride like normal (`W` to walk, `Shift` to go faster, `A` and `D` to steer). `Space` climbs, `Q` descends, and if you do neither the horse holds its height. The higher you are above the ground, the faster you fly, up to five times normal at 150 m. Descending goes at up to 8 m/s, the same as climbing, and slows down as the ground gets close. You and the horse can't get hurt until you're back on the ground. |
| Horse | **Horse God Mode** | mod | The horse you're riding (or your active horse, when you're on foot) can't be hurt or knocked down, and its health and stamina stay full. |
| Horse | **Refill Horse Health / Stamina** `< Once / Always >` | setting | Like Arthur's refill rows, for the horse you're riding (or your active horse if you're on foot). On Once, press `Enter` to fill that bar and its core right now. On Always, it stays full. |
| Crime | **Never Wanted** | mod | The law never comes after you while it's on: nobody around you can witness a crime, and lawmen aren't sent. Bounties aren't touched. |
| Crime | **Clear Bounty** | button | Wipes your bounty and the law's record of your past crimes. |
| Crime | **Clear Wanted Level** | button | Ends the law's current chase. Your bounty doesn't change. |
| Speech | **Skip Banned Lines** | mod | Cuts off the lines you've banned as soon as their subtitle shows, so only those exact lines go (a split second may still be heard). Everything else still plays, like Arthur's other goodbyes, or an insult if you pick Antagonize. Game subtitles must be on. |
| Speech | **Ban a Line** `< Latest / 2nd latest / ... >` | setting | Right after hearing a line you don't want again, open this row. The description shows the latest subtitle, and `Left`/`Right` go back through the last 8. `Enter` bans the one shown, or unbans it if it's already banned. |
| Speech | **Banned Lines** `< 1 of 3 >` | setting | Goes through the no-list. `Enter` unbans the one shown. |
| Debug | **Honor Watch** | mod | Only reads. Shows Arthur's honor, and the range the story allows, at the top right, and logs every change. |

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

- `Mods/<Section>/` holds one project per menu section: `Player`, `Weapons`, `Horse`, `Crime`, `Speech` and `Debug`. Each builds to `<Section>.dll`, which gets copied into the game's `scripts\` folder. Every entry in a project sets its `Category` to that section, so the menu and the folders line up: to find a mod's code, look in the folder named after its section. World and Game have no project yet; they get one when their first mod does.
- `Core/ModMenu/` is the `F9` menu itself. It's always loaded.
- `Common/` is shared code that gets compiled into every DLL, so each one works on its own:
  - the base classes and plumbing: `ModScript` (mods), `ModAction` (buttons), `ModChoice` (settings), `ModRegistry` (the list the menu reads from), `ModSettings` (remembers what's on), `ScreenText` (menu text that doesn't flicker) and `Log`;
  - game helpers used by more than one section: `Natives` (workarounds for V2 wrappers), `NearbyPeds` (finding the people around a spot), `Honor`, `Protection` (the god mode protections) and `NeedRefill` (the refill rows).
- `Directory.Build.props` has the shared build settings: finding the game folder, .NET Framework 4.8, x64 and the Script Hook API reference.
- `Directory.Build.targets` stops the build with a clear message if RDR2 or ScriptHookRDR2 .NET is missing, and copies each built DLL into the game.

## Adding a mod

1. Pick its section and add a class to that section's folder, `Mods/<Section>/`. Derive from `ModScript` for a mod, `ModAction` for a button, or `ModChoice` for a setting, and set `Category` to the section's name.
2. If the section has no project yet (World and Game, for now), create `Mods/<Section>/<Section>.csproj` with just `<Project Sdk="Microsoft.NET.Sdk" />` in it, and add it to `RDR2-Mod-Menu.slnx` under the `/Mods/` folder.
3. If it needs its own hotkeys, pick free ones from the Keys table and add them there.

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

For a setting, derive from `ModChoice`. Fill in `Category`, `Description`, `Choices` and `Apply(index)`, and `InitialChoice` if you want it to start somewhere other than the first option. `Left` and `Right` step through the choices and apply each one straight away. A few optional extras:

- Set `RememberChoice` to true to save the choice in `RDR2ModMenu.ini` and restore it on load.
- Set `CanUse` to true and fill in `Use()` to make `Enter` do something, like the refill rows' "refill now".
- Fill in `OnChoiceTick()` for anything that has to run every frame depending on the choice, like "Always" keeping a bar full.
- Set `OffChoice` to the choice that means off, so Turn All Mods Off can pick it (the refill rows use Once).
- Set `StepSize` and `Wraps` to change how far `Left` and `Right` move and whether they go round from the last choice to the first, and override `LiveChoice` to show a value read from the game rather than the last one picked. Set Honor uses all three.

### Sections

An entry's `Category` decides which section it lands in, so mods, buttons and settings about the same thing end up together. Inside a section, mods are listed first, then settings, then buttons. The sections and their order are set by `Sections` in `ModMenu.cs`. They're always shown, even when empty, and any other category goes after them in alphabetical order.

## Loading and unloading mod DLLs

Normally every mod stays loaded and you use the menu to turn them on and off. If you want the game to skip loading a section's DLL entirely, run `mods` from the repo root and then press `Insert` in-game:

```
mods                    list the section projects and whether each DLL is loaded
mods only Horse         load one, unload the rest
mods enable Debug
mods disable Debug
```

It just moves DLLs between the game's `scripts\` and `scripts_disabled\` folders. Builds put a mod back wherever it currently is, and the menu itself (anything under `Core`) is never moved.

## Speech lines

The Speech section knows a line by its subtitle, exactly as the game shows it, so what you ban is what you saw and heard. No native tells a script what's being said, so `SubtitleReader` reads the game's list of subtitles straight from memory, without changing anything. It finds the list by searching the game's code for the instruction that loads its address, a method from [Rdr2TcpSubtitles](https://github.com/Kanawanagasaki/Rdr2TcpSubtitles), so it doesn't depend on one game version. Every text pointer is checked with Windows before it's read.

- Every subtitle is logged, e.g. `Subtitle: "Okay, I'll catch you later then."`.
- The no-list is `RDR2ModMenu.banned.txt` next to `RDR2.exe`, one subtitle per line. It can be edited by hand.

## Things we learned the hard way

- V2's `Ped.GetLastWeaponImpactCoords` gives back scrambled coordinates, because the game pads each number to 8 bytes and V2 doesn't expect that. Use `Natives.TryGetLastWeaponImpact` instead, and watch for the same problem anywhere else V2 hands back a `Vector3` through a pointer.
- The game's "is in the air" check (`IsInAir`) can't be trusted for horses. It said false for most of a flight.
- Honor has no native. The story scripts keep it in a script global, `Global_40 + 11095 + 35`, and Halen84's decompiled scripts for this game version (1.0.1491.50) change it at exactly that address, clamped to -240..240 until a story flag (bit 6 of `Global_40 + 7858`) widens it to -320..320. An early read-only check found -240 that didn't move after a crime; that was honor at its lowest, not a wrong address. Writing the value changes honor for real (the game's next honor change counts from it). The on-screen meter doesn't follow: setting the `HONOR_CURRENT` stat, writing the rank into the meter's data binding, and showing `HUD_CTX_HONOR_SHOW` with the HUD's own timer and flag still left the needle at an old level. It catches up at the game's next honor change.
- To stop the law, clearing the wanted level isn't enough: the law keeps noticing crimes and flips between chasing you and giving up. Stopping witnesses from reporting (`SUPPRESS_WITNESSES_CALLING_POLICE_THIS_FRAME`) and disabling dispatch (`_SET_LAW_DISABLED`) stops it at the source. That still left witnesses showing on the HUD. `SUPPRESS_CRIME_THIS_FRAME` didn't help (with its undocumented arguments at 0 it never suppressed anything); setting ped config flag 146, `PCF_CantWitnessCrimes`, on the people around you is what's used now.
- To find water, `TEST_VERTICAL_PROBE_AGAINST_ALL_WATER` works on rivers. `GET_WATER_HEIGHT` and `GET_WATER_HEIGHT_NO_WAVES` didn't find anything.
- Don't use V2's `World.GetAllPeds`. The Script Hook function behind it can start failing partway through a session and keep failing, and V2 then quietly returns an empty list ([Halen84/ScriptHookRDR2DotNet-V2#2](https://github.com/Halen84/ScriptHookRDR2DotNet-V2/issues/2)). Here it stopped working mid-session, so Never Wanted flagged nobody and witnesses came back. `NearbyPeds` asks the game instead (`_GET_ENTITIES_NEAR_POINT` with entity type 1, into an itemset).
- **Bleed Out (shelved).** The goal was every gunshot working like the game's artery hit: the victim stays standing, spurts blood from the wound and bleeds out. It was built and then removed, because the artery state can't be started cleanly. What was found:
  - A headshot always does exactly the victim's remaining health, even 10,075 of 10,075, so extra health can't stop it. `_SET_PED_HEADSHOT_DAMAGE_MULTIPLIER` made no difference at 0, 0.1, 0.5 or 1. Ped config flag 263, `PCF_NoCriticalHits`, does stop it: a revolver headshot then did about 21, the same as a body shot. [ChaosModRDR](https://github.com/clixff/ChaosModRDR) uses the same flag, with extra health, for tough enemies.
  - The artery hit is the engine's "fatally wounded behaviour" (ped config flag 388, `PCF_DisableFatallyWoundedBehaviour`, turns it off). The victim is marked fatally injured but stays alive, then takes `WEAPON_BLEEDING` damage every third to half a second, 1.7 to 5 per tick depending on the victim, dying 5 to 25 seconds later. About 2% of gunshots started it, all near major vessels: neck, upper chest (spine4), collarbone, and one head graze. None came from arms or legs.
  - It's a kind of critical hit: flag 263 blocks it too.
  - What didn't start it on demand: flag 150 (`PCF_ForceBleeding`); `APPLY_DAMAGE_TO_PED`, whose bone argument is ignored (the damage lands on whatever bone was last hit); and `_SET_MIN_PED_HEALTH_THRESHOLD` (`_0x7883AA809DF43D98`) at 10 to 150, which changed nothing. `IS_PED_FATALLY_INJURED` is that state, not a health threshold, whatever the native database says.
  - What did: an extra invisible bullet into the neck (`SHOOT_SINGLE_BULLET_BETWEEN_COORDS`) started the real bleed-out for 4 of 15 victims. But it killed 8 and left a visible second wound, so it's not usable.
  - `_SET_PED_ACTIVATE_WOUND_EFFECT` draws a blood fountain, but with guessed arguments it floated beside the victim. The game's own scripts never call it.
  - [Ped Damage Overhaul](https://github.com/HJHughJanus/PedDamageOverhaulRDR2) imitates bleeding by chipping health away. Its code for the real artery bleed is commented out.
  - The damage event (`EVENT_ENTITY_DAMAGED` in event group 0) gives victim, attacker, weapon and damage, one 8-byte slot each ([femga's layout](https://github.com/femga/rdr3_discoveries/tree/master/AI/EVENTS)), and `IS_WEAPON_A_GUN` tells guns from everything else.
- `IS_SCRIPTED_SPEECH_PLAYING` crashed Script Hook when given a ped. Its argument isn't documented, so leave it alone.
- Camp talk is ambient speech, picked from named speech contexts ([femga's audio_banks.lua](https://github.com/femga/rdr3_discoveries/blob/master/audio/audio_banks/audio_banks.lua) lists every voice bank's). Scripts can only ask whether someone is speaking and whether it's ambient, not what's being said, and blocking a context with `_BLOCK_SPEECH_CONTEXT` didn't stop Arthur's goodbye. An earlier Silence Goodbyes guessed the goodbye from its place in the conversation (Arthur's third line) and cut it as it started. That was the wrong idea: the goodbye is simply what the third Greet says, and a third Antagonize says an insult instead, so it silenced insults too. Most third Greets also fit the moment, because each member of the gang has their own farewells, e.g. `GREET_CHARLES_THIRD_FAREWELL_GENERAL_CONV` ("Well, we're glad to have you."). The jarring ones come from the generic `CAMP_GREET_THIRD_FAREWELL` pool, which is why the Speech section bans exact lines instead.
- Reading lines by their speech ID was tried first. [HearTell](https://www.nexusmods.com/reddeadredemption2/mods/9961), an ambient subtitle mod, patches a call at `RDR2.exe + 0x2FABC43` where the game hands a line to its speech system, which gives its voice, context and take. That call doesn't mean the line is said, though: while Arthur faces someone, the game keeps preparing both his next greeting and his next insult, about every second, and most are never spoken. Reading the subtitles avoids that, because they only show for lines actually said.
- When Arthur or a horse is on the ground, the game ignores any speed you set, because the animation is what moves them. Moving them by position with "keep tasks" works instead. The comments in `FlyingHorse.cs` and `SuperSpeed.cs` go into more detail.
