# Testing and acceptance

## v0.29.1 movement patch

Player movement is accepted for initial play, after a Music Lab song, room changes, restart, Hypno Pan NPC movement, stop/release, reverse direction, gentle analogue input, wall collision and pause/resume. F8 verified the candidate active and no near-zero desired movement among 119 sampled full-input observations.

Six targeted test projects passed before preparation: PlayerMovementTiming, MovementTiming, MovementCapture, MontageAnimalTick, ItemNotifications and CassetteBundles. One normal and three faulty timing streams replayed with bounded elapsed-time carry; the normal stream retained all 1,957 updates. After the version bump, all 44 client regression projects, the Release build, repository validation and 10 packaging tests passed. Final archives were validated with checksums. The NuGet vulnerability metadata service was unavailable.

The player timing implementation accepted in gameplay is unchanged apart from the release version label. The new package has not been installed over the test client. See [movement report](testing/2026-10-12-player-movement-candidate.md) and [diagnostic report](testing/2026-10-12-player-timing-diagnostic.md).

## Additional accepted regression paths

- Star-blocked entrance can be retried immediately after receiving enough Stars, without area reload.
- Pair cassette receipts notify, deposit and become selectable without restart; persistence after restart passed.
- Original Plant Pipes/Hip Glasses pickups send checks despite already owning/using the items.
- Meoo hand-in sends its check without unlocking Meoo; Plunger and Roots/Lobby Star Eater checks passed.
- Weed Killer/Hip Glasses/Chicken Bucket consumption persists without reappearing; tested Combo Bucket conversion caused no noticeable lag.
- Superstar insertion consumes the cartridge; song availability, medal check and preview update passed.
- Quicksand/Bloody Tears source chests send randomized rewards without vanilla grants or duplicates after area return.
- Tracker's reported 62 reachable checks matched offline reconstruction of the then-current inventory and saved seed gates; this is not proof of all native routes.

## Published v0.29.0 baseline

Recorded release evidence: 42 client regression projects, 190 APWorld tests, 54 AP 0.6.8 generation cases, 1,888 Tracker states and package checks. These are historical release results, not claims that every suite is rerun for every documentation change. [Release notes](releases/v0.29.0.md).

## Scope for further testing

No exhaustive quest replay is required for the movement patch. Extended-session/multiplayer movement, triple bundles and live notification overflow remain broader coverage, not newly introduced movement regressions. Cold Storage/Cell Tower Star Eater has targeted findings recorded below; complete prerequisite isolation remains outstanding. Keep [known issues](KNOWN_ISSUES.md) open unless specific evidence closes them.

Automated native-shaped tests do not replace in-game acceptance. Do not claim a full human code audit or universal performance fix. For diagnostics and reports, see [reporting guidance](TESTING_AND_ISSUES.md).

[Historical testing ledger](archive/checkpoints/docs-TESTING.md) · [Archived evidence](archive/README.md)

## v0.29.2 Cell Tower escape repair

The user confirmed return-gate interaction/traversal with the native-interaction candidate; the unsafe Lobby exit fallback to Music Lab also passed. The initial visual-only and managed-wrapper candidates failed and are superseded. See the [acceptance record](testing/2026-10-22-hub-escape.md) for evidence and limits.

Cold Storage tests confirmed that early Violance did not prevent the note after the nectar sequence. The Cell Tower Star Eater worked without Cold Storage completion after leaving and returning to the area; no restart was required. This does not isolate every prerequisite and does not change APWorld logic.

Release validation: all 45 client regression projects passed, the Release build completed with zero warnings/errors, repository validation passed, and all 10 packaging tests passed. The APWorld asset is reused byte-for-byte from v0.29.1.
