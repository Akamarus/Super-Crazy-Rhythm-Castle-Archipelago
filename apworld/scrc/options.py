from dataclasses import dataclass

from Options import Choice, PerGameCommonOptions, Range


class RequiredStars(Range):
    """AP Stars needed before defeating King Ferdinand I to complete your goal."""
    display_name = "Required Stars"
    range_start = 1
    range_end = 66
    default = 50


class Difficulty(Choice):
    """Check tiers: Normal uses one-star/bronze, Hard adds two-star/silver,
    Expert adds three-star/gold, and Perfection adds perfect results.
    This does not change the native REG/PRO music setting.
    """
    display_name = "AP Performance Difficulty"
    option_normal = 0
    option_hard = 1
    option_expert = 2
    option_perfection = 3
    default = 0


class StartingArea(Choice):
    """Starting area access. Roots is currently the only validated start.
    Validated Random selects only from validated starting areas.
    """
    display_name = "Starting Area"
    # Keep schema-19 value 0; random is reserved by Archipelago Choice.
    option_validated_random = 0
    option_roots = 1
    default = 0


@dataclass
class SCRCOptions(PerGameCommonOptions):
    required_stars: RequiredStars
    difficulty: Difficulty
    starting_area: StartingArea
