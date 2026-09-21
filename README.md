# Super Crazy Rhythm Castle Archipelago

Unofficial Archipelago integration for **Super Crazy Rhythm Castle**.

## Current release: v0.28.1 — First Verified Victory

**Client v0.75.13 / APWorld v0.28.1**, schema 19 / campaign-mapping schema 1.

Randomize area access, progression items, Music Lab cassettes, Game Garage cartridges,
and supported character unlocks. Campaign results, quests, pickups and Music Lab
challenges send checks. Collect your configured AP Star goal (1–66, default 50),
then successfully clear **Level 22: King Ferdinand I** to win. An early clear must
be replayed after reaching the goal.

All **66 AP Stars** are active. Addressed checks: **Normal 164 / Hard 222 / Expert 280 / Perfection 316**.
AP performance difficulty selects check requirements; it does not change the game's
native REG/PRO setting. The validated starting area is Roots.

A fresh-save **Hard / 40-star run** is now verified complete without server
assistance. The final clear used 41 AP Stars, and the server recorded Victory.
The project remains experimental; see the known bugs and validation limits below.

[Download v0.28.1](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/tag/v0.28.1) · [Installation](docs/INSTALL.md) · [Full changelog](CHANGELOG.md) · [Known issues](docs/KNOWN_ISSUES.md) · [Progression](docs/PROGRESSION.md)

## Updating or starting a run

- Close the game and replace the three plugin DLLs using **RhythmCastleAP-v0.75.13.zip**.
- Existing v0.28 seeds and saves remain compatible. Connect to the seed before loading the save.
- For a new run, install the included **scrc.apworld** and use a fresh native save.
- This maintenance release updates APWorld to v0.28.1 while retaining its IDs and schema-19 contract. Updating the client does not rewrite seed placements.
- An earlier boss clear discarded by the old Victory bug is not reconstructed automatically. A qualifying clear on the repaired client reports Victory normally.

## Universal Tracker

**YAML-less Universal Tracker support is available in APWorld v0.28.1.** Install
the updated `scrc.apworld` on your tracker computer and connect to your existing
v0.28 seed. The tracker restores the exact server-generated Star gates, goal,
difficulty and starting area. No new seed or game-client update is needed.

Tested with UT v0.3.3 and v0.2.32 on Archipelago 0.6.7 in 13 headless integration cases,
including the completed Hard40 seed. See [setup and verification scope](docs/UNIVERSAL_TRACKER.md).
Updated APWorld source is tagged `v0.28.1-hotfix.1`; the original release tag is unchanged.

## Changelog — v0.28.1 (September 21, 2026)

These are all release changes since the September 18 v0.28.0 hotfix. Detailed
implementation and acceptance evidence is linked in the [release notes](docs/releases/v0.28.1.md)
and [testing record](docs/TESTING.md).

### Tracker support

- Added exact slot-data restoration through Universal Tracker regeneration hooks; no YAML required. Existing schema-19 seeds are supported, and malformed/older contracts produce an error instead of incorrect logic.

### Victory and progression fixes

- **Victory reporting:** accept an omitted optional variant in the native completion event when the saved score already matches the admitted level and variant. Explicit conflicts remain rejected. This fixed the observed successful boss clear failing to report Victory.
- **Completion retention:** retain qualifying persisted clears while native-save readiness is temporarily unavailable. Verify the same save before committing, journal completed goals, and retain reconnect delivery. Later Stars cannot qualify an earlier under-threshold clear.
- **Tower of Fear entrance softlock:** classify the exterior entrance as Tower of Fear. Without its Access item, entering from Royal Corridor redirects to Music Lab instead of trapping the player between a closed return door and the inaccessible Tower phone. Automated checks passed; the live route test was deferred.
- **Royal Corridor traversal:** its Access item opens the route independently of the Star Eater feeding threshold. Feeding stays a separate check; Stars are not consumed.
- **AP Star synchronization:** invalid or discontinuous item histories suspend Star-dependent entry, display and Victory qualification until a full replay restores authority. Stale connections cannot invalidate a newer session.
- **Save and quest handling:** new-save binding waits for readable freshness flags; stale expanded-check snapshots no longer authorize results from another save state.

### Cassette fixes

- Intercept the native reward path for the five audited Music Lab cassette chest sources: **Quicksand, Flamenco, Ten-Four Good Buddy, Zen and Wiggle**. Suppress their vanilla cassette grants and matching popups while preserving chest collection, randomized checks and native insertion.
- Send a cassette source check even when its AP cassette was received before the source was completed. Ownership no longer suppresses the source check.
- Existing cassette ownership leaked by earlier builds is not automatically removed. Broader fresh-chest and receipt-order gameplay coverage remains pending.

### Notifications and player feedback

- Show clearer in-game entry-denial messages with the required and missing AP Stars. Refresh the notice on repeated attempts and clear stale messages after relevant state changes.
- Improve queued reward popups and long-message handling so large result batches can continue displaying.
- Make **F6 recent-item history** measure wrapped rows and scroll; oversized popups have a scrollable body.
- Keep sent-item deduplication for the entire seed/slot identity, avoiding repeated notifications when a long history is replayed.

### Performance, cleanup and diagnostics

- Reject unrelated character-quest events before reflection and ownership snapshots, reducing unnecessary allocations and work.
- Remove the obsolete recursive post-result save probe, its unused helper paths, a diagnostic-only postfix and an unused save-selection stub.
- Make detailed Music Lab member dumps and bottom-HUD discovery opt-in; production reconciliation remains independent.
- Guard nullable native request construction and correct obsolete APWorld/example descriptions that called active Stars and Victory previews.
- Add optional **F8 movement capture**: an explicitly started, bounded local recording for investigating player/NPC motion. It is diagnostic support, not a movement fix.

## Earlier release changes

### v0.28.0 September 18 hotfix — Client v0.75.4

- Fixed the unsafe native reference assignment behind Roots entry and Star Eater interaction crashes, with assignment verification and rollback.
- Reduced repeated cassette-save scans, scene searches, inventory copying and empty notification work. Live play improved, but this did not resolve every hitch.
- Required **Plant Pipes** for Level 22 in generation logic, including its dependent rewards and Victory route. Existing seeds are not rewritten.
- Disabled optional performance diagnostics by default.

### v0.28.0 — AP Stars, Victory and expanded checks

- Activated 66 AP Stars, configurable victory goals, generated campaign entry gates and five Star Eater thresholds. Stars are checked without being consumed; native result ratings remain separate.
- Added 39 quest/source checks, including Cell Tower Star Eater and King Ferdinand unlock. Added two Bee and four Devil completion checks within that expansion.
- Repaired Meoo/Maniac ownership separation, delayed Hip Glasses/Chicken Bucket delivery, and the glowing but non-interactable 20-point Music Lab chest.
- Added source journals and recovery for expanded checks; corrected Tower statue, Combo Bucket and item-prerequisite logic, and rebalanced the generated Star curve.
- Retained the separate Music Lab Points economy: 20 items worth 180 points across nine chests.

### v0.26.0-dev and earlier

- Added randomized Old Game Data, Car Battery, Plunger, Meoo and Maniac support, item popups/F6 history, corrected check names and cassette prerequisites.
- Repaired native Game Garage insertion, inventory consumption, song availability, medal previews and restart persistence.
- Added the full normal campaign map, difficulty-based check filtering, Music Lab cassette/medal checks, AP Music Lab Points and area-access routing.

See [CHANGELOG.md](CHANGELOG.md) for the complete version-by-version history,
the [v0.28.0 notes](docs/releases/v0.28.0.md) for all 39 added check names, and
[permanent IDs](docs/IDS.md) for the authoritative item/location registry.

## Known bugs

| Issue | Current status / observed workaround |
| --- | --- |
| Intermittent player movement blocking or stuttering despite smooth animation | Seen after Music Lab songs and elsewhere. Leaving and returning cleared one reproduction. Cause remains under investigation. |
| NPC/Hypno Pan movement faults | Act 3: Montage animals can move slowly or face incorrectly; Hypno Minims can drift off stage. Also reported while moving NPCs with Hypno Pan outside levels. Unresolved. |
| Gameplay and Music Lab hitches | Occasional note-affecting stalls remain, including reports around Combo Bucket conversion / Lift Quest. Fullscreen helped one song reproduction, not every case. |
| Exclusive-fullscreen startup warning loop | Repeated fullscreen failures and fallback to borderless can accompany launch lag. Exact trigger and general fix remain unconfirmed. |
| Delayed AP cassette delivery | A received Zen cassette became available only after closing and reopening the game. This case remains open. |
| Hypno Pan crafting while already owned | Frog and Hippo's crafting interaction can be unavailable when the ability is already owned. Ability use was confirmed working. |
| Cassette ownership leaked by older builds | The new source repair does not erase previously leaked ownership from an existing save. No automatic save cleanup is applied. |

See [KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md) for investigation evidence and updates.

## Verification and remaining coverage

- **Passed for v0.28.1:** all 35 client regression projects, 141 APWorld tests, repository validation, Release build and independent code review. The build could not fetch NuGet vulnerability metadata; that is not a completed dependency-security audit.
- **Gameplay confirmed:** a fresh-save Hard run with a 40 AP Star goal completed without server assistance, and the server recorded Victory.
- **Not gameplay retested:** the Tower exterior access mapping; its automated regression passed and the live check was explicitly deferred.
- Fresh cassette chest paths, receipt-order combinations, failed special-level attempts and complete save/seed isolation need broader native acceptance.
- Demonic Tower, Demonic Escape, Demonic Lockers and Demolition Certificate were not individually replayed in the broad pass. Earlier Royal/Bunker feed tests used reduced thresholds.
- Additional seeds/difficulties, true cross-player notifications, local co-op and online co-op remain incompletely verified. One complete run is not proof of every configuration.
- Separate Hypno Pan crafting, Bizzle/Clive switches, independent Super Nectar component awards, Demon Key acquisition/use and the final demon interaction remain source investigations, not promised additional checks. Shared rewards are not counted as independent locations.

## Next development priorities

1. Investigate movement faults and frame pacing.
2. Verify delayed cassette delivery and remaining native source paths.
3. Expand seed, difficulty and multiworld/co-op coverage.

## Repository and development

- `client/`: the BepInEx/IL2CPP C# game plugin.
- `apworld/`: generation logic, options and Archipelago data.
- `docs/`: progression, permanent IDs, release history and testing evidence.
- `tools/`: validation and packaging helpers.

The implementation is developed with AI-generated code under human direction.
Design decisions, game logic, gameplay verification and acceptance are driven by
the project owner. See [project overview](docs/PROJECT_OVERVIEW.md) and
[progression](docs/PROGRESSION.md) for implementation details.

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
