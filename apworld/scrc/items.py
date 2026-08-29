from BaseClasses import ItemClassification

from .cassettes import CASSETTES


BASE_ID = 187256000
STAR_ITEM_NAME = "Star"
STAR_ITEM_COUNT = 66
HYPNO_PAN_ITEM_NAME = "Hypno Pan"
VIOLANCE_ITEM_NAME = "Violance"
MONEY_CASSETTE_ITEM_NAME = "Money Cassette"

NEW_ITEM_NAME_TO_ID = {
    STAR_ITEM_NAME: BASE_ID + 118,
    HYPNO_PAN_ITEM_NAME: BASE_ID + 121,
    VIOLANCE_ITEM_NAME: BASE_ID + 122,
    MONEY_CASSETTE_ITEM_NAME: BASE_ID + 123,
}

NEW_ITEM_CLASSIFICATIONS = {
    STAR_ITEM_NAME: ItemClassification.progression,
    HYPNO_PAN_ITEM_NAME: ItemClassification.progression,
    VIOLANCE_ITEM_NAME: ItemClassification.progression,
    MONEY_CASSETTE_ITEM_NAME: ItemClassification.progression,
}

# Money's pilot ID remains in NEW_ITEM_NAME_TO_ID above.  The catalog owns the
# full permanent cassette mapping so future consumers share the same names and
# IDs rather than duplicating a second song list.
CASSETTE_ITEM_NAME_TO_ID = {
    entry.item_name: entry.item_id
    for entry in CASSETTES
}

CASSETTE_ITEM_CLASSIFICATIONS = {
    entry.item_name: ItemClassification.progression
    for entry in CASSETTES
}


def planned_star_names() -> tuple[str, ...]:
    """Return the future 66-item Star inventory plan without activating it."""
    return tuple(STAR_ITEM_NAME for _ in range(STAR_ITEM_COUNT))


def validate_planned_item_capacity(
    available_locations: int,
    existing_required_items: int,
) -> None:
    """Reject a future pool plan that cannot fit all required items."""
    values = (available_locations, existing_required_items)
    if any(isinstance(value, bool) or not isinstance(value, int) or value < 0 for value in values):
        raise ValueError(
            "available_locations and existing_required_items must be non-negative integers"
        )

    required_items = existing_required_items + STAR_ITEM_COUNT
    if available_locations < required_items:
        raise ValueError(
            f"planned pool has {required_items} required items but only "
            f"{available_locations} locations"
        )
