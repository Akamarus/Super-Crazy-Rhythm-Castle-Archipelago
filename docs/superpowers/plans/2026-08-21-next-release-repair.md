# Next Release Repair Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce APWorld v0.18 and Client v0.67.61 with durable Plant Pipes, safe native-room behavior, truthful conservative reachability, and a fresh-seed route that cannot require blocked or unreasonable checks.

**Architecture:** Keep native-game interactions in `client/Plugin.cs`, but extract deterministic decisions into small pure C# policy classes with console-test projects. Move AP reachability classifications and safe-placement checks into focused Python modules consumed by the world class, so generation and tests use one source of truth. Slot data is the compatibility contract between both halves and all new behavior fails closed when that contract is absent.

**Tech Stack:** C#/.NET 6 BepInEx IL2CPP client, Harmony patches, Archipelago client libraries, Python 3 Archipelago world, `unittest`, repository validation and PowerShell packaging scripts.

**Spec:** `docs/superpowers/specs/2026-08-21-next-release-repair-design.md`

## Global Constraints

- Target APWorld version is `0.18`; target client version is `0.67.61`.
- Preserve the implementation-version prefix `area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17` and append a v0.18 repair marker.
- AP Stars, AP Music Lab Point items, cassette-item randomization, and final Level 22 Victory remain inactive.
- Required progression may not appear on Platinum, unverified Music Lab checks, blocked Royal-side checks, or disabled Game Garage checks.
- Normal and Pro must both be selectable; neither is forced.
- Do not invent native flags. Any Plant Pipes or barrier state used by production code must be demonstrated by a focused diagnostic trace.
- Deploy client files only to `BepInEx\plugins\RhythmCastleAP`; install the APWorld only through the normal custom-world path.
- Do not merge or push without project-owner approval.

---

### Task 1: Lock the v0.18 compatibility contract

**Files:**
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `tools/validate-repo.py`
- Modify: `apworld/tests/test_repository_contract.py`
- Modify: `client/Plugin.cs`
- Test: `client/tests/RepairCompatibility/RepairCompatibility.Tests.csproj`
- Test: `client/tests/RepairCompatibility/Program.cs`

**Interfaces:**
- Produces: `RepairCompatibilityPolicy.IsCompatible(string? implementationVersion, bool repairEnabled) -> bool`.
- Produces slot fields `repair_schema_version`, `plant_pipes_durable_reconciliation`, `music_lab_safe_location_classification`, `garage_routing_mode`, `royal_phone_side_split`, `level_22_native_mapping`, `native_difficulty_choice`, and `roots_intro_suppression`.
- Consumes: the preserved v0.17 implementation prefix defined in Global Constraints.

- [ ] **Step 1: Add failing APWorld and repository-contract tests**

```python
data = world.fill_slot_data()
self.assertEqual(data["schema_version"], 9)
self.assertEqual(data["repair_schema_version"], "next-release-repair-0.18")
self.assertTrue(data["implementation_version"].endswith("-next-release-repair-0.18"))
self.assertTrue(data["plant_pipes_durable_reconciliation"])
self.assertEqual(data["garage_routing_mode"], "interaction-gated")
self.assertTrue(data["royal_phone_side_split"])
self.assertTrue(data["level_22_native_mapping"])
self.assertTrue(data["native_difficulty_choice"])
self.assertTrue(data["roots_intro_suppression"])
self.assertFalse(data["star_items_active"])
```

- [ ] **Step 2: Run the focused Python tests and verify RED**

Run: `py -m unittest apworld.tests.test_world_integration apworld.tests.test_repository_contract -v`

Expected: FAIL because schema 9, the v0.18 suffix, and repair fields do not exist.

- [ ] **Step 3: Add failing client compatibility tests**

```csharp
const string RepairVersion =
    "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18";
Equal(true, RepairCompatibilityPolicy.IsCompatible(RepairVersion, true), "v0.18 contract");
Equal(true, RepairCompatibilityPolicy.IsCompatible(RepairVersion + "-patch1", true), "additive patch");
Equal(false, RepairCompatibilityPolicy.IsCompatible(RepairVersion, false), "feature flag off");
Equal(false, RepairCompatibilityPolicy.IsCompatible("next-release-repair-0.18", true), "missing preserved prefix");
Equal(false, RepairCompatibilityPolicy.IsCompatible(null, true), "missing version");
```

- [ ] **Step 4: Run the client policy test and verify RED**

Run: `dotnet run --project client/tests/RepairCompatibility/RepairCompatibility.Tests.csproj -c Release`

Expected: FAIL because `RepairCompatibilityPolicy` is undefined.

- [ ] **Step 5: Implement the minimal contract on both sides**

```csharp
internal static class RepairCompatibilityPolicy
{
    internal const string RequiredVersion =
        "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18";

    internal static bool IsCompatible(string? version, bool enabled) =>
        enabled && version != null && version.StartsWith(RequiredVersion, StringComparison.Ordinal);
}
```

Update `fill_slot_data()`, the validator expectations, and client slot parsing together. Keep every inactive feature field explicitly false.

- [ ] **Step 6: Run focused tests and repository validation**

Run: `dotnet run --project client/tests/RepairCompatibility/RepairCompatibility.Tests.csproj -c Release`

Run: `py -m unittest apworld.tests.test_world_integration apworld.tests.test_repository_contract -v`

Run: `py tools/validate-repo.py`

Expected: all PASS and validator reports APWorld v0.18 / Client v0.67.61.

- [ ] **Step 7: Commit the compatibility contract**

```powershell
git add apworld/scrc/__init__.py apworld/tests/test_world_integration.py apworld/tests/test_repository_contract.py tools/validate-repo.py client/Plugin.cs client/tests/RepairCompatibility
git commit -m "chore: define v0.18 repair contract"
```

### Task 2: Build the pure Plant Pipes reconciliation state machine

**Files:**
- Create: `client/PlantPipesReconciler.cs`
- Test: `client/tests/PlantPipesReconciler/PlantPipesReconciler.Tests.csproj`
- Test: `client/tests/PlantPipesReconciler/Program.cs`

**Interfaces:**
- Produces enum `PlantPipesDecision { Ignore, WaitForSave, WaitForProcessor, Apply, Verify, Satisfied }`.
- Produces record `PlantPipesSnapshot(bool Compatible, bool Synchronized, int ReceivedCount, bool SaveAvailable, bool ProcessorAvailable, bool NativeOwned, bool AttemptOutstanding, int RetryCount)`.
- Produces `PlantPipesReconciler.Decide(PlantPipesSnapshot snapshot) -> PlantPipesDecision`.
- Produces `PlantPipesReconciler.NextRetryDelay(int retryCount) -> TimeSpan?` with bounded delays of 250 ms, 500 ms, 1 s, 2 s, and 4 s.

- [ ] **Step 1: Write failing state-machine tests**

```csharp
Equal(PlantPipesDecision.Ignore, Decide(received: 0), "not owned");
Equal(PlantPipesDecision.WaitForSave, Decide(received: 1, save: false), "history before save");
Equal(PlantPipesDecision.WaitForProcessor, Decide(received: 1, save: true, processor: false), "save before processor");
Equal(PlantPipesDecision.Apply, Decide(received: 1, save: true, processor: true), "grant missing ability");
Equal(PlantPipesDecision.Satisfied, Decide(received: 2, nativeOwned: true), "duplicate history idempotent");
Equal<TimeSpan?>(null, PlantPipesReconciler.NextRetryDelay(5), "retry is bounded");
```

- [ ] **Step 2: Run the state-machine test and verify RED**

Run: `dotnet run --project client/tests/PlantPipesReconciler/PlantPipesReconciler.Tests.csproj -c Release`

Expected: FAIL because the reconciler types do not exist.

- [ ] **Step 3: Implement only the pure decisions and retry schedule**

```csharp
internal static PlantPipesDecision Decide(PlantPipesSnapshot s)
{
    if (!s.Compatible || !s.Synchronized || s.ReceivedCount < 1) return PlantPipesDecision.Ignore;
    if (s.NativeOwned) return PlantPipesDecision.Satisfied;
    if (!s.SaveAvailable) return PlantPipesDecision.WaitForSave;
    if (!s.ProcessorAvailable) return PlantPipesDecision.WaitForProcessor;
    return s.AttemptOutstanding ? PlantPipesDecision.Verify : PlantPipesDecision.Apply;
}
```

- [ ] **Step 4: Run the test and verify GREEN**

Run: `dotnet run --project client/tests/PlantPipesReconciler/PlantPipesReconciler.Tests.csproj -c Release`

Expected: PASS with every lifecycle and duplicate-delivery case covered.

- [ ] **Step 5: Commit the pure reconciler**

```powershell
git add client/PlantPipesReconciler.cs client/tests/PlantPipesReconciler
git commit -m "test: define durable Plant Pipes reconciliation"
```

### Task 3: Diagnose and integrate durable Plant Pipes ownership

**Files:**
- Modify: `client/Plugin.cs`
- Modify: `client/PlantPipesReconciler.cs`
- Modify: `docs/NEXT_RELEASE_BUG_FIXES.md`
- Test: `client/tests/PlantPipesReconciler/Program.cs`

**Interfaces:**
- Consumes: `PlantPipesReconciler.Decide` from Task 2.
- Produces: `PlantPipesRuntime.NoteReceivedCount(int count)`, `PlantPipesRuntime.OnSaveAvailable(object save)`, `PlantPipesRuntime.OnProcessorAvailable(object processor)`, `PlantPipesRuntime.OnLifecyclePoint(string reason)`, and `PlantPipesRuntime.TickPending(TimeSpan elapsed)`.
- Produces a diagnostic result identifying the verified native marker set used by the grant.

- [ ] **Step 1: Add a diagnostic-only read path for candidate native state**

Log the selected save identity and the read values of `WEED_KILLER_ABILITY` plus any marker discovered in the native grant request. Do not mutate a candidate flag merely because its name looks related.

- [ ] **Step 2: Run the focused native trace before choosing the write set**

Run the game on the diagnostic build, receive Plant Pipes once, finish Level 3, return to Hub2, reload the save, and restart. Capture the request type and exact flag transitions. Expected evidence: either `WEED_KILLER_ABILITY` alone survives through the normal save request, or the trace identifies the additional native marker written by vanilla.

- [ ] **Step 3: Add failing runtime tests using fake save and processor adapters**

```csharp
runtime.NoteReceivedCount(1);
runtime.OnLifecyclePoint("history replay");
Equal("save-unavailable", runtime.LastOutcome, "history waits");
runtime.OnSaveAvailable(fakeSave);
Equal("processor-unavailable", runtime.LastOutcome, "save alone remains pending");
runtime.OnProcessorAvailable(fakeProcessor);
Equal(1, fakeProcessor.SubmitCount, "grant submitted once");
runtime.OnLifecyclePoint("scene entry");
Equal(1, fakeProcessor.SubmitCount, "verified ownership is idempotent");
```

- [ ] **Step 4: Run the runtime test and verify RED**

Run: `dotnet run --project client/tests/PlantPipesReconciler/PlantPipesReconciler.Tests.csproj -c Release`

Expected: FAIL because lifecycle integration and adapters are absent.

- [ ] **Step 5: Integrate the verified grant path**

Wire received history, selected-save availability, processor capture, scene entry, level-result completion, reconnect, and the bounded pending timer into `PlantPipesRuntime`. After submission, re-read the verified native marker set; do not mark the attempt satisfied until the read succeeds.

- [ ] **Step 6: Run policy tests and build the client**

Run: `dotnet run --project client/tests/PlantPipesReconciler/PlantPipesReconciler.Tests.csproj -c Release`

Run: `dotnet build client/RhythmCastleAP.csproj -c Release`

Expected: PASS; build contains no continuous per-frame native write.

- [ ] **Step 7: Commit durable reconciliation**

```powershell
git add client/Plugin.cs client/PlantPipesReconciler.cs client/tests/PlantPipesReconciler docs/NEXT_RELEASE_BUG_FIXES.md
git commit -m "fix: reconcile Plant Pipes across save lifecycle"
```

### Task 4: Make Game Garage routing fail safe

**Files:**
- Create: `client/GarageRoutingPolicy.cs`
- Modify: `client/Plugin.cs`
- Test: `client/tests/GarageRoutingPolicy/GarageRoutingPolicy.Tests.csproj`
- Test: `client/tests/GarageRoutingPolicy/Program.cs`

**Interfaces:**
- Produces enum `GarageRoutingMode { InteractionGated, DisabledSafeFallback }`.
- Produces `GarageRoutingPolicy.ShouldKeepObjectAlive(string hierarchyPath) -> bool`.
- Produces `GarageRoutingPolicy.CanUseSong(GarageRoutingMode mode, string song, IReadOnlySet<string> ownedCartridges) -> bool`.
- Consumes slot field `garage_routing_mode` from Task 1.

- [ ] **Step 1: Write failing policy tests for zero, one, and six cartridges**

```csharp
Equal(true, GarageRoutingPolicy.ShouldKeepObjectAlive("Root/GameRoom_27_Logic/Objects/Cartridges"), "root remains alive");
Equal(false, GarageRoutingPolicy.CanUseSong(GarageRoutingMode.InteractionGated, "Vampire Killer", Empty), "zero items");
Equal(true, GarageRoutingPolicy.CanUseSong(GarageRoutingMode.InteractionGated, "Vampire Killer", VampireOnly), "matching item");
Equal(false, GarageRoutingPolicy.CanUseSong(GarageRoutingMode.InteractionGated, "Smooch", VampireOnly), "nonmatching item");
Equal(false, GarageRoutingPolicy.CanUseSong(GarageRoutingMode.DisabledSafeFallback, "Vampire Killer", VampireOnly), "fallback disables live routing");
```

- [ ] **Step 2: Run the policy test and verify RED**

Run: `dotnet run --project client/tests/GarageRoutingPolicy/GarageRoutingPolicy.Tests.csproj -c Release`

Expected: FAIL because the policy does not exist.

- [ ] **Step 3: Replace holder deactivation with the smallest verified interaction gate**

Keep `Root/GameRoom_27_Logic/Objects/Cartridges` and all initialization/lifecycle parents active. Gate only the verified pickup, insertion, or song-start interaction for an unowned cartridge. If the interaction surface cannot be isolated, report `DisabledSafeFallback` and leave native room initialization untouched.

- [ ] **Step 4: Run policy tests and build**

Run: `dotnet run --project client/tests/GarageRoutingPolicy/GarageRoutingPolicy.Tests.csproj -c Release`

Run: `dotnet build client/RhythmCastleAP.csproj -c Release`

Expected: PASS.

- [ ] **Step 5: Perform the focused room acceptance test**

Enter and menu-exit `GameRoom_27` with zero AP cartridges; receive one cartridge; re-enter; prove only the matching song works; exit and restart. If any black screen remains, set the slot contract and APWorld policy to `disabled-safe-fallback` before release.

- [ ] **Step 6: Commit Garage safety**

```powershell
git add client/GarageRoutingPolicy.cs client/Plugin.cs client/tests/GarageRoutingPolicy
git commit -m "fix: preserve Game Garage room lifecycle"
```

### Task 5: Map Level 22 and add narrow startup behavior

**Files:**
- Create: `client/RepairStartupPolicy.cs`
- Modify: `client/Plugin.cs`
- Test: `client/tests/RepairStartupPolicy/RepairStartupPolicy.Tests.csproj`
- Test: `client/tests/RepairStartupPolicy/Program.cs`

**Interfaces:**
- Produces `RepairStartupPolicy.TryMapLevel(string internalId, out int levelNumber) -> bool`.
- Produces `RepairStartupPolicy.RootsIntroFlags -> IReadOnlySet<string>` containing exactly `ROOTS_HUB_GATE_OPENED` and `ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE`.
- Produces `RepairStartupPolicy.ShouldUnlockDifficulty(string difficultyName) -> bool` true only for `Normal` and `Pro`.
- Consumes compatibility fields from Task 1.

- [ ] **Step 1: Write failing mapping and scope tests**

```csharp
Equal(true, RepairStartupPolicy.TryMapLevel("Level_28", out int level), "native Level 22 maps");
Equal(22, level, "player-facing number");
SetEqual(new[] { "ROOTS_HUB_GATE_OPENED", "ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE" }, RepairStartupPolicy.RootsIntroFlags, "narrow flags");
Equal(true, RepairStartupPolicy.ShouldUnlockDifficulty("Normal"), "normal available");
Equal(true, RepairStartupPolicy.ShouldUnlockDifficulty("Pro"), "pro available");
Equal(false, RepairStartupPolicy.ShouldUnlockDifficulty("Devil"), "no unrelated unlock");
```

- [ ] **Step 2: Run the test and verify RED**

Run: `dotnet run --project client/tests/RepairStartupPolicy/RepairStartupPolicy.Tests.csproj -c Release`

Expected: FAIL because the policy does not exist.

- [ ] **Step 3: Implement pure mapping and exact allowlists**

The map must treat `Level_28` as ordinary Level 22 completion. Do not connect it to `Victory`. Apply both difficulty choices after an AP save becomes available. Submit only the two approved Roots flags and leave all later quest flags untouched.

- [ ] **Step 4: Integrate the policy into native hooks**

Use the existing level-result location sender for Level 22. Use the same save-availability retry seam as Task 3 for startup flags and difficulty availability so history replay timing cannot lose them.

- [ ] **Step 5: Run focused tests and build**

Run: `dotnet run --project client/tests/RepairStartupPolicy/RepairStartupPolicy.Tests.csproj -c Release`

Run: `dotnet build client/RhythmCastleAP.csproj -c Release`

Expected: PASS.

- [ ] **Step 6: Commit Level 22 and startup behavior**

```powershell
git add client/RepairStartupPolicy.cs client/Plugin.cs client/tests/RepairStartupPolicy
git commit -m "fix: map Level 22 and normalize AP startup"
```

### Task 6: Encode conservative world reachability

**Files:**
- Create: `apworld/scrc/reachability.py`
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/scrc/difficulty.py`
- Test: `apworld/tests/test_reachability.py`
- Modify: `apworld/tests/test_world_integration.py`

**Interfaces:**
- Produces enum `LocationSafety` with values `VERIFIED_OPEN`, `VERIFIED_GATED`, and `UNVERIFIED`.
- Produces `location_safety(name: str, garage_mode: str) -> LocationSafety`.
- Produces `allows_required_progression(name: str, garage_mode: str) -> bool`.
- Produces `required_items() -> frozenset[str]`.
- Produces explicit Royal regions `Royal Phone Side`, `Royal Star Eater Side`, and `Royal Level 21 Side`.

- [ ] **Step 1: Write failing safety-classification tests**

```python
self.assertEqual(location_safety("Music Lab Cassette - Lets Go - Bronze", "interaction-gated"), LocationSafety.VERIFIED_OPEN)
self.assertEqual(location_safety("Music Lab Cassette - Zen - Bronze", "interaction-gated"), LocationSafety.UNVERIFIED)
self.assertFalse(allows_required_progression("Music Lab Cassette - Lets Go - Platinum", "interaction-gated"))
self.assertFalse(allows_required_progression("Game Garage - Smooch - Bronze", "disabled-safe-fallback"))
self.assertFalse(allows_required_progression("Royal Corridor - Star Eater", "interaction-gated"))
```

Use only cassette identities proven by the fresh-save trace as `VERIFIED_OPEN`; do not infer the rest from screen numbering.

- [ ] **Step 2: Write failing Royal and cartridge-rule tests**

```python
self.assertTrue(level22.access_rule(State(["Royal Corridor Access"])))
self.assertFalse(level21.access_rule(State(["Royal Corridor Access"])))
self.assertFalse(garage_song.access_rule(State([])))
self.assertTrue(garage_song.access_rule(State([garage_song.required_cartridge])))
```

- [ ] **Step 3: Run the reachability tests and verify RED**

Run: `py -m unittest apworld.tests.test_reachability apworld.tests.test_world_integration -v`

Expected: FAIL because safety classifications and Royal subregions do not exist.

- [ ] **Step 4: Implement one authoritative safety table and region split**

```python
def allows_required_progression(name: str, garage_mode: str) -> bool:
    if name.endswith(" - Platinum"):
        return False
    return location_safety(name, garage_mode) is not LocationSafety.UNVERIFIED
```

Create the phone-side Royal connection from `Royal Corridor Access`; leave Star Eater and Level 21 disconnected until their native requirements are represented. Apply each Garage song's matching cartridge rule. Treat Music Lab chests above proven reachable native points as non-progression-only.

- [ ] **Step 5: Run reachability and existing regression tests**

Run: `py -m unittest discover apworld/tests -v`

Expected: all PASS; existing Roots access and item IDs remain unchanged.

- [ ] **Step 6: Commit conservative reachability**

```powershell
git add apworld/scrc/reachability.py apworld/scrc/__init__.py apworld/scrc/difficulty.py apworld/tests/test_reachability.py apworld/tests/test_world_integration.py
git commit -m "fix: model conservative physical reachability"
```

### Task 7: Reject unsafe required-item placements

**Files:**
- Create: `apworld/scrc/generation_safety.py`
- Modify: `apworld/scrc/__init__.py`
- Test: `apworld/tests/test_generation_safety.py`
- Test: `apworld/tests/test_failed_seed_regression.py`

**Interfaces:**
- Produces `safe_progression_locations(world) -> tuple[str, ...]`.
- Produces `validate_progression_capacity(world, required_item_names: tuple[str, ...]) -> None`.
- Produces `validate_required_placement(world, item_name: str, location_name: str) -> None`.
- Consumes `allows_required_progression` and `required_items` from Task 6.

- [ ] **Step 1: Add failing capacity, self-lock, and Platinum tests**

```python
with self.assertRaisesRegex(ValueError, "safe progression capacity"):
    validate_progression_capacity(world_with_two_safe_locations, ("Weed Killer", "Plant Pipes", "Hip Glasses"))
with self.assertRaisesRegex(ValueError, "Platinum"):
    validate_required_placement(world, "Lobby Access", "Music Lab Cassette - Lets Go - Platinum")
with self.assertRaisesRegex(ValueError, "self-lock"):
    validate_required_placement(world, "Plant Pipes", "Music Lab - 64 Point Chest")
```

- [ ] **Step 2: Encode the failed v0.17 seed as a regression fixture**

The test must include the observed unsafe placements from `AP_28223804408101432968` and assert they are rejected, including Lobby Access on Lets Go Platinum and Plant Pipes on the 64 Point Chest.

- [ ] **Step 3: Run the generation tests and verify RED**

Run: `py -m unittest apworld.tests.test_generation_safety apworld.tests.test_failed_seed_regression -v`

Expected: FAIL because generation currently accepts unsafe locations.

- [ ] **Step 4: Implement fail-closed pre-fill and placement validation**

Build the required-item and safe-location sets before fill. Apply per-location item rules so required items cannot enter unsafe locations. Reject insufficient capacity with the selected difficulty, Garage mode, verified cassette count, required count, and safe count in the exception.

- [ ] **Step 5: Add deterministic sphere checks across a seed matrix**

```python
for seed in range(100):
    world = build_world(seed=seed, difficulty=difficulty)
    generate(world)
    self.assert_required_items_reachable_sphere_by_sphere(world)
```

Run the matrix for every supported AP performance difficulty and both Garage modes. The internal development Victory placeholder must be ignored by this assertion.

- [ ] **Step 6: Run all APWorld tests**

Run: `py -m unittest discover apworld/tests -v`

Expected: PASS with deterministic results and an explicit rejection for insufficient safe capacity.

- [ ] **Step 7: Commit generation safety**

```powershell
git add apworld/scrc/generation_safety.py apworld/scrc/__init__.py apworld/tests/test_generation_safety.py apworld/tests/test_failed_seed_regression.py
git commit -m "fix: reject unsafe progression placements"
```

### Task 8: Update release documentation and packaging contracts

**Files:**
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/PROJECT_OVERVIEW.md`
- Modify: `docs/PROGRESSION.md`
- Modify: `docs/NEXT_RELEASE_BUG_FIXES.md`
- Modify: `apworld/README.md`
- Modify: `apworld/scrc/docs/setup_en.md`
- Modify: `apworld/scrc/docs/en_Super Crazy Rhythm Castle.md`
- Modify: `client/RhythmCastleAP.csproj`
- Modify: `apworld/scrc/__init__.py`
- Modify: `tools/validate-repo.py`

**Interfaces:**
- Consumes: verified Garage mode, verified Music Lab classification, and passing acceptance results from Tasks 3–7.
- Produces: consistent user-facing v0.18 / v0.67.61 status and install instructions.

- [ ] **Step 1: Add failing repository-contract assertions for versions and claims**

```python
self.assert_repo_contains("APWorld v0.18")
self.assert_repo_contains("Client v0.67.61")
self.assert_repo_contains("Platinum checks are optional")
self.assert_repo_does_not_claim("AP Stars are active")
```

- [ ] **Step 2: Run repository tests and verify RED**

Run: `py -m unittest apworld.tests.test_repository_contract -v`

Expected: FAIL on old versions and stale live-feature wording.

- [ ] **Step 3: Update docs without prematurely claiming fixes**

Describe the conservative location policy, Royal split, Level 22 ordinary check, Normal/Pro choice, and current Garage mode. Keep each bug checklist box open until its gameplay acceptance step actually passes. List any unverified cassette/barrier groups under known issues.

- [ ] **Step 4: Update project and validator versions**

Set the client assembly/package version to `0.67.61`, APWorld version to `0.18`, and validator expectations to the exact implementation tag from Task 1.

- [ ] **Step 5: Run documentation and repository validation**

Run: `py -m unittest apworld.tests.test_repository_contract -v`

Run: `py tools/validate-repo.py`

Expected: PASS with no stale v0.17-as-current claim.

- [ ] **Step 6: Commit release documentation**

```powershell
git add README.md CHANGELOG.md docs/PROJECT_OVERVIEW.md docs/PROGRESSION.md docs/NEXT_RELEASE_BUG_FIXES.md apworld/README.md apworld/scrc/docs client/RhythmCastleAP.csproj apworld/scrc/__init__.py tools/validate-repo.py apworld/tests/test_repository_contract.py
git commit -m "docs: prepare v0.18 repair testing"
```

### Task 9: Package, install, and run fresh-seed acceptance

**Files:**
- Modify only if evidence requires it: `docs/NEXT_RELEASE_BUG_FIXES.md`
- Modify only after verified success: `CHANGELOG.md`
- Create through existing tooling: packaged `.apworld` and client build artifacts outside tracked source

**Interfaces:**
- Consumes: every implementation and test interface from Tasks 1–8.
- Produces: installed-artifact hashes, fresh seed identifier, focused gameplay logs, and a pass/fail record for every acceptance criterion.

- [ ] **Step 1: Run the complete automated verification suite**

Run: `py -m unittest discover apworld/tests -v`

Run each `client/tests/*/*.Tests.csproj` with `dotnet run --project <project> -c Release`.

Run: `dotnet build client/RhythmCastleAP.csproj -c Release`

Run: `py tools/validate-repo.py`

Expected: every command exits zero.

- [ ] **Step 2: Package without broad deployment**

Use the repository's existing APWorld packaging helper. Install only the matching APWorld and copy only the client plugin and its required files into `BepInEx\plugins\RhythmCastleAP`. Record SHA-256 hashes for built and installed artifacts and require exact matches.

- [ ] **Step 3: Generate a fresh seed and inspect its spoiler before play**

Verify sphere-by-sphere that all required progression avoids Platinum, blocked Music Lab machines, unsupported point chests, Garage songs without their cartridges, and Royal Level 21-side content. Reject the seed before launch if inspection disagrees with the solver.

- [ ] **Step 4: Run the native-room and startup acceptance cases**

On a fresh in-game save, confirm Normal and Pro are selectable, the redundant Roots post-Level-1 cutscene does not play, Game Garage loads/exits with zero and one cartridge, Royal Access reaches Level 22 but not Level 21, and one-Star `Level_28` sends the ordinary Level 22 check without Victory.

- [ ] **Step 5: Run the required Roots route without admin commands**

Complete `Weed Killer -> Frog/Hippo -> Plant Pipes -> Level 4 -> Hip Glasses -> Lift Quest -> Bucket Minion -> Chicken Bucket`. Reload, reconnect, change scenes, finish Level 3, enter Level 4, and fully restart at the Plant Pipes checkpoints. Expected: one received Plant Pipes remains usable throughout and no duplicate delivery is needed.

- [ ] **Step 6: Reconcile docs with evidence**

Check only the `docs/NEXT_RELEASE_BUG_FIXES.md` items whose acceptance evidence passed. Move those same items into `CHANGELOG.md` under **Fixed**. Leave every failure open and block release-ready wording.

- [ ] **Step 7: Run final verification and commit only tracked evidence/docs**

Run: `git diff --check`

Run: `git status --short`

Run the full automated suite from Step 1 again. Expected: all PASS and no untracked packaged binaries in the repository.

```powershell
git add CHANGELOG.md docs/NEXT_RELEASE_BUG_FIXES.md
git commit -m "test: record v0.18 fresh-seed acceptance"
```

Do not merge or push. Present the branch, test counts, artifact hashes, seed identifier, gameplay evidence, remaining open defects, and exact diff to the project owner for approval.
