from __future__ import annotations

from collections.abc import Iterable


DIFFICULTY_NAMES = ("Normal", "Hard", "Expert", "Perfection")
MEDAL_TIERS = ("Bronze", "Silver", "Gold", "Platinum")
MEDAL_TIER_LIMIT = (1, 2, 3, 4)
TIERED_PREFIXES = ("Game Garage - ", "Music Lab Cassette - ")


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
    allowed_tiers = set(MEDAL_TIERS[: MEDAL_TIER_LIMIT[difficulty]])
    filtered: list[str] = []

    for name in location_names:
        tier = _recognized_medal_tier(name)
        if tier is None or tier in allowed_tiers:
            filtered.append(name)

    return tuple(filtered)


def _recognized_medal_tier(location_name: str) -> str | None:
    if not location_name.startswith(TIERED_PREFIXES):
        return None
    for tier in MEDAL_TIERS:
        if location_name.endswith(f" - {tier}"):
            return tier
    return None
