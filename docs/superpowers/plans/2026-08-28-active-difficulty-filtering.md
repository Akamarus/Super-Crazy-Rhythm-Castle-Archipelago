# Active Difficulty Filtering Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Activate APWorld difficulty filtering so each new seed instantiates only its selected cumulative performance tiers and sizes its item pool to the resulting location capacity.

**Architecture:** Keep tier recognition and selection as pure functions in `apworld/scrc/difficulty.py`. `SCRCWorld.generate_early()` computes the immutable active-location set, `create_regions()` instantiates only that set, and `create_items()` derives filler capacity from instantiated addressed locations. Permanent ID registries remain unchanged, and Client v0.67.94 requires no gameplay-code changes.

**Tech Stack:** Python 3, Archipelago custom-world APIs, `unittest`, PowerShell packaging, Archipelago 0.6.7 generator

**Spec:** `docs/superpowers/specs/2026-08-28-active-difficulty-filtering-design.md`

## Global Constraints

- Target APWorld version is exactly `0.20.0`; target client remains exactly `0.67.94`.
- Append `difficulty-filtering-0.20` to the existing implementation-version string; do not replace its historical compatibility prefixes.
- Preserve every existing permanent item and location ID.
- AP Performance Difficulty must never force or change native REG/PRO difficulty.
- Add no new campaign locations or IDs; filter only already registered locations.
- Inactive tiers must be absent from instantiated regions, not merely excluded or filler-only.
- Music Lab point chests and active Level-22 2/3-Star checks remain unable to hold progression.
- Existing v0.19 generated seeds remain client-compatible; new filtered seeds require APWorld v0.20.
- Do not merge, push, publish, install, or create a release without explicit project-owner approval.

**Registry-count correction:** The active addressed-location totals are 68/105/142/178
for Normal/Hard/Expert/Perfection. The earlier 76/113/150/186 draft figures counted
eight unregistered values: `BASE_ID + 0` and the intentional reserved gap at
`BASE_ID + 4..+10`. Do not add locations or IDs to reach those obsolete figures.

---

## File Structure

- `apworld/scrc/difficulty.py` — pure recognition and cumulative active-tier/location selection.
- `apworld/scrc/__init__.py` — world lifecycle: compute active names, instantiate filtered regions, size item pool, and emit slot data.
- `apworld/scrc/archipelago.json` — APWorld package version.
- `apworld/tests/test_difficulty.py` — pure filtering behavior and invalid-input coverage.
- `apworld/tests/test_world_integration.py` — instantiated-region, capacity, placement, ID, and slot-data contracts.
- `apworld/tests/test_repository_contract.py` — repository version/validator contract.
- `tools/validate-repo.py` — independent v0.20 version and implementation baseline.
- `README.md`, `apworld/README.md`, `apworld/scrc/docs/*.md`, `docs/{INSTALL,ROADMAP,PROJECT_OVERVIEW,TESTING_AND_ISSUES}.md` — public v0.20 behavior and compatibility boundary.
- `docs/testing/2026-08-28-difficulty-filtering-acceptance.md` — real-generator acceptance record.

---

### Task 1: Extend the Pure Difficulty Filter

**Files:**
- Modify: `apworld/scrc/difficulty.py`
- Modify: `apworld/tests/test_difficulty.py`

**Interfaces:**
- Consumes: registered location names and integer difficulty values `0..3`.
- Produces: `campaign_star_tiers(difficulty: int) -> frozenset[int]`.
- Produces: `medal_tiers(difficulty: int) -> frozenset[str]`.
- Produces: `is_location_active(location_name: str, difficulty: int) -> bool`.
- Preserves: `filter_locations_for_difficulty(location_names, difficulty) -> tuple[str, ...]` as the ordered public selector used by world generation.

- [ ] **Step 1: Add failing tests for campaign and medal tiers**

Add these cases to `apworld/tests/test_difficulty.py`:

```python
def test_campaign_star_tiers_are_cumulative(self):
    self.assertEqual(difficulty.campaign_star_tiers(0), frozenset({1}))
    self.assertEqual(difficulty.campaign_star_tiers(1), frozenset({1, 2}))
    self.assertEqual(difficulty.campaign_star_tiers(2), frozenset({1, 2, 3}))
    self.assertEqual(difficulty.campaign_star_tiers(3), frozenset({1, 2, 3}))

def test_medal_tiers_are_cumulative(self):
    self.assertEqual(difficulty.medal_tiers(0), frozenset({"Bronze"}))
    self.assertEqual(difficulty.medal_tiers(1), frozenset({"Bronze", "Silver"}))
    self.assertEqual(difficulty.medal_tiers(2), frozenset({"Bronze", "Silver", "Gold"}))
    self.assertEqual(
        difficulty.medal_tiers(3),
        frozenset({"Bronze", "Silver", "Gold", "Platinum"}),
    )

def test_campaign_location_filter_uses_existing_star_locations(self):
    names = (
        "Level 22 - Completion",
        "Level 22 - 1 Star",
        "Level 22 - 2 Stars",
        "Level 22 - 3 Stars",
    )
    self.assertEqual(
        difficulty.filter_locations_for_difficulty(names, 0),
        names[:2],
    )
    self.assertEqual(
        difficulty.filter_locations_for_difficulty(names, 1),
        names[:3],
    )
    self.assertEqual(
        difficulty.filter_locations_for_difficulty(names, 2),
        names,
    )
    self.assertEqual(
        difficulty.filter_locations_for_difficulty(names, 3),
        names,
    )
```

- [ ] **Step 2: Run the focused tests and observe the intended failure**

Run:

```powershell
py -m unittest apworld.tests.test_difficulty -v
```

Expected: FAIL because `campaign_star_tiers`, `medal_tiers`, and campaign-Star recognition do not exist.

- [ ] **Step 3: Implement the minimal pure selectors**

In `apworld/scrc/difficulty.py`, define exact cumulative tables and recognize only registered-style campaign suffixes:

```python
CAMPAIGN_STAR_TIERS = (
    frozenset({1}),
    frozenset({1, 2}),
    frozenset({1, 2, 3}),
    frozenset({1, 2, 3}),
)

MEDAL_TIER_SETS = tuple(
    frozenset(MEDAL_TIERS[:limit]) for limit in MEDAL_TIER_LIMIT
)


def campaign_star_tiers(difficulty: int) -> frozenset[int]:
    _validate_difficulty(difficulty)
    return CAMPAIGN_STAR_TIERS[difficulty]


def medal_tiers(difficulty: int) -> frozenset[str]:
    _validate_difficulty(difficulty)
    return MEDAL_TIER_SETS[difficulty]


def _recognized_campaign_star_tier(location_name: str) -> int | None:
    if location_name.endswith(" - 1 Star"):
        return 1
    if location_name.endswith(" - 2 Stars"):
        return 2
    if location_name.endswith(" - 3 Stars"):
        return 3
    return None


def is_location_active(location_name: str, difficulty: int) -> bool:
    _validate_difficulty(difficulty)
    campaign_tier = _recognized_campaign_star_tier(location_name)
    if campaign_tier is not None:
        return campaign_tier in campaign_star_tiers(difficulty)
    medal_tier = _recognized_medal_tier(location_name)
    if medal_tier is not None:
        return medal_tier in medal_tiers(difficulty)
    return True
```

Refactor `filter_locations_for_difficulty` to preserve input order:

```python
return tuple(name for name in location_names if is_location_active(name, difficulty))
```

- [ ] **Step 4: Add invalid-value coverage for every public selector**

Extend the invalid-input test so `campaign_star_tiers`, `medal_tiers`, `is_location_active`, and `filter_locations_for_difficulty` each reject `-1`, `4`, `True`, and `"normal"` with `ValueError` containing `difficulty`.

- [ ] **Step 5: Run the focused tests**

Run:

```powershell
py -m unittest apworld.tests.test_difficulty -v
```

Expected: all difficulty tests PASS.

- [ ] **Step 6: Commit the pure filter**

```powershell
git add apworld/scrc/difficulty.py apworld/tests/test_difficulty.py
git commit -m "feat: filter active performance tiers"
```

---

### Task 2: Instantiate Only Active Locations and Size the Pool Dynamically

**Files:**
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/tests/test_world_integration.py`

**Interfaces:**
- Consumes: `filter_locations_for_difficulty(LOCATION_NAME_TO_ID, difficulty)` from Task 1.
- Produces: `self.active_location_names: frozenset[str]` during `generate_early()`.
- Produces: `self.active_campaign_star_tiers: frozenset[int]` and `self.active_medal_tiers: frozenset[str]`.
- Produces: `SCRCWorld._active_unfilled_location_capacity() -> int` after `create_regions()`.

- [ ] **Step 1: Replace preview assertions with failing active-world tests**

In `apworld/tests/test_world_integration.py`, add helpers:

```python
def build_world(self, difficulty=0, seed=22, starting_area=0):
    world = self.make_world(seed=seed, difficulty=difficulty, starting_area=starting_area)
    world.generate_early()
    world.create_regions()
    return world

@staticmethod
def addressed_names(world):
    return {
        location.name
        for region in world.multiworld.regions
        for location in region.locations
        if location.address is not None
    }
```

Add exact set tests:

```python
def test_normal_omits_inactive_performance_locations(self):
    names = self.addressed_names(self.build_world(difficulty=0))
    self.assertIn("Level 22 - Completion", names)
    self.assertIn("Level 22 - 1 Star", names)
    self.assertNotIn("Level 22 - 2 Stars", names)
    self.assertNotIn("Level 22 - 3 Stars", names)
    self.assertIn("Music Lab Cassette - Zen - Bronze", names)
    self.assertNotIn("Music Lab Cassette - Zen - Silver", names)
    self.assertNotIn("Game Garage - Smooch - Platinum", names)

def test_active_location_counts_are_exact(self):
    expected = {0: 68, 1: 105, 2: 142, 3: 178}
    for value, count in expected.items():
        with self.subTest(difficulty=value):
            self.assertEqual(len(self.addressed_names(self.build_world(value))), count)
```

- [ ] **Step 2: Add failing dynamic-pool tests**

Replace `test_live_pool_size_and_contents_remain_unchanged` with:

```python
def test_item_pool_matches_active_unfilled_capacity(self):
    expected = {0: 68, 1: 105, 2: 142, 3: 178}
    for value, count in expected.items():
        with self.subTest(difficulty=value):
            world = self.build_world(difficulty=value)
            world.create_items()
            self.assertEqual(len(world.multiworld.itempool), count)
            names = [item.name for item in world.multiworld.itempool]
            self.assertNotIn("Star", names)
            self.assertNotIn("Hypno Pan", names)
            self.assertNotIn("Violance", names)
```

Add a capacity failure test by removing all but fourteen addressed locations after `create_regions()` and asserting `create_items()` raises a `ValueError` containing `required progression items` and `active locations`.

- [ ] **Step 3: Run integration tests and observe failure**

Run:

```powershell
py -m unittest apworld.tests.test_world_integration -v
```

Expected: FAIL because inactive locations are still instantiated and the pool still uses the permanent registry count.

- [ ] **Step 4: Compute active sets in `generate_early()`**

Import `campaign_star_tiers`, `medal_tiers`, and `filter_locations_for_difficulty`. Replace preview-only state with:

```python
self.active_location_names = frozenset(
    filter_locations_for_difficulty(LOCATION_NAME_TO_ID, difficulty)
)
self.active_campaign_star_tiers = campaign_star_tiers(difficulty)
self.active_medal_tiers = medal_tiers(difficulty)
```

Keep the existing deterministic Star-requirement preview unchanged.

- [ ] **Step 5: Filter region construction at the point of instantiation**

Use a local predicate at the start of `create_regions()`:

```python
active_names = getattr(self, "active_location_names", frozenset(LOCATION_NAME_TO_ID))

def is_active(name: str) -> bool:
    if name not in LOCATION_NAME_TO_ID:
        raise ValueError(f"unregistered active location: {name}")
    return name in active_names
```

For the existing tier loops, `continue` before constructing a `SCRCLocation` when `not is_active(name)`. Apply this to Music Lab cassettes, Game Garage stickers, and Level-22 ordinary locations. Non-performance locations continue to instantiate unchanged.

Do not remove names from `LOCATION_NAME_TO_ID`.

- [ ] **Step 6: Derive pool capacity from instantiated unfilled addresses**

Add this method to `SCRCWorld`:

```python
def _active_unfilled_location_capacity(self) -> int:
    return sum(
        1
        for region in self.multiworld.regions
        for location in region.locations
        if location.address is not None and location.item is None
    )
```

In `create_items()`, build `progression_items` exactly as before, then calculate:

```python
capacity = self._active_unfilled_location_capacity()
required_count = len(progression_items)
if required_count > capacity:
    raise ValueError(
        f"{required_count} required progression items exceed "
        f"{capacity} active locations for {self.difficulty_name}"
    )
filler_count = capacity - required_count
```

Store `self.difficulty_name` during `generate_early()` from `DIFFICULTY_NAMES[difficulty]`.

- [ ] **Step 7: Preserve unsafe active-location placement rules**

Keep point-chest `item_rule` restrictions. Keep Level-22 2/3-Star restrictions whenever those locations are active. Keep Garage access rules and difficulty-active placement evaluation. Update test expectations so inactive locations raise `KeyError` from `get_location`, while active unsafe locations accept Stardust and reject `Plant Pipes`.

- [ ] **Step 8: Run integration and complete APWorld tests**

Run:

```powershell
py -m unittest apworld.tests.test_world_integration -v
py -m unittest discover apworld/tests -v
```

Expected: all tests PASS.

- [ ] **Step 9: Commit active region construction**

```powershell
git add apworld/scrc/__init__.py apworld/tests/test_world_integration.py
git commit -m "feat: generate only active difficulty checks"
```

---

### Task 3: Activate the v0.20 Slot and Repository Contract

**Files:**
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/scrc/archipelago.json`
- Modify: `apworld/tests/test_world_integration.py`
- Modify: `apworld/tests/test_repository_contract.py`
- Modify: `tools/validate-repo.py`

**Interfaces:**
- Consumes: active sets and instantiated capacity from Task 2.
- Produces slot fields: `difficulty_filtering_active`, `active_location_count`, `active_campaign_star_tiers`, and `active_medal_tiers`.
- Produces implementation version ending in `difficulty-filtering-0.20`.

- [ ] **Step 1: Write failing slot-data tests**

Add a test that builds regions before reading slot data:

```python
def test_slot_data_reports_active_difficulty_filtering(self):
    world = self.build_world(difficulty=1)
    data = world.fill_slot_data()
    self.assertTrue(data["difficulty_filtering_active"])
    self.assertEqual(data["active_location_count"], 105)
    self.assertEqual(data["active_campaign_star_tiers"], [1, 2])
    self.assertEqual(data["active_medal_tiers"], ["Bronze", "Silver"])
    self.assertTrue(data["implementation_version"].endswith("difficulty-filtering-0.20"))
```

Update the existing preview assertion to require `difficulty_filtering_active: true` rather than false.

- [ ] **Step 2: Make the independent repository contract fail first**

Change only these `EXPECTED` values in `tools/validate-repo.py`:

```python
"world_version": "0.20.0",
"implementation_version": (
    "area-routing-plant-pipes-0.15-generation-foundation-0.16-"
    "hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-"
    "consolidated-preview-0.19-difficulty-filtering-0.20"
),
```

Run:

```powershell
py tools/validate-repo.py
```

Expected: FAIL because APWorld metadata and slot data still report v0.19.

- [ ] **Step 3: Update APWorld metadata and slot data**

Set `world_version` to `0.20.0` in `archipelago.json`. Append the exact v0.20 suffix in `fill_slot_data()`, set `schema_version` to `11`, and emit:

```python
"difficulty_filtering_active": True,
"active_location_count": len(getattr(self, "active_location_names", LOCATION_NAME_TO_ID)),
"active_campaign_star_tiers": sorted(getattr(self, "active_campaign_star_tiers", {1})),
"active_medal_tiers": [
    tier for tier in MEDAL_TIERS
    if tier in getattr(self, "active_medal_tiers", {"Bronze"})
],
```

Preserve `difficulty_preview_location_count` only if a public compatibility consumer still uses it; if retained, make it equal the active count and label it legacy in a source comment.

- [ ] **Step 4: Update repository-contract assertions**

In `test_repository_contract.py`, change expected world and implementation versions to v0.20 and add assertions that validator output reports them. Fixture-mutation tests must still fail for their intended ID or manifest reason rather than failing early on version mismatch.

- [ ] **Step 5: Run slot and repository tests**

Run:

```powershell
py -m unittest apworld.tests.test_world_integration -v
py -m unittest apworld.tests.test_repository_contract -v
py tools/validate-repo.py
```

Expected: all commands PASS and validator prints Client v0.67.94 / APWorld v0.20.0.

- [ ] **Step 6: Commit the versioned slot contract**

```powershell
git add apworld/scrc/__init__.py apworld/scrc/archipelago.json apworld/tests/test_world_integration.py apworld/tests/test_repository_contract.py tools/validate-repo.py
git commit -m "feat: activate v0.20 difficulty slot contract"
```

---

### Task 4: Update Public Documentation and Package Contract

**Files:**
- Modify: `README.md`
- Modify: `apworld/README.md`
- Modify: `apworld/scrc/docs/en_Super Crazy Rhythm Castle.md`
- Modify: `apworld/scrc/docs/setup_en.md`
- Modify: `docs/INSTALL.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/PROJECT_OVERVIEW.md`
- Modify: `docs/TESTING_AND_ISSUES.md`
- Modify: `apworld/examples/SCRC-AreaRouting-PlantPipes.yaml`
- Create: `docs/testing/2026-08-28-difficulty-filtering-acceptance.md`

**Interfaces:**
- Consumes: final v0.20 names, counts, tier tables, and compatibility data from Tasks 1–3.
- Produces: tester-facing option guidance and a reproducible acceptance checklist.

- [ ] **Step 1: Update current-version language**

Replace current APWorld v0.19 baseline references in public/current sections with v0.20 while preserving explicitly historical v0.19 test records. State that Client v0.67.94 is unchanged.

- [ ] **Step 2: Document exact option behavior**

Add the approved table to the APWorld readme/setup pages and state:

```text
Normal: campaign Completion/1-Star; Bronze songs
Hard: add campaign 2-Star; add Silver songs
Expert: add campaign 3-Star; add Gold songs
Perfection: same campaign tiers as Expert; add Platinum songs
```

Explain that inactive checks are absent, native REG/PRO remains player-controlled, and this release filters only existing campaign performance locations.

- [ ] **Step 3: Update the example YAML comments**

Keep `difficulty: normal` and add comments listing all four choices. State that it controls AP locations, not native REG/PRO.

- [ ] **Step 4: Create the acceptance record**

Create `docs/testing/2026-08-28-difficulty-filtering-acceptance.md` with unchecked rows for:

```markdown
| Difficulty | Expected addressed locations | Campaign tiers | Medal tiers | Generated |
| --- | ---: | --- | --- | --- |
| Normal | 68 | 1 | Bronze | [ ] |
| Hard | 105 | 1–2 | Bronze–Silver | [ ] |
| Expert | 142 | 1–3 | Bronze–Gold | [ ] |
| Perfection | 178 | 1–3 | Bronze–Platinum | [ ] |
```

Include commands, seed numbers, output archive names, spoiler findings, and an explicit progression-placement result field. Do not mark rows complete until Task 5 runs the real generator.

- [ ] **Step 5: Run documentation and repository validation**

Run:

```powershell
py tools/validate-repo.py
git diff --check
```

Expected: PASS.

- [ ] **Step 6: Commit documentation**

```powershell
git add README.md apworld/README.md apworld/scrc/docs apworld/examples docs/INSTALL.md docs/ROADMAP.md docs/PROJECT_OVERVIEW.md docs/TESTING_AND_ISSUES.md docs/testing/2026-08-28-difficulty-filtering-acceptance.md
git commit -m "docs: document v0.20 difficulty filtering"
```

---

### Task 5: Full Verification and Real Generator Matrix

**Files:**
- Modify: `docs/testing/2026-08-28-difficulty-filtering-acceptance.md`
- Generated but do not commit: `dist/scrc.apworld`
- Temporary and do not commit: isolated player/output directories under `$env:TEMP`

**Interfaces:**
- Consumes: complete v0.20 APWorld and acceptance record.
- Produces: verified package plus four real generated-seed results.

- [ ] **Step 1: Run all APWorld tests**

```powershell
$env:PYTHONDONTWRITEBYTECODE = '1'
py -m unittest discover apworld/tests -v
```

Expected: all tests PASS; record the exact test count.

- [ ] **Step 2: Run repository validation and package the APWorld**

```powershell
py tools/validate-repo.py
.\tools\build-apworld.ps1
```

Expected: validator reports Client v0.67.94 / APWorld v0.20.0 and `dist\scrc.apworld` is created.

- [ ] **Step 3: Inspect package contents**

Open `dist/scrc.apworld` as a ZIP and confirm it contains only the `scrc/` distribution sources listed by `test_packaged_world_contains_only_distribution_sources`; confirm `archipelago.json` says `0.20.0`.

- [ ] **Step 4: Install only with explicit owner approval**

If approval is given, replace only:

```text
C:\ProgramData\Archipelago\custom_worlds\scrc.apworld
```

Restart Archipelago Launcher afterward. If approval is not given, perform the remaining real-generation matrix only after configuring an isolated Archipelago custom-world directory; do not silently replace the installed package.

- [ ] **Step 5: Generate one real seed per difficulty**

Create four isolated YAML directories under `$env:TEMP`, each based on `apworld/examples/SCRC-AreaRouting-PlantPipes.yaml`, with difficulties and deterministic seeds:

```text
Normal      seed 42001
Hard        seed 42002
Expert      seed 42003
Perfection  seed 42004
```

For each, run:

```powershell
& 'C:\ProgramData\Archipelago\ArchipelagoGenerate.exe' `
  --player_files_path $players `
  --outputpath $output `
  --seed $seed `
  --spoiler 3 `
  --log_level warning
```

Expected: exit code 0 and one output ZIP per difficulty.

- [ ] **Step 6: Audit real spoiler files**

For every seed, verify:

- addressed-location count equals 68/105/142/178;
- only the expected campaign and medal tiers appear;
- inactive tiers are absent, not populated with Stardust;
- point chests contain only Stardust;
- active Level-22 2/3-Star checks contain only Stardust;
- Garage progression appears only where the matching cartridge route is logically reachable;
- the generated playthrough reaches Victory.

Record archive names and findings in the acceptance document, then check the four `Generated` boxes.

- [ ] **Step 7: Run the final clean verification**

```powershell
py -m unittest discover apworld/tests -q
py tools/validate-repo.py
git diff --check
git status --short
```

Expected: tests and validator PASS; only the acceptance-record update is uncommitted; generated packages and temporary outputs are not staged.

- [ ] **Step 8: Commit acceptance evidence**

```powershell
git add docs/testing/2026-08-28-difficulty-filtering-acceptance.md
git commit -m "test: verify v0.20 difficulty generation"
```

---

## Completion Gate

The milestone is complete only when:

- all four difficulties instantiate exactly the approved cumulative tiers;
- inactive tiers are absent from real generated worlds;
- item-pool capacity exactly matches active locations;
- all permanent IDs remain unchanged;
- all APWorld tests and repository validation pass;
- four real Archipelago seeds pass spoiler and Victory-route audits;
- documentation identifies Client v0.67.94 / APWorld v0.20 correctly;
- no client DLL, save, game files, merge, push, or release was changed without explicit approval.
