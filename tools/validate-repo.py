#!/usr/bin/env python3
from __future__ import annotations

import ast
import hashlib
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
LOCAL_AI_DECISIONS = ROOT / "tools" / "local-ai" / "evaluation" / "decisions.json"
LOCAL_AI_CASES = ROOT / "tools" / "local-ai" / "evaluation" / "cases.json"
LOCAL_AI_CONFIG = ROOT / "tools" / "local-ai" / "Private" / "Config.ps1"
LOCAL_AI_EVALUATION_COMMAND = (
    ROOT / "tools" / "local-ai" / "Public" / "Invoke-LocalAiModelEvaluation.ps1"
)
LOCAL_AI_MANIFEST = ROOT / "tools" / "local-ai" / "LocalAiBridge.psd1"

LOCAL_AI_ALLOWED_MODELS = ("jacks-assistant", "jacks-assistant-fast")
LOCAL_AI_DECISION_SCHEMA = 1
LOCAL_AI_EVALUATION_SCHEMA = 1
LOCAL_AI_MANIFEST_WITH_EVALUATION_EXPORT_SHA256 = (
    "b25a0d429acc082218a71b06ad9f9db027b1781eb0ce9b9fd2271ae39a26ba2c"
)
REQUIRED_LOCAL_AI_DECISION_IDS = {
    "area-access-vs-level-access",
    "location-vs-item",
    "hip-glasses-chain",
    "historical-vs-current-design",
    "native-flag-confidence",
}
REQUIRED_LOCAL_AI_CASE_QUESTIONS = {
    "access_design": {"roots_access_required", "level_5_access_required"},
    "location_item_mapping": {"ap_location_grants_themed_item"},
    "hip_glasses_chain": {
        "hip_glasses_source_kind",
        "hip_glasses_item_kind",
        "bucket_minion_trade_source_kind",
        "chicken_bucket_item_kind",
        "combo_bucket_kind",
    },
    "historical_evidence": {
        "historical_gameplay_is_evidence",
        "pre_pivot_level_access_is_current_requirement",
    },
    "native_flag_confidence": {"unknown_native_flags_may_be_invented"},
}

EXPECTED = {
    "client_version": "0.67.64",
    "world_version": "0.19",
    "implementation_version": "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19",
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
    "hip_glasses_location_id": 187256180,
    "bucket_trade_location_id": 187256181,
    "next_item_id": 187256123,
    "next_location_id": 187256186,
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
    IDS,
    LOCAL_AI_DECISIONS,
    LOCAL_AI_CASES,
    LOCAL_AI_CONFIG,
    LOCAL_AI_EVALUATION_COMMAND,
    LOCAL_AI_MANIFEST,
):
    if not path.exists():
        fail(f"missing required file: {repo_path(path)}")

world_text = WORLD.read_text(encoding="utf-8")
client_text = CLIENT.read_text(encoding="utf-8")
ids_text = IDS.read_text(encoding="utf-8")
items_text = ITEMS.read_text(encoding="utf-8")
local_ai_config_text = LOCAL_AI_CONFIG.read_text(encoding="utf-8")
local_ai_manifest_text = LOCAL_AI_MANIFEST.read_text(encoding="utf-8")


def load_json(path: Path, label: str):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        fail(f"invalid {repo_path(path)} {label}: {exc}")


local_ai_decisions = load_json(LOCAL_AI_DECISIONS, "decision ledger")
local_ai_cases = load_json(LOCAL_AI_CASES, "evaluation cases")

if local_ai_decisions.get("schema_version") != LOCAL_AI_DECISION_SCHEMA:
    fail(
        f"{repo_path(LOCAL_AI_DECISIONS)} schema_version must be "
        f"{LOCAL_AI_DECISION_SCHEMA}"
    )
decision_entries = local_ai_decisions.get("decisions")
if not isinstance(decision_entries, list):
    fail(f"{repo_path(LOCAL_AI_DECISIONS)} decisions must be an array")
decision_ids = [entry.get("id") for entry in decision_entries if isinstance(entry, dict)]
if len(decision_ids) != len(decision_entries) or len(set(decision_ids)) != len(decision_ids):
    fail(f"{repo_path(LOCAL_AI_DECISIONS)} decision IDs must be present and unique")
missing_decision_ids = REQUIRED_LOCAL_AI_DECISION_IDS.difference(decision_ids)
if missing_decision_ids:
    fail(
        f"{repo_path(LOCAL_AI_DECISIONS)} missing required decision IDs: "
        f"{', '.join(sorted(missing_decision_ids))}"
    )

if local_ai_cases.get("schema_version") != LOCAL_AI_EVALUATION_SCHEMA:
    fail(
        f"{repo_path(LOCAL_AI_CASES)} schema_version must be "
        f"{LOCAL_AI_EVALUATION_SCHEMA}"
    )
case_entries = local_ai_cases.get("cases")
if not isinstance(case_entries, list):
    fail(f"{repo_path(LOCAL_AI_CASES)} cases must be an array")
case_ids = [entry.get("id") for entry in case_entries if isinstance(entry, dict)]
if len(case_ids) != len(case_entries) or len(set(case_ids)) != len(case_ids):
    fail(f"{repo_path(LOCAL_AI_CASES)} case IDs must be present and unique")
cases_by_id = {entry["id"]: entry for entry in case_entries}
missing_case_ids = set(REQUIRED_LOCAL_AI_CASE_QUESTIONS).difference(cases_by_id)
if missing_case_ids:
    fail(
        f"{repo_path(LOCAL_AI_CASES)} missing required case IDs: "
        f"{', '.join(sorted(missing_case_ids))}"
    )
for case_id, required_question_ids in REQUIRED_LOCAL_AI_CASE_QUESTIONS.items():
    questions = cases_by_id[case_id].get("questions")
    if not isinstance(questions, list):
        fail(f"{repo_path(LOCAL_AI_CASES)} case {case_id!r} questions must be an array")
    question_ids = [question.get("id") for question in questions if isinstance(question, dict)]
    if len(question_ids) != len(questions) or len(set(question_ids)) != len(question_ids):
        fail(
            f"{repo_path(LOCAL_AI_CASES)} case {case_id!r} question IDs "
            "must be present and unique"
        )
    missing_question_ids = required_question_ids.difference(question_ids)
    if missing_question_ids:
        fail(
            f"{repo_path(LOCAL_AI_CASES)} case {case_id!r} missing required "
            f"question IDs: {', '.join(sorted(missing_question_ids))}"
        )
    for question in questions:
        allowed_values = question.get("allowed_values")
        if not isinstance(allowed_values, list) or question.get("expected") not in allowed_values:
            fail(
                f"{repo_path(LOCAL_AI_CASES)} question {question.get('id')!r} "
                "expected value must be listed in allowed_values"
            )

model_policy_match = re.search(
    r"function\s+Get-AllowedLocalAiModelId\s*\{(?P<body>.*?)\}",
    local_ai_config_text,
    re.DOTALL,
)
if not model_policy_match:
    fail(f"{repo_path(LOCAL_AI_CONFIG)} is missing Get-AllowedLocalAiModelId")
configured_model_ids = tuple(re.findall(r"['\"]([^'\"]+)['\"]", model_policy_match.group("body")))
if configured_model_ids != LOCAL_AI_ALLOWED_MODELS:
    fail(
        f"{repo_path(LOCAL_AI_CONFIG)} allowed models must be exactly: "
        f"{', '.join(LOCAL_AI_ALLOWED_MODELS)}"
    )
manifest_sha256 = hashlib.sha256(local_ai_manifest_text.encode("utf-8")).hexdigest()
if manifest_sha256 != LOCAL_AI_MANIFEST_WITH_EVALUATION_EXPORT_SHA256:
    fail(
        f"{repo_path(LOCAL_AI_MANIFEST)} must match the reviewed canonical "
        "manifest that exports Invoke-LocalAiModelEvaluation"
    )

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
):
    match = re.search(rf'{symbol}\s*:\s*BASE_ID\s*\+\s*(\d+)', items_text)
    if not match:
        fail(f"could not locate {label} preview item ID assignment")
    absolute = 187256000 + int(match.group(1))
    if absolute != expected_id:
        fail(f"{label} item ID changed: expected {expected_id}, got {absolute}")

if f'"implementation_version": "{EXPECTED["implementation_version"]}"' not in world_text:
    fail("implementation_version changed without updating validator/baseline docs")
if f'"generation_foundation_version": "{EXPECTED["generation_foundation_version"]}"' not in world_text:
    fail("generation_foundation_version changed without updating validator/baseline docs")

for inactive_marker in (
    '"star_items_active": False',
    '"difficulty_filtering_active": False',
):
    if inactive_marker not in world_text:
        fail(f"missing inactive preview marker: {inactive_marker}")

for label, marker in (
    ("feature flag", '"randomize_hip_glasses_chicken_bucket": True'),
    ("Hip Glasses slot-data item", '"hip_glasses_item": HIP_GLASSES_ITEM'),
    ("Chicken Bucket slot-data item", '"chicken_bucket_item": CHICKEN_BUCKET_ITEM'),
    ("Level 4 slot-data source", '"hip_glasses_source_location": ROOTS_LEVEL4_HIP_GLASSES'),
    ("Bucket trade slot-data source", '"bucket_minion_trade_location": ROOTS_BUCKET_MINION_TRADE'),
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
    "hip_glasses_location_id": EXPECTED["hip_glasses_location_id"],
    "bucket_trade_location_id": EXPECTED["bucket_trade_location_id"],
    "next_item_id": EXPECTED["next_item_id"],
    "local_ai_allowed_models": list(LOCAL_AI_ALLOWED_MODELS),
    "local_ai_decision_schema": LOCAL_AI_DECISION_SCHEMA,
    "local_ai_evaluation_schema": LOCAL_AI_EVALUATION_SCHEMA,
}, indent=2))
print("v0.19 consolidated preview is active; the v0.18 repair contract remains enforced, preview abilities stay out of generated seeds, and Area Access remains authoritative.")
print(f"Next safe item ID:     {EXPECTED['next_item_id']}")
print(f"Next safe location ID: {EXPECTED['next_location_id']}")
