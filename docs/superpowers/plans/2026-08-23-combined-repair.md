# Combined Repair Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver Client v0.67.63 with reliable AP startup, Roots presentation, difficulty, Star HUD, Music Lab, Game Garage, Level 22, and Plant Pipes repairs for one combined gameplay acceptance run.

**Architecture:** Keep native reflection and Harmony wiring in `client/Plugin.cs`, but move every decision into a small pure C# policy with a standalone console test project. Runtime changes activate only after compatible v0.18 slot data is synchronized and use exact room/object/flag identities; APWorld logic changes only where the repaired client proves physical reachability.

**Tech Stack:** C#/.NET 6 BepInEx IL2CPP plugin, pure .NET 8 console policy tests, Harmony reflection hooks, Python 3 Archipelago world, `unittest`, PowerShell build/deploy scripts.

**Spec:** `docs/superpowers/specs/2026-08-22-combined-repair-design.md`

## Global Constraints

- Target APWorld version remains `0.18`; target client version is exactly `0.67.63`.
- Preserve the v0.18 implementation tag and compatibility contract unless a new slot-data field is explicitly added by this plan.
- Runtime changes require AP enabled, successful login, compatible slot data, and an exact native identity.
- Vanilla behavior is authoritative when AP is disabled, incompatible, unsynchronized, or native identification fails.
- No new network IDs, AP Stars, Victory logic, progression items, automatic merge, push, or publication.
- Use TDD: observe every focused test fail before adding its production policy or integration.
- Install only to `<GameDir>\BepInEx\plugins\RhythmCastleAP` after all automated checks pass.
- Preserve unrelated user files and the unrelated untracked `WordFactori/` directory in the main checkout.

---

## File Structure

**Create focused policies:**

- `client/RootsPresentationPolicy.cs` — queued/verified initial Roots flag and post-Level-1 suppression decisions.
- `client/DifficultyAvailabilityPolicy.cs` — AP compatibility and Normal/Pro availability decisions.
- `client/StarHudPolicy.cs` — native v0.18 Star counter visibility decision.
- `client/MusicLabBarrierPolicy.cs` — exact AP-only barrier selection.
- `client/GarageAvailabilityPolicy.cs` — cartridge ownership versus initialization/interaction decisions.
- `client/LevelCompletionPolicy.cs` — internal-level mapping and cumulative ordinary tier decisions.

**Create focused tests:**

- `client/tests/RootsPresentation/RootsPresentation.Tests.csproj` and `Program.cs`.
- `client/tests/DifficultyAvailability/DifficultyAvailability.Tests.csproj` and `Program.cs`.
- `client/tests/StarHud/StarHud.Tests.csproj` and `Program.cs`.
- `client/tests/MusicLabBarrier/MusicLabBarrier.Tests.csproj` and `Program.cs`.
- `client/tests/GarageAvailability/GarageAvailability.Tests.csproj` and `Program.cs`.
- `client/tests/LevelCompletion/LevelCompletion.Tests.csproj` and `Program.cs`.
- `apworld/scrc/placement.py` — conservative required-item eligibility for tiered, Garage, and native-point locations.
- `apworld/tests/fixtures/bk_seed_28223804408101432968.json` — retained unsafe-placement regression facts.
- `docs/testing/2026-08-23-v06763-native-identities.md` — exact difficulty, HUD, barrier, and Garage identities captured before mutation.

**Modify integration and contracts:**

- `client/NewSaveDirectStartPolicy.cs` and `client/tests/NewSaveDirectStart/Program.cs` — add compatibility, room, and one-shot consumption semantics.
- `client/PlantPipesReconciler.cs` and `client/tests/PlantPipesReconciler/Program.cs` — add explicit persisted-result lifecycle coverage.
- `client/Plugin.cs` — Harmony/native integration only.
- `client/tests/DiagnosticHotkeyRouting.Tests.csproj` — exclude every nested focused test project.
- `apworld/scrc/__init__.py` and `apworld/tests/test_world_integration.py` — safe required-item placement and Level 22 registration contract.
- `tools/validate-repo.py`, `client/build.ps1`, `README.md`, `CHANGELOG.md`, `docs/INSTALL.md`, `docs/ROADMAP.md`, `docs/NEXT_RELEASE_BUG_FIXES.md`, and `docs/TESTING_AND_ISSUES.md` — exact versions, test status, and release truthfulness.

---

### Task 0: Capture exact native repair identities

**Files:**
- Create: `docs/testing/2026-08-23-v06763-native-identities.md`
- Read: `D:\SteamLibrary\steamapps\common\Titus\BepInEx\LogOutput.log`
- Read: retained screenshots and evidence referenced by `docs/TESTING_AND_ISSUES.md`

**Interfaces:**
- Produces: exact full type/member for Normal/Pro availability, exact Hub6 Star HUD path/component, exact orange-barrier paths, and the exact Garage selection component that can be disabled without deactivating cartridge roots.
- Consumed by: Tasks 3–6. Those tasks may not substitute guesses or broad scene scans.

- [ ] **Step 1: Run existing read-only diagnostics in the relevant rooms**

In Hub6, run the focused hierarchy/save probe and capture the visible Star HUD plus every orange construction barrier. In Game Garage, run the cartridge/component probe with zero owned cartridges. At the difficulty choice, capture the exact condition/enquiry and request types used to show and select Normal/Pro.

- [ ] **Step 2: Correlate logs with visible objects**

For every candidate, record the full hierarchy path, component type, active state, and visible object it controls. Reject candidates that cannot be correlated one-to-one with the UI or barrier seen by the project owner.

- [ ] **Step 3: Write the evidence record**

Use four tables headed `Difficulty`, `Star HUD`, `Music Lab barriers`, and `Game Garage`. Every accepted row contains `room`, `exact path or full type`, `member/method`, `vanilla state`, `desired AP state`, and `evidence log line`. List rejected candidates beneath the corresponding table.

- [ ] **Step 4: Verify the evidence is complete**

Run:

```powershell
rg -n "^## (Difficulty|Star HUD|Music Lab barriers|Game Garage)$|GameRoom_Hub6|GameRoom_27|Normal|Pro" docs/testing/2026-08-23-v06763-native-identities.md
```

Expected: all four headings and at least one accepted exact identity under each; the barrier table has one accepted row per visually distinct orange blocker.

- [ ] **Step 5: Commit**

```powershell
git add docs/testing/2026-08-23-v06763-native-identities.md
git commit -m "test: capture v0.67.63 native repair identities"
```

If any section cannot produce an accepted identity, stop that dependent task and keep its bug open; do not guess.

---

### Task 1: Harden new-save direct start

**Files:**
- Modify: `client/NewSaveDirectStartPolicy.cs`
- Modify: `client/tests/NewSaveDirectStart/Program.cs`
- Modify: `client/Plugin.cs` in `IntroRoomToHubRedirectPatches`

**Interfaces:**
- Consumes: `enabled`, `compatible`, `freshSavePending`, `alreadyRedirected`, and transition room ID.
- Produces: `NewSaveDirectStartPolicy.Decide(bool enabled, bool compatible, bool freshSavePending, bool alreadyRedirected, string roomId)` and `ShouldUseLegacyFallback(bool enabled, bool compatible, string roomId)`.

- [ ] **Step 1: Extend the policy test with exact startup cases**

```csharp
Equal(NewSaveDirectStartDecision.Redirect,
    NewSaveDirectStartPolicy.Decide(true, true, true, false, "GameRoom_04A"),
    "fresh compatible AP save");
Equal(NewSaveDirectStartDecision.Ignore,
    NewSaveDirectStartPolicy.Decide(true, false, true, false, "GameRoom_04A"),
    "incompatible slot");
Equal(NewSaveDirectStartDecision.Ignore,
    NewSaveDirectStartPolicy.Decide(true, true, true, false, "GameRoom_Hub2"),
    "unrelated transition");
True(NewSaveDirectStartPolicy.ShouldUseLegacyFallback(true, true, "GameRoom_04A"),
    "exact legacy rescue");
```

- [ ] **Step 2: Run the focused test and verify failure**

Run: `dotnet run --project client/tests/NewSaveDirectStart/NewSaveDirectStart.Tests.csproj -c Release`

Expected: FAIL because the extended signatures and fallback policy do not exist.

- [ ] **Step 3: Implement the minimal exact-room policy**

```csharp
internal const string IntroRoomId = "GameRoom_04A";

internal static NewSaveDirectStartDecision Decide(
    bool enabled, bool compatible, bool freshSavePending,
    bool alreadyRedirected, string roomId) =>
    enabled && compatible && freshSavePending && !alreadyRedirected &&
    string.Equals(roomId, IntroRoomId, StringComparison.OrdinalIgnoreCase)
        ? NewSaveDirectStartDecision.Redirect
        : NewSaveDirectStartDecision.Ignore;
```

Implement `ShouldUseLegacyFallback` with the same enabled/compatible/exact-room boundary.

- [ ] **Step 4: Integrate and run the focused test**

Update `NewSaveCreatedPostfix` to arm only during a synchronized compatible AP session. Consume the one-shot token only when `Decide` returns `Redirect`; use `ShouldUseLegacyFallback` for established-save rescue.

Run: `dotnet run --project client/tests/NewSaveDirectStart/NewSaveDirectStart.Tests.csproj -c Release`

Expected: PASS with `New-save direct-start policy tests passed.`

- [ ] **Step 5: Commit**

```powershell
git add client/NewSaveDirectStartPolicy.cs client/tests/NewSaveDirectStart/Program.cs client/Plugin.cs
git commit -m "fix: harden AP new-save direct start"
```

---

### Task 2: Queue and verify Roots presentation suppression

**Files:**
- Create: `client/RootsPresentationPolicy.cs`
- Create: `client/tests/RootsPresentation/RootsPresentation.Tests.csproj`
- Create: `client/tests/RootsPresentation/Program.cs`
- Modify: `client/Plugin.cs` in `RootsAreaBaselineKeeper`, `EarlySequenceBlockerPatches`, and the Unity update keeper
- Modify: `client/tests/DiagnosticHotkeyRouting.Tests.csproj`

**Interfaces:**
- Produces: `RootsIntroDecision DecideIntro(RootsIntroSnapshot snapshot)` where decisions are `Vanilla`, `QueueFlag`, `Wait`, `AllowTransition`, and `FallbackVanilla`.
- Produces: `bool ShouldSuppressPostLevelOne(bool enabled, bool compatible, string sequenceType, bool levelOnePersisted)`.

- [ ] **Step 1: Create a failing pure-policy suite**

Test snapshots for incompatible AP, missing processor, queued flag, verified flag, retry exhaustion, wrong room, and post-Level-1 sequence identity. Assert missing processor returns `QueueFlag`, a queued-but-unverified flag returns `Wait`, verified `ROOTS_HUB_INTRO_WITNESSED` returns `AllowTransition`, and timeout returns `FallbackVanilla`.

- [ ] **Step 2: Run and verify the missing-file failure**

Run: `dotnet run --project client/tests/RootsPresentation/RootsPresentation.Tests.csproj -c Release`

Expected: FAIL because `RootsPresentationPolicy.cs` does not exist.

- [ ] **Step 3: Implement the pure policy and bounded retry schedule**

```csharp
internal readonly record struct RootsIntroSnapshot(
    bool Enabled, bool Compatible, bool EnteringRoots,
    bool NativeFlagOwned, bool ProcessorAvailable,
    bool SubmissionOutstanding, int RetryCount);

internal static TimeSpan? RetryDelay(int retryCount) => retryCount switch
{
    0 => TimeSpan.Zero,
    1 => TimeSpan.FromMilliseconds(50),
    2 => TimeSpan.FromMilliseconds(100),
    3 => TimeSpan.FromMilliseconds(200),
    _ => null,
};
```

- [ ] **Step 4: Integrate deferred transition and post-Level-1 suppression**

Capture the pending Hub2 transition request, submit only `ROOTS_HUB_INTRO_WITNESSED=True` on Unity's main thread, verify through selected-save enquiries, and replay the captured transition once. Guard replay with `[ThreadStatic]` re-entry state. On retry exhaustion, replay vanilla and log `ROOTS INTRO CUTSCENE BYPASS FALLBACK`.

Make the existing difficulty/gate sequence suppression depend on the exact post-Level-1 event instead of generic Level 2 ownership. Preserve result persistence and later Roots quests.

- [ ] **Step 5: Run focused and existing routing tests**

Run:

```powershell
dotnet run --project client/tests/RootsPresentation/RootsPresentation.Tests.csproj -c Release
dotnet run --project client/tests/DiagnosticHotkeyRouting.Tests.csproj -c Release
```

Expected: both PASS.

- [ ] **Step 6: Commit**

```powershell
git add client/RootsPresentationPolicy.cs client/tests/RootsPresentation client/tests/DiagnosticHotkeyRouting.Tests.csproj client/Plugin.cs
git commit -m "fix: suppress Roots presentations before transition"
```

---

### Task 3: Expose Normal and Pro without forcing selection

**Files:**
- Create: `client/DifficultyAvailabilityPolicy.cs`
- Create: `client/tests/DifficultyAvailability/DifficultyAvailability.Tests.csproj`
- Create: `client/tests/DifficultyAvailability/Program.cs`
- Modify: `client/Plugin.cs` near `AssignAndCommentOnMusicDifficultySequenceStep.Begin` and difficulty request hooks
- Modify: `client/tests/DiagnosticHotkeyRouting.Tests.csproj`

**Interfaces:**
- Produces: `DifficultyAvailabilityDecision Decide(bool enabled, bool compatible, bool normalAvailable, bool proAvailable)` with `Vanilla`, `ExposeBoth`, and `AlreadyAvailable`.

- [ ] **Step 1: Write failing cases**

Assert compatible AP returns `ExposeBoth` when either option is unavailable, `AlreadyAvailable` when both are available, and `Vanilla` when disabled or incompatible. Assert the decision contains no selected difficulty value.

- [ ] **Step 2: Run and verify failure**

Run: `dotnet run --project client/tests/DifficultyAvailability/DifficultyAvailability.Tests.csproj -c Release`

Expected: FAIL because the policy is absent.

- [ ] **Step 3: Add the minimal policy and integration**

Implement the enum and pure decision. In `Plugin.cs`, patch only the accepted full type/member recorded under `Difficulty` in Task 0. Force only its availability booleans for Normal and Pro during compatible AP sessions; never submit `SetPlayerTrackingDifficultyRequest` unless the player chooses normally.

- [ ] **Step 4: Run the focused test**

Run: `dotnet run --project client/tests/DifficultyAvailability/DifficultyAvailability.Tests.csproj -c Release`

Expected: PASS and no test source contains an automatic selection call.

- [ ] **Step 5: Commit**

```powershell
git add client/DifficultyAvailabilityPolicy.cs client/tests/DifficultyAvailability client/tests/DiagnosticHotkeyRouting.Tests.csproj client/Plugin.cs
git commit -m "fix: expose AP difficulty choices at startup"
```

---

### Task 4: Preserve the native v0.18 Star HUD

**Files:**
- Create: `client/StarHudPolicy.cs`
- Create: `client/tests/StarHud/StarHud.Tests.csproj`
- Create: `client/tests/StarHud/Program.cs`
- Modify: `client/Plugin.cs` around Hub HUD activation/progression hooks
- Modify: `client/tests/DiagnosticHotkeyRouting.Tests.csproj`

**Interfaces:**
- Produces: `StarHudDecision Decide(bool enabled, bool compatible, bool liveApStarsActive, bool isHubRoom, bool nativeHudVisible)`.
- v0.18 output is `ShowNative` only when AP is compatible, live AP Stars are inactive, and the player is in a hub room.

- [ ] **Step 1: Write failing visibility/source tests**

Cover compatible v0.18 hub, incompatible slot, non-hub room, already-visible HUD, and a future `liveApStarsActive=true` case that must return `DeferToApStars` rather than native normalization.

- [ ] **Step 2: Run and verify failure**

Run: `dotnet run --project client/tests/StarHud/StarHud.Tests.csproj -c Release`

Expected: FAIL because the policy is absent.

- [ ] **Step 3: Implement policy and exact HUD integration**

Keep only the accepted Task 0 Star HUD object/component active after fresh-save and level-result transitions. Read the native earned-Star total; do not synthesize or mutate saved Stars. Limit changes to compatible v0.18 sessions and that exact identity.

- [ ] **Step 4: Run focused test**

Run: `dotnet run --project client/tests/StarHud/StarHud.Tests.csproj -c Release`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add client/StarHudPolicy.cs client/tests/StarHud client/tests/DiagnosticHotkeyRouting.Tests.csproj client/Plugin.cs
git commit -m "fix: preserve native Star HUD in AP v0.18"
```

---

### Task 5: Bypass exact Music Lab construction barriers

**Files:**
- Create: `client/MusicLabBarrierPolicy.cs`
- Create: `client/tests/MusicLabBarrier/MusicLabBarrier.Tests.csproj`
- Create: `client/tests/MusicLabBarrier/Program.cs`
- Modify: `client/Plugin.cs` in Hub6 scene lifecycle
- Modify: `client/tests/DiagnosticHotkeyRouting.Tests.csproj`
- Create: `apworld/scrc/placement.py`
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_world_integration.py`
- Create: `apworld/tests/fixtures/bk_seed_28223804408101432968.json`

**Interfaces:**
- Produces: `bool ShouldDisable(bool enabled, bool compatible, string roomId, string exactPath)`.
- Consumes: accepted `Music Lab barriers` rows from Task 0.
- Produces: immutable `KnownBarrierPaths` copied exactly from those rows; no substring matching.
- Produces: `required_progression_allowed(location_name: str, active_tier_locations: frozenset[str]) -> bool` in `apworld/scrc/placement.py`.

- [ ] **Step 1: Capture exact barrier paths without mutation**

Use the existing Hub6 diagnostic scan on the retained save to log the orange barriers' complete hierarchy paths and active state. Record only objects visually confirmed by the tester or already evidenced by the retained screenshot/log.

- [ ] **Step 2: Write the failing exact-path test**

For each captured path, assert true only for enabled+compatible+`GameRoom_Hub6`. Assert false for sibling paths containing `Barrier`, reward chests, cassette machines, other rooms, disabled AP, and incompatible slots.

- [ ] **Step 3: Run and verify failure**

Run: `dotnet run --project client/tests/MusicLabBarrier/MusicLabBarrier.Tests.csproj -c Release`

Expected: FAIL because the policy is absent.

- [ ] **Step 4: Implement exact-path policy and reversible keeper**

On Hub6 load, cache each matched object's vanilla `activeSelf`, disable it while compatible AP is active, and restore the cached value when leaving the room or disabling AP. Log every exact path once. Do not scan by name or touch colliders outside those roots.

- [ ] **Step 5: Add APWorld reachability regression assertions**

Create `bk_seed_28223804408101432968.json` with these retained unsafe facts: Lobby Access on Lets Go Platinum, Weed Killer on Vampire Killer Platinum without its cartridge, Plant Pipes on the 64-point chest, and required items behind barrier-blocked cassette groups. In `test_world_integration.py`, load the fixture and initially assert each placement is rejected by `required_progression_allowed`; expect failure because the helper does not exist. Also assert eligible cassette locations remain in the Music Lab region after the barrier bypass.

- [ ] **Step 6: Implement conservative APWorld placement eligibility**

Use `filter_locations_by_difficulty` to build the active tier-location set from the selected option. `required_progression_allowed` returns false for a tiered location absent from that set and for every `Music Lab - <N> Point Chest` until Music Lab Points are solver-modeled. Garage locations retained by the selected tier remain governed by their existing matching-cartridge access rule. Apply the helper as each tiered/chest location's `item_rule`, allowing `Stardust` at unsafe locations and rejecting every item whose classification includes progression.

```python
def filler_or_safe_required(item, safe_for_required: bool) -> bool:
    return safe_for_required or not bool(item.classification & ItemClassification.progression)
```

- [ ] **Step 7: Run focused and APWorld tests**

Run:

```powershell
dotnet run --project client/tests/MusicLabBarrier/MusicLabBarrier.Tests.csproj -c Release
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
```

Expected: policy suite PASS and all APWorld tests PASS.

- [ ] **Step 8: Commit**

```powershell
git add client/MusicLabBarrierPolicy.cs client/tests/MusicLabBarrier client/tests/DiagnosticHotkeyRouting.Tests.csproj client/Plugin.cs apworld/scrc/placement.py apworld/scrc/__init__.py apworld/tests/test_world_integration.py apworld/tests/fixtures/bk_seed_28223804408101432968.json
git commit -m "fix: open Music Lab cassette paths in AP"
```

---

### Task 6: Keep Game Garage initialized with zero cartridges

**Files:**
- Create: `client/GarageAvailabilityPolicy.cs`
- Create: `client/tests/GarageAvailability/GarageAvailability.Tests.csproj`
- Create: `client/tests/GarageAvailability/Program.cs`
- Modify: `client/Plugin.cs` in `GarageCartridgeAccessKeeper` and `MusicLabDiscovery.PollGarageCartridgeSelection`
- Modify: `client/tests/DiagnosticHotkeyRouting.Tests.csproj`

**Interfaces:**
- Produces: `GarageObjectDecision Decide(bool enabled, bool compatible, bool ownsCartridge, GarageObjectRole role)`.
- `GarageObjectRole.Initialization` always remains active; `SongInteraction` is enabled only for owned cartridges; `Vanilla` is returned outside compatible AP.

- [ ] **Step 1: Write failing zero/one/multiple cartridge tests**

Assert all six initialization objects remain active with zero ownership, all six interactions are disabled, one owned song enables exactly one interaction, and disabled/incompatible AP returns `Vanilla` for every role.

- [ ] **Step 2: Run and verify failure**

Run: `dotnet run --project client/tests/GarageAvailability/GarageAvailability.Tests.csproj -c Release`

Expected: FAIL because the policy is absent.

- [ ] **Step 3: Implement policy and stop deactivating cartridge roots**

Replace the current `gameObject.SetActive(false)` ownership behavior on native cartridge roots. Resolve and patch the exact selection/start condition or interaction component instead. Preserve the six objects through room initialization and keep existing inserted-cartridge identity tracking.

- [ ] **Step 4: Add source assertions against the previous failure mode**

In the focused test, read the `GarageCartridgeAccessKeeper` integration slice and assert it does not deactivate unowned entries under `Root/GameRoom_27_Logic/Objects/Cartridges`.

- [ ] **Step 5: Run focused and diagnostic tests**

Run:

```powershell
dotnet run --project client/tests/GarageAvailability/GarageAvailability.Tests.csproj -c Release
dotnet run --project client/tests/DiagnosticHotkeyRouting.Tests.csproj -c Release
```

Expected: both PASS.

- [ ] **Step 6: Commit**

```powershell
git add client/GarageAvailabilityPolicy.cs client/tests/GarageAvailability client/tests/DiagnosticHotkeyRouting.Tests.csproj client/Plugin.cs
git commit -m "fix: preserve Game Garage initialization"
```

---

### Task 7: Map Level 22 ordinary checks without Victory

**Files:**
- Create: `client/LevelCompletionPolicy.cs`
- Create: `client/tests/LevelCompletion/LevelCompletion.Tests.csproj`
- Create: `client/tests/LevelCompletion/Program.cs`
- Modify: `client/Plugin.cs` in `LocationMap` and `GamePatches.ResultPersistedEventPostfix`
- Modify: `client/tests/DiagnosticHotkeyRouting.Tests.csproj`
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_world_integration.py`

**Interfaces:**
- Produces: `string? CompletionLocation(string internalLevel)` mapping `Level_28` to `Level 22 - Completion`.
- Produces: `IReadOnlyList<string> EarnedTierLocations(string internalLevel, int starsEarned)` returning cumulative one-to-three-Star locations and never `Victory`.

- [ ] **Step 1: Write failing mapping tests**

```csharp
Equal("Level 22 - Completion", LevelCompletionPolicy.CompletionLocation("Level_28"), "native Level 22");
SequenceEqual(new[] { "Level 22 - 1 Star" },
    LevelCompletionPolicy.EarnedTierLocations("Level_28", 1), "one-Star clear");
False(LevelCompletionPolicy.EarnedTierLocations("Level_28", 3).Contains("Victory"), "ordinary result only");
```

Also cover unknown levels, zero Stars, and cumulative two/three-Star output.

- [ ] **Step 2: Run and verify failure**

Run: `dotnet run --project client/tests/LevelCompletion/LevelCompletion.Tests.csproj -c Release`

Expected: FAIL because the policy is absent.

- [ ] **Step 3: Implement policy and integrate persisted-result mapping**

Use the existing `GetMostRecentStarRatingEvaluation` result to obtain `StarsEarned` after persistence. Queue completion and newly earned cumulative performance locations through the existing AP location queue. Keep `Victory` absent from all client result handling.

- [ ] **Step 4: Register matching APWorld locations without changing Victory**

Add Level 22 ordinary locations using the permanent registered names/IDs already present in `LocationMap`/ID registry. Attach them to Royal Corridor with current physical access rules. Leave the locked `Victory` event and completion condition unchanged.

- [ ] **Step 5: Run focused and APWorld tests**

Run:

```powershell
dotnet run --project client/tests/LevelCompletion/LevelCompletion.Tests.csproj -c Release
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
```

Expected: both PASS; APWorld tests explicitly prove Level 22 checks do not alter Victory.

- [ ] **Step 6: Commit**

```powershell
git add client/LevelCompletionPolicy.cs client/tests/LevelCompletion client/tests/DiagnosticHotkeyRouting.Tests.csproj client/Plugin.cs apworld/scrc/__init__.py apworld/tests/test_world_integration.py
git commit -m "fix: send ordinary Level 22 checks"
```

---

### Task 8: Close the completed-Level-4 Plant Pipes boundary

**Files:**
- Modify: `client/PlantPipesReconciler.cs`
- Modify: `client/tests/PlantPipesReconciler/Program.cs`
- Modify: `client/Plugin.cs` in `ApplyResultPostfix`, `ResultPersistedEventPostfix`, and reconciliation lifecycle calls

**Interfaces:**
- Consumes: existing `PlantPipesRuntime.OnLifecyclePoint(string reason)` and received history.
- Produces: explicit lifecycle reasons `level-result-applied:Level_08` and `level-result-persisted:Level_08` followed by bounded verification.

- [ ] **Step 1: Add a failing persisted-result lifecycle test**

Use `FakePlantPipesNativeAdapter` to start owned, simulate native ability clearing on Level 4 result persistence, call the two lifecycle points, and assert exactly one restorative submission followed by `verified-owned`. Assert duplicate persisted events do not duplicate AP receipts or native grants.

- [ ] **Step 2: Run and verify failure**

Run: `dotnet run --project client/tests/PlantPipesReconciler/PlantPipesReconciler.Tests.csproj -c Release`

Expected: FAIL because result lifecycle integration is not represented.

- [ ] **Step 3: Add explicit lifecycle integration**

After `Level_08` result application and persistence, invoke reconciliation on Unity's main thread. Preserve received history as authority, re-entry protection, retry bounds, and idempotence.

- [ ] **Step 4: Run the focused test**

Run: `dotnet run --project client/tests/PlantPipesReconciler/PlantPipesReconciler.Tests.csproj -c Release`

Expected: PASS with one restorative apply in the simulated clear case.

- [ ] **Step 5: Commit**

```powershell
git add client/PlantPipesReconciler.cs client/tests/PlantPipesReconciler/Program.cs client/Plugin.cs
git commit -m "fix: reconcile Plant Pipes after Level 4 results"
```

---

### Task 9: Version, documentation, and full automated gate

**Files:**
- Modify: `client/Plugin.cs`
- Modify: `client/build.ps1`
- Modify: `tools/validate-repo.py`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/INSTALL.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/NEXT_RELEASE_BUG_FIXES.md`
- Modify: `docs/TESTING_AND_ISSUES.md`

**Interfaces:**
- Produces: exact Client `0.67.63`, APWorld `0.18`, and truthful fixed/open issue status.

- [ ] **Step 1: Make repository validation fail on the old client version**

Change the independent expected client version in `tools/validate-repo.py` to `0.67.63`, then run it before changing production version strings.

Run: `& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' .\tools\validate-repo.py`

Expected: FAIL reporting the old client version.

- [ ] **Step 2: Update exact version strings and documentation**

Set `PluginVersion`, build messages, install verification text, README baseline, and roadmap boundary to `0.67.63`. Move only automatedly verified behavior into the candidate section. Keep every gameplay-only item marked pending until Task 10 passes.

- [ ] **Step 3: Run every client policy suite**

```powershell
$projects = Get-ChildItem client\tests -Recurse -Filter *.csproj | Sort-Object FullName
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
```

Expected: every focused suite prints its pass message and exits zero.

- [ ] **Step 4: Run all APWorld tests and repository validation**

```powershell
$python = 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
& $python -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
& $python .\tools\validate-repo.py
```

Expected: all tests PASS; validator reports Client v0.67.63 and APWorld v0.18.

- [ ] **Step 5: Build without installation**

Run: `.\client\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall`

Expected: build succeeds with zero errors and produces `client/bin/Release/net6.0/RhythmCastleAP.dll`.

- [ ] **Step 6: Commit**

```powershell
git add client/Plugin.cs client/build.ps1 tools/validate-repo.py README.md CHANGELOG.md docs/INSTALL.md docs/ROADMAP.md docs/NEXT_RELEASE_BUG_FIXES.md docs/TESTING_AND_ISSUES.md
git commit -m "chore: prepare v0.67.63 repair candidate"
```

---

### Task 10: Deploy once and run combined gameplay acceptance

**Files:**
- Modify after evidence: `CHANGELOG.md`
- Modify after evidence: `docs/NEXT_RELEASE_BUG_FIXES.md`
- Modify after evidence: `docs/TESTING_AND_ISSUES.md`
- Create: `docs/testing/2026-08-23-v06763-combined-acceptance.md`

**Interfaces:**
- Consumes: verified v0.67.63 DLL and matching installed APWorld v0.18.
- Produces: one timestamped evidence record with exact build, seed, save slot, admin sends, pass/fail result, and log excerpts.

- [ ] **Step 1: Install only the verified candidate**

Run: `.\client\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus'`

Expected: only `D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP` is replaced; log instructions reference v0.67.63.

- [ ] **Step 2: Generate a fresh deterministic v0.18 seed and start its server**

Record archive name, seed ID, port, player name, YAML, and spoiler placements. Confirm the server reports no prior save data.

- [ ] **Step 3: Verify process and mod before gameplay**

Launch `D:\SteamLibrary\steamapps\common\Titus\Rhythm Castle.exe`. Confirm local `WINHTTP.dll`, local `dotnet\coreclr.dll`, a fresh `BepInEx\LogOutput.log`, `[SCRC-AP] v0.67.63 loading.`, compatible slot data, and connection to the new port.

- [ ] **Step 4: Run the single combined route with the project owner**

Verify in order:

1. fresh save enters Music Lab without tutorial;
2. native Star counter is visible;
3. Normal and Pro are selectable;
4. every orange-barrier cassette path is physically open;
5. Garage visibly loads and exits with zero cartridges;
6. `/send Jack <one cartridge name>` enables exactly that song;
7. first Roots arrival cutscene does not play;
8. post-Level-1 gate/difficulty presentation does not play;
9. admin sends, if needed, are recorded before Weed Killer and Plant Pipes tests;
10. Level 3 and completed Level 4 preserve Plant Pipes through Hub2, save load, reconnect, and restart;
11. Level 22 one-Star completion sends its ordinary check and never Victory.

- [ ] **Step 5: Write evidence and update issue status accurately**

In `docs/testing/2026-08-23-v06763-combined-acceptance.md`, record `PASS`, `FAIL`, or `NOT RUN` for every numbered step. Include exact relevant log lines and note every admin send. Check off only passed blockers in `docs/NEXT_RELEASE_BUG_FIXES.md`.

- [ ] **Step 6: Rerun the full automated gate after documentation edits**

Repeat Task 9 Steps 3–5. Expected: all client suites, all APWorld tests, validator, and build PASS.

- [ ] **Step 7: Commit the acceptance evidence**

```powershell
git add CHANGELOG.md docs/NEXT_RELEASE_BUG_FIXES.md docs/TESTING_AND_ISSUES.md docs/testing/2026-08-23-v06763-combined-acceptance.md
git commit -m "test: record v0.67.63 combined acceptance"
```

Stop after this commit. Request explicit approval before merging, pushing, or making the candidate available to testers.
