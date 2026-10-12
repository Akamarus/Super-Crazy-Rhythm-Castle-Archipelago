"""Restore server-authored logic for Universal Tracker, never reroll its gates."""
from copy import deepcopy

from .cassette_bundles import validate_bundles
from .star_requirements import LEVEL_NAMES, validate_star_requirements
from .starting_areas import STARTING_AREA_NAMES, VALIDATED_STARTING_AREAS


def tracker_slot_data(slot_data: dict) -> dict:
    if (not isinstance(slot_data, dict) or type(slot_data.get("schema_version")) is not int
            or slot_data["schema_version"] not in (19, 20)):
        raise ValueError("SCRC Universal Tracker requires a schema-19 or schema-20 seed")
    schema = slot_data["schema_version"]
    size = slot_data.get("cassette_bundle_size", 1)
    groups = validate_bundles(size, slot_data.get("cassette_bundles", {}))
    if (schema == 20) != (size > 1):
        raise ValueError("SCRC cassette bundle mode does not match slot schema")
    goal = slot_data.get("required_stars")
    difficulty = slot_data.get("difficulty", {})
    difficulty = difficulty.get("value") if isinstance(difficulty, dict) else None
    if type(difficulty) is not int or difficulty not in range(4):
        raise ValueError("SCRC tracker slot has invalid difficulty")
    starter = slot_data.get("starting_area_item")
    if starter not in VALIDATED_STARTING_AREAS:
        raise ValueError("SCRC tracker slot has unsupported starting area")
    requested = slot_data.get("starting_area_requested")
    start_option = next((key for key, name in STARTING_AREA_NAMES.items() if name == requested), None)
    if start_option not in (0, 1):
        raise ValueError("SCRC tracker slot has unsupported starting-area option")
    raw = slot_data.get("generated_star_requirements")
    if not isinstance(raw, dict) or set(raw) != set(LEVEL_NAMES):
        raise ValueError("SCRC tracker slot is missing its exact generated Star gates")
    requirements = {name: raw[name] for name in LEVEL_NAMES}
    validate_star_requirements(requirements, goal)
    metadata = _validate_metadata(slot_data, difficulty, requirements)
    return {
        **metadata,
        "schema_version": schema,
        "cassette_bundle_size": size,
        "cassette_bundles": groups,
        "required_stars": goal,
        "difficulty": {"value": difficulty},
        "starting_area_requested": requested,
        "starting_area_option": start_option,
        "starting_area_item": starter,
        "generated_star_requirements": requirements,
    }


def _same_contract_value(actual, expected):
    if type(actual) is not type(expected):
        return False
    if isinstance(expected, dict):
        return (actual.keys() == expected.keys() and all(
            _same_contract_value(actual[key], value) for key, value in expected.items()))
    if isinstance(expected, list):
        return (len(actual) == len(expected) and all(
            _same_contract_value(a, b) for a, b in zip(
                sorted(actual, key=repr), sorted(expected, key=repr))))
    return actual == expected


def _validate_metadata(slot_data, difficulty, requirements):
    # Import the completed catalog lazily: the world imports this parser itself.
    from . import (LOCATION_NAME_TO_ID, CARTRIDGE_SOURCE_LOCATIONS,
                  VANILLA_GARAGE_CARTRIDGE_SONG, LOBBY_BEAN_TRUMPET_AWARD)
    from .campaign_levels import CAMPAIGN_LOCATION_NAMES, CAMPAIGN_LOCATION_TIERS
    from .difficulty import filter_locations_for_difficulty, campaign_star_tiers, medal_tiers, MEDAL_TIERS
    from .native_logic import star_eater_requirements

    active = {name for name in filter_locations_for_difficulty(LOCATION_NAME_TO_ID, difficulty)
              if name not in (CARTRIDGE_SOURCE_LOCATIONS[VANILLA_GARAGE_CARTRIDGE_SONG],
                              LOBBY_BEAN_TRUMPET_AWARD)
              and not name.startswith("Development Cache ")}
    campaign = sorted(active.intersection(CAMPAIGN_LOCATION_NAMES))
    expected = {
        "star_eater_requirements": star_eater_requirements(requirements),
        "active_location_count": len(active),
        "active_campaign_locations": campaign,
        "active_campaign_location_tiers": [tier for tier in CAMPAIGN_LOCATION_TIERS
                                           if any(n.endswith(f" - {tier}") for n in campaign)],
        "active_campaign_star_tiers": sorted(campaign_star_tiers(difficulty)),
        "active_medal_tiers": [tier for tier in MEDAL_TIERS if tier in medal_tiers(difficulty)],
        "difficulty_filtering_active": True,
    }
    for key, value in expected.items():
        # Legacy schema 19 may omit derived metadata. Present metadata is always
        # authoritative: reject a contradiction instead of silently replacing it.
        if key not in slot_data and slot_data["schema_version"] == 19:
            continue
        if key not in slot_data or not _same_contract_value(slot_data[key], value):
            raise ValueError(f"SCRC tracker slot has incompatible {key}")
    return deepcopy(expected)
