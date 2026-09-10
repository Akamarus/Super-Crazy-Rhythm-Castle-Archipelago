# AP Music Lab Points Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace native Music Lab medal-score chest currency with a strict, weighted, solver-aware Archipelago inventory for compatible v0.23 seeds while preserving native behavior for older seeds and non-AP play.

**Architecture:** APWorld owns an immutable three-item point catalog, adds 20 progression items by replacing Stardust, and gates the nine existing chests with a weighted `CollectionState` total. The client validates an exact v0.23 slot-data contract, rebuilds its effective total from `AllItemsReceived`, and applies that total at the already identified `CurrentPlayerSaveEnquiries.GetMedalScore()` boundary only after a diagnostic checkpoint proves the boundary. Native medal storage is never written.

**Tech Stack:** Python 3.13, Archipelago 0.6.7 world APIs, `unittest`, C#/.NET 6 client, .NET 8 pure-policy console tests, Archipelago.MultiClient.Net 6.7.1, Harmony/BepInEx IL2CPP, PowerShell build and packaging scripts.

**Spec:** `docs/superpowers/specs/2026-09-09-music-lab-points-design.md`

## Global Constraints

- Target APWorld version is exactly `0.23.0`; target client version is exactly `0.69.0`.
- Append `music-lab-points-0.23` to the complete existing implementation-version string; preserve every historical prefix.
- Advance top-level slot-data `schema_version` from `13` to `14`; use Music Lab Point sub-schema `1`.
- Allocate item IDs in order from the current frontier: `187256153`, `187256154`, and `187256155`; advance the next safe item ID to `187256156`.
- Allocate no location IDs; preserve next safe location ID `187256211`.
- Generate exactly 10 one-point items, 3 ten-point bundles, and 7 twenty-point large bundles: 20 instances worth 180 total.
- Replace exactly 20 Stardust instances; do not change addressed location counts `92/129/166/202`.
- Preserve chest thresholds `5/10/20/32/46/64/89/111/140` and their existing permanent locations.
- Preserve the five cassette and two Garage cartridge source reuses; create no duplicate checks.
- Before compatible v0.23 history synchronization, return zero and keep point chests locked. After synchronization, retain the last total during temporary disconnect.
- Recognized v0.22 and older seeds, plus non-AP play, must continue using native medal score.
- A malformed seed claiming v0.23 must fail closed at zero with an explicit incompatibility result.
- Never write AP points into native medal/save state, force a chest open, simulate its interaction, or enable the Shift+F4 cheat over a compatible AP point session.
- Diagnose the native score boundary before enabling production replacement. If evidence contradicts the design, stop and revise the spec instead of guessing.
- Build without deployment first. Deploy only after explicit approval and only to `D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP`.
- Do not merge, push, publish, package as a release, remove worktrees, or alter the unrelated `WordFactori/` directory without explicit approval.
- Gameplay acceptance is required before the feature may be described as live-verified.

## File and Responsibility Map

- `apworld/scrc/music_lab_points.py` — immutable point item catalog, thresholds, pool expansion, and weighted state calculation.
- `apworld/scrc/items.py` — exports point item IDs/classifications into the existing item registry.
- `apworld/scrc/__init__.py` — creates the 20 items, installs chest access rules, and emits the v0.23 slot-data contract.
- `apworld/scrc/placement.py` — removes the obsolete blanket ban on progression at point chests.
- `apworld/tests/test_music_lab_points.py` — focused catalog, arithmetic, contract, and threshold tests.
- `apworld/tests/test_world_integration.py` — pool balance, location count, placement, and solver-matrix integration tests.
- `client/MusicLabPointPolicy.cs` — pure strict-contract parser and identity-bound point state machine.
- `client/MusicLabPointRandomization.cs` — thread-safe adapter between the pure state machine, network history, and score postfix.
- `client/ReceivedItemDispatch.cs` — recognizes point items so they never fall through to experimental native progression.
- `client/Plugin.cs` — connection lifecycle wiring, bounded diagnostic hook, and effective-score integration.
- `client/tests/MusicLabPoints/` — pure client contract/runtime and production-wiring regression project.
- `docs/testing/2026-09-09-music-lab-points-acceptance.md` — hashes, versions, seed/save identity, diagnostic evidence, and live acceptance checklist.
- `docs/IDS.md`, living docs, embedded APWorld docs, `CHANGELOG.md`, `client/build.ps1`, `apworld/scrc/archipelago.json`, and `tools/validate-repo.py` — permanent IDs, candidate versions, truthful status, build text, and independent validation.

---

### Task 1: Add the Immutable APWorld Point Catalog and Permanent IDs

**Files:**
- Create: `apworld/scrc/music_lab_points.py`
- Create: `apworld/tests/test_music_lab_points.py`
- Modify: `apworld/scrc/items.py`
- Modify: `docs/IDS.md`

**Interfaces:**
- Consumes: current base ID `187256000` and next safe item offset `+153`.
- Produces: `MusicLabPointItem`, `MUSIC_LAB_POINT_ITEMS`, `MUSIC_LAB_POINT_ITEMS_BY_NAME`, `MUSIC_LAB_POINT_ITEMS_BY_ID`, `MUSIC_LAB_POINT_POOL`, `MUSIC_LAB_POINT_TOTAL_INSTANCES`, `MUSIC_LAB_POINT_TOTAL_VALUE`, `MUSIC_LAB_POINT_MAX_EFFECTIVE`, and `MUSIC_LAB_POINT_THRESHOLDS`.

- [ ] **Step 1: Write focused catalog tests that specify the permanent contract**

Create `test_music_lab_points.py` and load the module with `load_scrc_module("music_lab_points")`. Assert this exact table and the unchanged location frontier:

```python
expected = (
    ("Music Lab Point", 187256153, 1, 10),
    ("Music Lab Point Bundle", 187256154, 10, 3),
    ("Music Lab Point Large Bundle", 187256155, 20, 7),
)
self.assertEqual(
    tuple((x.name, x.item_id, x.value, x.count) for x in points.MUSIC_LAB_POINT_ITEMS),
    expected,
)
self.assertEqual(len(points.MUSIC_LAB_POINT_POOL), 20)
self.assertEqual(sum(x.value * x.count for x in points.MUSIC_LAB_POINT_ITEMS), 180)
self.assertEqual(points.MUSIC_LAB_POINT_MAX_EFFECTIVE, 180)
self.assertEqual(tuple(points.MUSIC_LAB_POINT_THRESHOLDS), (5, 10, 20, 32, 46, 64, 89, 111, 140))
```

Add negative fixture tests proving duplicate names, duplicate IDs, non-positive values/counts, a non-180 total, or a non-monotonic threshold list raises `ValueError` with the failing field in the message.

- [ ] **Step 2: Run the focused test and verify RED**

Run:

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_music_lab_points.py' -v
```

Expected: FAIL because `apworld/scrc/music_lab_points.py` does not exist.

- [ ] **Step 3: Implement the immutable catalog and validation**

Create the module with this public shape:

```python
from dataclasses import dataclass

BASE_ID = 187256000
MUSIC_LAB_POINT_SCHEMA = 1

@dataclass(frozen=True)
class MusicLabPointItem:
    name: str
    item_id: int
    value: int
    count: int

MUSIC_LAB_POINT_ITEMS = (
    MusicLabPointItem("Music Lab Point", BASE_ID + 153, 1, 10),
    MusicLabPointItem("Music Lab Point Bundle", BASE_ID + 154, 10, 3),
    MusicLabPointItem("Music Lab Point Large Bundle", BASE_ID + 155, 20, 7),
)

MUSIC_LAB_POINT_THRESHOLDS = {
    5: "Music Lab - 5 Point Chest",
    10: "Music Lab - 10 Point Chest",
    20: "Music Lab - 20 Point Chest",
    32: "Music Lab - 32 Point Chest",
    46: "Music Lab - 46 Point Chest",
    64: "Music Lab - 64 Point Chest",
    89: "Music Lab - 89 Point Chest",
    111: "Music Lab - 111 Point Chest",
    140: "Music Lab - 140 Point Chest",
}
```

Derive both lookup dictionaries, the 20-name tuple, and totals from the table. Call `validate_music_lab_point_catalog()` at import so invalid catalog edits fail generation immediately.

- [ ] **Step 4: Export IDs and progression classifications through `items.py`**

Import the catalog and add derived mappings:

```python
MUSIC_LAB_POINT_ITEM_NAME_TO_ID = {
    entry.name: entry.item_id for entry in MUSIC_LAB_POINT_ITEMS
}
MUSIC_LAB_POINT_ITEM_CLASSIFICATIONS = {
    entry.name: ItemClassification.progression for entry in MUSIC_LAB_POINT_ITEMS
}
```

Do not put these items into `planned_star_names()` or reuse the inactive Star ID.

- [ ] **Step 5: Record permanent IDs**

Add offsets `+153..+155` and the three exact names to `docs/IDS.md`. Change only the item frontier to `+156` / `187256156`; keep location frontier `+211` / `187256211`.

- [ ] **Step 6: Run focused and item-registry tests**

Run:

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_music_lab_points.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_items.py' -v
```

Expected: all tests PASS; the catalog reports 20 instances and 180 points.

- [ ] **Step 7: Commit the catalog**

```powershell
git add apworld/scrc/music_lab_points.py apworld/scrc/items.py apworld/tests/test_music_lab_points.py docs/IDS.md
git commit -m "feat(apworld): define Music Lab Point items"
```

---

### Task 2: Activate the 20-Item Pool and Weighted Chest Rules

**Files:**
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/scrc/placement.py`
- Modify: `apworld/scrc/music_lab_points.py`
- Modify: `apworld/tests/test_music_lab_points.py`
- Modify: `apworld/tests/test_world_integration.py`

**Interfaces:**
- Consumes: `MUSIC_LAB_POINT_POOL` and `MUSIC_LAB_POINT_THRESHOLDS` from Task 1.
- Produces: `weighted_music_lab_points(state, player) -> int` and live access rules for the nine existing chest locations.

- [ ] **Step 1: Write failing weighted-arithmetic and threshold tests**

Use a fake state whose `count(name, player)` returns controlled counts. Cover zero; ten singles; one of each bundle; 31 versus 32; 139 versus 140; and counts for a different player. Specify the formula directly:

```python
self.assertEqual(points.weighted_music_lab_points(state, 1),
                 singles * 1 + bundles * 10 + large_bundles * 20)
```

In `test_world_integration.py`, replace `test_all_music_lab_point_chests_reject_required_progression` with tests asserting:

- all nine chests accept progression placement;
- every chest's `access_rule` is false at `threshold - 1` and true at `threshold`;
- five cassette and two Garage cartridge source names still resolve to the same seven chest objects; and
- addressed location counts remain exactly `92/129/166/202`.

Rewrite the retained BK fixture assertion rather than deleting it: the old Platinum placements must remain rejected, while `Plant Pipes` at the 64-point chest is no longer rejected by `item_rule` alone and is accepted only in a sphere simulation that can first collect 64 weighted points. In the 100-seed audit, remove point chests from the hard-coded `unsafe` predicate and let their `access_rule(state)` decide reachability.

- [ ] **Step 2: Run the focused tests and verify RED**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
```

Expected: FAIL because the point items are not live, chest rules still have no weighted access requirement, and `placement.py` still rejects progression at point chests.

- [ ] **Step 3: Add weighted state arithmetic**

Implement:

```python
def weighted_music_lab_points(state, player: int) -> int:
    return sum(state.count(entry.name, player) * entry.value
               for entry in MUSIC_LAB_POINT_ITEMS)
```

Do not clamp solver state; the generated pool itself is exactly 180. Client clamping is a runtime safety policy only.

- [ ] **Step 4: Add the point mappings and 20 instances to the live item pool**

Import Task 1's mappings into `ITEM_NAME_TO_ID` and `ITEM_CLASSIFICATIONS`. In `create_items()`, extend `progression_items` with all names from `MUSIC_LAB_POINT_POOL` before capacity is calculated. Leave the existing `filler_count = capacity - required_count` calculation intact so exactly 20 fewer Stardust items are generated.

- [ ] **Step 5: Install the nine chest access rules**

In `set_rules()`, add one default-argument-safe closure per threshold:

```python
for threshold, location_name in MUSIC_LAB_POINT_THRESHOLDS.items():
    set_rule(
        self.multiworld.get_location(location_name, self.player),
        lambda state, required=threshold: (
            weighted_music_lab_points(state, self.player) >= required
        ),
    )
```

Do not add a second location, event, or locked reward for any chest.

- [ ] **Step 6: Remove only the obsolete blanket chest restriction**

Delete the `Music Lab - ... Point Chest` early return from `required_progression_allowed()`. Preserve tiered-location filtering and every filler-only restriction for synthetic caches and unverified physical sources.

- [ ] **Step 7: Add a deterministic solver-chain matrix**

Change the integration-test `State` from a set to `collections.Counter`, add `count(name, player)`, and add `collect(name)` so repeated point items remain distinct while `has(name, player)` still means count greater than zero. Extend the existing 100-seed × four-difficulty × two-start matrix so its sphere walk calls `state.collect(item.name)`, recalculates weighted points, and proves all generated required progression can be collected. Include two explicit fixtures:

- a valid chain where early locations provide 5 points, the 5-point chest advances the chain, and later chests become reachable; and
- an impossible fixture where all point value is placed behind thresholds it cannot reach, which the audit must reject.

- [ ] **Step 8: Run the APWorld suite**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
```

Expected: all tests PASS, all option matrices sphere, and location counts are unchanged.

- [ ] **Step 9: Commit APWorld behavior**

```powershell
git add apworld/scrc/__init__.py apworld/scrc/placement.py apworld/scrc/music_lab_points.py apworld/tests/test_music_lab_points.py apworld/tests/test_world_integration.py
git commit -m "feat(apworld): gate Music Lab chests with AP points"
```

---

### Task 3: Publish the Strict v0.23 Slot-Data Contract

**Files:**
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/scrc/archipelago.json`
- Modify: `apworld/tests/test_music_lab_points.py`
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `tools/validate-repo.py`
- Modify: `apworld/tests/test_repository_contract.py`

**Interfaces:**
- Consumes: the immutable catalog and threshold map from Task 1.
- Produces: the exact v0.23 fields consumed by `MusicLabPointContract.ValidateSlotData()` in Task 4.

- [ ] **Step 1: Write failing exact slot-data tests**

Assert the version suffix, top-level schema, and these deterministic fields:

```python
self.assertTrue(data["implementation_version"].endswith("music-lab-points-0.23"))
self.assertEqual(data["schema_version"], 14)
self.assertTrue(data["music_lab_points_enabled"])
self.assertEqual(data["music_lab_points_schema"], 1)
self.assertEqual(data["music_lab_point_items"], {
    "Music Lab Point": 187256153,
    "Music Lab Point Bundle": 187256154,
    "Music Lab Point Large Bundle": 187256155,
})
self.assertEqual(data["music_lab_point_values"], {
    "Music Lab Point": 1,
    "Music Lab Point Bundle": 10,
    "Music Lab Point Large Bundle": 20,
})
self.assertEqual(data["music_lab_point_counts"], {
    "Music Lab Point": 10,
    "Music Lab Point Bundle": 3,
    "Music Lab Point Large Bundle": 7,
})
self.assertEqual(data["music_lab_point_total_instances"], 20)
self.assertEqual(data["music_lab_point_total_value"], 180)
self.assertEqual(data["music_lab_point_max_effective"], 180)
self.assertEqual(tuple(map(int, data["music_lab_point_thresholds"])),
                 (5, 10, 20, 32, 46, 64, 89, 111, 140))
```

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
```

Expected: FAIL on the old v0.22 version/schema and absent point fields.

- [ ] **Step 3: Emit the exact contract from catalog-derived data**

Append `-music-lab-points-0.23` to the existing implementation string, set schema `14`, and serialize maps in `MUSIC_LAB_POINT_ITEMS` / threshold insertion order. Do not repeat numeric constants in `__init__.py`; derive them from the catalog.

- [ ] **Step 4: Advance only APWorld package metadata**

Set `world_version` to `0.23.0` in `archipelago.json`. Preserve handler `version: 7`, `compatible_version: 7`, and minimum Archipelago `0.6.7`.

- [ ] **Step 5: Advance the independent APWorld contract checks while retaining the old client version**

Update `tools/validate-repo.py` and `test_repository_contract.py` to expect APWorld `0.23.0`, schema 14, the complete implementation suffix, the three new IDs, the `10/3/7` counts, total 180, thresholds, next item frontier 156, and unchanged location frontier 211. Keep the validator's client expectation at `0.68.0` until Task 8 actually changes the client version. Add fixture mutations that independently alter one point ID, value, count, total, and threshold and assert a precise validation failure.

- [ ] **Step 6: Run focused tests, independent validation, and package once**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
py .\tools\validate-repo.py
powershell -ExecutionPolicy Bypass -File .\tools\build-apworld.ps1
```

Expected: focused behavior tests, repository contract, validator, and packaging PASS. The validator reports transitional Client `0.68.0` with APWorld `0.23.0`; no runtime claim is made until the client tasks are complete.

- [ ] **Step 7: Commit the server contract**

```powershell
git add apworld/scrc/__init__.py apworld/scrc/archipelago.json apworld/tests/test_music_lab_points.py apworld/tests/test_world_integration.py tools/validate-repo.py apworld/tests/test_repository_contract.py
git commit -m "feat(apworld): publish Music Lab Point slot contract"
```

---

### Task 4: Build the Pure Client Contract and Point State Machine

**Files:**
- Create: `client/MusicLabPointPolicy.cs`
- Create: `client/tests/MusicLabPoints/MusicLabPoints.Tests.csproj`
- Create: `client/tests/MusicLabPoints/Program.cs`

**Interfaces:**
- Consumes: the exact Task 3 slot fields.
- Produces: `MusicLabPointCompatibilityMode`, `MusicLabPointCompatibilityResult`, `MusicLabPointSessionIdentity`, `MusicLabPointReceipt`, `MusicLabPointRuntimeMode`, `MusicLabPointSnapshot`, `MusicLabPointContract.ValidateSlotData(...)`, and `MusicLabPointRuntime`.

- [ ] **Step 1: Create the test project and write failing contract tests**

The project targets `net8.0` and links only `../../MusicLabPointPolicy.cs`. In `Program.cs`, construct an exact compatible dictionary and prove every mutation fails independently: missing field, extra map entry, old schema, wrong feature flag, renamed item, changed ID/value/count, wrong totals, changed threshold/location, duplicate ID, and v0.23 suffix without the sub-contract.

Also prove a v0.22 implementation string returns `LegacyNative`, while a complete v0.23 contract returns `Compatible`.

- [ ] **Step 2: Write failing runtime-state tests**

Use these exact public shapes:

```csharp
internal readonly record struct MusicLabPointSessionIdentity(
    string Game, string Seed, string Slot, int Schema);
internal readonly record struct MusicLabPointReceipt(int Index, long ItemId);
internal readonly record struct MusicLabPointSnapshot(
    MusicLabPointRuntimeMode Mode, int Total, int AcceptedInstances, string Detail);
```

Tests must cover native, awaiting zero, synchronized weighted sum, duplicate receipt index, unknown ID, 180 cap, disconnect retention, same-identity reconnect, different-identity reset, malformed-v0.23 zero, and developer override precedence only in native mode.

- [ ] **Step 3: Run the new project and verify RED**

```powershell
dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release
```

Expected: FAIL because `MusicLabPointPolicy.cs` does not exist.

- [ ] **Step 4: Implement strict contract parsing**

Define three compatibility modes: `LegacyNative`, `Compatible`, and `IncompatibleClaim`. `ValidateSlotData(Dictionary<string, object>?)` treats a version ending in `music-lab-points-0.23` as a claim and validates every Task 3 field exactly. A pre-v0.23 string is legacy. A v0.23 claim with any mismatch returns `IncompatibleClaim` with one bounded `key expected actual` detail.

Use explicit numeric conversion that accepts `int`, in-range `long`, and numeric strings because deserialized slot-data number types vary. Reject booleans as numbers. Parse maps through `System.Collections.IDictionary`/enumerable entries without depending on JSON implementation types.

- [ ] **Step 5: Implement the identity-bound state machine**

Expose `internal MusicLabPointSnapshot Snapshot { get; }`. `Configure(result, identity)` enters native, incompatible-zero, awaiting-zero, or retains a disconnected total only when the complete identity matches. `Synchronize(identity, receipts)` deduplicates by `Index`, sums only exact permanent IDs, saturates at 180, and enters `Synchronized`. `MarkDisconnected(identity)` enters `RetainedDisconnected` only from synchronized state. `Reset()` returns to native and clears receipts.

Implement score precedence as:

```csharp
internal int ResolveEffectiveScore(int nativeScore, int? developerOverride) =>
    Snapshot.Mode switch
    {
        MusicLabPointRuntimeMode.AwaitingInitialSynchronization => 0,
        MusicLabPointRuntimeMode.Incompatible => 0,
        MusicLabPointRuntimeMode.Synchronized => Snapshot.Total,
        MusicLabPointRuntimeMode.RetainedDisconnected => Snapshot.Total,
        _ => developerOverride ?? nativeScore,
    };
```

- [ ] **Step 6: Run RED-to-GREEN and boundary cases**

```powershell
dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release
```

Expected: PASS with a final `Music Lab Point policy tests passed.` line.

- [ ] **Step 7: Commit the pure client policy**

```powershell
git add client/MusicLabPointPolicy.cs client/tests/MusicLabPoints
git commit -m "feat(client): model AP Music Lab Point state"
```

---

### Task 5: Wire Authoritative Received History and Connection Lifecycle

**Files:**
- Create: `client/MusicLabPointRandomization.cs`
- Modify: `client/ReceivedItemDispatch.cs`
- Modify: `client/Plugin.cs`
- Modify: `client/tests/MusicLabPoints/Program.cs`
- Modify: `client/tests/ReconnectPolicy/Program.cs`
- Modify: `client/tests/CassetteRandomization/Program.cs`
- Modify: `client/tests/PreviewAbilities/Program.cs`

**Interfaces:**
- Consumes: Task 4 policy types and Archipelago.MultiClient.Net `IReceivedItemsHelper.AllItemsReceived`.
- Produces: `MusicLabPointRandomization.Configure()`, `ApplySlotData(...)`, `SynchronizeHistory(...)`, `TryHandleItemName(...)`, `OnDisconnected(...)`, `Reset()`, `Snapshot`, and `ResolveEffectiveScore(...)`.

- [ ] **Step 1: Add failing adapter and wiring tests**

Specify that:

- point item names are handled before experimental fallback;
- item names alone never increment the total;
- login applies slot data, then rebuilds from `session.Items.AllItemsReceived`, then sets `_connected = true`;
- the item callback rebuilds from `helper.AllItemsReceived` after draining the queue;
- history conversion uses the list ordinal as `MusicLabPointReceipt.Index` and `NetworkItem.Item` as the permanent ID;
- disconnect retains the current identity's total; and
- replacement/shutdown cannot leak an old total into a new identity.

- [ ] **Step 2: Run client tests and verify RED**

```powershell
dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release
dotnet run --project .\client\tests\ReconnectPolicy\ReconnectPolicy.Tests.csproj -c Release
```

Expected: FAIL on absent adapter/lifecycle wiring.

- [ ] **Step 3: Implement the thread-safe adapter**

Wrap one `MusicLabPointRuntime` behind a private lock and active connection generation. `ApplySlotData` accepts `(slotData, game, seed, slot, generation)`, builds `MusicLabPointSessionIdentity`, validates the contract, and configures the runtime. `SynchronizeHistory` accepts only the active generation and a materialized `IEnumerable<MusicLabPointReceipt>`. Log only state transitions and total changes.

Use these exact adapter signatures:

```csharp
internal static void Configure();
internal static MusicLabPointSnapshot ApplySlotData(
    Dictionary<string, object>? slotData,
    string game,
    string seed,
    string slot,
    long generation);
internal static MusicLabPointSnapshot SynchronizeHistory(
    long generation,
    IEnumerable<MusicLabPointReceipt> receipts);
internal static bool TryHandleItemName(string itemName);
internal static void OnDisconnected(long generation);
internal static void Reset();
internal static MusicLabPointSnapshot Snapshot { get; }
internal static int ResolveEffectiveScore(int nativeScore, int? developerOverride);
```

`TryHandleItemName` returns true for the three exact item names so the generic native fallback does nothing; it does not change point state. Only `SynchronizeHistory` changes authoritative receipt counts.

- [ ] **Step 4: Add the point handler to dispatch**

Insert `Func<string, bool> musicLabPointHandler` immediately before `experimentalFallback` in `ReceivedItemDispatch.TryApply`. Add `musicLabPointHandler(itemName)` to the handled OR-chain and update every direct test call with an explicit fake handler.

- [ ] **Step 5: Wire login and full-history rebuild**

After successful login and before `_connected = true`, call:

```csharp
MusicLabPointRandomization.ApplySlotData(
    loginSuccess.SlotData,
    Plugin.GameName,
    session.RoomState.Seed,
    _slot,
    generation);
MusicLabPointRandomization.SynchronizeHistory(
    generation,
    session.Items.AllItemsReceived.Select(
        (item, index) => new MusicLabPointReceipt(index, item.Item)));
```

After each `ItemReceived` callback drains `helper`, rebuild from its complete `AllItemsReceived` list. Do not increment from `ItemName` or reuse `_processedReceivedItemIndexes` as the point source of truth.

- [ ] **Step 6: Wire disconnect, replacement, and shutdown**

Call `OnDisconnected(generation)` from the exact session-ending lifecycle that already clears `_connected`. Retain only a synchronized matching identity. Call `Reset()` on deliberate plugin shutdown and when a different AP identity is accepted. Preserve normal pending-check queue behavior.

- [ ] **Step 7: Run focused and regression tests**

```powershell
dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release
dotnet run --project .\client\tests\ReconnectPolicy\ReconnectPolicy.Tests.csproj -c Release
dotnet run --project .\client\tests\CassetteRandomization\CassetteRandomization.Tests.csproj -c Release
dotnet run --project .\client\tests\GarageCartridgePersistence\GarageCartridgePersistence.Tests.csproj -c Release
```

Expected: all four projects PASS; cassettes, cartridges, and reconnect behavior remain unchanged.

- [ ] **Step 8: Commit network integration**

```powershell
git add client/MusicLabPointRandomization.cs client/ReceivedItemDispatch.cs client/Plugin.cs client/tests/MusicLabPoints client/tests/ReconnectPolicy client/tests/CassetteRandomization client/tests/GarageCartridgePersistence
git commit -m "feat(client): synchronize Music Lab Points from AP history"
```

---

### Task 6: Prove the Native Score Boundary with a Diagnostic-Only Build

**Files:**
- Modify: `client/Plugin.cs`
- Modify: `client/tests/MusicLabPoints/Program.cs`
- Create: `docs/testing/2026-09-09-music-lab-points-acceptance.md`

**Interfaces:**
- Consumes: the existing `CurrentPlayerSaveEnquiries.GetMedalScore()` patch and native `Hub06MedalScoreRewardChest` metadata.
- Produces: bounded, read-only correlation evidence for the display and all nine chest instances. It does not yet return AP totals.

- [ ] **Step 1: Write failing diagnostic-safety tests**

Source-level tests require a default-false `Developer.EnableMusicLabPointBoundaryDiagnostics` config key, bounded deduplication, exact owner/method validation, and no diagnostic assignment to `__result`, native fields, save state, or chest state.

- [ ] **Step 2: Add bounded read-only correlation diagnostics**

When the developer key is true, install diagnostic-only prefix/postfix hooks around the exact zero-argument state builder on `Hub06MedalScoreRewardChest`. Use a thread-static correlation scope so `GetMedalScorePostfix` can log which verified chest instance requested the score. Reuse the already proven chest metadata/path reader to emit threshold and native object path. Cap unique records at 64 and deduplicate by `(room, threshold, path, caller)`.

Outside a chest correlation scope, log one Hub6 display-reader observation. If the exact type/method/threshold cannot be read, emit an unavailable marker and do not install a guessed hook.

- [ ] **Step 3: Run tests and build without deployment**

```powershell
dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release
powershell -ExecutionPolicy Bypass -File .\client\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus" -SkipInstall
```

Expected: tests and build PASS; no game/plugin directory changes occur.

- [ ] **Step 4: Commit the diagnostic checkpoint**

```powershell
git add client/Plugin.cs client/tests/MusicLabPoints docs/testing/2026-09-09-music-lab-points-acceptance.md
git commit -m "diag(client): trace Music Lab Point score consumers"
```

- [ ] **Step 5: Stop for explicit deployment approval, then collect live evidence**

After approval, verify the game is closed, install only the client build to the authorized plugin directory, launch normally, confirm BepInEx and the intended client hash/version, enter Music Lab, and trigger one F5 diagnostic scan. Record evidence that:

- all nine paths/thresholds `5/10/20/32/46/64/89/111/140` correlate to the exact getter;
- the Music Lab display reads the same effective getter;
- native score before/after the read-only scan is identical; and
- the log contains no diagnostic exception, native write, forced chest, or unrelated result mutation.

Update the acceptance record with the focused log excerpt and hashes. If any item fails, stop before Task 7 and revise the design from the evidence.

---

### Task 7: Enable Production Effective-Score Replacement

**Files:**
- Modify: `client/MusicLabPointRandomization.cs`
- Modify: `client/Plugin.cs`
- Modify: `client/tests/MusicLabPoints/Program.cs`

**Interfaces:**
- Consumes: accepted Task 6 boundary evidence and Task 5 synchronized snapshots.
- Produces: production AP score replacement with native/legacy fallback and developer-cheat precedence enforced by `MusicLabPointRuntime.ResolveEffectiveScore`.

- [ ] **Step 1: Write failing postfix-precedence and no-write tests**

Cover these exact cases through the production adapter: native 37 remains 37 on v0.22; developer override 64 applies only in native mode; compatible awaiting returns 0; compatible synchronized returns AP total; retained disconnect returns retained total; malformed v0.23 returns 0; and a compatible AP session ignores developer override 140.

Add source checks proving the AP production path calls no native field setter, save request, chest method, or reflection write.

- [ ] **Step 2: Run the client test and verify RED**

```powershell
dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release
```

Expected: FAIL because the score postfix still applies only the developer override.

- [ ] **Step 3: Route the exact postfix through the production adapter**

Keep the original native result, record it for diagnostics, derive the optional developer value only when `DeveloperHarness.Enabled`, and assign exactly once:

```csharp
int nativeScore = __result;
MusicLabPointOverride.RecordNativeScore(nativeScore);
int? developerScore = !SuppressOverride && DeveloperHarness.Enabled
    ? MusicLabPointOverride.OverrideScore
    : null;
__result = MusicLabPointRandomization.ResolveEffectiveScore(nativeScore, developerScore);
```

The adapter must return native behavior in legacy/non-AP mode and must never call the patched getter recursively.

- [ ] **Step 4: Add bounded production state logs**

Log contract accepted/rejected, awaiting history, synchronized instance count/total, retained disconnect, reconnect rebuild/correction, cap anomaly, unknown point ID, and getter unavailable. Emit each transition once per AP identity; do not log every getter call or full received history.

- [ ] **Step 5: Run focused and full client tests**

```powershell
dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release
$projects = Get-ChildItem .\client\tests -Recurse -Filter '*.Tests.csproj' | Sort-Object FullName
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
```

Expected: all client projects PASS with no regression.

- [ ] **Step 6: Commit production score behavior**

```powershell
git add client/MusicLabPointRandomization.cs client/Plugin.cs client/tests/MusicLabPoints
git commit -m "feat(client): drive Music Lab chests from AP points"
```

---

### Task 8: Advance Versions, Independent Validation, and Living Documentation

**Files:**
- Modify: `client/Plugin.cs`
- Modify: `client/build.ps1`
- Modify: `apworld/scrc/archipelago.json`
- Modify: `tools/validate-repo.py`
- Modify: `apworld/tests/test_repository_contract.py`
- Modify: `README.md`
- Modify: `apworld/README.md`
- Modify: `apworld/scrc/docs/setup_en.md`
- Modify: `apworld/scrc/docs/en_Super Crazy Rhythm Castle.md`
- Modify: `docs/PROJECT_OVERVIEW.md`
- Modify: `docs/PROGRESSION.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/INSTALL.md`
- Modify: `docs/TESTING.md`
- Modify: `docs/TESTING_AND_ISSUES.md`
- Modify: `docs/NEXT_RELEASE_BUG_FIXES.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/testing/2026-09-09-music-lab-points-acceptance.md`

**Interfaces:**
- Consumes: implemented behavior and automated evidence from Tasks 1–7.
- Produces: exact Client `0.69.0` / APWorld `0.23.0` candidate metadata and truthful public/testing documentation.

- [ ] **Step 1: Make independent repository validation expect the new contract**

Change the independent expected client version from `0.68.0` to `0.69.0`. Preserve Task 3's APWorld `0.23.0`, exact implementation suffix, schema 14, IDs `153..155`, next item frontier 156, counts/values/totals, thresholds, unchanged location frontier, and unchanged active location counts. Keep all point-contract fixture-mutation tests passing.

- [ ] **Step 2: Run validation and verify RED against stale client/docs**

```powershell
py .\tools\validate-repo.py
py -m unittest discover -s .\apworld\tests -p 'test_repository_contract.py' -v
```

Expected: FAIL on stale Client `0.68.0`, stale v0.22 documentation, or missing independent checks.

- [ ] **Step 3: Advance exact candidate versions**

Set `PluginVersion = "0.69.0"`, update both `client/build.ps1` version messages, confirm APWorld metadata is `0.23.0`, and use the complete implementation tag ending `full-cassettes-0.22-music-lab-points-0.23`.

- [ ] **Step 4: Update living and packaged documentation truthfully**

Document the 10/3/7 distribution, 180 total, 140 final threshold with 40 slack, fresh v0.23 seed/save requirement, strict compatibility, native v0.22 fallback, zero-before-sync behavior, retained disconnect total, no native medal contribution, no point milestones, existing chest reuse, and diagnostic-first native boundary.

Describe the feature as an **experimental v0.23 candidate requiring manual acceptance**, not a completed release. Preserve the development disclaimer and every unrelated known issue/manual-testing note.

- [ ] **Step 5: Complete the acceptance ledger template**

The ledger must have fields for commit, client DLL hash, APWorld hash, seed/archive, server/slot, save slot, game/BepInEx versions, diagnostic result, each threshold below/at result, chest check IDs, disconnect/reconnect, relaunch, v0.22 regression, malformed-contract regression, native medal invariance, errors, and tester approval.

- [ ] **Step 6: Run repository contract and validation GREEN**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_repository_contract.py' -v
py .\tools\validate-repo.py
```

Expected: PASS and report Client `0.69.0`, APWorld `0.23.0`, next item ID `187256156`, and next location ID `187256211`.

- [ ] **Step 7: Commit candidate versions and docs**

```powershell
git add client/Plugin.cs client/build.ps1 apworld/scrc/archipelago.json tools/validate-repo.py apworld/tests/test_repository_contract.py README.md apworld/README.md apworld/scrc/docs docs/PROJECT_OVERVIEW.md docs/PROGRESSION.md docs/ROADMAP.md docs/INSTALL.md docs/TESTING.md docs/TESTING_AND_ISSUES.md docs/NEXT_RELEASE_BUG_FIXES.md CHANGELOG.md docs/testing/2026-09-09-music-lab-points-acceptance.md
git commit -m "docs: prepare Music Lab Points test candidate"
```

---

### Task 9: Run Full Verification and Build Non-Deployed Artifacts

**Files:**
- Verify only; do not intentionally modify tracked source.

**Interfaces:**
- Consumes: complete candidate branch.
- Produces: automated test counts, clean validation, package contents/hash, client DLL hash, and a reviewable branch diff.

- [ ] **Step 1: Run the complete APWorld suite**

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
```

Expected: every test PASS, including the eight-option 100-seed matrix.

- [ ] **Step 2: Run every client regression project**

```powershell
$projects = Get-ChildItem .\client\tests -Recurse -Filter '*.Tests.csproj' | Sort-Object FullName
$passed = 0
foreach ($project in $projects) {
    dotnet run --project $project.FullName -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $passed++
}
Write-Output "CLIENT_TEST_PROJECTS_PASSED=$passed"
```

Expected: all projects, including `MusicLabPoints`, PASS.

- [ ] **Step 3: Validate and package the APWorld**

```powershell
py .\tools\validate-repo.py
powershell -ExecutionPolicy Bypass -File .\tools\build-apworld.ps1
Get-FileHash .\dist\scrc.apworld -Algorithm SHA256
```

Inspect the archive as a ZIP and confirm it contains only distribution source/metadata/docs—no tests, bytecode, logs, saves, credentials, game assemblies, client DLL, or local task files.

- [ ] **Step 4: Build the release client without installing it**

```powershell
powershell -ExecutionPolicy Bypass -File .\client\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus" -SkipInstall
Get-FileHash .\client\bin\Release\net6.0\RhythmCastleAP.dll -Algorithm SHA256
```

Expected: build succeeds; the authorized game plugin directory remains untouched.

- [ ] **Step 5: Review scope and request code review**

Run:

```powershell
git diff --check main...HEAD
git status --short
git log --oneline main..HEAD
```

Review every diff against the spec. Confirm no local AI bridge, unrelated bug fix, generated artifact, `WordFactori/` change, automatic merge/push, or release claim entered the branch. Use `superpowers:requesting-code-review`; resolve findings through `superpowers:receiving-code-review` and rerun affected tests.

---

### Task 10: Deploy Only with Approval and Perform Fresh v0.23 Gameplay Acceptance

**Files:**
- Modify after observed testing only: `docs/testing/2026-09-09-music-lab-points-acceptance.md`
- Modify if status changes are supported by evidence: `docs/PROJECT_OVERVIEW.md`, `docs/ROADMAP.md`, `docs/TESTING_AND_ISSUES.md`, `CHANGELOG.md`

**Interfaces:**
- Consumes: reviewed hashes from Task 9 and explicit user approval.
- Produces: live gameplay evidence; it does not itself authorize merge, push, or release.

- [ ] **Step 1: Stop and obtain explicit deployment approval**

State the exact APWorld/client hashes and targets. Confirm the game and Archipelago processes are closed before replacing files. Do not reuse a previous approval for a different hash.

- [ ] **Step 2: Install only the approved files**

Install `dist/scrc.apworld` through Archipelago's supported APWorld flow and the client only through:

```powershell
powershell -ExecutionPolicy Bypass -File .\client\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus"
```

Verify the installed client DLL hash matches Task 9 and the plugin target resolves inside `BepInEx\plugins\RhythmCastleAP`.

- [ ] **Step 3: Generate and host a fresh v0.23 seed**

Use player `Jack`, a fresh generated archive, and a newly erased/fresh save chosen by the user. Record the seed/archive hash, server port, slot, APWorld hash, save slot, and start time. Confirm the first client log line is v0.69.0, the full v0.23 contract is accepted, BepInEx is running, and no older room is connected.

- [ ] **Step 4: Verify initial synchronization and weighted receipts**

Confirm zero before history readiness, zero after a no-point initial history, then use the Archipelago server `/send Jack <item name>` command to deliver controlled 1/10/20-point items. Record exact before/after display totals and point logs. Repeated test-only sends are allowed; totals above 180 must saturate and log once.

- [ ] **Step 5: Test every threshold immediately below and exactly at it**

Use server-sent point items to reach `4/5`, `9/10`, `19/20`, `31/32`, `45/46`, `63/64`, `88/89`, `110/111`, and `139/140`. At each pair, verify the matching chest is unavailable below and interactable exactly at the threshold. Do not force-open a chest through code.

- [ ] **Step 6: Verify exactly-once chest/source behavior**

Open representative 5-, 32-, 46-, and 140-point chests. Confirm each existing location sends once; the Quicksand/Wiggle cassettes and Bloody Tears cartridge remain the rewards routed through those existing checks; re-entry/reconciliation sends no duplicate.

- [ ] **Step 7: Verify native medals do not affect AP points**

Complete one reachable Music Lab cassette medal and one Game Garage medal. Confirm native result/medal persistence works and the AP point display/total does not change without a received point item.

- [ ] **Step 8: Verify disconnect, reconnect, and relaunch**

After synchronization, stop the server temporarily. Confirm the last total remains visible and an opened chest queues. Restart/reconnect and verify the full history rebuild returns the same total and queued location sends once. Close normally, relaunch, select the same save slot manually, and confirm the authoritative total rebuilds again.

- [ ] **Step 9: Verify legacy and malformed compatibility**

Connect once to a retained v0.22 seed with its native Music Lab score and confirm native behavior remains unchanged. Separately generate a temporary malformed v0.23 fixture from an isolated APWorld copy with one point-contract value altered; confirm the client reports the exact mismatch and holds effective total at zero. Restore the approved v0.23 APWorld afterward and do not commit the malformed package.

- [ ] **Step 10: Record evidence and commit only truthful acceptance updates**

Fill every ledger field with actual observations, hashes, and focused log excerpts. Mark only passed rows. If the full matrix passes, update status from manually unverified to live-accepted; otherwise retain the blocker and describe the exact failure.

```powershell
git add docs/testing/2026-09-09-music-lab-points-acceptance.md docs/PROJECT_OVERVIEW.md docs/ROADMAP.md docs/TESTING_AND_ISSUES.md CHANGELOG.md
git commit -m "docs: record Music Lab Points gameplay acceptance"
```

- [ ] **Step 11: Stop before integration**

Run final verification again and present the branch, commits, hashes, automated counts, gameplay results, and unresolved limitations. Use `superpowers:finishing-a-development-branch` only after every required test passes. Merging, pushing, packaging as a release, and publishing remain separate explicit user decisions.
