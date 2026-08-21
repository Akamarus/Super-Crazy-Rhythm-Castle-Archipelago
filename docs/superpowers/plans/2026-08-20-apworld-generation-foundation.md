# APWorld Generation Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add deterministic, tested APWorld options and generation planners for Stars, difficulty, starting areas, and Level 1–22 requirements without activating systems that would make current seeds unplayable.

**Architecture:** Extract pure generation responsibilities from the monolithic world into focused modules, then connect additive preview data through the existing world integration. The live v0.15 item pool, location set, client rules, and Area Access victory remain unchanged; Stars and difficulty filtering are represented, validated, and explicitly inactive.

**Tech Stack:** Python 3.13 standard library, Archipelago 0.6.7 option/world APIs, `unittest`, PowerShell APWorld packaging, existing repository validator.

**Spec:** `docs/superpowers/specs/2026-08-20-apworld-generation-foundation-design.md`

## Global Constraints

- `required_stars` ranges from 1 through 66 and defaults to 50.
- `difficulty` values are Normal, Hard, Expert, and Perfection.
- `starting_area` represents random, Roots, Lobby, Meat Dimension, and Cell Tower; Tower of Fear and Royal Corridor are excluded.
- Only Roots is initially validated as a starter; random samples only validated starters.
- Explicit unsupported fixed starts fail generation and never silently become Roots.
- Exactly 66 planned `Star` instances share one permanent item ID, `187256118` (`BASE_ID + 118`).
- No new location IDs are allocated.
- Stars and difficulty filtering remain inactive in live generation.
- Current native mappings, client behavior, live item pool, live location set, and Area Access victory remain unchanged.
- All production behavior is developed test-first; every RED failure must be observed before implementation.

---

### Task 1: Pure Level Requirement Generator

**Files:**
- Create: `apworld/scrc/star_requirements.py`
- Create: `apworld/tests/support.py`
- Create: `apworld/tests/test_star_requirements.py`

**Interfaces:**
- Consumes: `required_stars: int`, seeded `random.Random`.
- Produces: `LEVEL_NAMES: tuple[str, ...]`, `generate_star_requirements(required_stars, rng) -> dict[str, int]`, and `validate_star_requirements(requirements, required_stars) -> None`.

- [ ] **Step 1: Add a direct module loader for tests**

Create `apworld/tests/support.py` so pure modules can be tested without importing `apworld/scrc/__init__.py` or requiring an Archipelago installation:

```python
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path

SCRC_DIR = Path(__file__).resolve().parents[1] / "scrc"


def load_scrc_module(module_name: str):
    path = SCRC_DIR / f"{module_name}.py"
    spec = spec_from_file_location(f"scrc_test_{module_name}", path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load {path}")
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    return module
```

- [ ] **Step 2: Write failing requirement tests**

Create `apworld/tests/test_star_requirements.py` with focused `unittest.TestCase` methods covering:

```python
import random
import unittest

from support import load_scrc_module

requirements = load_scrc_module("star_requirements")


class StarRequirementTests(unittest.TestCase):
    def test_generates_all_22_levels_with_monotonic_bounded_values(self):
        result = requirements.generate_star_requirements(50, random.Random(12345))
        self.assertEqual(tuple(result), tuple(f"Level {n}" for n in range(1, 23)))
        values = tuple(result.values())
        self.assertEqual(values, tuple(sorted(values)))
        self.assertTrue(all(0 <= value < 50 for value in values))

    def test_same_seed_is_reproducible(self):
        first = requirements.generate_star_requirements(50, random.Random(7))
        second = requirements.generate_star_requirements(50, random.Random(7))
        self.assertEqual(first, second)

    def test_small_goals_remain_valid(self):
        for goal in (1, 2, 10, 50, 66):
            with self.subTest(goal=goal):
                result = requirements.generate_star_requirements(goal, random.Random(99))
                requirements.validate_star_requirements(result, goal)
                self.assertLess(result["Level 22"], goal)
```

Add separate tests that assert `ValueError` messages for goals 0 and 67, missing/extra levels, booleans or non-integer thresholds, negative thresholds, thresholds at the goal, and decreasing sequences.

- [ ] **Step 3: Run the focused test and verify RED**

Run:

```powershell
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest discover -s .\apworld\tests -p 'test_star_requirements.py' -v
```

Expected: FAIL because `apworld/scrc/star_requirements.py` does not exist.

- [ ] **Step 4: Implement the minimal deterministic generator**

Create `star_requirements.py` with:

```python
from __future__ import annotations

import random
from collections.abc import Mapping

LEVEL_NAMES = tuple(f"Level {number}" for number in range(1, 23))
DEFAULT_DEPTH_FRACTIONS = (
    0.00, 0.02, 0.04, 0.08, 0.12, 0.16, 0.20, 0.24, 0.28, 0.32, 0.36,
    0.40, 0.46, 0.52, 0.58, 0.64, 0.70, 0.76, 0.82, 0.86, 0.90, 0.94,
)


def generate_star_requirements(required_stars: int, rng: random.Random) -> dict[str, int]:
    _validate_goal(required_stars)
    maximum = required_stars - 1
    values: list[int] = []
    previous = 0
    for fraction in DEFAULT_DEPTH_FRACTIONS:
        center = round(maximum * fraction)
        spread = max(0, round(maximum * 0.04))
        candidate = center + (rng.randint(-spread, spread) if spread else 0)
        value = min(maximum, max(previous, candidate, 0))
        values.append(value)
        previous = value
    result = dict(zip(LEVEL_NAMES, values, strict=True))
    validate_star_requirements(result, required_stars)
    return result
```

Implement validation exactly as specified, explicitly rejecting `bool` even though it subclasses `int`.

- [ ] **Step 5: Run RED→GREEN and the full foundation tests**

Run the focused command until it reports all tests passing, then:

```powershell
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
```

Expected: all tests pass with no warnings.

- [ ] **Step 6: Commit Task 1**

```powershell
git add apworld/scrc/star_requirements.py apworld/tests/support.py apworld/tests/test_star_requirements.py
git commit -m "feat(apworld): generate deterministic star requirements"
```

---

### Task 2: Pure Difficulty Filtering Preview

**Files:**
- Create: `apworld/scrc/difficulty.py`
- Create: `apworld/tests/test_difficulty.py`

**Interfaces:**
- Consumes: `Iterable[str]` of existing location names and integer difficulty 0–3.
- Produces: `filter_locations_for_difficulty(location_names, difficulty) -> tuple[str, ...]`, `DIFFICULTY_NAMES`, and tier limits.

- [ ] **Step 1: Write failing cumulative-filter tests**

Create representative existing names and assert exact results:

```python
class DifficultyTests(unittest.TestCase):
    ALL = (
        "Level 1 - Completion",
        "Game Garage - Smooch - Bronze",
        "Game Garage - Smooch - Silver",
        "Game Garage - Smooch - Gold",
        "Game Garage - Smooch - Platinum",
        "Music Lab Cassette - Zen - Bronze",
        "Music Lab Cassette - Zen - Silver",
        "Music Lab Cassette - Zen - Gold",
        "Music Lab Cassette - Zen - Platinum",
        "Music Lab - 5 Point Chest",
        "Roots - Gecko's Weed Killer",
    )

    def test_normal_keeps_bronze_and_non_tier_locations(self):
        result = difficulty.filter_locations_for_difficulty(self.ALL, 0)
        self.assertIn("Game Garage - Smooch - Bronze", result)
        self.assertNotIn("Game Garage - Smooch - Silver", result)
        self.assertIn("Music Lab - 5 Point Chest", result)
        self.assertIn("Roots - Gecko's Weed Killer", result)
```

Add separate exact tests for Hard, Expert, and Perfection; input-order preservation; tuple return type; and invalid values `-1`, `4`, `True`, and `"normal"` raising `ValueError`.

- [ ] **Step 2: Run and verify RED**

Run only `test_difficulty.py`. Expected: FAIL because the module is missing.

- [ ] **Step 3: Implement explicit tier recognition**

Create `difficulty.py` with numeric constants and explicit prefixes:

```python
DIFFICULTY_NAMES = ("Normal", "Hard", "Expert", "Perfection")
MEDAL_TIERS = ("Bronze", "Silver", "Gold", "Platinum")
MEDAL_TIER_LIMIT = (1, 2, 3, 4)
TIERED_PREFIXES = ("Game Garage - ", "Music Lab Cassette - ")
```

Recognize a medal tier only when the name starts with a known prefix and ends with ` - <tier>`. Preserve all other names. Keep campaign completion checks because no higher campaign tier names currently exist.

- [ ] **Step 4: Run RED→GREEN and all tests**

Expected: focused tests pass, then the complete `apworld/tests` suite passes.

- [ ] **Step 5: Commit Task 2**

```powershell
git add apworld/scrc/difficulty.py apworld/tests/test_difficulty.py
git commit -m "feat(apworld): preview cumulative difficulty locations"
```

---

### Task 3: Options and Conservative Starting-Area Resolution

**Files:**
- Create: `apworld/scrc/options.py`
- Create: `apworld/scrc/starting_areas.py`
- Create: `apworld/tests/test_options_and_starts.py`
- Modify: `apworld/scrc/__init__.py`

**Interfaces:**
- Consumes: option numeric values and seeded RNG.
- Produces: `SCRCOptions`, `RequiredStars`, `Difficulty`, `StartingArea`, `resolve_starting_area(requested, rng) -> str`, `VALIDATED_STARTING_AREAS`.

- [ ] **Step 1: Write failing pure starting-area tests**

Test these exact behaviors:

```python
self.assertEqual(starts.resolve_starting_area(0, random.Random(1)), "Roots Access")
self.assertEqual(starts.resolve_starting_area(1, random.Random(1)), "Roots Access")
with self.assertRaisesRegex(ValueError, "Lobby.*not validated"):
    starts.resolve_starting_area(2, random.Random(1))
```

Repeat failure assertions for Meat Dimension and Cell Tower. Assert that supported option values are exactly `0..4`, `VALIDATED_STARTING_AREAS == ("Roots Access",)`, and repeated seeded random resolution is deterministic.

- [ ] **Step 2: Write failing option-definition tests using lightweight stubs**

Before directly loading `options.py`, insert a test-only `Options` module in `sys.modules` with minimal `Choice`, `Range`, and `PerGameCommonOptions` classes. Assert:

- `RequiredStars.range_start == 1`, `range_end == 66`, `default == 50`;
- difficulty option values are 0–3 with default Normal;
- starting-area option values are random 0, Roots 1, Lobby 2, Meat Dimension 3, Cell Tower 4;
- `SCRCOptions` dataclass fields are `required_stars`, `difficulty`, and `starting_area` in addition to inherited common options.

Remove the stub from `sys.modules` in cleanup so tests cannot leak state.

- [ ] **Step 3: Run and verify RED**

Expected: tests fail because both modules are missing and `__init__.py` still defines an empty options dataclass.

- [ ] **Step 4: Implement the starting-area module and options**

Use stable integer constants in `starting_areas.py`, with `STARTING_AREA_TO_ITEM` mapping only represented choices. `resolve_starting_area` samples `VALIDATED_STARTING_AREAS` for random and raises a descriptive error for explicit unvalidated choices.

Implement Archipelago classes in `options.py`:

```python
class RequiredStars(Range):
    display_name = "Required Stars"
    range_start = 1
    range_end = 66
    default = 50


class Difficulty(Choice):
    display_name = "AP Performance Difficulty"
    option_normal = 0
    option_hard = 1
    option_expert = 2
    option_perfection = 3
    default = 0


class StartingArea(Choice):
    display_name = "Starting Area"
    option_random = 0
    option_roots = 1
    option_lobby = 2
    option_meat_dimension = 3
    option_cell_tower = 4
    default = 0
```

Move `SCRCOptions` to this module and import it from `__init__.py`; do not yet change live starter selection.

- [ ] **Step 5: Run RED→GREEN and regression tests**

Run the focused tests, full foundation suite, and repository validator. The validator must still report v0.15 at this intermediate commit.

- [ ] **Step 6: Commit Task 3**

```powershell
git add apworld/scrc/options.py apworld/scrc/starting_areas.py apworld/tests/test_options_and_starts.py apworld/scrc/__init__.py
git commit -m "feat(apworld): define generation foundation options"
```

---

### Task 4: Planned Star Inventory and Permanent ID

**Files:**
- Create: `apworld/scrc/items.py`
- Create: `apworld/tests/test_items.py`
- Modify: `apworld/scrc/__init__.py`
- Modify: `docs/IDS.md`

**Interfaces:**
- Consumes: `available_locations: int`, `existing_required_items: int`.
- Produces: `STAR_ITEM_NAME = "Star"`, `STAR_ITEM_COUNT = 66`, ID `BASE_ID + 118`, and `validate_planned_item_capacity(available_locations: int, existing_required_items: int) -> None`.

- [ ] **Step 1: Write failing Star model and capacity tests**

Use a lightweight `BaseClasses` stub exposing `ItemClassification.progression`. Assert:

```python
self.assertEqual(items.STAR_ITEM_NAME, "Star")
self.assertEqual(items.STAR_ITEM_COUNT, 66)
self.assertEqual(items.NEW_ITEM_NAME_TO_ID["Star"], 187256118)
self.assertEqual(items.planned_star_names(), ("Star",) * 66)
```

Assert `validate_planned_item_capacity(79, 13)` passes, while `(78, 13)` raises `ValueError` containing `79 required` and `78 locations`. Assert booleans, negative counts, and inconsistent counts fail clearly.

- [ ] **Step 2: Run and verify RED**

Expected: FAIL because `items.py` is missing.

- [ ] **Step 3: Implement the pure planning model**

`planned_star_names()` returns a newly constructed tuple of 66 identical names. Capacity is `existing_required_items + STAR_ITEM_COUNT`; equality with available locations is valid. Keep existing item dictionaries in `__init__.py` for minimal risk, importing and merging only the new Star ID/classification metadata. Do not append Stars in `create_items()`.

- [ ] **Step 4: Update the permanent ID registry**

Add `+118 / 187256118 / Star` to `docs/IDS.md`, then advance the next safe item offset/ID to `+119 / 187256119`. State that the item is registered for the generation foundation but inactive in the live pool.

- [ ] **Step 5: Run RED→GREEN and confirm live pool code is unchanged**

Run all unit tests and:

```powershell
git diff --check
rg -n 'progression_items\.append\("Star"\)|planned_star_names\(' apworld/scrc/__init__.py
```

Expected: no live `progression_items.append("Star")`; the planner may be imported for metadata/slot data only.

- [ ] **Step 6: Commit Task 4**

```powershell
git add apworld/scrc/items.py apworld/tests/test_items.py apworld/scrc/__init__.py docs/IDS.md
git commit -m "feat(apworld): register planned star inventory"
```

---

### Task 5: World Integration and Additive Preview Slot Data

**Files:**
- Create: `apworld/tests/test_world_integration.py`
- Modify: `apworld/scrc/__init__.py`
- Modify: `apworld/scrc/archipelago.json`
- Modify: `apworld/examples/SCRC-AreaRouting-PlantPipes.yaml`

**Interfaces:**
- Consumes: option objects, world RNG, pure helpers from Tasks 1–4, existing location names.
- Produces: resolved starter, generated requirement preview, difficulty-location preview count, and additive slot-data schema.

- [ ] **Step 1: Write failing source-level integration assertions**

Because the repository does not vendor Archipelago core, use `ast` plus direct pure-module calls to assert the integration contract without pretending to run the full framework. Tests must verify:

- `SCRCWorld.options_dataclass` resolves to imported `SCRCOptions`;
- `generate_early` calls `resolve_starting_area` with the option value and `self.random`;
- `generate_early` calls `generate_star_requirements` with `required_stars` and `self.random`;
- `create_items` does not add planned Stars;
- `create_regions` does not filter live locations;
- `fill_slot_data` contains every activation flag and preview field from the spec;
- the implementation/schema tag is updated consistently; and
- the current Area Access victory rule remains present.

Use AST node inspection for calls and assignments rather than brittle whole-file substring matching where practical.

- [ ] **Step 2: Run and verify RED**

Expected: FAIL because the world does not yet call the planners or expose preview slot data.

- [ ] **Step 3: Integrate options and preview generation**

In `generate_early`:

```python
requested_start = int(self.options.starting_area.value)
self.starting_area_item = resolve_starting_area(requested_start, self.random)
self.generated_star_requirements = generate_star_requirements(
    int(self.options.required_stars.value), self.random
)
self.difficulty_preview_locations = filter_locations_for_difficulty(
    LOCATION_NAME_TO_ID, int(self.options.difficulty.value)
)
self.multiworld.push_precollected(self.create_item(self.starting_area_item))
```

Do not catch unsupported fixed-start `ValueError`; allow generation to stop with its descriptive message.

- [ ] **Step 4: Add accurately labeled slot data**

Add exact fields:

```python
"schema_version": 8,
"required_stars": required_stars,
"difficulty": {"value": difficulty_value, "name": DIFFICULTY_NAMES[difficulty_value]},
"starting_area_requested": STARTING_AREA_NAMES[requested_start],
"starting_area_resolved": AREA_ITEM_TO_REGION[starter],
"validated_starting_areas": [AREA_ITEM_TO_REGION[item] for item in VALIDATED_STARTING_AREAS],
"star_item_name": STAR_ITEM_NAME,
"star_item_count": STAR_ITEM_COUNT,
"star_items_active": False,
"generated_star_requirements": dict(self.generated_star_requirements),
"generated_star_requirements_depth_model": "provisional-linear-level-order",
"client_star_gate_enforcement_active": False,
"difficulty_filtering_active": False,
"difficulty_preview_location_count": len(self.difficulty_preview_locations),
"development_area_access_victory_active": True,
```

Use safe defaults in `fill_slot_data` only for direct test construction; normal generation must populate all values in `generate_early`.

- [ ] **Step 5: Advance APWorld metadata and example YAML**

Set `world_version` to `0.16`, increment metadata `version` and `compatible_version` together to 8, and use implementation tag `generation-foundation-0.16`. Add documented YAML values:

```yaml
required_stars: 50
difficulty: normal
starting_area: random
```

Include comments that Stars, filtering, gate enforcement, and final victory remain inactive previews in v0.16.

- [ ] **Step 6: Run RED→GREEN and regression checks**

Run all unit tests. Inspect `git diff` to confirm `create_items`, `create_regions`, `set_rules`, and client code retain live behavior apart from imports/metadata required for previews.

- [ ] **Step 7: Commit Task 5**

```powershell
git add apworld/tests/test_world_integration.py apworld/scrc/__init__.py apworld/scrc/archipelago.json apworld/examples/SCRC-AreaRouting-PlantPipes.yaml
git commit -m "feat(apworld): export generation foundation previews"
```

---

### Task 6: Validator, Documentation, and Packaging Gate

**Files:**
- Create: `apworld/tests/test_repository_contract.py`
- Modify: `tools/validate-repo.py`
- Modify: `apworld/README.md`
- Modify: `README.md`
- Modify: `docs/ROADMAP.md`
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: v0.16 module layout, Star ID registry, slot-data tag, activation flags.
- Produces: repository-wide validation and accurate public status documentation.

- [ ] **Step 1: Write failing repository-contract tests**

Test that the validator's expected constants are updated to:

```python
"world_version": "0.16"
"implementation_version": "generation-foundation-0.16"
"star_item_id": 187256118
"next_item_id": 187256119
```

Assert it parses every `apworld/scrc/*.py` file, checks the Star allocation in `items.py`, checks both inactive activation flags, and no longer requires the literal forced-Roots assignment.

- [ ] **Step 2: Run and verify RED**

Expected: FAIL because the validator still encodes v0.15 and assumes all IDs live in `__init__.py`.

- [ ] **Step 3: Update the repository validator**

Parse every Python source file under `apworld/scrc`, report the relative filename on syntax errors, validate the new metadata/tag/ID markers, retain all existing native flag checks, and require:

```python
"star_items_active": False
"difficulty_filtering_active": False
```

The validator output must state that v0.16 generation foundations are previews and that live generation remains on the Area Access milestone.

- [ ] **Step 4: Update public documentation without overstating activation**

Document:

- available options and conservative random-start behavior;
- generated Level 1–22 preview requirements;
- registered/planned 66-Star inventory;
- inactive Star pool, difficulty filtering, client gates, and final victory;
- fresh-seed requirement for v0.16; and
- the capacity reason activation is deferred.

Update the roadmap status to `Implemented` only for foundation/planning helpers, while gameplay systems remain `Design approved / not implemented`.

- [ ] **Step 5: Run the complete verification gate**

Run, in order:

```powershell
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest discover -s .\apworld\tests -p 'test_*.py' -v
& 'C:\Users\Jack\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' .\tools\validate-repo.py
.\tools\build-apworld.ps1
git diff --check origin/main..HEAD
git status --short
```

Expected:

- all unit tests pass with zero failures/errors;
- repository validation reports v0.16 and Star ID `187256118`;
- `dist/scrc.apworld` is created successfully;
- diff check emits no errors; and
- only the packaged `dist` artifact, if ignored, remains outside tracked changes.

- [ ] **Step 6: Inspect the packaged APWorld**

Open the archive listing and confirm it includes:

```text
scrc/__init__.py
scrc/options.py
scrc/items.py
scrc/difficulty.py
scrc/starting_areas.py
scrc/star_requirements.py
scrc/archipelago.json
```

Confirm it contains no tests, local configuration, logs, save files, or proprietary assemblies.

- [ ] **Step 7: Commit Task 6**

```powershell
git add tools/validate-repo.py apworld/tests/test_repository_contract.py apworld/README.md README.md docs/ROADMAP.md CHANGELOG.md
git commit -m "docs: document APWorld generation foundation"
```

---

## Final Review and Handoff

- [ ] Run a fresh full unit suite, repository validator, APWorld package build, and `git diff --check`.
- [ ] Compare `origin/main..HEAD` and confirm no client code, native mapping, new location ID, live Star placement, live difficulty filtering, or victory change is present.
- [ ] Request an independent whole-branch code review against the approved spec and this plan.
- [ ] Fix every Critical or Important finding using a new failing test first.
- [ ] Present the verified branch for the project owner's choice; do not merge or push without explicit approval.
