"""Restore server-authored logic for Universal Tracker, never reroll its gates."""
from .star_requirements import LEVEL_NAMES, validate_star_requirements
from .starting_areas import STARTING_AREA_NAMES, VALIDATED_STARTING_AREAS


def tracker_slot_data(slot_data: dict) -> dict:
    if not isinstance(slot_data, dict) or slot_data.get("schema_version") != 19:
        raise ValueError("SCRC Universal Tracker requires a v0.28/schema-19 seed")
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
    return {
        "schema_version": 19,
        "required_stars": goal,
        "difficulty": {"value": difficulty},
        "starting_area_requested": requested,
        "starting_area_option": start_option,
        "starting_area_item": starter,
        "generated_star_requirements": requirements,
    }
