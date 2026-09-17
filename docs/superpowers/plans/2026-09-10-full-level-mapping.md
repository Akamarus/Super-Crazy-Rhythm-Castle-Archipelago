# Full Campaign Level Mapping Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add all 22 normal campaign levels as separate Completion and difficulty-filtered cumulative Star locations, retire synthetic Development Caches from new seeds, and add read-only diagnostics that collect the evidence required for later Bee/Devil production activation.

**Architecture:** APWorld and client each consume an immutable 22-level catalog with the same internal IDs and location names. APWorld owns permanent IDs, difficulty activation, regions, conservative placement rules, and a strict slot-data sub-contract; the client validates that contract and converts persisted default-variant results into only the active cumulative locations. Known Bee/Devil variants remain non-mutating diagnostics in this slice so their unverified source and lifecycle flags cannot be guessed.

**Tech Stack:** Python 3.13, Archipelago 0.6.7 world APIs, `unittest`, C#/.NET 6 client, .NET 8 pure-policy console tests, Archipelago.MultiClient.Net 6.7.1, Harmony/BepInEx IL2CPP, PowerShell build and packaging scripts.

**Spec:** `docs/superpowers/specs/2026-09-10-full-level-mapping-design.md`

## Global Constraints

- Target APWorld version is exactly `0.24.0`; target client version is exactly `0.70.0`.
- Append `full-level-mapping-0.24` to the complete existing implementation-version string; preserve every historical prefix.
- Advance top-level slot-data `schema_version` from `14` to `15`; use campaign-level-mapping sub-schema `1`.
- Preserve all existing IDs, including historical Development Cache offsets `+11..+20` and unused offsets `+4..+10`.
- Allocate exactly 81 new normal campaign location IDs from `187256211` through `187256291`; advance the safe location frontier to `187256292`.
- Allocate no new item IDs and no Bee/Devil or special-chain location IDs in this plan; preserve safe item frontier `187256156`.
- Instantiate exactly 44/66/88/88 normal campaign locations for Normal/Hard/Expert/Perfection.
- Remove the ten Development Caches from newly instantiated seeds while retaining their registered names and permanent IDs for compatibility.
- Expected total addressed locations are exactly `121/179/237/273` for Normal/Hard/Expert/Perfection.
- Keep all 66 AP Stars, generated Star gates, client Star enforcement, special-mode checks, and final Victory inactive.
- Unknown normal prerequisites remain conservative: they may be exposed as locations but must reject required progression placement until their route is verified.
- Known Bee and Devil variants must never emit default campaign or Star checks.
- Special-mode diagnostics are read-only: no native flag writes, reward suppression, item grants, AP checks, or permanent IDs.
- Build without deployment first. Deploy only after explicit approval and only to `D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP`.
- Do not merge, push, publish, remove worktrees, or alter the unrelated `WordFactori/` directory without explicit approval.
- Gameplay testing is the final acceptance criterion; unplayed mappings remain labeled manual-testing pending.

## File and Responsibility Map

- `apworld/scrc/campaign_levels.py` — immutable 22-level catalog, normal location names, legacy IDs, new ID allocation, area ownership, and verified placement status.
- `apworld/scrc/difficulty.py` — campaign tier activation for Completion, 1 Star, 2 Stars, and 3 Stars.
- `apworld/scrc/__init__.py` — location registration, region construction, conservative rules, active totals, item-pool capacity, and slot-data contract.
- `apworld/tests/test_campaign_levels.py` — catalog identity, ID frontier, ordering, uniqueness, and difficulty tests.
- `apworld/tests/test_world_integration.py` — instantiated totals, regions, placement safety, pool balance, slot data, and seed matrices.
- `client/CampaignLevelCatalog.cs` — client-side immutable internal-level catalog and canonical location-name construction.
- `client/CampaignLocationContract.cs` — strict v0.24 slot-data validation and active-location set.
- `client/LevelCompletionPolicy.cs` — pure default-variant result evaluation intersected with active seed locations.
- `client/SpecialVariantDiagnosticPolicy.cs` — known Bee/Devil identities and bounded diagnostic-token classification.
- `client/Plugin.cs` — slot-data wiring, persisted-result routing, retry-safe deduplication, and read-only diagnostic capture.
- `client/tests/LevelCompletion/` — catalog, contract, cumulative result, compatibility, replay, and production-wiring tests.
- `client/tests/SpecialVariantDiagnostic/` — pure variant/token classification and read-only wiring tests.
- `docs/testing/2026-09-10-full-level-mapping-acceptance.md` — exact build/seed/save identity, targeted runtime evidence, and remaining manual map.
- `docs/IDS.md`, `docs/PROJECT_OVERVIEW.md`, `docs/PROGRESSION.md`, `docs/TESTING.md`, `README.md`, `CHANGELOG.md`, embedded APWorld docs, version files, and `tools/validate-repo.py` — permanent registry, public behavior, counts, candidate status, and independent validation.

---

### Task 1: Add the Immutable APWorld Campaign Catalog and Permanent IDs

**Files:**
- Create: `apworld/scrc/campaign_levels.py`
- Create: `apworld/tests/test_campaign_levels.py`
- Modify: `apworld/scrc/__init__.py`
- Modify: `docs/IDS.md`

**Interfaces:**
- Consumes: base ID `187256000`, existing campaign IDs at offsets `+1..+3` and `+182..+185`, and safe location frontier `+211`.
- Produces: `CampaignLevel`, `CAMPAIGN_LEVELS`, `CAMPAIGN_LEVELS_BY_INTERNAL_ID`, `CAMPAIGN_LOCATION_NAMES`, `CAMPAIGN_LOCATION_NAME_TO_ID`, `NEW_CAMPAIGN_LOCATION_NAME_TO_ID`, `campaign_location_names()`, and `validate_campaign_catalog()`.

- [ ] **Step 1: Write catalog tests that lock all 22 native identities**

Create `apworld/tests/test_campaign_levels.py` using the existing `load_scrc_module()` test helper. Assert this exact identity table:

```python
expected = (
    (1, "Light Humor", "Level_05", "Roots"),
    (2, "Pop Party", "Level_06", "Roots"),
    (3, "The Megafying Ritual", "Level_07", "Roots"),
    (4, "DJ Eggplant", "Level_08", "Roots"),
    (5, "Lift Quest", "Level_09", "Roots"),
    (6, "Boring Room", "Level_02", "Lobby"),
    (7, "Demolition Training", "Level_19", "Lobby"),
    (8, "Minim Tower", "Level_11", "Lobby"),
    (9, "School Trip", "Level_20", "Lobby"),
    (10, "The Vault", "Level_01", "Lobby"),
    (11, "Act 1: Flavor", "Level_12", "Meat Dimension"),
    (12, "Act 2: Sauce and Spice", "Level_15", "Meat Dimension"),
    (13, "Act 3: Montage", "Level_22", "Meat Dimension"),
    (14, "Act 4: Habanero", "Level_23", "Meat Dimension"),
    (15, "Central Mainframe", "Level_16", "Cell Tower"),
    (16, "The Thief Prince", "Level_24", "Cell Tower"),
    (17, "Cold Storage", "Level_21", "Lobby"),
    (18, "The Darkness", "Level_03", "Tower of Fear"),
    (19, "Escape", "Level_13", "Tower of Fear"),
    (20, "Loneliness", "Level_25", "Tower of Fear"),
    (21, "Locker Room", "Level_14", "Royal Corridor"),
    (22, "King Ferdinand I", "Level_28", "Royal Corridor"),
)
```

Assert 22 unique numbers, 22 unique internal IDs, 88 unique location names, and the exact suffix order `Completion`, `1 Star`, `2 Stars`, `3 Stars` for every level.

- [ ] **Step 2: Write permanent-ID tests**

Lock the existing mappings and new block:

```python
legacy = {
    "Level 1 - Completion": 187256001,
    "Level 2 - Completion": 187256002,
    "Level 3 - Completion": 187256003,
    "Level 22 - Completion": 187256182,
    "Level 22 - 1 Star": 187256183,
    "Level 22 - 2 Stars": 187256184,
    "Level 22 - 3 Stars": 187256185,
}
self.assertEqual(
    {name: campaign.CAMPAIGN_LOCATION_NAME_TO_ID[name] for name in legacy},
    legacy,
)
self.assertEqual(len(campaign.NEW_CAMPAIGN_LOCATION_NAME_TO_ID), 81)
self.assertEqual(min(campaign.NEW_CAMPAIGN_LOCATION_NAME_TO_ID.values()), 187256211)
self.assertEqual(max(campaign.NEW_CAMPAIGN_LOCATION_NAME_TO_ID.values()), 187256291)
self.assertEqual(len(set(campaign.CAMPAIGN_LOCATION_NAME_TO_ID.values())), 88)
```

Add fixture tests proving duplicate level numbers, internal IDs, location names, IDs, a missing tier, or a reordered/non-contiguous new block raises `ValueError` naming the invalid field.

- [ ] **Step 3: Run the new tests and verify RED**

Run:

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_campaign_levels.py' -v
```

Expected: FAIL because `campaign_levels.py` does not exist.

- [ ] **Step 4: Implement the immutable catalog**

Create `campaign_levels.py` with this public shape:

```python
from dataclasses import dataclass

BASE_ID = 187256000
CAMPAIGN_LOCATION_TIERS = ("Completion", "1 Star", "2 Stars", "3 Stars")

@dataclass(frozen=True)
class CampaignLevel:
    number: int
    display_name: str
    internal_id: str
    area: str
    required_items: tuple[str, ...] = ()
    progression_safe: bool = False

    def location_name(self, tier: str) -> str:
        return f"Level {self.number} - {tier}"
```

Populate `CAMPAIGN_LEVELS` with the exact 22-row table from Step 1. Use this conservative contract:

```python
verified_requirements = {
    1: (),
    2: (),
    3: ("Weed Killer", "Plant Pipes"),
    4: ("Weed Killer", "Plant Pipes"),
    5: ("Weed Killer", "Plant Pipes", "Hip Glasses", "Chicken Bucket"),
    18: ("Plant Pipes",),
    19: (),
    20: ("Hypno Pan",),
    21: ("Plant Pipes",),
    22: (),
}
progression_safe_levels = frozenset((1, 2, 3, 4, 5, 22))
```

Levels 18–21 retain their known in-level requirements but remain unsafe for required progression until their area/story routes are fully modeled. All other unverified levels use an empty requirement tuple and `progression_safe=False`; the empty tuple means “no verified AP-item rule yet,” not “confirmed ability-free.” Keep region reachability and item-placement safety as separate fields.

Use explicit legacy mappings, then build the new block in stable catalog/tier order:

```python
LEGACY_CAMPAIGN_LOCATION_IDS = {
    "Level 1 - Completion": BASE_ID + 1,
    "Level 2 - Completion": BASE_ID + 2,
    "Level 3 - Completion": BASE_ID + 3,
    "Level 22 - Completion": BASE_ID + 182,
    "Level 22 - 1 Star": BASE_ID + 183,
    "Level 22 - 2 Stars": BASE_ID + 184,
    "Level 22 - 3 Stars": BASE_ID + 185,
}
NEW_CAMPAIGN_LOCATION_START = BASE_ID + 211
NEW_CAMPAIGN_LOCATION_NAMES = tuple(
    level.location_name(tier)
    for level in CAMPAIGN_LEVELS
    for tier in CAMPAIGN_LOCATION_TIERS
    if level.location_name(tier) not in LEGACY_CAMPAIGN_LOCATION_IDS
)
NEW_CAMPAIGN_LOCATION_NAME_TO_ID = {
    name: NEW_CAMPAIGN_LOCATION_START + index
    for index, name in enumerate(NEW_CAMPAIGN_LOCATION_NAMES)
}
CAMPAIGN_LOCATION_NAME_TO_ID = {
    **LEGACY_CAMPAIGN_LOCATION_IDS,
    **NEW_CAMPAIGN_LOCATION_NAME_TO_ID,
}
```

Call `validate_campaign_catalog()` at import.

- [ ] **Step 5: Replace hand-written campaign registration with catalog exports**

In `apworld/scrc/__init__.py`, import the catalog and update `LOCATION_NAME_TO_ID` with `CAMPAIGN_LOCATION_NAME_TO_ID`. Remove only the old inline Level 1–3 and Level 22 assignments; keep Development Cache registrations reserved.

- [ ] **Step 6: Record the exact permanent-ID block**

In `docs/IDS.md`, document the deterministic 81-location block at offsets `+211..+291`, list every generated name-to-ID mapping, and advance only the location frontier to `+292` / `187256292`.

- [ ] **Step 7: Run focused tests and commit**

Run:

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_campaign_levels.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_items.py' -v
```

Expected: all focused tests pass.

Commit:

```powershell
git add apworld/scrc/campaign_levels.py apworld/scrc/__init__.py apworld/tests/test_campaign_levels.py docs/IDS.md
git commit -m "feat: register full campaign level catalog"
```

---

### Task 2: Instantiate Difficulty-Filtered Campaign Locations and Retire Development Caches

**Files:**
- Modify: `apworld/scrc/difficulty.py`
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_campaign_levels.py`
- Modify: `apworld/tests/test_world_integration.py`

**Interfaces:**
- Consumes: `CAMPAIGN_LEVELS`, `CAMPAIGN_LOCATION_NAME_TO_ID`, each descriptor's area/requirements/safety, and existing `filler_or_safe_required()`.
- Produces: all active normal campaign locations in their physical regions and exact total counts `121/179/237/273`.

- [ ] **Step 1: Write failing difficulty and total-count tests**

Add exact campaign expectations:

```python
expected_campaign_counts = {0: 44, 1: 66, 2: 88, 3: 88}
expected_world_counts = {0: 121, 1: 179, 2: 237, 3: 273}
for difficulty_value, expected in expected_campaign_counts.items():
    active = difficulty.active_location_names(
        campaign.CAMPAIGN_LOCATION_NAMES,
        difficulty_value,
    )
    self.assertEqual(len(active), expected)
for difficulty_value, expected in expected_world_counts.items():
    world = build_world(difficulty=difficulty_value)
    self.assertEqual(addressed_location_count(world), expected)
```

Assert all Completion and 1-Star names exist on Normal, 2-Star names start on Hard, 3-Star names start on Expert, and campaign totals do not change between Expert and Perfection.

Assert no instantiated location begins with `Development Cache`, while `LOCATION_NAME_TO_ID["Development Cache 01"]` remains `187256011`.

- [ ] **Step 2: Run the focused tests and verify RED**

Run:

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_campaign_levels.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_world_integration.py' -v
```

Expected: FAIL because the world still instantiates only its partial campaign map and all ten caches.

- [ ] **Step 3: Make campaign tier activation explicit**

In `difficulty.py`, recognize only catalog-owned campaign names and apply:

```python
CAMPAIGN_TIERS_BY_DIFFICULTY = {
    0: frozenset(("Completion", "1 Star")),
    1: frozenset(("Completion", "1 Star", "2 Stars")),
    2: frozenset(("Completion", "1 Star", "2 Stars", "3 Stars")),
    3: frozenset(("Completion", "1 Star", "2 Stars", "3 Stars")),
}
```

Do not classify arbitrary names merely because they end in `- 2 Stars` or `- 3 Stars`; membership must come from `CAMPAIGN_LOCATION_NAMES`.

- [ ] **Step 4: Build campaign locations from descriptors**

Replace the separate Level 1–3 and Level 22 creation blocks in `create_regions()` with one catalog loop. For each active tier, create an `SCRCLocation` in `concrete_regions[level.area]`, install its verified `set_rule()` requirements, and use:

```python
location.item_rule = lambda item, safe=level.progression_safe: (
    filler_or_safe_required(item, safe)
)
```

Preserve the existing special Level 22 restriction: 2-Star and 3-Star locations stay filler-only until their performance dependencies are proven, even though the level descriptor permits Completion and 1 Star to hold required progression.

- [ ] **Step 5: Stop instantiating synthetic caches**

Delete the `for index in range(1, 11)` region-construction loop. Do not remove cache names or IDs from `LOCATION_NAME_TO_ID`.

- [ ] **Step 6: Add region and safety assertions**

Test every campaign location's parent region against the catalog. Assert known requirements:

```python
self.assert_requires("Level 3 - Completion", "Weed Killer", "Plant Pipes")
self.assert_requires("Level 18 - Completion", "Plant Pipes")
self.assert_requires("Level 20 - Completion", "Hypno Pan")
self.assert_requires("Level 21 - Completion", "Plant Pipes")
self.assert_allows_required_progression("Level 22 - 1 Star")
self.assert_rejects_required_progression("Level 22 - 2 Stars")
self.assert_rejects_required_progression("Level 6 - Completion")
```

- [ ] **Step 7: Run APWorld integration tests and commit**

Run:

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_campaign_levels.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_difficulty.py' -v
py -m unittest discover -s .\apworld\tests -p 'test_world_integration.py' -v
```

Expected: all focused tests pass with exact totals `121/179/237/273`.

Commit:

```powershell
git add apworld/scrc/difficulty.py apworld/scrc/__init__.py apworld/tests/test_campaign_levels.py apworld/tests/test_world_integration.py
git commit -m "feat: activate full normal campaign checks"
```

---

### Task 3: Publish a Strict v0.24 Campaign Slot-Data Contract

**Files:**
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `apworld/scrc/archipelago.json`
- Modify: `apworld/README.md`

**Interfaces:**
- Consumes: active campaign location names from Task 2.
- Produces: slot-data keys `campaign_level_mapping_schema`, `active_campaign_locations`, `active_campaign_location_tiers`, and `special_variant_locations_active`.

- [ ] **Step 1: Write failing slot-data contract tests**

For all four difficulties, assert:

```python
self.assertEqual(data["schema_version"], 15)
self.assertEqual(data["campaign_level_mapping_schema"], 1)
self.assertEqual(data["active_campaign_locations"], sorted(expected_active_names))
self.assertEqual(data["active_campaign_location_tiers"], expected_tiers)
self.assertFalse(data["special_variant_locations_active"])
self.assertEqual(data["special_variant_locations"], [])
self.assertFalse(data["star_items_active"])
self.assertFalse(data["client_star_gate_enforcement_active"])
self.assertEqual(data["development_cache_count"], 0)
self.assertTrue(data["development_cache_ids_reserved"])
```

Also assert `active_location_count` equals the number of instantiated addressed locations, not the size of the permanent registry.

- [ ] **Step 2: Run the slot-data tests and verify RED**

Run:

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_world_integration.py' -v
```

Expected: FAIL on missing campaign contract keys and old schema/count values.

- [ ] **Step 3: Emit only instantiated campaign names**

In `fill_slot_data()`, derive the active campaign set by intersecting the world's instantiated addressed locations with `CAMPAIGN_LOCATION_NAMES`. Emit it sorted for deterministic wire output. Use the exact keys and values from Step 1.

- [ ] **Step 4: Advance candidate metadata**

Set APWorld version `0.24.0`, top-level schema `15`, and append `full-level-mapping-0.24` to the implementation version. Update embedded APWorld documentation to say all 22 normal campaign checks are mapped, special variants are diagnostic-only, AP Stars remain inactive, and a new seed is mandatory.

- [ ] **Step 5: Run APWorld tests and commit**

Run:

```powershell
py -m unittest discover apworld/tests -v
```

Expected: all APWorld tests pass.

Commit:

```powershell
git add apworld/scrc/__init__.py apworld/tests/test_world_integration.py apworld/scrc/archipelago.json apworld/README.md
git commit -m "feat: publish campaign mapping slot contract"
```

---

### Task 4: Add the Client Campaign Catalog and Pure Result Policy

**Files:**
- Create: `client/CampaignLevelCatalog.cs`
- Create: `client/CampaignLocationContract.cs`
- Modify: `client/LevelCompletionPolicy.cs`
- Modify: `client/tests/LevelCompletion/LevelCompletion.Tests.csproj`
- Modify: `client/tests/LevelCompletion/Program.cs`

**Interfaces:**
- Consumes: v0.24 slot-data contract from Task 3.
- Produces: `CampaignLevelDescriptor`, `CampaignLevelCatalog.TryGet(string?, out CampaignLevelDescriptor)`, `CampaignLocationContract.Validate(Dictionary<string, object>?)`, `CampaignLocationCompatibilityResult`, and `LevelCompletionPolicy.LocationsForPersistedResult(string?, string?, int?, IReadOnlySet<string>)`.

- [ ] **Step 1: Expand pure-policy tests and verify RED**

Add all 22 descriptors to the expected client table. Test:

```csharp
SequenceEqual(
    new[] { "Level 6 - Completion", "Level 6 - 1 Star", "Level 6 - 2 Stars" },
    LevelCompletionPolicy.LocationsForPersistedResult(
        "Level_02", "LevelVariant_Default", 3,
        Set("Level 6 - Completion", "Level 6 - 1 Star", "Level 6 - 2 Stars")),
    "policy intersects result tiers with active seed locations");

SequenceEqual(Array.Empty<string>(),
    LevelCompletionPolicy.LocationsForPersistedResult(
        "Level_06", "LevelVariant_BeeMode", 3, AllCampaignLocations),
    "Bee variant never sends normal checks");
SequenceEqual(Array.Empty<string>(),
    LevelCompletionPolicy.LocationsForPersistedResult(
        "Level_02", "LevelVariant_DevilMode", 3, AllCampaignLocations),
    "Devil variant never sends normal checks");
SequenceEqual(Array.Empty<string>(),
    LevelCompletionPolicy.LocationsForPersistedResult(
        "Level_02", "LevelVariant_Unknown", 3, AllCampaignLocations),
    "unknown variant fails closed");
```

Add contract tests for exact schema `15`, sub-schema `1`, all list entries being known canonical names, no duplicates, `special_variant_locations_active=false`, legacy v0.23 classification, and malformed v0.24 fail-closed classification.

Run:

```powershell
dotnet run --project .\client\tests\LevelCompletion\LevelCompletion.Tests.csproj -c Release
```

Expected: FAIL because the new catalog, contract, and method signature do not exist.

- [ ] **Step 2: Implement the exact client catalog**

Create `CampaignLevelCatalog.cs`:

```csharp
internal sealed record CampaignLevelDescriptor(
    int Number,
    string DisplayName,
    string InternalId,
    string Area)
{
    internal string Location(string tier) => $"Level {Number} - {tier}";
}

internal static class CampaignLevelCatalog
{
    internal static readonly IReadOnlyList<CampaignLevelDescriptor> All =
        new[]
        {
            new(1, "Light Humor", "Level_05", "Roots"),
            new(2, "Pop Party", "Level_06", "Roots"),
            new(3, "The Megafying Ritual", "Level_07", "Roots"),
            new(4, "DJ Eggplant", "Level_08", "Roots"),
            new(5, "Lift Quest", "Level_09", "Roots"),
            new(6, "Boring Room", "Level_02", "Lobby"),
            new(7, "Demolition Training", "Level_19", "Lobby"),
            new(8, "Minim Tower", "Level_11", "Lobby"),
            new(9, "School Trip", "Level_20", "Lobby"),
            new(10, "The Vault", "Level_01", "Lobby"),
            new(11, "Act 1: Flavor", "Level_12", "Meat Dimension"),
            new(12, "Act 2: Sauce and Spice", "Level_15", "Meat Dimension"),
            new(13, "Act 3: Montage", "Level_22", "Meat Dimension"),
            new(14, "Act 4: Habanero", "Level_23", "Meat Dimension"),
            new(15, "Central Mainframe", "Level_16", "Cell Tower"),
            new(16, "The Thief Prince", "Level_24", "Cell Tower"),
            new(17, "Cold Storage", "Level_21", "Lobby"),
            new(18, "The Darkness", "Level_03", "Tower of Fear"),
            new(19, "Escape", "Level_13", "Tower of Fear"),
            new(20, "Loneliness", "Level_25", "Tower of Fear"),
            new(21, "Locker Room", "Level_14", "Royal Corridor"),
            new(22, "King Ferdinand I", "Level_28", "Royal Corridor"),
        };
}
```

Build a case-insensitive dictionary and throw during static initialization on duplicate internal IDs or numbers.

- [ ] **Step 3: Implement strict slot-data validation**

Create `CampaignLocationContract.cs` with:

```csharp
internal enum CampaignLocationCompatibilityMode
{
    Legacy,
    Compatible,
    IncompatibleClaim,
}

internal sealed record CampaignLocationCompatibilityResult(
    CampaignLocationCompatibilityMode Mode,
    IReadOnlySet<string> ActiveLocations,
    string Detail);
```

Accept only implementation strings ending in `full-level-mapping-0.24`, top-level schema `15`, mapping schema `1`, a unique known-name list, and `special_variant_locations_active=false`. Treat recognized older implementations as `Legacy`; treat a malformed v0.24 claim as `IncompatibleClaim` with the failing field in `Detail`.

- [ ] **Step 4: Generalize cumulative default-variant evaluation**

Replace the Level-22-only policy with:

```csharp
internal static IReadOnlyList<string> LocationsForPersistedResult(
    string? internalLevel,
    string? variant,
    int? starsEarned,
    IReadOnlySet<string> activeLocations)
{
    if (!string.Equals(variant, "LevelVariant_Default", StringComparison.OrdinalIgnoreCase) ||
        starsEarned is null or < 1 ||
        !CampaignLevelCatalog.TryGet(internalLevel, out var level))
        return Array.Empty<string>();

    var candidates = new List<string> { level.Location("Completion") };
    for (int stars = 1; stars <= Math.Clamp(starsEarned.Value, 1, 3); stars++)
        candidates.Add(level.Location(stars == 1 ? "1 Star" : $"{stars} Stars"));
    return candidates.Where(activeLocations.Contains).ToArray();
}
```

Victory must never be returned by this method.

- [ ] **Step 5: Run pure client tests and commit**

Run:

```powershell
dotnet run --project .\client\tests\LevelCompletion\LevelCompletion.Tests.csproj -c Release
```

Expected: `Level completion policy tests passed.`

Commit:

```powershell
git add client/CampaignLevelCatalog.cs client/CampaignLocationContract.cs client/LevelCompletionPolicy.cs client/tests/LevelCompletion
git commit -m "feat: generalize campaign result policy"
```

---

### Task 5: Wire the Strict Campaign Contract into Persisted Results

**Files:**
- Modify: `client/Plugin.cs`
- Modify: `client/tests/LevelCompletion/Program.cs`
- Modify: `client/tests/ReconnectPolicy/Program.cs`

**Interfaces:**
- Consumes: `CampaignLocationContract.Validate()` and `LevelCompletionPolicy.LocationsForPersistedResult()` from Task 4.
- Produces: `CampaignLevelRandomization.ApplySlotData()`, `CampaignLevelRandomization.EvaluatePersistedResult()`, and production queue wiring.

- [ ] **Step 1: Write production-wiring and replay tests**

Add source-wiring assertions that login calls `CampaignLevelRandomization.ApplySlotData(loginSuccess.SlotData)` before the client reports connected. Add pure state tests for:

- v0.24 compatible state enabling the exact active set;
- legacy state preserving the existing Level 1–3 and Level 22 behavior;
- malformed v0.24 returning no new locations;
- disconnect retaining the last valid active set for the same connection generation;
- identity replacement clearing the set;
- repeated one-Star then three-Star results returning the new tiers while queue deduplication suppresses already checked names.

Run:

```powershell
dotnet run --project .\client\tests\LevelCompletion\LevelCompletion.Tests.csproj -c Release
dotnet run --project .\client\tests\ReconnectPolicy\ReconnectPolicy.Tests.csproj -c Release
```

Expected: FAIL because runtime wiring is absent and the existing `PersistedThisSession` guard drops improved results.

- [ ] **Step 2: Add campaign session state**

Implement a lock-protected `CampaignLevelRandomization` adapter in `Plugin.cs` or a focused new file if it exceeds 200 lines. `ApplySlotData()` stores `Compatible`, `Legacy`, or `IncompatibleClaim` plus an immutable active-name set. `Shutdown()` and AP identity replacement clear it. Temporary disconnect retains compatible state for the same session generation.

- [ ] **Step 3: Route every compatible default result through the policy**

In `GamePatches.ResultPersistedEventPostfix`, call the adapter for all campaign levels, then queue each returned location through `Plugin.AP?.QueueLocation(location)`. Remove the special Level 22 branch and the partial `LocationMap.InternalToLocationName` campaign branch once their legacy behavior is represented by the adapter.

- [ ] **Step 4: Permit legitimate improved results**

Do not return early solely because the same `level|variant` persisted twice. Let the canonical location set and `ArchipelagoClient._queuedOrSent` provide location-level idempotency. Retain bounded informational logging for repeated persisted events. This change is required so a later 2-Star or 3-Star result can send its newly earned location.

- [ ] **Step 5: Preserve Level 22/Victory separation**

Assert the ordinary result path never queues `Victory` and does not consult a previous Level 22 clear when a Star item later arrives. Existing development Area Access victory behavior remains unchanged in this slice; production Star victory stays inactive.

- [ ] **Step 6: Run focused and reconnect tests and commit**

Run:

```powershell
dotnet run --project .\client\tests\LevelCompletion\LevelCompletion.Tests.csproj -c Release
dotnet run --project .\client\tests\ReconnectPolicy\ReconnectPolicy.Tests.csproj -c Release
```

Expected: both projects pass.

Commit:

```powershell
git add client/Plugin.cs client/tests/LevelCompletion/Program.cs client/tests/ReconnectPolicy/Program.cs
git commit -m "feat: send mapped campaign result checks"
```

---

### Task 6: Add Read-Only Bee and Devil Native Diagnostics

**Files:**
- Create: `client/SpecialVariantDiagnosticPolicy.cs`
- Create: `client/tests/SpecialVariantDiagnostic/SpecialVariantDiagnostic.Tests.csproj`
- Create: `client/tests/SpecialVariantDiagnostic/Program.cs`
- Modify: `client/Plugin.cs`

**Interfaces:**
- Consumes: result level/variant, `GameProgressionFlagUpdatedEvent`, current room, hierarchy paths, and managed component names.
- Produces: `SpecialVariantKind`, `SpecialVariantDiagnosticPolicy.ClassifyVariant()`, `IsRelevantProgressionFlag()`, `IsRelevantSceneObject()`, and bounded `SpecialModeDiscovery` snapshots.

- [ ] **Step 1: Write pure diagnostic-classification tests**

Lock exact variant pairs:

```csharp
Equal(SpecialVariantKind.BeeNectarParty,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_06", "LevelVariant_BeeMode"));
Equal(SpecialVariantKind.BeeAct1BNectar,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_12", "LevelVariant_BeeMode"));
Equal(SpecialVariantKind.DevilDemonicRoom,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_02", "LevelVariant_DevilMode"));
Equal(SpecialVariantKind.DevilDemonicTower,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_11", "LevelVariant_DevilMode"));
Equal(SpecialVariantKind.DevilDemonicEscape,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_13", "LevelVariant_DevilMode"));
Equal(SpecialVariantKind.DevilDemonicLockers,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_14", "LevelVariant_DevilMode"));
Equal(SpecialVariantKind.None,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_06", "LevelVariant_Default"));
```

Test case-insensitive tokens `BIZZLE`, `CLIVE`, `BEE`, `NECTAR`, `DEMON`, `DEVIL`, `SEAL`, and `BUNKER` as relevant. Exclude unrelated `STAR`, `SCORE`, `CASSETTE`, and generic `KEY` names unless `DEMON` or `BUNKER` is also present.

- [ ] **Step 2: Run the diagnostic project and verify RED**

Run:

```powershell
dotnet run --project .\client\tests\SpecialVariantDiagnostic\SpecialVariantDiagnostic.Tests.csproj -c Release
```

Expected: FAIL because the project and policy do not exist.

- [ ] **Step 3: Implement the pure policy**

Define:

```csharp
internal enum SpecialVariantKind
{
    None,
    BeeNectarParty,
    BeeAct1BNectar,
    DevilDemonicRoom,
    DevilDemonicTower,
    DevilDemonicEscape,
    DevilDemonicLockers,
}
```

Use an exact `(internalLevel, variant)` dictionary for classification. Use the bounded token rules from Step 1 only for diagnostic flag/object selection, never to infer a gameplay identity.

- [ ] **Step 4: Capture bounded read-only evidence**

Add `SpecialModeDiscovery` with these behaviors:

- On a relevant `GameProgressionFlagUpdatedEvent`, log the exact flag once per room/session.
- On relevant result application and persistence, log exact internal level, exact variant, score, difficulty, and classified special identity.
- On plain F5 outside Hub6/Game Garage, scan at most 100 active scene objects whose paths or component names match the bounded tokens and log path, active state, and component types.
- Emit a begin/end line containing `SPECIAL MODE DIAGNOSTIC`, `readOnly=True`, room, emitted count, and truncated count.
- Never call `TrySubmitProgressionFlag`, `QueueLocation`, item dispatch, reward suppression, or any Unity mutation from diagnostic code.

- [ ] **Step 5: Add source-level read-only regression assertions**

In `Program.cs`, read `Plugin.cs` and extract the `SpecialModeDiscovery` source region. Assert it contains result/flag logging and contains none of:

```text
TrySubmitProgressionFlag
QueueLocation
SetActive(
ReceivedItemDispatch
AllowNative
```

Assert `ProgressionPatches.ProgressionFlagEventPostfix` forwards the exact event and flag, and `GamePatches` forwards both result lifecycle points.

- [ ] **Step 6: Run diagnostic tests and the undeployed client build, then commit**

Run:

```powershell
dotnet run --project .\client\tests\SpecialVariantDiagnostic\SpecialVariantDiagnostic.Tests.csproj -c Release
.\client\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus" -SkipInstall
```

Expected: diagnostic tests pass and client build succeeds without installation.

Commit:

```powershell
git add client/SpecialVariantDiagnosticPolicy.cs client/tests/SpecialVariantDiagnostic client/Plugin.cs
git commit -m "feat: add special mode native diagnostics"
```

---

### Task 7: Update Versions, Living Documentation, and Independent Validation

**Files:**
- Modify: `client/build.ps1`
- Modify: `client/Plugin.cs`
- Modify: `apworld/scrc/archipelago.json`
- Modify: `tools/validate-repo.py`
- Modify: `docs/PROJECT_OVERVIEW.md`
- Modify: `docs/PROGRESSION.md`
- Modify: `docs/TESTING.md`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Create: `docs/testing/2026-09-10-full-level-mapping-acceptance.md`
- Test: `apworld/tests/test_repository_contract.py`

**Interfaces:**
- Consumes: final IDs, totals, slot keys, client behavior, and diagnostic controls from Tasks 1–6.
- Produces: truthful v0.24/v0.70 candidate documentation and validator-enforced release contract.

- [ ] **Step 1: Write failing repository-contract tests**

Update `test_repository_contract.py` to require:

```python
self.assert_public_counts("121", "179", "237", "273")
self.assertIn("Client v0.70.0 / APWorld v0.24.0", overview)
self.assertIn("special variants are diagnostic-only", overview.lower())
self.assertIn("66 AP Stars remain inactive", overview)
```

Extend `tools/validate-repo.py` assertions for:

- safe location frontier `187256292`;
- exactly 88 campaign location names and IDs;
- exactly 81 IDs in `187256211..187256291`;
- reserved cache IDs still unchanged;
- no instantiated Development Caches;
- exact active totals `121/179/237/273`;
- schema `15` and mapping schema `1`;
- Star and special activation booleans false.

- [ ] **Step 2: Run contract tests and validator and verify RED**

Run:

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_repository_contract.py' -v
py .\tools\validate-repo.py
```

Expected: FAIL on old versions, old counts, and old frontier assertions.

- [ ] **Step 3: Update candidate versions and public status**

Set client `0.70.0` and APWorld `0.24.0` everywhere the validator recognizes. Document:

- all 22 normal campaign identities;
- separate Completion and 1-Star checks;
- difficulty totals `121/179/237/273`;
- Development Caches retired but IDs reserved;
- AP Stars, Star gates, Victory, and special-mode locations still inactive;
- Bee/Devil diagnostics are observation-only;
- fresh v0.24 seed required;
- unplayed mappings remain manual-testing pending.

Do not claim all 22 levels are gameplay-verified.

- [ ] **Step 4: Create the targeted acceptance record**

Create `docs/testing/2026-09-10-full-level-mapping-acceptance.md` with checkboxes and exact evidence fields for:

```text
Commit:
Client SHA-256:
APWorld SHA-256:
Seed name:
AP slot:
Native save slot:
AP difficulty:
Normal first-clear locations:
Improved-result locations:
Offline/reconnect result:
Bee diagnostic log excerpt:
Devil diagnostic log excerpt:
Unplayed campaign mappings:
Tester verdict:
```

Pre-fill candidate versions, expected behavior, commands, and the approved limitation that broad campaign replay is deferred. Leave evidence fields blank checkboxes for the later manual session; do not mark them passed.

- [ ] **Step 5: Run documentation tests and commit**

Run:

```powershell
py -m unittest discover -s .\apworld\tests -p 'test_repository_contract.py' -v
py .\tools\validate-repo.py
```

Expected: both pass and report the new frontiers and versions.

Commit:

```powershell
git add client/build.ps1 client/Plugin.cs apworld/scrc/archipelago.json tools/validate-repo.py docs/PROJECT_OVERVIEW.md docs/PROGRESSION.md docs/TESTING.md docs/testing/2026-09-10-full-level-mapping-acceptance.md README.md CHANGELOG.md apworld/tests/test_repository_contract.py
git commit -m "docs: describe full campaign mapping candidate"
```

---

### Task 8: Run Full Verification and Prepare the Manual Candidate

**Files:**
- Modify only if verification exposes a defect in files already owned by Tasks 1–7.

**Interfaces:**
- Consumes: complete v0.24/v0.70 candidate.
- Produces: reproducible build/test evidence and an undeployed candidate artifact.

- [ ] **Step 1: Run every pure client test project**

Run the same project loop used by the repository's accepted client verification workflow, including the new `SpecialVariantDiagnostic` project.

Expected: every client test project exits 0.

- [ ] **Step 2: Build the client without deployment**

Run:

```powershell
.\client\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus" -SkipInstall
```

Expected: build succeeds, `client/bin/Release/net6.0/RhythmCastleAP.dll` exists, and nothing is copied into the game directory.

- [ ] **Step 3: Run the complete APWorld suite**

Run:

```powershell
py -m unittest discover apworld/tests -v
```

Expected: all tests pass.

- [ ] **Step 4: Run independent validation and package locally**

Run:

```powershell
py .\tools\validate-repo.py
.\tools\build-apworld.ps1
```

Expected: validation passes, reports item frontier `187256156` and location frontier `187256292`, and produces a local `.apworld` candidate without installing or publishing it.

- [ ] **Step 5: Inspect the final diff and status**

Run:

```powershell
git diff main...HEAD --check
git status --short
git log --oneline --decorate main..HEAD
```

Expected: no whitespace errors, no generated binaries/logs/packages are tracked, and only approved feature/docs commits appear.

- [ ] **Step 6: Record automated evidence and stop before deployment**

Add commit hash, artifact hashes, automated counts, and validator output to the acceptance record. Commit only that evidence update:

```powershell
git add docs/testing/2026-09-10-full-level-mapping-acceptance.md
git commit -m "test: record campaign mapping candidate evidence"
```

Report the candidate ready for review. Do not deploy, launch the game, merge, push, or publish without the user's next explicit approval.

## Follow-up Plan Boundary

After the read-only diagnostic ledger identifies exact native events, write a separate implementation plan for:

- Bizzle and Clive item/source lifecycle;
- Super Nectar Bubbles and Recipes items, Bee completion checks, and delivery check;
- Demon Key item/source lifecycle;
- four Devil completion/seal checks and final demon reward source;
- the resulting permanent item/location IDs and new capacity totals.

That follow-up plan must use the captured identifiers verbatim, append IDs from the then-current safe frontiers, and include targeted reload/reconnect acceptance. Production special-mode logic must not be added by extending this plan with inferred flag names.
