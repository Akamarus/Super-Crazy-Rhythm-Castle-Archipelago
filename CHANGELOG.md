# Changelog

## Unreleased

- Pause automatic reconnect when the player needs to correct their login or restore a damaged quest journal. Temporary outages and occupied slots retry with increasing delays up to one minute.
- Preserve pending quest checks across failed logins and identify the journal file that needs attention in the client log.
- Limit memory used for notifications before login and reuse the F6 history layout between changes.
- Release gameplay hooks and plugin-created Unity components on unload and restore Star Eater threshold changes the client still owns.

## v0.30.1 — November 8, 2026 — Client 0.77.1 / APWorld 0.30.0

- Default remote server addresses to encrypted WebSockets without automatic plaintext fallback; explicit `ws://` remains available for trusted plaintext servers and loopback defaults remain compatible.
- Bound incoming decompressed messages to 32 MiB before JSON parsing and terminate partial messages after 60 seconds. Idle connections remain supported. Refuse connections if the receive protection cannot load.
- Preserve the existing dependency DLLs, APWorld contracts, item IDs and seed/save behavior. Build and automated tests are separate from pending in-game acceptance.

[Full notes and limits](docs/releases/v0.30.1.md).

## v0.30.0 — October 28, 2026 — Client 0.77.0 / APWorld 0.30.0

- Added Bunker progression, six checks and an optional True Ending goal, with schema 21 support in Universal Tracker.
- Opened the Demolition Training and School Trip doors with Lobby Access and Old Tom's route with Cell Tower Access.
- Repaired the final-level transition crash and retired F8 recording and temporary profiling hooks.

[Full notes and limits](docs/releases/v0.30.0.md).

## v0.29.2 — October 22, 2026 — Client 0.76.2 / APWorld 0.29.0

- Remove the separate native attempt requirement from the exact return-exit switch, preserving its normal interaction and transition.
- Keep the exact Central Mainframe external return gate open for authenticated Cell Tower Access owners without marking the level attempted.
- Route Cell Tower exits to Music Lab while Fish Tears delivery is incomplete or unreadable, avoiding the upper-Lobby hand-blocker trap.
- No APWorld, IDs, seed placements or quest-completion changes; both reported escape routes passed targeted gameplay testing.

[Full notes and limits](docs/releases/v0.29.2.md).

## v0.29.1 — October 12, 2026 — Client 0.76.1 / APWorld 0.29.0

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
