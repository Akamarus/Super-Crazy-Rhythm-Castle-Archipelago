import random
import json
from pathlib import Path
import types
import unittest

from support import FakeMultiWorld, OptionValue, load_scrc_world


class State:
    def __init__(self, owned):
        self.owned = set(owned)

    def has(self, name, player):
        return name in self.owned


class WorldIntegrationTests(unittest.TestCase):
    def setUp(self):
        self.module, cleanup = load_scrc_world()
        self.addCleanup(cleanup)

    def make_world(self, seed=22, required_stars=50, difficulty=0, starting_area=0):
        world = object.__new__(self.module.SCRCWorld)
        world.player = 1
        world.random = random.Random(seed)
        world.multiworld = FakeMultiWorld()
        world.options = types.SimpleNamespace(
            required_stars=OptionValue(required_stars),
            difficulty=OptionValue(difficulty),
            starting_area=OptionValue(starting_area),
        )
        return world

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

    def test_generate_early_resolves_start_and_builds_previews(self):
        world = self.make_world()
        world.generate_early()

        self.assertEqual([item.name for item in world.multiworld.precollected], ["Roots Access"])
        self.assertEqual(len(world.generated_star_requirements), 22)
        self.assertEqual(tuple(world.generated_star_requirements), tuple(f"Level {i}" for i in range(1, 23)))
        self.assertTrue(all(0 <= value < 50 for value in world.generated_star_requirements.values()))
        self.assertEqual(tuple(world.generated_star_requirements.values()), tuple(sorted(world.generated_star_requirements.values())))
        self.assertTrue(world.difficulty_preview_locations)
        self.assertNotIn("Game Garage - Bloody Tears - Silver", world.difficulty_preview_locations)

    def test_unsupported_fixed_start_stops_before_precollect(self):
        world = self.make_world(starting_area=2)
        with self.assertRaisesRegex(ValueError, "Lobby.*not validated"):
            world.generate_early()
        self.assertEqual(world.multiworld.precollected, [])

    def test_live_pool_size_and_contents_remain_unchanged(self):
        world = self.make_world()
        world.generate_early()
        world.create_items()

        self.assertEqual(len(world.multiworld.itempool), len(self.module.LOCATION_NAME_TO_ID))
        self.assertNotIn("Star", [item.name for item in world.multiworld.itempool])
        self.assertNotIn("Hypno Pan", [item.name for item in world.multiworld.itempool])
        self.assertNotIn("Violance", [item.name for item in world.multiworld.itempool])

    def test_preview_abilities_are_registered_but_not_generated(self):
        world = self.make_world()
        world.generate_early()
        world.create_items()

        self.assertEqual(world.create_item("Hypno Pan").code, 187256121)
        self.assertEqual(world.create_item("Hypno Pan").classification, "progression")
        self.assertEqual(world.create_item("Violance").code, 187256122)
        self.assertEqual(world.create_item("Violance").classification, "progression")
        generated_names = [item.name for item in world.multiworld.itempool]
        self.assertNotIn("Hypno Pan", generated_names)
        self.assertNotIn("Violance", generated_names)

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
        self.assertTrue(level4.access_rule(State(["Roots Access", "Weed Killer", "Plant Pipes"])))
        self.assertFalse(trade.access_rule(State([])))
        self.assertTrue(trade.access_rule(State(["Hip Glasses"])))
        self.assertFalse(combo_event.access_rule(State(["Chicken Bucket"])))
        self.assertFalse(combo_event.access_rule(State(["Bucket Minion Trade Complete"])))
        self.assertTrue(
            combo_event.access_rule(State(["Bucket Minion Trade Complete", "Chicken Bucket"]))
        )

    def test_live_pool_contains_one_of_each_roots_bucket_item(self):
        world = self.make_world()
        world.generate_early()
        world.create_items()
        names = [item.name for item in world.multiworld.itempool]

        self.assertEqual(names.count("Hip Glasses"), 1)
        self.assertEqual(names.count("Chicken Bucket"), 1)
        self.assertEqual(len(names), len(self.module.LOCATION_NAME_TO_ID))

    def test_slot_data_labels_preview_features_as_inactive(self):
        world = self.make_world()
        world.generate_early()
        data = world.fill_slot_data()

        self.assertEqual(data["schema_version"], 9)
        self.assertEqual(
            data["implementation_version"],
            "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18",
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
        self.assertFalse(data["star_items_active"])
        self.assertEqual(data["generated_star_requirements"], world.generated_star_requirements)
        self.assertEqual(data["generated_star_requirements_depth_model"], "provisional-linear-level-order")
        self.assertFalse(data["client_star_gate_enforcement_active"])
        self.assertFalse(data["difficulty_filtering_active"])
        self.assertEqual(data["difficulty_preview_location_count"], len(world.difficulty_preview_locations))
        self.assertTrue(data["development_area_access_victory_active"])
        self.assertTrue(data["randomize_hip_glasses_chicken_bucket"])
        self.assertEqual(data["repair_schema_version"], "next-release-repair-0.18")
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
        self.assertIn("v0.16", data["routing_logic_note"])
        self.assertIn("preview", data["routing_logic_note"])

    def test_slot_data_has_safe_defaults_for_direct_construction(self):
        world = object.__new__(self.module.SCRCWorld)
        data = world.fill_slot_data()

        self.assertEqual(data["required_stars"], 50)
        self.assertEqual(data["difficulty"], {"value": 0, "name": "Normal"})
        self.assertEqual(data["starting_area_requested"], "Random")
        self.assertEqual(data["generated_star_requirements"], {})
        self.assertEqual(data["difficulty_preview_location_count"], 0)

    def test_equal_seed_and_options_are_reproducible(self):
        first = self.make_world(seed=77, difficulty=2)
        second = self.make_world(seed=77, difficulty=2)
        first.generate_early()
        second.generate_early()

        self.assertEqual(first.generated_star_requirements, second.generated_star_requirements)
        self.assertEqual(len(first.difficulty_preview_locations), len(second.difficulty_preview_locations))

    def test_existing_area_access_victory_rule_is_unchanged(self):
        world = self.make_world()
        world.create_regions()
        world.set_rules()
        rule = world.multiworld.get_location("Victory", 1).access_rule

        self.assertTrue(rule(State(self.module.AREA_ACCESS_ITEMS)))
        self.assertFalse(rule(State(self.module.AREA_ACCESS_ITEMS[:-1])))
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

        world = self.make_world()
        world.create_regions()
        royal = next(region for region in world.multiworld.regions if region.name == "Royal Corridor")
        self.assertEqual(
            {location.name for location in royal.locations},
            set(expected),
        )
        self.assertNotIn("Victory", {location.name for location in royal.locations})

        progression = world.create_item("Plant Pipes")
        filler = world.create_item("Stardust")
        self.assertTrue(world.multiworld.get_location("Level 22 - 1 Star", 1).item_rule(progression))
        self.assertFalse(world.multiworld.get_location("Level 22 - 2 Stars", 1).item_rule(progression))
        self.assertTrue(world.multiworld.get_location("Level 22 - 2 Stars", 1).item_rule(filler))

    def test_royal_access_reaches_only_phone_side_level_22_route(self):
        world = self.make_world()
        world.create_regions()
        world.set_rules()
        state = State(["Royal Corridor Access"])
        reachable = self.reachable_regions(world, state)

        self.assertIn("Royal Corridor", reachable)
        for name in self.module.LEVEL_22_ORDINARY_LOCATIONS:
            location = world.multiworld.get_location(name, 1)
            self.assertIn(location.parent_region.name, reachable)
            self.assertTrue(location.access_rule(state))

        exposed_names = {
            location.name
            for region in world.multiworld.regions
            if region.name in reachable
            for location in region.locations
        }
        self.assertFalse(any(name.startswith("Level 21 -") for name in exposed_names))
        self.assertNotIn("Royal Corridor - Star Eater", exposed_names)
        self.assertNotIn("Royal Corridor - Bridge Complete", exposed_names)

    def test_seed_matrix_has_reachable_safe_slots_for_every_required_item(self):
        supported_starts = (0, 1)  # Random currently resolves only to validated Roots; Roots is explicit.
        supported_difficulties = range(4)

        for seed in range(100):
            for difficulty in supported_difficulties:
                for starting_area in supported_starts:
                    with self.subTest(seed=seed, difficulty=difficulty, starting_area=starting_area):
                        world = self.make_world(
                            seed=seed,
                            difficulty=difficulty,
                            starting_area=starting_area,
                        )
                        world.generate_early()
                        world.create_regions()
                        world.create_items()
                        world.set_rules()

                        state = State(item.name for item in world.multiworld.precollected)
                        required = [
                            item
                            for item in world.multiworld.itempool
                            if item.classification == "progression"
                        ]
                        world.random.shuffle(required)
                        used_locations = set()

                        while required:
                            reachable = self.reachable_regions(world, state)
                            selected = None
                            for item in required:
                                safe_location = next(
                                    (
                                        location
                                        for region in world.multiworld.regions
                                        if region.name in reachable
                                        for location in region.locations
                                        if location.address is not None
                                        and location.name not in used_locations
                                        and location.access_rule(state)
                                        and location.item_rule(item)
                                    ),
                                    None,
                                )
                                if safe_location is not None:
                                    selected = (item, safe_location)
                                    break

                            self.assertIsNotNone(selected, f"stranded required items: {[item.name for item in required]}")
                            item, location = selected
                            used_locations.add(location.name)
                            state.owned.add(item.name)
                            required.remove(item)

                        progression_probe = world.create_item("Plant Pipes")
                        for region in world.multiworld.regions:
                            for location in region.locations:
                                unsafe = (
                                    (location.name.startswith("Music Lab - ") and location.name.endswith(" Point Chest"))
                                    or (
                                        location.name.startswith(("Game Garage - ", "Music Lab Cassette - "))
                                        and location.name not in world.difficulty_preview_locations
                                    )
                                    or location.name in {"Level 22 - 2 Stars", "Level 22 - 3 Stars"}
                                )
                                if unsafe:
                                    self.assertFalse(location.item_rule(progression_probe), location.name)

    def test_retained_bk_seed_rejects_required_items_at_unsafe_locations(self):
        fixture_path = Path(__file__).parent / "fixtures" / "bk_seed_28223804408101432968.json"
        facts = json.loads(fixture_path.read_text(encoding="utf-8"))
        world = self.make_world(seed=facts["seed"], difficulty=0)
        world.generate_early()
        active = frozenset(world.difficulty_preview_locations)

        for placement in facts["unsafe_placements"]:
            with self.subTest(**placement):
                self.assertFalse(
                    self.module.required_progression_allowed(placement["location"], active)
                )

    def test_safe_location_rules_allow_filler_but_reject_progression(self):
        world = self.make_world(difficulty=0)
        world.generate_early()
        world.create_regions()
        progression = world.create_item("Plant Pipes")
        filler = world.create_item("Stardust")

        chest = world.multiworld.get_location("Music Lab - 64 Point Chest", 1)
        platinum = world.multiworld.get_location("Music Lab Cassette - Lets Go - Platinum", 1)
        bronze = world.multiworld.get_location("Music Lab Cassette - Lets Go - Bronze", 1)

        self.assertFalse(chest.item_rule(progression))
        self.assertTrue(chest.item_rule(filler))
        self.assertFalse(platinum.item_rule(progression))
        self.assertTrue(platinum.item_rule(filler))
        self.assertTrue(bronze.item_rule(progression))


if __name__ == "__main__":
    unittest.main()
