# Super Crazy Rhythm Castle Archipelago

Unofficial Archipelago integration for **Super Crazy Rhythm Castle**.

> [!WARNING]
> This is an experimental development prerelease, not a stable release. Back up your save and expect incomplete logic.

## TL;DR

- Latest development prerelease: APWorld v0.23.0 and Client v0.69.0 add AP Music Lab Points while retaining all 30 cassette items and sources. Use the two matching components together with a fresh v0.23 seed and fresh in-game save.
- Active now: configurable Star goal, AP performance difficulty, and conservative starting-area options; deterministic Level 1–22 Star-requirement previews; a registered 66-Star inventory plan; difficulty-filtered existing campaign performance locations; and completed four-seed real-generator acceptance for the filtering matrix.
- Not active yet: live AP Stars, client Star gates, final Level 22 victory, full-game logic, balanced item pool, verified local co-op, and stable-release readiness.
- Testers can download both matching components from the latest GitHub prerelease or build them from source, then generate a fresh seed with the matching APWorld.
- Start with the installation guide, then use the testing/reporting checklist when something breaks.

[Latest development release](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/tag/v0.23.0-dev) · [Installation guide](docs/INSTALL.md) · [Roadmap](docs/ROADMAP.md) · [Testing and issue reports](docs/TESTING_AND_ISSUES.md) · [Project overview](docs/PROJECT_OVERVIEW.md) · [Approved randomizer design](docs/superpowers/specs/2026-08-20-randomizer-logic-design.md)

## Latest development release — v0.23.0-dev

- Added **AP Music Lab Points** as 20 progression items worth 180 total points: ten 1-point items, three 10-point bundles, and seven 20-point large bundles.
- The nine existing Music Lab chests now use AP totals at 5/10/20/32/46/64/89/111/140 points. The final chest leaves 40 points of routing slack, and solver-reachable chests may contain progression.
- Retained all 30 randomized cassette items and sources from v0.22, including normal player insertion at the matching Music Lab machines.
- Kept native medals separate from AP points. Compatible v0.23 sessions rebuild the authoritative total on reconnect or relaunch without writing native score/save state.
- Fixed a pre-login connection-refused callback stall so normal reconnect retries remain responsive when the server is temporarily unavailable.
- Core point totals, the cap, all nine one-time chest checks, native-medal isolation, disconnect retention, reconnect, and relaunch have live-test evidence. Queued-check recovery and the v0.22/non-AP/malformed-v0.23 compatibility matrix remain pending, so this stays an experimental prerelease.

See the full [changelog](CHANGELOG.md) and [Music Lab Points acceptance record](docs/testing/2026-09-09-music-lab-points-acceptance.md) for detailed status and remaining test cases.

## Simplified roadmap

1. Current playable prototype — Roots routing through Hip Glasses, the Bucket Minion trade, Chicken Bucket, and native Combo Bucket conversion, plus Garage/cassette/chest checks.
2. Generation foundation — options, deterministic Star previews, active difficulty filtering, and the four-seed filtering generator matrix are complete; live Star placement and client Star gates still wait for broader location capacity and solver logic.
3. Native discovery — remaining areas, cassette sources, quest items, characters, multiplayer/versus behavior.
4. Full randomizer logic — accept the Music Lab Points candidate, then continue Stars, level requirements, item pool, and Level 22 victory.
5. Player features — verified local co-op, integrated AP log, DeathLink, then low-priority online co-op.

See the [detailed roadmap](docs/ROADMAP.md) for status tables and acceptance gates.

This repository contains both halves of the implementation:

- `client/` — BepInEx/IL2CPP C# client plugin used inside the game.
- `apworld/` — Archipelago world definition and generation logic.
- `docs/` — progression design, permanent network-ID registry, testing notes, and repository workflow.
- `tools/` — local validation and APWorld packaging helpers.

## Current baseline

| Component | Version | Status |
| --- | --- | --- |
| Client | `0.69.0` | Room-scoped AP Music Lab Points candidate; manual acceptance pending |
| APWorld | `0.23.0` | Strict schema 14 point contract; retains all 30 cassette sources |
| Archipelago implementation tag | `area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23` | Preserves every historical marker and appends the point contract |

Music Lab Points use 10 one-point items, 3 ten-point bundles, and 7 twenty-point large bundles: 20 progression items worth 180 points replace 20 Stardust. The nine existing chests require 5/10/20/32/46/64/89/111/140 points; the final chest leaves 40 points of slack. Solver-reachable chests may hold progression. No point milestones or duplicate cassette/cartridge checks are added, and active location totals remain 92/129/166/202.

Only an exact v0.23 point contract enables AP totals. In the Music Lab hub, the effective total is zero before history synchronization, then the weighted AP total; a temporary disconnect retains the last synchronized total. Native medals never contribute. Recognized v0.22 seeds and non-AP play retain native scoring; a malformed v0.23 contract reports incompatibility and stays at zero. The client uses the existing managed score getter only in `GameRoom_Hub6`, writes no native score/save state, and installs no chest/native detour. The diagnostic-first investigation and pending gameplay matrix are recorded in the [acceptance record](docs/testing/2026-09-09-music-lab-points-acceptance.md).

Current Roots progression includes randomized **Weed Killer**, **Plant Pipes**, **Hip Glasses**, and **Chicken Bucket**. Level 4 and the normal Bucket Minion trade send AP checks; AP-delivered inventory is consumed only by the normal trade and Lift Quest interactions. Combo Bucket remains a native, non-network ability.

Plant Pipes reconciliation passed a fresh v0.18 live route: Frog/Hippo sent its AP check, one AP receipt granted the native ability, and the ability survived Hub2 → Level 4 → Hub2, a full close/relaunch, and save load. A completed Level 4 result still requires replay before the broader durability gate closes. See [Next release bug-fix gate](docs/NEXT_RELEASE_BUG_FIXES.md).

## Development disclaimer

The implementation is being developed with AI-generated code under human direction. Design decisions, game logic, test plans, gameplay verification, and acceptance/rejection of changes are driven by the project owner; code and documentation are largely generated and iterated with AI assistance.

## Build the client

The client references BepInEx and IL2CPP interop assemblies from the local game installation. **Do not commit those DLLs to this repository.**

From PowerShell:

```powershell
cd .\client
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus" -SkipInstall
```

This builds the candidate without installing it. After review and explicit approval of the built artifact, omitting `-SkipInstall` installs it into:

```text
<GameDir>\BepInEx\plugins\RhythmCastleAP
```

## Build the APWorld

From the repository root:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\tools\build-apworld.ps1
```

The generated file is written to:

```text
dist\scrc.apworld
```

Install that file in Archipelago's `custom_worlds` directory and generate a fresh seed when APWorld data changes.

## Validate before committing

```powershell
python .\tools\validate-repo.py
```

This validates the APWorld Python syntax, metadata, committed ID frontier, key progression mappings, and repository layout without requiring an Archipelago installation.

## Repository rules

1. Never reuse an existing or historical Archipelago item/location ID. See [`docs/IDS.md`](docs/IDS.md).
2. Do not commit game DLLs, BepInEx binaries, generated plugin binaries, runtime logs, or generated `.apworld` files.
3. Preserve meaningful vanilla progression instead of replacing it with generic unlock flags when possible.
4. Keep AP reachability logic consistent with what the client can actually do in-game.
5. Update `CHANGELOG.md`, `docs/IDS.md`, and `docs/PROGRESSION.md` when a milestone changes them.

## Current development direction

The default random starter conservatively samples only validated starts, currently Roots. AP performance difficulty controls which existing campaign performance locations are addressed: Normal has 92, Hard 129, Expert 166, and Perfection 202. Inactive checks are absent from the seed, not filler. The v0.23 test candidate retains all 30 Music Lab cassettes: completing a mapped source sends one AP check, receiving its cassette persists `HAVE_IN_BAG`, and the player inserts it normally. Five point-chest cassettes reuse their existing chest locations. Broader manual source-route verification remains pending. Vampire Killer remains a physical vanilla pickup for Game Garage; its separate native-inventory blocker is still open. Older cassette-schema mismatches preserve native cassette behavior; a historical v0.21 seed with historical Client v0.67.95 retains the Money-only pilot. APWorld v0.23.0 requires a fresh seed/save for acceptance.

See [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) for the full living project guide and [`docs/PROGRESSION.md`](docs/PROGRESSION.md) for the concise progression logic model.
