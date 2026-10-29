# Super Crazy Rhythm Castle — Archipelago Integration

A personal game-modding and systems-design project by **Akamarus**, bringing *Super Crazy Rhythm Castle* into Archipelago's multiworld randomizer ecosystem.

The integration turns a largely linear rhythm-adventure campaign into an item-driven progression system. Rewards can come from another player's game, while completing objectives in Rhythm Castle can send items back to them.

[Latest release](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/latest) · [Setup guide](docs/INSTALL.md) · [Changelog](CHANGELOG.md)

## Project direction and my role

I lead the project's design, progression rules, feature priorities, and gameplay acceptance. My work includes mapping the game's prerequisites, deciding which rewards and abilities to randomize, and balancing access rules around practical play rather than technically possible but unreliable routes.

I test changes in-game, document unexpected behavior with logs and recordings, and accept or reject changes based on those results. That process has included full playthroughs, multiplayer async and sync sessions, and targeted tests of item delivery, movement, quest order, and softlock recovery.

## What the project does

- Randomizes area access, progression items, character unlocks, cassettes, and Game Garage cartridges.
- Converts campaign results, pickups, quests, and optional activities into Archipelago checks.
- Supports configurable difficulty, Star requirements, cassette bundles, and regular or True Ending victory goals.
- Integrates received items into native inventory and progression, with in-game notifications and recent-item history.
- Reconstructs progression rules for Universal Tracker and preserves compatibility with supported older seeds.

## Technical implementation

| Component | Purpose |
| --- | --- |
| **C# client · BepInEx / IL2CPP** | Connects the game to Archipelago, handles native interactions and rewards, and reports completed checks. |
| **Python APWorld** | Defines items, locations, options, access rules, and victory conditions for seed generation. |
| **Progression contracts** | Keep permanent network IDs and client/world mappings consistent across updates. |
| **Validation and packaging tools** | Check rule consistency, exercise generation, and assemble releases with dependency licenses. |

The central challenge is keeping three views of progression aligned: what the generator considers reachable, what the client unlocks, and what the player can actually do. Work has focused on separating randomized rewards from vanilla ownership, maintaining save continuity, and preventing story-dependent doors from trapping players who arrive out of order.

Explore the [client](client/), [APWorld](apworld/), [progression rules](docs/PROGRESSION.md), or [build workflow](docs/WORKFLOW.md).

## Results and current scope

The project is playable and maintained primarily for personal use and as a portfolio piece. Completed runs include server-confirmed victory and a successful multiplayer sync playthrough. Targeted gameplay testing has also covered inventory persistence, movement repairs, and recovery from blocked routes.

Automated generation and regression checks complement those playtests. Coverage varies by feature; the newer randomized Bunker and True Ending flow still needs a complete end-to-end acceptance run. The [testing record](docs/TESTING.md) and [known issues](docs/KNOWN_ISSUES.md) describe the evidence and remaining limitations.

## Running it

Requires a legally installed Windows copy of *Super Crazy Rhythm Castle*, the supported BepInEx setup, and Archipelago. Follow the [installation guide](docs/INSTALL.md) for client setup and connection details, or the [Universal Tracker guide](docs/UNIVERSAL_TRACKER.md) for tracking support.

Use a fresh game save for a new seed. See the [documentation index](docs/INDEX.md) for configuration, compatibility, and development references.

## License

Original project code is [MIT licensed](LICENSE). Dependencies retain their own licenses; see [third-party notices](THIRD_PARTY_NOTICES.md). This is an unofficial integration; game assets and trademarks are not included.
