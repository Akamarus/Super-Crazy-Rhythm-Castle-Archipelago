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
IDS = ROOT / "docs" / "IDS.md"

EXPECTED = {
    "client_version": "0.67.59",
    "world_version": "0.16",
    "implementation_version": "generation-foundation-0.16",
    "weed_killer_item_id": 187256116,
    "plant_pipes_item_id": 187256117,
    "gecko_location_id": 187256178,
    "frog_hippo_location_id": 187256179,
    "star_item_id": 187256118,
    "next_item_id": 187256119,
    "next_location_id": 187256180,
}


def fail(message: str) -> None:
    print(f"ERROR: {message}", file=sys.stderr)
    raise SystemExit(1)


for path in (WORLD, ITEMS, META, CLIENT, IDS):
    if not path.exists():
        fail(f"missing required file: {path.relative_to(ROOT)}")

world_text = WORLD.read_text(encoding="utf-8")
client_text = CLIENT.read_text(encoding="utf-8")
ids_text = IDS.read_text(encoding="utf-8")
items_text = ITEMS.read_text(encoding="utf-8")

# Syntax-only validation does not require Archipelago to be installed.
for python_source in sorted(WORLD_DIR.glob("*.py")):
    try:
        ast.parse(python_source.read_text(encoding="utf-8"), filename=str(python_source))
    except SyntaxError as exc:
        fail(f"APWorld Python syntax error in {python_source.relative_to(ROOT)}: {exc}")

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

if f'"implementation_version": "{EXPECTED["implementation_version"]}"' not in world_text:
    fail("implementation_version changed without updating validator/baseline docs")

for inactive_marker in (
    '"star_items_active": False',
    '"difficulty_filtering_active": False',
):
    if inactive_marker not in world_text:
        fail(f"missing inactive preview marker: {inactive_marker}")

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
    "star_item_id": EXPECTED["star_item_id"],
    "next_item_id": EXPECTED["next_item_id"],
}, indent=2))
print("v0.16 generation foundations are previews; live generation remains on the Area Access milestone.")
print(f"Next safe item ID:     {EXPECTED['next_item_id']}")
print(f"Next safe location ID: {EXPECTED['next_location_id']}")
