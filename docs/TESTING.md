# Testing Workflow

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
- Stable unrelated systems remain intact: Game Garage, Music Lab reward chests, cassettes, Secret Bunker.

## Logs

`LogOutput.log` is a test artifact, not source. Do not commit it. Attach/upload it when diagnosing a run.

Useful exact log phrases for the current Roots chain include:

```text
ROOTS INTRO CUTSCENE BYPASSED
ROOTS FIRST AREA GATE SUPPRESSED
ROOTS STAR EATER BLOCKADE SUPPRESSED
ROOTS WEED KILLER VANILLA GRANT SUPPRESSED
ROOTS WEED KILLER SOURCE AP CHECK
ROOTS WEED KILLER NATIVE GRANT APPLIED
ROOTS PLANT PIPES VANILLA GRANT SUPPRESSED
ROOTS PLANT PIPES SOURCE AP CHECK
ROOTS PLANT PIPES NATIVE GRANT APPLIED
```
