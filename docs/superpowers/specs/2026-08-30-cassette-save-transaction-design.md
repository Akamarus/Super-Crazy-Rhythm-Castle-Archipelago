# Cassette Save-Transaction Design

**Date:** 2026-08-30  
**Status:** Implemented and live-accepted on the feature branch; not released
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
- Use only the exact pointer-bound public promotion/urgent-write route; avoid private save APIs, generic persist requests, and the crashing selected-slot event hook.

## Non-goals

- Changing cassette item placement, source routing, medal logic, or machine insertion.
- Creating a custom save file or independent persistence layer.
- Automatically depositing cassettes or force-unlocking Music Lab levels.
- Repairing unrelated difficulty/HUD behavior.

## Architecture

### Loaded-save epochs

The client maintains a monotonically increasing loaded-save epoch. The broad `SelectMostRecentlyUsedRegularPlayerSaveSlotRequest`, `SelectPlayerSaveSlotRequest`, `EnsureAPlayerSaveSlotIsSelectedRequest`, and `CreateNewPlayerSaveFileInSlotRequest` postfixes are not authoritative: live testing proved that startup can expose slot 0 through those enquiries before the UI installs the final playable slot 4, and the UI flow may not produce a later broad-request postfix.

Instead, exact postfixes on `SaveDataRequestProcessor.ChangeSelectedPlayerSaveSlot(Int32)`, `SaveDataRequestProcessor.CreateNewPlayerSaveFileInEmptySlot(Int32)`, and `SaveDataRequestProcessor.ProcessRequest(BuildPlayerSaveStateFromFileRequest)` enqueue an expected slot, signal kind, and generation. Each callback synchronously closes gameplay readiness before inspecting any callback argument; malformed or unreadable arguments therefore leave the old epoch failed closed and emit only a bounded rejection diagnostic. `BuildPlayerSaveStateFromFileRequest` supplies both `SlotNumber` and `PlayerSaveFile`, making it the explicit state-build/replacement signal. These callbacks only mutate managed boundary state; they never enquire into the selected save or read/write native cassette state.

Queuing any exact Selection, Creation, or Build signal immediately suspends reconciliation and delayed verification for the previously active epoch. While a boundary signal or candidate is pending, all native reads, submissions, and verification retries fail closed. The old epoch is not resumed if stabilization is delayed or fails. Activation of a newly stabilized candidate clears stale epoch-local attempts/verifications, but cassette work remains closed until the gameplay-ready marker gate below opens for that exact epoch.

The Unity keeper stabilizes the `PlayerSaveRequestProcessor` state pointer after the newest exact boundary signal before advancing the epoch. Immediately before any cassette status read, grant, or delayed verification, it then requires the authoritative public loaded-save evidence: `PlayerSaveManagementEnquiries.IsAValidExistingSaveSelected()` must return `true`, and `TryGetSelectedSlotSaveFileState()` must return a non-null public state whose `Il2CppObjectBase.Pointer` exactly matches the active epoch's stabilized player-save pointer. An invalid selection, unreadable enquiry, empty state, or pointer mismatch defers without consuming the cassette attempt.

`PlayerSaveManagementEnquiries.GetSelectedSaveFileSlotNumber()` and `SaveDataState.SelectedPlayerSaveSlot` are diagnostic only. Live Continue testing proved that either can remain numeric slot 0 while the validity enquiry and selected-state pointer identify the actually loaded save. Slot 0 therefore neither authorizes nor rejects reconciliation: a valid selection plus exact pointer equality is the authority. The boundary signal's slot remains epoch bookkeeping and logging, not a second global-selection gate.

No cassette is considered terminal across epochs. `HAVE_IN_BAG` and `HAVE_DEPOSITED` are terminal only within the epoch in which an authoritative read observed them.

Reconciliation remains inactive before the first confirmed loaded-save epoch.

### Gameplay-ready marker gate

The activated save-state pointer can be correct before native scene loading has finished rebuilding that same object. Cassette reconciliation therefore also requires a fresh incarnation of the exact live Hub6 marker `Root/GameRoom_Hub6_Logic/Objects/Phones`. The always-on Unity keeper observes only that public scene object, records its public Unity instance identity, and correlates absence-to-presence or identity-change evidence with the current managed save-boundary revision. Boundary callbacks never inspect Unity objects.

A marker object already live from the previous boundary cannot open a new epoch. It must disappear and return, or be replaced by an object with a different instance identity, after the boundary began. Evidence may arrive before pointer stabilization and epoch activation, which supports direct-start loads whose fresh Hub6 scene becomes live before the second stable pointer observation. Once opened, the gate remains open across ordinary room travel; later marker absence does not close it. Every exact Selection, Creation, Build, activation without a pending boundary, deactivation, or incompatible slot-data configuration closes it. Hub6 readiness queues ordinary semantic reconciliation and never creates an epoch or submits a save request itself.

Marker-boundary revisions do not coalesce: every exact postfix advances the revision before argument extraction and invalidates evidence retained by any earlier pending callback. After successful extraction, `QueueSaveBoundarySignal` records only the save-identity signal and does not advance the marker revision a second time. Thus only marker lifetime evidence observed after the newest exact callback can open its eventual epoch, even when the separate save-identity stabilizer coalesces several signals for one load.

There is no proven public title/save-unload callback with safe ordering. The client therefore does not guess one. During the residual interval after returning to title and before the next exact Selection/Creation/Build callback, the previous epoch may remain marked gameplay-ready; the next exact boundary closes it before the plugin extracts or processes any new-load identity, and all later cassette work remains closed until fresh marker-lifetime and selected-pointer evidence agree. This limitation is explicit rather than hidden behind an unsafe broad hook.

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

1. Confirms compatible synchronized slot data, an active epoch, no pending save-boundary signal or candidate, and a gameplay-ready marker incarnation correlated to that exact epoch.
2. Authoritatively reads the selected save through `SongCassetteEnquiries.GetSongCassetteStatus`.
3. Leaves `HAVE_IN_BAG` and `HAVE_DEPOSITED` unchanged and satisfies them only for the current epoch.
4. Leaves unreadable or unknown state pending and fails closed.
5. For `INVALID` or `HAVE_NOT_EARNED`, constructs the public parameterless `RecordSongCassetteStatusInSaveDataRequest`, assigns only its public `Song` and `CassetteStatus=HAVE_IN_BAG` properties, and verifies those values plus an absent public `Bundle` readback. The native request processor's proven absent-bundle fallback supplies `DEFAULT`.
6. Invokes the matching `PlayerSaveRequestProcessor.ProcessRequest(...)` overload through the compatible captured processor, or the proven stateless processor fallback only after selected-save enquiries are readable.
7. Treats a successful invocation as `verification-pending`, never as persisted or satisfied.
8. Re-reads once after a bounded delay. Only authoritative `HAVE_IN_BAG` or `HAVE_DEPOSITED` satisfies the current epoch. A cassette receives at most one semantic grant attempt per epoch; another lifecycle observation cannot duplicate it.

Live close-and-relaunch testing disproved the earlier ordinary-lifecycle assumption. After the gameplay-ready gate opened, all four mod grants verified as `HAVE_IN_BAG` on the exact selected/player pointer and appeared in the UI bag, while the public state still reported `HasUnstagedChanges=true`. A normal close did not persist those changes; the next launch loaded all four as unearned and reconciliation granted them again. The semantic request therefore proves an in-memory `DEFAULT` bundle change only, not disk durability.

Before the live pointer-bound acceptance, cassette reconciliation did not construct or submit `PersistSaveChangeBundleRequest`, `PersistAllSaveChangeBundlesRequest`, `TriggerUrgentSaveWriteIfAnyChangesRequest`, or `RequestWriteForPlayerSave`; automatic production behavior ran no synthetic disk-write transaction. The client never called a private save manager or flush method. A bounded ownership snapshot ran once after a delayed verification wave newly verified one or more mod-granted `HAVE_IN_BAG` songs. It recorded the authoritative selected/player identity, registered and retained SaveData processor identities, the active-slot and numeric-selected dictionary entries, public state/bundle/write metadata, and cassette/UI evidence.

The diagnostic decision rule is observational only:

- If `RegularPlayerSaves[SelectedPlayerSaveSlot]` exists and its pointer matches the active epoch pointer, the narrow guarded request route is a candidate for a separately approved implementation.
- If only `RegularPlayerSaves[activeEpochSlot]` matches the active pointer, a pointer-bound public-state route is the candidate for separate review.
- If neither entry matches, no write route is considered safe.

The active epoch entry is looked up before the numeric selected-slot entry so a missing or empty global selection cannot hide positive manager ownership evidence. None of these diagnostic outcomes authorizes a write by itself.

The exact cassette request mechanism has live evidence: the parameterless request with explicit `Song`/`CassetteStatus` and absent `Bundle` reaches the native RecordSong processor as effective `DEFAULT`, while reflected nullable bundle construction produced invalid payloads. Safe loaded-save timing and epoch-scoped verification were established before the durable promotion boundary was tested separately.

### Historical explicit pointer-bound persistence acceptance trial

This section records the one-shot build used to establish the public persistence boundary before automatic production orchestration was approved. `Developer.EnableCassettePointerBoundPersistenceAcceptance` defaults to `false`; in the final implementation it controls only startup installation of the optional write-completed-event diagnostic hook. It does not enable, disable, cancel, or admit production durability. In the historical trial build, explicit startup opt-in admitted at most one newly verified mod-grant wave per epoch, and only on the Unity thread after all of these public facts were freshly proven: compatible cassette routing; live gameplay-ready identity; current generation/epoch/slot/pointer; valid selected-save enquiry with the exact active pointer; registered Persist processor identity equal to the retained/joined SaveData processor; `RegularPlayerSaves[active slot]` equal to the active player-state pointer; the diagnostic decision exactly `pointer-bound-public-state-candidate`; a non-empty `DEFAULT` bundle; retained `HAVE_IN_BAG` statuses; `HasUnstagedChanges=true`; `HasChanges=true`; and no write already required. The aggregate target reader could be partially false only for the live-proven `target-selected-entry-missing` shape after all mandatory expected-slot evidence had been captured; every other unreadable or malformed target shape rejected the trial.

Before mutation the client resolves the exact public method owners/signatures and installs an immutable attempt token plus write/redundancy/status baseline. It then invokes, on the same exact `PlayerSaveFileState`, `PersistAllChangesInBundle(DEFAULT)` followed by inherited public `BaseSaveFileState.RequestUrgentWriteToDisk()`. It does not construct a generic Persist request, submit through `RequestSystem`, call a private writer/manager, mutate selection, persist other bundles, or retry after either call may have executed. If promotion returns but urgency throws, the epoch is indeterminate and tombstoned.

The opt-in event hook treats `PlayerSaveWriteCompletedEvent` only as an uncorrelated same-slot wake hint. Native events expose no attempt token, so neither a success nor a failure event can verify, fail, or tombstone the current attempt; an old same-slot event may otherwise be mistaken for a fresh epoch's write. Terminal success and failure come only from fresh public-state polling against the immutable PRE baseline. Success requires exact identity/ownership and retained cassette statuses; `HasUnstagedChanges=false`, `RequiresWriteToDisk=false`, and `HasChanges=false`; no advanced failure time/reason; and either an advanced public success timestamp or redundancy index/revision. An advanced public failure time/reason, status regression, unreadable state, or identity mismatch fails closed. A 130-second active-update watchdog bounds the trial. Save boundary, deactivation, or config disable cancels the logical attempt; late and wrong-slot events cannot reopen it.

Every acceptance transition and its immutable marker admission occur together while holding the acceptance state lock. Markers enter one attempt-keyed journal with a global monotonic sequence; a terminal admission seals that attempt, and an epoch reset retires its identity without discarding already queued records. Callers never emit their own copied marker. After releasing the state lock they invoke one serialized drain, which dequeues under the state lock and physically emits every pending record in journal order. Therefore an `INVOKED` admitted before a concurrent config cancellation is always logged before `CANCELLED`, an `EVENT` admitted before a polling terminal is always logged before that terminal, and an old terminal can never appear after a fresh epoch's markers. The adapter invocation remains outside both the state lock and serialized emitter so a synchronous native callback cannot deadlock. Logs are bounded to `PRE`, `INVOKED`, `IMMEDIATE POST`, `EVENT`, and one terminal `VERIFIED`, `FAILED`, `TIMEOUT`, or `CANCELLED` marker; every record includes its global sequence and exact attempt identity. `INVOKED` is admitted only after the prepared token remains active and `MarkInvoked` succeeds.

The decisive live trial used seed archive `AP_54469213998348218286.zip`, server `127.0.0.1:38282`, player `Jack`, and UI save slot 4. Four newly verified AP grants (`BADASS`, `HEAVY_METAL`, `KEEP_ON_HUSTLIN`, and `ON_THE_WAY`) produced an exact pointer-bound PRE baseline with `HasUnstagedChanges=true`, `HasChanges=true`, `RequiresWriteToDisk=false`, redundancy index/revision `0/16`, and the known `target-selected-entry-missing` ownership shape. After the public promotion/urgent-write calls, the same pointer reached `HasUnstagedChanges=false`, `HasChanges=false`, `RequiresWriteToDisk=false`, and redundancy `1/17`, producing terminal `VERIFIED`. A normal menu close and full restart then loaded all four as `HAVE_IN_BAG` from slot 4 with zero semantic regrant submissions and zero second persistence attempt. This proves durable disk persistence through the pointer-bound public-state route.

### Production pointer-bound persistence

Production cassette durability uses the same exact public-state route without a developer opt-in and without the uncorrelated write-completed event hook. It is active only when full cassette routing is compatible. Each newly verified mod-grant wave is queued against its immutable generation/epoch/slot/pointer identity, and a write may begin only after every acceptance eligibility fact is freshly reproved. Pre-invocation unreadable or temporarily unavailable evidence defers the queued wave without mutation. The client still never constructs a generic Persist request, mutates selected-slot globals, calls private save managers/writers, wraps a raw pointer as a save object, persists a non-`DEFAULT` bundle, or treats invocation return as success.

The final orchestration type is `CassetteProductionPersistenceCoordinator`. It owns the identity-bound `CassettePersistenceWaveQueue`, a `CassettePointerBoundPersistenceRuntime` configured with `CassettePersistenceAttemptPolicy.SequentialVerifiedBatches`, and the marker journal. `Plugin.cs` delegates adapter admission, terminal observation, wave completion/tombstoning, reconciliation callbacks, and marker dequeueing to that pure coordinator instead of duplicating those transitions. The coordinator installs PRE before invoking the injected adapter callback, while the public native call itself remains outside the coordinator lock so synchronous callbacks cannot deadlock.

Only one semantic-grant/write transaction may be active at a time. While a production write is active, additional AP cassette ownership remains pending and no additional native cassette grant request is submitted; reconciliation resumes after the active write reaches verified terminal state. Multiple songs already verified before the write starts are coalesced into one sorted, deduplicated batch. A successful verified batch allows a later batch in the same unchanged save epoch. Any possibly mutating invocation exception, public-state failure, identity/ownership/status regression, or timeout tombstones production persistence for the rest of that epoch; no second disk-write attempt is made until a new confirmed load creates a new epoch. AP ownership remains process-wide, so the next load can repair any missing cassette safely.

Production terminal decisions come only from fresh public-state polling against the immutable PRE baseline. The success predicate remains exact: same generation/epoch/slot/pointer and ownership; every batch cassette remains `HAVE_IN_BAG`; `HasUnstagedChanges=false`; `HasChanges=false`; `RequiresWriteToDisk=false`; no advanced failure; and an advanced public success time or redundancy index/revision. A 130-second active-update watchdog remains the hard bound. Save boundary/deactivation cancels the logical attempt, retires the old batch identity, and prevents stale work or logs from entering a new epoch.

Every coordinator transition compares the complete generation/epoch/slot/pointer identity under the coordinator lock. Re-observing the same identity is a no-op that preserves its active wave. A stale cancellation for epoch A cannot cancel, clear, relabel, or emit `CANCELLED` for a newer queued or active epoch B; B remains single-admission and can still reach polling VERIFIED. This exact-identity rule supplements the globally ordered marker journal and prevents delayed boundary callbacks from retiring newer work.

The acceptance-only configuration key may remain as default-false diagnostic compatibility while production behavior is independent of it. Normal production logging uses the same attempt-keyed, globally sequenced journal and serialized emitter under a `CASSETTE PERSISTENCE` prefix. It records PRE, INVOKED, IMMEDIATE POST, and exactly one terminal VERIFIED, FAILED, TIMEOUT, or CANCELLED marker. Event markers are absent in normal production because the native event is not correlated to an attempt and is not required for polling.

The automatic production path was accepted live with deterministic seed `91001`, archive `AP_03679412317094840404.zip`, server `127.0.0.1:38282`, player `Jack`, and a fresh UI save slot 4. `HEAVY_METAL` produced production attempt 1 on redundancy `2/3` and reached polling `VERIFIED` at `3/4`. Without changing generation, epoch, slot, or pointer, `ON_THE_WAY` then produced production attempt 2 on `3/4` and reached polling `VERIFIED` at `4/5`. The developer acceptance switch remained false, and normal production emitted no event-hint markers. After a normal close and clean relaunch, both songs loaded as `HAVE_IN_BAG` during Music Lab gameplay readiness with zero semantic grant submissions, zero persistence starts, zero persistence terminal markers, and zero errors. This proves both sequential same-epoch durability and clean-load idempotence in the installed production build.

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
17. Default production wiring contains no selected-slot event hook, synthetic Persist request construction/submission, private save flush/save-manager API, forced deposit/unlock, or process-wide satisfaction cache.
18. The acceptance switch defaults false; only an explicit startup opt-in installs the write-completed event observer, and the switch never enables, disables, cancels, or admits production persistence.
19. Every acceptance gate rejects independently; the exact public invocation order is promotion then urgency on one pointer-validated state, with the token installed before either call.
20. Native success and failure events are uncorrelated non-terminal wake hints only; status/identity/advanced-failure/unreadable regressions fail closed through polling, a 130-second watchdog tombstones, and save switches cancel without allowing old same-slot events to fail a fresh attempt.
21. A relaunch where the cassette is already persisted produces no newly verified mod-grant wave and therefore no production attempt. A relaunch where it is missing may produce one fresh production wave in the new epoch after every automatic-production gate is true.
22. Deterministic interleavings preserve physical marker order for `INVOKED` then cancellation, `EVENT` then polling terminal, and old terminal then reset/fresh epoch; terminal markers seal their attempt and no record is duplicated, skipped, or relabeled.
23. Production persistence is enabled by compatible cassette routing, not by the default-false developer acceptance key; with that key false, normal production installs no write-completed event hook.
24. A newly verified wave that encounters a transient pre-invocation gate failure remains queued and performs no mutation; it may start later only after every gate is freshly true.
25. While a production write is active, later AP cassette receipts cannot submit another native grant; after VERIFIED they resume and may form one later batch in the same epoch.
26. Two verified batches in one unchanged epoch produce two sequential pointer-bound attempts, while an indeterminate, failed, or timed-out attempt tombstones the epoch and rejects every later batch.
27. A save boundary cancels the active attempt, clears queued epoch-local waves, and prevents old markers or work from appearing after the new epoch begins.
28. A normal clean reload of already persisted cassettes performs zero semantic grants and zero persistence attempts.
29. Re-observing the exact current coordinator identity preserves its active wave, while stale cancellation for an older full identity cannot cancel or clear a newer queued/active wave, emit its `CANCELLED` marker, or cause a duplicate adapter admission.

An earlier semantic-route acceptance used fresh seed `8302601` at `127.0.0.1:38282`, slot `Jack`, with UI save slot 4. It is retained as historical lifecycle coverage, not as the decisive four-song pointer-bound durability trial:

- no cassette submission occurs while the validity enquiry is false or the selected-state pointer differs from the activated epoch, regardless of the numeric selected-slot value;
- an epoch for native slot 4 is logged after its selected `(slot, pointer)` stabilizes;
- AP history restores Badass and The Heist into the loaded save;
- both appear in the top-right inventory;
- a full restart before insertion restores both as `HAVE_IN_BAG` without duplicate requests or replayed checks;
- normal machine insertion unlocks their corresponding Music Lab levels;
- a second full restart reports them `HAVE_DEPOSITED` and does not return them;
- no repeated receipt loop or crash occurs.

## Acceptance boundary

The feature branch has automated regression coverage and the single decisive four-song live durability trial recorded above. That evidence is deliberately narrower than broad gameplay or release coverage. This documentation task does not install or launch the client and does not authorize merging, pushing, publishing, or releasing the feature branch; any further deployment requires separate approval.
