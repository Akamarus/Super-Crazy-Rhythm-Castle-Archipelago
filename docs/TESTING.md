# Testing and acceptance

## Unreleased client audit fixes

The connection tests cover login refusal, retry after a temporary outage, a damaged quest journal and recovery from backup with pending checks from another seed. Notification tests replay interleaved receipts and sends and count text measurements while the F6 list is open. The unload tests cover component ownership, restoring thresholds and retrying a failed hook removal. [Audit findings](cleanup-audit.md#follow-up-on-the-external-v0282-audit)

For gameplay acceptance, reconnect to a test room with queued items and open F6 to confirm their order and readability. In a disposable save, revisit an affected Star Eater across client unload and reload to check the restored threshold and interaction.

## v0.30.1 network security

At release, all 47 client regression projects, the repository validator and 11 tool tests passed. The Release build completed with zero warnings or errors. Local tests used the pinned network library to check message size limits, fragmented JSON and TLS failure handling. In-game connection and live large-room acceptance remain open. [Validation record](testing/2026-11-08-network-security.md)

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

## v0.30.0 Cell Tower route

User acceptance confirms note reading, area re-entry, Star Eater feeding and onward-door access before nectar delivery. Later nectar delivery has not yet been retested. See [test and compatibility record](testing/2026-10-22-cell-tower-path.md).

## v0.30.0 Lobby door access

The door policy tests and three-player generation passed. Entering both doors with Lobby Access before the story prerequisites, checking the Star gates, and playing School Trip's Gecko variant remain open gameplay checks. [Lobby door test record](testing/2026-10-28-lobby-doors.md)

## v0.30.0 final-level crash repair

The corrected native event handler passed the previously crashing final-level transition and boss clear on October 28. Old Tom's route still needs a recheck with that handler. [Crash investigation and validation](testing/2026-10-28-final-transition-crash.md)

## 2026-10-28 Bunker APWorld validation

204 Python tests passed, including 12 Bunker-focused cases for source independence, goal selection, strict Star gates, legacy reconstruction and malformed Tracker contracts. Two real AP 0.6.8 six-player generations passed beatability: both goals, four difficulties, all bundle modes, the enabled two-Star minimum and disabled one-Star legacy mode. The second generation additionally covered School Trip/Gecko-source exclusions and starting Hypno Pan/Plant Pipes. Independent source review found no actionable logic defects. These checks validate APWorld behavior only, not the pending native schema21 client.

## Bunker client and diagnostic cleanup candidate

All 46 client regression projects, cleanup smoke, repository validation and 11 tool tests pass; Release build has zero warnings/errors. [Candidate details and remaining gameplay acceptance](testing/2026-10-28-bunker-client.md). No deployment or native acceptance is claimed.

## 2026-10-28 Ionium browser CI validation

The pre-release Bunker APWorld passed Ionium's full browser CI preset on AP 0.6.7 and 0.6.8. The runs passed 205 and 253 unit tests respectively, including Universal Tracker. Eleven fuzz variants ran on each version (29,000 attempted cases in total), with zero failures and zero timeouts. The fuzzer ignored expected incompatible option combinations.

The initial run exposed two generate_early validation checks raising ValueError instead of Options.OptionError (True Ending with Bunker disabled; Bunker enabled with required_stars below two). Both now raise OptionError; regression assertions cover the exception type. The full local 204-test suite and repository validator pass. Two outdated client-source assertions were updated to expect MusicLabProgressionKeeper and gameplay callbacks while rejecting retired QuestActionDiscovery recording calls.

The tested candidate still carried internal version 0.29.0. Its SHA256 was `5659712FE58DAA107FB55A7F1E90B320149E27E9FB73E52A1367306ACCC0AC5F`.

## v0.30.0 release verification

Client 0.77.0 built with zero warnings or errors. All 46 client regression projects, 204 APWorld regression tests, 11 tool tests, the diagnostic cleanup smoke test and the repository validator passed. The release retained the Ionium-tested Python logic and updated the metadata and setup documentation to APWorld 0.30.0. Full randomized schema 21 gameplay acceptance remains open.
