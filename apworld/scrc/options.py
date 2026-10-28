from dataclasses import dataclass

from Options import Choice, PerGameCommonOptions, Range


class RequiredStars(Range):
    """AP Stars needed before completing the selected ending; Bunker mode needs at least 2."""
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


class CassetteBundleSize(Choice):
    """Group cassettes into seed-specific random pairs or triples. Each received
    bundle grants all its songs. Individual preserves separate cassette items.
    """
    display_name = "Cassette Bundle Size"
    option_individual = 1
    option_pairs = 2
    option_triples = 3
    default = 1


class BunkerProgression(Choice):
    """Randomize Bunker Keycard and quest items; requires a schema21-compatible client."""
    display_name = "Bunker Progression"
    option_disabled = 0
    option_enabled = 1
    default = 1


class Goal(Choice):
    """Complete the first King fight or Gecko's True Ending, after the Star goal."""
    display_name = "Victory Goal"
    option_king_ferdinand = 0
    option_true_ending = 1
    default = 0


@dataclass
class SCRCOptions(PerGameCommonOptions):
    bunker_progression: BunkerProgression
    goal: Goal
    required_stars: RequiredStars
    difficulty: Difficulty
    starting_area: StartingArea
    cassette_bundle_size: CassetteBundleSize
