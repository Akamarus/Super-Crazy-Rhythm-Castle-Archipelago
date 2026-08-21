# Super Crazy Rhythm Castle Archipelago

Unofficial Archipelago integration for **Super Crazy Rhythm Castle**.

> [!WARNING]
> This is an experimental development build, not a release. Back up your save and expect incomplete logic.

## TL;DR

- Playable now: APWorld v0.16 retains the Roots-first Area Access milestone, randomized Game Garage cartridges, Weed Killer, Plant Pipes, Levels 1–3 checks, Music Lab cassette medal checks, and nine Music Lab chest checks.
- Foundation now: configurable Star goal, difficulty, and conservative starting-area options; deterministic Level 1–22 requirement previews; and a registered 66-Star inventory plan.
- Not active yet: live AP Stars, difficulty-filtered locations, client Star gates, final Level 22 victory, full-game logic, balanced item pool, verified local co-op, and release packaging.
- Testers currently build both components from source and must generate a fresh seed with the matching APWorld.
- Start with the installation guide, then use the testing/reporting checklist when something breaks.

[Installation guide](docs/INSTALL.md) · [Roadmap](docs/ROADMAP.md) · [Testing and issue reports](docs/TESTING_AND_ISSUES.md) · [Project overview](docs/PROJECT_OVERVIEW.md) · [Approved randomizer design](docs/superpowers/specs/2026-08-20-randomizer-logic-design.md)

## Simplified roadmap

1. Current playable prototype — Roots routing, Weed Killer, Plant Pipes, Garage/cassette/chest checks.
2. Generation foundation — options and deterministic previews are implemented; live Star placement and filtering wait for location capacity and solver logic.
3. Native discovery — remaining areas, cassette sources, quest items, characters, multiplayer/versus behavior.
4. Full randomizer logic — Stars, Music Lab Points, level requirements, item pool, Level 22 victory.
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
| Client | `0.67.59` | Current tested baseline |
| APWorld | `0.16` | Generation previews; random currently resolves only to validated Roots |
| Archipelago implementation tag | `area-routing-plant-pipes-0.15-generation-foundation-0.16` | Preserves current client feature gates while appending the v0.16 preview marker |

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

## Evaluate the local AI profiles

Contributors with both allowlisted Open WebUI profiles can run the manual comparison from the repository root:

```powershell
Import-Module .\tools\local-ai\LocalAiBridge.psd1 -Force
Invoke-LocalAiModelEvaluation `
    -RepositoryRoot $PWD `
    -ModelId @('jacks-assistant','jacks-assistant-fast') `
    -OpenWebUiTimeoutSec 600
```

The default remains `jacks-assistant`; the ignored `tools/local-ai/config.local.psd1` may manually select `jacks-assistant-fast`. Reports are written beneath ignored `.local-ai/evaluations/<evaluation-id>/` directories and never change configuration automatically. Scoring is deterministic but intentionally narrow, so its recommendation is advisory: human review remains mandatory, and local model evaluation does not validate APWorld generation, the client runtime, or gameplay. See [`tools/local-ai/README.md`](tools/local-ai/README.md) for setup, report, and selection details.

## Repository rules

1. Never reuse an existing or historical Archipelago item/location ID. See [`docs/IDS.md`](docs/IDS.md).
2. Do not commit game DLLs, BepInEx binaries, generated plugin binaries, runtime logs, or generated `.apworld` files.
3. Preserve meaningful vanilla progression instead of replacing it with generic unlock flags when possible.
4. Keep AP reachability logic consistent with what the client can actually do in-game.
5. Update `CHANGELOG.md`, `docs/IDS.md`, and `docs/PROGRESSION.md` when a milestone changes them.

## Current development direction

The default random starter conservatively samples only validated starts, currently Roots. v0.16 generates provisional Star requirements but does not enforce them. The next discovery work is to reconcile the historically observed **Level 4 Hip Glasses → Bucket Minion trade → Chicken Bucket** chain with the current Area Access design. Live Stars remain deferred until enough validated locations exist for the 66 planned items plus existing required progression and the solver can prove the pool beatable.

See [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) for the full living project guide and [`docs/PROGRESSION.md`](docs/PROGRESSION.md) for the concise progression logic model.
