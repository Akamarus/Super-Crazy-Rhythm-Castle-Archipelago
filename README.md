# Super Crazy Rhythm Castle — Archipelago Integration

This is my Archipelago integration for *Super Crazy Rhythm Castle*. I maintain it for my own playthroughs and as part of my portfolio.

It lets you play Rhythm Castle in an Archipelago multiworld, where the items you need can come from someone else's game and the objectives you complete can send items to other players.

[Latest release](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/latest) · [Setup guide](docs/INSTALL.md) · [Changelog](CHANGELOG.md)

## Features

- Randomized area access, progression items, character unlocks, cassettes, and Game Garage cartridges.
- Checks for level results, pickups, quests, and optional activities.
- Configurable difficulty and Star requirements, with regular or True Ending victory goals.
- Cassettes available individually or in random pairs and trios.
- In-game item notifications and recent-item history.
- Universal Tracker support and compatibility with supported older seeds.

## How it works

The project has two main parts: a C# client that runs inside the game through BepInEx, and a Python APWorld that handles seed generation and progression rules.

| Component | What it handles |
| --- | --- |
| [C# client](client/) | Connects to Archipelago, applies received items, reports completed checks, and handles changes to the game's behavior. |
| [Python APWorld](apworld/) | Defines items, locations, options, access requirements, and victory conditions. |
| [Development tools](tools/) | Validate mappings and progression rules, test generation, and package releases. |

My focus has been making the randomized progression work in actual play. Changing when you receive an item or enter an area can expose assumptions in the original game's story order. That means checking more than whether a door opens—it also means making sure you can get back out, receive the right reward, and continue without restarting or getting stuck.

The [progression rules](docs/PROGRESSION.md) document how access works. The [build guide](docs/WORKFLOW.md) covers working with the source.

## Setup

You'll need a Windows copy of *Super Crazy Rhythm Castle*, the supported BepInEx setup, and Archipelago. I've kept the setup steps in the [installation guide](docs/INSTALL.md), with separate instructions for [Universal Tracker](docs/UNIVERSAL_TRACKER.md).

Use a fresh game save for each new seed. The [documentation index](docs/INDEX.md) links to the configuration details and other project notes.

## License

Original project code is available under the [MIT license](LICENSE). Dependencies keep their own licenses, listed in [third-party notices](THIRD_PARTY_NOTICES.md).

This is an unofficial integration. Game assets and trademarks are not included.
