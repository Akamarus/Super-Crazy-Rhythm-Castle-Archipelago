# Super Crazy Rhythm Castle Archipelago

Unofficial Archipelago multiworld integration for **Super Crazy Rhythm Castle** on Windows.

[Download the latest published release](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/releases/latest) · [Install and connect](docs/INSTALL.md) · [Known issues](docs/KNOWN_ISSUES.md) · [Changelog](CHANGELOG.md)

**Current release: v0.29.1 — Client 0.76.1 / APWorld 0.29.0.**

This maintenance release repairs the captured player movement stalls and reorganizes the documentation. [Release notes](docs/releases/v0.29.1.md).

## What it randomizes

- Area access, supported progression and character unlock items, cassettes and Game Garage cartridges.
- Campaign results, supported quest interactions, pickups, Music Lab medals and reward chests send checks.
- Cassettes can be individual items or seed-specific random pairs/triples.
- Collect AP Stars to open generated level gates and meet your victory goal; Star Eaters check your total without spending Stars.
- In-game sent/received popups and **F6** recent-item history help you track rewards.

To win, collect your configured AP Star goal (**1–66, default 50**), then successfully clear **Level 22: King Ferdinand I**. A clear before the goal must be repeated after reaching it. A Hard / 40-Star run has completed with server-confirmed Victory without assistance.

Checks by AP difficulty: **Normal 164 / Hard 222 / Expert 280 / Perfection 316**. AP difficulty selects required results; the game's Regular/Pro chart setting is separate. Roots is the validated starting area.

## Start or update

Follow the [installation guide](docs/INSTALL.md) for the tested BepInEx build, client installation and exact connection settings. Connect to your room before loading its save. Use a fresh native save for a new seed; keep your associated save when updating a compatible client.

The 0.76.1 client changes movement timing only; existing APWorld 0.29.0 seeds, placements and Star gates stay intact. No new seed or APWorld update is required for this patch.

Archipelago 0.6.8 generation and Universal Tracker 0.2.32 were tested. For Tracker, install the matching APWorld and follow its normal Players-folder YAML setup. See [Universal Tracker](docs/UNIVERSAL_TRACKER.md).

## Status and support

The project is playable and under active development. [Known issues](docs/KNOWN_ISSUES.md) distinguishes open bugs, modeling limits and untested configurations; [testing](docs/TESTING.md) records acceptance scope.

For a bug report, include your client/APWorld versions, what happened and `BepInEx/LogOutput.log`. Movement reports can include an **F8** capture. Keep passwords and private room details out of public reports. See [reporting and testing](docs/TESTING_AND_ISSUES.md).

## Development and licensing

Original project code is [MIT licensed](LICENSE); game assets and trademarks are not included. [Third-party notices](THIRD_PARTY_NOTICES.md) accompany distributions.

This project uses substantial AI assistance under human direction. Read the [AI disclosure](docs/AI_DISCLOSURE.md) for the owner's design and gameplay-testing role, tools used and limits of human source review.

[Documentation index](docs/INDEX.md) · [Build and contribution workflow](docs/WORKFLOW.md) · [Progression rules](docs/PROGRESSION.md) · [Permanent IDs](docs/IDS.md) · [Historical archive](docs/archive/README.md)
