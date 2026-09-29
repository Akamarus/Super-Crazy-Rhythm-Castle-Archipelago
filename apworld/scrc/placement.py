from __future__ import annotations

from collections.abc import Collection

from BaseClasses import ItemClassification


TIERED_PREFIXES = ("Game Garage - ", "Music Lab Cassette - ")


def required_progression_allowed(
    location_name: str,
    active_tier_locations: Collection[str],
) -> bool:
    if location_name.startswith(TIERED_PREFIXES):
        return location_name in active_tier_locations
    return True


def filler_or_safe_required(item, safe_for_required: bool) -> bool:
    if safe_for_required:
        return True
    classification = item.classification
    progression = ItemClassification.progression
    try:
        return not bool(classification & progression)
    except TypeError:
        return classification != progression


def filler_item_allowed(item) -> bool:
    """Accept any world's filler/traps without stranding useful or progression items."""
    classification = item.classification
    try:
        return not bool(classification & (ItemClassification.progression | ItemClassification.useful))
    except TypeError:
        return classification not in (ItemClassification.progression, ItemClassification.useful)
