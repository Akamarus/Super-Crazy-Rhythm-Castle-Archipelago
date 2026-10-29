# Super Crazy Rhythm Castle — Archipelago Integration

A personal game-modding and systems-design project by **Akamarus**, bringing *Super Crazy Rhythm Castle* into Archipelago's multiworld randomizer ecosystem.

The integration turns a largely linear rhythm-adventure campaign into an item-driven progression system. Rewards can come from another player's game, while completing objectives in Rhythm Castle can send items back to them.

[Latest release](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/latest) · [Setup guide](docs/INSTALL.md) · [Changelog](CHANGELOG.md)

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

## Running it

Requires a legally installed Windows copy of *Super Crazy Rhythm Castle*, the supported BepInEx setup, and Archipelago. Follow the [installation guide](docs/INSTALL.md) for client setup and connection details, or the [Universal Tracker guide](docs/UNIVERSAL_TRACKER.md) for tracking support.

Use a fresh game save for a new seed. See the [documentation index](docs/INDEX.md) for configuration, compatibility, and development references.

## License

Original project code is [MIT licensed](LICENSE). Dependencies retain their own licenses; see [third-party notices](THIRD_PARTY_NOTICES.md). This is an unofficial integration; game assets and trademarks are not included.
