import random
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

    def test_slot_data_labels_preview_features_as_inactive(self):
        world = self.make_world()
        world.generate_early()
        data = world.fill_slot_data()

        self.assertEqual(data["schema_version"], 8)
        self.assertEqual(
            data["implementation_version"],
            "area-routing-plant-pipes-0.15-generation-foundation-0.16",
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
        world.set_rules()
        rule = world.multiworld.victory.access_rule

        self.assertTrue(rule(State(self.module.AREA_ACCESS_ITEMS)))
        self.assertFalse(rule(State(self.module.AREA_ACCESS_ITEMS[:-1])))
        self.assertTrue(world.multiworld.completion_condition[1](State(["Victory"])))
        self.assertFalse(world.multiworld.completion_condition[1](State([])))


if __name__ == "__main__":
    unittest.main()
