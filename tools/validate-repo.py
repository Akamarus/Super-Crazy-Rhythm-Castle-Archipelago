#!/usr/bin/env python3
from __future__ import annotations

import ast
import json
import os
import re
import sys
from pathlib import Path

ROOT = Path(os.environ.get("SCRC_REPO_ROOT", Path(__file__).resolve().parents[1])).resolve()
WORLD = ROOT / "apworld" / "scrc" / "__init__.py"
WORLD_DIR = WORLD.parent
ITEMS = WORLD_DIR / "items.py"
META = ROOT / "apworld" / "scrc" / "archipelago.json"
CLIENT = ROOT / "client" / "Plugin.cs"
CASSETTE_POLICY = ROOT / "client" / "CassetteRandomizationPolicy.cs"
IDS = ROOT / "docs" / "IDS.md"
EXPECTED = {
    "client_version": "0.68.0",
    "world_version": "0.22.0",
    "implementation_version": (
        "area-routing-plant-pipes-0.15-generation-foundation-0.16-"
        "hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-"
        "consolidated-preview-0.19-difficulty-filtering-0.20-"
        "vanilla-vampire-garage-0.21-full-cassettes-0.22"
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
    "next_item_id": 187256153,
    "next_location_id": 187256211,
}


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


def repo_path(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


for path in (
    WORLD,
    ITEMS,
    META,
    CLIENT,
    CASSETTE_POLICY,
    IDS,
):
    if not path.exists():
        fail(f"missing required file: {repo_path(path)}")

world_text = WORLD.read_text(encoding="utf-8")
client_text = CLIENT.read_text(encoding="utf-8")
cassette_policy_text = CASSETTE_POLICY.read_text(encoding="utf-8")
ids_text = IDS.read_text(encoding="utf-8")
items_text = ITEMS.read_text(encoding="utf-8")


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

try:
    metadata = json.loads(META.read_text(encoding="utf-8"))
except Exception as exc:
    fail(f"invalid archipelago.json: {exc}")

if metadata.get("world_version") != EXPECTED["world_version"]:
    fail(f"expected APWorld version {EXPECTED['world_version']}, got {metadata.get('world_version')!r}")

if f'PluginVersion = "{EXPECTED["client_version"]}"' not in client_text:
    fail(f"client PluginVersion is not {EXPECTED['client_version']}")

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
if f'"generation_foundation_version": "{EXPECTED["generation_foundation_version"]}"' not in world_text:
    fail("generation_foundation_version changed without updating validator/baseline docs")

for required_marker in (
    '"star_items_active": False',
    '"difficulty_filtering_active": True',
    '"cassette_schema": 1',
    '"full_cassette_randomization": True',
    '"cassette_count": len(CASSETTES)',
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
    if marker not in client_text:
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
    "next_item_id": EXPECTED["next_item_id"],
    "next_location_id": EXPECTED["next_location_id"],
}, indent=2))
print("v0.22 full Music Lab cassette routing is active; physical vanilla Vampire Killer Garage entry and earlier repair contracts remain enforced.")
print(f"Next safe item ID:     {EXPECTED['next_item_id']}")
print(f"Next safe location ID: {EXPECTED['next_location_id']}")
