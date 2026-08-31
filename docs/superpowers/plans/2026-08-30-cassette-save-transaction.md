# Cassette Loaded-Save Reconciliation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist every AP-owned cassette into the confirmed loaded save through the exact native semantic player-save request, with epoch-scoped authoritative verification and bounded retries.

**Architecture:** AP ownership remains session-wide, while native satisfaction and outstanding attempts belong only to a monotonically increasing loaded-save epoch established by safe save-selection request postfixes. After that epoch exists, Unity-thread lifecycle points read before writing, submit `RecordSongCassetteStatusInSaveDataRequest(song, HAVE_IN_BAG, DEFAULT)` through a compatible `PlayerSaveRequestProcessor`, and verify later; request return alone is never satisfaction. Live diagnostics proved the earlier `PersistSaveChangeBundleRequest`/`PersistAllSaveChangeBundlesRequest` assumption false because neither prefix ran during supported save and gameplay actions.

**Tech Stack:** C#/.NET 6, BepInEx 6 IL2CPP, Harmony, generated-interop reflection adapters, console regression projects, PowerShell build/validation scripts.

**Spec:** `docs/superpowers/specs/2026-08-30-cassette-save-transaction-design.md`

## Global Constraints

- Do not change cassette placement, sources, medal logic, or normal cassette-machine insertion.
- Do not automatically deposit cassettes or directly unlock Music Lab levels.
- Do not call private save-manager/flush APIs or synthesize a persist request.
- Do not patch `SelectedPlayerSaveSlotChangedEvent.HandleEvent`.
- Do not rely on `PersistSaveChangeBundleRequest` or `PersistAllSaveChangeBundlesRequest`; live diagnostics observed no prefix entry for load, travel, title return, difficulty change, shutdown, or the durable Plant Pipes grant.
- Begin an epoch only after a safe selection request completes and both selected-slot number and selected-state enquiries succeed; same-slot reselection begins a new epoch.
- Keep AP ownership session-wide, but never keep satisfaction process-wide. Bag/deposited status is terminal only in the epoch that authoritatively observed it.
- Read before every write. Preserve `HAVE_IN_BAG` and `HAVE_DEPOSITED`.
- Submit only `RecordSongCassetteStatusInSaveDataRequest(song, HAVE_IN_BAG, DEFAULT)` through a compatible `PlayerSaveRequestProcessor` on the Unity thread.
- Treat request return as verification-pending. Require a later authoritative read in the same epoch.
- Retry only at safe lifecycle points and through a bounded timer.
- APWorld stays v0.22 and client stays v0.68 for this repair.
- No merge, push, publish, or release is authorized.

---

## File Structure

- Modify `client/CassetteRandomizationPolicy.cs`: replace persist-token staging with epoch-scoped read/apply/verify decisions and bounded retry state.
- Modify `client/CassetteSaveTransactionAdapter.cs`: retain selection and read adapters; expose exact semantic `DEFAULT` request submission; remove natural-persist bundle staging.
- Modify `client/Plugin.cs`: retain safe epoch hooks, reconcile from safe Unity lifecycle points, use the Plant Pipes compatible processor pattern, and remove reliance on absent Persist hooks.
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

### Task 3: Safe Post-Epoch Unity Wiring

**Files:**
- Modify: `client/Plugin.cs`
- Modify: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Produces: `CassetteReceiptRandomization.TryReconcile(string reason)` and `TickPendingNativeGrants(TimeSpan elapsed)`.
- Consumes: revised runtime/adapter and the existing Plant Pipes processor handoff.
- Preserves: receipt recognition, source interception, normal machine deposit, and four safe selection postfixes.

- [ ] **Step 1: Write failing wiring tests**

Require hooks for `SelectPlayerSaveSlotRequest`, `SelectMostRecentlyUsedRegularPlayerSaveSlotRequest`, `EnsureAPlayerSaveSlotIsSelectedRequest`, and `CreateNewPlayerSaveFileInSlotRequest`. Require reconciliation to check synchronized compatibility plus active epoch, read before write, preserve bag/deposited, and delay verification. Require the Plant Pipes stateless processor handoff only after selected-save readability. Reject cassette Persist prefix/postfix registration, the unsafe selected-slot event, private flush APIs, and continuous submission.

- [ ] **Step 2: Run focused test and verify RED**

Run Task 1 Step 2. Expected: FAIL because production still waits for absent Persist callbacks.

- [ ] **Step 3: Retain epoch activation and remove absent-boundary code**

Keep `SaveSelectionPostfix` activation only after slot plus selected state succeed. Remove cassette Persist hook registrations, transaction queues, callbacks, and diagnostics used only by the disproven boundary. Do not infer an epoch from room/result/slot-data callbacks.

- [ ] **Step 4: Implement read-before-write reconciliation**

Invoke `TryReconcile` after confirmed selection, AP history/receipt delivery, compatible processor capture, and existing safe scene/lifecycle points. Snapshot epoch/processor, read authoritatively, reject a stale epoch before submission, and wrap only the exact request invocation in `_applyingNativeGrant`. Record request return as verification-pending.

- [ ] **Step 5: Implement bounded delayed verification**

The keeper advances only pending timers. On expiry, re-read on the Unity thread. Bag/deposited satisfies the current epoch; unearned permits only the next bounded attempt; epoch changes discard stale attempts and reevaluate from AP ownership.

- [ ] **Step 6: Run complete verification**

Run the focused suite and all 17 client projects, then:

```powershell
& .\client\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall
py .\tools\validate-repo.py
git diff --check
```

Expected: focused and 17/17 suites pass; build has 0 errors; validator and diff check pass; unrelated warnings do not increase.

- [ ] **Step 7: Commit**

```powershell
git add client/Plugin.cs client/tests/CassetteRandomization/Program.cs
git commit -m "fix(client): reconcile cassettes after save selection"
```

---

### Task 4: Two-Restart Live Acceptance

**Files:**
- Modify: `docs/testing/2026-08-29-full-cassette-acceptance.md`
- Modify: `docs/TESTING_AND_ISSUES.md`

**Interfaces:**
- Consumes: verified v0.68 client and retained v0.22 seed at `127.0.0.1:38281`, slot `Jack`.
- Produces: auditable Badass/The Heist bag and deposited durability evidence.

- [ ] **Step 1: Record automated evidence**

Document commits, RED/GREEN output, 17/17 result, no-install build, validator, diff check, and independent reviews.

- [ ] **Step 2: Install only with explicit owner authorization**

With the game closed, replace only `D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP\`. Do not replace APWorld v0.22.

- [ ] **Step 3: Verify loaded-save ordering**

Require startup, compatible routing, connection, epoch activation, processor availability, and read-before-write logs. Reject any submission before epoch activation.

- [ ] **Step 4: Restart before insertion**

Let AP history restore Badass and The Heist. Require semantic submission followed by later authoritative `HAVE_IN_BAG`; owner confirms both in inventory. Close without insertion, relaunch the same slot, require a new epoch and `HAVE_IN_BAG` for both with no duplicate source checks or unnecessary resubmission.

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
