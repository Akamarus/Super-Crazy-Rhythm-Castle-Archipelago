from dataclasses import dataclass

from Options import Choice, PerGameCommonOptions, Range


class RequiredStars(Range):
    display_name = "Required Stars"
    range_start = 1
    range_end = 66
    default = 50


class Difficulty(Choice):
    display_name = "AP Performance Difficulty"
    option_normal = 0
    option_hard = 1
    option_expert = 2
    option_perfection = 3
    default = 0


class StartingArea(Choice):
    display_name = "Starting Area"
    option_random = 0
    option_roots = 1
    option_lobby = 2
    option_meat_dimension = 3
    option_cell_tower = 4
    default = 0


@dataclass
class SCRCOptions(PerGameCommonOptions):
    required_stars: RequiredStars
    difficulty: Difficulty
    starting_area: StartingArea
