# Cassette Save-Transaction Design

**Date:** 2026-08-30  
**Status:** Revised boundary approved after live diagnostics
**Related design:** `2026-08-29-full-cassette-randomization-design.md`

## Problem

Archipelago cassette ownership is session-wide, while the game's cassette state belongs to the currently loaded save. The current client can stage `HAVE_IN_BAG` before a slot finishes loading or during a level-result transaction, observe that transient state, and cache the cassette as satisfied for the rest of the process. A later slot load or enclosing save transaction can replace that state. The client then refuses to revalidate, leaving the received cassette absent from the player's inventory.

`VerifiedBag` therefore cannot mean “persisted.” It only means the current in-memory processor state contains `HAVE_IN_BAG` at that instant.

## Goals

- Apply every AP-owned cassette to the actual loaded save.
- Persist receipts through the game's native semantic player-save request lifecycle.
- Revalidate after every load, reload, and save switch, including reselecting the same slot.
- Never return or duplicate a cassette already marked `HAVE_DEPOSITED`.
- Support receipts arriving during campaign and Music Lab result processing.
- Avoid private save APIs, forced flushes, and the crashing selected-slot event hook.

## Non-goals

- Changing cassette item placement, source routing, medal logic, or machine insertion.
- Creating a custom save file or independent persistence layer.
- Automatically depositing cassettes or force-unlocking Music Lab levels.
- Repairing unrelated difficulty/HUD behavior.

## Architecture

### Loaded-save epochs

The client maintains a monotonically increasing loaded-save epoch. The broad `SelectMostRecentlyUsedRegularPlayerSaveSlotRequest`, `SelectPlayerSaveSlotRequest`, `EnsureAPlayerSaveSlotIsSelectedRequest`, and `CreateNewPlayerSaveFileInSlotRequest` postfixes are not authoritative: live testing proved that startup can expose slot 0 through those enquiries before the UI installs the final playable slot 4, and the UI flow may not produce a later broad-request postfix.

Instead, exact postfixes on `SaveDataRequestProcessor.ChangeSelectedPlayerSaveSlot(Int32)`, `SaveDataRequestProcessor.CreateNewPlayerSaveFileInEmptySlot(Int32)`, and `SaveDataRequestProcessor.ProcessRequest(BuildPlayerSaveStateFromFileRequest)` enqueue an expected slot, signal kind, and generation. `BuildPlayerSaveStateFromFileRequest` supplies both `SlotNumber` and `PlayerSaveFile`, making it the explicit state-build/replacement signal. These callbacks only mutate managed boundary state; they never enquire into the selected save or read/write native cassette state.

Queuing any exact Selection, Creation, or Build signal immediately suspends reconciliation and delayed verification for the previously active epoch. While a boundary signal or candidate is pending, all native reads, submissions, and verification retries fail closed. The old epoch is not resumed if stabilization is delayed or fails. Only activation of a newly stabilized candidate resumes reconciliation, clears stale epoch-local attempts/verifications, and reevaluates session-wide AP ownership in the new epoch.

The Unity keeper stabilizes the `PlayerSaveRequestProcessor` state pointer after the newest exact boundary signal before advancing the epoch. Immediately before any cassette status read, grant, or delayed verification, it then requires the authoritative public loaded-save evidence: `PlayerSaveManagementEnquiries.IsAValidExistingSaveSelected()` must return `true`, and `TryGetSelectedSlotSaveFileState()` must return a non-null public state whose `Il2CppObjectBase.Pointer` exactly matches the active epoch's stabilized player-save pointer. An invalid selection, unreadable enquiry, empty state, or pointer mismatch defers without consuming the cassette attempt.

`PlayerSaveManagementEnquiries.GetSelectedSaveFileSlotNumber()` and `SaveDataState.SelectedPlayerSaveSlot` are diagnostic only. Live Continue testing proved that either can remain numeric slot 0 while the validity enquiry and selected-state pointer identify the actually loaded save. Slot 0 therefore neither authorizes nor rejects reconciliation: a valid selection plus exact pointer equality is the authority. The boundary signal's slot remains epoch bookkeeping and logging, not a second global-selection gate.

No cassette is considered terminal across epochs. `HAVE_IN_BAG` and `HAVE_DEPOSITED` are terminal only within the epoch in which an authoritative read observed them.

Reconciliation remains inactive before the first confirmed loaded-save epoch.

Duplicate create/build/change signals coalesce into one pending generation until the candidate stabilizes. Every newer exact signal, including a same-slot signal, advances/coalesces that generation, invalidates the current candidate identity, and resets the consecutive-observation count to zero. Thus both matching observations occur after the newest signal. Same-slot coalescing retains `includesBuild=true` if any signal was Build; a later Creation or Selection signal cannot erase it. A signal for a different expected slot replaces the stale expected slot and Build bit. A stable slot change or native pointer change advances the epoch. A new consumed Build generation for the selected slot also advances the epoch once, even if IL2CPP reuses the same pointer, so same-slot reloads cannot inherit satisfaction. Each coalesced generation activates at most once, and later non-Build signals with the already-activated `(slot, pointer)` do not create duplicate epochs.

### Pending AP ownership

AP receipt history remains the source of session-wide cassette ownership. Each owned cassette is evaluated against the active epoch:

- `HAVE_DEPOSITED`: mark satisfied for this epoch and never submit a bag grant.
- `HAVE_IN_BAG`: mark satisfied for this epoch.
- missing: keep pending until the selected save is readable and a compatible player-save processor is available.
- authoritative read unavailable or unknown: fail closed and keep pending.

### Revised semantic request boundary

Live diagnostics disproved the earlier assumption that `PersistSaveChangeBundleRequest` or `PersistAllSaveChangeBundlesRequest` provides a reachable cassette boundary. Neither prefix ran during save load, area travel, return to title, difficulty change, shutdown, nor a proven durable AP Plant Pipes grant. Cassette reconciliation must not wait for those absent callbacks.

After a loaded-save epoch is confirmed, reconciliation runs only on the Unity thread at existing safe lifecycle points. For each pending AP-owned cassette it:

1. Confirms compatible synchronized slot data, an active epoch, and no pending save-boundary signal or candidate.
2. Authoritatively reads the selected save through `SongCassetteEnquiries.GetSongCassetteStatus`.
3. Leaves `HAVE_IN_BAG` and `HAVE_DEPOSITED` unchanged and satisfies them only for the current epoch.
4. Leaves unreadable or unknown state pending and fails closed.
5. For `INVALID` or `HAVE_NOT_EARNED`, constructs the public parameterless `RecordSongCassetteStatusInSaveDataRequest`, assigns only its public `Song` and `CassetteStatus=HAVE_IN_BAG` properties, and verifies those values plus an absent public `Bundle` readback. The native request processor's proven absent-bundle fallback supplies `DEFAULT`.
6. Invokes the matching `PlayerSaveRequestProcessor.ProcessRequest(...)` overload through the compatible captured processor, or the proven stateless processor fallback only after selected-save enquiries are readable.
7. Treats a successful invocation as `verification-pending`, never as persisted or satisfied.
8. Re-reads once after a bounded delay. Only authoritative `HAVE_IN_BAG` or `HAVE_DEPOSITED` satisfies the current epoch. A cassette receives at most one semantic grant attempt per epoch; another lifecycle observation cannot duplicate it.

This mirrors the durable Plant Pipes lifecycle. The native record request participates in the game's own save/change-bundle and ordinary save-write lifecycle. Cassette reconciliation does not construct or submit `PersistSaveChangeBundleRequest`, `PersistAllSaveChangeBundlesRequest`, `TriggerUrgentSaveWriteIfAnyChangesRequest`, or `RequestWriteForPlayerSave`; it does not subscribe to `PlayerSaveWriteCompletedEvent`; and it runs no synthetic disk-write timer or success/failure transaction. The client never calls a private save manager or flush method.

The exact cassette request mechanism has live evidence: the parameterless request with explicit `Song`/`CassetteStatus` and absent `Bundle` reaches the native RecordSong processor as effective `DEFAULT`, while reflected nullable bundle construction produced invalid payloads. The remaining responsibility is safe loaded-save timing and epoch-scoped verification, not a new persistence API.

### Load and switch behavior

Every confirmed load/reload/switch:

- advances the epoch;
- clears per-epoch satisfaction and request-attempt markers;
- preserves AP ownership;
- re-reads all owned cassettes;
- keeps missing cassettes pending for post-epoch semantic request reconciliation.

The client must not use `SelectedPlayerSaveSlotChangedEvent.HandleEvent`; the prior diagnostic established that unsafe IL2CPP event/reflection hooks can corrupt the runtime.

Room transitions, level results, slot-data synchronization, and processor capture may request reconciliation only inside an already-active epoch. They never establish or replace a loaded-save epoch. A newer exact save-boundary signal invalidates any unstabilized candidate and stale queued work from the prior generation.

## Logging

Logs distinguish submission from verified native state:

- `pending`: AP-owned but missing from the active save;
- `grant submitted`: exact semantic request returned normally; verification remains pending;
- `verified bag`: observed during a later authoritative read;
- `verified deposited`: terminal for the current epoch;
- `deferred`: no active epoch, selected save unreadable, compatible processor unavailable, or bounded retry pending.

No log may call a cassette persisted merely because request invocation returned without an exception.

## Failure handling

- Missing save state or reader: do not submit; retry at a later safe boundary.
- Unknown native status: do not overwrite; log once per epoch/status transition.
- Native semantic request failure or failed bounded verification: leave AP ownership pending, but do not duplicate the semantic grant in the same epoch; the next activated epoch revalidates and may grant once again if still unearned.
- Save load during pending work: discard epoch-local markers and evaluate the new epoch.
- Exact save-boundary signal during pending work: suspend the old epoch immediately; do not read, submit, or verify until a new candidate activates.
- Duplicate AP receipt: idempotent; it does not create another native request.

## Testing

Automated tests must prove:

1. No native request occurs before a confirmed loaded-save epoch.
2. Exact create/change/build callbacks only enqueue and never perform native cassette reads or writes.
3. A candidate cannot activate until the same non-zero player-save processor pointer is observed on two consecutive Unity updates after the newest boundary generation.
4. A first load, same-slot reload (including pointer reuse), and different-slot switch each advance the epoch exactly once; duplicate signals coalesce.
5. A numeric selected-slot value of 0 is non-authoritative: valid selection plus a selected-state pointer matching the active epoch is ready, while invalid/unreadable selection or a pointer mismatch defers without a request.
6. Room transition alone never creates or replaces an epoch.
7. An active slot-0 epoch followed by a queued slot-4 signal performs no old-epoch read, submission, or delayed verification before slot 4 activates.
8. Signal slot 4, observe `(4, 400)` once, then signal same-slot Build: the count resets; the next observation does not activate and the following matching observation activates exactly once with Build preserved.
9. Process-wide AP ownership survives epoch changes while native satisfaction does not.
10. Missing cassettes submit only after an active loaded-save epoch and an authoritative unearned read.
11. Submission uses the public parameterless semantic request, public `Song`/`CassetteStatus=HAVE_IN_BAG` setters, verified absent `Bundle`, and the native `DEFAULT` fallback.
12. `HAVE_IN_BAG` and `HAVE_DEPOSITED` prevent submission within an epoch.
13. Deposited cassettes remain deposited across reloads.
14. Authoritative-read failures fail closed.
15. A receipt arriving during campaign or Music Lab result processing waits for the Unity keeper rather than writing inside the result transaction.
16. Same-slot reload after a transient bag observation revalidates and repairs the active save.
17. Source wiring contains no selected-slot event hook, synthetic Persist construction/submission, write-completed event consumer, private save flush/save-manager API, forced deposit/unlock, or process-wide satisfaction cache.

Live acceptance uses fresh seed `8302601` at `127.0.0.1:38282`, slot `Jack`, with UI save slot 4:

- no cassette submission occurs while the validity enquiry is false or the selected-state pointer differs from the activated epoch, regardless of the numeric selected-slot value;
- an epoch for native slot 4 is logged after its selected `(slot, pointer)` stabilizes;
- AP history restores Badass and The Heist into the loaded save;
- both appear in the top-right inventory;
- a full restart before insertion restores both as `HAVE_IN_BAG` without duplicate requests or replayed checks;
- normal machine insertion unlocks their corresponding Music Lab levels;
- a second full restart reports them `HAVE_DEPOSITED` and does not return them;
- no repeated receipt loop or crash occurs.

## Acceptance boundary

This repair may be installed into the existing authorized test targets after automated verification and independent review. It does not authorize merging, pushing, publishing, or releasing the feature branch.
