# Consolidated Stability and Preview Abilities Design

**Status:** Approved design; implementation pending  
**Date:** 2026-08-23  
**Target baseline:** Client v0.67.63 / APWorld v0.18

## Purpose

Prepare one consolidated development build for a later large manual acceptance suite. The batch combines current client-stability repairs with preview implementations of Hypno Pan and Violance. It must reduce repeated gameplay, preserve already verified behavior, and keep unverified native progression out of generated seed logic.

This specification supplements the approved randomizer architecture. The Area Access model, permanent-ID policy, native source/item/use model, and existing acceptance evidence remain authoritative.

## Scope

The batch includes:

- the Level 22 false-positive completion repair;
- automatic recovery from an unexpected Archipelago connection loss;
- campaign Star HUD behavior that remains distinct from the Music Lab Points HUD;
- investigation and repair of the combined bottom character/difficulty control;
- cleanup of failed runtime diagnostics;
- deterministic verification of Royal Corridor's split routing and seed reachability;
- registered preview AP items for Hypno Pan and Violance; and
- one evidence-led manual acceptance suite after automated verification.

The batch does not add verified native source locations for Hypno Pan or Violance, make either item solver-required, merge, push, or publish a release.

## Preview item registration

Allocate the next permanent item IDs:

| ID | Item | Initial classification |
| ---: | --- | --- |
| `187256121` | Hypno Pan | Registered preview progression item; excluded from generated pool |
| `187256122` | Violance | Registered preview progression item; excluded from generated pool |

The items must appear in the AP datapackage so server commands such as `/send Jack Hypno Pan` and `/send Jack Violance` resolve normally. They must not appear in normal generation, placement, completion rules, or required sphere expansion until their sources and dependency graph pass native validation.

No source-location IDs are allocated in this batch. Permanent source locations are added only after native source events and durable story markers are proven.

## Native ability reconciliation

Each preview item uses a separate narrowly scoped reconciler so the already verified Plant Pipes path is not refactored or destabilized.

```text
AP received-item history
        ↓
compatible AP-bound save confirmed
        ↓
selected save and native request processor available
        ↓
submit idempotent native ability grant
        ↓
read back and verify native flag
```

Mappings:

- Hypno Pan grants `PIED_PIPER_ABILITY`.
- Violance grants `VIOLIN_ABILITY`.

Reconciliation runs after initial synchronization, delayed save availability, scene transitions, reconnect, save reload, and full restart. Duplicate deliveries and repeated lifecycle events are harmless. The client retains the last synchronized AP ownership during a temporary disconnect.

The reconcilers operate only when compatible slot data identifies the current implementation and the save is AP-bound. Vanilla saves and incompatible sessions retain native behavior. Since the abilities are permanent, reconciliation may restore a missing native flag whenever AP ownership is present; no unverified source or consumption marker is changed.

## Connection recovery

An unexpected WebSocket loss starts a bounded reconnect state machine. Retries use increasing delays with a maximum interval and never block the Unity thread. A deliberate client shutdown, configuration change, or explicit disconnect does not reconnect.

On recovery, the client:

1. validates the same server, slot, game, and compatible slot data;
2. rebuilds received-item ownership from server history;
3. retains or reconciles permanent native state idempotently;
4. flushes queued location checks once; and
5. returns to the connected state without duplicating callbacks, items, or checks.

Before the first successful synchronization, randomized gates fail closed. After synchronization, temporary connection loss retains the last valid item and currency state.

## HUD behavior

### Campaign Stars

Campaign hubs display synchronized AP Stars. Hub6 continues to display Music Lab Points instead of the campaign Star counter. The repair must distinguish display context rather than globally forcing the Star HUD active.

The Star display is validated across direct start, first campaign-area entry, Level 1 completion, hub transitions, save reload, and full restart.

### Character and difficulty control

The combined bottom HUD controls both character selection and native REG/PRO difficulty. It is unrelated to the Bizzle and Clive inventory item.

The failed native `DifficultyToggler` phase experiment is removed. Investigation must identify the actual native initialization condition that makes the control interactive after Level 1. Production code may normalize only proven initialization state. If existing code and evidence do not prove that state, the consolidated build includes focused read-only instrumentation and leaves the behavior explicitly unresolved rather than forcing a guessed phase or unrelated story flag.

Both REG and PRO remain available at initial character selection. The player chooses; the client never forces a difficulty.

## Level 22 result handling

The client emits ordinary Level 22 checks only when a matching `Level_28` result contains at least one earned Star.

- Missing result data: no checks.
- Zero Stars or failed result: no checks.
- One Star: completion plus one-Star.
- Two Stars: completion plus one- and two-Star.
- Three Stars: completion plus all three Star tiers.
- Ordinary results never emit Victory.

The future victory rule remains ordered: the configured AP Star goal must already be synchronized before a subsequent successful Level 22 clear.

## Royal Corridor and generation safety

`Royal Corridor Access` unlocks only the Hub6 phone route to the Level 22 side of Royal Corridor. It does not logically grant:

- access across the broken bridge;
- the Royal Star Eater interaction;
- the completed bridge state; or
- Level 21 reachability.

Deterministic seed matrices must prove that required progression is never placed behind falsely reachable Music Lab machines, unsafe native point thresholds, unowned Game Garage songs, unsupported performance tiers, or the inaccessible side of Royal Corridor.

## Diagnostic cleanup and evidence discipline

Remove failed or inactive runtime experiments before producing the consolidated build. Normal logs retain concise connection, item reconciliation, HUD decision, Level 22 suppression/success, and routing messages. Frame-level scans and repeated state dumps remain development-only.

The v0.67.63 acceptance ledger is the source of truth for manual evidence. A verified behavior is not retested unless this batch changes an overlapping code path or a tester reports a regression. Corrections remain explicit:

- Plant Pipes and Level 3 are already verified and are not replayed merely for completeness.
- Bizzle and Clive are not character unlocks and do not control the bottom HUD.
- Hub6 intentionally displays Music Lab Points instead of campaign Stars.

## Automated verification

Before installation, the batch must pass:

- red/green tests for Hypno Pan and Violance ownership, delayed save availability, duplicate delivery, reconnect, restart reconciliation, and incompatible-session isolation;
- reconnect state tests covering bounded backoff, recovery, deliberate shutdown, and duplicate suppression;
- Star HUD context tests separating campaign Stars from Music Lab Points;
- Level 22 failure, success, cumulative tier, and no-Victory tests;
- Royal routing and deterministic generation matrices;
- all client regression projects;
- all APWorld tests;
- repository validation; and
- a release-mode client build.

Automated verification authorizes a manual candidate, not a release claim.

## Consolidated manual acceptance suite

After automated verification, use one fresh compatible seed and save where required. Preserve prior evidence and combine overlapping checks into the fewest practical play sessions.

1. Verify direct Hub6 start, BepInEx/client versions, slot-data compatibility, and initial synchronization.
2. Verify REG and PRO are available without forcing either choice.
3. Compare the Hub6 Music Lab Points display with campaign-area AP Stars.
4. Exercise the bottom character/difficulty HUD before and after Level 1, hub transitions, save reload, and restart.
5. Restart the Archipelago server and verify automatic client recovery without restarting the game.
6. Deliver Plant Pipes, Hypno Pan, and Violance through Archipelago; verify native abilities, scene transitions, reconnect, save reload, and full restart persistence.
7. Exercise representative native Hypno Pan and Violance interactions while logging source, use, story, and dependency flags for future production locations.
8. Recheck only overlapping Game Garage and Music Lab access behavior.
9. Verify Royal phone-side routing without claiming Level 21 or Star Eater access.
10. Run a controlled Level 22 failure followed by a successful one-Star clear, confirming correct ordinary checks and no premature Victory.

If a test becomes unreasonable or requires repeated skilled gameplay, stop and improve the controlled test setup. Tester fatigue is not an acceptance mechanism.

## Release gate

Hypno Pan and Violance remain preview-only until native source events, downstream dependencies, persistence, and out-of-order behavior are validated and incorporated into solver rules. Every open release blocker remains open until its applicable manual acceptance passes.

No merge, push, tester publication, or release occurs without explicit owner approval.
