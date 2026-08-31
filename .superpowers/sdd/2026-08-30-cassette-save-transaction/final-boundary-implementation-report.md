# Final Loaded-Save Boundary Implementation Report

## Scope

Implemented the approved exact final loaded-save boundary. Broad selection requests no longer activate cassette save epochs. The client now observes exact `SaveDataRequestProcessor` selection, creation, and state-build mutations, queues managed-only boundary signals, and stabilizes the selected slot plus public native save-state pointer on the Unity keeper before activation.

## RED evidence

The first focused run failed to compile because `CassetteSaveIdentityStabilizer`, `CassetteSaveActivation`, and `CassetteSaveBoundarySignalKind` did not exist. After the pure stabilizer was added, the focused suite failed at the production-wiring assertion because the exact `ChangeSelectedPlayerSaveSlot(Int32)` hook was absent.

A later regression test exposed stale Build coalescing across completed generations:

```text
duplicate selection does not create another epoch:
expected null, got CassetteSaveActivation { Slot = 4, Pointer = 400,
Generation = 4, Reason = Selection, IncludesBuild = True }
```

This proved a consumed Build marker could incorrectly leak into a later non-Build generation.

## GREEN implementation

- Added exact postfix hooks for:
  - `SaveDataRequestProcessor.ChangeSelectedPlayerSaveSlot(Int32)`
  - `SaveDataRequestProcessor.CreateNewPlayerSaveFileInEmptySlot(Int32)`
  - `SaveDataRequestProcessor.ProcessRequest(BuildPlayerSaveStateFromFileRequest)`
- Exact callbacks only extract the expected slot and queue `Selection`, `Creation`, or `Build` signals.
- Every signal immediately marks the boundary pending, clears queued old-epoch reconciliation, invalidates the candidate, and resets its observation count.
- Same-slot pending signals preserve Build inclusion; a completed generation cannot leak Build state into a later generation.
- The Unity keeper reads `GetSelectedSaveFileSlotNumber()`, `TryGetSelectedSlotSaveFileState()`, and the public native `Pointer`, then requires two consecutive matching observations after the newest signal.
- Slot or pointer changes activate a new epoch. A pending Build generation activates one same-slot reload epoch even when the pointer is reused. Stable duplicate non-Build signals resume the existing epoch without duplicating it.
- Pending boundaries fail closed before processor fallback, cassette reads, submissions, runtime timers, or delayed verification.
- Room/lifecycle reconciliation notifications never signal or activate an epoch.

## Automated verification

- Focused cassette regression suite: passed.
- All client regression projects: `17/17` passed in Release configuration.
- No-install v0.68 Release build: succeeded with zero errors; the seven pre-existing nullable warnings remain.
- Repository validator: passed for client v0.68/APWorld v0.22.
- `git diff --check`: passed.

## Live acceptance still required

Automated verification cannot prove the exact UI slot-4 runtime call sequence. Live acceptance remains required on seed `8302601`: observe a slot-4 exact signal and stabilized epoch before cassette submission, restart before insertion, then restart after normal machine deposit. No release or installation was performed.

## Review fix: bounded argument diagnostics

Review identified that both exact callback families failed closed silently when Harmony/IL2CPP argument extraction differed from the expected generated shape. A source-wiring regression was added first and failed because no diagnostic branch existed. The callbacks now emit a warning once per method/request identity and failure reason for null arguments, wrong argument counts, non-`Int32` slot values, missing Build requests, and unreadable `BuildPlayerSaveStateFromFileRequest.SlotNumber`.

The diagnostics inspect only safe method identity, argument count/runtime type name, the known request identity, and the known `SlotNumber` member. They do not query selected save state, reconcile cassettes, submit requests, or traverse arbitrary object graphs. Focused tests and all 17 client regression projects passed after the fix; the final build/validator evidence is recorded with the fix commit.

## Live diagnosis instrumentation: identity stabilization stages

The first live final-boundary run queued slot-4 Creation but produced no activation, while the existing adapter collapsed identity-read failures and selected-slot mismatches into silence. Test-first, behavior-neutral instrumentation now reports bounded identity stages:

- owner/slot-method/state-method unavailable;
- selected slot empty or conversion failure;
- selected state null;
- Pointer property missing, wrong type, zero, or invocation failure;
- successful expected/actual slot and pointer observation classified as mismatch, first stable, activated, or stable duplicate.

The keeper logs only when this compact diagnostic state changes, preventing per-frame spam while retaining the first and second stabilization transitions. Exception summaries are reduced to base exception type plus a single-line message capped at 160 characters. No save object enumeration or arbitrary traversal was added, and the identity result continues through the same fail-closed stabilizer path without changing activation or reconciliation behavior.

RED evidence was the missing `ExpectedSlot`, `ObserveDetailed`, observation-kind contract, three-output adapter overload, and keeper diagnostic wiring. GREEN evidence: focused cassette suite passed, all 17 client projects passed, and the final build/validator/diff gate passed. A live rerun is required to identify the previously hidden stage.

## Existing-save public request-path diagnostics

The next live run showed that loading an existing slot emitted no authoritative exact signal at all, so identity stabilization never started. Test-first diagnostic-only postfixes were added for the exact generated overloads:

- `SaveDataRequestProcessor.ProcessRequest(SelectPlayerSaveSlotRequest)` — logs public `SlotNumber`;
- `ProcessRequest(SelectMostRecentlyUsedRegularPlayerSaveSlotRequest)` — identity only;
- `ProcessRequest(EnsureAPlayerSaveSlotIsSelectedRequest)` — logs public `DefaultSlotNumber`;
- `ProcessRequest(EnsurePlayerSaveFileExistsInSelectedSlotRequest)` — identity only.

The initial source-wiring test failed because the exact public selection hooks were absent. The implementation logs once per request identity and safe scalar detail, and logs bounded argument rejection when the request or documented slot member is unavailable. These callbacks do not emit boundary signals, suspend an epoch, query selected save state, activate/deactivate, reconcile, or submit native requests. Focused and 17/17 client tests passed; a live reload is required to identify which public request actually represents existing-save selection before any authority is revised.

## Selected-slot setter diagnostic

Live public-request instrumentation showed only `SelectMostRecentlyUsedRegularPlayerSaveSlotRequest` during existing slot-4 reload; no explicit-slot public request or existing authoritative signal fired. A test-first, diagnostic-only postfix was therefore added for the exact generated setter:

```text
SaveDataState.set_SelectedPlayerSaveSlot(Il2CppSystem.Nullable<Int32>)
```

The initial source test failed because the exact setter hook was absent. The postfix safely unwraps only its sole documented nullable value and logs `slot=<empty>` or the numeric slot once per value. Null/wrong-count/conversion failures use the existing bounded rejection logger. It does not emit a boundary signal, suspend or activate an epoch, query selected save state, reconcile, or write. Focused and 17/17 client tests passed; live reload must now establish whether the setter wrapper is crossed and where its value occurs relative to the most-recent request.

## Most-recent Unity identity probe

Live testing established that existing-save reload crosses only the public `SelectMostRecentlyUsedRegularPlayerSaveSlotRequest` postfix; neither the selected-slot setter nor the previously instrumented exact boundary paths execute. A test-first, behavior-neutral probe now queues a managed flag from that postfix and performs the existing safe identity read on the Unity keeper. The probe reports its first slot/pointer observation and, at most, one confirming observation; an unreadable identity clears immediately. Re-queueing replaces the previous diagnostic generation.

The pure probe result cannot contain a `CassetteSaveActivation`, and source guards prohibit the queue callback from signaling the authoritative stabilizer, activating/deactivating an epoch, reading a save, reconciling, or submitting a grant. Its Unity consumer only calls `TryGetLoadedSaveIdentity`, advances the bounded diagnostic state, and logs the stage plus safe scalar identity. It does not suspend or activate an epoch and cannot initiate cassette reads or writes.

RED evidence: the focused project initially failed to compile because `CassetteMostRecentIdentityProbe` and its result kinds did not exist. GREEN evidence: the focused cassette suite passed, including `most_recent_identity_probe_is_bounded_and_non_authoritative`; all 17 client regression projects passed. The no-install build, repository validator, and diff gate are recorded below after final verification. A live existing-slot reload remains necessary to reveal whether the next Unity update observes the final slot-4 identity or a startup identity.

## Most-recent public-state fingerprint

The bounded Unity probe now augments each of its at-most-two observations with a fixed public-state fingerprint: runtime type, `GameStats.PlayTimeInSeconds`, `GameStats.LastPlayDateTimeUtc.Ticks`, and the `I_GOT_MONEY` and `BADASS` cassette statuses. Each known-member read has an explicit bounded failure stage. The adapter does not enumerate members, traverse collections, submit requests, persist, or write; the probe remains incapable of activation and clears after one failed read or two identity observations.

RED evidence: the focused project failed to compile because `CassetteSaveFingerprint` and `TryGetLoadedSaveFingerprint` did not exist. GREEN evidence: adapter characterization verifies all five fingerprint values plus a precise missing-GameStats failure, the source guard proves the probe block does not signal or submit, the focused cassette suite passed, and all 17 client projects passed. Live comparison against UI slot 4 and a distinct save remains required to decide whether the public state tracks the playable file despite the contradictory slot-0 scalar.

### Review fix: public-only fingerprint contract

Review found that the initial fingerprint used the adapter's broad reflection flags and could bind non-public replacement members if a public generated contract changed. A failing regression fake exposed only private `GameStats` and cassette members; the adapter incorrectly accepted it. Fingerprint reflection now uses dedicated `Public | Static` and `Public | Instance` flags for the enquiry, GameStats, playtime, last-play ticks, and cassette-status method. The private-member fake now fails closed at `fingerprint-game-stats-missing`, and a source guard rejects broad reflection flags inside the fingerprint method. No other adapter behavior changed.

### Review fix: public-only nullable unwrapping

A second review found that the fingerprint still called the adapter's legacy broad nullable helper, whose `HasValue` and `Value` lookup includes non-public members. RED fakes with private nullable-shaped state and cassette-status wrappers were incorrectly accepted. The fingerprint now uses a dedicated `UnwrapNullablePublic` helper for both returned state and cassette status. Missing or private `HasValue`/`Value` fails closed; the legacy helper remains unchanged for unrelated adapter paths. Focused and full regression tests cover both private-wrapper cases.

## Processor-local selected-save fingerprint

Live A/B evidence proved the static most-recent enquiry fingerprint remains the same preliminary slot-0 state when the user selects UI slot 3 or slot 4. The same log also proved the exact UI selection callback supplies slot 3 and is followed by a compatible `PlayerSaveRequestProcessor` capture. A new diagnostic-only state machine now starts a generation on exact `Selection`, invalidates every earlier diagnostic processor, and arms only when a later processor callback occurs. Its Unity keeper reads `ObtainState()` from that captured processor and logs the returned public pointer/type, playtime, last-play ticks, `I_GOT_MONEY`, and `BADASS` statuses for at most two observations.

The diagnostic result cannot produce a save activation. A newer selection rejects stale-generation observations and requires another processor capture. It does not feed the authoritative stabilizer, activate an epoch, initiate reconciliation, submit a request, mutate state, or write. Fingerprint fields and nullable values remain public-only; the existing proven `ObtainState()` reflection is used solely to obtain the processor-local state.

RED evidence: the focused project failed to compile because the processor probe and adapter method did not exist. GREEN evidence: tests prove pre-selection capture rejection, post-selection arming, two-observation boundedness, no activation, newer-generation invalidation, stale-observation rejection, and fixed processor-local fingerprint values. Focused and all 17 client regression projects passed. Live slot-3/slot-4 comparison remains required before this processor identity can be proposed as authoritative.

### Review fix: public-only processor state acquisition

Review found the diagnostic processor fingerprint looked up `ObtainState` with the adapter's broad instance flags. A private-only `PlayerSaveRequestProcessor` fixture demonstrated the diagnostic would invoke a non-public method. The lookup now uses `Public | Instance`; the private fixture fails closed at `processor-fingerprint-obtain-state-missing`, and a source guard prevents `AllInstance` from returning to `TryGetProcessorSaveFingerprint`. All other processor and fingerprint behavior is unchanged.

## Preexisting-processor selection discriminator

Live slot-4 selection produced no later player-save processor callback, while slot 3 did. A second, independently labeled diagnostic probe now handles this asymmetry. On every exact Selection it invalidates its prior generation; if and only if a compatible processor is already captured at that boundary, it immediately arms against that retained instance. The Unity keeper logs at most two fingerprint observations with `source='preexisting-processor'`. The original later-callback path remains separate and logs `source='post-selection-capture'`.

Both probes reject stale-generation observations and return no activation. The shared diagnostic helper is source-guarded against stabilizer access, epoch activation, reconciliation, semantic submission, or writes. RED evidence was the absence of the preexisting probe wiring. GREEN tests cover no-preexisting behavior, selection-time arming, distinct source labels, two-observation boundedness, generation invalidation, stale-result rejection, and non-authoritative results. Focused and all 17 client projects passed; live slot-4 comparison remains required.

### Review fix: atomic processor-probe snapshots

Review found that the Unity keeper previously read each probe and processor field before the diagnostic helper acquired `Sync`. A replacement or newer selection could therefore pair a stale/null processor with the current pending generation, consume it as `ReadFailure`, or allow a detached probe to complete after replacement. RED evidence: the focused project failed to compile after adding a deterministic regression because `CassetteProcessorIdentityProbeSnapshot` did not yet exist.

The keeper now captures probe reference, generation, pending state, and paired processor together under `Sync`. After the public-only fingerprint read, it reacquires `Sync` and rejects the result unless both the probe reference and generation still match the current labeled probe. The deterministic regression proves replacement and newer-generation snapshots cannot consume or clear current state. GREEN evidence: the focused cassette suite passed, including `processor_probe_snapshot_is_atomic_and_replacement_safe`; all 17 client projects passed; the no-install Release build completed with zero errors (seven pre-existing nullable warnings).

## Approved processor-local loaded-save authority

The live A/B diagnostics proved the static selected-save enquiry remains on a preliminary slot-0 state while the UI selection and the captured `PlayerSaveRequestProcessor.ObtainState()` distinguish actual saves. The authoritative stabilizer now begins only from the exact UI `Selection` signal and binds that explicit slot to either the compatible long-lived processor already captured at selection or a compatible processor captured afterward. The Unity keeper reads only the public processor-local state pointer and requires the same non-zero pointer in two consecutive observations for the same signal generation and processor reference before activating an epoch.

Missing processors, failed reads, zero pointers, pointer changes, wrong processor references, and stale generations remain suspended and cannot activate or reconcile. A pointer change resets the two-observation count. The old static enquiry and temporary fingerprint probes no longer participate in Unity authority; MostRecent remains explicitly logged as non-authoritative. Cassette read-before-write, semantic `HAVE_IN_BAG` request construction, native persistence, and the 250ms/1s/3s delayed authoritative verification path are unchanged.

RED evidence: the focused project initially failed to compile because `CassetteProcessorSaveIdentityStabilizer` did not exist. GREEN evidence includes deterministic slot-3 to slot-4 distinct pointer/epoch coverage, preexisting and post-selection processor binding, stale-generation rejection, pointer-change reset, failed-read suspension, and no first-observation activation. The focused cassette suite passed; all 17 client projects passed; the no-install Release build passed with zero errors and seven pre-existing nullable warnings; repository validation and `git diff --check` passed.

### Review fix: interrupted confirmations reset stability

Review found that an invalid observation returned without clearing the first pointer sample, allowing two nonconsecutive valid samples to activate. RED evidence reproduced this with a valid pointer followed by a failed/zero read: the next matching pointer incorrectly activated immediately. The stabilizer now clears its candidate pointer and matching count for every invalid observation while keeping the selection pending. Deterministic tests cover failed/zero reads, wrong processor references, and stale generations between matching samples; each requires two new consecutive valid observations afterward. Pointer-change reset behavior is preserved. Focused and all 17 client suites passed, the no-install Release build passed with zero errors, and repository validation plus `git diff --check` passed.

## Diagnostic-only regular-save pointer join

Same-slot live selection showed that MostRecent may be the only request when the startup-selected slot is reselected. A bounded, behavior-neutral diagnostic now queues the exact `SaveDataRequestProcessor` instance from the MostRecent postfix. On Unity, after a compatible player-save processor is available, it obtains the public `SaveDataState.RegularPlayerSaves` entries and compares each public `PlayerSaveFileState.Pointer` with the public `PlayerSaveRequestProcessor.ObtainState().Pointer`. Exactly one match reports its real UI slot plus bounded `(slot, pointer, LastPlayDateTimeUtc.Ticks)` fingerprints. Zero matches, duplicates, null entries, key conversion failures, enumeration failures, missing contracts, and pointer failures report explicit stages and fail closed.

The diagnostic state snapshots its generation and both processor references under `Sync`; a newer queue rejects stale results, and a current result consumes once. Its result type cannot contain an activation, and source guards prohibit signalling, epoch activation, reconciliation, or cassette submission. Reflection is public-only and enumeration is capped at 32 entries. RED evidence was compile failure before `TryMatchRegularSaveSlot`, its entry result, and bounded probe existed. GREEN tests cover unique slot 3 and slot 4 matches, zero/duplicate/null/conversion/enumeration failures, generation replacement, single consumption, and non-authoritative wiring. Focused and all 17 client suites passed, the no-install Release build passed with zero errors, repository validation passed, and `git diff --check` passed.

### Review fixes: strict member contracts and bounded log signatures

Review found that absent `Key`, `GameStats`, `LastPlayDateTimeUtc`, or `Ticks` members could flow as null into `Convert`, which yields zero for null instead of failing closed. RED coverage demonstrated a missing Key incorrectly producing a successful slot-0 join. The adapter now validates every public property and raw value before conversion, with exact missing/null stages for key, game stats, last-play date, and ticks. Tests cover all eight missing/null variants plus the existing conversion and enumeration failures.

Repeated MostRecent calls could also emit identical queue and result logs indefinitely. Queue notices are now emitted once per configured runtime, and result logs pass through a managed signature deduplicator containing outcome, matched slot, stage, and complete bounded fingerprints. Identical successes and failures are suppressed; changed slot, fingerprint, or outcome logs again. Configure resets both bounds. The diagnostic remains non-authoritative. Focused and all 17 client suites passed, the no-install Release build passed with zero errors, repository validation passed, and `git diff --check` passed.

## Approved MostRecent startup/no-op authority

The public pointer join now closes the startup case where selecting the already-current most-recent UI slot is a no-op and emits no exact Selection. The MostRecent postfix immediately creates an unresolved stabilizer generation, clears reconciliation intent, and suspends the prior epoch without trusting the stale static selected-slot enquiry. On Unity, the exact `SaveDataRequestProcessor` and compatible captured `PlayerSaveRequestProcessor` perform the strict public RegularPlayerSaves pointer join. Only one unique match resolves the real UI slot into the existing processor-local stabilizer. Resolution is not activation: the same player-state pointer must then be observed in two consecutive Unity updates for the same generation and processor reference.

Zero/duplicate matches and all null, contract, conversion, enumeration, processor, or pointer failures leave the unresolved boundary suspended. A newer MostRecent generation rejects stale snapshots. Exact Selection cancels the pending MostRecent generation and directly supersedes it. The stabilizer's existing failed-read, wrong-processor, stale-generation, and pointer-change interruption resets remain enforced. Temporary diagnostic-only result activation and signature plumbing were removed; retained logs report only boundary queued, uniquely resolved/awaiting confirmation, unresolved failure, stabilization changes, and activation.

RED evidence: focused tests initially failed because unresolved suspension and pointer-join cancellation did not exist. GREEN coverage proves startup slot 4 activates only after a unique join plus two stable samples; slot 3 pointer matching; immediate prior-epoch suspension; exact Selection cancellation; zero-match unresolved state; stale join rejection; and pointer interruption reset. Focused and all 17 client suites passed, the no-install Release build passed with zero errors, repository validation passed, and `git diff --check` passed.

### Review fix: fail-closed MostRecent entry and diagnostic cleanup

The exact MostRecent hook is now a prefix whose first synchronized action creates an unresolved processor-save generation. It cancels any older pointer join and clears reconciliation intent before checking whether the Harmony instance is null or has the expected `SaveDataRequestProcessor` runtime type. Consequently null, wrong-type, and extraction/ownership failure paths retain prior epoch data but leave it suspended: none can allow native cassette reads, reconciliation, verification, or writes against the old save. Source-order coverage and active-prior-epoch regressions enforce this entry contract.

All temporary public-selection, selected-slot-setter, identity-probe, and fingerprint diagnostics have been removed from hook registration, runtime policy, adapter code, and tests. Production retains only exact Selection/Creation/Build signals, the authoritative MostRecent pointer join, processor-local two-sample stabilization, and concise boundary logs. Queue and pointer-join result messages use independent signature deduplicators: identical repeated stages/results are suppressed, while a changed stage, slot, outcome, or bounded entry fingerprint logs again.

GREEN evidence: the focused cassette suite passed; all 17 client regression projects passed in Release; the no-restore Release client build succeeded with 0 errors and the same 7 pre-existing nullable warnings; repository validation passed for client v0.68.0/APWorld v0.22.0; and `git diff --check` passed.
