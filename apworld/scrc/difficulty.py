from __future__ import annotations

from collections.abc import Iterable


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


def campaign_star_tiers(difficulty: int) -> frozenset[int]:
    _validate_difficulty(difficulty)
    return CAMPAIGN_STAR_TIERS[difficulty]


def medal_tiers(difficulty: int) -> frozenset[str]:
    _validate_difficulty(difficulty)
    return MEDAL_TIER_SETS[difficulty]


def _recognized_campaign_star_tier(location_name: str) -> int | None:
    if location_name.endswith(" - 1 Star"):
        return 1
    if location_name.endswith(" - 2 Stars"):
        return 2
    if location_name.endswith(" - 3 Stars"):
        return 3
    return None


def is_location_active(location_name: str, difficulty: int) -> bool:
    _validate_difficulty(difficulty)
    campaign_tier = _recognized_campaign_star_tier(location_name)
    if campaign_tier is not None:
        return campaign_tier in campaign_star_tiers(difficulty)
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
