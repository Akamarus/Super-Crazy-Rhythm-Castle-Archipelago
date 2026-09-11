from __future__ import annotations

from collections.abc import Iterable

from .campaign_levels import CAMPAIGN_LOCATION_NAMES, CAMPAIGN_LOCATION_TIERS

DIFFICULTY_NAMES = ("Normal", "Hard", "Expert", "Perfection")
MEDAL_TIERS = ("Bronze", "Silver", "Gold", "Platinum")
MEDAL_TIER_LIMIT = (1, 2, 3, 4)
TIERED_PREFIXES = ("Game Garage - ", "Music Lab Cassette - ")
CAMPAIGN_STAR_TIERS = (
    frozenset({1}),
    frozenset({1, 2}),
    frozenset({1, 2, 3}),
    frozenset({1, 2, 3}),
)
CAMPAIGN_TIERS_BY_DIFFICULTY = {
    0: frozenset(("Completion", "1 Star")),
    1: frozenset(("Completion", "1 Star", "2 Stars")),
    2: frozenset(("Completion", "1 Star", "2 Stars", "3 Stars")),
    3: frozenset(("Completion", "1 Star", "2 Stars", "3 Stars")),
}
CAMPAIGN_LOCATION_TIERS_BY_NAME = {
    name: tier
    for name in CAMPAIGN_LOCATION_NAMES
    for tier in CAMPAIGN_LOCATION_TIERS
    if name.endswith(f" - {tier}")
}
MEDAL_TIER_SETS = tuple(
    frozenset(MEDAL_TIERS[:limit]) for limit in MEDAL_TIER_LIMIT
)


def _validate_difficulty(difficulty: int) -> None:
    if isinstance(difficulty, bool) or not isinstance(difficulty, int):
        raise ValueError("difficulty must be an integer from 0 through 3")
    if not 0 <= difficulty < len(DIFFICULTY_NAMES):
        raise ValueError("difficulty must be from 0 through 3")


def filter_locations_for_difficulty(
    location_names: Iterable[str],
    difficulty: int,
) -> tuple[str, ...]:
    _validate_difficulty(difficulty)
    return tuple(name for name in location_names if is_location_active(name, difficulty))


def active_location_names(
    location_names: Iterable[str],
    difficulty: int,
) -> tuple[str, ...]:
    """Return catalog-owned campaign locations enabled at this difficulty."""
    _validate_difficulty(difficulty)
    active_tiers = CAMPAIGN_TIERS_BY_DIFFICULTY[difficulty]
    return tuple(
        name
        for name in location_names
        if CAMPAIGN_LOCATION_TIERS_BY_NAME.get(name) in active_tiers
    )


def campaign_star_tiers(difficulty: int) -> frozenset[int]:
    _validate_difficulty(difficulty)
    return CAMPAIGN_STAR_TIERS[difficulty]


def medal_tiers(difficulty: int) -> frozenset[str]:
    _validate_difficulty(difficulty)
    return MEDAL_TIER_SETS[difficulty]


def _recognized_campaign_tier(location_name: str) -> str | None:
    return CAMPAIGN_LOCATION_TIERS_BY_NAME.get(location_name)


def is_location_active(location_name: str, difficulty: int) -> bool:
    _validate_difficulty(difficulty)
    campaign_tier = _recognized_campaign_tier(location_name)
    if campaign_tier is not None:
        return campaign_tier in CAMPAIGN_TIERS_BY_DIFFICULTY[difficulty]
    medal_tier = _recognized_medal_tier(location_name)
    if medal_tier is not None:
        return medal_tier in medal_tiers(difficulty)
    return True


def _recognized_medal_tier(location_name: str) -> str | None:
    if not location_name.startswith(TIERED_PREFIXES):
        return None
    for tier in MEDAL_TIERS:
        if location_name.endswith(f" - {tier}"):
            return tier
    return None
