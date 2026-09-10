"""Immutable normal-campaign level identities and permanent location IDs."""

from dataclasses import dataclass
from typing import Mapping, Sequence


BASE_ID = 187256000
CAMPAIGN_LOCATION_TIERS = ("Completion", "1 Star", "2 Stars", "3 Stars")


@dataclass(frozen=True)
class CampaignLevel:
    number: int
    display_name: str
    internal_id: str
    area: str
    required_items: tuple[str, ...] = ()
    progression_safe: bool = False

    def location_name(self, tier: str) -> str:
        return f"Level {self.number} - {tier}"


verified_requirements = {
    1: (),
    2: (),
    3: ("Weed Killer", "Plant Pipes"),
    4: ("Weed Killer", "Plant Pipes"),
    5: ("Weed Killer", "Plant Pipes", "Hip Glasses", "Chicken Bucket"),
    18: ("Plant Pipes",),
    19: (),
    20: ("Hypno Pan",),
    21: ("Plant Pipes",),
    22: (),
}
progression_safe_levels = frozenset((1, 2, 3, 4, 5, 22))


def _level(number: int, display_name: str, internal_id: str, area: str) -> CampaignLevel:
    return CampaignLevel(
        number,
        display_name,
        internal_id,
        area,
        verified_requirements.get(number, ()),
        number in progression_safe_levels,
    )


CAMPAIGN_LEVELS: tuple[CampaignLevel, ...] = (
    _level(1, "The Little Things", "Level_05", "Roots"),
    _level(2, "Pop Party", "Level_06", "Roots"),
    _level(3, "Jolt City", "Level_07", "Roots"),
    _level(4, "Quieres Bailar", "Level_08", "Roots"),
    _level(5, "Lift Quest", "Level_09", "Roots"),
    _level(6, "Boring Room", "Level_02", "Lobby"),
    _level(7, "Demolition Training", "Level_19", "Lobby"),
    _level(8, "Minim Tower", "Level_11", "Lobby"),
    _level(9, "School Trip", "Level_20", "Lobby"),
    _level(10, "The Vault", "Level_01", "Lobby"),
    _level(11, "Act 1: Flavor", "Level_12", "Meat Dimension"),
    _level(12, "Act 2", "Level_15", "Meat Dimension"),
    _level(13, "Act 3", "Level_22", "Meat Dimension"),
    _level(14, "Act 4", "Level_23", "Meat Dimension"),
    _level(15, "Central Mainframe", "Level_16", "Cell Tower"),
    _level(16, "Thief Prince", "Level_24", "Cell Tower"),
    _level(17, "Cold Storage", "Level_21", "Lobby"),
    _level(18, "Darkness", "Level_03", "Tower of Fear"),
    _level(19, "Escape", "Level_13", "Tower of Fear"),
    _level(20, "Loneliness", "Level_25", "Tower of Fear"),
    _level(21, "Locker Room", "Level_14", "Royal Corridor"),
    _level(22, "King Ferdinand I", "Level_28", "Royal Corridor"),
)

CAMPAIGN_LEVELS_BY_INTERNAL_ID = {
    level.internal_id: level for level in CAMPAIGN_LEVELS
}


def campaign_location_names(level: CampaignLevel | None = None) -> tuple[str, ...]:
    """Return canonical campaign locations in catalog and cumulative-tier order."""
    levels = CAMPAIGN_LEVELS if level is None else (level,)
    return tuple(
        candidate.location_name(tier)
        for candidate in levels
        for tier in CAMPAIGN_LOCATION_TIERS
    )


CAMPAIGN_LOCATION_NAMES = campaign_location_names()

LEGACY_CAMPAIGN_LOCATION_IDS = {
    "Level 1 - Completion": BASE_ID + 1,
    "Level 2 - Completion": BASE_ID + 2,
    "Level 3 - Completion": BASE_ID + 3,
    "Level 22 - Completion": BASE_ID + 182,
    "Level 22 - 1 Star": BASE_ID + 183,
    "Level 22 - 2 Stars": BASE_ID + 184,
    "Level 22 - 3 Stars": BASE_ID + 185,
}
NEW_CAMPAIGN_LOCATION_START = BASE_ID + 211
NEW_CAMPAIGN_LOCATION_NAMES = tuple(
    name
    for name in CAMPAIGN_LOCATION_NAMES
    if name not in LEGACY_CAMPAIGN_LOCATION_IDS
)
NEW_CAMPAIGN_LOCATION_NAME_TO_ID = {
    name: NEW_CAMPAIGN_LOCATION_START + index
    for index, name in enumerate(NEW_CAMPAIGN_LOCATION_NAMES)
}
CAMPAIGN_LOCATION_NAME_TO_ID = {
    **LEGACY_CAMPAIGN_LOCATION_IDS,
    **NEW_CAMPAIGN_LOCATION_NAME_TO_ID,
}


def _first_duplicate(values: Sequence[object]) -> object | None:
    seen: set[object] = set()
    for value in values:
        if value in seen:
            return value
        seen.add(value)
    return None


def validate_campaign_catalog(
    *,
    catalog: tuple[CampaignLevel, ...] = CAMPAIGN_LEVELS,
    location_names: Sequence[str] = CAMPAIGN_LOCATION_NAMES,
    legacy_location_ids: Mapping[str, int] = LEGACY_CAMPAIGN_LOCATION_IDS,
    new_location_names: Sequence[str] = NEW_CAMPAIGN_LOCATION_NAMES,
    new_location_name_to_id: Mapping[str, int] = NEW_CAMPAIGN_LOCATION_NAME_TO_ID,
    location_name_to_id: Mapping[str, int] = CAMPAIGN_LOCATION_NAME_TO_ID,
) -> None:
    """Reject catalog or permanent-ID drift before it reaches a datapackage."""
    if len(catalog) != 22:
        raise ValueError(f"campaign level count is {len(catalog)}, expected 22")

    for field, values in (
        ("level number", [level.number for level in catalog]),
        ("internal ID", [level.internal_id for level in catalog]),
    ):
        duplicate = _first_duplicate(values)
        if duplicate is not None:
            raise ValueError(f"duplicate campaign {field}: {duplicate}")

    if tuple(level.number for level in catalog) != tuple(range(1, 23)):
        raise ValueError("campaign level numbers must be the ordered range 1..22")

    expected_names = tuple(
        candidate.location_name(tier)
        for candidate in catalog
        for tier in CAMPAIGN_LOCATION_TIERS
    )
    actual_names = tuple(location_names)
    duplicate = _first_duplicate(actual_names)
    if duplicate is not None:
        raise ValueError(f"duplicate campaign location name: {duplicate}")
    if actual_names != expected_names:
        raise ValueError("campaign location tier sequence is invalid")

    expected_legacy_ids = {
        "Level 1 - Completion": BASE_ID + 1,
        "Level 2 - Completion": BASE_ID + 2,
        "Level 3 - Completion": BASE_ID + 3,
        "Level 22 - Completion": BASE_ID + 182,
        "Level 22 - 1 Star": BASE_ID + 183,
        "Level 22 - 2 Stars": BASE_ID + 184,
        "Level 22 - 3 Stars": BASE_ID + 185,
    }
    if dict(legacy_location_ids) != expected_legacy_ids:
        raise ValueError("legacy campaign location IDs changed")

    expected_new_names = tuple(
        name for name in expected_names if name not in legacy_location_ids
    )
    actual_new_names = tuple(new_location_names)
    duplicate = _first_duplicate(actual_new_names)
    if duplicate is not None:
        raise ValueError(f"duplicate new campaign location name: {duplicate}")
    if actual_new_names != expected_new_names:
        raise ValueError("new campaign location order is invalid")

    new_ids = [new_location_name_to_id.get(name) for name in actual_new_names]
    if any(value is None for value in new_ids) or set(new_location_name_to_id) != set(actual_new_names):
        raise ValueError("new campaign location names are invalid")
    duplicate = _first_duplicate(new_ids)
    if duplicate is not None:
        raise ValueError(f"duplicate campaign location ID: {duplicate}")
    expected_new_ids = list(
        range(NEW_CAMPAIGN_LOCATION_START, NEW_CAMPAIGN_LOCATION_START + len(expected_new_names))
    )
    if new_ids != expected_new_ids:
        raise ValueError("new campaign location IDs must be contiguous and ordered")

    expected_location_ids = {
        **dict(legacy_location_ids),
        **dict(new_location_name_to_id),
    }
    if dict(location_name_to_id) != expected_location_ids:
        raise ValueError("campaign location ID registry is invalid")
    duplicate = _first_duplicate(list(location_name_to_id.values()))
    if duplicate is not None:
        raise ValueError(f"duplicate campaign location ID: {duplicate}")
    if set(location_name_to_id) != set(expected_names):
        raise ValueError("campaign location names are incomplete")


validate_campaign_catalog()
