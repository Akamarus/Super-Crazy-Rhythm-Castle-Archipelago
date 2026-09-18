from __future__ import annotations

import random
from collections.abc import Mapping


LEVEL_NAMES = tuple(f"Level {number}" for number in range(1, 23))

# Seeded depth targets over a conservative half-goal ceiling. Native quest
# closures are enforced separately; unused Stars leave capacity for item routing.
DEFAULT_DEPTH_FRACTIONS = (
    0.00,
    0.02,
    0.04,
    0.08,
    0.12,
    0.16,
    0.20,
    0.24,
    0.28,
    0.32,
    0.36,
    0.40,
    0.46,
    0.52,
    0.58,
    0.64,
    0.70,
    0.76,
    0.82,
    0.86,
    0.90,
    0.94,
)


def _validate_goal(required_stars: int) -> None:
    if isinstance(required_stars, bool) or not isinstance(required_stars, int):
        raise ValueError("required_stars must be an integer from 1 through 66")
    if not 1 <= required_stars <= 66:
        raise ValueError("required_stars must be from 1 through 66")


def generate_star_requirements(
    required_stars: int,
    rng: random.Random,
) -> dict[str, int]:
    _validate_goal(required_stars)
    # Full-goal scaling failed actual Normal restrictive fill at seed285001.
    # Reserve at least half the goal for routing beyond the highest entry gate.
    maximum = min(required_stars - 1, required_stars // 2)
    values: list[int] = []
    previous = 0

    for index, fraction in enumerate(DEFAULT_DEPTH_FRACTIONS):
        # Goal-independent normalized jitter ensures a larger configured goal
        # cannot lower a requirement when the seed is unchanged.
        adjusted_fraction = max(0.0, min(1.0, fraction + rng.uniform(-0.04, 0.04)))
        candidate = 0 if index < 2 else round(maximum * adjusted_fraction)
        value = min(maximum, max(previous, candidate, 0))
        values.append(value)
        previous = value

    result = dict(zip(LEVEL_NAMES, values, strict=True))
    validate_star_requirements(result, required_stars)
    return result


def validate_star_requirements(
    requirements: Mapping[str, int],
    required_stars: int,
) -> None:
    _validate_goal(required_stars)
    if tuple(requirements) != LEVEL_NAMES:
        raise ValueError("requirements must contain exactly Levels 1 through 22 in order")

    previous = 0
    for level_name, value in requirements.items():
        if isinstance(value, bool) or not isinstance(value, int):
            raise ValueError(f"{level_name} requirement must be an integer")
        if value < 0:
            raise ValueError(f"{level_name} requirement must be non-negative")
        if value >= required_stars:
            raise ValueError(f"{level_name} requirement must be below required_stars")
        if value < previous:
            raise ValueError("campaign requirements must be nondecreasing")
        previous = value
