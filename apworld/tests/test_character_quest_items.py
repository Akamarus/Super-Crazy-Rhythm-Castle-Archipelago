import random
import types
import unittest
from collections import Counter

from support import FakeMultiWorld, OptionValue, load_scrc_world


class CharacterQuestItemWorldTests(unittest.TestCase):
    def setUp(self):
        self.module, cleanup = load_scrc_world()
        self.addCleanup(cleanup)

    def make_world(self, difficulty=0):
        world = object.__new__(self.module.SCRCWorld)
        world.player = 1
        world.random = random.Random(925)
        world.multiworld = FakeMultiWorld()
        world.options = types.SimpleNamespace(
            required_stars=OptionValue(50), difficulty=OptionValue(difficulty),
            starting_area=OptionValue(0))
        world.generate_early()
        world.create_regions()
        world.create_items()
        return world

    def test_exact_ids_progression_classification_and_existing_source_checks(self):
        world = self.make_world()
        expected = {
            "Old Game Data": (187256159, "Music Lab - 5 Point Chest", 187256169),
            "Car Battery": (187256160, "Music Lab - 20 Point Chest", 187256171),
        }
        locations = [location for region in world.multiworld.regions
                     for location in region.locations]
        for name, (item_id, source, check_id) in expected.items():
            item = world.create_item(name)
            self.assertEqual(item.code, item_id)
            self.assertEqual(item.classification, "progression")
            matching = [location for location in locations if location.name == source]
            self.assertEqual(len(matching), 1)
            self.assertEqual(matching[0].address, check_id)

    def test_pool_replaces_two_fillers_at_every_difficulty_without_activating_staging(self):
        for difficulty, count in enumerate((164, 222, 280, 316)):
            with self.subTest(difficulty=difficulty):
                world = self.make_world(difficulty)
                names = Counter(item.name for item in world.multiworld.itempool)
                self.assertEqual(len(world.multiworld.itempool), count)
                self.assertEqual(names["Old Game Data"], 1)
                self.assertEqual(names["Car Battery"], 1)
                self.assertEqual(names["Stardust"], count - 137)
                for inactive in ("Important Letters", "Bean Trumpet", "Demolition Certificate",
                                 "Victory",):
                    self.assertEqual(names[inactive], 0)

    def test_schema18_exports_exact_native_bag_and_reused_check_maps(self):
        data = self.make_world().fill_slot_data()
        self.assertEqual(data["schema_version"], 19)
        self.assertTrue(data["implementation_version"].endswith("-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28"))
        self.assertEqual(data["character_quest_item_schema"], 1)
        self.assertIs(data["randomize_character_quest_items"], True)
        self.assertEqual(data["character_quest_items"], {
            "Old Game Data": "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM",
            "Car Battery": "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM",
        })
        self.assertEqual(data["character_quest_item_locations"], {
            "Old Game Data": "Music Lab - 5 Point Chest",
            "Car Battery": "Music Lab - 20 Point Chest",
        })
        for inactive in ("randomize_lobby_letters_bean_trumpet", "randomize_demolition_certificate"):
            self.assertIs(data[inactive], False)


if __name__ == "__main__":
    unittest.main()
