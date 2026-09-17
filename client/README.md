# RhythmCastleAP client

Current testing release: **v0.73.9**, paired with **APWorld v0.26.0**. New quest features require fresh-seed gameplay acceptance.

This is the BepInEx/IL2CPP client for the Super Crazy Rhythm Castle Archipelago implementation.

For the complete public source-build, installation, configuration, APWorld, update, and uninstall flow, use the [canonical installation guide](../docs/INSTALL.md).

## Requirements

- Super Crazy Rhythm Castle installed locally.
- BepInEx 6 IL2CPP installed for the game.
- .NET 6 SDK available to `dotnet`.
- Current compatible APWorld: **v0.26.0** (new seed/save for new quest features).

## Build and install

```powershell
cd .\client
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus"
```

The project resolves BepInEx and Unity IL2CPP references directly from the supplied game directory. Those proprietary/generated assemblies must not be committed to Git. The build updates only `<GameDir>\BepInEx\plugins\RhythmCastleAP` and removes stale plugin-local DLLs; it does not remove BepInEx core files.

After BepInEx has completed its first IL2CPP interop-generating game launch, confirm `<GameDir>\BepInEx\LogOutput.log` contains `[SCRC-AP] v0.67.60 loading.` Configure the current Roots-first development flow in `jack.rhythmcastle.archipelago.cfg` as described in the [canonical guide](../docs/INSTALL.md), including Area Access routing rather than the retired `RandomizeEarlyProgression` prototype.

## Current Roots behavior

- Hub6 is the AP home hub.
- Roots Access is supplied by APWorld v0.15 as the forced starter.
- `FirstAreaGate` is disabled while Roots Access is owned.
- `StarEaterBlockade/Blockade` is disabled while Roots Access is owned.
- `ROOTS_HUB_INTRO_WITNESSED` is set immediately before the first permitted Roots transition to bypass the displaced vanilla arrival cutscene.
- Gecko's vanilla `WEED_KILLER_BAG_ITEM` reward is suppressed and converted to the AP location `Roots - Gecko's Weed Killer`.
- Receiving `Weed Killer` grants the native consumable item so vanilla can consume it to reveal Level 3.
- Level 3 Frog/Hippo's vanilla `WEED_KILLER_ABILITY` reward is suppressed and converted to `Roots - Level 3 - Plant Pipes Pickup` via the retained `LEVEL_07_WK_ABILITY_EARNED` source marker.
- Receiving `Plant Pipes` grants the real native `WEED_KILLER_ABILITY`.

Use `BepInEx\LogOutput.log` as the primary runtime test artifact; do not commit logs to this repository.
