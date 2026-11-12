import sys
import types
import unittest

from support import load_scrc_module


class ItemCatalogTests(unittest.TestCase):
    def setUp(self):
        self.previous_base_classes = sys.modules.get("BaseClasses")
        base_classes = types.ModuleType("BaseClasses")

        class ItemClassification:
            progression = "progression"
            useful = "useful"

        base_classes.ItemClassification = ItemClassification
        sys.modules["BaseClasses"] = base_classes
        self.items = load_scrc_module("items")

    def tearDown(self):
        if self.previous_base_classes is None:
            sys.modules.pop("BaseClasses", None)
        else:
            sys.modules["BaseClasses"] = self.previous_base_classes

    def test_66_individual_stars_share_one_network_id(self):
        self.assertEqual(self.items.STAR_ITEM_NAME, "Star")
        self.assertEqual(self.items.STAR_ITEM_COUNT, 66)
        self.assertEqual(
            self.items.NEW_ITEM_NAME_TO_ID,
            {
                "Star": 187256118,
                "Hypno Pan": 187256121,
                "Violance": 187256122,
                "Money Cassette": 187256123,
            },
        )

    def test_preview_abilities_have_stable_names_and_progression_classification(self):
        self.assertEqual(self.items.HYPNO_PAN_ITEM_NAME, "Hypno Pan")
        self.assertEqual(self.items.VIOLANCE_ITEM_NAME, "Violance")
        self.assertEqual(self.items.NEW_ITEM_CLASSIFICATIONS["Hypno Pan"], "progression")
        self.assertEqual(self.items.NEW_ITEM_CLASSIFICATIONS["Violance"], "progression")

    def test_money_cassette_has_a_permanent_progression_id(self):
        self.assertEqual(self.items.MONEY_CASSETTE_ITEM_NAME, "Money Cassette")
        self.assertEqual(self.items.NEW_ITEM_NAME_TO_ID["Money Cassette"], 187256123)
        self.assertEqual(
            self.items.NEW_ITEM_CLASSIFICATIONS["Money Cassette"],
            "progression",
        )


if __name__ == "__main__":
    unittest.main()
