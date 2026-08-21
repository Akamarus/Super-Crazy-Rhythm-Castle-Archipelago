# Super Crazy Rhythm Castle Archipelago

Unofficial Archipelago integration for **Super Crazy Rhythm Castle**.

> [!WARNING]
> This is an experimental development build, not a release. Back up your save and expect incomplete logic.

## TL;DR

- Playable now: Roots-first APWorld v0.15 with Area Access routing, randomized Game Garage cartridges, Weed Killer, Plant Pipes, campaign checks for Levels 1–3, Music Lab cassette medal checks, and nine Music Lab chest checks.
- Not finished: full-game logic, generated AP Stars, randomized cassettes/quest items, victory, balanced item pool, verified local co-op, and release packaging.
- Testers currently build both components from source and must generate a fresh seed with the matching APWorld.
- Start with the installation guide, then use the testing/reporting checklist when something breaks.

[Installation guide](docs/INSTALL.md) · [Roadmap](docs/ROADMAP.md) · [Testing and issue reports](docs/TESTING_AND_ISSUES.md) · [Project overview](docs/PROJECT_OVERVIEW.md) · [Approved randomizer design](docs/superpowers/specs/2026-08-20-randomizer-logic-design.md)

## Simplified roadmap

1. Current playable prototype — Roots routing, Weed Killer, Plant Pipes, Garage/cassette/chest checks.
2. Native discovery — remaining areas, cassette sources, quest items, characters, multiplayer/versus behavior.
3. Full randomizer logic — Stars, Music Lab Points, level requirements, item pool, Level 22 victory.
4. Player features — verified local co-op, integrated AP log, DeathLink, then low-priority online co-op.

See the [detailed roadmap](docs/ROADMAP.md) for status tables and acceptance gates.

This repository contains both halves of the implementation:

- `client/` — BepInEx/IL2CPP C# client plugin used inside the game.
- `apworld/` — Archipelago world definition and generation logic.
- `docs/` — progression design, permanent network-ID registry, testing notes, and repository workflow.
- `tools/` — local validation and APWorld packaging helpers.

## Current baseline

| Component | Version | Status |
| --- | --- | --- |
| Client | `0.67.59` | Current tested baseline |
| APWorld | `0.15` | Current tested baseline; Roots forced starter |
| Archipelago implementation tag | `area-routing-plant-pipes-0.15` | Current slot-data implementation |

Current Roots progression includes randomized **Weed Killer** and **Plant Pipes**, plus the Frog/Hippo in-level AP check. The first Roots arrival cutscene is bypassed for AP routing, and the campaign-order Star Eater blockade / FirstAreaGate are normalized while Roots Access is owned.

## Development disclaimer

The implementation is being developed with AI-generated code under human direction. Design decisions, game logic, test plans, gameplay verification, and acceptance/rejection of changes are driven by the project owner; code and documentation are largely generated and iterated with AI assistance.

## Build the client

The client references BepInEx and IL2CPP interop assemblies from the local game installation. **Do not commit those DLLs to this repository.**

From PowerShell:

```powershell
cd .\client
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus"
```

The script builds the plugin and installs it into:

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

Roots is intentionally forced as the starter while its progression chain is being implemented and audited. The next work is to reconcile the historically observed **Level 4 Hip Glasses → Bucket Minion trade → Chicken Bucket** chain with the current Area Access design; it is not implemented randomizer logic. Randomized Star requirements come after the meaningful item/story prerequisites are mapped correctly.

See [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) for the full living project guide and [`docs/PROGRESSION.md`](docs/PROGRESSION.md) for the concise progression logic model.
