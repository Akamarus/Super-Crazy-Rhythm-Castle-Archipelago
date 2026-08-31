# Cassette Loaded-Save Reconciliation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist every AP-owned cassette into the confirmed loaded save through the exact native semantic player-save request, with epoch-scoped authoritative verification and bounded retries.

**Architecture:** AP ownership remains session-wide, while native satisfaction and outstanding attempts belong only to a monotonically increasing loaded-save epoch. Exact save-state mutation postfixes enqueue expected-slot generations; the Unity keeper requires a matching non-null selected `(slot, PlayerSaveFilePublicState.Pointer)` to remain stable for two updates before activating an epoch. After that epoch exists, Unity-thread lifecycle points read before writing, submit `RecordSongCassetteStatusInSaveDataRequest(song, HAVE_IN_BAG, DEFAULT)` through a compatible `PlayerSaveRequestProcessor`, and verify later; request return alone is never satisfaction.

**Tech Stack:** C#/.NET 6, BepInEx 6 IL2CPP, Harmony, generated-interop reflection adapters, console regression projects, PowerShell build/validation scripts.

**Spec:** `docs/superpowers/specs/2026-08-30-cassette-save-transaction-design.md`

## Global Constraints

- Do not change cassette placement, sources, medal logic, or normal cassette-machine insertion.
- Do not automatically deposit cassettes or directly unlock Music Lab levels.
- Do not call private save-manager/flush APIs or synthesize a persist request.
- Do not patch `SelectedPlayerSaveSlotChangedEvent.HandleEvent`.
- Do not rely on `PersistSaveChangeBundleRequest` or `PersistAllSaveChangeBundlesRequest`; live diagnostics observed no prefix entry for load, travel, title return, difficulty change, shutdown, or the durable Plant Pipes grant.
- Broad selection-request postfixes are not authoritative and must not activate an epoch.
- Exact callbacks for `SaveDataRequestProcessor.ChangeSelectedPlayerSaveSlot(Int32)`, `CreateNewPlayerSaveFileInEmptySlot(Int32)`, and `ProcessRequest(BuildPlayerSaveStateFromFileRequest)` enqueue only; they perform no native cassette read or write.
- Every exact signal immediately suspends reconciliation and delayed verification for the prior active epoch. Native work remains fail-closed until a new candidate activates.
- Begin an epoch only after the Unity keeper observes the signal's expected slot plus a non-null selected `PlayerSaveFilePublicState.Pointer` unchanged for two consecutive updates.
- Every newer exact signal, including same-slot, advances the pending generation and resets stability to zero; both matching observations must occur afterward.
- Slot change, pointer change, or one newly consumed Build generation for the selected slot advances the epoch; same-slot coalescing preserves Build inclusion and each coalesced generation activates at most once.
- Room transitions never establish or replace an epoch.
- Keep AP ownership session-wide, but never keep satisfaction process-wide. Bag/deposited status is terminal only in the epoch that authoritatively observed it.
- Read before every write. Preserve `HAVE_IN_BAG` and `HAVE_DEPOSITED`.
- Submit only `RecordSongCassetteStatusInSaveDataRequest(song, HAVE_IN_BAG, DEFAULT)` through a compatible `PlayerSaveRequestProcessor` on the Unity thread.
- Treat request return as verification-pending. Require a later authoritative read in the same epoch.
- Retry only at safe lifecycle points and through a bounded timer.
- APWorld stays v0.22 and client stays v0.68 for this repair.
- No merge, push, publish, or release is authorized.

---

## File Structure

- Modify `client/CassetteRandomizationPolicy.cs`: retain epoch-scoped read/apply/verify decisions and add pure expected-slot/native-pointer stabilization state.
- Modify `client/CassetteSaveTransactionAdapter.cs`: retain selection and read adapters; expose exact semantic `DEFAULT` request submission; remove natural-persist bundle staging.
- Modify `client/Plugin.cs`: install exact queue-only save mutation hooks, stabilize selected identity in the Unity keeper, reconcile only after activation, and retain the Plant Pipes compatible processor pattern.
- Modify `client/tests/CassetteRandomization/Program.cs`: cover pure behavior, native-shaped request semantics, and production wiring.
- Modify `docs/testing/2026-08-29-full-cassette-acceptance.md` and `docs/TESTING_AND_ISSUES.md`: record two-restart live acceptance.

---

### Task 1: Epoch-Scoped Read/Apply/Verify Runtime

**Files:**
- Modify: `client/CassetteRandomizationPolicy.cs`
- Modify: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Produces: `CassetteSaveEpochRuntime.ActivateSave(int slot)`, `DeactivateSave()`, `Receive(string nativeSong)`, `Evaluate(string nativeSong, string? nativeStatus, bool processorAvailable)`, `RecordSubmission(string nativeSong)`, `RecordSubmissionFailure(string nativeSong)`, `Verify(string nativeSong, string? nativeStatus)`, and `Tick(TimeSpan elapsed)`.
- Produces: `CassetteReconcileDecision` values `Inactive`, `Satisfied`, `WaitForReadableSave`, `WaitForProcessor`, `Submit`, `Verify`, and `RetryExhausted`.

- [ ] **Step 1: Write failing runtime tests**

```csharp
var runtime = new CassetteSaveEpochRuntime();
runtime.Receive("BADASS");
Equal(CassetteReconcileDecision.Inactive,
    runtime.Evaluate("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true),
    "pre-epoch receipt cannot submit");
runtime.ActivateSave(4);
Equal(CassetteReconcileDecision.Submit,
    runtime.Evaluate("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true),
    "confirmed loaded save permits submission");
runtime.RecordSubmission("BADASS");
Equal(CassetteReconcileDecision.Verify,
    runtime.Evaluate("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true),
    "request return is not satisfaction");
runtime.Verify("BADASS", CassetteRandomizationPolicy.HaveInBag);
Equal(false, runtime.IsPending("BADASS"), "later bag read satisfies this epoch");
```

Also test same-slot reload, different-slot switch, deposited preservation, unreadable/unknown state, missing processor, failed submission, bounded retry exhaustion, duplicate receipt, and stale prior-epoch verification.

- [ ] **Step 2: Run focused test and verify RED**

```powershell
dotnet run --project client/tests/CassetteRandomization/CassetteRandomization.Tests.csproj -c Release
```

Expected: FAIL because the runtime still models persist tokens.

- [ ] **Step 3: Implement the minimal runtime**

Keep `_owned` across epochs. Clear satisfaction, attempts, retry counts, and timers on every activate/deactivate. Require active epoch, recognized native status, and compatible processor before `Submit`. Use bounded delays of 250 ms, 1 s, and 3 s; never submit every frame.

- [ ] **Step 4: Run focused test to verify GREEN**

Run Step 2. Expected: every revised runtime case passes.

- [ ] **Step 5: Commit**

```powershell
git add client/CassetteRandomizationPolicy.cs client/tests/CassetteRandomization/Program.cs
git commit -m "fix(client): model post-load cassette reconciliation"
```

---

### Task 2: Exact Semantic Native Adapter

**Files:**
- Modify: `client/CassetteSaveTransactionAdapter.cs`
- Modify: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Produces: `TryGetLoadedSave(out int slot)`, `TryReadCassetteStatus(object processor, string nativeSong, out string? nativeStatus)`, `IsCompatiblePlayerSaveRequestProcessor(object? processor)`, and `TrySubmitHaveInBag(object processor, string nativeSong, out string detail)`.
- Consumes: `CassetteNativeRequestFactory.TryCreateHaveInBagRequest(...)` and `PlayerSaveManagementEnquiries`.

- [ ] **Step 1: Write failing native-shaped tests**

```csharp
var processor = new FakePlayerSaveRequestProcessor();
Equal(true, CassetteSaveTransactionAdapter.TrySubmitHaveInBag(
    processor, nameof(ePlayableSong.BADASS), out _), "semantic request submits");
Equal(ePlayableSong.BADASS, processor.LastRequest!.Song, "exact song");
Equal(eSongCassetteStatus.HAVE_IN_BAG, processor.LastRequest.CassetteStatus, "bag status");
Equal(ePlayerSaveChangeBundleKey.DEFAULT, processor.LastRequest.Bundle, "default bundle");
```

Require incompatible processors to fail closed. Retain IL2CPP nullable `HasValue`/`Value` selection cases. Audit out `SaveDataManager`, `WritePlayerSaveFile`, `RequestWriteForPlayerSave`, both Persist request types, deposited submission, and direct Music Lab unlock calls.

- [ ] **Step 2: Run focused test and verify RED**

Run Task 1 Step 2. Expected: FAIL because the adapter still stages into a supplied persist bundle.

- [ ] **Step 3: Implement exact request submission**

Validate exact processor type name `PlayerSaveRequestProcessor`. Call `CassetteNativeRequestFactory.TryCreateHaveInBagRequest(...)`, which supplies `HAVE_IN_BAG` and `DEFAULT`, then invoke only the matching one-parameter `ProcessRequest` overload. Remove adapter methods used only for natural persist extraction/staging.

- [ ] **Step 4: Run focused test to verify GREEN**

Run Task 1 Step 2. Expected: request semantics and prohibited-API audits pass.

- [ ] **Step 5: Commit**

```powershell
git add client/CassetteSaveTransactionAdapter.cs client/tests/CassetteRandomization/Program.cs
git commit -m "fix(client): submit semantic cassette save requests"
```

---

### Task 3: Exact Final Loaded-Save Boundary and Unity Stabilization

**Files:**
- Modify: `client/CassetteRandomizationPolicy.cs`
- Modify: `client/Plugin.cs`
- Modify: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Produces: `CassetteSaveBoundarySignalKind` values `Selection`, `Creation`, and `Build`.
- Produces: `CassetteSaveIdentityStabilizer.Signal(int expectedSlot, CassetteSaveBoundarySignalKind kind)`, `Observe(int? selectedSlot, long selectedStatePointer)`, and `Reset()`.
- Produces: an activation result containing `Slot`, `Pointer`, `Generation`, and `Reason` only after two matching Unity observations.
- Consumes: `PlayerSaveManagementEnquiries.GetSelectedSaveFileSlotNumber()`, `TryGetSelectedSlotSaveFileState()`, and public `Il2CppObjectBase.Pointer`.
- Preserves: AP receipt routing, epoch-scoped satisfaction, exact semantic request submission, bounded verification, normal machine insertion, and all prohibitions.

- [ ] **Step 1: Write failing stabilization behavior tests**

Add pure tests with explicit signals and observations:

```csharp
var stabilizer = new CassetteSaveIdentityStabilizer();
stabilizer.Signal(4, CassetteSaveBoundarySignalKind.Creation);
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(0, 100), "old startup slot cannot satisfy slot-4 signal");
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 400), "first matching observation is not stable");
var activation = stabilizer.Observe(4, 400);
Equal(4, activation!.Value.Slot, "slot 4 activates after two matching Unity observations");
Equal(400L, activation.Value.Pointer, "activation records native state identity");

stabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection);
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 400), "first observation before newer signal");
stabilizer.Signal(4, CassetteSaveBoundarySignalKind.Build);
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 400), "same-slot Build resets stability to zero");
activation = stabilizer.Observe(4, 400);
Equal(true, activation!.Value.IncludesBuild, "coalesced generation preserves Build");
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 400), "coalesced generation activates at most once");
```

Also require:

- a newer signal invalidates a stale candidate;
- slot switch activates once;
- same-slot pointer replacement activates once;
- a new Build generation activates once when the same slot and pointer are reused;
- duplicate create/build/change signals coalesce around one final identity;
- repeated non-Build signals for the activated identity do not duplicate epochs;
- `Observe` without an exact signal, including room-transition calls, cannot activate.
- an active slot-0 epoch followed by a queued slot-4 signal reports boundary pending immediately, and old-epoch reconciliation/verification eligibility remains false until slot 4 activates.

- [ ] **Step 2: Run focused test and verify RED**

```powershell
dotnet run --project client/tests/CassetteRandomization/CassetteRandomization.Tests.csproj -c Release
```

Expected: FAIL because no expected-slot/native-pointer stabilizer exists.

- [ ] **Step 3: Implement the minimal pure stabilizer**

Keep signal generation, expected slot, an `includesBuild` marker, boundary-pending state, candidate identity, consecutive-match count, activated identity, and last-consumed Build generation. Every `Signal`, including same-slot, advances/coalesces the pending generation, immediately marks the boundary pending, invalidates the candidate identity, and resets the consecutive count to zero. Signals for the same expected slot preserve `includesBuild=true` if any signal was Build, even when a later Selection or Creation signal arrives. A different expected slot replaces the stale expected slot and Build marker. `Observe` ignores null slots, zero pointers, and slot mismatches; the first matching identity records a candidate and the second consecutive match may return one activation.

Return an activation when:

- the stable slot differs from the activated slot;
- the stable pointer differs from the activated pointer; or
- the stable signal includes an unconsumed Build generation for the selected slot.

Consume a Build generation at most once. Return no activation for later non-Build signals with the same stable identity. Activation clears boundary-pending state; no other observation or lifecycle notification clears it.

- [ ] **Step 4: Run focused test to verify stabilizer GREEN**

Run Step 2. Expected: every pure identity and generation case passes.

- [ ] **Step 5: Write failing production-wiring tests**

Require exact hook registration for:

```text
SaveDataRequestProcessor.ChangeSelectedPlayerSaveSlot(Int32)
SaveDataRequestProcessor.CreateNewPlayerSaveFileInEmptySlot(Int32)
SaveDataRequestProcessor.ProcessRequest(BuildPlayerSaveStateFromFileRequest)
```

Require callbacks to extract the exact expected slot and call only the queue/stabilizer signal API. Reject `TryReconcile`, native status reads, semantic submissions, and epoch activation inside those callbacks.

Require every exact callback to suspend prior-epoch reconciliation through pure managed state immediately. While the stabilizer reports boundary pending, require `TryReconcile`, delayed verification, and semantic submission paths to fail closed before any native enquiry.

Require the Unity keeper to:

- read selected slot and selected public state;
- use the public native `Pointer`;
- feed the stabilizer every update only while a signal is pending;
- call `ActivateLoadedSave` only from a returned stabilized activation.

Require the four broad selection request postfixes not to call `ActivateLoadedSave`. Continue rejecting `SelectedPlayerSaveSlotChangedEvent.HandleEvent`, Persist hooks, private save APIs, forced deposit/unlock, and process-wide satisfaction.

- [ ] **Step 6: Run focused test and verify wiring RED**

Run Step 2. Expected: FAIL because broad selection postfixes still activate immediately and exact mutation hooks are absent.

- [ ] **Step 7: Install exact queue-only mutation hooks**

Add an exact-signature patch helper rather than matching every similarly named method. Postfixes receive:

```csharp
public static void SelectedSlotMutationPostfix(object[]? __args, MethodBase __originalMethod)
public static void BuiltPlayerSaveStatePostfix(object[]? __args)
```

For `ChangeSelectedPlayerSaveSlot` and `CreateNewPlayerSaveFileInEmptySlot`, extract the sole `Int32` argument and enqueue `Selection` or `Creation`. For `BuildPlayerSaveStateFromFileRequest`, extract its `SlotNumber` and enqueue `Build`. Enqueueing immediately suspends the prior epoch in managed state. Missing/unreadable arguments fail closed and log once; callbacks perform no save enquiry or cassette work.

The existing broad request postfixes become diagnostic-only or are removed. They do not activate/deactivate epochs.

- [ ] **Step 8: Stabilize identity in the Unity keeper**

On every keeper update with a pending signal:

1. read the selected slot;
2. read the selected public state;
3. obtain its public `Pointer` as a non-zero `IntPtr`;
4. call `Observe(selectedSlot, pointer.ToInt64())`;
5. if an activation is returned, call `ActivateLoadedSave` with its exact slot/generation reason and queue reconciliation.

A newer signal, including same-slot, resets stability and replaces stale candidate work so both observations occur afterward. Same-slot signals preserve Build inclusion, and one coalesced generation resumes reconciliation at most once. While any boundary signal/candidate is pending, the keeper may observe identity but must not advance old verification timers or enter native reconciliation. Room transition and other lifecycle callbacks remain reconciliation-only and never call the stabilizer's `Signal`.

- [ ] **Step 9: Run complete verification**

Run the focused suite and all 17 client projects, then:

```powershell
& .\client\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall
py .\tools\validate-repo.py
git diff --check
```

Expected: focused and 17/17 suites pass; build has 0 errors; validator and diff check pass; unrelated warnings do not increase.

- [ ] **Step 10: Commit**

```powershell
git add client/CassetteRandomizationPolicy.cs client/Plugin.cs client/tests/CassetteRandomization/Program.cs
git commit -m "fix(client): stabilize final loaded save identity"
```

---

### Task 4: Two-Restart Live Acceptance

**Files:**
- Modify: `docs/testing/2026-08-29-full-cassette-acceptance.md`
- Modify: `docs/TESTING_AND_ISSUES.md`

**Interfaces:**
- Consumes: verified v0.68 client and fresh v0.22 seed `8302601` at `127.0.0.1:38282`, AP slot `Jack`, UI save slot 4.
- Produces: auditable Badass/The Heist bag and deposited durability evidence.

- [ ] **Step 1: Record automated evidence**

Document commits, RED/GREEN output, 17/17 result, no-install build, validator, diff check, and independent reviews.

- [ ] **Step 2: Install only with explicit owner authorization**

With the game closed, replace only `D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP\`. Do not replace APWorld v0.22.

- [ ] **Step 3: Verify final slot-4 stabilization before submission**

Require startup, compatible routing, and connection logs. The preliminary startup slot-0 selection must not authorize cassette submission. Create or select UI slot 4, then require exact mutation-signal logs, two matching non-null slot-4 pointer observations, and one slot-4 epoch activation before processor reconciliation or cassette submission. Reject any epoch created by room transition alone.

- [ ] **Step 4: Restart before insertion**

Let AP history restore Badass and The Heist. Require semantic submission followed by later authoritative `HAVE_IN_BAG`; owner confirms both in inventory. Close without insertion, relaunch UI slot 4, require an exact Build/selection signal, a new stabilized epoch (including if the numeric slot and native pointer are reused), and `HAVE_IN_BAG` for both with no duplicate source checks or unnecessary resubmission.

- [ ] **Step 5: Restart after normal deposit**

Insert both normally and confirm their levels unlock. Relaunch again; require a new epoch, `HAVE_DEPOSITED`, no returned cassette, no forced unlock, no request loop, and no crash.

- [ ] **Step 6: Record result and commit**

Keep unrelated manual matrix rows open and record any failure as a blocker.

```powershell
git add docs/testing/2026-08-29-full-cassette-acceptance.md docs/TESTING_AND_ISSUES.md
git commit -m "docs: record loaded-save cassette acceptance"
```

---

## Final Verification Gate

```powershell
dotnet run --project client/tests/CassetteRandomization/CassetteRandomization.Tests.csproj -c Release
& .\client\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall
py .\tools\validate-repo.py
git diff --check
git status --short
```

After independent task reviews, run one whole-branch review from merge base through `HEAD`. Do not merge or push without explicit owner approval.
