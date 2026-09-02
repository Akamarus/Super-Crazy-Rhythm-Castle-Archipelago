# Game Garage Inserted-Cartridge State Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make AP-randomized Game Garage cartridges remain consumed after normal insertion, including after room travel, restart, reconnect, and switching local saves on the same Archipelago player slot, without pre-completing their randomized physical-source checks.

**Architecture:** Separate AP ownership, slot-scoped inserted state, and native bag state. A pure state machine controls grants and insertion detection; a narrow adapter reads and monotonically writes five `Scope.Slot` Archipelago data-storage keys; `ArchipelagoClient` owns connection generations and initial synchronization; and the Unity keeper supplies authoritative Game Garage object-release and native bag transitions. Unknown server or native state always fails closed.

**Tech Stack:** C#/.NET 6 client, .NET 8 console policy tests, BepInEx 6 IL2CPP, Archipelago.MultiClient.Net 6.7.1 data storage, Unity lifecycle polling, PowerShell/Python repository validation.

**Spec:** `docs/superpowers/specs/2026-09-02-game-garage-inserted-cartridge-state-design.md`

## Global Constraints

- Keep client version `0.68.0`, APWorld version `0.22.0`, item IDs, location IDs, and slot-data schema unchanged. This is a client bug fix, not a content migration.
- Preserve the existing physical-source flow. Never write any native `*_COLLECTED` progression flag from an AP receipt, insertion observation, data-storage read, or reconnect.
- Keep Vampire Killer entirely outside AP insertion reconciliation; its vanilla pickup and Game Garage entrance behavior must remain unchanged.
- Treat Archipelago received-item history as ownership only. It must never imply either native bag presence or terminal insertion.
- Use only the five approved `Scope.Slot` keys and write only `true`. Never write `false`, delete a key, or combine all cartridges into one shared object.
- Do not call Unity, IL2CPP, or native save APIs from Archipelago socket/data-storage callback threads. Callbacks update synchronized managed state and request later Unity-thread reconciliation only.
- Server-state reads are connection-generation scoped. A callback from a replaced or disconnected session must not make a newer generation ready or mutate its values.
- Initial synchronization is all-or-nothing: no AP cartridge bag grant until all five keys are known booleans for the current connection. Absent keys initialize to `false`; malformed/unreadable keys remain unknown.
- An insertion requires all evidence in one Garage visit: AP ownership, known server `false`, an authoritative held observation, corresponding object release, and an authoritative held-to-absent transition while still in `GameRoom_27`.
- Mark insertion in process memory before submitting the server write so reconciliation cannot resurrect the bag item during the write window.
- Current uncommitted diagnostic edits in `client/GarageCartridgeNativePolicy.cs`, `client/Plugin.cs`, `client/tests/GarageAvailability/Program.cs`, and `client/tests/PlantPipesReconciler/Program.cs` belong to this branch. Retain the processor-sharing fix and verified enum mapping only where the final design still needs them; remove the `HasGarageCartridgeBeenCollected` terminal-state experiment rather than committing it as architecture.
- Do not install, launch, merge, push, package, publish, or release while executing Tasks 1–5. Live deployment and acceptance require separate approval after automated verification and review.

---

### Task 1: Define the cartridge catalog keys and pure state machine

**Files:**
- Create: `client/GarageCartridgeInsertionState.cs`
- Modify: `client/GarageCartridgeNativePolicy.cs`
- Create: `client/tests/GarageCartridgePersistence/GarageCartridgePersistence.Tests.csproj`
- Create: `client/tests/GarageCartridgePersistence/Program.cs`
- Modify: `client/tests/GarageAvailability/Program.cs`

**Interfaces:**
- `GarageCartridgeNativeDefinition` gains `string ServerInsertionKey` and continues to carry `Song`, `ItemName`, `RelativePath`, `NativeBagFlag`, `NativeCartridgeType`, and `UsesPhysicalVanillaEntrance`.
- `GarageInsertionServerValue`: `Unknown`, `NotInserted`, `Inserted`.
- `GarageNativeGrantDecision`: `None`, `WaitForServer`, `WaitForNativeRead`, `AlreadyHeld`, `AlreadyInserted`, `ApplyBagItem`.
- `GarageInsertionObservation`: immutable input containing compatibility, AP ownership, physical-vanilla status, server value, Garage-room state, released-this-visit state, previous authoritative bag observation, and current authoritative bag observation.
- `GarageCartridgeInsertionPolicy.DecideGrant(...)` and `GarageCartridgeInsertionPolicy.ShouldRecordInsertion(...)` remain pure and have no Archipelago, reflection, or Unity references.

- [ ] **Step 1: Add a focused test project that links only pure production files**

Create `client/tests/GarageCartridgePersistence/GarageCartridgePersistence.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="..\..\GarageCartridgeNativePolicy.cs" Link="GarageCartridgeNativePolicy.cs" />
    <Compile Include="..\..\GarageCartridgeInsertionState.cs" Link="GarageCartridgeInsertionState.cs" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Write strict RED tests for catalog identity and grant gating**

In `client/tests/GarageCartridgePersistence/Program.cs`, add local `Equal`, `True`, and `False` assertions and prove:

```csharp
string[] expectedKeys =
{
    "Bloody Tears|scrc:garage_inserted:v1:bloody_tears",
    "Gradius Remix|scrc:garage_inserted:v1:gradius_remix",
    "Smooch|scrc:garage_inserted:v1:smooch",
    "Superstar|scrc:garage_inserted:v1:superstar",
    "Wag the Dog|scrc:garage_inserted:v1:wag_the_dog",
};

Equal(
    string.Join("\n", expectedKeys),
    string.Join("\n", GarageCartridgeNativePolicy.RandomizedCartridges.Select(x =>
        $"{x.Song}|{x.ServerInsertionKey}")),
    "the five versioned slot keys are exact and deterministic");

Equal(GarageNativeGrantDecision.WaitForServer,
    GarageCartridgeInsertionPolicy.DecideGrant(
        compatible: true, apOwned: true, usesPhysicalVanillaEntrance: false,
        serverValue: GarageInsertionServerValue.Unknown,
        nativeBagReadable: true, nativeBagHeld: false),
    "unknown server state fails closed");

Equal(GarageNativeGrantDecision.ApplyBagItem,
    GarageCartridgeInsertionPolicy.DecideGrant(
        compatible: true, apOwned: true, usesPhysicalVanillaEntrance: false,
        serverValue: GarageInsertionServerValue.NotInserted,
        nativeBagReadable: true, nativeBagHeld: false),
    "known not-inserted missing bag grants once");

Equal(GarageNativeGrantDecision.AlreadyInserted,
    GarageCartridgeInsertionPolicy.DecideGrant(
        compatible: true, apOwned: true, usesPhysicalVanillaEntrance: false,
        serverValue: GarageInsertionServerValue.Inserted,
        nativeBagReadable: true, nativeBagHeld: false),
    "inserted state is terminal");
```

Also test incompatible routing, no AP ownership, unreadable native bag, already-held bag, and a physical-vanilla cartridge. Explicitly assert that Vampire Killer is absent from `RandomizedCartridges` and has no server key.

- [ ] **Step 3: Write strict RED tests for insertion evidence**

Build observations proving all of these are false independently: missing bag outside Garage, missing bag without a previous authoritative held observation, missing bag before its Garage object was released, AP unowned, server unknown, server already inserted, incompatible routing, and physical-vanilla entrance.

The one true case must be:

```csharp
var inserted = new GarageInsertionObservation(
    Compatible: true,
    ApOwned: true,
    UsesPhysicalVanillaEntrance: false,
    ServerValue: GarageInsertionServerValue.NotInserted,
    InGarage: true,
    ReleasedThisVisit: true,
    PreviousBagReadable: true,
    PreviousBagHeld: true,
    CurrentBagReadable: true,
    CurrentBagHeld: false);

True(GarageCartridgeInsertionPolicy.ShouldRecordInsertion(inserted),
    "released AP cartridge held-to-absent transition records insertion");
```

- [ ] **Step 4: Run the new suite and capture RED**

```powershell
dotnet run --project client/tests/GarageCartridgePersistence/GarageCartridgePersistence.Tests.csproj -c Release
```

Expected: compilation fails because the new state types, policy, and server keys do not exist.

- [ ] **Step 5: Implement the minimal pure model and exact key mapping**

Create the enums, record, and two pure methods in `GarageCartridgeInsertionState.cs`. `DecideGrant` must check, in order: compatibility/AP/physical-vanilla eligibility, server readiness/value, native readability, inserted terminal state, held idempotence, then grant. `ShouldRecordInsertion` must require every approved observation and exactly `previous held && current absent`.

Add the five exact keys to the existing definitions. Do not add a key to Vampire Killer. Remove `nativeCartridgeRegistered` and `AlreadyRegistered` from the old grant policy rather than maintaining two competing terminal-state models.

- [ ] **Step 6: Update the existing mapping regression test**

Keep the verified `eRoom27GameCartridgeType` names in `GarageAvailability/Program.cs` as diagnostic metadata, append each server key to its expected mapping, and delete assertions that treat `HasGarageCartridgeBeenCollected` as terminal insertion state.

- [ ] **Step 7: Run both focused suites GREEN**

```powershell
dotnet run --project client/tests/GarageCartridgePersistence/GarageCartridgePersistence.Tests.csproj -c Release
dotnet run --project client/tests/GarageAvailability/GarageAvailability.Tests.csproj -c Release
```

Expected: both pass.

- [ ] **Step 8: Commit the pure state machine**

```powershell
git add client/GarageCartridgeInsertionState.cs client/GarageCartridgeNativePolicy.cs client/tests/GarageCartridgePersistence client/tests/GarageAvailability/Program.cs
git commit -m "test(client): define garage insertion state"
```

---

### Task 2: Build a generation-scoped slot-storage coordinator

**Files:**
- Create: `client/GarageCartridgeInsertionStorage.cs`
- Modify: `client/tests/GarageCartridgePersistence/GarageCartridgePersistence.Tests.csproj`
- Modify: `client/tests/GarageCartridgePersistence/Program.cs`

**Interfaces:**
- `IGarageInsertionDataStore.ReadAsync(string key, CancellationToken)` returns `Task<GarageInsertionReadResult>` where the result distinguishes `KnownFalse`, `KnownTrue`, `Malformed`, and `Failed`.
- `IGarageInsertionDataStore.WriteTrueAsync(string key, CancellationToken)` returns `Task<GarageInsertionWriteResult>` and succeeds only after a server callback plus authoritative `GetAsync<JToken>()` reread confirms boolean `true`.
- `GarageCartridgeInsertionCoordinator` owns one immutable key set, current connection generation, per-song server values, pending writes, and initial-sync readiness.
- Public coordinator operations are `BeginConnection(long generation, IGarageInsertionDataStore store)`, `EndConnection(long generation)`, `NoteInserted(string song)`, `RetryPendingWrites()`, `GetServerValue(string song)`, and `InitialSyncReady`.

- [ ] **Step 1: Add RED coordinator tests with a deterministic fake store**

Link `GarageCartridgeInsertionStorage.cs` into the test project. Implement a fake only in `Program.cs` that records reads/writes and completes them under test control. Prove:

1. `BeginConnection` requests all five exact keys once.
2. Four completed reads leave `InitialSyncReady == false`; the fifth valid boolean makes it true.
3. An absent key represented by `KnownFalse` becomes `NotInserted`.
4. Malformed or failed reads remain `Unknown`, keep the gate closed, and are retryable on a new connection generation.
5. A stale read completion from generation 1 cannot affect generation 2.
6. `NoteInserted` changes in-memory state to `Inserted` before a write completes.
7. Duplicate `NoteInserted` calls submit at most one active write.
8. A failed write remains pending; reconnect retries it.
9. A successful confirmed write clears pending state and logs/raises one durable transition.
10. Server `true` after simulated restart prevents the state returning to not-inserted.

- [ ] **Step 2: Run the focused suite and capture RED**

```powershell
dotnet run --project client/tests/GarageCartridgePersistence/GarageCartridgePersistence.Tests.csproj -c Release
```

Expected: compilation fails because storage results, interface, and coordinator are missing.

- [ ] **Step 3: Implement the minimal coordinator without Archipelago library references**

Keep the coordinator pure managed C#. Use a monotonically increasing `long generation` supplied by the caller. Every continuation checks the captured generation under a private lock before mutating state. Use a per-song state record:

```csharp
internal sealed record GarageInsertionEntryState(
    GarageInsertionServerValue ServerValue,
    bool InitialReadComplete,
    bool WritePending,
    bool WriteInFlight,
    bool DurableLogged);
```

`NoteInserted` must synchronously set `ServerValue = Inserted` and `WritePending = true`, then start/queue the write. A write failure clears only `WriteInFlight`; it must not clear `Inserted` or `WritePending`. `EndConnection` cancels that generation and leaves monotonic pending writes available for the next compatible connection.

- [ ] **Step 4: Implement the production Archipelago adapter in the same file**

Add `ArchipelagoGarageInsertionDataStore`, wrapping the authenticated `ArchipelagoSession`. Read each key only through `session.DataStorage[Scope.Slot, key]`:

```csharp
DataStorageElement element = _session.DataStorage[Scope.Slot, key];
element.Initialize(JToken.FromObject(false));
JToken value = await element.GetAsync().ConfigureAwait(false);
```

Accept only `JTokenType.Boolean`; absent keys become boolean `false` because `Initialize` does not overwrite existing values. For writes, use the library's one-shot callback and then reread:

```csharp
var callbackCompletion = new TaskCompletionSource<bool>(
    TaskCreationOptions.RunContinuationsAsynchronously);
DataStorageElement write = true;
_session.DataStorage[Scope.Slot, key] = write + Callback.Add(
    (original, current, context) =>
        callbackCompletion.TrySetResult(current.Type == JTokenType.Boolean && current.Value<bool>()));

bool callbackTrue = await callbackCompletion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
JToken confirmed = await _session.DataStorage[Scope.Slot, key].GetAsync().ConfigureAwait(false);
```

Return success only when both the callback and reread are boolean `true`. Catch socket, cancellation, conversion, and malformed-value failures into typed results; do not throw them onto the Unity thread. Do not log passwords, connection URIs containing credentials, or raw slot data.

- [ ] **Step 5: Run the focused suite GREEN and compile the real client**

```powershell
dotnet run --project client/tests/GarageCartridgePersistence/GarageCartridgePersistence.Tests.csproj -c Release
dotnet build client/RhythmCastleAP.csproj -c Release -p:GameDir='D:\SteamLibrary\steamapps\common\Titus'
```

Expected: coordinator tests pass and the Archipelago 6.7.1 adapter compiles against the installed package API.

- [ ] **Step 6: Commit the storage layer**

```powershell
git add client/GarageCartridgeInsertionStorage.cs client/tests/GarageCartridgePersistence
git commit -m "feat(client): persist garage insertion state by AP slot"
```

---

### Task 3: Bind synchronization to the Archipelago connection lifecycle

**Files:**
- Modify: `client/Plugin.cs` (`ArchipelagoClient`, `GarageCartridgeAccess`)
- Modify: `client/tests/GarageCartridgePersistence/Program.cs`
- Modify: `client/tests/PlantPipesReconciler/Program.cs`

**Interfaces:**
- `ArchipelagoClient` owns `long _connectionGeneration` and calls `GarageCartridgeAccess.BeginServerSync(session, generation)` only after successful compatible slot-data application.
- Socket close, failed login, replaced session, and deliberate shutdown call `GarageCartridgeAccess.EndServerSync(generation)`.
- `GarageCartridgeAccess` stores AP ownership immediately but delegates inserted values/readiness/pending writes to one `GarageCartridgeInsertionCoordinator`.
- Native reconciliation remains Unity-dispatched through the existing keeper; network continuations call `RequestUnityReconciliation` rather than native APIs.

- [ ] **Step 1: Add RED production-wiring assertions**

Extend the focused test to inspect `client/Plugin.cs` as text and require:

- a generation increment for every new session attempt;
- `BeginServerSync(session, generation)` after `ApplySlotData` and successful login;
- `EndServerSync(generation)` on current-session socket close, login failure/exception cleanup, replaced-session cleanup, and `Shutdown`;
- no call to `TryFlushPendingNativeGrants` directly from a data-storage callback;
- `GarageCartridgeAccess.Configure()` resets server synchronization and bag observations without clearing a pending monotonic write belonging to an active compatible AP session.

Update `PlantPipesReconciler/Program.cs` to preserve its existing assertion that `PlantPipesRandomization.EnsureProcessorAvailable()` shares a discovered `PlayerSaveRequestProcessor` with `GarageCartridgeAccess`.

- [ ] **Step 2: Run focused suites and capture RED**

```powershell
dotnet run --project client/tests/GarageCartridgePersistence/GarageCartridgePersistence.Tests.csproj -c Release
dotnet run --project client/tests/PlantPipesReconciler/PlantPipesReconciler.Tests.csproj -c Release
```

Expected: Garage lifecycle assertions fail before production wiring exists; Plant Pipes remains green.

- [ ] **Step 3: Wire a unique generation around each session**

At session creation, capture `long generation = Interlocked.Increment(ref _connectionGeneration)`. Every socket handler must first prove both `IsCurrentSession(session)` and that its generation is current. After a successful login:

1. apply slot data;
2. if cartridge routing is compatible, construct `ArchipelagoGarageInsertionDataStore(session)`;
3. call `GarageCartridgeAccess.BeginServerSync(generation, store)`;
4. then set `_connected = true` and process normal reconciliation.

Item history may have already populated `OwnedSongs`; this is allowed. Native grant decisions must return `WaitForServer` until coordinator initial sync is ready.

On close/replacement/shutdown/failure, call `EndServerSync` for that generation. Ensure callbacks from the old session cannot update the coordinator after a reconnect.

- [ ] **Step 4: Replace the failed native registration experiment**

In `GarageCartridgeAccess.TryFlushPendingNativeGrants`:

- keep `TryReadProgressionFlag(cartridge.NativeBagFlag, out bool held)` as the only native bag truth;
- obtain `GarageInsertionServerValue` from the coordinator;
- call `GarageCartridgeInsertionPolicy.DecideGrant`;
- retain the current `_applyingArchipelagoGrant` guard and exact `TrySubmitProgressionFlag(..., true)` bag grant;
- remove `TryReadCartridgeCollected`, `HasGarageCartridgeBeenCollected`, and any use of `NativeCartridgeType` as terminal state;
- log `WaitForServer`, `WaitForNativeRead`, `AlreadyHeld`, `AlreadyInserted`, and applied-grant changes without per-frame spam.

Keep the processor-sharing call added to `PlantPipesRandomization.EnsureProcessorAvailable`; it is the proven fix that lets Garage receive the stateless processor discovered by another subsystem.

- [ ] **Step 5: Run focused and connection-regression suites GREEN**

```powershell
dotnet run --project client/tests/GarageCartridgePersistence/GarageCartridgePersistence.Tests.csproj -c Release
dotnet run --project client/tests/GarageAvailability/GarageAvailability.Tests.csproj -c Release
dotnet run --project client/tests/PlantPipesReconciler/PlantPipesReconciler.Tests.csproj -c Release
dotnet run --project client/tests/ReconnectPolicy/ReconnectPolicy.Tests.csproj -c Release
dotnet build client/RhythmCastleAP.csproj -c Release -p:GameDir='D:\SteamLibrary\steamapps\common\Titus'
```

Expected: all pass.

- [ ] **Step 6: Commit connection lifecycle integration**

```powershell
git add client/Plugin.cs client/tests/GarageCartridgePersistence/Program.cs client/tests/PlantPipesReconciler/Program.cs
git commit -m "fix(client): gate garage grants on slot state"
```

---

### Task 4: Detect a real Garage insertion and suppress resurrection immediately

**Files:**
- Modify: `client/Plugin.cs` (`GarageCartridgeAccess`, `GarageCartridgeAccessKeeper`)
- Modify: `client/tests/GarageCartridgePersistence/Program.cs`
- Modify: `client/tests/GarageAvailability/Program.cs`

**Interfaces:**
- `GarageCartridgeAccess.ObserveNativeBag(string song, bool readable, bool held, bool inGarage, bool releasedThisVisit)` owns per-save bag observation and calls the pure insertion policy.
- `GarageCartridgeAccess.ResetNativeBagObservations(string reason)` runs on room transition, save lifecycle reset, disconnect, and processor replacement.
- `GarageCartridgeAccess.NoteGarageObjectReleased(string song)` records only the current `GameRoom_27` visit.
- `GarageCartridgeInsertionCoordinator.NoteInserted(song)` provides immediate in-memory terminal state and queues the monotonic server write.

- [ ] **Step 1: Add RED sequence tests**

Use a small managed `GarageCartridgeInsertionTracker` (or the coordinator if it remains pure enough) to test full sequences rather than only individual policy calls:

1. AP ownership + known false + missing bag outside Garage requests a native grant but never records insertion.
2. First observation missing in Garage does not record insertion.
3. Held in Music Lab, then missing in Garage before release does not record insertion.
4. Held in Garage, object released, then absent records exactly once.
5. Immediately after record and before write completion, grant decision is `AlreadyInserted`.
6. Repeated absent polls, object deactivation, room exit, and duplicate server callbacks do not submit duplicate writes.
7. Save observation reset removes held/released evidence but retains AP ownership and server insertion state.
8. Vampire Killer never arms or records.

Add a source-safety assertion over production source text that no Garage AP insertion/grant method submits any flag ending `_COLLECTED`.

- [ ] **Step 2: Run the focused suite and capture RED**

```powershell
dotnet run --project client/tests/GarageCartridgePersistence/GarageCartridgePersistence.Tests.csproj -c Release
```

Expected: sequence tests fail because the tracker/runtime integration does not exist.

- [ ] **Step 3: Feed authoritative native observations from the Unity keeper**

During the keeper's one-second native reconciliation poll, read each randomized cartridge's exact `NativeBagFlag`. Preserve readability separately from its boolean value. During the 0.15-second Garage object poll, call `NoteGarageObjectReleased` only after the real object has been activated/released for that visit.

For every authoritative bag read, call:

```csharp
GarageCartridgeAccess.ObserveNativeBag(
    cartridge.Song,
    readable: true,
    held,
    inGarage: string.Equals(room, GarageRoomId, StringComparison.Ordinal),
    releasedThisVisit: _releasedThisVisit.Contains(cartridge.Song));
```

Do not infer absence when reflection fails. Clear only visit-local release and bag-history evidence on room/save transitions.

- [ ] **Step 4: Implement the insertion transition**

When the policy returns true:

1. call `coordinator.NoteInserted(song)` while holding only managed-state locks;
2. clear that song's pending native-grant decision;
3. log `native consumption observed` and `server insertion write pending`;
4. request later Unity reconciliation;
5. when the coordinator reports confirmed `true`, log `server insertion state confirmed durable` exactly once.

Never reactivate a consumed Garage object, regrant its native bag flag, or call native save APIs from the write continuation. `HasCartridge(song)` continues to mean AP ownership for Garage availability, so the inserted song remains playable.

- [ ] **Step 5: Preserve source checks and vanilla entrance behavior**

Keep `ShouldSuppressVanillaSourceGrant` scoped to randomized bag-item writes outside `GameRoom_27`. Keep `RecordVanillaSourceCollected` as the sole handler for native `*_COLLECTED` source events and its existing AP location mapping. Confirm Vampire Killer's source grant is not suppressed under v0.21/v0.22-compatible slot data.

- [ ] **Step 6: Run all cartridge-focused tests and client build GREEN**

```powershell
dotnet run --project client/tests/GarageCartridgePersistence/GarageCartridgePersistence.Tests.csproj -c Release
dotnet run --project client/tests/GarageAvailability/GarageAvailability.Tests.csproj -c Release
dotnet run --project client/tests/CassetteRandomization/CassetteRandomization.Tests.csproj -c Release
dotnet build client/RhythmCastleAP.csproj -c Release -p:GameDir='D:\SteamLibrary\steamapps\common\Titus'
```

Expected: all pass with no native `*_COLLECTED` write path.

- [ ] **Step 7: Commit the insertion detector**

```powershell
git add client/Plugin.cs client/tests/GarageCartridgePersistence/Program.cs client/tests/GarageAvailability/Program.cs
git commit -m "fix(client): record garage cartridge insertion"
```

---

### Task 5: Document diagnostics and complete automated verification

**Files:**
- Modify: `docs/PROJECT_OVERVIEW.md`
- Modify: `docs/NEXT_RELEASE_BUG_FIXES.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/superpowers/plans/2026-09-02-game-garage-inserted-cartridge-state.md`

- [ ] **Step 1: Document candidate behavior without claiming live completion**

Add a concise Game Garage persistence section covering:

- the five randomized cartridges use AP slot-scoped inserted state;
- inserting one consumes its native bag representation permanently for that AP player slot;
- switching local save slots does not restore an inserted cartridge;
- physical cartridge sources remain AP checks;
- Vampire Killer remains vanilla;
- this fix is a candidate until the full Superstar live test passes.

Add the exact useful log markers testers should include in issue reports:

```text
GAME GARAGE INSERTION SYNC pending|ready|failed
GAME GARAGE CARTRIDGE NATIVE GRANT APPLIED
GAME GARAGE INSERTION CANDIDATE ARMED
GAME GARAGE NATIVE CONSUMPTION OBSERVED
GAME GARAGE SERVER INSERTION WRITE PENDING
GAME GARAGE SERVER INSERTION CONFIRMED DURABLE
GAME GARAGE ALREADY INSERTED NO REGRANT
```

Do not claim fixed/released or change public installation instructions yet.

- [ ] **Step 2: Run every client console suite**

```powershell
$projects = Get-ChildItem -LiteralPath client/tests -Recurse -Filter *.csproj | Sort-Object FullName
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { throw "Client test failed: $($project.FullName)" }
}
```

Expected: all 18 projects pass (the current 17 plus `GarageCartridgePersistence`). Record the actual count in this plan's completion record rather than trusting the expected count if repository contents changed.

- [ ] **Step 3: Run the APWorld suite and repository validator**

```powershell
py -m unittest discover apworld/tests -v
py tools/validate-repo.py
```

Expected: all APWorld tests pass and validation reports no client/APWorld contract or permanent-ID regressions.

- [ ] **Step 4: Run a clean Release build without deployment**

```powershell
dotnet clean client/RhythmCastleAP.csproj -c Release -p:GameDir='D:\SteamLibrary\steamapps\common\Titus'
dotnet build client/RhythmCastleAP.csproj -c Release -p:GameDir='D:\SteamLibrary\steamapps\common\Titus'
```

Expected: build succeeds. Do not copy the result into `BepInEx\plugins\RhythmCastleAP` during this task.

- [ ] **Step 5: Inspect the final diff for scope and unsafe state writes**

```powershell
git diff --check
git status --short
rg -n "garage_inserted|HasGarageCartridgeBeenCollected|_COLLECTED|Scope\.Slot" client docs
```

Manually verify:

- only the five approved keys exist;
- every data-storage assignment writes `true`;
- `HasGarageCartridgeBeenCollected` is absent from terminal reconciliation;
- no new Garage grant/insertion path writes a native `_COLLECTED` flag;
- unrelated files and the main checkout's untracked `WordFactori/` are untouched.

- [ ] **Step 6: Commit documentation and completion evidence**

Update this plan with the exact commands, pass counts, build result, and commit hashes. Then commit only documentation changes:

```powershell
git add docs/PROJECT_OVERVIEW.md docs/NEXT_RELEASE_BUG_FIXES.md docs/TESTING.md docs/superpowers/plans/2026-09-02-game-garage-inserted-cartridge-state.md
git commit -m "docs: add garage insertion persistence testing"
```

---

### Task 6: Independent review and separately approved live acceptance

**Files:**
- Review only unless a finding requires a new TDD repair cycle.
- Update after evidence: `docs/superpowers/plans/2026-09-02-game-garage-inserted-cartridge-state.md`
- Update after evidence: `docs/NEXT_RELEASE_BUG_FIXES.md`
- Update after evidence: `docs/TESTING.md`

- [ ] **Step 1: Request independent code review**

Use `superpowers:requesting-code-review`. The reviewer must compare the entire branch against the approved design and specifically challenge connection-generation races, callback thread safety, false insertion detection, monotonic write retry, source-check preservation, and Vampire Killer exclusion.

- [ ] **Step 2: Resolve findings with test-first changes**

Use `superpowers:receiving-code-review`. Reproduce every accepted defect with a failing focused test before changing production code, rerun affected focused suites, and commit each coherent repair. Do not dismiss a finding solely because existing tests pass.

- [ ] **Step 3: Re-run Task 5 verification after review**

Repeat all client tests, all APWorld tests, repository validation, clean Release build, `git diff --check`, and final state-write inspection. Record new totals and hashes.

- [ ] **Step 4: Stop and request approval before live deployment**

The game must be closed. After approval, replace only:

```text
D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP\
```

with the reviewed Release build. Do not replace the APWorld, seed, server configuration, saves, or any other plugin directory.

- [ ] **Step 5: Execute the decisive Superstar acceptance sequence**

With a fresh compatible v0.22 seed and fresh local save:

1. deliver Superstar Cartridge through Archipelago;
2. confirm it appears in the top-right inventory;
3. enter Game Garage normally and confirm Superstar becomes available;
4. leave Game Garage and confirm Superstar does not return to inventory;
5. confirm the log contains the server-confirmed durable marker;
6. close and relaunch, reconnect, and load the same local save;
7. confirm Superstar remains absent from inventory and available in Game Garage;
8. load a different local save for the same AP slot and confirm Superstar is still treated as inserted;
9. reach the physical Superstar source, confirm it still exists until collected, and confirm collecting it sends exactly its AP location check.

Any regrant, missing song, absent source, premature source check, unknown-state grant, or missing durable confirmation fails acceptance.

- [ ] **Step 6: Run the remaining cartridge regression matrix later**

Repeat inventory → insertion → room exit → restart/reconnect → different-save behavior for Bloody Tears, Gradius Remix, Smooch, and Wag the Dog. Separately confirm Vampire Killer's physical pickup, inventory, and Game Garage entrance sequence remain vanilla.

- [ ] **Step 7: Record evidence and stop at the integration boundary**

Only after the Superstar sequence passes, change the bug entry from candidate to live-verified and record seed/archive, server port, player slot, local save slots, relevant log excerpts, build hash, and test counts. Commit the evidence. Do not merge, push, package, publish, or release without a new explicit user approval.
