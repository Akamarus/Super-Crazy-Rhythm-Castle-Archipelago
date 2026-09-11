# Super Crazy Rhythm Castle Archipelago

Unofficial Archipelago integration for **Super Crazy Rhythm Castle**.

> [!WARNING]
> This is an experimental development prerelease, not a stable release. Back up your save and expect incomplete logic.

## TL;DR

- Latest testing prerelease: APWorld v0.24.0 and Client v0.70.0 map all 22 normal campaign identities. Use the matching components only with a fresh v0.24 seed and fresh in-game save.
- Active normal checks are separate Completion and cumulative 1-Star/2-Star/3-Star locations: Normal 121 / Hard 179 / Expert 237 / Perfection 273 addressed locations.
- Not active yet: all 66 AP Stars, generated Star gates, final Victory, production special-mode locations, full-game logic, balanced item pool, verified local co-op, and stable-release readiness.
- Testers can download both matching components from the latest GitHub prerelease or build them from source, then generate a fresh seed with the matching APWorld.
- Start with the installation guide, then use the testing/reporting checklist when something breaks.

[Latest testing release](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/tag/v0.24.0-dev) · [Installation guide](docs/INSTALL.md) · [Roadmap](docs/ROADMAP.md) · [Testing and issue reports](docs/TESTING_AND_ISSUES.md) · [Project overview](docs/PROJECT_OVERVIEW.md) · [Full-level-mapping acceptance record](docs/testing/2026-09-10-full-level-mapping-acceptance.md)

## Latest testing release — v0.24.0-dev

- Added the complete normal campaign map: all 22 identities, four permanent candidate location names per level, and active difficulty filtering.
- Development Caches are retired from new seeds; their historical IDs remain reserved. The next safe item/location frontiers are `187256156` / `187256292`.
- Bee/Devil variants are diagnostic-only observation paths. They cannot queue normal campaign checks, AP Stars, Star gates, Victory, or special-mode locations.
- Retained AP Music Lab Points under the new schema-15 contract and preserved legacy v0.23 Level 22 cumulative Star checks.
- Fixed campaign checks queued during temporary disconnects or failed reconnect attempts. Pending results are isolated by authenticated game, seed, team, and slot and flush exactly once after recovery.
- Automated coverage proves the mapping contract and client queue behavior; individual campaign mappings have not had a broad gameplay replay. Manual acceptance remains required, so this is a prerelease for testers rather than a stable release.

See the full [changelog](CHANGELOG.md) and [full-level-mapping acceptance record](docs/testing/2026-09-10-full-level-mapping-acceptance.md) for detailed status and remaining test cases.

## Simplified roadmap

1. Current playable prototype — Roots routing through Hip Glasses, the Bucket Minion trade, Chicken Bucket, and native Combo Bucket conversion, plus Garage/cassette/chest checks.
2. Generation foundation — options, deterministic Star previews, active difficulty filtering, and the four-seed filtering generator matrix are complete; live Star placement and client Star gates still wait for broader location capacity and solver logic.
3. Native discovery — remaining areas, cassette sources, quest items, characters, multiplayer/versus behavior.
4. Full randomizer logic — the full normal campaign map is implemented; next come broader gameplay acceptance, AP Stars, enforced level requirements, the final item pool, and Level 22 victory.
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
| Client | `0.70.0` | Full normal-campaign mapping candidate; manual acceptance pending |
| APWorld | `0.24.0` | Schema 15 / campaign-mapping schema 1; special locations inactive |
| Archipelago implementation tag | `area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23-full-level-mapping-0.24` | Preserves every historical marker and appends the campaign mapping contract |

Music Lab Points retain their prior exact contract. The v0.24 candidate adds normal campaign checks while keeping normal Completion separate from 1 Star. Addressed totals are Normal 121 / Hard 179 / Expert 237 / Perfection 273; inactive checks are absent from a seed, not filler.

Exact v0.23/schema-14 and v0.24/schema-15 point contracts enable AP totals. In the Music Lab hub, the effective total is zero before history synchronization, then the weighted AP total; a temporary disconnect retains the last synchronized total. Native medals never contribute. Recognized v0.22 seeds and non-AP play retain native scoring; malformed or unsupported AP contracts report incompatibility and stay at zero. The client uses the existing managed score getter only in `GameRoom_Hub6`, writes no native score/save state, and installs no chest/native detour. The diagnostic-first investigation and pending gameplay matrix are recorded in the [Music Lab Points acceptance record](docs/testing/2026-09-09-music-lab-points-acceptance.md) and [v0.24 mapping acceptance record](docs/testing/2026-09-10-full-level-mapping-acceptance.md).

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

The default random starter conservatively samples only validated starts, currently Roots. AP performance difficulty controls the normal campaign map: Normal 121 / Hard 179 / Expert 237 / Perfection 273. All 22 normal identities are mapped, but broad campaign replay is deferred and unplayed mappings remain manual-testing pending. Bee/Devil diagnostics are observation-only. APWorld v0.24.0 requires a fresh seed/save for acceptance.

See [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) for the full living project guide and [`docs/PROGRESSION.md`](docs/PROGRESSION.md) for the concise progression logic model.
