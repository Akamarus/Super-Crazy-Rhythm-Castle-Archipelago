"""Immutable Archipelago item definitions for Music Lab points."""

from dataclasses import dataclass
from typing import Mapping, Sequence


BASE_ID = 187256000
MUSIC_LAB_POINT_SCHEMA = 1
_EXPECTED_MUSIC_LAB_POINT_TOTAL_VALUE = 180


@dataclass(frozen=True)
class MusicLabPointItem:
    name: str
    item_id: int
    value: int
    count: int


MUSIC_LAB_POINT_ITEMS = (
    MusicLabPointItem("Music Lab Point", BASE_ID + 153, 1, 10),
    MusicLabPointItem("Music Lab Point Bundle", BASE_ID + 154, 10, 3),
    MusicLabPointItem("Music Lab Point Large Bundle", BASE_ID + 155, 20, 7),
)

MUSIC_LAB_POINT_THRESHOLDS = {
    5: "Music Lab - 5 Point Chest",
    10: "Music Lab - 10 Point Chest",
    20: "Music Lab - 20 Point Chest",
    32: "Music Lab - 32 Point Chest",
    46: "Music Lab - 46 Point Chest",
    64: "Music Lab - 64 Point Chest",
    89: "Music Lab - 89 Point Chest",
    111: "Music Lab - 111 Point Chest",
    140: "Music Lab - 140 Point Chest",
}


def validate_music_lab_point_catalog(
    catalog: Sequence[MusicLabPointItem] = MUSIC_LAB_POINT_ITEMS,
    thresholds: Mapping[int, str] = MUSIC_LAB_POINT_THRESHOLDS,
) -> None:
    """Reject catalog edits that would alter the permanent point contract."""
    names = tuple(entry.name for entry in catalog)
    if len(names) != len(set(names)):
        raise ValueError("duplicate Music Lab Point item name")

    item_ids = tuple(entry.item_id for entry in catalog)
    if len(item_ids) != len(set(item_ids)):
        raise ValueError("duplicate Music Lab Point item_id")

    for entry in catalog:
        if isinstance(entry.value, bool) or not isinstance(entry.value, int) or entry.value <= 0:
            raise ValueError("Music Lab Point value must be a positive integer")
        if isinstance(entry.count, bool) or not isinstance(entry.count, int) or entry.count <= 0:
            raise ValueError("Music Lab Point count must be a positive integer")

    total_value = sum(entry.value * entry.count for entry in catalog)
    if total_value != _EXPECTED_MUSIC_LAB_POINT_TOTAL_VALUE:
        raise ValueError(
            "Music Lab Point total must be "
            f"{_EXPECTED_MUSIC_LAB_POINT_TOTAL_VALUE}, got {total_value}"
        )

    threshold_values = tuple(thresholds)
    if any(
        isinstance(threshold, bool) or not isinstance(threshold, int) or threshold <= 0
        for threshold in threshold_values
    ):
        raise ValueError("Music Lab Point threshold must be a positive integer")
    if any(left >= right for left, right in zip(threshold_values, threshold_values[1:])):
        raise ValueError("Music Lab Point threshold list must be strictly increasing")


validate_music_lab_point_catalog()

MUSIC_LAB_POINT_ITEMS_BY_NAME = {
    entry.name: entry for entry in MUSIC_LAB_POINT_ITEMS
}
MUSIC_LAB_POINT_ITEMS_BY_ID = {
    entry.item_id: entry for entry in MUSIC_LAB_POINT_ITEMS
}
MUSIC_LAB_POINT_POOL = tuple(
    entry.name
    for entry in MUSIC_LAB_POINT_ITEMS
    for _ in range(entry.count)
)
MUSIC_LAB_POINT_TOTAL_INSTANCES = len(MUSIC_LAB_POINT_POOL)
MUSIC_LAB_POINT_TOTAL_VALUE = sum(
    entry.value * entry.count for entry in MUSIC_LAB_POINT_ITEMS
)
MUSIC_LAB_POINT_MAX_EFFECTIVE = sum(
    entry.value * entry.count for entry in MUSIC_LAB_POINT_ITEMS
)


def weighted_music_lab_points(state, player: int) -> int:
    return sum(
        state.count(entry.name, player) * entry.value
        for entry in MUSIC_LAB_POINT_ITEMS
    )
