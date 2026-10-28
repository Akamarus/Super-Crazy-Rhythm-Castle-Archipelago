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

## Unreleased Cell Tower route

User acceptance confirms note reading, area re-entry, Star Eater feeding and onward-door access before nectar delivery. Later nectar delivery has not yet been retested. See [test and compatibility record](testing/2026-10-22-cell-tower-path.md).

## Unreleased Lobby door access

Demolition Training and School Trip entrance barriers are removed for authenticated Lobby Access owners. Boring Room, Important Letters delivery, the story cutscene and Demolition Training completion are no longer door prerequisites. Generated Star requirements, completion rewards, certificate award and Gecko quest objectives remain unchanged. The patch targets only the two exact native doors and their covers; it does not write story flags.

New seeds advertise `lobby_open_doors` and require the corresponding client hooks. Universal Tracker preserves the old prerequisite graph for older seeds. The candidate is not installed or published; native door interaction and School Trip's Gecko variant still require acceptance.

## Final-level crash candidate
See [crash investigation and validation](testing/2026-10-28-final-transition-crash.md). Automated checks pass; installation and native transition acceptance remain pending.

The corrected native registration candidate passed the previously crashing final-level transition and boss clear on 2026-10-28; see the linked crash record for remaining checks.

## 2026-10-28 Bunker APWorld validation

204 Python tests passed, including 12 Bunker-focused cases for source independence, goal selection, strict Star gates, legacy reconstruction and malformed Tracker contracts. Two real AP 0.6.8 six-player generations passed beatability: both goals, four difficulties, all bundle modes, the enabled two-Star minimum and disabled one-Star legacy mode. The second generation additionally covered School Trip/Gecko-source exclusions and starting Hypno Pan/Plant Pipes. Independent source review found no actionable logic defects. These checks validate APWorld behavior only, not the pending native schema21 client.

## Bunker client and diagnostic cleanup candidate

All 46 client regression projects, cleanup smoke, repository validation and 11 tool tests pass; Release build has zero warnings/errors. [Candidate details and remaining gameplay acceptance](testing/2026-10-28-bunker-client.md). No deployment or native acceptance is claimed.

## 2026-10-28 Ionium browser CI validation

The unreleased Bunker/schema21 APWorld passed the full CI preset at https://aptests.ionium.fyi/ on AP 0.6.7 and 0.6.8: 205 and 253 unit tests respectively; all 11 fuzz variants on both versions (29,000 attempted cases), zero failures and zero timeouts. Expected incompatible option combinations are ignored by the fuzzer. Universal Tracker passed on both versions. This is browser validation, not index CI approval or native gameplay acceptance.

The initial run exposed two generate_early validation checks raising ValueError instead of Options.OptionError (True Ending with Bunker disabled; Bunker enabled with required_stars below two). Both now raise OptionError; regression assertions cover the exception type. The full local 204-test suite and repository validator pass. Two outdated client-source assertions were updated to expect MusicLabProgressionKeeper and gameplay callbacks while rejecting retired QuestActionDiscovery recording calls.

Tested archive SHA256: 5659712FE58DAA107FB55A7F1E90B320149E27E9FB73E52A1367306ACCC0AC5F. Internal package version remains 0.29.0 pending release versioning; this hash identifies the unreleased candidate. Local report: outputs/ionium-bunker-validation-20261028/passing-report.zip in the task workspace; initial failing report retained alongside it. No installation or publication was performed during this validation.

## v0.30.0 release verification
Client0.77.0 Release build: zero warnings/errors. All46 client regression projects,204 APWorld regression tests,11 packaging tests, diagnostic-cleanup smoke and repository validator passed. Independent source review found no concrete release-blocking defect. Ionium-tested Python logic is unchanged; package metadata/setup documentation updated to APWorld0.30.0. Full randomized native schema21 acceptance remains pending.

