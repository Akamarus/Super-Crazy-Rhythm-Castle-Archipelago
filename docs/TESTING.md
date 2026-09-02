# Testing Workflow

> [!NOTE]
> For the public, end-to-end development-test smoke test and safe issue-report template, start with [Testing and issue reports](TESTING_AND_ISSUES.md). This document is the advanced developer checklist for targeted regression and diagnostics.

## Runtime environment baseline

Known working baseline:

- BepInEx `6.0.0-be.785`
- Unity `2021.3.26f1`
- .NET runtime `6.0.7`
- Game executable: `Rhythm Castle.exe`

## Client build

```powershell
cd .\client
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus"
```

Launch the game normally and verify `BepInEx\LogOutput.log` contains the expected client version.

## APWorld changes

Whenever items, locations, rules, slot data, or datapackage IDs change:

1. Build a new `.apworld` with `tools/build-apworld.ps1`.
2. Replace the installed custom world.
3. Generate a **fresh seed**.
4. Prefer a fresh in-game save when testing first-time story/cutscene behavior.

Client-only fixes that do not change the APWorld can usually reuse an existing compatible seed.

## Current Roots regression checklist

For the accepted Roots repair evidence, use [the v0.67.94 / v0.19 area-arrival repair acceptance](testing/2026-08-27-area-arrival-repair-acceptance.md). The completed [v0.67.64 consolidated preview record](testing/2026-08-23-consolidated-preview-acceptance.md) remains the broader gameplay evidence baseline; verified items must not be repeated unless overlapping changes could invalidate them.

Before accepting a Roots milestone, confirm:

- Roots is the precollected starter in the seed.
- Hub6 → Roots phone works.
- First Roots arrival does not steal movement with the displaced vanilla intro cutscene.
- `FirstAreaGate` is traversable.
- `StarEaterBlockade/Blockade` is traversable.
- Gecko's Weed Killer source sends `Roots - Gecko's Weed Killer`.
- Gecko does not directly grant the randomized Weed Killer item.
- Receiving `Weed Killer` creates the native item and vanilla consumes it for Level 3 access.
- Frog/Hippo sends `Roots - Level 3 - Frog and Hippo`.
- Frog/Hippo does not directly grant the Plant Pipes ability.
- Without Plant Pipes, menu exit from Level 3 works.
- Receiving `Plant Pipes` grants `WEED_KILLER_ABILITY`.
- With Plant Pipes, Level 3 can be completed.
- Level 4 sets `LEVEL_08_GLASSES_COLLECTED` and sends `Roots - Level 4 - Hip Glasses` exactly once without locally granting Hip Glasses.
- AP-delivered Hip Glasses enables the normal Bucket Minion interaction.
- The normal trade consumes Hip Glasses, sends `Roots - Bucket Minion Trade`, preserves dialogue/blockade/King flags, and does not locally grant Chicken Bucket.
- AP-delivered Chicken Bucket enables the normal Lift Quest interaction.
- Lift Quest consumes Chicken Bucket, grants native Combo Bucket, completes Level 09, and transitions to the Lobby normally.
- Reloading/reconnecting while held preserves each item; reloading/reconnecting after consumption does not restore it or duplicate a check.
- An old/incompatible seed and a non-AP save retain the complete vanilla chain.
- Stable unrelated systems remain intact: Game Garage, Music Lab reward chests, cassettes, Secret Bunker.

## Logs

`LogOutput.log` is a test artifact, not source. Do not commit it. For public reports, follow the redaction and excerpt guidance in [Testing and issue reports](TESTING_AND_ISSUES.md); do not attach credentials, private addresses, personal paths, game files, or proprietary assemblies.

Useful exact log phrases for the current Roots chain include:

```text
AREA ARRIVAL OVERRIDE ARMED
AREA ARRIVAL CONDITION BYPASSED
ROOTS FIRST AREA GATE SUPPRESSED
ROOTS STAR EATER BLOCKADE SUPPRESSED
ROOTS WEED KILLER VANILLA GRANT SUPPRESSED
ROOTS WEED KILLER SOURCE AP CHECK
ROOTS WEED KILLER NATIVE GRANT APPLIED
ROOTS PLANT PIPES VANILLA GRANT SUPPRESSED
ROOTS PLANT PIPES SOURCE AP CHECK
ROOTS PLANT PIPES NATIVE GRANT APPLIED
ROOTS BUCKET RANDOMIZATION ENABLED
ROOTS BUCKET VANILLA GRANT SUPPRESSED
ROOTS BUCKET SOURCE AP CHECK
ROOTS BUCKET NATIVE GRANT APPLIED
```

When testing later score or objective checks without Combo Bucket, report the exact level/song, native difficulty, AP performance tier, score, stars, medal or missed objective, player count, abilities, attempt count, best result, and whether `COMBO_BUCKET_ABILITY` was absent. Include a focused log excerpt and video when practical. A success proves feasibility under those conditions; a failed attempt alone does not prove impossibility and must not create a solver requirement by itself.
