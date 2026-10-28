# Changelog

## v0.30.0 — Bunker and True Ending

October 28, 2026. Client 0.77.0 / APWorld 0.30.0.

## New progression

- Optional True Ending victory alongside the default King Ferdinand ending.
- Randomized Bunker Keycard, Demon Key, Virus, Cheat Codes and Game Ender, plus six new checks.
- Bunker progression defaults to enabled; requires at least two goal Stars. The Bunker Star Eater always requires fewer Stars than the Level 22 entrance and its Superstar cartridge source is randomized.
- The Demon Key controls the separate Devil quest; Gecko's Data Stick and Note Pad remain native quest setup.
- New seeds open Demolition Training and School Trip doors with Lobby Access, retaining generated Star gates.
- Cell Tower Access opens Old Tom's connecting route without nectar delivery; read the note and leave/re-enter the area to update the Star Eater route.

## Fixes and cleanup

- Correct incompatible Bunker/goal settings to raise Archipelago OptionError, rather than generation crashes.
- Replace an unsafe native event conversion implicated in final-level transition crashes.
- Remove F8 movement recording and temporary profiling/diagnostic callbacks while preserving movement repairs and ordinary error logging.
- Add schema21 Universal Tracker reconstruction and preserve legacy schema19/20 rules.

## Updating and validation

Install the client archive with its licenses and scrc.apworld into Archipelago's custom_worlds folder while applications are closed. Use a fresh seed and native save for the new Bunker options. Existing schema19/20 rooms retain their original placements and rules; do not regenerate an active room. Keep its YAML for Universal Tracker. The Ionium YAML creator may still offer the previous indexed version until this release is indexed.

The corrected APWorld passed Ionium's full browser CI preset on Archipelago 0.6.7 and 0.6.8: 458 unit tests, 29,000 attempted fuzz cases, zero failures/timeouts, including Universal Tracker. Invalid option combinations were correctly ignored. Version metadata is updated for this release; index CI remains authoritative.

The native Gecko quest and final ending were investigated in staged saves, but the complete randomized schema21 receipt/source/goal flow has not yet completed an end-to-end gameplay acceptance run. Intermittent performance reports and broader multiplayer coverage remain open. See [known issues](docs/KNOWN_ISSUES.md) and [testing](docs/TESTING.md).

This project uses substantial AI assistance under human direction; see the [AI disclosure](docs/AI_DISCLOSURE.md).

## v0.29.2 — October 22, 2026 — Client 0.76.2 / APWorld 0.29.0

- Remove the separate native attempt requirement from the exact return-exit switch, preserving its normal interaction and transition.
- Keep the exact Central Mainframe external return gate open for authenticated Cell Tower Access owners without marking the level attempted.
- Route Cell Tower exits to Music Lab while Fish Tears delivery is incomplete or unreadable, avoiding the upper-Lobby hand-blocker trap.
- No APWorld, IDs, seed placements or quest-completion changes; both reported escape routes passed targeted gameplay testing.

[Full notes and limits](docs/releases/v0.29.2.md).

## v0.29.1 â€” October 12, 2026 â€” Client 0.76.1 / APWorld 0.29.0

- Repair the captured player movement stalls without changing display mode or imposing a frame-rate cap.
- Add bounded, opt-in F8 timing evidence and repair counters.
- Simplify current documentation and archive obsolete plans and checkpoint logs.
- Record completed movement, entrance, item-delivery and randomized-source regression tests.

[Full notes and limits](docs/releases/v0.29.1.md).

## Published releases

| Release | Main changes |
| --- | --- |
| [v0.29.0](docs/releases/v0.29.0.md) | Random cassette bundles, generation capacity, Montage movement, delivery/selector/booth/chest fixes and audited access rules. |
| [v0.28.2](docs/releases/v0.28.2.md) | Generation and multiworld compatibility. |
| [v0.28.1](docs/releases/v0.28.1.md) | First verified Victory, tracker support, cassette-source and notification repairs. |
| [v0.28.0](docs/releases/v0.28.0.md) | AP Stars, Victory, expanded checks and subsequent crash/performance hotfix. |
| [v0.26.0-dev](docs/releases/v0.26.0-dev.md) | Quest items, character unlocks, item notifications and Game Garage repairs. |

Earlier detailed changes, including pre-release work, remain in the [historical changelog](docs/archive/checkpoints/CHANGELOG.md). Historical candidate statuses are not current installation instructions.
