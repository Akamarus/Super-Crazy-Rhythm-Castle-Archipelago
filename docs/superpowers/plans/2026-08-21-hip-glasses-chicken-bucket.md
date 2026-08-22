# Hip Glasses and Chicken Bucket Randomization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Randomize the Roots Hip Glasses and Chicken Bucket chain while preserving the native Level 4 reward, Bucket Minion trade, Lift Quest conversion, Combo Bucket ability, and Lobby arrival sequence.

**Architecture:** Add two permanent AP items and two permanent AP source locations, plus non-network event state for the native trade and Combo Bucket conversion. A focused client policy owns fail-closed activation, exact source-grant suppression, received-item reconciliation, and consumed-item precedence; `Plugin.cs` connects that policy to the existing save-request and Archipelago boundaries without forcing interactions or story flags.

**Tech Stack:** Python 3.13, Archipelago 0.6.7 world APIs, `unittest`, C#/.NET 8 pure policy tests, BepInEx/IL2CPP client plugin, PowerShell packaging and deployment helpers.

**Spec:** `docs/superpowers/specs/2026-08-21-hip-glasses-chicken-bucket-design.md`

## Global Constraints

- Use the verified native identifiers exactly: `HIP_GLASSES_BAG_ITEM`, `LEVEL_08_GLASSES_COLLECTED`, `ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES`, `ROOTS_HUB_BUCKET_MINION_DIALOGUE_PROGRESSION`, `ROOTS_HUB_BUCKET_MINION_BLOCKADE_REMOVED`, `ROOTS_HUB_KING_LIFT_CHAT_WITNESSED`, `CHICKEN_BUCKET_BAG_ITEM`, `COMBO_BUCKET_ABILITY`, `LEVEL_09_COMBO_ABILITY_EARNED`, `LEVEL_09_COMPLETED`, and `OVERALL_PROGRESS_REACHED_LOBBY_HUB`.
- Add only the network items `Hip Glasses` and `Chicken Bucket`; `Combo Bucket` remains a native, non-network ability.
- Add only the network locations `Roots - Level 4 - Hip Glasses` and `Roots - Bucket Minion Trade`.
- Allocate items `BASE_ID + 119` (`187256119`) and `BASE_ID + 120` (`187256120`), and locations `BASE_ID + 180` (`187256180`) and `BASE_ID + 181` (`187256181`); advance the next safe IDs to item `+121` and location `+182`.
- Area Access remains authoritative. Do not restore `Level 5 Access` or add any individual level-access item.
- Suppress only the two vanilla inventory grants. Never suppress Level 4 completion, Hip Glasses consumption, Chicken Bucket consumption, trade dialogue/blockade/King flags, Level 09 completion, Combo Bucket ability, or Lobby transition.
- Never force the Bucket Minion conversation, Lift Quest interaction, route opening, cutscene, or transition.
- Received-item history establishes ownership; durable native consumed markers take precedence during replay and reconnection.
- Fail closed until compatible slot-data synchronization completes. Vanilla and incompatible AP saves retain vanilla behavior.
- Preserve the implementation-version prefix `area-routing-plant-pipes-0.15-generation-foundation-0.16`; append this feature additively and require an explicit feature flag.
- Existing Weed Killer, Plant Pipes, cartridges, Music Lab checks, Area Access, and starter behavior must remain enabled.
- Generated Level 4 and Level 5 Star requirements remain preview-only until the separate Star system is activated; this feature must preserve `client_star_gate_enforcement_active: False` and must not make inactive Stars part of live reachability.
- Basic clears do not require Combo Bucket solely for score assistance. Add a Combo Bucket solver requirement only to an individually validated performance check.
- Build and test in the isolated worktree. Deploy only through the existing restricted helper to `BepInEx\plugins\RhythmCastleAP` after explicit approval.
- Do not merge or push without project-owner approval.

---

### Task 1: Permanent AP Definitions and Registry

**Files:**
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_items.py`
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `docs/IDS.md`

**Interfaces:**
- Consumes: existing `BASE_ID`, `ITEM_NAME_TO_ID`, `ITEM_CLASSIFICATIONS`, and `LOCATION_NAME_TO_ID` registries.
- Produces: `HIP_GLASSES_ITEM`, `CHICKEN_BUCKET_ITEM`, `ROOTS_LEVEL4_HIP_GLASSES`, and `ROOTS_BUCKET_MINION_TRADE` string constants and their permanent IDs.

- [ ] **Step 1: Write failing permanent-definition tests**

Add behavior assertions to the real-world harness in `test_world_integration.py` and classification assertions to `test_items.py`:

```python
self.assertEqual(module.ITEM_NAME_TO_ID[module.HIP_GLASSES_ITEM], 187256119)
self.assertEqual(module.ITEM_NAME_TO_ID[module.CHICKEN_BUCKET_ITEM], 187256120)
self.assertEqual(module.LOCATION_NAME_TO_ID[module.ROOTS_LEVEL4_HIP_GLASSES], 187256180)
self.assertEqual(module.LOCATION_NAME_TO_ID[module.ROOTS_BUCKET_MINION_TRADE], 187256181)
self.assertEqual(module.ITEM_CLASSIFICATIONS[module.HIP_GLASSES_ITEM], ItemClassification.progression)
self.assertEqual(module.ITEM_CLASSIFICATIONS[module.CHICKEN_BUCKET_ITEM], ItemClassification.progression)
self.assertEqual(len(set(module.ITEM_NAME_TO_ID.values())), len(module.ITEM_NAME_TO_ID))
self.assertEqual(len(set(module.LOCATION_NAME_TO_ID.values())), len(module.LOCATION_NAME_TO_ID))
```

- [ ] **Step 2: Run the focused tests and verify RED**

Run:

```powershell
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest apworld.tests.test_items apworld.tests.test_world_integration -v
```

Expected: FAIL because the four constants and IDs do not exist.

- [ ] **Step 3: Add the minimal definitions**

Add these exact entries in `apworld/scrc/__init__.py`:

```python
ROOTS_LEVEL4_HIP_GLASSES = "Roots - Level 4 - Hip Glasses"
ROOTS_BUCKET_MINION_TRADE = "Roots - Bucket Minion Trade"
LOCATION_NAME_TO_ID[ROOTS_LEVEL4_HIP_GLASSES] = BASE_ID + 180
LOCATION_NAME_TO_ID[ROOTS_BUCKET_MINION_TRADE] = BASE_ID + 181

HIP_GLASSES_ITEM = "Hip Glasses"
CHICKEN_BUCKET_ITEM = "Chicken Bucket"
ITEM_NAME_TO_ID[HIP_GLASSES_ITEM] = BASE_ID + 119
ITEM_NAME_TO_ID[CHICKEN_BUCKET_ITEM] = BASE_ID + 120
ITEM_CLASSIFICATIONS[HIP_GLASSES_ITEM] = ItemClassification.progression
ITEM_CLASSIFICATIONS[CHICKEN_BUCKET_ITEM] = ItemClassification.progression
```

Follow the file's existing dictionary construction order and do not renumber any existing entry.

- [ ] **Step 4: Record the allocations in the permanent registry**

Add the four rows to `docs/IDS.md`, then set the next safe values to:

```text
Item: +121 / 187256121
Location: +182 / 187256182
```

State that neither ID may be reused even if the feature changes later.

- [ ] **Step 5: Run the focused tests and verify GREEN**

Run the Step 2 command. Expected: PASS, including uniqueness and progression classification.

- [ ] **Step 6: Commit Task 1**

```powershell
git add apworld/scrc/__init__.py apworld/tests/test_items.py apworld/tests/test_world_integration.py docs/IDS.md
git commit -m "feat(apworld): register Roots bucket progression"
```

---

### Task 2: Solver Regions, Native Events, Item Pool, and Slot Data

**Files:**
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/support.py`
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `apworld/tests/test_repository_contract.py`
- Modify: `apworld/scrc/archipelago.json`
- Modify: `apworld/examples/SCRC-AreaRouting-PlantPipes.yaml`

**Interfaces:**
- Consumes: the four constants from Task 1 and existing live Roots Access, Weed Killer, Plant Pipes, and Area Access rules; generated Star requirements remain exported preview metadata only.
- Produces: two address-bearing source locations, internal events `Bucket Minion Trade Complete` and `Combo Bucket Event`, exactly one copy of each new network item, and slot-data feature contract `randomize_hip_glasses_chicken_bucket`.

- [ ] **Step 1: Extend the world harness for event locations**

Make the lightweight `Location` and `Item` stubs in `apworld/tests/support.py` retain `name`, `address`, `item`, and `access_rule`. Make `place_locked_item(item)` store `item` on the location. This lets tests distinguish network locations (`address is not None`) from internal events (`address is None`).

- [ ] **Step 2: Write failing integration tests for pool balance and graph shape**

Add tests that call the real `create_regions`, `create_items`, and `set_rules` methods and assert:

```python
self.assertEqual(item_names.count("Hip Glasses"), 1)
self.assertEqual(item_names.count("Chicken Bucket"), 1)
self.assertEqual(len(world.multiworld.itempool), len(module.LOCATION_NAME_TO_ID))
self.assertIsNotNone(location_by_name("Roots - Level 4 - Hip Glasses").address)
self.assertIsNotNone(location_by_name("Roots - Bucket Minion Trade").address)
self.assertIsNone(location_by_name("Bucket Minion Trade Complete").address)
self.assertEqual(location_by_name("Bucket Minion Trade Complete").item.name, "Bucket Minion Trade Complete")
self.assertIsNone(location_by_name("Combo Bucket Event").address)
self.assertEqual(location_by_name("Combo Bucket Event").item.name, "Combo Bucket Event")
```

Exercise each captured access rule with a fake state. Prove that the Level 4 source is false without Roots Access, Weed Killer, or Plant Pipes and true when all current live prerequisites are present. Prove the trade network location and trade event are false without Hip Glasses and true with it. Prove the Combo event is false without Chicken Bucket or the trade event and true with both plus the current live Lift Quest route. Separately assert that changing preview-only Level 4 or Level 5 Star thresholds does not change reachability while `client_star_gate_enforcement_active` is false.

- [ ] **Step 3: Write a failing representative no-self-lock generation test**

For seeds `0..99`, build the real region/rule graph with the test harness, distribute the live item pool using the harness's deterministic assumed-fill helper, and assert both progression items are reachable through sphere expansion:

```python
for seed in range(100):
    result = generate_and_sphere(seed)
    self.assertIn("Hip Glasses", result.collected_items, seed)
    self.assertIn("Chicken Bucket", result.collected_items, seed)
    self.assertTrue(result.can_reach("Bucket Minion Trade Complete"), seed)
    self.assertTrue(result.can_reach("Combo Bucket Event"), seed)
```

The helper must fail with the seed and remaining unreachable locations; it must not silently move either item to a reachable location.

- [ ] **Step 4: Write failing slot-data and compatibility tests**

Assert `fill_slot_data()` exports these exact values:

```python
"implementation_version": "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17"
"randomize_hip_glasses_chicken_bucket": True
"hip_glasses_item": "Hip Glasses"
"chicken_bucket_item": "Chicken Bucket"
"hip_glasses_source_location": "Roots - Level 4 - Hip Glasses"
"bucket_minion_trade_location": "Roots - Bucket Minion Trade"
"hip_glasses_source_flag": "LEVEL_08_GLASSES_COLLECTED"
"hip_glasses_native_flag": "HIP_GLASSES_BAG_ITEM"
"bucket_trade_flag": "ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES"
"chicken_bucket_native_flag": "CHICKEN_BUCKET_BAG_ITEM"
"combo_bucket_conversion_flag": "LEVEL_09_COMBO_ABILITY_EARNED"
```

Also assert the implementation version still starts with `area-routing-plant-pipes-0.15-generation-foundation-0.16`.

- [ ] **Step 5: Run the focused suite and verify RED**

Run:

```powershell
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest apworld.tests.test_world_integration apworld.tests.test_repository_contract -v
```

Expected: FAIL for missing regions, events, pool entries, rules, and slot-data fields.

- [ ] **Step 6: Implement the minimal APWorld graph**

Create the two network locations in the appropriate Roots regions. Add two addressless event locations and place locked progression event items named exactly `Bucket Minion Trade Complete` and `Combo Bucket Event`. Rules must use the existing rule helpers and this dependency chain:

```text
Roots - Level 4 - Hip Glasses
  <- Roots Access + Weed Killer + Plant Pipes

Roots - Bucket Minion Trade
Bucket Minion Trade Complete
  <- reachable Roots trade route + Hip Glasses

Combo Bucket Event
  <- Bucket Minion Trade Complete + Chicken Bucket
     + reachable Lift Quest route
```

Keep the generated Level 4 and Level 5 Star thresholds in slot-data preview metadata. Do not add them to live rules until the separately approved Star pool and client gate enforcement are active.

The locked event items have no IDs, never enter `ITEM_NAME_TO_ID`, and never enter `multiworld.itempool`. Append exactly one `Hip Glasses` and one `Chicken Bucket` in `create_items()`, then use the existing filler calculation so address-bearing locations and randomized items remain balanced.

- [ ] **Step 7: Export additive slot data and metadata**

Add the exact Step 4 fields. Advance the APWorld public version to `0.17`, metadata version/compatible version together according to the existing convention, and keep the v0.15/v0.16 prefix intact. Update the example YAML comments to describe the two active randomized items and physical interactions; do not add a user option to disable the chain in v0.17.

- [ ] **Step 8: Run focused and full APWorld tests**

Run the Step 5 command, then:

```powershell
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
```

Expected: all tests pass; the 100 representative seeds all sphere successfully.

- [ ] **Step 9: Commit Task 2**

```powershell
git add apworld/scrc/__init__.py apworld/scrc/archipelago.json apworld/examples/SCRC-AreaRouting-PlantPipes.yaml apworld/tests/support.py apworld/tests/test_world_integration.py apworld/tests/test_repository_contract.py
git commit -m "feat(apworld): model Hip Glasses and Chicken Bucket chain"
```

---

### Task 3: Pure Client Lifecycle Policy

**Files:**
- Create: `client/RootsBucketRandomizationPolicy.cs`
- Create: `client/tests/RootsBucketRandomization/RootsBucketRandomization.Tests.csproj`
- Create: `client/tests/RootsBucketRandomization/Program.cs`
- Modify: `client/RhythmCastleAP.csproj`

**Interfaces:**
- Consumes: synchronized-feature state, room ID, requested native flag/value, AP received count, native-held state, and durable consumed state.
- Produces: `RootsBucketItemKind`, `RootsBucketGrantDecision`, `RootsBucketRandomizationPolicy.IsCompatible`, `ShouldSuppressVanillaGrant`, and `DecideReceivedItemGrant`.

- [ ] **Step 1: Create the isolated executable test project**

Create a .NET 8 executable project linking only `../../RootsBucketRandomizationPolicy.cs`. Confirm `client/RhythmCastleAP.csproj` excludes `tests/**/*.cs` from the production compile exactly as it does for the existing diagnostic tests.

- [ ] **Step 2: Write a failing table-driven policy test**

In `Program.cs`, use explicit assertions for these signatures:

```csharp
internal enum RootsBucketItemKind { HipGlasses, ChickenBucket }
internal enum RootsBucketGrantDecision { Ignore, Apply, AlreadyHeld, Consumed }

RootsBucketRandomizationPolicy.IsCompatible(string? implementationVersion, bool featureFlag)
RootsBucketRandomizationPolicy.ShouldSuppressVanillaGrant(
    bool synchronized, bool compatible, string? roomId, string flagName, bool value)
RootsBucketRandomizationPolicy.DecideReceivedItemGrant(
    bool synchronized, bool compatible, int receivedCount, bool nativeHeld, bool consumed)
```

Cover every condition:

```text
compatible exact additive tag + feature true -> active
old tag, wrong prefix, missing tag, or feature false -> inactive
GameRoom_08 + HIP_GLASSES_BAG_ITEM=true -> suppress only when active
GameRoom_Hub2 + CHICKEN_BUCKET_BAG_ITEM=true -> suppress only when active
wrong room, false value, or any other flag -> do not suppress
received=0 -> Ignore
received>0, not held, not consumed -> Apply
received>0, held -> AlreadyHeld
received>0, consumed -> Consumed even if held was observed stale
unsynchronized or incompatible -> Ignore
```

- [ ] **Step 3: Run and verify RED**

Run:

```powershell
dotnet run --project .\client\tests\RootsBucketRandomization\RootsBucketRandomization.Tests.csproj
```

Expected: build FAIL because the policy file and types do not exist.

- [ ] **Step 4: Implement the minimal pure policy**

`IsCompatible` must require both the explicit feature flag and an implementation version that starts with:

```csharp
"area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17"
```

`ShouldSuppressVanillaGrant` must use `StringComparison.Ordinal`, exact room/flag pairs, `value == true`, and both synchronization booleans. `DecideReceivedItemGrant` must check `consumed` before `nativeHeld` so a stale held probe cannot resurrect a consumed item.

- [ ] **Step 5: Run and verify GREEN**

Run the Step 3 command. Expected: exit code 0 and a summary naming every passing case.

- [ ] **Step 6: Commit Task 3**

```powershell
git add client/RootsBucketRandomizationPolicy.cs client/tests/RootsBucketRandomization client/RhythmCastleAP.csproj
git commit -m "test(client): define Roots bucket lifecycle policy"
```

---

### Task 4: Client Runtime Integration and Reconciliation

**Files:**
- Modify: `client/Plugin.cs`
- Modify: `client/RootsBucketRandomizationPolicy.cs`
- Modify: `client/tests/RootsBucketRandomization/Program.cs`

**Interfaces:**
- Consumes: Task 2 slot data, Task 3 policy decisions, existing Archipelago received-item loop, save-request processor hooks, durable native flag reads, and existing queued location submission.
- Produces: `RootsBucketRandomization.Configure`, `ApplySlotData`, `TryApplyItem`, `CapturePlayerSaveRequestProcessor`, `TryFlushPendingNativeGrants`, `ShouldSuppressVanillaGrant`, and `RecordSourceCollected`.

- [ ] **Step 1: Add failing state-machine tests before runtime code**

Extend the pure policy with an immutable input/output transition:

```csharp
internal readonly record struct RootsBucketLifecycleState(
    int HipGlassesReceived,
    int ChickenBucketReceived,
    bool HipGlassesHeld,
    bool HipGlassesConsumed,
    bool ChickenBucketHeld,
    bool ChickenBucketConsumed);

internal readonly record struct RootsBucketReconciliation(
    RootsBucketGrantDecision HipGlasses,
    RootsBucketGrantDecision ChickenBucket);

RootsBucketRandomizationPolicy.Reconcile(
    bool synchronized,
    bool compatible,
    RootsBucketLifecycleState state)
```

Test initial delivery, repeated delivery, save/reload, reconnect while held, Hip Glasses consumed, Chicken Bucket consumed, both consumed, incompatible seed, and pre-sync fail-closed behavior. In every consumed case assert `Consumed`, never `Apply`.

- [ ] **Step 2: Run and verify RED**

Run the Task 3 test project. Expected: build FAIL because `Reconcile` and its records do not exist.

- [ ] **Step 3: Implement and verify the pure reconciliation function**

Implement `Reconcile` solely by calling `DecideReceivedItemGrant` for each item. Run the policy test project until it passes before editing `Plugin.cs`.

- [ ] **Step 4: Add the runtime coordinator**

Add `internal static class RootsBucketRandomization` beside the existing Weed Killer and Plant Pipes coordinators. It must own:

```csharp
internal const string HipGlassesItem = "Hip Glasses";
internal const string ChickenBucketItem = "Chicken Bucket";
internal const string HipGlassesLocation = "Roots - Level 4 - Hip Glasses";
internal const string BucketTradeLocation = "Roots - Bucket Minion Trade";
internal const string HipGlassesNativeFlag = "HIP_GLASSES_BAG_ITEM";
internal const string HipGlassesSourceFlag = "LEVEL_08_GLASSES_COLLECTED";
internal const string BucketTradeFlag = "ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES";
internal const string ChickenBucketNativeFlag = "CHICKEN_BUCKET_BAG_ITEM";
internal const string ChickenConsumedFlag = "LEVEL_09_COMBO_ABILITY_EARNED";
```

`ApplySlotData` resets synchronization, validates the exact metadata from Task 2, records compatibility, then reconciles after native state is available. `TryApplyItem` increments received history for only the two exact item names and queues reconciliation. `CapturePlayerSaveRequestProcessor` stores the processor and triggers a flush. `TryFlushPendingNativeGrants` reads the held and consumed flags, calls `Reconcile`, and submits only decisions equal to `Apply` through the existing progression-flag request pattern.

- [ ] **Step 5: Connect received items and slot data**

In the existing received-item loop, call:

```csharp
bool handledRootsBucket = RootsBucketRandomization.TryApplyItem(item.ItemName);
```

Include it in the same handled-item decision used by Weed Killer and Plant Pipes. After login slot data is accepted, call `RootsBucketRandomization.ApplySlotData(loginSuccess.SlotData)`. Call `Configure()` during plugin initialization.

- [ ] **Step 6: Connect exact suppression and source recording**

In the existing progression-request prefix, after capturing the processor and before the native request executes:

```csharp
if (RootsBucketRandomization.ShouldSuppressVanillaGrant(req, flag))
    return false;
```

The method may return true only for:

```text
GameRoom_08 + HIP_GLASSES_BAG_ITEM=true
GameRoom_Hub2 + CHICKEN_BUCKET_BAG_ITEM=true
```

and only after compatible synchronization. In the postfix, call `RecordSourceCollected(req, flag)`; submit Level 4 from `LEVEL_08_GLASSES_COLLECTED=true` and the trade from `ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES=true` using the existing durable, idempotent queued-location mechanism.

- [ ] **Step 7: Protect AP-applied grants from self-suppression**

Wrap each client-originated native grant with an internal apply-depth guard:

```csharp
_applyingArchipelagoGrant++;
try
{
    WeedKillerRandomization.TrySubmitProgressionFlag(processor, flagName, true, out detail);
}
finally
{
    _applyingArchipelagoGrant--;
}
```

`ShouldSuppressVanillaGrant` must return false while the guard is nonzero. Do not set trade, dialogue, blockade, King-chat, Combo Bucket, completion, or transition flags from AP code.

- [ ] **Step 8: Add structured lifecycle logging**

Log one concise line for activation, each received count, each native grant result, each source submission, each consumed-state skip, and each fail-closed reason. Never print the API key, connection password, or complete received history. Use existing log levels and avoid per-frame messages.

- [ ] **Step 9: Run client tests and compile the plugin**

Run:

```powershell
dotnet run --project .\client\tests\DiagnosticHotkeyRouting.Tests.csproj
dotnet run --project .\client\tests\RootsBucketRandomization\RootsBucketRandomization.Tests.csproj
dotnet build .\client\RhythmCastleAP.csproj -c Release
```

Expected: both executable test suites exit 0 and the Release build has zero errors. Do not deploy the DLL.

- [ ] **Step 10: Commit Task 4**

```powershell
git add client/Plugin.cs client/RootsBucketRandomizationPolicy.cs client/tests/RootsBucketRandomization/Program.cs
git commit -m "feat(client): randomize Roots bucket progression"
```

---

### Task 5: Repository Contracts, Versions, and Tester Documentation

**Files:**
- Modify: `tools/validate-repo.py`
- Modify: `apworld/tests/test_repository_contract.py`
- Modify: `client/RhythmCastleAP.csproj`
- Modify: `README.md`
- Modify: `apworld/README.md`
- Modify: `docs/PROGRESSION.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/TESTING.md`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: all production contracts from Tasks 1–4.
- Produces: APWorld `0.17`, client `0.67.60`, fail-closed repository validation, install/testing guidance, and an acceptance checklist.

- [ ] **Step 1: Write failing validator fixture tests**

Extend `test_repository_contract.py` to copy the repository into its temporary fixture and verify the validator accepts the real contract, then independently mutate and reject each of:

```text
Hip Glasses ID 187256119
Chicken Bucket ID 187256120
Level 4 source ID 187256180
Bucket trade ID 187256181
the preserved implementation-version prefix
the explicit feature flag
one required native flag string
the exact item/location slot-data names
```

Each rejection must be based on parsed top-level Python/PowerShell/C# structure already supported by the validator, not a match that can be satisfied by comments, strings, nested manifest data, or test fixtures.

- [ ] **Step 2: Run and verify RED**

Run:

```powershell
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest apworld.tests.test_repository_contract -v
```

Expected: FAIL because the validator does not yet require the v0.17 chain.

- [ ] **Step 3: Update validation and versions**

Require APWorld `0.17`, client `0.67.60`, the four permanent IDs, the additive implementation tag, explicit feature metadata, and all five inventory/source/consumption flags used by the client. Preserve every prior validation rule. Keep the validator Python-standard-library-only so CI's Python Alpine job remains supported.

- [ ] **Step 4: Update design and public documentation**

Document the two randomized items and checks, physical interaction requirements, received-item reconciliation, non-network Combo Bucket event, fresh-seed requirement, APWorld/client versions, and that there is no `Level 5 Access`. In `docs/TESTING.md`, retain and prominently link the existing no-Combo-Bucket report template, including level/song, native difficulty, AP tier, score/stars/medal/objective, player count, abilities, attempt count, best result, log excerpt, and optional video.

- [ ] **Step 5: Run the complete automated gate**

Run:

```powershell
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
dotnet run --project .\client\tests\DiagnosticHotkeyRouting.Tests.csproj
dotnet run --project .\client\tests\RootsBucketRandomization\RootsBucketRandomization.Tests.csproj
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' .\tools\validate-repo.py
dotnet build .\client\RhythmCastleAP.csproj -c Release
.\tools\build-apworld.ps1
git diff --check
git status --short
```

Expected: all Python and C# tests pass, validation reports APWorld `0.17` and client `0.67.60`, both builds succeed, diff check is silent, and only intended tracked files are changed.

- [ ] **Step 6: Inspect the package and deployment boundary**

List `dist/scrc.apworld` and confirm it contains production `scrc` modules and metadata but no tests, saves, logs, secrets, proprietary assemblies, or client DLL. Confirm no command in this task copied files into the game directory.

- [ ] **Step 7: Commit Task 5**

```powershell
git add tools/validate-repo.py apworld/tests/test_repository_contract.py client/RhythmCastleAP.csproj README.md apworld/README.md docs/PROGRESSION.md docs/ROADMAP.md docs/TESTING.md CHANGELOG.md
git commit -m "docs: publish Roots bucket randomization testing guidance"
```

---

### Task 6: Fresh-Seed Gameplay Acceptance

**Files:**
- Create: `docs/testing/2026-08-21-hip-glasses-chicken-bucket-acceptance.md`
- Modify only if a defect is found: the smallest production/test files from Tasks 1–5.

**Interfaces:**
- Consumes: packaged APWorld v0.17, Release client v0.67.60, existing restricted deployment helper, a fresh generated seed, a fresh game save, and development-session logs.
- Produces: a timestamped acceptance record with seed, slot, versions, observed flags, checks, reconnect results, vanilla result, and unresolved performance evidence.

- [ ] **Step 1: Create the acceptance record before launching the game**

Create the document with unchecked rows for all eight spec acceptance cases and fields for APWorld hash/version, client hash/version, seed name, slot name, save identity, game difficulty, player count, start/end timestamps, log path, and tester notes.

- [ ] **Step 2: Package and request deployment approval**

Re-run the complete Task 5 gate. Present the exact built APWorld and DLL paths and ask for explicit approval before using the restricted deployment helper. Do not manually copy to any other BepInEx directory.

- [ ] **Step 3: Generate a fresh compatible seed and start a fresh save**

Confirm the connection log reports the exact additive implementation tag and feature flag. Before Level 4, record that neither new native bag flag is set and no new source location is checked.

- [ ] **Step 4: Verify Level 4 source behavior**

Complete Level 4 normally. Record evidence that `LEVEL_08_GLASSES_COLLECTED=true`, `Roots - Level 4 - Hip Glasses` was sent once, and `HIP_GLASSES_BAG_ITEM` remains false until AP delivers Hip Glasses. Confirm completion, cutscene, and unrelated flags remain native.

- [ ] **Step 5: Verify Hip Glasses delivery and Bucket Minion trade**

Deliver Hip Glasses through AP, interact normally, and record: held flag becomes true; the normal trade consumes it; trade/dialogue/blockade/King flags remain native; the trade check sends once; and Chicken Bucket is not locally granted by the trade.

- [ ] **Step 6: Verify Chicken Bucket delivery and Lift Quest conversion**

Deliver Chicken Bucket through AP, perform Lift Quest normally, and record: held flag becomes true; the quest consumes it; `COMBO_BUCKET_ABILITY`, `LEVEL_09_COMBO_ABILITY_EARNED`, and `LEVEL_09_COMPLETED` become true; and the native transition reaches `GameRoom_Hub1A` with the Lobby cutscene.

- [ ] **Step 7: Verify replay and reconnection at each lifecycle boundary**

Save/reload and disconnect/reconnect once while each item is held and once after each is consumed. Record that held items remain available, consumed items do not return, checks do not duplicate, no interaction is forced, and no ability is duplicated.

- [ ] **Step 8: Verify fail-closed and vanilla behavior**

Connect once with an incompatible/old seed and verify neither suppression path activates. Then run a non-AP save through the focused source/trade sequence and record that vanilla Hip Glasses and Chicken Bucket grants occur normally.

- [ ] **Step 9: Record Combo Bucket feasibility observations**

Attempt representative later score/objective content without Combo Bucket when practical. Record exact content, difficulty/tier, score/stars/medal/objective, player count, abilities, attempts, best result, and log/video references. Do not change solver rules from a failed attempt alone.

- [ ] **Step 10: Resolve failures test-first**

For each defect, stop gameplay, preserve the focused log excerpt, add the smallest automated regression test that reproduces the failure, verify RED, implement the minimal fix, re-run the complete automated gate, and repeat only the affected gameplay stage plus all later stages.

- [ ] **Step 11: Commit the acceptance evidence**

```powershell
git add docs/testing/2026-08-21-hip-glasses-chicken-bucket-acceptance.md
git commit -m "test: record Roots bucket gameplay acceptance"
```

Do not include saves, full logs, credentials, or proprietary game files.

---

## Final Review and Handoff

- [ ] Run the full Python suite, both C# executable suites, repository validator, Release client build, APWorld package build, and `git diff --check` from a clean worktree.
- [ ] Compare the finished branch with the approved spec and confirm both source grants, both received items, both consumed-state guards, the native Combo Bucket conversion, and all compatibility constraints are represented.
- [ ] Confirm no `Level 5 Access`, automatic conversation, automatic route opening, synthesized Combo Bucket grant, blanket Combo Bucket score rule, unrestricted deployment, merge, or push was introduced.
- [ ] Request an independent whole-branch code review against the spec and this plan.
- [ ] Fix every Critical or Important review finding with a newly observed failing test before changing production code.
- [ ] Present automated and gameplay evidence to the project owner. Merge or push only after explicit approval.
