# Candidate Stabilization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preserve the verified Level 22 false-positive repair and remove failed bottom-HUD experiments before further work.

**Architecture:** Keep Level 22 result selection in the pure `LevelCompletionPolicy`; the runtime queues only locations returned by that policy. Remove the unregistered native phase experiment completely so later HUD investigation begins from a clean baseline.

**Tech Stack:** C#/.NET 6 client, .NET 8 console regression projects, PowerShell build, Git.

**Spec:** `docs/superpowers/specs/2026-08-23-consolidated-stability-preview-abilities-design.md`

## Global Constraints

- Do not merge, push, publish, or install a build without explicit owner approval.
- Preserve all verified behavior in `docs/testing/2026-08-23-v06763-acceptance-ledger.md`.
- Use TDD and commit only files belonging to the current task.
- Do not modify `WordFactori/` or unrelated user changes.

---

### Task 1: Remove the failed bottom-HUD phase experiment

**Files:**
- Modify: `client/DifficultyAvailabilityPolicy.cs`
- Modify: `client/tests/DifficultyAvailability/Program.cs`
- Modify: `client/Plugin.cs`

**Interfaces:**
- Consumes: existing `DifficultyAvailabilityPolicy.Decide(bool, bool, bool, bool)`.
- Produces: no `DifficultyTogglerPhaseDecision`, `DecideTogglerPhase`, or `HubCharacterDifficultyControlKeeper` symbols.

- [ ] **Step 1: Record the clean-baseline expectation**

Keep the five existing availability assertions and remove all assertions that call `DecideTogglerPhase`. The remaining test ends with:

```csharp
Equal(DifficultyAvailabilityDecision.AlreadyAvailable,
    DifficultyAvailabilityPolicy.Decide(true, true, true, true),
    "both native choices remain untouched");
```

- [ ] **Step 2: Remove the experiment**

Delete `DifficultyTogglerPhaseDecision`, `DecideTogglerPhase`, and the complete `HubCharacterDifficultyControlKeeper` class. Confirm no `AddComponent<HubCharacterDifficultyControlKeeper>()` registration exists.

- [ ] **Step 3: Run the focused test**

Run: `dotnet run --project client/tests/DifficultyAvailability/DifficultyAvailability.Tests.csproj`  
Expected: `Difficulty availability policy tests passed.`

- [ ] **Step 4: Build the client without installation**

Run: `./client/build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Titus' -SkipInstall`  
Expected: build succeeds with zero errors.

- [ ] **Step 5: Commit**

```powershell
git add client/DifficultyAvailabilityPolicy.cs client/tests/DifficultyAvailability/Program.cs client/Plugin.cs
git commit -m "chore: remove failed bottom hud diagnostic"
```

### Task 2: Finalize Level 22 result gating

**Files:**
- Modify: `client/LevelCompletionPolicy.cs`
- Modify: `client/tests/LevelCompletion/Program.cs`
- Modify: `client/Plugin.cs`
- Modify: `docs/NEXT_RELEASE_BUG_FIXES.md`
- Modify: `docs/testing/2026-08-23-v06763-acceptance-ledger.md`

**Interfaces:**
- Produces: `LevelCompletionPolicy.LocationsForPersistedResult(string? internalLevel, int? starsEarned) -> IReadOnlyList<string>`.
- Consumes: `PendingResult.StarsEarned` captured from the native result request.

- [ ] **Step 1: Verify the regression test covers the observed break**

```csharp
SequenceEqual(Array.Empty<string>(),
    LevelCompletionPolicy.LocationsForPersistedResult("Level_28", null),
    "failed Level 22 persisted without result data");
SequenceEqual(Array.Empty<string>(),
    LevelCompletionPolicy.LocationsForPersistedResult("Level_28", 0),
    "failed zero-Star Level 22 result");
```

- [ ] **Step 2: Verify successful cumulative results**

Assert literal arrays for one, two, and three Stars. The one-Star literal is:

```csharp
new[] { "Level 22 - Completion", "Level 22 - 1 Star" }
```

Also assert that no returned array contains `Victory`.

- [ ] **Step 3: Run the focused test**

Run: `dotnet run --project client/tests/LevelCompletion/LevelCompletion.Tests.csproj`  
Expected: `Level completion policy tests passed.`

- [ ] **Step 4: Verify runtime integration**

For `Level_28`, queue only the locations returned by `LocationsForPersistedResult`. When the list is empty, log `LEVEL 22 RESULT SUPPRESSED` and queue nothing. Other levels continue through `LocationMap.InternalToLocationName` unchanged.

- [ ] **Step 5: Run all client tests and repository validation**

Run every `client/tests/**/*.csproj`, then `python -m unittest discover apworld/tests -v`, then `python tools/validate-repo.py`.  
Expected: 12 client projects pass, 57 APWorld tests pass, repository validation passes.

- [ ] **Step 6: Commit**

```powershell
git add client/LevelCompletionPolicy.cs client/tests/LevelCompletion/Program.cs client/Plugin.cs docs/NEXT_RELEASE_BUG_FIXES.md docs/TESTING.md docs/testing/2026-08-23-v06763-acceptance-ledger.md
git commit -m "fix: reject failed Level 22 results"
```

