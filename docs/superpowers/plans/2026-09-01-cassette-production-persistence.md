# Cassette Production Persistence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every newly verified AP cassette grant durably survive a clean game restart through the live-proven pointer-bound public save-state route.

**Architecture:** Generalize the acceptance runtime into a sequential, epoch-scoped persistence runtime and add an identity-bound wave queue. Production starts automatically when full cassette routing is compatible, serializes native cassette grants with the disk transaction, polls only public state for terminal proof, and permanently tombstones the current epoch after any possibly mutating ambiguity. The default-false acceptance key remains diagnostic compatibility only; production does not install or depend on the uncorrelated native write-completed event hook.

**Tech Stack:** C#/.NET 6, BepInEx 6 IL2CPP, Harmony, reflection over public IL2CPP save-state APIs, PowerShell verification scripts.

**Spec:** `docs/superpowers/specs/2026-08-30-cassette-save-transaction-design.md`

## Completion record

- Task 1 landed as `ed84122` with `CassettePersistenceWaveQueue` and `CassettePointerBoundPersistenceRuntime` supporting `OneShotPerEpoch` and `SequentialVerifiedBatches`.
- Task 2 production wiring landed as `db84da7`. Review follow-up `dba7221` extracted the executable `CassetteProductionPersistenceCoordinator`, which owns queue/runtime/marker transitions and exposes injected adapter-admission and reconciliation callbacks while `Plugin.cs` delegates orchestration. Review follow-up `a22d9ae` bound `BeginEpoch`, `TryInvoke`, and `CancelEpoch` to the complete generation/epoch/slot/pointer identity so a stale epoch cannot cancel or clear newer queued/active work.
- The decisive live trial used `AP_54469213998348218286.zip`, `127.0.0.1:38282`, player `Jack`, UI save slot 4, and the four-song batch `BADASS`, `HEAVY_METAL`, `KEEP_ON_HUSTLIN`, `ON_THE_WAY`. PRE redundancy was `0/16`; polling VERIFIED at `1/17`. After a clean close and relaunch, all four remained present with zero semantic regrants and zero second persistence attempt.
- Automatic production was then installed and accepted with deterministic seed `91001`, archive `AP_03679412317094840404.zip`, player `Jack`, and fresh UI save slot 4. With the developer acceptance switch false, `HEAVY_METAL` persisted in attempt 1 (`2/3` to `3/4`) and `ON_THE_WAY` persisted in attempt 2 (`3/4` to `4/5`) in the same unchanged save epoch. A clean close and relaunch restored both as `HAVE_IN_BAG` with zero semantic regrants, zero persistence attempts, and zero errors.
- These commits and live trials remain feature-branch evidence. They do not represent a merge, push, public release, or broad manual gameplay pass.

## Global Constraints

- Production may invoke only public `PlayerSaveFileState.PersistAllChangesInBundle(DEFAULT/1)` followed by inherited public `BaseSaveFileState.RequestUrgentWriteToDisk()` on the exact pointer-validated active state.
- Never construct or submit generic Persist requests, mutate selected-slot globals, call private save managers/writers, wrap a raw pointer, or persist a non-`DEFAULT` bundle.
- Invocation occurs only on the Unity thread after the exact generation/epoch/slot/pointer, selected-save, registered/retained processor, active-slot entry, status, bundle, and public write-state gates are freshly proven.
- Native write-completed events are uncorrelated hints and are not installed or required by normal production behavior.
- One semantic-grant/write transaction is active at a time. A verified batch may be followed by another batch in the same epoch; indeterminate, failed, or timed-out mutation tombstones the epoch.
- The hard active-update timeout remains 130 seconds; save boundaries and deactivation cancel old work and prevent stale logs from entering a new epoch.
- Do not merge, push, publish, release, install, or launch during implementation. Live installation requires separate approval after tests, Release build, validation, and independent review.

---

### Task 1: Sequential persistence runtime and identity-bound wave queue

**Files:**
- Modify: `client/CassetteRandomizationPolicy.cs:1388-1715`
- Modify: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Consumes: existing `CassettePersistenceAcceptanceIdentity`, `CassettePersistenceAcceptanceBaseline`, `CassettePersistenceAcceptanceAttempt`, `CassettePublicWriteDiagnosticState`, and marker journal/emitter types.
- Produces: `CassettePersistenceQueuedWave`, `CassettePersistenceWaveQueue`, and a generalized `CassettePointerBoundPersistenceRuntime` supporting `OneShotPerEpoch` and `SequentialVerifiedBatches` policies.

- [x] **Step 1: Write failing queue tests**

Add focused scenarios that construct exact identities and prove this contract:

```csharp
var queue = new CassettePersistenceWaveQueue();
var id = new CassettePersistenceAcceptanceIdentity(2, 1, 4, 0x4400);
queue.BeginEpoch(id);
True(queue.TryEnqueue(id, new[] { "ON_THE_WAY", "BADASS", "BADASS" }));
True(queue.TryPeek(id, out CassettePersistenceQueuedWave wave));
SequenceEqual(new[] { "BADASS", "ON_THE_WAY" }, wave.Songs);
False(queue.TryEnqueue(id with { Epoch = 2 }, new[] { "HEAVY_METAL" }));
queue.Complete(wave);
False(queue.TryPeek(id, out _));
```

Also prove that a wave arriving while an attempt is active remains queued, a save-boundary identity replacement clears old waves, and `TombstoneEpoch` rejects/clears all later same-epoch waves.

- [x] **Step 2: Run the focused suite and capture strict RED**

Run:

```powershell
dotnet run --project client/tests/CassetteRandomization/CassetteRandomization.Tests.csproj -c Release
```

Expected: compile failure because the queue types and methods do not exist.

- [x] **Step 3: Implement the minimal identity-bound queue**

Add these exact shapes:

```csharp
internal readonly record struct CassettePersistenceQueuedWave(
    CassettePersistenceAcceptanceIdentity Identity,
    IReadOnlyList<string> Songs,
    long WaveId);

internal sealed class CassettePersistenceWaveQueue
{
    internal void BeginEpoch(CassettePersistenceAcceptanceIdentity identity);
    internal bool TryEnqueue(CassettePersistenceAcceptanceIdentity identity,
        IEnumerable<string> songs);
    internal bool TryPeek(CassettePersistenceAcceptanceIdentity identity,
        out CassettePersistenceQueuedWave wave);
    internal void Complete(CassettePersistenceQueuedWave wave);
    internal void TombstoneEpoch(CassettePersistenceAcceptanceIdentity identity);
    internal void CancelEpoch(CassettePersistenceAcceptanceIdentity identity);
}
```

Normalize songs with ordinal distinct/sort. Never accept a zero pointer, empty song set, stale identity, or tombstoned epoch. `TryPeek` must not remove the batch before terminal VERIFIED; pre-invocation deferral therefore cannot lose it.

- [x] **Step 4: Write failing sequential-runtime tests**

Extend the existing one-shot tests to prove:

```csharp
var runtime = new CassettePointerBoundPersistenceRuntime(
    CassettePersistenceAttemptPolicy.SequentialVerifiedBatches);
runtime.BeginEpoch(identity);
True(runtime.TryPrepare(identity, firstBaseline, out var first));
True(runtime.MarkInvoked(first));
Equal(CassettePersistenceAcceptanceOutcome.Verified,
    runtime.Observe(first, identity, verifiedState, true, true,
        TimeSpan.FromMilliseconds(16), out _));
True(runtime.TryPrepare(identity, secondBaseline, out var second));
True(second.Id > first.Id);
```

Add separate cases where invocation becomes indeterminate, polling fails, or timeout occurs; every later `TryPrepare` in that epoch must return false. Preserve a `OneShotPerEpoch` case proving the acceptance behavior still rejects a second verified batch.

- [x] **Step 5: Generalize the runtime with no weakened terminal rules**

Rename the pure runtime to `CassettePointerBoundPersistenceRuntime`, add:

```csharp
internal enum CassettePersistenceAttemptPolicy
{
    OneShotPerEpoch,
    SequentialVerifiedBatches,
}
```

Replace `_attemptedThisEpoch` with explicit `_epochTombstoned` and `_completedAttemptCount`. VERIFIED permits another prepare only for `SequentialVerifiedBatches`; indeterminate/FAILED/TIMEOUT tombstone. Cancellation retires the active attempt, and `BeginEpoch` is the only operation that clears a tombstone. Keep identity, status, failure-baseline, redundancy/success, bounded elapsed, and exact pointer rules byte-for-byte equivalent where possible.

- [x] **Step 6: Run the focused suite and commit Task 1**

Run the focused command above. Expected: all cassette policy scenarios pass.

```powershell
git add client/CassetteRandomizationPolicy.cs client/tests/CassetteRandomization/Program.cs
git commit -m "refactor: support sequential cassette persistence batches"
```

---

### Task 2: Production orchestration and grant serialization

**Files:**
- Modify: `client/Plugin.cs:52-166`
- Modify: `client/Plugin.cs:17965-18940`
- Modify: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Consumes: existing target diagnostic readers, eligibility/ownership proof, and the exact public adapter.
- Produces: `CassetteProductionPersistenceCoordinator`, which owns `CassettePersistenceWaveQueue`, sequential `CassettePointerBoundPersistenceRuntime`, marker admission, adapter admission, terminal wave handling, and the reconciliation callback; `Plugin.cs` delegates to it for the automatic `CASSETTE PERSISTENCE` lifecycle and `IsPersistenceWriteActive` behavior.

- [x] **Step 1: Write failing production-wiring tests**

Add source and behavior tests proving all of the following:

```csharp
True(pluginSource.Contains(
    "if (cassettePointerBoundPersistenceAcceptance.Value)",
    StringComparison.Ordinal));
True(pluginSource.Contains(
    "PatchMethodsByParameter(\"HandleEvent\", \"PlayerSaveWriteCompletedEvent\"",
    StringComparison.Ordinal));
True(policySource.Contains("CassettePersistenceAttemptPolicy.SequentialVerifiedBatches",
    StringComparison.Ordinal));
True(receiptSource.Contains("CASSETTE PERSISTENCE PRE", StringComparison.Ordinal));
```

Behavior fixtures must prove:

- compatible routing enables production even while `EnableCassettePointerBoundPersistenceAcceptance=false`;
- an exact newly verified wave is queued before eligibility is evaluated;
- transient pre-invocation unreadability leaves the same wave queued and performs no adapter call;
- once gates recover, that exact wave starts once;
- an active persistence attempt makes `CanSubmitNativeCassetteGrant` false;
- after VERIFIED, reconciliation is queued and a later cassette can submit/form a second wave;
- FAILED/TIMEOUT/indeterminate tombstones the epoch and blocks later adapter calls;
- boundary cancellation clears the old queue and old markers precede the new epoch's PRE.

- [x] **Step 2: Run the focused suite and capture RED**

Run the focused cassette test project. Expected: assertions fail because production remains opt-in and no production queue/grant gate exists.

- [x] **Step 3: Separate diagnostic compatibility from production enablement**

Keep the developer key bound with default `false` only so existing acceptance configs remain parseable, but change configuration to:

```csharp
CassetteReceiptRandomization.Configure(
    acceptanceDiagnosticsEnabled: cassettePointerBoundPersistenceAcceptance.Value);
```

Production persistence itself must be enabled solely by compatible cassette routing inside `ApplySlotData`. Retain the `PlayerSaveWriteCompletedEvent` Harmony patch only behind the default-false startup diagnostic key; production must not install or depend on it when that key is false. The setting may control additional acceptance-detail logging only; changing it must never enable/disable durability or cancel a production attempt.

- [x] **Step 4: Queue waves before attempting eligibility**

When the existing `TryConsumeNewlyVerifiedGrantDiagnosticWave` producer returns a current wave, production immediately delegates `TryEnqueue` to `CassetteProductionPersistenceCoordinator` before any eligibility read. The producer name remains for compatibility with its bounded ownership diagnostic role; the queued value is production work after admission. On each Unity update, if no write is active and the epoch is not tombstoned, the coordinator exposes the exact queue head and production freshly runs every gate. If a gate fails before invocation, one bounded `DEFERRED` transition is logged and the exact wave remains queued.

- [x] **Step 5: Serialize semantic grants with disk writes**

Inside the existing locked decision at `Plugin.cs` around `_runtime.CanSubmit(...)`, require:

```csharp
submit = _productionPersistence.CanSubmitNativeCassetteGrant(
    _runtime.CanSubmit(nativeSong, status, processorAvailable: true));
```

Do not suppress authoritative reads or verification of the active batch. On terminal VERIFIED, complete the exact queued wave and call the existing Unity-thread observation queue so newly received ownership can reconcile. On FAILED/TIMEOUT/indeterminate, tombstone the queue for that identity and do not resume a second persistence attempt until a new epoch.

- [x] **Step 6: Reuse the exact adapter and public polling proof**

`CassetteProductionPersistenceCoordinator.TryInvoke` installs the attempt token/baseline and PRE marker before calling the injected adapter. The adapter invocation remains outside both `Plugin.Sync` and the coordinator/marker-emitter locks. The exact same-object pointer plan and promotion-then-urgency order remain unchanged. `Plugin.cs` gathers fresh public evidence each Unity update and delegates terminal observation back to the coordinator, which owns the same state/identity/ownership/status rules and 130-second bound. The coordinator admits globally sequenced production markers and the existing serialized emitter drains them:

```text
CASSETTE PERSISTENCE PRE
CASSETTE PERSISTENCE INVOKED
CASSETTE PERSISTENCE IMMEDIATE POST
CASSETTE PERSISTENCE VERIFIED|FAILED|TIMEOUT|CANCELLED
```

Do not emit `EVENT` in normal production.

- [x] **Step 7: Run focused tests and commit Task 2**

Run the focused suite. Expected: all cassette scenarios pass, including two sequential batches and every tombstone/boundary interleaving.

Task 2 was committed as `db84da7` (`fix: persist randomized cassette grants automatically`). Executable coordinator extraction and exact-identity cancellation review fixes followed in `dba7221` and `a22d9ae`; both retain the Task 2 safety boundary.

---

### Task 3: Documentation, regression verification, and handoff

**Files:**
- Modify: `docs/superpowers/specs/2026-08-30-cassette-save-transaction-design.md`
- Modify: `docs/superpowers/plans/2026-09-01-cassette-production-persistence.md`
- Modify if behavior/status lists require it: `README.md`, `CHANGELOG.md`

**Interfaces:**
- Consumes: completed production implementation and live acceptance evidence.
- Produces: reviewer-ready documentation and a verified, non-installed branch commit.

**Task 3 verification record (2026-09-01):** The focused cassette suite passed; every discovered client test project passed (`CLIENT_TEST_PROJECTS=17/17`); the Release build completed with installation skipped, zero errors, and seven existing nullable warnings; repository validation passed; and final diff/status checks were clean apart from the two intended documentation files.

- [x] **Step 1: Reconcile documentation with the final implementation**

Document the live proof: exact slot 4 pointer ownership, four-cassette batch, PRE `0/16`, terminal `1/17`, clean close/relaunch, zero regrant, and zero second trial. State that automatic production is polling-only, sequential, epoch-tombstoned after ambiguity, and independent of the developer acceptance key. Do not claim a public release or broad manual coverage.

- [x] **Step 2: Run the full client test matrix**

```powershell
$projects = Get-ChildItem -LiteralPath client/tests -Recurse -Filter *.csproj | Sort-Object FullName
$passed = 0
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { throw "Failed: $($project.FullName)" }
    $passed++
}
"CLIENT_TEST_PROJECTS=$passed/$($projects.Count)"
```

Expected: every discovered client test project passes.

- [x] **Step 3: Build without installation and validate the repository**

```powershell
.\client\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall
py -3 .\tools\validate-repo.py
git diff --check
git status --short
```

Expected: Release build succeeds with zero errors, installation is explicitly skipped, validator passes, diff check is clean, and only intended files are modified.

- [x] **Step 4: Commit documentation and request independent review**

```powershell
git add docs/superpowers/specs/2026-08-30-cassette-save-transaction-design.md docs/superpowers/plans/2026-09-01-cassette-production-persistence.md README.md CHANGELOG.md
git commit -m "docs: define production cassette persistence"
```

Request review specifically for public-only method ownership, exact pointer gates, wave preservation on pre-invocation deferral, grant serialization, sequential verified batches, epoch tombstones, polling-only terminal proof, marker ordering, and absence of live installation/merge/push.

- [x] **Step 5: Stop before live deployment**

Report commits and exact verification output. Do not install or launch. A later authorized live test should use a fresh seed/save, verify one batch persists, receive a second cassette in the same epoch, verify a second sequential write, then clean-relaunch with zero regrants.
