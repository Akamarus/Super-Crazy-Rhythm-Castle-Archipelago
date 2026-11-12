from BaseClasses import Item, ItemClassification, Location, Region, Tutorial
from Options import OptionError
from worlds.AutoWorld import WebWorld, World
from worlds.generic.Rules import set_rule

from . import bunker as bunker_logic
from .cassette_bundles import BUNDLE_ITEM_NAME_TO_ID, generate_bundles, bundle_for_cassette

from .difficulty import (
    DIFFICULTY_NAMES,
    filter_locations_for_difficulty,
    location_metadata,
)
from .cassettes import (
    CASSETTES,
    NEW_CASSETTE_SOURCE_IDS,
    validate_cassette_catalog,
)
from .campaign_levels import (
    CAMPAIGN_LEVELS,
    CAMPAIGN_LEVELS_BY_INTERNAL_ID,
    CAMPAIGN_LOCATION_NAME_TO_ID,
    CAMPAIGN_LOCATION_TIERS,
)
from .items import (
    QUEST_ITEM_NAME_TO_ID,
    QUEST_ITEM_CLASSIFICATIONS,
    CHARACTER_QUEST_ITEM_NAME_TO_ID,
    CHARACTER_QUEST_ITEM_CLASSIFICATIONS,
    CHARACTER_QUEST_ITEMS,
    CHARACTER_QUEST_ITEM_LOCATIONS,
    CASSETTE_ITEM_CLASSIFICATIONS,
    CASSETTE_ITEM_NAME_TO_ID,
    MUSIC_LAB_POINT_ITEM_CLASSIFICATIONS,
    MUSIC_LAB_POINT_ITEM_NAME_TO_ID,
    LOBBY_ITEM_CLASSIFICATIONS,
    LOBBY_ITEM_NAME_TO_ID,
    NEW_ITEM_CLASSIFICATIONS,
    NEW_ITEM_NAME_TO_ID,
)
from .items import (
    HYPNO_PAN_ITEM_NAME,
    STAR_ITEM_COUNT,
    STAR_ITEM_NAME,
    VIOLANCE_ITEM_NAME,
)
from .expanded_checks import EXPANDED_CHECKS, EXPANDED_CHECK_LOCATION_NAME_TO_ID
from .pool_capacity import fit_non_filler_pool
from .options import SCRCOptions
from .universal_tracker import tracker_slot_data
from .music_lab_points import (
    MUSIC_LAB_POINT_ITEMS,
    MUSIC_LAB_POINT_POOL,
    MUSIC_LAB_POINT_SCHEMA,
    MUSIC_LAB_POINT_MAX_EFFECTIVE,
    MUSIC_LAB_POINT_TOTAL_INSTANCES,
    MUSIC_LAB_POINT_TOTAL_VALUE,
    MUSIC_LAB_POINT_THRESHOLDS,
    weighted_music_lab_points,
)
from .placement import filler_item_allowed, non_progression_item_allowed
from .star_requirements import generate_star_requirements
from .native_logic import can_complete, source_rule, SAFE_SOURCES, star_eater_requirements
from .optional_routes import BUNKER_PROGRESSION_SOURCES, optional_route
from .starting_areas import (
    STARTING_AREA_NAMES,
    VALIDATED_STARTING_AREAS,
    resolve_starting_area,
)


GAME_NAME = "Super Crazy Rhythm Castle"
BASE_ID = 187256000

GARAGE_SONGS = (
    "Bloody Tears",
    "Gradius Remix",
    "Smooch",
    "Superstar",
    "Vampire Killer",
    "Wag the Dog",
)

GARAGE_STICKER_TIERS = (
    "Bronze",
    "Silver",
    "Gold",
    "Platinum",
)

GARAGE_CARTRIDGE_ITEMS = {
    "Bloody Tears": "Bloody Tears Cartridge",
    "Gradius Remix": "Gradius Remix Cartridge",
    "Smooch": "Smooch Cartridge",
    "Superstar": "Superstar Cartridge",
    "Vampire Killer": "Vampire Killer Cartridge",
    "Wag the Dog": "Wag the Dog Cartridge",
}

VANILLA_GARAGE_CARTRIDGE_SONG = "Vampire Killer"
VANILLA_GARAGE_CARTRIDGE_ITEM = GARAGE_CARTRIDGE_ITEMS[VANILLA_GARAGE_CARTRIDGE_SONG]
RANDOMIZED_GARAGE_CARTRIDGE_ITEMS = {
    song: item
    for song, item in GARAGE_CARTRIDGE_ITEMS.items()
    if song != VANILLA_GARAGE_CARTRIDGE_SONG
}

CASSETTE_SONGS = (
    "The Little Things",
    "No Plan B",
    "Jolt City",
    "Quieres Bailar",
    "Quicksand",
    "Gold",
    "I Got Money",
    "Hippo and Frog",
    "On the Way",
    "Badass",
    "Heavy Metal",
    "AOK",
    "Rainbow Melodies",
    "Sneaking",
    "The Heist",
    "Money",
    "Lets Go",
    "Bounce",
    "Epical",
    "Hollywood Trailer",
    "False Data",
    "Gotta Get Up",
    "Fumblin Around",
    "Party Non Stop",
    "Keep On Hustlin",
    "Another Day In Paradise",
    "Flamenco",
    "Ten-Four Good Buddy",
    "Zen",
    "Wiggle",
)

CASSETTE_MEDAL_TIERS = (
    "Bronze",
    "Silver",
    "Gold",
    "Platinum",
)

AREA_ACCESS_ITEMS = (
    "Roots Access",
    "Lobby Access",
    "Meat Dimension Access",
    "Cell Tower Access",
    "Tower of Fear Access",
    "Royal Corridor Access",
)

AREA_ITEM_TO_REGION = {
    "Roots Access": "Roots",
    "Lobby Access": "Lobby",
    "Meat Dimension Access": "Meat Dimension",
    "Cell Tower Access": "Cell Tower",
    "Tower of Fear Access": "Tower of Fear",
    "Royal Corridor Access": "Royal Corridor",
}

# Preserve every ID from GateTest v0.2. New Music Lab checks start at +21,
# leaving the historical +4..+10 gap untouched.
LOCATION_NAME_TO_ID = {
    "Development Cache 01": BASE_ID + 11,
    "Development Cache 02": BASE_ID + 12,
    "Development Cache 03": BASE_ID + 13,
    "Development Cache 04": BASE_ID + 14,
    "Development Cache 05": BASE_ID + 15,
    "Development Cache 06": BASE_ID + 16,
    "Development Cache 07": BASE_ID + 17,
    "Development Cache 08": BASE_ID + 18,
    "Development Cache 09": BASE_ID + 19,
    "Development Cache 10": BASE_ID + 20,
}
LOCATION_NAME_TO_ID.update(CAMPAIGN_LOCATION_NAME_TO_ID)

GARAGE_LOCATION_START = BASE_ID + 21
for song_index, song in enumerate(GARAGE_SONGS):
    for tier_index, tier in enumerate(GARAGE_STICKER_TIERS):
        LOCATION_NAME_TO_ID[f"Game Garage - {song} - {tier}"] = (
            GARAGE_LOCATION_START + song_index * len(GARAGE_STICKER_TIERS) + tier_index
        )

MUSIC_LAB_5_POINT_CHEST = "Music Lab - 5 Point Chest"
MUSIC_LAB_10_POINT_CHEST = "Music Lab - 10 Point Chest"
MUSIC_LAB_20_POINT_CHEST = "Music Lab - 20 Point Chest"
MUSIC_LAB_32_POINT_CHEST = "Music Lab - 32 Point Chest"
MUSIC_LAB_46_POINT_CHEST = "Music Lab - 46 Point Chest"
MUSIC_LAB_64_POINT_CHEST = "Music Lab - 64 Point Chest"
LOCATION_NAME_TO_ID[MUSIC_LAB_64_POINT_CHEST] = BASE_ID + 45

CASSETTE_LOCATION_START = BASE_ID + 46
for song_index, song in enumerate(CASSETTE_SONGS):
    for tier_index, tier in enumerate(CASSETTE_MEDAL_TIERS):
        LOCATION_NAME_TO_ID[f"Music Lab Cassette - {song} - {tier}"] = (
            CASSETTE_LOCATION_START + song_index * len(CASSETTE_MEDAL_TIERS) + tier_index
        )

MUSIC_LAB_89_POINT_CHEST = "Music Lab - 89 Point Chest"
MUSIC_LAB_111_POINT_CHEST = "Music Lab - 111 Point Chest"
MUSIC_LAB_140_POINT_CHEST = "Music Lab - 140 Point Chest"

LOCATION_NAME_TO_ID[MUSIC_LAB_89_POINT_CHEST] = BASE_ID + 166
LOCATION_NAME_TO_ID[MUSIC_LAB_111_POINT_CHEST] = BASE_ID + 167
LOCATION_NAME_TO_ID[MUSIC_LAB_140_POINT_CHEST] = BASE_ID + 168

# v0.10 appends the five earlier reward chests without moving any existing ID.
LOCATION_NAME_TO_ID[MUSIC_LAB_5_POINT_CHEST] = BASE_ID + 169
LOCATION_NAME_TO_ID[MUSIC_LAB_10_POINT_CHEST] = BASE_ID + 170
LOCATION_NAME_TO_ID[MUSIC_LAB_20_POINT_CHEST] = BASE_ID + 171
LOCATION_NAME_TO_ID[MUSIC_LAB_32_POINT_CHEST] = BASE_ID + 172
LOCATION_NAME_TO_ID[MUSIC_LAB_46_POINT_CHEST] = BASE_ID + 173

# v0.13 converts the four non-Music-Lab vanilla cartridge sources into AP checks.
# Gradius Remix and Bloody Tears already use the existing 10/46-point Music Lab
# chest locations, so creating duplicate cartridge-source checks for them would be wrong.
CARTRIDGE_SOURCE_LOCATIONS = {
    "Smooch": "Cartridge Pickup - Smooch",
    "Superstar": "Cartridge Pickup - Superstar",
    "Vampire Killer": "Cartridge Pickup - Vampire Killer",
    "Wag the Dog": "Cartridge Pickup - Wag the Dog",
}
RANDOMIZED_CARTRIDGE_SOURCE_LOCATIONS = {
    song: location
    for song, location in CARTRIDGE_SOURCE_LOCATIONS.items()
    if song != VANILLA_GARAGE_CARTRIDGE_SONG
}
for index, name in enumerate(CARTRIDGE_SOURCE_LOCATIONS.values()):
    LOCATION_NAME_TO_ID[name] = BASE_ID + 174 + index

# v0.14: Gecko's native Weed Killer reward becomes a real Roots AP source check.
ROOTS_GECKO_WEED_KILLER = "Roots - Gecko's Weed Killer"
LOCATION_NAME_TO_ID[ROOTS_GECKO_WEED_KILLER] = BASE_ID + 178

# v0.15: Frog and Hippo's Level 3 Plant Pipes source becomes an AP check.
# The source is reachable after entering Level 3 with Weed Killer; Plant Pipes
# itself is only required to complete Level 3, not to reach this check.
ROOTS_LEVEL3_FROG_HIPPO = "Roots - Level 3 - Plant Pipes Pickup"
LOCATION_NAME_TO_ID[ROOTS_LEVEL3_FROG_HIPPO] = BASE_ID + 179

# v0.17: Level 4's Hip Glasses reward and the Bucket Minion trade become AP checks.
ROOTS_LEVEL4_HIP_GLASSES = "Roots - Level 4 - Hip Glasses"
ROOTS_BUCKET_MINION_TRADE = "Roots - Bucket Minion Trade"
LOCATION_NAME_TO_ID[ROOTS_LEVEL4_HIP_GLASSES] = BASE_ID + 180
LOCATION_NAME_TO_ID[ROOTS_BUCKET_MINION_TRADE] = BASE_ID + 181

LEVEL_2_MONEY_CASSETTE_SOURCE = "Level 2 - Money Cassette"
LOCATION_NAME_TO_ID[LEVEL_2_MONEY_CASSETTE_SOURCE] = BASE_ID + 186

# Full cassette randomization appends only genuinely new physical source
# locations.  The six reused sources (including Money's pilot location) retain
# their established locations and never receive duplicate checks.
LOCATION_NAME_TO_ID.update(NEW_CASSETTE_SOURCE_IDS)
validate_cassette_catalog(CASSETTE_SONGS, LOCATION_NAME_TO_ID)

LOBBY_LETTERS_PICKUP = "Lobby - Important Letters Pickup"
LOBBY_BEAN_TRUMPET_AWARD = "Lobby - Bean Trumpet Award"
LOCATION_NAME_TO_ID[LOBBY_LETTERS_PICKUP] = BASE_ID + 292
LOCATION_NAME_TO_ID[LOBBY_BEAN_TRUMPET_AWARD] = BASE_ID + 293

# These four physical sources keep the IDs assigned before AP Stars were added.
QUEST_LOCATION_NAME_TO_ID = {
    "Lobby - Plunger Pickup": BASE_ID + 294,
    "Lobby - Car Battery Hand-In": BASE_ID + 295,
    "Game Garage - Old Game Data Hand-In": BASE_ID + 296,
    "Roots - Star Eater Fed": BASE_ID + 297,
}
LOCATION_NAME_TO_ID.update(QUEST_LOCATION_NAME_TO_ID)
LOCATION_NAME_TO_ID.update(EXPANDED_CHECK_LOCATION_NAME_TO_ID)
LOCATION_NAME_TO_ID.update(bunker_logic.CHECK_IDS)


def _active_check_names(difficulty: int, bunker_progression: bool) -> frozenset[str]:
    # The datapackage keeps published IDs for reserved checks. Generation and
    # Tracker must apply the same exclusions when reconstructing a seed.
    return frozenset(
        name
        for name in filter_locations_for_difficulty(LOCATION_NAME_TO_ID, difficulty)
        if name != CARTRIDGE_SOURCE_LOCATIONS[VANILLA_GARAGE_CARTRIDGE_SONG]
        and name != LOBBY_BEAN_TRUMPET_AWARD
        and not name.startswith("Development Cache ")
        and (bunker_progression or name not in bunker_logic.CHECK_IDS)
    )


MUSIC_LAB_REWARD_CHEST_LOCATIONS = (
    MUSIC_LAB_5_POINT_CHEST,
    MUSIC_LAB_10_POINT_CHEST,
    MUSIC_LAB_20_POINT_CHEST,
    MUSIC_LAB_32_POINT_CHEST,
    MUSIC_LAB_46_POINT_CHEST,
    MUSIC_LAB_64_POINT_CHEST,
    MUSIC_LAB_89_POINT_CHEST,
    MUSIC_LAB_111_POINT_CHEST,
    MUSIC_LAB_140_POINT_CHEST,
)

SPECIAL_COMPLETION_BY_VARIANT = {
    (entry.level, entry.variant): entry.name for entry in EXPANDED_CHECKS if entry.variant
}
CASSETTE_ITEM_BY_SONG = {entry.display_song: entry.item_name for entry in CASSETTES}

ITEM_NAME_TO_ID = {
    # Historical development item IDs are preserved even though Level 2/3
    # Access are no longer generated by v0.13.
    "Level 2 Access": BASE_ID + 101,
    "Level 3 Access": BASE_ID + 102,
    "Stardust": BASE_ID + 103,
    "Roots Access": BASE_ID + 104,
    "Lobby Access": BASE_ID + 105,
    "Meat Dimension Access": BASE_ID + 106,
    "Cell Tower Access": BASE_ID + 107,
    "Tower of Fear Access": BASE_ID + 108,
    "Royal Corridor Access": BASE_ID + 109,
    "Bloody Tears Cartridge": BASE_ID + 110,
    "Gradius Remix Cartridge": BASE_ID + 111,
    "Smooch Cartridge": BASE_ID + 112,
    "Superstar Cartridge": BASE_ID + 113,
    "Vampire Killer Cartridge": BASE_ID + 114,
    "Wag the Dog Cartridge": BASE_ID + 115,
    "Weed Killer": BASE_ID + 116,
    "Plant Pipes": BASE_ID + 117,
    "Hip Glasses": BASE_ID + 119,
    "Chicken Bucket": BASE_ID + 120,
    **NEW_ITEM_NAME_TO_ID,
    **CASSETTE_ITEM_NAME_TO_ID,
    **MUSIC_LAB_POINT_ITEM_NAME_TO_ID,
    **LOBBY_ITEM_NAME_TO_ID,
    **CHARACTER_QUEST_ITEM_NAME_TO_ID,
    **QUEST_ITEM_NAME_TO_ID,
    **BUNDLE_ITEM_NAME_TO_ID,
    **bunker_logic.ITEM_IDS,
}

HIP_GLASSES_ITEM = "Hip Glasses"
CHICKEN_BUCKET_ITEM = "Chicken Bucket"

ITEM_CLASSIFICATIONS = {
    "Level 2 Access": ItemClassification.progression,
    "Level 3 Access": ItemClassification.progression,
    "Stardust": ItemClassification.filler,
    **{name: ItemClassification.progression for name in AREA_ACCESS_ITEMS},
    **{name: ItemClassification.progression for name in GARAGE_CARTRIDGE_ITEMS.values()},
    "Weed Killer": ItemClassification.progression,
    "Plant Pipes": ItemClassification.progression,
    HIP_GLASSES_ITEM: ItemClassification.progression,
    CHICKEN_BUCKET_ITEM: ItemClassification.progression,
    **NEW_ITEM_CLASSIFICATIONS,
    **CASSETTE_ITEM_CLASSIFICATIONS,
    **MUSIC_LAB_POINT_ITEM_CLASSIFICATIONS,
    **LOBBY_ITEM_CLASSIFICATIONS,
    **CHARACTER_QUEST_ITEM_CLASSIFICATIONS,
    **QUEST_ITEM_CLASSIFICATIONS,
    **{name: ItemClassification.progression for name in BUNDLE_ITEM_NAME_TO_ID},
    **{name: ItemClassification.progression for name in bunker_logic.ITEM_IDS},
}


class SCRCItem(Item):
    game = GAME_NAME


class SCRCLocation(Location):
    game = GAME_NAME


class SCRCWebWorld(WebWorld):
    game = GAME_NAME
    theme = "partyTime"

    setup_en = Tutorial(
        "Multiworld Setup Guide",
        "Development setup guide for Super Crazy Rhythm Castle Archipelago.",
        "English",
        "setup_en.md",
        "setup/en",
        ["Jack", "OpenAI"],
    )

    tutorials = [setup_en]


class SCRCWorld(World):
    """Generate castle progression and reconstruct published seeds for Universal Tracker."""

    game = GAME_NAME
    web = SCRCWebWorld()

    options_dataclass = SCRCOptions
    options: SCRCOptions

    item_name_to_id = ITEM_NAME_TO_ID
    location_name_to_id = LOCATION_NAME_TO_ID

    ut_can_gen_without_yaml = True

    @staticmethod
    def interpret_slot_data(slot_data: dict) -> dict:
        return tracker_slot_data(slot_data)

    def generate_early(self) -> None:
        passthrough = getattr(self.multiworld, "re_gen_passthrough", {}) or {}
        tracker = tracker_slot_data(passthrough[self.game]) if self.game in passthrough else None
        self.bunker_progression = tracker["bunker_progression"] if tracker is not None else bool(getattr(getattr(self.options, "bunker_progression", None), "value", 0))
        self.goal = tracker["goal"] if tracker is not None else bunker_logic.GOALS[getattr(getattr(self.options, "goal", None), "value", 0)]
        if self.goal == "true_ending" and not self.bunker_progression:
            raise OptionError("True Ending requires Bunker Progression enabled")
        self.cell_tower_open_route = tracker["cell_tower_open_route"] if tracker is not None else True
        self.lobby_open_doors = tracker["lobby_open_doors"] if tracker is not None else True
        if tracker is not None:
            self.options.required_stars.value = tracker["required_stars"]
            self.options.difficulty.value = tracker["difficulty"]["value"]
            self.options.starting_area.value = tracker["starting_area_option"]
        requested_start = int(self.options.starting_area.value)
        required_stars = int(self.options.required_stars.value)
        if self.bunker_progression and required_stars < 2:
            raise OptionError("Bunker Progression requires required_stars >= 2 so its Star Eater can be below Level 22")
        difficulty = self.options.difficulty.value
        self.active_location_names = _active_check_names(difficulty, self.bunker_progression)

        if tracker is not None:
            self.starting_area_item = tracker["starting_area_item"]
            self.generated_star_requirements = tracker["generated_star_requirements"]
        else:
            self.starting_area_item = resolve_starting_area(requested_start, self.random)
            self.generated_star_requirements = generate_star_requirements(
                required_stars, self.random
            )
        if self.bunker_progression and tracker is None:
            self.generated_star_requirements["Level 22"] = max(1, self.generated_star_requirements["Level 22"])
        self.cassette_bundle_size = (tracker["cassette_bundle_size"] if tracker is not None else
            getattr(getattr(self.options, "cassette_bundle_size", None), "value", 1))
        self.cassette_bundles = (tracker["cassette_bundles"] if tracker is not None else
            generate_bundles(self.cassette_bundle_size, self.random))
        if hasattr(self.options, "cassette_bundle_size"):
            self.options.cassette_bundle_size.value = self.cassette_bundle_size
        self.multiworld.push_precollected(self.create_item(self.starting_area_item))

    def create_regions(self) -> None:
        def is_active(name: str) -> bool:
            if name not in LOCATION_NAME_TO_ID:
                raise ValueError(f"unregistered active location: {name}")
            return name in self.active_location_names

        # Archipelago core begins reachability sweeps from a region literally
        # named "Menu". Keep that logical root even though the in-game start
        # is Hub6 / Phone Hub.
        menu = Region("Menu", self.player, self.multiworld)
        phone_hub = Region("Phone Hub", self.player, self.multiworld)
        music_lab = Region("Music Lab", self.player, self.multiworld)
        game_garage = Region("Game Garage", self.player, self.multiworld)
        roots = Region("Roots", self.player, self.multiworld)
        lobby = Region("Lobby", self.player, self.multiworld)
        meat = Region("Meat Dimension", self.player, self.multiworld)
        cell = Region("Cell Tower", self.player, self.multiworld)
        tower = Region("Tower of Fear", self.player, self.multiworld)
        royal = Region("Royal Corridor", self.player, self.multiworld)

        for name in (
            ROOTS_GECKO_WEED_KILLER, ROOTS_LEVEL3_FROG_HIPPO,
            ROOTS_LEVEL4_HIP_GLASSES, ROOTS_BUCKET_MINION_TRADE,
        ):
            roots.locations.append(SCRCLocation(self.player, name, LOCATION_NAME_TO_ID[name], roots))

        bucket_trade_event = SCRCLocation(
            self.player,
            "Bucket Minion Trade Complete",
            None,
            roots,
        )
        bucket_trade_event.place_locked_item(
            SCRCItem(
                "Bucket Minion Trade Complete",
                ItemClassification.progression,
                None,
                self.player,
            )
        )
        roots.locations.append(bucket_trade_event)

        # Letters is now one passive source in EXPANDED_CHECKS. Bean Trumpet's
        # second reward from that same interaction remains a reserved source.

        for name, parent, hand_in in (
            ("Lobby - Plunger Pickup", lobby, None),
            ("Lobby - Car Battery Hand-In", lobby, "Car Battery"),
            ("Game Garage - Old Game Data Hand-In", game_garage, "Old Game Data"),
            ("Roots - Star Eater Fed", roots, None),
        ):
            if not is_active(name):
                continue
            location = SCRCLocation(self.player, name, LOCATION_NAME_TO_ID[name], parent)
            if hand_in is None:
                set_rule(location, lambda state, name=name: source_rule(self, name, state))
            else:
                set_rule(location, lambda state, item=hand_in: state.has(item, self.player))
            parent.locations.append(location)

        combo_bucket_event = SCRCLocation(
            self.player,
            "Combo Bucket Event",
            None,
            roots,
        )
        combo_bucket_event.place_locked_item(
            SCRCItem(
                "Combo Bucket Event",
                ItemClassification.progression,
                None,
                self.player,
            )
        )
        roots.locations.append(combo_bucket_event)

        for name in RANDOMIZED_CARTRIDGE_SOURCE_LOCATIONS.values():
            location = SCRCLocation(self.player, name, LOCATION_NAME_TO_ID[name], phone_hub)
            self._set_optional_route(location)
            phone_hub.locations.append(location)

        for chest_name in MUSIC_LAB_REWARD_CHEST_LOCATIONS:
            location = SCRCLocation(
                self.player,
                chest_name,
                LOCATION_NAME_TO_ID[chest_name],
                music_lab,
            )
            music_lab.locations.append(location)

        concrete_regions = {
            "Cell Tower": cell,
            "Lobby": lobby,
            "Meat Dimension": meat,
            "Music Lab": music_lab,
            "Roots": roots,
            "Royal Corridor": royal,
            "Tower of Fear": tower,
        }

        # Legacy seeds model the native keycard at individual checks. Schema 21
        # gates the whole region with the randomized keycard and its Lobby route.
        bunker = Region("Secret Bunker", self.player, self.multiworld)
        lobby.connect(bunker, "Lobby -> Secret Bunker",
            (lambda state: bunker_logic.can_enter(self,state)) if self.bunker_progression else None)
        concrete_regions["Secret Bunker"] = bunker

        if self.bunker_progression:
            for name, address in bunker_logic.CHECK_IDS.items():
                location = SCRCLocation(self.player, name, address, bunker)
                set_rule(location, lambda state, name=name: bunker_logic.rule(self,name,state))
                bunker.locations.append(location)

        for entry in EXPANDED_CHECKS:
            if not is_active(entry.name):
                continue
            parent = concrete_regions[entry.region]
            source = SCRCLocation(self.player, entry.name, entry.location_id, parent)
            if entry.name in SAFE_SOURCES:
                set_rule(source, lambda state, name=entry.name: source_rule(self, name, state))
            else:
                self._set_optional_route(source)
            parent.locations.append(source)

        for level in CAMPAIGN_LEVELS:
            for tier in CAMPAIGN_LOCATION_TIERS:
                name = level.location_name(tier)
                if not is_active(name):
                    continue
                location = SCRCLocation(
                    self.player,
                    name,
                    LOCATION_NAME_TO_ID[name],
                    concrete_regions[level.area],
                )
                if level.number == 16 and tier in ("2 Stars", "3 Stars"):
                    set_rule(location, lambda state: can_complete(self, state, 16)
                             and state.has("Plant Pipes", self.player))
                else:
                    set_rule(location, lambda state, n=level.number: can_complete(self, state, n))
                if level.number == 22 and tier in ("2 Stars", "3 Stars"):
                    # Higher boss ratings remain outside the verified performance model.
                    location.item_rule = non_progression_item_allowed
                concrete_regions[level.area].locations.append(location)

        cassette_source_regions = {}

        def route_is_open(state, triggers):
            return any(
                all(
                    state.has(requirement.item, self.player)
                    for requirement in trigger.requirements
                )
                for trigger in triggers
            )

        for entry in CASSETTES:
            if entry.source_type == "Music Lab point chest":
                continue

            if all(trigger.region == "Secret Bunker" for trigger in entry.triggers):
                raise ValueError(
                    f"cassette source has no active non-Bunker route: {entry.source_name}"
                )

            parent = concrete_regions.get(entry.region)
            if parent is None:
                parent = cassette_source_regions.get(entry.region)
                if parent is None:
                    parent = Region(entry.region, self.player, self.multiworld)
                    cassette_source_regions[entry.region] = parent
                    matching_triggers = tuple(
                        trigger
                        for cassette in CASSETTES
                        if cassette.region == entry.region
                        for trigger in cassette.triggers
                        if trigger.region != "Secret Bunker"
                    )
                    phone_hub.connect(
                        parent,
                        f"Phone Hub -> {entry.region}",
                        lambda state, triggers=matching_triggers: route_is_open(state, triggers),
                    )

            source = SCRCLocation(
                self.player,
                entry.source_name,
                LOCATION_NAME_TO_ID[entry.source_name],
                parent,
            )
            set_rule(source, lambda state, triggers=entry.triggers:
                     any(self._cassette_route(state, trigger) for trigger in triggers))
            parent.locations.append(source)

        bundle_by_item = bundle_for_cassette(self.cassette_bundles)
        for song in CASSETTE_SONGS:
            cassette_item = CASSETTE_ITEM_BY_SONG[song]
            bundle = bundle_by_item.get(cassette_item)

            def can_play_song(state, item=cassette_item, bundle=bundle):
                return state.has(item, self.player) or (bundle is not None and state.has(bundle, self.player))

            for tier in CASSETTE_MEDAL_TIERS:
                name = f"Music Lab Cassette - {song} - {tier}"
                if not is_active(name):
                    continue
                location = SCRCLocation(self.player, name, LOCATION_NAME_TO_ID[name], music_lab)
                set_rule(location, can_play_song)
                music_lab.locations.append(location)

        for song in GARAGE_SONGS:
            cartridge_item = GARAGE_CARTRIDGE_ITEMS[song]
            for tier in GARAGE_STICKER_TIERS:
                name = f"Game Garage - {song} - {tier}"
                if not is_active(name):
                    continue
                location = SCRCLocation(
                    self.player, name, LOCATION_NAME_TO_ID[name], game_garage
                )
                if song != VANILLA_GARAGE_CARTRIDGE_SONG:
                    set_rule(
                        location,
                        lambda state, item=cartridge_item: state.has(item, self.player),
                    )
                game_garage.locations.append(location)

        # AP core root -> in-game home base. This connection is always free.
        menu.connect(phone_hub, "Menu -> Phone Hub")

        # Music Lab + Garage are permanent home-base side regions.
        phone_hub.connect(music_lab, "Phone Hub -> Music Lab")
        phone_hub.connect(game_garage, "Phone Hub -> Game Garage")

        # The six red phones are represented as six independently gated
        # connections from Hub6. The client enforces the same items physically.
        for access_item, region_name in AREA_ITEM_TO_REGION.items():
            phone_hub.connect(
                concrete_regions[region_name],
                f"Phone Hub -> {region_name}",
                lambda state, item=access_item: state.has(item, self.player),
            )

        victory = SCRCLocation(self.player, "Victory", None, royal)
        victory.place_locked_item(
            SCRCItem("Victory", ItemClassification.progression, None, self.player)
        )
        royal.locations.append(victory)

        self.multiworld.regions += [
            menu,
            phone_hub,
            music_lab,
            game_garage,
            roots,
            lobby,
            meat,
            cell,
            tower,
            royal,
            bunker,
            *cassette_source_regions.values(),
        ]
        # Frog and Hippo are reachable inside Level 3 before Plant Pipes are
        # needed for the clear. The player can take the pickup and menu out.
        frog = self.multiworld.get_location(ROOTS_LEVEL3_FROG_HIPPO, self.player)
        set_rule(frog, lambda state: can_complete(self, state, 2)
            and state.has("Weed Killer", self.player)
            and state.count(STAR_ITEM_NAME, self.player) >= self.generated_star_requirements["Level 3"])
        set_rule(self.multiworld.get_location(ROOTS_LEVEL4_HIP_GLASSES, self.player),
                 lambda state: can_complete(self, state, 4))
        for name in (ROOTS_BUCKET_MINION_TRADE, "Bucket Minion Trade Complete"):
            set_rule(self.multiworld.get_location(name, self.player),
                lambda state: state.has('Roots Access', self.player) and state.has(HIP_GLASSES_ITEM, self.player))
        set_rule(self.multiworld.get_location("Combo Bucket Event", self.player),
            lambda state: source_rule(self, "Roots - Combo Bucket Conversion", state))

    def _set_optional_route(self, source) -> None:
        set_rule(source, lambda state, name=source.name: optional_route(self, name, state))
        # Set placement restrictions before create_items counts eligible slots.
        # Schema 21 allows progression at the modeled Bunker and Devil checks.
        if not (self.bunker_progression and source.name in BUNKER_PROGRESSION_SOURCES):
            source.item_rule = filler_item_allowed

    def _cassette_route(self, state, trigger) -> bool:
        if trigger.level == "Level_09" and trigger.variant == "LevelVariant_Default":
            # Forward-route inventory must not suppress the Lobby entrance to Lift Quest.
            return can_complete(self, state, 5)
        if not all(state.has(r.item, self.player) for r in trigger.requirements):
            return False
        if trigger.variant == "LevelVariant_Default":
            level = CAMPAIGN_LEVELS_BY_INTERNAL_ID[trigger.level]
            return can_complete(self, state, level.number)
        check = SPECIAL_COMPLETION_BY_VARIANT.get((trigger.level, trigger.variant))
        return check is not None and optional_route(self, check, state)

    def fill_hook(self, progitempool, usefulitempool, filleritempool, fill_locations):
        # Reverse fill consumes the end first. Place counted currencies and
        # single-song unlocks while broad access items are still assumed.
        # Only reorder this player's positions; preserve every item instance
        # and other worlds' relative fill order.
        def priority(item):
            if item.name == STAR_ITEM_NAME:
                return 3
            if item.name in MUSIC_LAB_POINT_ITEM_NAME_TO_ID:
                return 2
            if item.name in ("Old Game Data", "Car Battery") or item.name.endswith((" Cassette", " Cartridge")) or item.name in BUNDLE_ITEM_NAME_TO_ID:
                return 1
            return 0

        indexes = [i for i, item in enumerate(progitempool) if item.player == self.player]
        own_items = sorted((progitempool[i] for i in indexes), key=priority)
        for index, item in zip(indexes, own_items):
            progitempool[index] = item

    def create_items(self) -> None:
        starter = getattr(self, "starting_area_item", AREA_ACCESS_ITEMS[0])

        # Starter is precollected in generate_early; only the other five
        # Area Access items occupy randomized locations.
        progression_items = []
        for name in AREA_ACCESS_ITEMS:
            if name != starter:
                progression_items.append(name)

        # Vampire Killer remains a permanent datapackage item for old seeds, but
        # v0.21 grants it natively so the Garage entrance sequence is always valid.
        progression_items.extend(RANDOMIZED_GARAGE_CARTRIDGE_ITEMS.values())

        # First meaningful vanilla quest item randomized by the area-routing world.
        # Gecko's source is reachable with Roots Access alone, so Weed Killer can
        # safely be progression and may be placed anywhere logic can reach.
        progression_items.append("Weed Killer")

        # Plant Pipes is obtained at Frog/Hippo inside Level 3 in vanilla. AP logic
        # allows that source with Weed Killer alone, but requires Plant Pipes for
        # Level 3 Completion. This permits the intended menu-exit partial-level route.
        progression_items.append("Plant Pipes")

        progression_items.append(HIP_GLASSES_ITEM)
        progression_items.append(CHICKEN_BUCKET_ITEM)
        progression_items.append(HYPNO_PAN_ITEM_NAME)
        progression_items.append(VIOLANCE_ITEM_NAME)
        progression_items.extend(self.cassette_bundles if self.cassette_bundles else
                                 (entry.item_name for entry in CASSETTES))
        progression_items.extend(MUSIC_LAB_POINT_POOL)
        # Character hand-in inputs and randomized quest rewards each appear once.
        progression_items.extend(CHARACTER_QUEST_ITEM_NAME_TO_ID)
        progression_items.extend(QUEST_ITEM_NAME_TO_ID)
        if self.bunker_progression:
            progression_items.extend(bunker_logic.ITEM_IDS)

        progression_items.extend([STAR_ITEM_NAME] * STAR_ITEM_COUNT)

        unfilled = [
            location
            for location in self.multiworld.get_locations(self.player)
            if location.address is not None and location.item is None
        ]
        # A Star tests which slots accept progression. fit_non_filler_pool
        # counts those slots after exclusions and preserves the total point value.
        star = self.create_item(STAR_ITEM_NAME)
        eligible_names = [location.name for location in unfilled if location.item_rule(star)]
        excluded = getattr(getattr(self.options, "exclude_locations", None), "value", set())
        progression_items = fit_non_filler_pool(progression_items, eligible_names, excluded)
        self.music_lab_point_pool_counts = {
            entry.name: progression_items.count(entry.name) for entry in MUSIC_LAB_POINT_ITEMS}
        filler_count = len(unfilled) - len(progression_items)

        for name in progression_items:
            self.multiworld.itempool.append(self.create_item(name))

        for _ in range(filler_count):
            self.multiworld.itempool.append(self.create_item("Stardust"))

    def create_item(self, name: str) -> SCRCItem:
        if name in ("Victory", "Bucket Minion Trade Complete", "Combo Bucket Event"):
            return SCRCItem(name, ItemClassification.progression, None, self.player)

        return SCRCItem(
            name,
            ITEM_CLASSIFICATIONS[name],
            ITEM_NAME_TO_ID[name],
            self.player,
        )

    def get_filler_item_name(self) -> str:
        return "Stardust"

    def set_rules(self) -> None:
        set_rule(self.multiworld.get_location("Victory", self.player),
            lambda state: (bunker_logic.can_finish(self,state) if self.goal == "true_ending" else can_complete(self, state, 22))
            and state.count(STAR_ITEM_NAME, self.player) >= int(self.options.required_stars.value))

        self.multiworld.completion_condition[self.player] = (
            lambda state: state.has("Victory", self.player)
        )

        for threshold, location_name in MUSIC_LAB_POINT_THRESHOLDS.items():
            set_rule(
                self.multiworld.get_location(location_name, self.player),
                lambda state, required=threshold: (
                    weighted_music_lab_points(state, self.player) >= required
                ),
            )

    def fill_slot_data(self) -> dict:
        starter = getattr(self, "starting_area_item", AREA_ACCESS_ITEMS[0])
        options = getattr(self, "options", None)
        required_stars = int(
            getattr(getattr(options, "required_stars", None), "value", 50)
        )
        difficulty_value = getattr(getattr(options, "difficulty", None), "value", 0)
        requested_start = int(
            getattr(getattr(options, "starting_area", None), "value", 0)
        )
        generated_requirements = getattr(self, "generated_star_requirements", {})
        multiworld = getattr(self, "multiworld", None)
        if multiworld is None:
            if hasattr(self, "active_location_names"):
                instantiated_addressed_names = self.active_location_names
            else:
                instantiated_addressed_names = _active_check_names(
                    difficulty_value, getattr(self, "bunker_progression", False)
                )
        else:
            instantiated_addressed_names = {
                location.name
                for location in multiworld.get_locations(self.player)
                if location.address is not None
            }
        active_metadata = location_metadata(instantiated_addressed_names, difficulty_value)
        return {
            "implementation_version": "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23-full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28".replace("-generation-foundation", ("-cell-tower-path-1" if getattr(self, "cell_tower_open_route", False) else "") + ("-lobby-doors-1" if getattr(self, "lobby_open_doors", False) else "") + ("-bunker-1" if getattr(self, "bunker_progression", False) else "") + "-generation-foundation"),
            "lobby_open_doors": getattr(self, "lobby_open_doors", False),
            "cell_tower_open_route": getattr(self, "cell_tower_open_route", False),
            "generation_foundation_version": "generation-foundation-0.16",
            **bunker_logic.metadata(getattr(self,"bunker_progression",False), getattr(self,"goal","king_ferdinand")),
            "schema_version": 21 if getattr(self,"bunker_progression",False) else (19 if getattr(self, "cassette_bundle_size", 1) == 1 else 20),
            "cassette_bundle_size": getattr(self, "cassette_bundle_size", 1),
            "cassette_bundles": {name: list(songs) for name, songs in getattr(self, "cassette_bundles", {}).items()},
            "expanded_checks_schema": 1,
            "expanded_check_locations": dict(EXPANDED_CHECK_LOCATION_NAME_TO_ID),
            "expanded_special_completions_active": True,
            "expanded_check_native_rewards_preserved": True,
            "quest_checks_schema": 1,
            "quest_items": dict(QUEST_ITEM_NAME_TO_ID),
            "quest_locations": dict(QUEST_LOCATION_NAME_TO_ID),
            "character_quest_item_schema": 1,
            "randomize_character_quest_items": True,
            "character_quest_items": dict(CHARACTER_QUEST_ITEMS),
            "character_quest_item_locations": dict(CHARACTER_QUEST_ITEM_LOCATIONS),
            "required_stars": required_stars,
            "difficulty": {
                "value": difficulty_value,
                "name": DIFFICULTY_NAMES[difficulty_value],
            },
            "starting_area_requested": STARTING_AREA_NAMES[requested_start],
            "starting_area_resolved": AREA_ITEM_TO_REGION[starter],
            "validated_starting_areas": [
                AREA_ITEM_TO_REGION[item] for item in VALIDATED_STARTING_AREAS
            ],
            "star_item_name": STAR_ITEM_NAME,
            "star_item_count": STAR_ITEM_COUNT,
            "star_items_active": True,
            "star_victory_schema": 1,
            "star_item_id": ITEM_NAME_TO_ID[STAR_ITEM_NAME],
            "star_item_maximum": STAR_ITEM_COUNT,
            "post_threshold_victory_active": True,
            "victory_level_name": "Level 22",
            "victory_level_internal_id": "Level_28",
            "star_eater_requirements": star_eater_requirements(generated_requirements, getattr(self,"bunker_progression",False)) if generated_requirements else {},
            "generated_star_requirements": dict(generated_requirements),
            "generated_star_requirements_depth_model": "conservative-native-routes-v1",
            "client_star_gate_enforcement_active": True,
            **active_metadata,
            "campaign_level_mapping_schema": 1,
            "special_variant_locations_active": False,
            "special_variant_locations": [],
            "development_area_access_victory_active": False,
            "logical_root_region": "Menu",
            "home_region": "Phone Hub",
            "starting_area_item": starter,
            "starting_area": AREA_ITEM_TO_REGION[starter],
            "starting_area_forced": False,
            "area_access_items": list(AREA_ACCESS_ITEMS),
            "always_open_regions": ["Phone Hub", "Music Lab", "Game Garage"],
            "development_cache_count": 0,
            "development_cache_ids_reserved": True,
            "development_caches_filler_only": False,
            "game_garage_song_count": len(GARAGE_SONGS),
            "randomize_game_garage_cartridges": True,
            "game_garage_cartridge_items": dict(RANDOMIZED_GARAGE_CARTRIDGE_ITEMS),
            "randomize_vanilla_cartridge_sources": True,
            "cartridge_source_locations": dict(RANDOMIZED_CARTRIDGE_SOURCE_LOCATIONS),
            "vanilla_game_garage_cartridge": VANILLA_GARAGE_CARTRIDGE_SONG,
            "vanilla_game_garage_cartridge_item": VANILLA_GARAGE_CARTRIDGE_ITEM,
            "game_garage_vanilla_entrance_pickup_required": True,
            "randomize_weed_killer": True,
            "weed_killer_item": "Weed Killer",
            "weed_killer_source_location": ROOTS_GECKO_WEED_KILLER,
            "weed_killer_native_bag_flag": "WEED_KILLER_BAG_ITEM",
            "weed_killer_native_collected_flag": "ROOTS_HUB_WEED_KILLER_COLLECTED",
            "randomize_plant_pipes": True,
            "plant_pipes_item": "Plant Pipes",
            "plant_pipes_source_location": ROOTS_LEVEL3_FROG_HIPPO,
            "plant_pipes_native_ability_flag": "WEED_KILLER_ABILITY",
            "plant_pipes_native_source_marker_flag": "LEVEL_07_WK_ABILITY_EARNED",
            "plant_pipes_source_room": "GameRoom_07",
            "randomize_hip_glasses_chicken_bucket": True,
            "randomize_lobby_letters_bean_trumpet": False,
            "lobby_letters_item": "Important Letters",
            "lobby_bean_trumpet_item": "Bean Trumpet",
            "lobby_letters_source_location": LOBBY_LETTERS_PICKUP,
            "lobby_bean_trumpet_source_location": LOBBY_BEAN_TRUMPET_AWARD,
            "lobby_letters_native_flag": "LOBBY_HUB_MENIAL_TASK_ITEM",
            "lobby_letters_collected_flag": "LOBBY_HUB_MENIAL_TASK_ITEM_COLLECTED",
            "lobby_letters_deposited_flag": "LOBBY_HUB_MENIAL_TASK_ITEM_DEPOSITED",
            "lobby_bean_trumpet_native_flag": "BEAN_TRUMPET_ABILITY",
            "randomize_demolition_certificate": False,
            "randomize_level_2_money_cassette": True,
            "cassette_schema": 1,
            "full_cassette_randomization": True,
            "cassette_count": len(CASSETTES),
            "cassette_items": {**CASSETTE_ITEM_BY_SONG},
            "cassette_sources": {
                cassette.display_song: cassette.source_name for cassette in CASSETTES
            },
            "cassette_reused_locations": {
                cassette.display_song: cassette.source_name
                for cassette in CASSETTES
                if cassette.reused_location
            },
            "music_lab_points_enabled": True,
            "music_lab_points_schema": MUSIC_LAB_POINT_SCHEMA,
            "music_lab_point_items": {
                entry.name: entry.item_id for entry in MUSIC_LAB_POINT_ITEMS
            },
            "music_lab_point_values": {
                entry.name: entry.value for entry in MUSIC_LAB_POINT_ITEMS
            },
            "music_lab_point_counts": {
                entry.name: entry.count for entry in MUSIC_LAB_POINT_ITEMS
            },
            # Nominal catalog remains the legacy client validation contract;
            # actual seed pool counts may consolidate denominations for exclusions.
            "music_lab_point_total_instances": MUSIC_LAB_POINT_TOTAL_INSTANCES,
            "music_lab_point_pool_counts": getattr(self, "music_lab_point_pool_counts",
                {entry.name: entry.count for entry in MUSIC_LAB_POINT_ITEMS}),
            "music_lab_point_total_value": MUSIC_LAB_POINT_TOTAL_VALUE,
            "music_lab_point_max_effective": MUSIC_LAB_POINT_MAX_EFFECTIVE,
            "music_lab_point_thresholds": dict(MUSIC_LAB_POINT_THRESHOLDS),
            "repair_schema_version": "next-release-repair-0.18",
            "consolidated_preview_version": "consolidated-preview-0.19",
            "preview_ability_items_registered": [HYPNO_PAN_ITEM_NAME, VIOLANCE_ITEM_NAME],
            "preview_ability_items_generated": True,
            "plant_pipes_durable_reconciliation": True,
            "music_lab_safe_location_classification": "conservative-v1",
            "garage_routing_mode": "interaction-gated",
            "royal_phone_side_split": True,
            "level_22_native_mapping": True,
            "native_difficulty_choice": True,
            "roots_intro_suppression": True,
            "hip_glasses_item": HIP_GLASSES_ITEM,
            "chicken_bucket_item": CHICKEN_BUCKET_ITEM,
            "hip_glasses_source_location": ROOTS_LEVEL4_HIP_GLASSES,
            "bucket_minion_trade_location": ROOTS_BUCKET_MINION_TRADE,
            "hip_glasses_source_flag": "LEVEL_08_GLASSES_COLLECTED",
            "hip_glasses_native_flag": "HIP_GLASSES_BAG_ITEM",
            "bucket_trade_flag": "ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES",
            "chicken_bucket_native_flag": "CHICKEN_BUCKET_BAG_ITEM",
            "combo_bucket_conversion_flag": "LEVEL_09_COMBO_ABILITY_EARNED",
            "level_3_logic": {
                "entry_requires": ["Roots Access", "Weed Killer"],
                "frog_hippo_check_requires": ["Roots Access", "Weed Killer"],
                "completion_requires": ["Roots Access", "Weed Killer", "Plant Pipes"],
                "menu_exit_without_plant_pipes_is_intended": True,
            },
            "game_garage_sticker_tiers": list(GARAGE_STICKER_TIERS),
            "game_garage_location_format": "Game Garage - {song} - {tier}",
            "music_lab_cassette_song_count": len(CASSETTE_SONGS),
            "music_lab_cassette_songs": list(CASSETTE_SONGS),
            "music_lab_cassette_medal_tiers": list(CASSETTE_MEDAL_TIERS),
            "music_lab_cassette_location_format": "Music Lab Cassette - {song} - {tier}",
            "music_lab_reward_chests": {
                "5": MUSIC_LAB_5_POINT_CHEST,
                "10": MUSIC_LAB_10_POINT_CHEST,
                "20": MUSIC_LAB_20_POINT_CHEST,
                "32": MUSIC_LAB_32_POINT_CHEST,
                "46": MUSIC_LAB_46_POINT_CHEST,
                "64": MUSIC_LAB_64_POINT_CHEST,
                "89": MUSIC_LAB_89_POINT_CHEST,
                "111": MUSIC_LAB_111_POINT_CHEST,
                "140": MUSIC_LAB_140_POINT_CHEST,
            },
            "internal_level_map": {
                "Level 1 - Completion": "Level_05",
                "Level 2 - Completion": "Level_06",
                "Level 3 - Completion": "Level_07",
            },
            "routing_logic_complete": False,
            "routing_logic_note": (
                "Roots-first area routing, difficulty filtering, AP Stars and "
                "generated Star gates are active. Level 3 retains its partial "
                "Plant Pipes source route. Victory requires the configured AP "
                "Star goal and a subsequent Level 22 clear with Plant Pipes. "
                "Native route rules remain conservative pending full-playthrough "
                "acceptance; unsupported sources cannot hold progression."
            ),
        }
