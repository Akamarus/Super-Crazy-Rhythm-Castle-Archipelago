#!/usr/bin/env python3
from __future__ import annotations

import ast
from importlib.util import module_from_spec, spec_from_file_location
import json
import os
import random
import re
import runpy
import sys
import types
from pathlib import Path

ROOT = Path(os.environ.get("SCRC_REPO_ROOT", Path(__file__).resolve().parents[1])).resolve()
WORLD = ROOT / "apworld" / "scrc" / "__init__.py"
WORLD_DIR = WORLD.parent
ITEMS = WORLD_DIR / "items.py"
POINTS = WORLD_DIR / "music_lab_points.py"
CAMPAIGN = WORLD_DIR / "campaign_levels.py"
EXPANDED = WORLD_DIR / "expanded_checks.py"
EXPANDED_CLIENT = ROOT / "client" / "ExpandedCheckCatalog.cs"
EXPANDED_POLICY = ROOT / "client" / "ExpandedChecksPolicy.cs"
META = ROOT / "apworld" / "scrc" / "archipelago.json"
CLIENT = ROOT / "client" / "Plugin.cs"
CLIENT_BUILD = ROOT / "client" / "build.ps1"
CASSETTE_POLICY = ROOT / "client" / "CassetteRandomizationPolicy.cs"
IDS = ROOT / "docs" / "IDS.md"
OVERVIEW = ROOT / "docs" / "PROJECT_OVERVIEW.md"
TEST_SUPPORT = ROOT / "apworld" / "tests" / "support.py"
EXPECTED = {
    "client_version": "0.75.13",
    "world_version": "0.28.1",
    "implementation_version": (
        "area-routing-plant-pipes-0.15-generation-foundation-0.16-"
        "hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-"
        "consolidated-preview-0.19-difficulty-filtering-0.20-"
        "vanilla-vampire-garage-0.21-full-cassettes-0.22-"
        "music-lab-points-0.23-full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28"
    ),
    "generation_foundation_version": "generation-foundation-0.16",
    "weed_killer_item_id": 187256116,
    "plant_pipes_item_id": 187256117,
    "gecko_location_id": 187256178,
    "frog_hippo_location_id": 187256179,
    "star_item_id": 187256118,
    "hip_glasses_item_id": 187256119,
    "chicken_bucket_item_id": 187256120,
    "hypno_pan_item_id": 187256121,
    "violance_item_id": 187256122,
    "money_cassette_item_id": 187256123,
    "hip_glasses_location_id": 187256180,
    "bucket_trade_location_id": 187256181,
    "money_cassette_location_id": 187256186,
    "music_lab_point_schema": 1,
    "music_lab_point_items": (
        ("Music Lab Point", 187256153, 1, 10),
        ("Music Lab Point Bundle", 187256154, 10, 3),
        ("Music Lab Point Large Bundle", 187256155, 20, 7),
    ),
    "music_lab_point_total_value": 180,
    "music_lab_point_total_instances": 20,
    "music_lab_point_max_effective": 180,
    "music_lab_point_thresholds": (
        (5, "Music Lab - 5 Point Chest"),
        (10, "Music Lab - 10 Point Chest"),
        (20, "Music Lab - 20 Point Chest"),
        (32, "Music Lab - 32 Point Chest"),
        (46, "Music Lab - 46 Point Chest"),
        (64, "Music Lab - 64 Point Chest"),
        (89, "Music Lab - 89 Point Chest"),
        (111, "Music Lab - 111 Point Chest"),
        (140, "Music Lab - 140 Point Chest"),
    ),
    "quest_checks_schema": 1,
    "quest_items": {"Plunger": 187256161, "Meoo": 187256162, "Maniac": 187256163},
    "quest_locations": {
        "Lobby - Plunger Pickup": 187256294,
        "Lobby - Car Battery Hand-In": 187256295,
        "Game Garage - Old Game Data Hand-In": 187256296,
        "Roots - Star Eater Fed": 187256297,
    },
    "next_item_id": 187256164,
    "next_location_id": 187256336,
    "campaign_location_count": 88,
    "new_campaign_location_count": 81,
    "new_campaign_location_start": 187256211,
    "new_campaign_location_end": 187256291,
    "active_location_totals": {
        "Normal": 164,
        "Hard": 222,
        "Expert": 280,
        "Perfection": 316,
    },
}


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def repo_path(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


for path in (
    WORLD,
    ITEMS,
    POINTS,
    CAMPAIGN,
    EXPANDED,
    EXPANDED_CLIENT,
    EXPANDED_POLICY,
    META,
    CLIENT,
    CLIENT_BUILD,
    CASSETTE_POLICY,
    IDS,
    OVERVIEW,
    TEST_SUPPORT,
):
    if not path.exists():
        fail(f"missing required file: {repo_path(path)}")

world_text = WORLD.read_text(encoding="utf-8")
client_text = CLIENT.read_text(encoding="utf-8")
bucket_runtime_text = (ROOT / "client/RootsBucketRandomization.cs").read_text(encoding="utf-8")
client_build_text = CLIENT_BUILD.read_text(encoding="utf-8")
cassette_policy_text = CASSETTE_POLICY.read_text(encoding="utf-8")
ids_text = IDS.read_text(encoding="utf-8")
overview_text = OVERVIEW.read_text(encoding="utf-8")
items_text = ITEMS.read_text(encoding="utf-8")
points_text = POINTS.read_text(encoding="utf-8")


def load_json(path: Path, label: str):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        fail(f"invalid {repo_path(path)} {label}: {exc}")


# Syntax-only validation does not require Archipelago to be installed.
for python_source in sorted(WORLD_DIR.glob("*.py")):
    try:
        ast.parse(python_source.read_text(encoding="utf-8"), filename=str(python_source))
    except SyntaxError as exc:
        fail(f"APWorld Python syntax error in {repo_path(python_source)}: {exc}")


campaign_namespace = runpy.run_path(str(CAMPAIGN))
campaign_location_names = tuple(campaign_namespace["CAMPAIGN_LOCATION_NAMES"])
campaign_location_name_to_id = dict(campaign_namespace["CAMPAIGN_LOCATION_NAME_TO_ID"])
new_campaign_location_names = tuple(campaign_namespace["NEW_CAMPAIGN_LOCATION_NAMES"])
new_campaign_location_name_to_id = dict(campaign_namespace["NEW_CAMPAIGN_LOCATION_NAME_TO_ID"])

if len(campaign_namespace["CAMPAIGN_LEVELS"]) != 22:
    fail("campaign level count changed: expected 22")
if len(campaign_location_names) != EXPECTED["campaign_location_count"]:
    fail("campaign location name count changed")
if len(campaign_location_name_to_id) != EXPECTED["campaign_location_count"]:
    fail("campaign location ID count changed")
if set(campaign_location_names) != set(campaign_location_name_to_id):
    fail("campaign location names and IDs differ")
if len(new_campaign_location_names) != EXPECTED["new_campaign_location_count"]:
    fail("new campaign location count changed")
if set(new_campaign_location_names) != set(new_campaign_location_name_to_id):
    fail("new campaign location IDs do not match the new-name block")
expected_new_campaign_ids = list(
    range(
        EXPECTED["new_campaign_location_start"],
        EXPECTED["new_campaign_location_end"] + 1,
    )
)
if list(new_campaign_location_name_to_id.values()) != expected_new_campaign_ids:
    fail("new campaign location ID range changed")
if len(set(campaign_location_name_to_id.values())) != EXPECTED["campaign_location_count"]:
    fail("campaign location IDs are not unique")

# Compare both production catalogs, including native identities, independently
# of live-world construction so static mutation checks cannot skip this contract.
expanded_entries = tuple(runpy.run_path(str(EXPANDED))["EXPANDED_CHECKS"])
expanded_locations = {entry.name: entry.location_id for entry in expanded_entries}
if len(expanded_entries) != 39 or len(expanded_locations) != 39:
    fail("expanded check catalog must contain 39 unique names")
if {entry.location_id for entry in expanded_entries} != {187256292, *range(187256298, 187256336)}:
    fail("expanded permanent location ID range changed")
if expanded_locations.get("Lobby - Important Letters Pickup") != 187256292:
    fail("expanded Letters source must retain reserved ID 187256292")
expanded_client_text = EXPANDED_CLIENT.read_text(encoding="utf-8")
client_rows = re.findall(r'^\s*new\((\d+,\s*"[^"\n]*".*)\),\s*$', expanded_client_text, re.MULTILINE)
try:
    client_entries = []
    for row in client_rows:
        values = json.loads("[" + row.replace(', Character: "KING"', "") + "]")
        if len(values) == 4:
            values.extend(("", ""))
        client_entries.append(tuple(values))
except (ValueError, TypeError):
    fail("expanded client catalog is malformed")
expected_entries = [(e.location_id, e.name, e.room, e.flag, e.level, e.variant) for e in expanded_entries]
if client_entries != expected_entries:
    fail("expanded C# and Python native source catalogs differ")
combo_requirements = ("Weed Killer", "Plant Pipes", "Hip Glasses", "Chicken Bucket", "Bucket Minion Trade Complete")
for entry in expanded_entries:
    combo = entry.name == "Roots - Combo Bucket Conversion"
    if entry.progression_safe != combo or entry.required_items != (combo_requirements if combo else ()):
        fail("expanded conservative progression requirements changed")
if len([e for e in expanded_entries if e.variant]) != 6:
    fail("expanded special completion count changed")
for marker in (
    '"expanded_checks_schema": 1',
    '"expanded_check_locations": dict(EXPANDED_CHECK_LOCATION_NAME_TO_ID)',
    '"expanded_special_completions_active": True',
    '"expanded_check_native_rewards_preserved": True',
):
    if marker not in world_text:
        fail(f"expanded slot contract marker missing: {marker}")
for marker in (
    "ExpandedChecksPolicy.Validate(loginSuccess.SlotData)",
    "expandedMode == ExpandedChecksMode.Invalid",
    "ExpandedChecks.Configure(generation,",
    "ExpandedChecks.Reset();",
    "ExpandedChecks.CaptureResultContext()",
    "ExpandedChecks.ObservePersistedResult(level, variant, result?.ExpandedContext);",
    "ExpandedChecks.Tick(elapsed);",
    "ExpandedChecks.BeforeProgressionRequest(req, flag);",
    "ExpandedChecks.Observe(evt, flag);",
):
    if marker not in client_text:
        fail(f"expanded client integration missing: {marker}")
expanded_policy_text = EXPANDED_POLICY.read_text(encoding="utf-8")
for marker in (
    'version?.EndsWith(VersionSuffix + (stars ? "-ap-stars-0.28" : ""), StringComparison.Ordinal) != true',
    '(long?)root["schema_version"] != (stars ? 19 : 18)',
    '(long?)root["expanded_checks_schema"] != 1',
    'map.Count != entries.Count',
    'map[e.Name]?.Type != JTokenType.Integer',
    '(long?)map[e.Name] != e.Id',
):
    if marker not in expanded_policy_text:
        fail(f"expanded strict client contract missing: {marker}")

for number in range(1, 11):
    marker = f'"Development Cache {number:02d}": BASE_ID + {number + 10}'
    if marker not in world_text:
        fail("Development Cache ID reservation changed")

for marker in (
    'and not name.startswith("Development Cache ")',
    '"development_cache_count": 0',
    '"development_cache_ids_reserved": True',
    '"development_caches_filler_only": False',
):
    if marker not in world_text:
        fail("Development Caches are no longer reserved-only")


support_spec = spec_from_file_location("scrc_validator_support", TEST_SUPPORT)
if support_spec is None or support_spec.loader is None:
    fail("could not load APWorld validation support")
support = module_from_spec(support_spec)
support_spec.loader.exec_module(support)


def validate_live_world_structure() -> dict[str, int]:
    """Build the real world at each supported difficulty without AP installed."""
    campaign_tiers_by_difficulty = (
        frozenset(("Completion", "1 Star")),
        frozenset(("Completion", "1 Star", "2 Stars")),
        frozenset(("Completion", "1 Star", "2 Stars", "3 Stars")),
        frozenset(("Completion", "1 Star", "2 Stars", "3 Stars")),
    )
    world_module, cleanup = support.load_scrc_world()
    try:
        totals: dict[str, int] = {}
        for difficulty, (label, expected_count) in enumerate(
            EXPECTED["active_location_totals"].items()
        ):
            world = object.__new__(world_module.SCRCWorld)
            world.player = 1
            world.random = random.Random(22)
            world.multiworld = support.FakeMultiWorld()
            world.options = types.SimpleNamespace(
                required_stars=support.OptionValue(50),
                difficulty=support.OptionValue(difficulty),
                starting_area=support.OptionValue(0),
            )
            world.generate_early()
            world.create_regions()
            instantiated_locations = [
                location
                for region in world.multiworld.regions
                for location in region.locations
                if location.player == world.player
            ]
            cache_name = next(
                (
                    location.name
                    for location in instantiated_locations
                    if location.name.startswith("Development Cache")
                ),
                None,
            )
            if cache_name is not None:
                fail(f"instantiated Development Cache: {cache_name}")
            addressed_locations = [
                location
                for location in instantiated_locations
                if location.address is not None
            ]
            actual_count = len(addressed_locations)
            if actual_count != expected_count:
                fail(
                    f"live active-location count changed for {label}: "
                    f"expected {expected_count}, got {actual_count}"
                )
            addressed_names = {location.name for location in addressed_locations}
            expected_campaign_names = {
                name
                for name in campaign_location_names
                if name.rsplit(" - ", 1)[-1] in campaign_tiers_by_difficulty[difficulty]
            }
            if addressed_names.intersection(campaign_location_names) != expected_campaign_names:
                fail(f"live campaign location names changed for {label}")
            world.create_items()
            item_names = [item.name for item in world.multiworld.itempool]
            for item_name in ("Old Game Data", "Car Battery"):
                if item_names.count(item_name) != 1:
                    fail(f"character quest item pool count changed: {item_name}")
                if world.create_item(item_name).classification != "progression":
                    fail(f"character quest item classification changed: {item_name}")
            if len(item_names) != expected_count:
                fail(f"live item pool capacity changed for {label}")
            character_data = world.fill_slot_data()
            if character_data.get("character_quest_item_schema") != 1 or character_data.get("randomize_character_quest_items") is not True:
                fail("character quest item slot contract is inactive or malformed")
            for field in ("quest_checks_schema", "quest_items", "quest_locations"):
                if character_data.get(field) != EXPECTED[field]:
                    fail(f"quest slot contract changed: {field}")
            for name, item_id in EXPECTED["quest_items"].items():
                item = world.create_item(name)
                if item_names.count(name) != 1 or item.code != item_id or item.classification != ("progression" if name == "Plunger" else "useful"):
                    fail(f"quest item pool or classification changed: {name}")
            for name, location_id in EXPECTED["quest_locations"].items():
                matches = [location for location in addressed_locations if location.name == name]
                if len(matches) != 1 or matches[0].address != location_id:
                    fail(f"quest location registry changed: {name}")
            for name in ("Lobby - Plunger Pickup", "Roots - Star Eater Fed"):
                source = world.multiworld.get_location(name, world.player)
                for item in world.multiworld.itempool:
                    if not source.item_rule(item):
                        fail(f"quest modeled-source placement changed: {name}")
            if sum(name != "Stardust" for name in item_names) != 137:
                fail("expanded batch changed the 137-instance non-filler pool")
            if character_data.get("expanded_checks_schema") != 1 or character_data.get("expanded_check_locations") != expanded_locations:
                fail("expanded live slot contract changed")
            if "Lobby - Bean Trumpet Award" in addressed_names:
                fail("expanded batch activated reserved duplicate Bean source")
            for entry in expanded_entries:
                source = world.multiworld.get_location(entry.name, world.player)
                if source.address != entry.location_id:
                    fail(f"expanded source ID changed: {entry.name}")
                if entry.name not in world_module.SAFE_SOURCES:
                    for item in world.multiworld.itempool:
                        if source.item_rule(item) != (item.name == "Stardust"):
                            fail(f"expanded filler-only placement changed: {entry.name}")
            safe_capacity = sum(location.item_rule(world.create_item("Star")) for location in addressed_locations)
            if safe_capacity != (152, 209, 266, 302)[difficulty]:
                fail(f"expanded progression-eligible capacity changed for {label}")
            if item_names.count("Star") != 66:
                fail("Star pool must contain exactly66 individual items")
            star_data = world.fill_slot_data()
            gates = star_data["generated_star_requirements"]
            if len(gates) != 22 or gates.get("Level 1") != 0 or gates.get("Level 2") != 0:
                fail("Star gates have an invalid campaign/opening map")
            if not all(0 <= gate < 50 and gate <= 25 for gate in gates.values()):
                fail("Star gates exceed the configured goal or capacity ceiling")
            if set(star_data["star_eater_requirements"]) != {"Roots", "Lobby", "Cell Tower", "Royal Corridor", "Secret Bunker"}:
                fail("Star Eater contract changed")
            totals[label] = actual_count
        return totals
    finally:
        cleanup()


def assignment_value(tree: ast.AST, name: str) -> ast.AST:
    for node in tree.body:
        if isinstance(node, ast.Assign) and any(
            isinstance(target, ast.Name) and target.id == name
            for target in node.targets
        ):
            return node.value
    fail(f"could not locate Music Lab Point {name} assignment")


def static_int(node: ast.AST) -> int:
    if isinstance(node, ast.Constant) and isinstance(node.value, int) and not isinstance(node.value, bool):
        return node.value
    if isinstance(node, ast.Name) and node.id == "BASE_ID":
        return 187256000
    if isinstance(node, ast.BinOp) and isinstance(node.op, ast.Add):
        return static_int(node.left) + static_int(node.right)
    fail("could not parse Music Lab Point integer contract value")


# Check the assignments before using the fixed base to evaluate ID expressions.
# Otherwise a changed module base could silently pass every offset check below.
for base_path, base_text in ((POINTS, points_text), (ITEMS, items_text), (WORLD, world_text)):
    base_node = assignment_value(ast.parse(base_text), "BASE_ID")
    if not (
        isinstance(base_node, ast.Constant)
        and type(base_node.value) is int
        and base_node.value == 187256000
    ):
        fail(f"BASE_ID changed in {repo_path(base_path)}: expected 187256000")


def is_catalog_value_sum(node: ast.AST) -> bool:
    return (
        isinstance(node, ast.Call)
        and isinstance(node.func, ast.Name)
        and node.func.id == "sum"
        and len(node.args) == 1
        and isinstance(node.args[0], ast.GeneratorExp)
        and len(node.args[0].generators) == 1
        and not node.args[0].generators[0].ifs
        and node.args[0].generators[0].is_async == 0
        and isinstance(node.args[0].elt, ast.BinOp)
        and isinstance(node.args[0].elt.op, ast.Mult)
        and isinstance(node.args[0].elt.left, ast.Attribute)
        and isinstance(node.args[0].elt.right, ast.Attribute)
        and isinstance(node.args[0].elt.left.value, ast.Name)
        and isinstance(node.args[0].elt.right.value, ast.Name)
        and node.args[0].elt.left.value.id == node.args[0].elt.right.value.id
        and node.args[0].elt.left.attr == "value"
        and node.args[0].elt.right.attr == "count"
        and isinstance(node.args[0].generators[0].target, ast.Name)
        and node.args[0].generators[0].target.id == node.args[0].elt.left.value.id
        and isinstance(node.args[0].generators[0].iter, ast.Name)
        and node.args[0].generators[0].iter.id == "MUSIC_LAB_POINT_ITEMS"
    )


def is_sum_generator(node: ast.AST) -> bool:
    return (
        isinstance(node, ast.Call)
        and isinstance(node.func, ast.Name)
        and node.func.id == "sum"
        and len(node.args) == 1
        and isinstance(node.args[0], ast.GeneratorExp)
    )


def exported_point_total(name: str, expected: int) -> int:
    node = assignment_value(points_tree, name)
    if (
        isinstance(node, ast.Call)
        and isinstance(node.func, ast.Name)
        and node.func.id == "len"
        and len(node.args) == 1
        and isinstance(node.args[0], ast.Name)
        and node.args[0].id == "MUSIC_LAB_POINT_POOL"
    ):
        actual = sum(count for _, _, _, count in point_items)
    elif is_catalog_value_sum(node):
        actual = sum(value * count for _, _, value, count in point_items)
    elif is_sum_generator(node):
        fail(
            "Music Lab Point exported "
            f"{name.lower().replace('music_lab_point_', '').replace('_', ' ')} "
            "expression changed"
        )
    else:
        actual = static_int(node)
    if actual != expected:
        fail(f"Music Lab Point exported {name.lower().replace('music_lab_point_', '').replace('_', ' ')} changed")
    return actual


points_tree = ast.parse(points_text, filename=str(POINTS))
point_items_node = assignment_value(points_tree, "MUSIC_LAB_POINT_ITEMS")
if not isinstance(point_items_node, (ast.Tuple, ast.List)):
    fail("could not parse Music Lab Point item catalog")

point_items = []
for entry in point_items_node.elts:
    if not (
        isinstance(entry, ast.Call)
        and isinstance(entry.func, ast.Name)
        and entry.func.id == "MusicLabPointItem"
        and len(entry.args) == 4
        and isinstance(entry.args[0], ast.Constant)
        and isinstance(entry.args[0].value, str)
    ):
        fail("could not parse Music Lab Point item catalog entry")
    point_items.append((
        entry.args[0].value,
        static_int(entry.args[1]),
        static_int(entry.args[2]),
        static_int(entry.args[3]),
    ))
point_items = tuple(point_items)
expected_point_items = EXPECTED["music_lab_point_items"]
if tuple(entry[0] for entry in point_items) != tuple(entry[0] for entry in expected_point_items):
    fail("Music Lab Point item names changed")
if tuple(entry[1] for entry in point_items) != tuple(entry[1] for entry in expected_point_items):
    fail("Music Lab Point ID changed")
if tuple(entry[2] for entry in point_items) != tuple(entry[2] for entry in expected_point_items):
    fail("Music Lab Point value changed")
if tuple(entry[3] for entry in point_items) != tuple(entry[3] for entry in expected_point_items):
    fail("Music Lab Point count changed")

point_total_node = assignment_value(points_tree, "_EXPECTED_MUSIC_LAB_POINT_TOTAL_VALUE")
if static_int(point_total_node) != EXPECTED["music_lab_point_total_value"]:
    fail("Music Lab Point total changed")
if sum(value * count for _, _, value, count in point_items) != EXPECTED["music_lab_point_total_value"]:
    fail("Music Lab Point total changed")

point_schema_node = assignment_value(points_tree, "MUSIC_LAB_POINT_SCHEMA")
if static_int(point_schema_node) != EXPECTED["music_lab_point_schema"]:
    fail("Music Lab Point schema changed")

thresholds_node = assignment_value(points_tree, "MUSIC_LAB_POINT_THRESHOLDS")
if not isinstance(thresholds_node, ast.Dict):
    fail("could not parse Music Lab Point thresholds")
if any(not isinstance(value, ast.Constant) or not isinstance(value.value, str) for value in thresholds_node.values):
    fail("could not parse Music Lab Point threshold locations")
point_thresholds = tuple(
    (static_int(key), value.value)
    for key, value in zip(thresholds_node.keys, thresholds_node.values)
)
expected_point_thresholds = EXPECTED["music_lab_point_thresholds"]
if tuple(threshold for threshold, _ in point_thresholds) != tuple(
    threshold for threshold, _ in expected_point_thresholds
):
    fail("Music Lab Point thresholds changed")
if point_thresholds != expected_point_thresholds:
    fail("Music Lab Point threshold locations changed")

point_total_instances = exported_point_total(
    "MUSIC_LAB_POINT_TOTAL_INSTANCES",
    EXPECTED["music_lab_point_total_instances"],
)
point_total_value = exported_point_total(
    "MUSIC_LAB_POINT_TOTAL_VALUE",
    EXPECTED["music_lab_point_total_value"],
)
point_max_effective = exported_point_total(
    "MUSIC_LAB_POINT_MAX_EFFECTIVE",
    EXPECTED["music_lab_point_max_effective"],
)

try:
    metadata = json.loads(META.read_text(encoding="utf-8"))
except Exception as exc:
    fail(f"invalid archipelago.json: {exc}")

if metadata.get("world_version") != EXPECTED["world_version"]:
    fail(f"expected APWorld version {EXPECTED['world_version']}, got {metadata.get('world_version')!r}")

if f'PluginVersion = "{EXPECTED["client_version"]}"' not in client_text:
    fail(f"client PluginVersion is not {EXPECTED['client_version']}")
if f"RhythmCastleAP v{EXPECTED['client_version']}" not in client_build_text:
    fail(f"client build script version is not {EXPECTED['client_version']}")

for label, expected_id, pattern in (
    ("Weed Killer", EXPECTED["weed_killer_item_id"], r'"Weed Killer"\s*:\s*BASE_ID\s*\+\s*(\d+)'),
    ("Plant Pipes", EXPECTED["plant_pipes_item_id"], r'"Plant Pipes"\s*:\s*BASE_ID\s*\+\s*(\d+)'),
    ("Gecko location", EXPECTED["gecko_location_id"], r'LOCATION_NAME_TO_ID\[ROOTS_GECKO_WEED_KILLER\]\s*=\s*BASE_ID\s*\+\s*(\d+)'),
    ("Frog/Hippo location", EXPECTED["frog_hippo_location_id"], r'LOCATION_NAME_TO_ID\[ROOTS_LEVEL3_FROG_HIPPO\]\s*=\s*BASE_ID\s*\+\s*(\d+)'),
    ("Hip Glasses item", EXPECTED["hip_glasses_item_id"], r'"Hip Glasses"\s*:\s*BASE_ID\s*\+\s*(\d+)'),
    ("Chicken Bucket item", EXPECTED["chicken_bucket_item_id"], r'"Chicken Bucket"\s*:\s*BASE_ID\s*\+\s*(\d+)'),
    ("Level 4 source", EXPECTED["hip_glasses_location_id"], r'LOCATION_NAME_TO_ID\[ROOTS_LEVEL4_HIP_GLASSES\]\s*=\s*BASE_ID\s*\+\s*(\d+)'),
    ("Bucket trade", EXPECTED["bucket_trade_location_id"], r'LOCATION_NAME_TO_ID\[ROOTS_BUCKET_MINION_TRADE\]\s*=\s*BASE_ID\s*\+\s*(\d+)'),
):
    match = re.search(pattern, world_text)
    if not match:
        fail(f"could not locate {label} ID assignment")
    absolute = 187256000 + int(match.group(1))
    if absolute != expected_id:
        fail(f"{label} ID changed: expected {expected_id}, got {absolute}")

star_match = re.search(r'STAR_ITEM_NAME\s*:\s*BASE_ID\s*\+\s*(\d+)', items_text)
if not star_match:
    fail("could not locate Star ID assignment")
star_id = 187256000 + int(star_match.group(1))
if star_id != EXPECTED["star_item_id"]:
    fail(f"Star ID changed: expected {EXPECTED['star_item_id']}, got {star_id}")

for label, expected_id, symbol in (
    ("Hypno Pan", EXPECTED["hypno_pan_item_id"], "HYPNO_PAN_ITEM_NAME"),
    ("Violance", EXPECTED["violance_item_id"], "VIOLANCE_ITEM_NAME"),
    ("Money Cassette", EXPECTED["money_cassette_item_id"], "MONEY_CASSETTE_ITEM_NAME"),
):
    match = re.search(rf'{symbol}\s*:\s*BASE_ID\s*\+\s*(\d+)', items_text)
    if not match:
        fail(f"could not locate {label} preview item ID assignment")
    absolute = 187256000 + int(match.group(1))
    if absolute != expected_id:
        fail(f"{label} item ID changed: expected {expected_id}, got {absolute}")

for label, expected_id in (
    ("Important Letters", 187256156),
    ("Bean Trumpet", 187256157),
    ("Demolition Certificate", 187256158),
    ("Old Game Data", 187256159),
    ("Car Battery", 187256160),
):
    match = re.search(rf'"{label}"\s*:\s*BASE_ID\s*\+\s*(\d+)', items_text)
    if not match or 187256000 + int(match.group(1)) != expected_id:
        fail(f"{label} reserved item ID is missing or changed")

for symbol, expected_id in (
    ("LOBBY_LETTERS_PICKUP", 187256292),
    ("LOBBY_BEAN_TRUMPET_AWARD", 187256293),
):
    match = re.search(rf'LOCATION_NAME_TO_ID\[{symbol}\]\s*=\s*BASE_ID\s*\+\s*(\d+)', world_text)
    if not match or 187256000 + int(match.group(1)) != expected_id:
        fail(f"{symbol} reserved location ID is missing or changed")

# Validate permanent new quest IDs independently, even in static-only mode.
for source_text, symbol, expected in (
    (items_text, "QUEST_ITEM_NAME_TO_ID", EXPECTED["quest_items"]),
    (world_text, "QUEST_LOCATION_NAME_TO_ID", EXPECTED["quest_locations"]),
):
    node = assignment_value(ast.parse(source_text), symbol)
    if not isinstance(node, ast.Dict):
        fail(f"quest ID contract is not a dictionary: {symbol}")
    actual = {ast.literal_eval(key): static_int(value) for key, value in zip(node.keys, node.values)}
    if actual != expected or len(node.keys) != len(expected):
        fail(f"quest ID contract changed: {symbol}")

# Retain original native bag flags and chest sources independently of new rewards.
for symbol, expected in (
    ("CHARACTER_QUEST_ITEMS", {
        "Old Game Data": "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM",
        "Car Battery": "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM",
    }),
    ("CHARACTER_QUEST_ITEM_LOCATIONS", {
        "Old Game Data": "Music Lab - 5 Point Chest",
        "Car Battery": "Music Lab - 20 Point Chest",
    }),
):
    if ast.literal_eval(assignment_value(ast.parse(items_text), symbol)) != expected:
        fail(f"character quest item contract changed: {symbol}")

money_cassette_location_match = re.search(
    r'LOCATION_NAME_TO_ID\[LEVEL_2_MONEY_CASSETTE_SOURCE\]\s*=\s*BASE_ID\s*\+\s*(\d+)',
    world_text,
)
if not money_cassette_location_match:
    fail("could not locate Money Cassette source ID assignment")
money_cassette_location_id = 187256000 + int(money_cassette_location_match.group(1))
if money_cassette_location_id != EXPECTED["money_cassette_location_id"]:
    fail(
        "Money Cassette source ID changed: expected "
        f"{EXPECTED['money_cassette_location_id']}, got {money_cassette_location_id}"
    )

if f'"implementation_version": "{EXPECTED["implementation_version"]}"' not in world_text:
    fail("implementation_version changed without updating validator/baseline docs")
if '"schema_version": 19' not in world_text:
    fail("full-level-mapping slot-data schema changed")
if '"campaign_level_mapping_schema": 1' not in world_text:
    fail("campaign level mapping schema changed")
if f'"generation_foundation_version": "{EXPECTED["generation_foundation_version"]}"' not in world_text:
    fail("generation_foundation_version changed without updating validator/baseline docs")

for marker in ('"star_victory_schema": 1', '"star_items_active": True',
               '"client_star_gate_enforcement_active": True',
               '"post_threshold_victory_active": True',
               '"victory_level_name": "Level 22"',
               '"victory_level_internal_id": "Level_28"',
               '"development_area_access_victory_active": False'):
    if marker not in world_text:
        fail(f"Star contract marker missing: {marker}")

for required_marker in (
    '"quest_checks_schema": 1',
    '"quest_items": dict(QUEST_ITEM_NAME_TO_ID)',
    '"quest_locations": dict(QUEST_LOCATION_NAME_TO_ID)',
    '"character_quest_item_schema": 1',
    '"randomize_character_quest_items": True',
    '"character_quest_items": dict(CHARACTER_QUEST_ITEMS)',
    '"character_quest_item_locations": dict(CHARACTER_QUEST_ITEM_LOCATIONS)',
    '"star_items_active": True',
    '"client_star_gate_enforcement_active": True',
    '"special_variant_locations_active": False',
    '"special_variant_locations": []',
    '"difficulty_filtering_active": True',
    '"cassette_schema": 1',
    '"full_cassette_randomization": True',
    '"cassette_count": len(CASSETTES)',
    '"music_lab_points_enabled": True',
    '"music_lab_points_schema": MUSIC_LAB_POINT_SCHEMA',
    '"music_lab_point_items": {',
    '"music_lab_point_values": {',
    '"music_lab_point_counts": {',
    '"music_lab_point_total_instances": MUSIC_LAB_POINT_TOTAL_INSTANCES',
    '"music_lab_point_total_value": MUSIC_LAB_POINT_TOTAL_VALUE',
    '"music_lab_point_max_effective": MUSIC_LAB_POINT_MAX_EFFECTIVE',
    '"music_lab_point_thresholds": dict(MUSIC_LAB_POINT_THRESHOLDS)',
):
    if required_marker not in world_text:
        fail(f"missing required slot-data marker: {required_marker}")

for label, marker in (
    ("feature flag", '"randomize_hip_glasses_chicken_bucket": True'),
    ("Hip Glasses slot-data item", '"hip_glasses_item": HIP_GLASSES_ITEM'),
    ("Chicken Bucket slot-data item", '"chicken_bucket_item": CHICKEN_BUCKET_ITEM'),
    ("Level 4 slot-data source", '"hip_glasses_source_location": ROOTS_LEVEL4_HIP_GLASSES'),
    ("Bucket trade slot-data source", '"bucket_minion_trade_location": ROOTS_BUCKET_MINION_TRADE'),
    ("cassette item mapping", '"cassette_items": {'),
    ("cassette source mapping", '"cassette_sources": {'),
    ("cassette reused-location mapping", '"cassette_reused_locations": {'),
):
    if marker not in world_text:
        fail(f"{label} contract is missing or changed")

for label, marker in (
    ("Hip Glasses native marker", 'internal const string HipGlassesNativeFlag = "HIP_GLASSES_BAG_ITEM";'),
    ("Level 4 source marker", 'internal const string HipGlassesSourceFlag = "LEVEL_08_GLASSES_COLLECTED";'),
    ("Bucket trade marker", 'internal const string BucketTradeFlag = "ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES";'),
    ("Chicken Bucket native marker", 'internal const string ChickenBucketNativeFlag = "CHICKEN_BUCKET_BAG_ITEM";'),
    ("native consumed marker", 'internal const string ChickenConsumedFlag = "LEVEL_09_COMBO_ABILITY_EARNED";'),
):
    if marker not in bucket_runtime_text:
        fail(f"{label} contract is missing or changed")

for label, marker in (
    ("cassette schema compatibility", "internal const int Schema = 1;"),
    ("cassette count compatibility", "internal const int Count = 30;"),
):
    if marker not in cassette_policy_text:
        fail(f"{label} contract is missing or changed")

for required in (
    "ROOTS_HUB_INTRO_WITNESSED",
    "WEED_KILLER_BAG_ITEM",
    "ROOTS_HUB_WEED_KILLER_COLLECTED",
    "WEED_KILLER_ABILITY",
    "LEVEL_07_WK_ABILITY_EARNED",
):
    if required not in client_text:
        fail(f"client mapping missing required native flag: {required}")

for expected in (str(EXPECTED["next_item_id"]), str(EXPECTED["next_location_id"]), "+4..+10"):
    if expected not in ids_text:
        fail(f"ID registry is missing expected marker: {expected}")

live_world_validation_enabled = os.environ.get("SCRC_VALIDATE_LIVE_WORLD", "1") != "0"
live_active_location_totals = (
    validate_live_world_structure()
    if live_world_validation_enabled
    else dict(EXPECTED["active_location_totals"])
)

expected_totals = EXPECTED["active_location_totals"]
if (
    f"Normal {expected_totals['Normal']} / Hard {expected_totals['Hard']} / Expert {expected_totals['Expert']} / Perfection {expected_totals['Perfection']}" not in overview_text
):
    fail("active location totals changed")

print("Repository validation passed.")
print(f"Client:  v{EXPECTED['client_version']}")
print(f"APWorld: v{EXPECTED['world_version']} ({EXPECTED['implementation_version']})")
print(json.dumps({
    "world_version": EXPECTED["world_version"],
    "implementation_version": EXPECTED["implementation_version"],
    "generation_foundation_version": EXPECTED["generation_foundation_version"],
    "star_item_id": EXPECTED["star_item_id"],
    "hip_glasses_item_id": EXPECTED["hip_glasses_item_id"],
    "chicken_bucket_item_id": EXPECTED["chicken_bucket_item_id"],
    "hypno_pan_item_id": EXPECTED["hypno_pan_item_id"],
    "violance_item_id": EXPECTED["violance_item_id"],
    "money_cassette_item_id": EXPECTED["money_cassette_item_id"],
    "hip_glasses_location_id": EXPECTED["hip_glasses_location_id"],
    "bucket_trade_location_id": EXPECTED["bucket_trade_location_id"],
    "money_cassette_location_id": EXPECTED["money_cassette_location_id"],
    "music_lab_point_schema": static_int(point_schema_node),
    "music_lab_point_item_ids": [
        item_id for _, item_id, _, _ in point_items
    ],
    "music_lab_point_values": [
        value for _, _, value, _ in point_items
    ],
    "music_lab_point_counts": [
        count for _, _, _, count in point_items
    ],
    "music_lab_point_total_instances": point_total_instances,
    "music_lab_point_total_value": point_total_value,
    "music_lab_point_max_effective": point_max_effective,
    "music_lab_point_thresholds": dict(point_thresholds),
    "campaign_location_count": len(campaign_location_names),
    "new_campaign_location_count": len(new_campaign_location_names),
    "new_campaign_location_id_range": [
        EXPECTED["new_campaign_location_start"],
        EXPECTED["new_campaign_location_end"],
    ],
    "campaign_level_mapping_schema": 1,
    "slot_data_schema": 19,
    "expanded_checks_schema": 1,
    "expanded_check_count": len(expanded_entries),
    "expanded_special_completion_count": 6,
    "quest_checks_schema": EXPECTED["quest_checks_schema"],
    "quest_items": EXPECTED["quest_items"],
    "quest_locations": EXPECTED["quest_locations"],
    "character_quest_item_schema": 1,
    "character_quest_item_ids": {"Old Game Data": 187256159, "Car Battery": 187256160},
    "character_quest_source_ids": {"Old Game Data": 187256169, "Car Battery": 187256171},
    "active_location_totals": live_active_location_totals,
    "live_world_structure_checked": live_world_validation_enabled,
    "star_items_active": True,
    "client_star_gate_enforcement_active": True,
    "special_variant_locations_active": False,
    "next_item_id": EXPECTED["next_item_id"],
    "next_location_id": EXPECTED["next_location_id"],
}, indent=2))
print("v0.28 AP Stars and post-threshold Level 22 Victory passed a Hard40 native playthrough; broader acceptance remains pending.")
print(f"Next safe item ID:     {EXPECTED['next_item_id']}")
print(f"Next safe location ID: {EXPECTED['next_location_id']}")
