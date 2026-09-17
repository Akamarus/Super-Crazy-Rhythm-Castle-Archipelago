"""The immutable, evidence-backed cassette catalog.

Each definition is one Archipelago item and one idempotent source location.  A
source with multiple native level award routes keeps every route in ``triggers``;
the routes are alternatives, never additional Archipelago locations.
"""

from dataclasses import dataclass
from typing import Mapping, Sequence

from .campaign_levels import CAMPAIGN_LEVELS_BY_INTERNAL_ID


BASE_ID = 187256000


@dataclass(frozen=True)
class CassetteRequirement:
    item: str


@dataclass(frozen=True)
class CassetteTrigger:
    """One verified native level-award route for a cassette source."""

    level: str
    variant: str
    region: str
    requirements: tuple[CassetteRequirement, ...]


@dataclass(frozen=True)
class CassetteDefinition:
    display_song: str
    native_song: str
    item_name: str
    item_id: int
    source_name: str
    source_id: int | None
    reused_location: bool
    region: str
    requirements: tuple[CassetteRequirement, ...]
    source_type: str
    level: str | None
    variant: str | None
    triggers: tuple[CassetteTrigger, ...] = ()


VALID_REGIONS = frozenset(
    {
        "Cell Tower",
        "Lobby",
        "Lobby / Cell Tower return",
        "Lobby OR Secret Bunker",
        "Meat Dimension",
        "Meat Dimension OR Cell Tower",
        "Music Lab",
        "Roots",
        "Roots OR Cell Tower",
        "Roots OR Lobby",
        "Royal Corridor",
        "Royal Corridor OR Secret Bunker",
        "Secret Bunker",
        "Tower of Fear",
        "Tower of Fear OR Secret Bunker",
    }
)


def _requirements(*items: str) -> tuple[CassetteRequirement, ...]:
    return tuple(CassetteRequirement(item) for item in items)


def _trigger(
    level: str,
    variant: str,
    region: str,
    *requirements: str,
) -> CassetteTrigger:
    # Normal cassette awards require finishing their campaign level. Keep
    # route-specific restrictions, but never omit the shared campaign gates.
    # Special variants retain their own routes and must not inherit normal gates.
    if variant == "LevelVariant_Default":
        campaign = CAMPAIGN_LEVELS_BY_INTERNAL_ID[level]
        requirements = tuple(dict.fromkeys((
            *requirements, f"{campaign.area} Access", *campaign.required_items,
        )))
    return CassetteTrigger(level, variant, region, _requirements(*requirements))


def _level_definition(
    display_song: str,
    native_song: str,
    item_id: int,
    source_id: int | None,
    region: str,
    triggers: tuple[CassetteTrigger, ...],
    *,
    source_name: str | None = None,
    item_name: str | None = None,
    reused_location: bool = False,
) -> CassetteDefinition:
    first = triggers[0]
    return CassetteDefinition(
        display_song=display_song,
        native_song=native_song,
        item_name=item_name or f"{display_song} Cassette",
        item_id=item_id,
        source_name=source_name or f"Cassette Source - {display_song}",
        source_id=source_id,
        reused_location=reused_location,
        region=region,
        requirements=first.requirements,
        source_type="Level-earned reward" if len(triggers) == 1 else "Level-earned reward with aliases",
        level=first.level,
        variant=first.variant,
        triggers=triggers,
    )


def _point_chest_definition(
    display_song: str,
    native_song: str,
    item_id: int,
    source_name: str,
) -> CassetteDefinition:
    return CassetteDefinition(
        display_song=display_song,
        native_song=native_song,
        item_name=f"{display_song} Cassette",
        item_id=item_id,
        source_name=source_name,
        source_id=None,
        reused_location=True,
        region="Music Lab",
        requirements=(),
        source_type="Music Lab point chest",
        level=None,
        variant=None,
    )


CASSETTES: tuple[CassetteDefinition, ...] = (
    _level_definition("The Little Things", "THE_LITTLE_THINGS", BASE_ID + 124, BASE_ID + 187, "Cell Tower", (
        _trigger("Level_24", "LevelVariant_Default", "Cell Tower", "Cell Tower Access"),
    )),
    _level_definition("No Plan B", "NO_PLAN_B", BASE_ID + 125, BASE_ID + 188, "Meat Dimension", (
        _trigger("Level_22", "LevelVariant_Default", "Meat Dimension", "Meat Dimension Access", "Hypno Pan"),
    )),
    _level_definition("Jolt City", "JOLT_CITY", BASE_ID + 126, BASE_ID + 189, "Meat Dimension", (
        _trigger("Level_23", "LevelVariant_Default", "Meat Dimension", "Meat Dimension Access", "Hypno Pan"),
    )),
    _level_definition("Quieres Bailar", "QUIERES_BAILAR", BASE_ID + 127, BASE_ID + 190, "Cell Tower", (
        _trigger("Level_16", "LevelVariant_Default", "Cell Tower", "Cell Tower Access"),
    )),
    _point_chest_definition("Quicksand", "QUICKSAND", BASE_ID + 128, "Music Lab - 32 Point Chest"),
    _level_definition("Gold", "GOLD", BASE_ID + 129, BASE_ID + 191, "Roots OR Lobby", (
        _trigger("Level_05", "LevelVariant_Default", "Roots", "Roots Access"),
        _trigger("Level_11", "LevelVariant_Default", "Lobby", "Lobby Access"),
        _trigger("Level_11", "LevelVariant_DevilMode", "Lobby", "Lobby Access"),
    )),
    _level_definition("I Got Money", "I_GOT_MONEY", BASE_ID + 123, BASE_ID + 186, "Roots OR Cell Tower", (
        _trigger("Level_06", "LevelVariant_Default", "Roots", "Roots Access"),
        _trigger("Level_06", "LevelVariant_BeeMode", "Cell Tower", "Cell Tower Access"),
    ), source_name="Level 2 - Money Cassette", item_name="Money Cassette", reused_location=True),
    _level_definition("Hippo and Frog", "HIPPO_AND_FROG", BASE_ID + 130, BASE_ID + 192, "Roots", (
        _trigger("Level_07", "LevelVariant_Default", "Roots", "Roots Access", "Weed Killer", "Plant Pipes"),
    )),
    _level_definition("On the Way", "ON_THE_WAY", BASE_ID + 131, BASE_ID + 193, "Roots OR Lobby", (
        _trigger("Level_08", "LevelVariant_Default", "Roots", "Roots Access", "Weed Killer", "Plant Pipes"),
        _trigger("Level_11", "LevelVariant_Default", "Lobby", "Lobby Access"),
        _trigger("Level_11", "LevelVariant_DevilMode", "Lobby", "Lobby Access"),
    )),
    _level_definition("Badass", "BADASS", BASE_ID + 132, BASE_ID + 194, "Roots", (
        _trigger("Level_09", "LevelVariant_Default", "Roots", "Roots Access", "Hip Glasses", "Chicken Bucket"),
    )),
    _level_definition("Heavy Metal", "HEAVY_METAL", BASE_ID + 133, BASE_ID + 195, "Roots", (
        _trigger("Level_09", "LevelVariant_Default", "Roots", "Roots Access", "Hip Glasses", "Chicken Bucket"),
    )),
    _level_definition("AOK", "AOK", BASE_ID + 134, BASE_ID + 196, "Lobby OR Secret Bunker", (
        _trigger("Level_02", "LevelVariant_Default", "Lobby", "Lobby Access"),
        _trigger("Level_02", "LevelVariant_DevilMode", "Secret Bunker"),
    )),
    _level_definition("Rainbow Melodies", "RAINBOW_MELODIES", BASE_ID + 135, BASE_ID + 197, "Lobby OR Secret Bunker", (
        _trigger("Level_11", "LevelVariant_Default", "Lobby", "Lobby Access"),
        _trigger("Level_11", "LevelVariant_DevilMode", "Secret Bunker"),
        _trigger("Level_19", "LevelVariant_Default", "Lobby", "Lobby Access"),
    )),
    _level_definition("Sneaking", "SNEAKING_LOOP", BASE_ID + 136, BASE_ID + 198, "Lobby", (
        _trigger("Level_19", "LevelVariant_Default", "Lobby", "Lobby Access"),
        _trigger("Level_20", "LevelVariant_Default", "Lobby", "Lobby Access"),
    )),
    _level_definition("The Heist", "THE_HEIST", BASE_ID + 137, BASE_ID + 199, "Lobby", (
        _trigger("Level_20", "LevelVariant_Default", "Lobby", "Lobby Access"),
    )),
    _level_definition("Money", "MONEY_DUB", BASE_ID + 138, BASE_ID + 200, "Lobby", (
        _trigger("Level_01", "LevelVariant_Default", "Lobby", "Lobby Access"),
    ), item_name="Money Dub Cassette"),
    _level_definition("Lets Go", "LETS_GO", BASE_ID + 139, BASE_ID + 201, "Meat Dimension OR Cell Tower", (
        _trigger("Level_12", "LevelVariant_Default", "Meat Dimension", "Meat Dimension Access"),
        _trigger("Level_12", "LevelVariant_BeeMode", "Cell Tower", "Cell Tower Access"),
    )),
    _level_definition("Bounce", "BOUNCE", BASE_ID + 140, BASE_ID + 202, "Meat Dimension", (
        _trigger("Level_15", "LevelVariant_Default", "Meat Dimension", "Meat Dimension Access", "Hypno Pan"),
    )),
    _level_definition("Epical", "THE_EPICAL", BASE_ID + 141, BASE_ID + 203, "Lobby / Cell Tower return", (
        _trigger("Level_21", "LevelVariant_Default", "Lobby", "Lobby Access", "Hypno Pan"),
    )),
    _level_definition("Hollywood Trailer", "HOLLYWOOD_TRAILER", BASE_ID + 142, BASE_ID + 204, "Lobby / Cell Tower return", (
        _trigger("Level_21", "LevelVariant_Default", "Lobby", "Lobby Access", "Hypno Pan"),
    )),
    _level_definition("False Data", "FALSE_DATA", BASE_ID + 143, BASE_ID + 205, "Lobby / Cell Tower return", (
        _trigger("Level_21", "LevelVariant_Default", "Lobby", "Lobby Access", "Hypno Pan"),
    )),
    _level_definition("Gotta Get Up", "GOTTA_GET_UP", BASE_ID + 144, BASE_ID + 206, "Tower of Fear", (
        _trigger("Level_03", "LevelVariant_Default", "Tower of Fear", "Tower of Fear Access"),
    )),
    _level_definition("Fumblin Around", "FUMBLIN_AROUND", BASE_ID + 145, BASE_ID + 207, "Tower of Fear OR Secret Bunker", (
        _trigger("Level_13", "LevelVariant_Default", "Tower of Fear", "Tower of Fear Access"),
        _trigger("Level_13", "LevelVariant_DevilMode", "Secret Bunker"),
    )),
    _level_definition("Party Non Stop", "PARTY_NON_STOP", BASE_ID + 146, BASE_ID + 208, "Tower of Fear", (
        _trigger("Level_25", "LevelVariant_Default", "Tower of Fear", "Tower of Fear Access"),
    )),
    _level_definition("Keep On Hustlin", "KEEP_ON_HUSTLIN", BASE_ID + 147, BASE_ID + 209, "Royal Corridor OR Secret Bunker", (
        _trigger("Level_14", "LevelVariant_Default", "Royal Corridor", "Royal Corridor Access", "Violance", "Weed Killer"),
        _trigger("Level_14", "LevelVariant_DevilMode", "Secret Bunker", "Weed Killer"),
    )),
    _level_definition("Another Day In Paradise", "ANOTHER_DAY_IN_PARADISE", BASE_ID + 148, BASE_ID + 210, "Royal Corridor", (
        _trigger("Level_28", "LevelVariant_Default", "Royal Corridor", "Royal Corridor Access"),
    )),
    _point_chest_definition("Flamenco", "FLAMENCO", BASE_ID + 149, "Music Lab - 64 Point Chest"),
    _point_chest_definition("Ten-Four Good Buddy", "TEN_FOUR_GOOD_BUDDY", BASE_ID + 150, "Music Lab - 89 Point Chest"),
    _point_chest_definition("Zen", "ZEN", BASE_ID + 151, "Music Lab - 111 Point Chest"),
    _point_chest_definition("Wiggle", "WIGGLE", BASE_ID + 152, "Music Lab - 140 Point Chest"),
)

CASSETTE_BY_ITEM = {entry.item_name: entry for entry in CASSETTES}
CASSETTE_BY_NATIVE_SONG = {entry.native_song: entry for entry in CASSETTES}
NEW_CASSETTE_SOURCE_IDS = {
    entry.source_name: entry.source_id
    for entry in CASSETTES
    if not entry.reused_location
}


def _first_duplicate(values: Sequence[object]) -> object | None:
    seen: set[object] = set()
    for value in values:
        if value in seen:
            return value
        seen.add(value)
    return None


def validate_cassette_catalog(
    registered_medal_songs: Sequence[str],
    registered_locations: Mapping[str, int],
    *,
    catalog: tuple[CassetteDefinition, ...] = CASSETTES,
) -> None:
    """Reject catalog drift before IDs are exposed through the datapackage."""
    if len(catalog) != 30:
        raise ValueError(f"cassette catalog count is {len(catalog)}, expected 30")

    for label, values in (
        ("display song", [entry.display_song for entry in catalog]),
        ("native song", [entry.native_song for entry in catalog]),
        ("item name", [entry.item_name for entry in catalog]),
        ("item ID", [entry.item_id for entry in catalog]),
        ("source name", [entry.source_name for entry in catalog]),
    ):
        duplicate = _first_duplicate(values)
        if duplicate is not None:
            raise ValueError(f"duplicate cassette {label}: {duplicate}")

    catalog_songs = {entry.display_song for entry in catalog}
    registered_songs = set(registered_medal_songs)
    missing = registered_songs - catalog_songs
    unexpected = catalog_songs - registered_songs
    if missing:
        raise ValueError(f"cassette catalog is missing registered medal song: {sorted(missing)[0]}")
    if unexpected:
        raise ValueError(f"cassette catalog has unregistered medal song: {sorted(unexpected)[0]}")

    source_names_by_id: dict[int, str] = {}
    for entry in catalog:
        if entry.reused_location:
            continue
        if entry.source_id is None:
            continue
        original_source = source_names_by_id.get(entry.source_id)
        if original_source is not None:
            raise ValueError(
                f"duplicate new cassette source ID for {entry.source_name}: "
                f"{entry.source_id} (already assigned to {original_source})"
            )
        source_names_by_id[entry.source_id] = entry.source_name

    for source_id, source_name in source_names_by_id.items():
        for registered_name, registered_id in registered_locations.items():
            if registered_id == source_id and registered_name != source_name:
                raise ValueError(
                    f"new cassette source ID collision for {source_name}: "
                    f"{source_id} is already assigned to {registered_name}"
                )

    for entry in catalog:
        if not entry.native_song:
            raise ValueError(f"cassette {entry.display_song} is missing native identity")
        if entry.region not in VALID_REGIONS:
            raise ValueError(f"cassette {entry.display_song} has unsupported region: {entry.region}")
        for trigger in entry.triggers:
            if trigger.region not in VALID_REGIONS:
                raise ValueError(f"cassette {entry.display_song} has unsupported trigger region: {trigger.region}")
        if entry.reused_location:
            if entry.source_name not in registered_locations:
                raise ValueError(f"reused cassette source is not registered: {entry.source_name}")
            if (
                entry.source_id is not None
                and registered_locations[entry.source_name] != entry.source_id
            ):
                raise ValueError(
                    f"reused cassette source ID changed: {entry.source_name}: {entry.source_id}"
                )
        else:
            if entry.source_id is None:
                raise ValueError(f"new cassette source has no ID: {entry.source_name}")
            registered_id = registered_locations.get(entry.source_name)
            if registered_id != entry.source_id:
                raise ValueError(
                    f"new cassette source ID collision for {entry.source_name}: {entry.source_id}"
                )
