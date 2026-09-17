# Star Victory and Quest-Capacity Design

**Date:** 2026-09-11

**Status:** Approved design; implementation planning pending

**Target:** First solver-valid single-player playthrough with active AP Stars, generated campaign gates, and post-threshold Level 22 Victory

## 1. Purpose and authority

This specification defines the milestone that turns the existing full normal-campaign map into a completable Archipelago playthrough. It adds enough genuine quest/action locations to fit the approved Star inventory, activates generated Star requirements, synchronizes those requirements with the game client, and replaces the development Victory rule with the approved Level 22 goal.

This document narrows and advances the Star, capacity, and Victory portions of:

- `docs/superpowers/specs/2026-08-20-randomizer-logic-design.md`
- `docs/superpowers/specs/2026-09-10-full-level-mapping-design.md`

The Area Access model, physical source/item/use philosophy, permanent-ID policy, difficulty tiers, and tested Music Lab Point economy remain authoritative. Historical evidence in `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md` is useful for native events and ordering, but its earlier per-level access design remains superseded.

No native mapping may be guessed. A proposed action does not become a production AP location until its distinct event, reachability, one-time behavior, and persistence are proven.

## 2. Approved outcome

The first functional-playthrough milestone has these player-facing outcomes:

1. The pool contains exactly 66 individual `Star` items. There are no Star bundles.
2. `required_stars` remains configurable from 1 through 66, with 50 as the recommended default.
3. Every normal campaign level has a deterministic generated Star requirement for the seed. Opening levels may require zero Stars.
4. Area Access, meaningful item prerequisites, native story state, and generated Stars all remain independent layers of reachability.
5. Native stars earned on result screens continue to create performance checks, but only received AP Stars satisfy generated gates.
6. Level 22 remains playable below the final Star goal.
7. Victory occurs only when the player completes Level 22 after already synchronizing at least the configured number of AP Stars.
8. An early Level 22 clear sends ordinary checks but does not arm a later automatic win.
9. Receiving the final required Star after that early clear does not win. The player must complete Level 22 again.

The milestone preserves the currently forced Roots starter. Restoring randomized starter areas is separate work after all candidate starts have clean-save routing and healthy opening-sphere evidence.

## 3. Approaches considered

### Approved: verified quest/action locations

Add distinct, reportable vanilla actions that are useful checks independently of their associated level results. This expands real gameplay coverage, improves the eventual logic graph, and creates permanent capacity without changing an accepted economy.

### Rejected: shrink the Music Lab Point item count

Reducing the 20-item `10 x 1`, `3 x 10`, `7 x 20` distribution would create capacity, but that economy has already passed gameplay testing. It must not regress merely to make room for Stars.

### Deferred: use Bee/Devil checks for the immediate capacity requirement

The two Bee and four Devil completion checks are intended to join the Normal pool later. Their source, access, switch, reward, seal, and persistence chains still need diagnostic proof. They are not structural padding for this milestone and are not counted among the required action checks.

Adding a new AP item together with one new source location is capacity-neutral. Only net-new action locations increase the room available for Stars and Stardust.

## 4. Capacity requirement

The current v0.24 Normal seed has 121 addressed locations and 66 randomized non-Star item instances:

| Category | Instances |
| --- | ---: |
| Nonstarter Area Access items | 5 |
| Randomized Game Garage cartridges | 5 |
| Active quest items and abilities | 6 |
| Music Lab cassettes | 30 |
| Music Lab Point items and bundles | 20 |
| **Current total** | **66** |
| Individual AP Stars to activate | **66** |
| **Required location capacity** | **132** |

Normal is therefore short by exactly 11 locations. Implementation must confirm and activate at least 11 net-new progression-safe action locations before it can activate Stars. The target is 15 confirmed locations, producing 136 Normal locations and four Stardust slots under the current item catalog.

The final totals are derived from instantiated locations, not copied from this estimate. Every supported difficulty must have enough locations for its complete item pool. Action locations included in Normal also exist in Hard, Expert, and Perfection; difficulty continues to control only cumulative performance tiers.

No Development Cache, automatic Star milestone, automatic Music Lab Point milestone, higher-difficulty performance tier, or synthetic location may be used to satisfy this requirement.

## 5. Candidate action ledger

Every candidate is tracked in one of four states:

- **Confirmed:** distinct native event, stable identifier, reachability, one-time behavior, and persistence are verified.
- **Provisional:** supported by the historical playthrough but missing at least one production requirement.
- **Rejected:** duplicates another check, cannot be detected safely, or creates unacceptable logic.
- **Deferred:** valid future content outside this milestone.

The initial provisional ledger is:

| Area | Candidate action location | Existing evidence or concern |
| --- | --- | --- |
| Roots | Convert Chicken Bucket into Combo Bucket during Lift Quest | `LEVEL_09_COMBO_ABILITY_EARNED` is known and occurs before Level 5 completion. Confirm distinct, exactly-once reconciliation. |
| Lobby | Complete the Important Letters / Bean Trumpet mail delivery | Historical route is distinct from Boring Room completion. Exact durable completion flag and prerequisites require recovery. |
| Lobby | Complete the Plunger hand-in | Optional Star Eater route; exact source/use event and routing require recovery. |
| Lobby | Deliver Fish Tears | `LOBBY_HUB_FISH_TEARS_DEPOSITED` and blocker-cleared state are known. Confirm the one action used as the check. |
| Meat Dimension | Create the Hypno Pan | Wooden Spoon and Frying Pan conversion is observed. Confirm distinct creation marker and replay behavior. |
| Meat Dimension | Complete the Act 1 music delivery | `MEAT_HUB_ACT_ONE_MUSIC_DONE` is known. Confirm exact player action and reachability. |
| Meat Dimension | Complete the Act 3 music delivery | `MEAT_HUB_ACT_THREE_MUSIC_DONE` is known. Confirm prerequisites and separation from bouncer completion. |
| Meat Dimension | Complete the Act 4 music delivery | `MEAT_HUB_ACT_FOUR_MUSIC_DONE` is known. Confirm prerequisites and separation from the mouse route. |
| Meat Dimension | Return the cat to its bouncer | Historical Act 2 action. Recover its durable flag and prerequisites. |
| Meat Dimension | Return Scruffy | Historical Act 3 action and bouncer requirement. Recover the exact durable flag. |
| Meat Dimension | Trigger the mouse revolution | `MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED` is known. Confirm exactly-once behavior and prerequisites. |
| Cell Tower | Return both Super Nectar components | `PRISON_HUB_BEES_ESCAPED` follows consumption of both items. Confirm a distinct delivery event that does not duplicate either Bee completion. |
| Tower of Fear | Restore Minim's Eye statue | Separate delivered part and statue interaction; exact statue flag and ability rule require proof. |
| Tower of Fear | Restore Minim's Brain/Mind statue | Separate delivered part and statue interaction; exact statue flag and ability rule require proof. |
| Tower of Fear | Restore Minim's Heart statue | Separate delivered part and statue interaction; exact statue flag and ability rule require proof. |
| Tower of Fear | Complete the Totem sequence | Known to be distinct from the three branch level results. Recover the durable completion identity. |
| Royal Corridor | Feed the Royal Star Eater | `KING_CORRIDOR_STAR_EATER_FED` is known, but phone-side Level 22 access is physically separated from this route. Model the bridge/routing split before activation. |
| Roots | Feed the Roots Star Eater | Added to the discovery queue on 2026-09-14. `ROOTS_HUB_STAR_EATER_FED` exists in native metadata, but live action, routing, threshold, persistence, and identity behavior remain unproven. Blockade removal is not feeding. Diagnostic-only until confirmation. |

This 18-candidate discovery queue is not authorization to allocate IDs. Implementation selects only candidates that pass the confirmation gate. If fewer than 11 pass, Star activation remains blocked and the failed candidates stay documented with their evidence gaps.

## 6. Diagnostic-first confirmation

Evidence recovery occurs before requesting new gameplay:

1. Review `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md` and the original **Archipelago Game Implementation** conversation.
2. Recover relevant uploaded log observations and existing client diagnostics.
3. Reconcile old conclusions through the current Area Access design.
4. Request a focused gameplay trace only when the historical record is absent, contradictory, or cannot prove current behavior.

A candidate may become Confirmed only after establishing:

1. Its distinct native completion flag or event.
2. The exact physical action that causes it.
3. Whether it can fire more than once.
4. Reload and reconnect behavior.
5. Area, item, story, and Star prerequisites.
6. Whether its vanilla reward is already an AP item or another AP source.
7. Whether the action is distinguishable from a level result or neighboring quest event.
8. A safe reconciliation path when the action occurred while disconnected.

Diagnostics may observe state and bounded before/after changes. They must not allocate IDs, force interactions, grant production progression, or send unregistered locations.

## 7. APWorld architecture

### Item pool

The already registered `Star` item at permanent ID `187256118` becomes active with exactly 66 progression instances. Existing item counts, including all 20 Music Lab Point instances, remain unchanged. Remaining capacity becomes `Stardust`.

New action locations receive permanent IDs only after confirmation, beginning at the then-current safe location frontier. `docs/IDS.md`, the explicit location table, tests, and repository validator are updated in the same implementation change.

### Generated Star requirements

The existing requirements preview becomes an enforced seed contract. For each normal campaign level, generation produces one requirement that is:

- deterministic for the generated seed;
- zero or low for opening routes;
- generally nondecreasing along mandatory prerequisite chains;
- allowed to tie another level or create concurrent routes;
- scaled to `required_stars` and the active location set;
- strictly less than `required_stars`; and
- proven reachable from the actual Area Access, item, story, point, cassette/cartridge, and performance graph.

Every entrance rule includes `state.has("Star", player, requirement)` in addition to its meaningful prerequisites. Generation rejects or regenerates a threshold set when the solver cannot obtain enough Stars before a gate, an opening sphere is unhealthy, or a required Star can self-lock.

Level 22's entrance requirement is always below the Victory requirement. The optional Royal Star Eater has its own solver-valid threshold near Level 21 depth and is never required to reach Level 22 or Victory.

### Victory

The development event that treats possession of all Area Access items as Victory is removed from new-contract seeds. For generation and solver validation, an addressless `Victory` event is placed on the logical Level 22 completion route. Its rule mirrors the verified Level 22 completion requirements and additionally requires `required_stars` AP Stars. The world completion condition remains possession of that locked event.

This models the approved replayable goal as:

```text
at least required_stars AP Stars
+ a Level 22 completion performed after that total was synchronized
```

Level 22 Completion and its enabled cumulative performance locations remain ordinary checks and may be collected below the goal. Replaying Level 22 after reaching the goal does not duplicate those checks; it sends Archipelago goal status separately.

## 8. Client architecture

### Strict slot-data contract

The APWorld publishes a versioned Star/Victory sub-contract containing:

- enabled flags for AP Stars, client gate enforcement, and post-threshold Victory;
- Star item name, permanent ID, total count, and maximum effective total;
- configured `required_stars` value;
- an exact mapping of all 22 campaign identities to generated requirements;
- a campaign mapping/version reference; and
- enough catalog identity data or a digest to reject mismatched clients.

The client validates the entire sub-contract before activating any new behavior. Partial matching is insufficient.

### Synchronized Star state

The client reconstructs the effective Star total from authoritative Archipelago received-item history using permanent item ID and receipt identity/index. Duplicate callbacks do not increment it twice. The effective total is clamped to `0..66`; extra administrative or malformed receipts are reported without creating an unbounded value.

Star state belongs to the complete AP identity: game, seed/generation, team, player slot, and validated schema. An identity change clears the previous state before new history is applied. A cache may retain the last synchronized total for the same identity during a temporary disconnect, but a complete history sync replaces the cache.

The client never writes the AP Star total into native earned-Star storage. Native result stars remain performance records and AP locations only.

### HUD and gate enforcement

Where the game normally displays the campaign Star total, the client substitutes the synchronized AP Star total. The Music Lab continues to display Music Lab Points and does not gain an incorrect Star HUD.

One centralized gate policy supplies both the displayed requirement and the actual interaction decision. This prevents the visible number, door prompt, and physical access rule from disagreeing. Native prerequisites remain authoritative beneath the AP requirement.

Before the first authoritative history sync, AP Star gates fail closed. After a successful sync, a temporary disconnect uses the last synchronized total. Non-AP play and recognized old contracts retain their existing behavior.

### Action checks

A focused registry maps each confirmed native action identity to its permanent AP location. Detection reports a native event; registry resolution decides whether that event is an active AP check. The client never infers a location from display text or an ambiguous neighboring flag.

Checks completed while disconnected enter the existing identity-bound pending queue and are resent after reconnect. Native flags or a durable identity-bound journal support reconciliation. Server checked-location state and client suppression make duplicate observations harmless.

### Level 22 completion

Victory is evaluated only inside a verified successful normal Level 22 completion event:

1. Evaluate and queue ordinary Level 22 locations.
2. Snapshot the currently synchronized AP Star total.
3. If the snapshot meets `required_stars`, send Archipelago goal status.
4. Otherwise, record a bounded non-winning-clear diagnostic and do nothing further.

Receiving a Star never runs Victory evaluation. If Level 22 is completed during a disconnect, Victory may be queued only when the last synchronized total already met the goal at the moment of completion. A later reconnect or later Star receipt cannot retroactively qualify that clear.

## 9. Failure handling and compatibility

Progression safety favors a clear block over guessed state:

- Before initial sync, only AP Star-gated interactions fail closed; already accessible ungated content remains usable.
- A temporary disconnect retains the last fully synchronized Star total and identity-bound pending checks.
- A seed, team, slot, game, or schema change clears retained Star and pending state before accepting the new identity.
- A malformed new contract reports the first useful incompatibility and does not fall back to native Stars.
- An outdated client cannot silently play a new-contract seed with vanilla gates.
- A recognized old seed does not enable this system and retains its existing compatibility path.
- Failure to install the required HUD, gate, result, or history integration prevents the client from claiming the new feature active.

Normal diagnostics record bounded state changes rather than frame polling:

- contract and feature versions;
- non-secret seed/team/slot identity;
- initial synchronization readiness and effective Star total;
- each evaluated gate's identity, required total, current total, and decision;
- native action event and AP location resolution;
- pending-check queue and retry transitions; and
- Level 22 completion, Star snapshot, and Victory decision.

Logs never include server passwords, credentials, or an unbounded received-item history.

## 10. Testing strategy

### Stage 1: evidence and focused diagnostics

- Promote historical evidence into the candidate ledger.
- Run only missing, action-specific diagnostics.
- Use existing saves and server-sent setup items when first-time state is not under test.
- Use a fresh native save only for first-time flags, identity binding, or persistence behavior that cannot be proven otherwise.

### Stage 2: automated APWorld verification

Tests must prove:

1. Exactly 66 `Star` instances enter every supported generated pool.
2. Existing item counts and the `10/3/7` Music Lab Point distribution remain unchanged.
3. At least 11 confirmed action locations exist and every difficulty has sufficient capacity.
4. Item and location IDs are unique, permanent, and consistent with `docs/IDS.md`.
5. Generated requirements are deterministic, complete, in range, and below the goal.
6. Opening spheres are healthy and enough Stars are reachable before every gate.
7. Broad generation matrices sphere successfully across supported difficulties and representative `required_stars` values.
8. Impossible and self-locked fixtures are rejected.
9. Level 22 is reachable below the goal.
10. An early Level 22 clear does not satisfy Victory, a later Star receipt does not satisfy Victory, and a subsequent clear does.
11. The Royal Star Eater is optional and cannot block the Level 22 phone-side route.
12. Old contracts retain their existing behavior and malformed new contracts fail closed.

### Stage 3: automated client verification

Pure-policy and wiring tests must cover:

1. strict contract acceptance and field-by-field rejection;
2. authoritative history reconstruction and duplicate receipt handling;
3. same-identity disconnect retention and different-identity clearing;
4. campaign HUD substitution without changing the Music Lab Point HUD;
5. one policy driving both displayed and enforced gate requirements;
6. zero/awaiting, synchronized, retained-disconnect, incompatible, legacy, and non-AP states;
7. action-event registry resolution, unknown-event rejection, pending retries, and duplicate suppression;
8. Level 22 ordinary checks and Victory separation;
9. offline Level 22 Star snapshots without retroactive qualification; and
10. absence of native earned-Star writes.

The complete client regression suite, APWorld suite, repository validator, APWorld package build, and release-mode client build must pass before deployment is considered.

### Stage 4: targeted gameplay acceptance

Before requesting a complete replay, test a fresh matching seed and client for:

1. initial Star synchronization and campaign HUD behavior;
2. unchanged Music Lab Point display;
3. representative gates immediately below and at their requirements;
4. several confirmed action checks, including offline/reconnect delivery;
5. save reload and AP identity switching;
6. an early Level 22 clear that sends ordinary checks but not Victory;
7. receipt of the final required Star without automatic Victory; and
8. a subsequent Level 22 clear that sends goal status exactly once.

Server-sent items may prepare these targeted diagnostics. Such tests prove client behavior, not generated-seed solvability.

### Stage 5: full-playthrough acceptance

Only after the prior stages pass should a tester complete a fresh Normal seed without manually sent progression. Acceptance requires:

- natural progression through Area Access, meaningful items, quest actions, Music Lab Points, cassettes/cartridges, and generated Stars;
- no unintended progression block;
- an early non-winning Level 22 clear;
- natural receipt of the configured Star goal;
- a later Level 22 clear that completes the Archipelago goal; and
- successful save, quit, reconnect, and resume behavior.

Automated verification is necessary but not sufficient. The feature remains an experimental candidate until this gameplay acceptance passes.

## 11. Bee and Devil follow-up

The two Bee and four Devil completion checks remain a planned Normal-pool expansion. When their native chains are verified and activated:

- each special variant contributes one completion-only check at every AP difficulty;
- no Bee or Devil Star tiers are added;
- a special clear never emits the base level's normal locations;
- Bizzle and Clive remains one combined inventory item, not a character;
- the Demon Key and Bunker Keycard routes remain player-driven; and
- their new source/action items and locations must be accounted for together rather than assumed to provide net capacity.

This follow-up is not required for the first Star/Victory playthrough milestone and receives its own implementation plan after diagnostic confirmation.

## 12. Non-goals

This milestone does not:

- change the tested Music Lab Point distribution or thresholds;
- add automatic Star or point milestones;
- use Development Caches or performance tiers as padding;
- activate Bee/Devil production checks;
- restore per-level Access items;
- restore randomized starting areas;
- infer unverified quest flags or level prerequisites;
- add the future integrated text client, online co-op, DeathLink, traps, or versus content;
- claim local co-op gameplay verification; or
- authorize deployment, merge, push, package publication, or release.

## 13. Acceptance boundary

This specification authorizes creation of a detailed implementation plan after user review. It does not itself authorize implementation or permanent ID allocation. Production work must remain in an isolated worktree, follow test-driven development, update living documentation with player-facing changes, and stop before merge, push, deployment, or release unless separately approved.
