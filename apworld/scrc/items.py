from BaseClasses import ItemClassification

from .cassettes import CASSETTES
from .music_lab_points import MUSIC_LAB_POINT_ITEMS


BASE_ID = 187256000
STAR_ITEM_NAME = "Star"
STAR_ITEM_COUNT = 66
HYPNO_PAN_ITEM_NAME = "Hypno Pan"
VIOLANCE_ITEM_NAME = "Violance"
MONEY_CASSETTE_ITEM_NAME = "Money Cassette"

# Reserved for future randomization. The Lobby pickup grants two native rewards
# in one interaction; safe seed/save-bound source recovery is not yet proven.
LOBBY_ITEM_NAME_TO_ID = {
    "Important Letters": BASE_ID + 156,
    "Bean Trumpet": BASE_ID + 157,
    # Registered for permanent ID stability; source recovery is not yet proven.
    "Demolition Certificate": BASE_ID + 158,
}

LOBBY_ITEM_CLASSIFICATIONS = {
    "Important Letters": ItemClassification.progression,
    "Bean Trumpet": ItemClassification.useful,
    "Demolition Certificate": ItemClassification.progression,
}

# v0.25: existing Music Lab 5/20-point checks become these item sources.
# v0.26: their hand-in checks may hold progression, so both inputs are progression.
CHARACTER_QUEST_ITEM_NAME_TO_ID = {
    "Old Game Data": BASE_ID + 159,
    "Car Battery": BASE_ID + 160,
}
CHARACTER_QUEST_ITEM_CLASSIFICATIONS = {
    name: ItemClassification.progression for name in CHARACTER_QUEST_ITEM_NAME_TO_ID
}
CHARACTER_QUEST_ITEMS = {
    "Old Game Data": "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM",
    "Car Battery": "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM",
}
CHARACTER_QUEST_ITEM_LOCATIONS = {
    "Old Game Data": "Music Lab - 5 Point Chest",
    "Car Battery": "Music Lab - 20 Point Chest",
}

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

MUSIC_LAB_POINT_ITEM_NAME_TO_ID = {
    entry.name: entry.item_id
    for entry in MUSIC_LAB_POINT_ITEMS
}

MUSIC_LAB_POINT_ITEM_CLASSIFICATIONS = {
    entry.name: ItemClassification.progression
    for entry in MUSIC_LAB_POINT_ITEMS
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

# v0.26: permanent IDs allocated after native source and hand-in mapping.
QUEST_ITEM_NAME_TO_ID = {
    "Plunger": BASE_ID + 161,
    "Meoo": BASE_ID + 162,
    "Maniac": BASE_ID + 163,
}
# Characters remain useful; Plunger gates the modeled Lobby feeding action.
QUEST_ITEM_CLASSIFICATIONS = {
    name: ItemClassification.useful for name in QUEST_ITEM_NAME_TO_ID
}

# Plunger now gates the proved Lobby Star Eater action.
QUEST_ITEM_CLASSIFICATIONS["Plunger"] = ItemClassification.progression
