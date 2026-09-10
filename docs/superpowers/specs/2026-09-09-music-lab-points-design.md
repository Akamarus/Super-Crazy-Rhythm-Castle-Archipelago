# AP Music Lab Points Design

**Date:** 2026-09-09  
**Status:** Approved; not implemented  
**Target compatibility:** APWorld v0.23 and its matching client  
**Related design:** `2026-08-20-randomizer-logic-design.md`, section 6

## Problem

Music Lab reward chests currently use the game's native clean-medal score through `CurrentPlayerSaveEnquiries.GetMedalScore()`. That couples chest progression to local Music Lab performance instead of the Archipelago item pool. It also prevents the solver from reasoning about the currency needed for the nine existing chest locations.

The approved randomizer design replaces that currency only for a compatible APWorld v0.23 session. Music Lab Points become received Archipelago inventory, separate from AP Stars and from the native medal score. The existing Music Lab display and chest interactions remain the player-facing interface.

## Goals

- Add a weighted AP Music Lab Point economy totaling 180 points in 20 item instances.
- Use that AP total for the existing Music Lab point display and the nine native reward-chest thresholds.
- Let the Archipelago solver place point items behind earlier point chests when the resulting chain is reachable.
- Preserve all nine existing chest locations and all cassette/cartridge source reuse without duplicate checks.
- Rebuild the point total idempotently from authoritative received-item history.
- Preserve the last synchronized total during a temporary disconnect.
- Leave native clean-medal results and saved medal score untouched.
- Preserve native point behavior for older v0.22 seeds and non-AP play.
- Diagnose and prove the native integration boundary before production behavior is enabled.

## Non-goals

- Adding Music Lab Point milestone locations.
- Awarding AP points from campaign Stars, cassette medals, or Game Garage medals.
- Replacing the Music Lab display with a custom HUD.
- Writing the AP point total into the native save or medal-score fields.
- Changing chest identities, rewards, collection flags, or check IDs.
- Creating duplicate locations for cassettes or cartridges already sourced from point chests.
- Supporting the new economy on an existing v0.22 seed or treating an old save as a v0.23 acceptance baseline.
- Packaging, deploying, merging, pushing, publishing, or releasing without separate approval.

## Compatibility Boundary

APWorld v0.23 introduces a new, strict Music Lab Point slot-data contract and requires a newly generated seed. Initial gameplay acceptance also uses a fresh native save.

The client has three compatibility outcomes:

1. **Compatible v0.23 contract:** enable AP Music Lab Points and ignore native medal score for the effective Music Lab total.
2. **Recognized v0.22 or older contract:** do not enable this feature; preserve the existing native medal-score behavior.
3. **A seed claiming v0.23 with a missing, malformed, or contradictory point contract:** fail closed, show a clear incompatibility error, and keep the effective point total at zero rather than silently falling back to native medals.

Non-AP saves remain vanilla. A server, seed, generation, or player-slot identity change clears the prior AP point state so a total cannot leak between sessions.

## AP Item Economy

The v0.23 pool contains these progression items:

| Item | Count | Value each | Total value |
| --- | ---: | ---: | ---: |
| Music Lab Point | 10 | 1 | 10 |
| Music Lab Point Bundle | 3 | 10 | 30 |
| Music Lab Point Large Bundle | 7 | 20 | 140 |
| **Total** | **20** |  | **180** |

These 20 instances replace 20 Stardust filler instances. They do not increase the number of generated locations. All three item names receive new permanent network IDs in the approved order from the next safe item ID recorded in `docs/IDS.md`; the registry is updated during implementation, not by this design-only change.

The effective total is the weighted sum of authoritative receipts, clamped to the contract range `0..180`. The cap prevents malformed data or extra duplicate/admin deliveries from creating an unbounded native display value. No item may contribute merely because a transient callback fired; receipt indexes/history are authoritative.

## Slot-Data Contract

The APWorld publishes an explicit Music Lab Point sub-contract in v0.23. It contains:

- an enabled flag and point-schema version;
- the exact three item names and their permanent IDs;
- exact values `1`, `10`, and `20`;
- exact pool counts `10`, `3`, and `7`;
- total item instances `20`;
- total available value `180`;
- maximum effective total `180`; and
- the exact threshold-to-location map for `5`, `10`, `20`, `32`, `46`, `64`, `89`, `111`, and `140`.

The repository's top-level slot-data schema advances for v0.23. The client validates the complete sub-contract before enabling AP point behavior; partial matching is not sufficient. Validation uses the permanent IDs rather than display text alone when rebuilding received inventory.

## Chest Logic and Solver Behavior

The existing Music Lab locations remain:

| Required AP points | Existing location |
| ---: | --- |
| 5 | Music Lab - 5 Point Chest |
| 10 | Music Lab - 10 Point Chest |
| 20 | Music Lab - 20 Point Chest |
| 32 | Music Lab - 32 Point Chest |
| 46 | Music Lab - 46 Point Chest |
| 64 | Music Lab - 64 Point Chest |
| 89 | Music Lab - 89 Point Chest |
| 111 | Music Lab - 111 Point Chest |
| 140 | Music Lab - 140 Point Chest |

Each location's access rule compares its threshold with the weighted AP point total in `CollectionState`. The final chest intentionally leaves 40 points of slack.

The current blanket rule that forbids progression items in all Music Lab point chests is removed for v0.23. Point items and other progression may appear behind a chest only when normal Archipelago fill and reachability prove an earlier valid route to that chest. This is ordinary monotonic solver progression, not a special exception or hand-authored item placement.

The five cassette sources already represented by the 32-, 64-, 89-, 111-, and 140-point chests continue to reuse those exact locations. Gradius Remix and Bloody Tears cartridge sources continue to reuse the 10- and 46-point chests. Opening one physical chest sends its one existing AP location exactly once; no additional currency, cassette, or cartridge check is created.

Cassette and Game Garage performance locations remain separate checks. Their native medal results never add AP Music Lab Points.

## Client State Model

The client keeps an identity-bound point state with these effective modes:

- **Native:** non-AP play or a recognized pre-v0.23 seed; return the real native medal score.
- **Awaiting initial synchronization:** a compatible v0.23 contract is active but authoritative received-item history is not ready; return zero so every point chest remains locked.
- **Synchronized:** received-item history is ready; return the weighted AP total.
- **Disconnected after synchronization:** retain and return the last synchronized total until the same AP identity reconnects or is replaced.
- **Incompatible v0.23:** return zero and surface the contract error; never substitute native medals.

On initial synchronization and every reconnect, the client rebuilds the total from the complete received-item history for the active AP player. Reprocessing the same receipt indexes is idempotent. A newly received point item updates the synchronized total once, and reconnecting verifies the result rather than incrementing cached state again.

The synchronized state belongs to the full AP identity: game, seed/generation, player slot, and validated schema. Deactivation or an identity change discards it. A temporary network loss after successful synchronization does not discard it, and chest checks completed offline continue through the existing queued-location mechanism.

## Native Integration

The intended integration reuses the existing exact `CurrentPlayerSaveEnquiries.GetMedalScore()` postfix currently used by the developer-only Music Lab point override. Production behavior is not enabled merely because the method name appears correct.

Before implementation commits to this boundary, a diagnostic build must prove:

- the native Music Lab point display reads this result;
- all nine chest eligibility paths read this result or an equivalent value derived from it;
- returning AP totals does not alter unrelated score, result, progression, or save behavior; and
- no native save field changes when only the effective result is replaced.

If diagnostics show unrelated consumers, implementation must narrow the override using a separately verified context or identify a narrower native boundary. It must not guess from type or object names.

Once proven, one postfix records the original native value and applies this precedence:

1. suppress recursion while deliberately reading the original native value;
2. if a compatible AP point session is awaiting synchronization or incompatible, return zero;
3. if it is synchronized or retaining a disconnected total, return that AP total;
4. otherwise, permit the existing developer-only override when explicitly enabled; and
5. otherwise, return the untouched native value.

The developer Shift+F4 point override is disabled while a compatible v0.23 AP point session owns the effective total. This prevents a testing cheat from affecting live AP chest eligibility.

The client never writes the AP total into native medal storage. Native campaign, cassette, and Game Garage result processing continues normally. The Music Lab's existing display and chest interactions update because they consume the effective getter result; no chest is forced open and no interaction is simulated.

## Failure Handling

- **History unavailable before first synchronization:** remain at zero and keep chests locked.
- **Temporary disconnect after synchronization:** retain the last valid total and queue newly completed locations through existing behavior.
- **Reconnect:** rebuild from authoritative history, compare with the retained total, and replace the cache with the rebuilt result without double-counting.
- **Malformed v0.23 contract:** keep the feature incompatible and the effective total at zero; log and display a concise reason without sensitive connection details.
- **Unknown point item or invalid item ID:** ignore it for point arithmetic and report the contract/history mismatch once per connection identity.
- **Arithmetic overflow or value above the contract maximum:** saturate at 180 and report the anomaly.
- **Native getter or patch unavailable:** do not claim the economy active; fail the compatibility/diagnostic gate and leave production release blocked.
- **Native chest read unavailable:** preserve the existing chest discovery/reconciliation retry behavior; never synthesize a collected check.
- **Duplicate chest observation or AP receipt:** remain idempotent.

## Logging and Player Feedback

Normal logs record bounded state transitions rather than frame polling:

- point-contract accepted or rejected;
- awaiting initial item synchronization;
- synchronized total and item-instance count;
- one weighted-total change per newly accepted receipt index;
- disconnect with retained total;
- reconnect rebuild result and any corrected mismatch; and
- native effective-score boundary enabled or unavailable.

The existing AP connection/incompatibility feedback should explain when the point contract prevents play. Logs must not expose credentials, private server data, or unbounded received-item history.

## Automated Verification

Implementation follows test-driven development.

APWorld tests must prove:

1. the three permanent items have unique, unreused IDs and progression classification;
2. the pool contains exactly `10/3/7` point items worth exactly 180 total;
3. the 20 items replace Stardust and do not change active location counts;
4. every chest has the correct weighted access threshold;
5. point arithmetic uses counts multiplied by values rather than raw item count;
6. progression is no longer blanket-forbidden at point chests;
7. solver-valid chains can place points or other progression behind earlier chests without self-lock;
8. deliberately impossible/self-locked fixtures are rejected;
9. reused cassette and cartridge sources create no duplicate locations;
10. no Music Lab Point milestone locations exist;
11. slot data contains the exact strict contract; and
12. representative generation matrices across every supported difficulty/start combination sphere successfully and reproducibly.

Client pure-policy and wiring tests must prove:

1. recognized v0.22 and non-AP play return native score;
2. compatible v0.23 returns zero before initial synchronization;
3. exact contract validation rejects every missing, extra, renamed, re-IDed, or revalued field;
4. receipt history produces the exact weighted total and is idempotent by receipt identity/index;
5. totals clamp to `0..180` safely;
6. disconnect retains the last synchronized total;
7. reconnect rebuilds and corrects cached state without double-counting;
8. AP identity changes clear the prior total;
9. incompatible v0.23 stays at zero and cannot fall back to native medals;
10. the developer point override cannot supersede a compatible AP session;
11. production wiring uses only the diagnosed score boundary and performs no native score write; and
12. chest reconciliation remains exactly-once and queues checks while disconnected.

The full APWorld suite, all client regression projects, repository validator, APWorld packaging, and a release-mode client build must pass before deployment is considered.

## Manual Acceptance

The first decisive live test uses a fresh v0.23 seed, matching client, and fresh native save.

1. Confirm BepInEx loads the intended client build and the v0.23 point contract is accepted.
2. Before item synchronization, confirm the effective total is zero and all nine chests remain locked.
3. Complete synchronization with no point items and confirm the Music Lab display remains zero.
4. Deliver a controlled mix of 1-, 10-, and 20-point items and confirm the display changes by the exact weighted amounts.
5. Test immediately below and exactly at each threshold: `5`, `10`, `20`, `32`, `46`, `64`, `89`, `111`, and `140`.
6. Confirm a chest becomes interactable only when its threshold is met; the client does not force it open.
7. Open representative early, middle, and final chests and confirm each sends its existing AP check exactly once.
8. Confirm the five reused chest cassettes and two reused Garage cartridges still follow their normal AP receipt/use paths without duplicate checks.
9. Earn cassette and Game Garage medals and confirm native results persist while the AP point total does not change.
10. Disconnect after synchronization, earn/open an available chest, and confirm the retained total remains visible and the check queues for reconnection.
11. Reconnect and confirm authoritative history rebuilds the same total and queued checks send once.
12. Close and relaunch the game, load the same AP identity, and confirm the rebuilt total is correct.
13. Load a recognized v0.22 seed and confirm its native Music Lab medal-score economy is unchanged.
14. Load a malformed v0.23 contract fixture and confirm the client explains the incompatibility and keeps the effective total at zero.

## Acceptance Boundary

Automated verification is necessary but not sufficient for the client/native boundary. The feature remains labeled manually unverified until the complete fresh-seed live acceptance passes, including threshold behavior, chest checks, disconnect/reconnect, relaunch, and the v0.22 regression.

This design authorizes only creation of the subsequent implementation plan. It does not authorize implementation, deployment, merge, push, package publication, or release.
