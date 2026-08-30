# Cassette Save-Transaction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist AP-owned cassettes into the actually loaded player save by staging them inside the game's next natural save transaction and revalidating them after every load, reload, or save switch.

**Architecture:** AP ownership remains process-wide, but native satisfaction is scoped to a monotonically increasing loaded-save epoch established only by safe save-selection request postfixes. Missing receipts stage into the bundle already being persisted by the game; the original native persist request performs the write, and only its postfix may mark the cassette satisfied for that epoch.

**Tech Stack:** C#/.NET 6, BepInEx 6 IL2CPP, Harmony, reflection adapters for generated interop types, console-based regression projects, PowerShell build/validation scripts.

**Spec:** `docs/superpowers/specs/2026-08-30-cassette-save-transaction-design.md`

## Global Constraints

- Do not change cassette item placement, source routing, medal logic, or normal cassette-machine insertion.
- Do not automatically deposit cassettes or directly unlock Music Lab levels.
- Do not call private save-manager/flush APIs or synthesize an additional persist request.
- Do not patch `SelectedPlayerSaveSlotChangedEvent.HandleEvent`.
- A save epoch begins only after a safe save-selection request completes and selected-slot number plus selected-slot state enquiries both succeed.
- Reselecting the same numeric slot creates a new epoch.
- `HAVE_IN_BAG` and `HAVE_DEPOSITED` are terminal only for the epoch in which an authoritative read observes them.
- Stage an unowned cassette only inside the prefix of the game's natural `PersistSaveChangeBundleRequest` or `PersistAllSaveChangeBundlesRequest`.
- Use the natural bundle for a single-bundle persist and `DEFAULT` for persist-all.
- Postfix verification must match the epoch and slot captured by the prefix.
- The existing installed APWorld remains v0.22 and the client remains v0.68 for this repair.
- No merge, push, publish, or release is authorized by this plan.

---

## File Structure

- Modify `client/CassetteRandomizationPolicy.cs`: replace process-wide terminal-song behavior with a pure loaded-save epoch and natural-persist state machine.
- Create `client/CassetteSaveTransactionAdapter.cs`: isolate reflection for selected-slot verification, authoritative cassette reads, natural bundle extraction, and native cassette staging.
- Modify `client/Plugin.cs`: install safe save-selection and persist hooks, route callbacks into the state machine, and remove periodic native writes.
- Modify `client/tests/CassetteRandomization/Program.cs`: add behavioral and production-wiring regressions for epochs and transaction boundaries.
- Modify `docs/testing/2026-08-29-full-cassette-acceptance.md`: record automated results and the Badass/The Heist live recovery result.
- Modify `docs/TESTING_AND_ISSUES.md`: document the loaded-save transaction acceptance procedure.

---

### Task 1: Loaded-Save Epoch State Machine

**Files:**
- Modify: `client/CassetteRandomizationPolicy.cs`
- Modify: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Produces: `CassetteSaveEpochRuntime` with `ActivateSave(int slot)`, `DeactivateSave()`, `Receive(string nativeSong)`, `Observe(string nativeSong, string? nativeStatus)`, `BeginPersist(string bundle)`, and `CompletePersist(CassettePersistToken token, Func<string, string?> readStatus)`.
- Produces: immutable `CassettePersistToken(long epoch, int slot, string bundle, IReadOnlyList<string> stagedSongs)`.
- Consumes: existing `CassetteRandomizationPolicy` status values and AP-owned native-song identities.

- [ ] **Step 1: Replace faulty terminal-cache expectations with failing epoch tests**

Add named cases that execute the pure runtime:

```csharp
var runtime = new CassetteSaveEpochRuntime();
runtime.Receive("BADASS");
Equal(false, runtime.HasActiveSave, "receipt before save selection remains inactive");
Equal(0, runtime.BeginPersist("DEFAULT").StagedSongs.Count,
    "no staging before a confirmed save load");

runtime.ActivateSave(2);
Equal(1L, runtime.Epoch, "first selected save creates epoch one");
Equal(true, runtime.IsPending("BADASS"), "owned cassette is pending in loaded save");

runtime.Observe("BADASS", CassetteRandomizationPolicy.HaveInBag);
runtime.ActivateSave(2);
Equal(2L, runtime.Epoch, "same-slot reload creates a new epoch");
Equal(true, runtime.IsPending("BADASS"), "same-slot reload revalidates native state");
```

Add the ten cases from the root-cause report: pre-selection isolation, pre-slot bag invalidation, same-slot reload, deposited save A versus unowned save B, deposited revalidation, broad lifecycle neutrality, persist-prefix-only staging, persist-all default bundle, selection change during persist, and authoritative-read failure.

- [ ] **Step 2: Run the focused test and capture the expected RED**

Run:

```powershell
dotnet run --project client/tests/CassetteRandomization/CassetteRandomization.Tests.csproj -c Release
```

Expected: FAIL because `CassetteSaveEpochRuntime`, `CassettePersistToken`, and epoch-scoped behavior do not exist, and because the old process-wide terminal assertions disagree with the new requirements.

- [ ] **Step 3: Implement the minimal pure epoch model**

Implement a runtime with these invariants:

```csharp
internal sealed class CassetteSaveEpochRuntime
{
    private readonly HashSet<string> _owned = new(StringComparer.Ordinal);
    private readonly HashSet<string> _satisfiedThisEpoch = new(StringComparer.Ordinal);
    private readonly HashSet<string> _inFlightThisEpoch = new(StringComparer.Ordinal);

    internal long Epoch { get; private set; }
    internal int? ActiveSlot { get; private set; }
    internal bool HasActiveSave => ActiveSlot.HasValue;

    internal void ActivateSave(int slot)
    {
        Epoch++;
        ActiveSlot = slot;
        _satisfiedThisEpoch.Clear();
        _inFlightThisEpoch.Clear();
    }

    internal void DeactivateSave()
    {
        ActiveSlot = null;
        _satisfiedThisEpoch.Clear();
        _inFlightThisEpoch.Clear();
    }
}
```

`BeginPersist` returns an empty token without an active save. Otherwise it snapshots epoch, slot, and the owned songs that are neither satisfied nor in flight. `CompletePersist` ignores stale epoch/slot tokens and marks only authoritative `HAVE_IN_BAG` or `HAVE_DEPOSITED` results satisfied. Unknown/read failures remain pending.

- [ ] **Step 4: Run focused tests to verify GREEN**

Run the command from Step 2.

Expected: PASS with every epoch and transaction-state case named in output.

- [ ] **Step 5: Commit Task 1**

```powershell
git add client/CassetteRandomizationPolicy.cs client/tests/CassetteRandomization/Program.cs
git commit -m "fix(client): scope cassette receipts to loaded saves"
```

---

### Task 2: Native Save-Selection and Persistence Adapters

**Files:**
- Create: `client/CassetteSaveTransactionAdapter.cs`
- Modify: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Consumes: `CassettePersistToken` and existing `CassetteNativeRequestFactory`.
- Produces: `TryGetLoadedSave(out int slot)`, `TryReadCassetteStatus(object processor, string nativeSong, out string? nativeStatus)`, `TryGetPersistBundle(object request, out object? nativeBundle, out string bundleName)`, and `TryStageHaveInBag(object processor, string nativeSong, object nativeBundle, out string detail)`.
- Produces no save-write or flush operation.

- [ ] **Step 1: Write failing native-shaped adapter tests**

Create managed fakes with the exact public shapes used by generated interop types:

```csharp
sealed class FakeSaveManagementEnquiries
{
    public static int? SelectedSlot;
    public static object? SelectedState;
    public static int? GetSelectedSaveFileSlotNumber() => SelectedSlot;
    public static object? TryGetSelectedSlotSaveFileState() => SelectedState;
}

sealed class FakePersistSaveChangeBundleRequest
{
    public FakeBundle? Bundle { get; init; }
}
```

Assert that selection requires both slot and state; nullable bundles resolve safely; single-bundle persistence preserves the exact native bundle object; persist-all resolves `DEFAULT`; and staging uses the semantic cassette constructor with the passed bundle.

Add a source audit that fails if the adapter contains any of:

```text
PersistAllChangesInBundle
RequestWriteForPlayerSave
SaveDataManager
WritePlayerSaveFile
SelectedPlayerSaveSlotChangedEvent
```

- [ ] **Step 2: Run the focused test and capture RED**

Run:

```powershell
dotnet run --project client/tests/CassetteRandomization/CassetteRandomization.Tests.csproj -c Release
```

Expected: FAIL because `CassetteSaveTransactionAdapter.cs` and its interfaces do not exist.

- [ ] **Step 3: Implement the reflection adapter**

Resolve types and members by exact name, cache only `Type`/`MethodInfo`/`ConstructorInfo`, and never retain an IL2CPP save-state wrapper between calls. `TryGetLoadedSave` must query slot and state together and return false if either is missing. `TryStageHaveInBag` must call:

```csharp
CassetteNativeRequestFactory.Create(
    requestType,
    songType,
    cassetteStatusType,
    bundleType,
    nativeSong,
    "HAVE_IN_BAG",
    nativeBundle);
```

It may invoke the existing `PlayerSaveRequestProcessor.ProcessRequest` overload but must not invoke persistence itself.

- [ ] **Step 4: Run focused tests to verify GREEN**

Run the command from Step 2.

Expected: PASS, including prohibited-API audits.

- [ ] **Step 5: Commit Task 2**

```powershell
git add client/CassetteSaveTransactionAdapter.cs client/tests/CassetteRandomization/Program.cs
git commit -m "fix(client): adapt cassette grants to native save bundles"
```

---

### Task 3: Hook Safe Save Lifecycle Boundaries

**Files:**
- Modify: `client/Plugin.cs`
- Modify: `client/tests/CassetteRandomization/Program.cs`

**Interfaces:**
- Consumes: `CassetteSaveEpochRuntime` and `CassetteSaveTransactionAdapter`.
- Produces Harmony callbacks for save-selection postfixes and natural-persist prefix/postfix pairs.
- Preserves existing AP receipt, source interception, and machine-deposit behavior.

- [ ] **Step 1: Write failing production-wiring tests**

Require hooks for these exact request types:

```text
SelectPlayerSaveSlotRequest
SelectMostRecentlyUsedRegularPlayerSaveSlotRequest
EnsureAPlayerSaveSlotIsSelectedRequest
CreateNewPlayerSaveFileInSlotRequest
PersistSaveChangeBundleRequest
PersistAllSaveChangeBundlesRequest
```

Assert that selection postfixes call one verifier that activates a new epoch only after `TryGetLoadedSave` succeeds. Assert that persist prefixes capture an epoch token, re-read before staging, pass the natural bundle, and allow the original method to run. Assert postfixes verify only when token epoch and slot still match.

Assert `TickPendingNativeGrants` performs observation/logging only and contains no cassette request submission.

- [ ] **Step 2: Run the focused test and capture RED**

Run:

```powershell
dotnet run --project client/tests/CassetteRandomization/CassetteRandomization.Tests.csproj -c Release
```

Expected: FAIL because selection/persist hooks and transaction callbacks are absent and periodic reconciliation still submits requests.

- [ ] **Step 3: Install safe selection hooks**

Patch the relevant `SaveDataRequestProcessor.ProcessRequest` overloads by exact request parameter type. Postfixes call:

```csharp
if (CassetteSaveTransactionAdapter.TryGetLoadedSave(out int slot))
    CassetteReceiptRandomization.ActivateLoadedSave(slot, reason);
else
    CassetteReceiptRandomization.DeactivateLoadedSave(reason);
```

The same slot must still advance the epoch. Slot-data synchronization, room transitions, result events, and processor capture may request observation but cannot activate an epoch.

- [ ] **Step 4: Install natural persist prefix/postfix hooks**

For single-bundle persistence, prefix state captures the exact native bundle and the runtime token. For persist-all, prefix uses native `DEFAULT`. Before staging each song, authoritatively re-read it and skip bag/deposited states. The prefix never returns false and never replaces the native request.

The postfix schedules authoritative re-read on the next Unity tick and completes only the matching token. Remove request submission from the periodic keeper.

- [ ] **Step 5: Run focused tests to verify GREEN**

Run the command from Step 2.

Expected: PASS.

- [ ] **Step 6: Run the complete client verification**

Run all 17 client test projects using the repository's established cassette acceptance command, then:

```powershell
& .\client\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall
& .\tools\validate_repo.ps1
git diff --check
```

Expected: 17/17 test projects pass; Release build has 0 errors; validator passes; diff check is clean. Existing unrelated nullable warnings may remain documented but must not increase.

- [ ] **Step 7: Commit Task 3**

```powershell
git add client/Plugin.cs client/tests/CassetteRandomization/Program.cs
git commit -m "fix(client): persist cassettes with native save transactions"
```

---

### Task 4: Acceptance Evidence and Live Recovery

**Files:**
- Modify: `docs/testing/2026-08-29-full-cassette-acceptance.md`
- Modify: `docs/TESTING_AND_ISSUES.md`

**Interfaces:**
- Consumes the verified v0.68 client and existing v0.22 seed on `127.0.0.1:38281`, slot `Jack`.
- Produces auditable automated and live acceptance evidence; no merge/push/release.

- [ ] **Step 1: Record automated verification before installation**

Document commit IDs, focused RED/GREEN evidence, 17/17 suite result, no-install build result, validator result, and independent task-review verdicts.

- [ ] **Step 2: Obtain the existing installation boundary**

Confirm the game process is closed. Replace only:

```text
D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP\
```

Do not replace the APWorld because v0.22 seed/schema are unchanged.

- [ ] **Step 3: Verify every relaunch before gameplay**

Require fresh log lines for:

```text
[SCRC-AP] v0.68.0 loading.
[SCRC-AP] CASSETTE RECEIPT RECONCILIATION ENABLED entries=30.
[SCRC-AP] CASSETTE SOURCE RANDOMIZATION ENABLED entries=30 levelSources=25 chestSources=5.
[SCRC-AP] CONNECTED server=127.0.0.1:38281 slot='Jack'
```

- [ ] **Step 4: Recover Badass and The Heist without replaying checks**

Load the same save. AP history must keep both receipts pending until the loaded-save epoch is confirmed. Trigger one normal save boundary without replaying Level 1 or a Music Lab medal check. Confirm logs show staging inside that boundary and post-persist verification.

The owner confirms both cassettes appear in the top-right inventory.

- [ ] **Step 5: Verify normal insertion and deposited persistence**

Insert each cassette normally. Confirm the matching Music Lab levels unlock. Restart fully; require `HAVE_DEPOSITED`, no returned cassette, no repeated request, and no crash.

- [ ] **Step 6: Update documentation and commit**

Record pass/fail evidence precisely. Do not mark the overall cassette feature complete if the 32-point reused chest source or other representative matrix rows remain pending.

```powershell
git add docs/testing/2026-08-29-full-cassette-acceptance.md docs/TESTING_AND_ISSUES.md
git commit -m "docs: record cassette transaction acceptance"
```

---

## Final Verification Gate

After all tasks receive independent task review:

```powershell
dotnet run --project client/tests/CassetteRandomization/CassetteRandomization.Tests.csproj -c Release
& .\client\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall
& .\tools\validate_repo.ps1
git diff --check
git status --short
```

Then run one whole-branch review from the feature branch merge base through `HEAD`. Do not merge or push without explicit owner approval.
