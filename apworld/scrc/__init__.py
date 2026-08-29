from BaseClasses import Item, ItemClassification, Location, Region, Tutorial
from worlds.AutoWorld import WebWorld, World
from worlds.generic.Rules import set_rule

from .difficulty import (
    DIFFICULTY_NAMES,
    MEDAL_TIERS,
    campaign_star_tiers,
    filter_locations_for_difficulty,
    medal_tiers,
)
from .cassettes import (
    CASSETTES,
    CASSETTE_BY_ITEM,
    NEW_CASSETTE_SOURCE_IDS,
    validate_cassette_catalog,
)
from .items import (
    CASSETTE_ITEM_CLASSIFICATIONS,
    CASSETTE_ITEM_NAME_TO_ID,
    NEW_ITEM_CLASSIFICATIONS,
    NEW_ITEM_NAME_TO_ID,
)
from .items import (
    HYPNO_PAN_ITEM_NAME,
    MONEY_CASSETTE_ITEM_NAME,
    STAR_ITEM_COUNT,
    STAR_ITEM_NAME,
    VIOLANCE_ITEM_NAME,
)
from .options import SCRCOptions
from .placement import filler_or_safe_required, required_progression_allowed
from .star_requirements import generate_star_requirements
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
    "Level 1 - Completion": BASE_ID + 1,
    "Level 2 - Completion": BASE_ID + 2,
    "Level 3 - Completion": BASE_ID + 3,

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
ROOTS_LEVEL3_FROG_HIPPO = "Roots - Level 3 - Frog and Hippo"
LOCATION_NAME_TO_ID[ROOTS_LEVEL3_FROG_HIPPO] = BASE_ID + 179

# v0.17: Level 4's Hip Glasses reward and the Bucket Minion trade become AP checks.
ROOTS_LEVEL4_HIP_GLASSES = "Roots - Level 4 - Hip Glasses"
ROOTS_BUCKET_MINION_TRADE = "Roots - Bucket Minion Trade"
LOCATION_NAME_TO_ID[ROOTS_LEVEL4_HIP_GLASSES] = BASE_ID + 180
LOCATION_NAME_TO_ID[ROOTS_BUCKET_MINION_TRADE] = BASE_ID + 181

LEVEL_22_ORDINARY_LOCATIONS = (
    "Level 22 - Completion",
    "Level 22 - 1 Star",
    "Level 22 - 2 Stars",
    "Level 22 - 3 Stars",
)
for index, name in enumerate(LEVEL_22_ORDINARY_LOCATIONS):
    LOCATION_NAME_TO_ID[name] = BASE_ID + 182 + index

LEVEL_2_MONEY_CASSETTE_SOURCE = "Level 2 - Money Cassette"
LOCATION_NAME_TO_ID[LEVEL_2_MONEY_CASSETTE_SOURCE] = BASE_ID + 186

# Full cassette randomization appends only genuinely new physical source
# locations.  The six reused sources (including Money's pilot location) retain
# their established locations and never receive duplicate checks.
LOCATION_NAME_TO_ID.update(NEW_CASSETTE_SOURCE_IDS)
validate_cassette_catalog(CASSETTE_SONGS, LOCATION_NAME_TO_ID)

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

MUSIC_LAB_TEST_LOCATIONS = {
    name
    for name in LOCATION_NAME_TO_ID
    if (
        name.startswith("Game Garage - ")
        or name.startswith("Music Lab Cassette - ")
        or name in MUSIC_LAB_REWARD_CHEST_LOCATIONS
    )
}

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
    """
    Area-routing development world.

    Archipelago Menu is the required logical root and connects freely to Hub6.
    Hub6 is the in-game logical home region. Music Lab and Game Garage are always
    connected to it. The six major castle areas are reached through Area
    Access items. v0.16 exposes a conservative starting-area option. Random
    currently samples only validated Roots Access, while unsupported fixed
    starts stop generation. The other five Area Access items are placed normally.

    v0.16 retains cartridge routing, Gecko Weed Killer randomization, and
    Plant Pipes as a separate randomized progression item. Frog/Hippo's
    Level 3 source check requires Weed Killer, while Level 3 Completion requires
    both Weed Killer and Plant Pipes. This is still not the final star-logic
    milestone; cassette and broader vanilla-world prerequisites remain incomplete.
    """

    game = GAME_NAME
    web = SCRCWebWorld()

    options_dataclass = SCRCOptions
    options: SCRCOptions

    item_name_to_id = ITEM_NAME_TO_ID
    location_name_to_id = LOCATION_NAME_TO_ID

    def generate_early(self) -> None:
        requested_start = int(self.options.starting_area.value)
        required_stars = int(self.options.required_stars.value)
        difficulty = self.options.difficulty.value
        active_campaign_star_tiers = campaign_star_tiers(difficulty)
        active_medal_tiers = medal_tiers(difficulty)

        self.starting_area_item = resolve_starting_area(requested_start, self.random)
        self.generated_star_requirements = generate_star_requirements(
            required_stars, self.random
        )
        self.difficulty_name = DIFFICULTY_NAMES[difficulty]
        self.active_location_names = frozenset(
            name
            for name in filter_locations_for_difficulty(LOCATION_NAME_TO_ID, difficulty)
            if name != CARTRIDGE_SOURCE_LOCATIONS[VANILLA_GARAGE_CARTRIDGE_SONG]
        )
        self.active_campaign_star_tiers = active_campaign_star_tiers
        self.active_medal_tiers = active_medal_tiers
        self.multiworld.push_precollected(self.create_item(self.starting_area_item))

    def create_regions(self) -> None:
        active_names = getattr(self, "active_location_names", frozenset(LOCATION_NAME_TO_ID))

        def is_active(name: str) -> bool:
            if name not in LOCATION_NAME_TO_ID:
                raise ValueError(f"unregistered active location: {name}")
            return name in active_names

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

        # Existing live level checks belong to Roots. Level 3 is intentionally
        # split into entrance/source reachability and completion: Weed Killer
        # gets the player into Level 3, while Plant Pipes is required to finish it.
        for name in ("Level 1 - Completion", "Level 2 - Completion", "Level 3 - Completion"):
            location = SCRCLocation(self.player, name, LOCATION_NAME_TO_ID[name], roots)
            if name == "Level 3 - Completion":
                set_rule(
                    location,
                    lambda state: (
                        state.has("Weed Killer", self.player)
                        and state.has("Plant Pipes", self.player)
                    ),
                )
            roots.locations.append(location)

        # Gecko is reachable from the Roots hub once Roots Access is owned.
        # Weed Killer is NOT required to reach this source; it is the reward
        # that vanilla later consumes to unlock entry to Level 3.
        roots.locations.append(
            SCRCLocation(
                self.player,
                ROOTS_GECKO_WEED_KILLER,
                LOCATION_NAME_TO_ID[ROOTS_GECKO_WEED_KILLER],
                roots,
            )
        )

        # Frog/Hippo is an in-level check before Level 3 completion. Weed Killer
        # is sufficient to enter Level 3 and reach them. Plant Pipes is deliberately
        # NOT required here so the source can be checked before the level is completable.
        frog_hippo = SCRCLocation(
            self.player,
            ROOTS_LEVEL3_FROG_HIPPO,
            LOCATION_NAME_TO_ID[ROOTS_LEVEL3_FROG_HIPPO],
            roots,
        )
        set_rule(
            frog_hippo,
            lambda state: state.has("Weed Killer", self.player),
        )
        roots.locations.append(frog_hippo)

        level4_hip_glasses = SCRCLocation(
            self.player,
            ROOTS_LEVEL4_HIP_GLASSES,
            LOCATION_NAME_TO_ID[ROOTS_LEVEL4_HIP_GLASSES],
            roots,
        )
        set_rule(
            level4_hip_glasses,
            lambda state: (
                state.has("Roots Access", self.player)
                and state.has("Weed Killer", self.player)
                and state.has("Plant Pipes", self.player)
            ),
        )
        roots.locations.append(level4_hip_glasses)

        bucket_trade = SCRCLocation(
            self.player,
            ROOTS_BUCKET_MINION_TRADE,
            LOCATION_NAME_TO_ID[ROOTS_BUCKET_MINION_TRADE],
            roots,
        )
        set_rule(
            bucket_trade,
            lambda state: state.has(HIP_GLASSES_ITEM, self.player),
        )
        roots.locations.append(bucket_trade)

        bucket_trade_event = SCRCLocation(
            self.player,
            "Bucket Minion Trade Complete",
            None,
            roots,
        )
        set_rule(
            bucket_trade_event,
            lambda state: state.has(HIP_GLASSES_ITEM, self.player),
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

        combo_bucket_event = SCRCLocation(
            self.player,
            "Combo Bucket Event",
            None,
            roots,
        )
        set_rule(
            combo_bucket_event,
            lambda state: (
                state.has("Bucket Minion Trade Complete", self.player)
                and state.has(CHICKEN_BUCKET_ITEM, self.player)
            ),
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

        # Preserve historical location IDs/datapackage names, but keep the
        # old synthetic caches filler-only so progression can never be placed
        # on a location the game client cannot actually report.
        for index in range(1, 11):
            name = f"Development Cache {index:02d}"
            location = SCRCLocation(self.player, name, LOCATION_NAME_TO_ID[name], phone_hub)
            location.item_rule = lambda item: item.name == "Stardust"
            phone_hub.locations.append(location)

        # v0.14 keeps the four cartridge sources that are not already
        # represented by Music Lab reward chests. Their exact physical-region
        # logic is intentionally not modeled yet, so keep these checks filler-only:
        # generation may never strand progression on an inaccurately modeled source.
        for name in RANDOMIZED_CARTRIDGE_SOURCE_LOCATIONS.values():
            location = SCRCLocation(self.player, name, LOCATION_NAME_TO_ID[name], phone_hub)
            location.item_rule = lambda item: item.name == "Stardust"
            phone_hub.locations.append(location)

        # Hub6 side content is always logically reachable. v0.14 intentionally
        # permits Area Access items here so current-save networking can test
        # real AP-driven area unlocks. Remaining cassette/point/full world
        # prerequisites will be modeled in a later logic milestone.
        for chest_name in MUSIC_LAB_REWARD_CHEST_LOCATIONS:
            location = SCRCLocation(
                self.player,
                chest_name,
                LOCATION_NAME_TO_ID[chest_name],
                music_lab,
            )
            safe = required_progression_allowed(chest_name, active_names)
            location.item_rule = lambda item, allowed=safe: filler_or_safe_required(item, allowed)
            music_lab.locations.append(location)

        concrete_regions = {
            "Cell Tower": cell,
            "Lobby": lobby,
            "Meat Dimension": meat,
            "Music Lab": music_lab,
            "Roots": roots,
            "Royal Corridor": royal,
            "Secret Bunker": phone_hub,
            "Tower of Fear": tower,
        }
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
            set_rule(
                source,
                lambda state, triggers=entry.triggers: route_is_open(state, triggers),
            )
            parent.locations.append(source)

        cassette_items_by_song = {
            entry.display_song: item_name
            for item_name, entry in CASSETTE_BY_ITEM.items()
        }
        for song in CASSETTE_SONGS:
            cassette_item = cassette_items_by_song[song]
            for tier in CASSETTE_MEDAL_TIERS:
                name = f"Music Lab Cassette - {song} - {tier}"
                if not is_active(name):
                    continue
                location = SCRCLocation(self.player, name, LOCATION_NAME_TO_ID[name], music_lab)
                set_rule(
                    location,
                    lambda state, item=cassette_item: state.has(item, self.player),
                )
                safe = required_progression_allowed(name, active_names)
                location.item_rule = lambda item, allowed=safe: filler_or_safe_required(item, allowed)
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
                safe = required_progression_allowed(name, active_names)
                location.item_rule = lambda item, allowed=safe: filler_or_safe_required(item, allowed)
                game_garage.locations.append(location)

        # Royal Access lands on the phone-side Level 22 route. Completion and
        # one Star are confirmed ability-free. Two/three-Star requirements are
        # still under investigation, so those tiers remain filler-only.
        for name in LEVEL_22_ORDINARY_LOCATIONS:
            if not is_active(name):
                continue
            location = SCRCLocation(self.player, name, LOCATION_NAME_TO_ID[name], royal)
            if name in {"Level 22 - 2 Stars", "Level 22 - 3 Stars"}:
                location.item_rule = lambda item: filler_or_safe_required(item, False)
            royal.locations.append(location)

        # AP core root -> in-game home base. This connection is always free.
        menu.connect(phone_hub, "Menu -> Phone Hub")

        # Music Lab + Garage are permanent home-base side regions.
        phone_hub.connect(music_lab, "Phone Hub -> Music Lab")
        phone_hub.connect(game_garage, "Phone Hub -> Game Garage")

        # The six red phones are represented as six independently gated
        # connections from Hub6. The client enforces the same items physically.
        area_regions = {
            "Roots Access": roots,
            "Lobby Access": lobby,
            "Meat Dimension Access": meat,
            "Cell Tower Access": cell,
            "Tower of Fear Access": tower,
            "Royal Corridor Access": royal,
        }
        for access_item, region in area_regions.items():
            phone_hub.connect(
                region,
                f"Phone Hub -> {region.name}",
                lambda state, item=access_item: state.has(item, self.player),
            )

        victory = SCRCLocation(self.player, "Victory", None, phone_hub)
        victory.place_locked_item(
            SCRCItem("Victory", ItemClassification.progression, None, self.player)
        )
        phone_hub.locations.append(victory)

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
            *cassette_source_regions.values(),
        ]

    def _active_unfilled_location_capacity(self) -> int:
        return sum(
            1
            for region in self.multiworld.regions
            for location in region.locations
            if (
                location.player == self.player
                and location.address is not None
                and location.item is None
            )
        )

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
        progression_items.extend(entry.item_name for entry in CASSETTES)

        capacity = self._active_unfilled_location_capacity()
        required_count = len(progression_items)
        if required_count > capacity:
            raise ValueError(
                f"{required_count} required progression items exceed "
                f"{capacity} active locations for {self.difficulty_name}"
            )
        filler_count = capacity - required_count

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
        # Development completion condition for the routing milestone: obtain
        # all six Area Access items (one starter + five randomized).
        set_rule(
            self.multiworld.get_location("Victory", self.player),
            lambda state: all(
                state.has(item_name, self.player)
                for item_name in AREA_ACCESS_ITEMS
            ),
        )

        self.multiworld.completion_condition[self.player] = (
            lambda state: state.has("Victory", self.player)
        )

    def fill_slot_data(self) -> dict:
        starter = getattr(self, "starting_area_item", AREA_ACCESS_ITEMS[0])
        options = getattr(self, "options", None)
        required_stars = int(
            getattr(getattr(options, "required_stars", None), "value", 50)
        )
        difficulty_value = getattr(getattr(options, "difficulty", None), "value", 0)
        default_active_location_names = frozenset(
            name
            for name in filter_locations_for_difficulty(LOCATION_NAME_TO_ID, difficulty_value)
            if name != CARTRIDGE_SOURCE_LOCATIONS[VANILLA_GARAGE_CARTRIDGE_SONG]
        )
        default_campaign_star_tiers = campaign_star_tiers(difficulty_value)
        default_medal_tiers = medal_tiers(difficulty_value)
        requested_start = int(
            getattr(getattr(options, "starting_area", None), "value", 0)
        )
        generated_requirements = getattr(self, "generated_star_requirements", {})
        active_location_names = getattr(
            self,
            "active_location_names",
            default_active_location_names,
        )
        return {
            "implementation_version": "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21",
            "generation_foundation_version": "generation-foundation-0.16",
            "schema_version": 12,
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
            "star_items_active": False,
            "generated_star_requirements": dict(generated_requirements),
            "generated_star_requirements_depth_model": "provisional-linear-level-order",
            "client_star_gate_enforcement_active": False,
            "difficulty_filtering_active": True,
            "active_location_count": len(active_location_names),
            "active_campaign_star_tiers": sorted(
                getattr(self, "active_campaign_star_tiers", default_campaign_star_tiers)
            ),
            "active_medal_tiers": [
                tier
                for tier in MEDAL_TIERS
                if tier in getattr(self, "active_medal_tiers", default_medal_tiers)
            ],
            "development_area_access_victory_active": True,
            "logical_root_region": "Menu",
            "home_region": "Phone Hub",
            "starting_area_item": starter,
            "starting_area": AREA_ITEM_TO_REGION[starter],
            "starting_area_forced": False,
            "area_access_items": list(AREA_ACCESS_ITEMS),
            "always_open_regions": ["Phone Hub", "Music Lab", "Game Garage"],
            "development_cache_count": 10,
            "development_caches_filler_only": True,
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
            "randomize_level_2_money_cassette": True,
            "repair_schema_version": "next-release-repair-0.18",
            "consolidated_preview_version": "consolidated-preview-0.19",
            "preview_ability_items_registered": [HYPNO_PAN_ITEM_NAME, VIOLANCE_ITEM_NAME],
            "preview_ability_items_generated": False,
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
                "APWorld v0.21.0 retains Roots-first area routing, randomized Weed "
                "Killer, and split Level 3 Plant Pipes logic. Vampire Killer is "
                "native for immediate Garage access. Difficulty filtering is active. "
                "Star requirements remain inactive previews, and live "
                "Star gates plus remaining full-game prerequisites are deferred."
            ),
        }
