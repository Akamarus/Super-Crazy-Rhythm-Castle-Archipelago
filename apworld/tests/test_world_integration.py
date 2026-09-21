import random
import json
from collections import Counter
from pathlib import Path
import types
import unittest

from support import FakeMultiWorld, OptionValue, load_scrc_module, load_scrc_world


class State:
    def __init__(self, owned):
        self.owned = Counter(owned)

    def has(self, name, player):
        return self.count(name, player) > 0

    def count(self, name, player):
        return self.owned[name]

    def collect(self, name):
        self.owned[name] += 1


class WorldIntegrationTests(unittest.TestCase):
    def setUp(self):
        self.module, cleanup = load_scrc_world()
        self.addCleanup(cleanup)
        self.catalog = load_scrc_module("cassettes")
        self.campaign = load_scrc_module("campaign_levels")
        self.difficulty = load_scrc_module("difficulty")
        self.points = load_scrc_module("music_lab_points")

    def make_world(
        self,
        seed=22,
        required_stars=50,
        difficulty=0,
        starting_area=0,
        player=1,
        multiworld=None,
    ):
        world = object.__new__(self.module.SCRCWorld)
        world.player = player
        world.random = random.Random(seed)
        world.multiworld = multiworld or FakeMultiWorld()
        world.options = types.SimpleNamespace(
            required_stars=OptionValue(required_stars),
            difficulty=OptionValue(difficulty),
            starting_area=OptionValue(starting_area),
        )
        return world

    def build_world(self, difficulty=0, seed=22, starting_area=0):
        world = self.make_world(
            seed=seed,
            difficulty=difficulty,
            starting_area=starting_area,
        )
        world.generate_early()
        world.create_regions()
        return world

    @staticmethod
    def addressed_names(world):
        return {
            location.name
            for region in world.multiworld.regions
            for location in region.locations
            if location.address is not None
        }

    @staticmethod
    def reachable_regions(world, state):
        reachable = {"Menu"}
        changed = True
        while changed:
            changed = False
            for region in world.multiworld.regions:
                if region.name not in reachable:
                    continue
                for connection in region.connections:
                    if (
                        connection.connected_region.name not in reachable
                        and connection.access_rule(state)
                    ):
                        reachable.add(connection.connected_region.name)
                        changed = True
        return reachable

    @classmethod
    def location_is_reachable(cls, world, location, state):
        return (
            location.parent_region.name in cls.reachable_regions(world, state)
            and location.access_rule(state)
        )

    def full_state(self, missing=()):
        return State([name for name in self.module.ITEM_NAME_TO_ID if name not in set(missing) | {"Star"}]
                     + ([] if "Star" in missing else ["Star"] * 66))

    def assert_requires(self, location_name, *required_items):
        world = self.build_world(difficulty=2)
        location = world.multiworld.get_location(location_name, world.player)
        self.assertTrue(location.access_rule(self.full_state()))
        for missing_item in required_items:
            with self.subTest(location=location_name, missing=missing_item):
                remaining = tuple(item for item in required_items if item != missing_item)
                self.assertFalse(location.access_rule(self.full_state((missing_item,))))

    def assert_allows_required_progression(self, location_name):
        world = self.build_world(difficulty=2)
        location = world.multiworld.get_location(location_name, world.player)
        self.assertTrue(location.item_rule(world.create_item("Plant Pipes")))

    def assert_rejects_required_progression(self, location_name, difficulty=0):
        world = self.build_world(difficulty=difficulty)
        location = world.multiworld.get_location(location_name, world.player)
        self.assertFalse(location.item_rule(world.create_item("Plant Pipes")))

    @classmethod
    def collect_placement_spheres(cls, world, state, placements):
        collected = set()
        while True:
            newly_collected = [
                location_name
                for location_name in placements
                if location_name not in collected
                and cls.location_is_reachable(
                    world,
                    world.multiworld.get_location(location_name, world.player),
                    state,
                )
                and world.multiworld.get_location(location_name, world.player).item_rule(
                    world.create_item(placements[location_name])
                )
            ]
            if not newly_collected:
                return len(collected) == len(placements)
            for location_name in newly_collected:
                state.collect(placements[location_name])
                collected.add(location_name)

    def test_generate_early_resolves_start_and_builds_previews(self):
        world = self.make_world()
        world.generate_early()

        self.assertEqual([item.name for item in world.multiworld.precollected], ["Roots Access"])
        self.assertEqual(len(world.generated_star_requirements), 22)
        self.assertEqual(tuple(world.generated_star_requirements), tuple(f"Level {i}" for i in range(1, 23)))
        self.assertTrue(all(0 <= value < 50 for value in world.generated_star_requirements.values()))
        self.assertEqual(tuple(world.generated_star_requirements.values()), tuple(sorted(world.generated_star_requirements.values())))
        self.assertTrue(world.active_location_names)
        self.assertNotIn("Game Garage - Bloody Tears - Silver", world.active_location_names)

    def test_unsupported_fixed_start_stops_before_precollect(self):
        world = self.make_world(starting_area=2)
        with self.assertRaisesRegex(ValueError, "Lobby.*not validated"):
            world.generate_early()
        self.assertEqual(world.multiworld.precollected, [])

    def test_generate_early_rejects_invalid_raw_difficulty_values(self):
        for value in (True, 4):
            with self.subTest(difficulty=value):
                world = self.make_world(difficulty=value)
                with self.assertRaisesRegex(ValueError, "difficulty"):
                    world.generate_early()
                self.assertEqual(world.multiworld.precollected, [])

    def test_normal_omits_inactive_performance_locations(self):
        names = self.addressed_names(self.build_world(difficulty=0))
        self.assertIn("Level 22 - Completion", names)
        self.assertIn("Level 22 - 1 Star", names)
        self.assertNotIn("Level 22 - 2 Stars", names)
        self.assertNotIn("Level 22 - 3 Stars", names)
        self.assertIn("Music Lab Cassette - Zen - Bronze", names)
        self.assertNotIn("Music Lab Cassette - Zen - Silver", names)
        self.assertNotIn("Game Garage - Smooch - Platinum", names)

    def test_active_location_counts_are_exact(self):
        expected = {0: 164, 1: 222, 2: 280, 3: 316}
        for value, count in expected.items():
            with self.subTest(difficulty=value):
                self.assertEqual(len(self.addressed_names(self.build_world(value))), count)

    def test_slot_data_counts_only_instantiated_addressed_locations(self):
        world = self.build_world(difficulty=0)

        self.assertEqual(
            world.fill_slot_data()["active_location_count"],
            len(self.addressed_names(world)),
        )

    def test_slot_data_publishes_the_strict_campaign_mapping_contract(self):
        expected_tiers = {
            0: ["Completion", "1 Star"],
            1: ["Completion", "1 Star", "2 Stars"],
            2: ["Completion", "1 Star", "2 Stars", "3 Stars"],
            3: ["Completion", "1 Star", "2 Stars", "3 Stars"],
        }
        for difficulty, tiers in expected_tiers.items():
            with self.subTest(difficulty=difficulty):
                world = self.build_world(difficulty=difficulty)
                data = world.fill_slot_data()
                expected_active_names = {
                    name
                    for name in self.addressed_names(world)
                    if name in self.campaign.CAMPAIGN_LOCATION_NAMES
                }

                self.assertEqual(data["schema_version"], 19)
                self.assertEqual(data["campaign_level_mapping_schema"], 1)
                self.assertEqual(
                    data["active_campaign_locations"],
                    sorted(expected_active_names),
                )
                self.assertEqual(data["active_campaign_location_tiers"], tiers)
                self.assertFalse(data["special_variant_locations_active"])
                self.assertEqual(data["special_variant_locations"], [])
                self.assertTrue(data["star_items_active"])
                self.assertTrue(data["client_star_gate_enforcement_active"])
                self.assertEqual(data["development_cache_count"], 0)
                self.assertTrue(data["development_cache_ids_reserved"])
                self.assertEqual(data["active_location_count"], len(self.addressed_names(world)))

    def test_slot_data_scopes_campaign_mapping_to_its_player_in_shared_multiworld(self):
        multiworld = FakeMultiWorld()
        worlds = [
            self.make_world(player=1, difficulty=0, multiworld=multiworld),
            self.make_world(player=2, difficulty=1, multiworld=multiworld),
        ]
        expected = {
            1: (164, ["Completion", "1 Star"], 0),
            2: (222, ["Completion", "1 Star", "2 Stars"], 1),
        }
        for world in worlds:
            world.generate_early()
            world.create_regions()

        for world in worlds:
            expected_count, expected_tiers, difficulty = expected[world.player]
            with self.subTest(player=world.player):
                data = world.fill_slot_data()
                self.assertEqual(data["active_location_count"], expected_count)
                self.assertEqual(data["active_campaign_location_tiers"], expected_tiers)
                self.assertEqual(
                    data["active_campaign_locations"],
                    sorted(
                        self.difficulty.active_location_names(
                            self.campaign.CAMPAIGN_LOCATION_NAMES,
                            difficulty,
                        )
                    ),
                )

    def test_full_campaign_catalog_is_active_and_development_caches_are_reserved_only(self):
        expected = {0: 164, 1: 222, 2: 280, 3: 316}
        for difficulty, count in expected.items():
            with self.subTest(difficulty=difficulty):
                world = self.build_world(difficulty=difficulty)
                addressed = self.addressed_names(world)

                self.assertEqual(len(addressed), count)
                self.assertEqual(world.fill_slot_data()["active_location_count"], count)
                self.assertEqual(world.fill_slot_data()["active_location_count"], len(addressed))
                self.assertEqual(
                    {
                        name
                        for name in addressed
                        if name in self.campaign.CAMPAIGN_LOCATION_NAMES
                    },
                    set(
                        self.difficulty.active_location_names(
                            self.campaign.CAMPAIGN_LOCATION_NAMES,
                            difficulty,
                        )
                    )
                )
                self.assertFalse(
                    any(name.startswith("Development Cache") for name in addressed)
                )
                self.assertEqual(world.fill_slot_data()["development_cache_count"], 0)
                self.assertFalse(world.fill_slot_data()["development_caches_filler_only"])

        self.assertEqual(
            self.module.LOCATION_NAME_TO_ID["Development Cache 01"],
            187256011,
        )

    def test_campaign_locations_use_catalog_regions_and_verified_placement_rules(self):
        world = self.build_world(difficulty=2)
        for level in self.campaign.CAMPAIGN_LEVELS:
            for tier in self.campaign.CAMPAIGN_LOCATION_TIERS:
                name = level.location_name(tier)
                with self.subTest(location=name):
                    self.assertEqual(
                        world.multiworld.get_location(name, world.player).parent_region.name,
                        level.area,
                    )

        self.assert_requires("Level 3 - Completion", "Weed Killer", "Plant Pipes")
        self.assert_requires("Level 18 - Completion", "Plant Pipes")
        self.assert_requires("Level 20 - Completion", "Hypno Pan")
        self.assert_requires("Level 21 - Completion", "Plant Pipes")
        self.assert_allows_required_progression("Level 22 - 1 Star")
        self.assert_rejects_required_progression("Level 22 - 2 Stars", difficulty=2)
        self.assert_allows_required_progression("Level 6 - Completion")

    def test_item_pool_matches_active_unfilled_capacity(self):
        expected = {0: 164, 1: 222, 2: 280, 3: 316}
        for value, count in expected.items():
            with self.subTest(difficulty=value):
                world = self.build_world(difficulty=value)
                world.create_items()
                self.assertEqual(len(world.multiworld.itempool), count)
                names = [item.name for item in world.multiworld.itempool]
                self.assertEqual(names.count("Star"), 66)
                self.assertEqual(names.count("Hypno Pan"), 1)
                self.assertEqual(names.count("Violance"), 1)

    def test_every_cassette_has_one_live_item_and_one_source(self):
        for difficulty in range(4):
            with self.subTest(difficulty=difficulty):
                world = self.build_world(difficulty=difficulty)
                world.create_items()
                item_names = [item.name for item in world.multiworld.itempool]
                addressed_names = [
                    location.name
                    for region in world.multiworld.regions
                    for location in region.locations
                    if location.address is not None
                ]

                for entry in self.catalog.CASSETTES:
                    self.assertEqual(item_names.count(entry.item_name), 1, entry.item_name)
                    self.assertEqual(addressed_names.count(entry.source_name), 1, entry.source_name)
                    source = world.multiworld.get_location(entry.source_name, world.player)
                    if not entry.reused_location:
                        self.assertEqual(source.address, entry.source_id)
                        self.assertEqual(source.parent_region.name, entry.region)

    def test_all_cassette_medals_require_only_the_matching_cassette(self):
        world = self.build_world(difficulty=3)
        all_prerequisites = {
            requirement.item
            for entry in self.catalog.CASSETTES
            for trigger in entry.triggers
            for requirement in trigger.requirements
        }

        for index, entry in enumerate(self.catalog.CASSETTES):
            wrong_item = self.catalog.CASSETTES[(index + 1) % len(self.catalog.CASSETTES)].item_name
            for tier in self.module.CASSETTE_MEDAL_TIERS:
                with self.subTest(song=entry.display_song, tier=tier):
                    medal = world.multiworld.get_location(
                        f"Music Lab Cassette - {entry.display_song} - {tier}",
                        world.player,
                    )
                    self.assertFalse(medal.access_rule(State(all_prerequisites)))
                    self.assertFalse(medal.access_rule(State(all_prerequisites | {wrong_item})))
                    self.assertTrue(medal.access_rule(State(all_prerequisites | {entry.item_name})))

    def test_cassette_rewards_cannot_bypass_campaign_item_gates(self):
        # Independent regression cases: these rewards were reachable without
        # items needed to finish their native levels, allowing self-locks.
        cases = {
            "Badass": {"Roots Access", "Weed Killer", "Plant Pipes", "Hip Glasses", "Chicken Bucket"},
            "Heavy Metal": {"Roots Access", "Weed Killer", "Plant Pipes", "Hip Glasses", "Chicken Bucket"},
            "Gotta Get Up": {"Tower of Fear Access", "Plant Pipes"},
            "Party Non Stop": {"Tower of Fear Access", "Hypno Pan"},
            "Keep On Hustlin": {"Royal Corridor Access", "Violance", "Weed Killer", "Plant Pipes"},
        }
        for difficulty in range(4):
            world = self.build_world(difficulty=difficulty)
            for song, required in cases.items():
                source = world.multiworld.get_location(f"Cassette Source - {song}", world.player)
                with self.subTest(difficulty=difficulty, song=song, owned="all"):
                    self.assertTrue(self.location_is_reachable(world, source, self.full_state()))
                for missing in required:
                    with self.subTest(difficulty=difficulty, song=song, missing=missing):
                        self.assertFalse(self.location_is_reachable(world, source, self.full_state((missing,))))

    def test_normal_cassette_routes_include_campaign_requirements(self):
        for entry in self.catalog.CASSETTES:
            for trigger in entry.triggers:
                if trigger.variant != "LevelVariant_Default":
                    continue
                level = self.campaign.CAMPAIGN_LEVELS_BY_INTERNAL_ID[trigger.level]
                expected = {f"{level.area} Access", *level.required_items}
                actual = {req.item for req in trigger.requirements}
                with self.subTest(song=entry.display_song, level=level.number):
                    self.assertTrue(expected <= actual, f"Missing {expected - actual}")

    def test_cassette_sources_use_non_bunker_verified_routes_in_the_region_graph(self):
        world = self.build_world(difficulty=3)

        for entry in self.catalog.CASSETTES:
            if entry.source_type == "Music Lab point chest":
                continue
            source = world.multiworld.get_location(entry.source_name, world.player)
            active_triggers = tuple(
                trigger for trigger in entry.triggers
                if trigger.region != "Secret Bunker"
            )
            self.assertTrue(active_triggers, entry.source_name)
            self.assertFalse(
                self.location_is_reachable(world, source, State([])),
                entry.source_name,
            )
            for trigger in active_triggers:
                with self.subTest(source=entry.source_name, route=(trigger.level, trigger.variant)):
                    route_items = {requirement.item for requirement in trigger.requirements}
                    route_state = self.full_state()
                    self.assertIn(
                        source.parent_region.name,
                        self.reachable_regions(world, route_state),
                    )
                    self.assertTrue(self.location_is_reachable(world, source, route_state))
                    for missing in set.intersection(*({r.item for r in t.requirements} for t in active_triggers)):
                        self.assertFalse(
                            self.location_is_reachable(
                                world,
                                source,
                                self.full_state((missing,)),
                            ),
                            f"{entry.source_name} without {missing}",
                        )
            route_items = {
                requirement.item
                for trigger in active_triggers
                for requirement in trigger.requirements
            }
            wrong_route_item = next(
                item for item in self.module.AREA_ACCESS_ITEMS
                if item not in route_items
            )
            self.assertFalse(
                self.location_is_reachable(world, source, State({wrong_route_item})),
                f"{entry.source_name} through wrong route {wrong_route_item}",
            )

    def test_secret_bunker_aliases_do_not_open_from_phone_hub(self):
        world = self.build_world(difficulty=3)

        for entry in self.catalog.CASSETTES:
            bunker_triggers = tuple(
                trigger for trigger in entry.triggers
                if trigger.region == "Secret Bunker"
            )
            if not bunker_triggers:
                continue
            source = world.multiworld.get_location(entry.source_name, world.player)
            for trigger in bunker_triggers:
                with self.subTest(source=entry.source_name, route=(trigger.level, trigger.variant)):
                    bunker_only_items = {
                        requirement.item for requirement in trigger.requirements
                    }
                    self.assertFalse(
                        self.location_is_reachable(world, source, State(bunker_only_items))
                    )

    def test_cell_tower_return_cassettes_require_lobby_route_and_hypno_pan(self):
        world = self.build_world(difficulty=3)

        for song in ("Epical", "Hollywood Trailer", "False Data"):
            with self.subTest(song=song):
                source = world.multiworld.get_location(
                    f"Cassette Source - {song}",
                    world.player,
                )
                self.assertFalse(
                    self.location_is_reachable(world, source, State({"Hypno Pan"}))
                )
                self.assertFalse(
                    self.location_is_reachable(world, source, State({"Lobby Access"}))
                )
                self.assertTrue(
                    self.location_is_reachable(
                        world,
                        source,
                        self.full_state(),
                    )
                )

    def test_music_lab_point_chests_accept_progression_and_gate_by_weighted_thresholds(self):
        world = self.build_world(difficulty=3)
        world.set_rules()
        required = world.create_item(self.catalog.CASSETTES[0].item_name)

        self.assertEqual(len(self.module.MUSIC_LAB_REWARD_CHEST_LOCATIONS), 9)
        for threshold, chest_name in self.module.MUSIC_LAB_POINT_THRESHOLDS.items():
            with self.subTest(chest=chest_name, threshold=threshold):
                chest = world.multiworld.get_location(chest_name, world.player)
                self.assertTrue(chest.item_rule(required))
                below = State(["Music Lab Point"] * (threshold - 1))
                at_threshold = State(["Music Lab Point"] * threshold)
                self.assertFalse(chest.access_rule(below))
                self.assertTrue(chest.access_rule(at_threshold))

    def test_music_lab_point_chest_aliases_and_location_counts_remain_stable(self):
        world = self.build_world(difficulty=3)
        cassette_sources = {
            "Quicksand": "Music Lab - 32 Point Chest",
            "Flamenco": "Music Lab - 64 Point Chest",
            "Ten-Four Good Buddy": "Music Lab - 89 Point Chest",
            "Zen": "Music Lab - 111 Point Chest",
            "Wiggle": "Music Lab - 140 Point Chest",
        }
        garage_sources = {
            "Gradius Remix": "Music Lab - 10 Point Chest",
            "Bloody Tears": "Music Lab - 46 Point Chest",
        }

        source_names = []
        for song, chest_name in cassette_sources.items():
            source_name = next(
                entry.source_name
                for entry in self.catalog.CASSETTES
                if entry.display_song == song
            )
            self.assertEqual(source_name, chest_name)
            source_names.append(source_name)
        for song, chest_name in garage_sources.items():
            self.assertEqual(
                self.module.GARAGE_CARTRIDGE_ITEMS[song],
                f"{song} Cartridge",
            )
            source_names.append(chest_name)

        chest_objects = [
            world.multiworld.get_location(source_name, world.player)
            for source_name in source_names
        ]
        self.assertEqual(len(chest_objects), 7)
        self.assertEqual(len({id(chest) for chest in chest_objects}), 7)

        expected_counts = {0: 164, 1: 222, 2: 280, 3: 316}
        for difficulty, count in expected_counts.items():
            with self.subTest(difficulty=difficulty):
                self.assertEqual(len(self.addressed_names(self.build_world(difficulty))), count)

    def test_eight_option_matrix_has_no_cassette_self_lock(self):
        for difficulty in range(4):
            for starting_area in (0, 1):
                with self.subTest(difficulty=difficulty, starting_area=starting_area):
                    world = self.make_world(
                        seed=43001 + difficulty * 2 + starting_area,
                        difficulty=difficulty,
                        starting_area=starting_area,
                    )
                    world.generate_early()
                    world.create_regions()
                    world.create_items()
                    world.set_rules()

                    item_names = [item.name for item in world.multiworld.itempool]
                    for entry in self.catalog.CASSETTES:
                        self.assertEqual(item_names.count(entry.item_name), 1)

                    generated_progression = [
                        item.name
                        for item in world.multiworld.itempool
                        if item.classification == "progression"
                    ]
                    generated_progression.extend(
                        item.name for item in world.multiworld.precollected
                    )
                    for entry in self.catalog.CASSETTES:
                        all_but_self = State(generated_progression)
                        all_but_self.owned[entry.item_name] -= 1
                        source = world.multiworld.get_location(entry.source_name, world.player)
                        self.assertTrue(
                            self.location_is_reachable(world, source, all_but_self),
                            entry.source_name,
                        )
                        bronze = world.multiworld.get_location(
                            f"Music Lab Cassette - {entry.display_song} - Bronze",
                            world.player,
                        )
                        self.assertFalse(bronze.access_rule(all_but_self), entry.item_name)

    def test_shared_multiworld_capacity_and_pool_are_scoped_per_player(self):
        multiworld = FakeMultiWorld()
        worlds = [
            self.make_world(player=player, multiworld=multiworld)
            for player in (1, 2)
        ]
        for world in worlds:
            world.generate_early()
            world.create_regions()

        self.assertEqual(
            [world._active_unfilled_location_capacity() for world in worlds],
            [164, 164],
        )

        for world in worlds:
            world.create_items()

        self.assertEqual(
            [
                sum(item.player == player for item in multiworld.itempool)
                for player in (1, 2)
            ],
            [164, 164],
        )

    def test_item_pool_rejects_insufficient_active_locations(self):
        world = self.build_world()
        retained_names = set(sorted(self.addressed_names(world))[:12])
        for region in world.multiworld.regions:
            region.locations[:] = [
                location
                for location in region.locations
                if location.address is None or location.name in retained_names
            ]

        with self.assertRaisesRegex(ValueError, "required progression items.*active locations"):
            world.create_items()

    def test_cassette_prerequisite_abilities_are_generated_once(self):
        world = self.build_world()
        world.create_items()

        self.assertEqual(world.create_item("Hypno Pan").code, 187256121)
        self.assertEqual(world.create_item("Hypno Pan").classification, "progression")
        self.assertEqual(world.create_item("Violance").code, 187256122)
        self.assertEqual(world.create_item("Violance").classification, "progression")
        generated_names = [item.name for item in world.multiworld.itempool]
        self.assertEqual(generated_names.count("Hypno Pan"), 1)
        self.assertEqual(generated_names.count("Violance"), 1)

    def test_roots_bucket_progression_has_permanent_unique_ids(self):
        self.assertEqual(self.module.ITEM_NAME_TO_ID[self.module.HIP_GLASSES_ITEM], 187256119)
        self.assertEqual(self.module.ITEM_NAME_TO_ID[self.module.CHICKEN_BUCKET_ITEM], 187256120)
        self.assertEqual(
            self.module.LOCATION_NAME_TO_ID[self.module.ROOTS_LEVEL4_HIP_GLASSES],
            187256180,
        )
        self.assertEqual(
            self.module.LOCATION_NAME_TO_ID[self.module.ROOTS_BUCKET_MINION_TRADE],
            187256181,
        )
        self.assertEqual(
            self.module.ITEM_CLASSIFICATIONS[self.module.HIP_GLASSES_ITEM],
            "progression",
        )
        self.assertEqual(
            self.module.ITEM_CLASSIFICATIONS[self.module.CHICKEN_BUCKET_ITEM],
            "progression",
        )
        self.assertEqual(
            len(set(self.module.ITEM_NAME_TO_ID.values())),
            len(self.module.ITEM_NAME_TO_ID),
        )
        self.assertEqual(
            len(set(self.module.LOCATION_NAME_TO_ID.values())),
            len(self.module.LOCATION_NAME_TO_ID),
        )

    def test_money_cassette_registers_unique_progression_source_and_pool_item(self):
        self.assertEqual(
            self.module.ITEM_NAME_TO_ID["Money Cassette"],
            187256123,
        )
        self.assertEqual(
            self.module.LOCATION_NAME_TO_ID[self.module.LEVEL_2_MONEY_CASSETTE_SOURCE],
            187256186,
        )
        self.assertEqual(
            self.module.ITEM_CLASSIFICATIONS["Money Cassette"],
            "progression",
        )
        self.assertEqual(len(set(self.module.ITEM_NAME_TO_ID.values())), len(self.module.ITEM_NAME_TO_ID))
        self.assertEqual(
            len(set(self.module.LOCATION_NAME_TO_ID.values())),
            len(self.module.LOCATION_NAME_TO_ID),
        )

        world = self.build_world(difficulty=3)
        world.create_items()
        self.assertEqual(
            [item.name for item in world.multiworld.itempool].count("Money Cassette"),
            1,
        )

    def test_money_cassette_source_and_i_got_money_medals_follow_its_rules(self):
        world = self.build_world(difficulty=3)
        source = world.multiworld.get_location(self.module.LEVEL_2_MONEY_CASSETTE_SOURCE, 1)

        self.assertEqual(source.address, 187256186)
        self.assertEqual(source.parent_region.name, "Roots OR Cell Tower")
        self.assertFalse(source.access_rule(State([])))
        self.assertTrue(source.access_rule(State(["Roots Access"])))
        self.assertIn("Roots", self.reachable_regions(world, State(["Roots Access"])))

        for tier in self.module.CASSETTE_MEDAL_TIERS:
            location = world.multiworld.get_location(
                f"Music Lab Cassette - I Got Money - {tier}", 1
            )
            self.assertFalse(location.access_rule(State([])))
            self.assertTrue(location.access_rule(State(["Money Cassette"])))

    def test_roots_bucket_graph_uses_network_sources_and_internal_events(self):
        world = self.make_world()
        world.generate_early()
        world.create_regions()

        level4 = world.multiworld.get_location(self.module.ROOTS_LEVEL4_HIP_GLASSES, 1)
        trade = world.multiworld.get_location(self.module.ROOTS_BUCKET_MINION_TRADE, 1)
        trade_event = world.multiworld.get_location("Bucket Minion Trade Complete", 1)
        combo_event = world.multiworld.get_location("Combo Bucket Event", 1)

        self.assertEqual(level4.address, 187256180)
        self.assertEqual(trade.address, 187256181)
        self.assertIsNone(trade_event.address)
        self.assertEqual(trade_event.item.name, "Bucket Minion Trade Complete")
        self.assertIsNone(combo_event.address)
        self.assertEqual(combo_event.item.name, "Combo Bucket Event")

        self.assertFalse(level4.access_rule(State(["Weed Killer", "Plant Pipes"])))
        self.assertFalse(level4.access_rule(State(["Roots Access", "Plant Pipes"])))
        self.assertFalse(level4.access_rule(State(["Roots Access", "Weed Killer"])))
        self.assertTrue(level4.access_rule(self.full_state()))
        self.assertFalse(trade.access_rule(State([])))
        self.assertTrue(trade.access_rule(self.full_state()))
        self.assertFalse(combo_event.access_rule(State(["Chicken Bucket"])))
        self.assertFalse(combo_event.access_rule(State(["Bucket Minion Trade Complete"])))
        self.assertTrue(
            combo_event.access_rule(self.full_state())
        )

    def test_live_pool_contains_one_of_each_roots_bucket_item(self):
        world = self.build_world()
        world.create_items()
        names = [item.name for item in world.multiworld.itempool]

        self.assertEqual(names.count("Hip Glasses"), 1)
        self.assertEqual(names.count("Chicken Bucket"), 1)
        self.assertEqual(len(names), len(self.addressed_names(world)))

    def test_live_pool_contains_the_exact_twenty_music_lab_point_instances(self):
        world = self.build_world(difficulty=0)
        world.create_items()
        names = [item.name for item in world.multiworld.itempool]

        self.assertEqual(
            Counter(name for name in names if name.startswith("Music Lab Point")),
            Counter(self.module.MUSIC_LAB_POINT_POOL),
        )
        self.assertEqual(names.count("Stardust"), 27)

    def test_slot_data_labels_active_difficulty_filtering(self):
        world = self.make_world()
        world.generate_early()
        data = world.fill_slot_data()

        self.assertEqual(data["schema_version"], 19)
        self.assertEqual(
            data["implementation_version"],
            "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23-full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28",
        )
        self.assertTrue(data["implementation_version"].startswith("area-routing"))
        self.assertTrue(data["implementation_version"].startswith("area-routing-plant-pipes-0.15"))
        self.assertEqual(data["generation_foundation_version"], "generation-foundation-0.16")
        self.assertEqual(data["required_stars"], 50)
        self.assertEqual(data["difficulty"], {"value": 0, "name": "Normal"})
        self.assertEqual(data["starting_area_requested"], "Random")
        self.assertEqual(data["starting_area_resolved"], "Roots")
        self.assertEqual(data["validated_starting_areas"], ["Roots"])
        self.assertEqual(data["star_item_name"], "Star")
        self.assertEqual(data["star_item_count"], 66)
        self.assertTrue(data["star_items_active"])
        self.assertEqual(data["generated_star_requirements"], world.generated_star_requirements)
        self.assertEqual(data["generated_star_requirements_depth_model"], "conservative-native-routes-v1")
        self.assertTrue(data["client_star_gate_enforcement_active"])
        self.assertTrue(data["difficulty_filtering_active"])
        self.assertFalse(data["development_area_access_victory_active"])
        self.assertTrue(data["randomize_hip_glasses_chicken_bucket"])
        self.assertTrue(data["randomize_level_2_money_cassette"])
        self.assertEqual(data["cassette_schema"], 1)
        self.assertTrue(data["full_cassette_randomization"])
        self.assertEqual(data["cassette_count"], 30)
        self.assertEqual(len(data["cassette_items"]), 30)
        self.assertEqual(len(data["cassette_sources"]), 30)
        self.assertEqual(
            data["cassette_reused_locations"]["Quicksand"],
            "Music Lab - 32 Point Chest",
        )
        self.assertTrue(data["implementation_version"].endswith("full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28"))
        self.assertEqual(data["vanilla_game_garage_cartridge"], "Vampire Killer")
        self.assertEqual(
            data["vanilla_game_garage_cartridge_item"],
            "Vampire Killer Cartridge",
        )
        self.assertTrue(data["game_garage_vanilla_entrance_pickup_required"])
        self.assertNotIn("Vampire Killer", data["game_garage_cartridge_items"])
        self.assertNotIn("Vampire Killer", data["cartridge_source_locations"])
        self.assertEqual(data["repair_schema_version"], "next-release-repair-0.18")
        self.assertEqual(data["consolidated_preview_version"], "consolidated-preview-0.19")
        self.assertEqual(data["preview_ability_items_registered"], ["Hypno Pan", "Violance"])
        self.assertTrue(data["preview_ability_items_generated"])
        self.assertTrue(data["plant_pipes_durable_reconciliation"])
        self.assertEqual(data["music_lab_safe_location_classification"], "conservative-v1")
        self.assertEqual(data["garage_routing_mode"], "interaction-gated")
        self.assertTrue(data["royal_phone_side_split"])
        self.assertTrue(data["level_22_native_mapping"])
        self.assertTrue(data["native_difficulty_choice"])
        self.assertTrue(data["roots_intro_suppression"])
        self.assertEqual(data["hip_glasses_item"], "Hip Glasses")
        self.assertEqual(data["chicken_bucket_item"], "Chicken Bucket")
        self.assertEqual(data["hip_glasses_source_location"], "Roots - Level 4 - Hip Glasses")
        self.assertEqual(data["bucket_minion_trade_location"], "Roots - Bucket Minion Trade")
        self.assertEqual(data["hip_glasses_source_flag"], "LEVEL_08_GLASSES_COLLECTED")
        self.assertEqual(data["hip_glasses_native_flag"], "HIP_GLASSES_BAG_ITEM")
        self.assertEqual(
            data["bucket_trade_flag"],
            "ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES",
        )
        self.assertEqual(data["chicken_bucket_native_flag"], "CHICKEN_BUCKET_BAG_ITEM")
        self.assertEqual(
            data["combo_bucket_conversion_flag"],
            "LEVEL_09_COMBO_ABILITY_EARNED",
        )
        self.assertFalse(data["starting_area_forced"])
        self.assertIn("difficulty filtering", data["routing_logic_note"])
        self.assertNotIn("inactive previews", data["routing_logic_note"])

    def test_slot_data_publishes_the_strict_music_lab_point_contract(self):
        data = self.build_world().fill_slot_data()

        self.assertTrue(data["implementation_version"].endswith("full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28"))
        self.assertEqual(data["schema_version"], 19)
        self.assertTrue(data["music_lab_points_enabled"])
        self.assertEqual(data["music_lab_points_schema"], 1)
        self.assertEqual(data["music_lab_point_items"], {
            "Music Lab Point": 187256153,
            "Music Lab Point Bundle": 187256154,
            "Music Lab Point Large Bundle": 187256155,
        })
        self.assertEqual(data["music_lab_point_values"], {
            "Music Lab Point": 1,
            "Music Lab Point Bundle": 10,
            "Music Lab Point Large Bundle": 20,
        })
        self.assertEqual(data["music_lab_point_counts"], {
            "Music Lab Point": 10,
            "Music Lab Point Bundle": 3,
            "Music Lab Point Large Bundle": 7,
        })
        self.assertEqual(data["music_lab_point_total_instances"], 20)
        self.assertEqual(data["music_lab_point_total_value"], 180)
        self.assertEqual(data["music_lab_point_max_effective"], 180)
        self.assertEqual(data["music_lab_point_thresholds"], {
            5: "Music Lab - 5 Point Chest",
            10: "Music Lab - 10 Point Chest",
            20: "Music Lab - 20 Point Chest",
            32: "Music Lab - 32 Point Chest",
            46: "Music Lab - 46 Point Chest",
            64: "Music Lab - 64 Point Chest",
            89: "Music Lab - 89 Point Chest",
            111: "Music Lab - 111 Point Chest",
            140: "Music Lab - 140 Point Chest",
        })

    def test_slot_data_has_safe_defaults_for_direct_construction(self):
        world = object.__new__(self.module.SCRCWorld)
        data = world.fill_slot_data()

        self.assertEqual(data["required_stars"], 50)
        self.assertEqual(data["difficulty"], {"value": 0, "name": "Normal"})
        self.assertEqual(data["starting_area_requested"], "Random")
        self.assertEqual(data["generated_star_requirements"], {})
        self.assertEqual(data["active_location_count"], 164)
        self.assertEqual(data["active_campaign_star_tiers"], [1])
        self.assertEqual(data["active_medal_tiers"], ["Bronze"])

    def test_slot_data_matches_neutral_full_cassette_contract(self):
        fixture_path = Path(__file__).resolve().parents[2] / "contracts" / "cassette-contract-v1.json"
        fixture = json.loads(fixture_path.read_text(encoding="utf-8"))
        data = self.make_world().fill_slot_data()

        self.assertEqual(fixture["schema"], data["cassette_schema"])
        self.assertEqual(fixture["count"], data["cassette_count"])
        self.assertEqual(
            {entry["display_song"]: entry["item_name"] for entry in fixture["entries"]},
            data["cassette_items"],
        )
        self.assertEqual(
            {entry["display_song"]: entry["source_name"] for entry in fixture["entries"]},
            data["cassette_sources"],
        )
        self.assertEqual(
            {
                entry["display_song"]: entry["source_name"]
                for entry in fixture["entries"]
                if entry["reused_location"]
            },
            data["cassette_reused_locations"],
        )

    def test_fill_slot_data_rejects_invalid_raw_difficulty_values(self):
        for value in (True, 4):
            with self.subTest(difficulty=value):
                world = self.make_world(difficulty=value)
                with self.assertRaisesRegex(ValueError, "difficulty"):
                    world.fill_slot_data()

    def test_slot_data_reports_active_difficulty_filtering(self):
        world = self.build_world(difficulty=1)
        data = world.fill_slot_data()

        self.assertTrue(data["difficulty_filtering_active"])
        self.assertEqual(data["active_location_count"], 222)
        self.assertEqual(data["active_campaign_star_tiers"], [1, 2])
        self.assertEqual(data["active_medal_tiers"], ["Bronze", "Silver"])
        self.assertTrue(data["implementation_version"].endswith("full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28"))

    def test_vampire_killer_is_vanilla_but_permanent_ids_are_preserved(self):
        world = self.build_world(difficulty=0)
        names = self.addressed_names(world)
        item_names = [item.name for item in world.multiworld.itempool]

        self.assertNotIn("Cartridge Pickup - Vampire Killer", names)
        self.assertNotIn("Vampire Killer Cartridge", item_names)
        self.assertEqual(
            self.module.LOCATION_NAME_TO_ID["Cartridge Pickup - Vampire Killer"],
            187256176,
        )
        self.assertEqual(
            self.module.ITEM_NAME_TO_ID["Vampire Killer Cartridge"],
            187256114,
        )

        vampire = world.multiworld.get_location("Game Garage - Vampire Killer - Bronze", 1)
        smooch = world.multiworld.get_location("Game Garage - Smooch - Bronze", 1)
        self.assertTrue(vampire.access_rule(State([])))
        self.assertFalse(smooch.access_rule(State([])))
        self.assertTrue(smooch.access_rule(State(["Smooch Cartridge"])))

    def test_equal_seed_and_options_are_reproducible(self):
        first = self.make_world(seed=77, difficulty=2)
        second = self.make_world(seed=77, difficulty=2)
        first.generate_early()
        second.generate_early()

        self.assertEqual(first.generated_star_requirements, second.generated_star_requirements)
        self.assertEqual(len(first.active_location_names), len(second.active_location_names))

    def test_victory_requires_post_threshold_repeatable_boss_route(self):
        world = self.build_world(); world.set_rules()
        rule = world.multiworld.get_location("Victory", 1).access_rule
        self.assertFalse(rule(State(self.module.AREA_ACCESS_ITEMS)))
        self.assertFalse(rule(State(["Royal Corridor Access", "Plant Pipes"] + ["Star"] * 49)))
        self.assertTrue(rule(State(["Royal Corridor Access", "Plant Pipes"] + ["Star"] * 50)))
        self.assertFalse(rule(State(["Star"] * 66)))
        self.assertTrue(world.multiworld.completion_condition[1](State(["Victory"])))
        self.assertFalse(world.multiworld.completion_condition[1](State([])))

    def test_level_22_ordinary_locations_use_new_permanent_ids_without_victory(self):
        expected = {
            "Level 22 - Completion": 187256182,
            "Level 22 - 1 Star": 187256183,
            "Level 22 - 2 Stars": 187256184,
            "Level 22 - 3 Stars": 187256185,
        }
        for name, location_id in expected.items():
            self.assertEqual(self.module.LOCATION_NAME_TO_ID[name], location_id)

        world = self.build_world()
        royal = next(region for region in world.multiworld.regions if region.name == "Royal Corridor")
        self.assertTrue(
            {
                "Level 21 - Completion",
                "Level 21 - 1 Star",
                "Level 22 - Completion",
                "Level 22 - 1 Star",
                "Cassette Source - Another Day In Paradise",
            }.issubset({location.name for location in royal.locations})
        )
        self.assertIn("Victory", {location.name for location in royal.locations})

        progression = world.create_item("Plant Pipes")
        filler = world.create_item("Stardust")
        self.assertTrue(world.multiworld.get_location("Level 22 - 1 Star", 1).item_rule(progression))
        with self.assertRaises(KeyError):
            world.multiworld.get_location("Level 22 - 2 Stars", 1)

        expert = self.build_world(difficulty=2)
        two_stars = expert.multiworld.get_location("Level 22 - 2 Stars", 1)
        self.assertFalse(two_stars.item_rule(progression))
        self.assertTrue(two_stars.item_rule(filler))

    def test_royal_access_reaches_phone_side_royal_campaign_routes(self):
        world = self.build_world()
        world.set_rules()
        state = State(["Royal Corridor Access", "Plant Pipes"] + ["Star"] * world.generated_star_requirements["Level 22"])
        reachable = self.reachable_regions(world, state)

        self.assertIn("Royal Corridor", reachable)
        for name in ("Level 22 - Completion", "Level 22 - 1 Star"):
            location = world.multiworld.get_location(name, 1)
            self.assertIn(location.parent_region.name, reachable)
            self.assertTrue(location.access_rule(state))

        exposed_names = {
            location.name
            for region in world.multiworld.regions
            if region.name in reachable
            for location in region.locations
        }
        self.assertIn("Level 21 - Completion", exposed_names)
        self.assertIn("Level 21 - 1 Star", exposed_names)
        self.assertNotIn("Royal Corridor - Star Eater", exposed_names)
        self.assertNotIn("Royal Corridor - Bridge Complete", exposed_names)

    def test_option_matrix_all_inventory_reaches_every_progression_slot(self):
        # Actual restrictive-fill/playthrough matrix runs separately against AP core.
        # This fixture verifies route completeness, not an arbitrary greedy fill.
        for difficulty in range(4):
            for goal in (1,25,50,66):
                for seed in range(3):
                    world=self.make_world(seed=seed,difficulty=difficulty,required_stars=goal)
                    world.generate_early();world.create_regions();world.create_items();world.set_rules()
                    state=self.full_state()
                    state.owned['Music Lab Point Large Bundle']=9
                    for region in world.multiworld.regions:
                        for location in region.locations:
                            if location.address and location.item_rule(world.create_item('Star')):
                                self.assertTrue(self.location_is_reachable(world,location,state),location.name)
                    self.assertEqual(sum(i.name=='Star' for i in world.multiworld.itempool),66)

    def test_retained_bk_seed_rejects_required_items_at_unsafe_locations(self):
        fixture_path = Path(__file__).parent / "fixtures" / "bk_seed_28223804408101432968.json"
        facts = json.loads(fixture_path.read_text(encoding="utf-8"))
        world = self.make_world(seed=facts["seed"], difficulty=0)
        world.generate_early()
        active = world.active_location_names

        platinum_placements = [
            placement
            for placement in facts["unsafe_placements"]
            if placement["location"].endswith("Platinum")
        ]
        for placement in platinum_placements:
            with self.subTest(**placement):
                self.assertFalse(
                    self.module.required_progression_allowed(placement["location"], active)
                )

        world.create_regions()
        world.set_rules()
        chest = world.multiworld.get_location("Music Lab - 64 Point Chest", world.player)
        plant_pipes = world.create_item("Plant Pipes")
        self.assertTrue(chest.item_rule(plant_pipes))
        self.assertFalse(chest.access_rule(State([])))

        state = State(item.name for item in world.multiworld.precollected)
        self.assertEqual(
            state.owned,
            Counter(item.name for item in world.multiworld.precollected),
        )
        self.assertFalse(chest.access_rule(state))

        prerequisite_points = {
            "Level 1 - Completion": "Music Lab Point",
            "Level 2 - Completion": "Music Lab Point",
            self.module.ROOTS_GECKO_WEED_KILLER: "Music Lab Point",
            "Cassette Source - Gold": "Music Lab Point",
            self.module.LEVEL_2_MONEY_CASSETTE_SOURCE: "Music Lab Point",
            "Music Lab - 5 Point Chest": "Music Lab Point Bundle",
            "Music Lab - 10 Point Chest": "Music Lab Point Bundle",
            "Music Lab - 20 Point Chest": "Music Lab Point Large Bundle",
            "Music Lab - 32 Point Chest": "Music Lab Point Large Bundle",
        }
        self.assertTrue(
            self.collect_placement_spheres(
                world,
                state,
                prerequisite_points,
            )
        )
        self.assertGreaterEqual(
            self.module.weighted_music_lab_points(state, world.player),
            64,
        )
        self.assertFalse(state.has("Plant Pipes", world.player))
        self.assertTrue(chest.access_rule(state))
        self.assertTrue(
            self.collect_placement_spheres(
                world,
                state,
                {"Music Lab - 64 Point Chest": "Plant Pipes"},
            )
        )
        self.assertTrue(state.has("Plant Pipes", world.player))

    def test_music_lab_point_sphere_fixtures_accept_a_chain_and_reject_a_deadlock(self):
        world = self.build_world(difficulty=3)
        world.set_rules()
        valid_state = State(["Music Lab Point"] * 5)
        valid_chain = {
            "Music Lab - 5 Point Chest": "Music Lab Point Bundle",
            "Music Lab - 10 Point Chest": "Music Lab Point Bundle",
            "Music Lab - 20 Point Chest": "Music Lab Point Large Bundle",
            "Music Lab - 32 Point Chest": "Music Lab Point Large Bundle",
            "Music Lab - 46 Point Chest": "Music Lab Point Large Bundle",
            "Music Lab - 64 Point Chest": "Music Lab Point Large Bundle",
            "Music Lab - 89 Point Chest": "Music Lab Point Large Bundle",
            "Music Lab - 111 Point Chest": "Music Lab Point Large Bundle",
        }
        self.assertTrue(self.collect_placement_spheres(world, valid_state, valid_chain))
        self.assertGreaterEqual(
            self.module.weighted_music_lab_points(valid_state, world.player),
            140,
        )

        impossible_state = State([])
        impossible_chain = {
            "Music Lab - 5 Point Chest": "Music Lab Point Large Bundle",
            "Music Lab - 10 Point Chest": "Music Lab Point Large Bundle",
        }
        self.assertFalse(
            self.collect_placement_spheres(world, impossible_state, impossible_chain)
        )

    def test_safe_location_rules_allow_filler_and_point_chests_allow_progression(self):
        world = self.build_world(difficulty=0)
        progression = world.create_item("Plant Pipes")
        filler = world.create_item("Stardust")

        chest = world.multiworld.get_location("Music Lab - 64 Point Chest", 1)
        bronze = world.multiworld.get_location("Music Lab Cassette - Lets Go - Bronze", 1)

        self.assertTrue(chest.item_rule(progression))
        self.assertTrue(chest.item_rule(filler))
        self.assertTrue(bronze.item_rule(progression))
        with self.assertRaises(KeyError):
            world.multiworld.get_location("Music Lab Cassette - Lets Go - Platinum", 1)

        perfection = self.build_world(difficulty=3)
        platinum = perfection.multiworld.get_location("Music Lab Cassette - Lets Go - Platinum", 1)
        self.assertTrue(platinum.item_rule(filler))
        self.assertTrue(platinum.item_rule(progression))


if __name__ == "__main__":
    unittest.main()
