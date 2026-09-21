# Super Crazy Rhythm Castle Archipelago

Unofficial Archipelago integration for **Super Crazy Rhythm Castle**.

## v0.28.1 — First Verified Victory

**Client v0.75.13 / APWorld v0.28.0**, schema 19 / campaign-mapping schema 1.
Collect your configured AP Star goal (1–66, default 50), then successfully clear
**Level 22: King Ferdinand I**. An early clear must be replayed after reaching the goal.

**A fresh-save Hard / 40-star run is now verified complete without server assistance.**
The server recorded Victory after the qualifying boss clear. The project remains
experimental: movement/performance issues and broader multiplayer testing are open.

This release fixes Victory reporting and the Tower entrance softlock, improves
notifications and locked-level messages, repairs cassette reward/check handling,
and reduces unnecessary diagnostic work. The Tower entrance fix has automated
coverage; its live test was deferred.

All 66 AP Stars remain active. Addressed checks: Normal 164 / Hard 222 / Expert 280 / Perfection 316. Existing v0.28 seeds/saves are compatible; connect before loading.
For a new run, use the included APWorld and a fresh native save.

[Download v0.28.1](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/tag/v0.28.1) · [Installation](docs/INSTALL.md) · [Release notes](docs/releases/v0.28.1.md) · [Changelog](CHANGELOG.md) · [Known issues](docs/KNOWN_ISSUES.md) · [Progression](docs/PROGRESSION.md)

## Current priorities

1. Player/NPC movement and frame-pacing investigation.
2. Delayed cassette delivery and broader native source acceptance.
3. Additional seed/difficulty and multiworld/co-op testing.

The versioned sections below record earlier development milestones.

## Previous testing release — v0.24.0-dev

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
5. Player features — verified local co-op, cross-player notification testing, DeathLink, then low-priority online co-op.

See the [detailed roadmap](docs/ROADMAP.md) for status tables and acceptance gates.

This repository contains both halves of the implementation:

- `client/` — BepInEx/IL2CPP C# client plugin used inside the game.
- `apworld/` — Archipelago world definition and generation logic.
- `docs/` — progression design, permanent network-ID registry, testing notes, and repository workflow.
- `tools/` — local validation and APWorld packaging helpers.

## Published campaign baseline (historical v0.24)

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

The default random starter conservatively samples only validated starts, currently Roots. AP performance difficulty controls the normal campaign map: Normal 125 / Hard 183 / Expert 241 / Perfection 277. All 22 normal identities are mapped, but broad campaign replay is deferred and unplayed mappings remain manual-testing pending. Bee/Devil diagnostics are observation-only. APWorld v0.26.0 requires a fresh seed/save for new-feature acceptance.

See [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) for the full living project guide and [`docs/PROGRESSION.md`](docs/PROGRESSION.md) for the concise progression logic model.


## In-game item notifications (client v0.72.0 candidate)

Received items and server-confirmed sends appear in six-second upper-right popups,
with at most three visible at once. Press **F6** to open/close the recent-item
history (up to 100 entries); Escape also closes it. Gameplay continues while the
history is open. The panel lists item, sender/recipient, and source location.
Own rewards appear once as **Found**. Initial received history loads silently;
reconnects within the running client announce only newly received items, including
offline arrivals. History clears on a different seed/team/slot and is not saved to
disk; historical outgoing sends are not reconstructed after relaunch.

Set `Notifications.ShowItemPopups=false` to disable popups while retaining F6 history.
The feature is local UI only and does not grant items or send checks. The client
honors the connected seed's old/new Plant Pipes source name, so the notification
update can be tested on the retained seed without resetting saves. The renamed
Plant Pipes check appears in newly generated seeds using the updated APWorld.

Build/automated verification does not establish runtime visual acceptance. Check
readability, placement, F6 history, self rewards, remote sends/receipts, and
reconnect behavior in game before accepting this candidate.
