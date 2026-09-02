# Cassette Pointer-Bound Persistence Acceptance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add one explicitly opt-in, one-shot acceptance trial that promotes and urgently writes the exact active player-save state after a newly verified cassette grant wave, without enabling automatic production persistence.

**Architecture:** A new pure `CassettePointerBoundPersistenceAcceptanceRuntime` owns immutable attempt identity/baselines, event hints, one-shot/tombstone state, timeout, and terminal decisions. `CassetteSaveTransactionAdapter` resolves and invokes only public `PlayerSaveFileState.PersistAllChangesInBundle(DEFAULT)` followed by public `BaseSaveFileState.RequestUrgentWriteToDisk()` on the exact pointer-bearing state. `CassetteReceiptRandomization` admits a trial only from the existing post-verification wave after every public identity, ownership, status, bundle, and no-write-in-flight gate passes.

**Tech Stack:** C#/.NET 6 plugin, .NET 8 executable policy fixtures, Harmony public event observation, BepInEx configuration, IL2CPP public reflection.

**Spec:** `docs/superpowers/specs/2026-08-30-cassette-save-transaction-design.md`

## Global Constraints

- Config key defaults `false`; disabled behavior installs no write-completed hook and performs no promotion/write.
- Acceptance invokes no generic Persist request, private save manager/writer, raw-pointer wrapper, selected-slot mutation, PersistAll-bundles request, TriggerUrgent request, or RequestWrite method.
- Both native calls run only from the existing Unity keeper tick and target the exact active public state object.
- A token and immutable PRE baseline exist before either native call; any possibly-mutating invocation failure tombstones the epoch trial.
- A completion event is only a wake/failure hint. Public state and identity proof are mandatory for success.
- There is no retry after invocation, indeterminate outcome, terminal failure, or timeout in the same epoch.

---

### Task 1: Pure acceptance state machine

**Files:**
- Modify: `client/CassetteRandomizationPolicy.cs`
- Test: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Produces: `CassettePointerBoundPersistenceAcceptanceRuntime`, immutable attempt/baseline/snapshot records, prepare/invoked/indeterminate/event/observe/cancel operations.

- [x] **Step 1: Write failing executable tests** covering default idle state; prepare once per epoch; synchronous same-slot event after token installation; wrong-slot/stale events; event-alone pending; exact success via advanced timestamp or redundancy; failure time/reason, status, identity, unreadable, timeout, cancellation, and tombstone behavior.
- [x] **Step 2: Run** `dotnet run --project client/tests/CassetteRandomization/CassetteRandomization.Tests.csproj -c Release` and record the missing-type/API RED.
- [x] **Step 3: Implement the minimal pure runtime** with a 130-second capped-active-update watchdog and no retry after an invocation begins.
- [x] **Step 4: Rerun the focused suite** and require every pure state-machine scenario to pass.

### Task 2: Exact public invocation adapter

**Files:**
- Modify: `client/CassetteSaveTransactionAdapter.cs`
- Test: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Produces: `TryPreparePointerBoundPersistenceInvocation(processor, expectedPointer, out plan, out stage)` and `InvokePointerBoundPersistence(plan, out result, out stage)`.
- `plan` contains the exact active state, public method handles, and parsed `DEFAULT` enum; it never constructs a pointer wrapper or request.

- [x] **Step 1: Write failing fixtures** whose public owner/signatures match `PlayerSaveFileState.PersistAllChangesInBundle(ePlayerSaveChangeBundleKey)` and inherited `BaseSaveFileState.RequestUrgentWriteToDisk()`. Assert order `PersistDefault`, then `UrgentWrite`, exact same object, one call each.
- [x] **Step 2: Add rejection fixtures** for pointer mismatch, private/missing/ambiguous/wrong-owner/wrong-signature methods, invalid DEFAULT, promotion throw, and urgency throw after promotion.
- [x] **Step 3: Run the focused suite** and capture the missing adapter RED.
- [x] **Step 4: Implement public-only plan resolution and invocation**, returning distinct `NotInvoked`, `PromotionIndeterminate`, `UrgencyIndeterminate`, and `Invoked` results.
- [x] **Step 5: Rerun the focused suite** and require exact call order and every fail-closed case to pass.

### Task 3: Opt-in orchestration and gated event observation

**Files:**
- Modify: `client/Plugin.cs`
- Test: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Config: `[Developer] EnableCassettePointerBoundPersistenceAcceptance=false`.
- Logging: `CASSETTE PERSISTENCE ACCEPTANCE PRE`, `INVOKED`, `IMMEDIATE POST`, `EVENT`, `VERIFIED`, `FAILED`, `TIMEOUT`, `CANCELLED`.
- Consumes: existing verified wave, selected-save evidence, target diagnostic, active state, public write state, and adapter plan.

- [x] **Step 1: Write failing behavioral/source-boundary tests** proving default false, opt-in-only event patch, complete gate matrix, current identity, pointer-bound decision, processor ownership, expected-slot pointer, DEFAULT nonzero, retained bag statuses, and no already-required write.
- [x] **Step 2: Write failing orchestration tests** proving token-before-call, one invocation per epoch, promotion-success/urgency-throw tombstone, event wake only, boundary/config cancellation, late-event ignore, relaunch-persisted no wave/trial, and relaunch-missing one fresh opt-in trial.
- [x] **Step 3: Run the focused suite** and capture the orchestration RED.
- [x] **Step 4: Bind the default-false config and conditionally install the public event patch.** Configure/reset acceptance state before slot-data/application work.
- [x] **Step 5: Add the complete post-wave gate and Unity-thread invocation flow.** Resolve all public evidence before preparing the token; log PRE before mutation; invoke once; take immediate post-state evidence; poll active attempts on later ticks.
- [x] **Step 6: Route exact save boundaries, deactivation, and config disable to logical cancellation.** Event handler decodes public header first and only touches an active acceptance token.
- [x] **Step 7: Rerun the focused suite** and require all gates, ordering, terminal outcomes, and prohibited-path checks to pass.

### Task 4: Acceptance-only specification and full verification

**Files:**
- Modify: `docs/superpowers/specs/2026-08-30-cassette-save-transaction-design.md`

**Interfaces:**
- Documents that the trial is default-off acceptance instrumentation, not production automatic persistence.

- [x] **Step 1: Update the spec** with config key, exact gates, public method sequence, token/event/poll semantics, success proof, tombstones, log markers, and explicit non-production status.
- [x] **Step 2: Run the focused cassette suite** and require all scenarios to pass.
- [x] **Step 3: Run all 17 client test projects** and require `17/17` passing.
- [x] **Step 4: Run** `.\client\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall` and require zero errors plus explicit installation skipped.
- [x] **Step 5: Run** `py -3 .\tools\validate-repo.py` and require repository validation passed.
- [x] **Step 6: Run** `git diff --check`, inspect the complete diff, and verify only intended files changed.
- [ ] **Step 7: Commit locally** without installing, launching, merging, or pushing.
