import sys
import types
import unittest

from support import load_scrc_module


class ItemPlanningTests(unittest.TestCase):
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

    def test_plans_66_individual_stars_with_one_network_id(self):
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
        self.assertEqual(self.items.planned_star_names(), ("Star",) * 66)

    def test_each_pool_plan_is_new_and_immutable(self):
        first = self.items.planned_star_names()
        second = self.items.planned_star_names()

        self.assertIsNot(first, second)
        self.assertIsInstance(first, tuple)

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

    def test_capacity_accepts_exact_fit(self):
        self.items.validate_planned_item_capacity(79, 13)

    def test_capacity_rejects_insufficient_locations(self):
        with self.assertRaisesRegex(ValueError, "79 required.*78 locations"):
            self.items.validate_planned_item_capacity(78, 13)

    def test_capacity_rejects_invalid_counts(self):
        for available, existing in ((True, 13), (79, True), (-1, 13), (79, -1)):
            with self.subTest(available=available, existing=existing):
                with self.assertRaisesRegex(ValueError, "non-negative integers"):
                    self.items.validate_planned_item_capacity(available, existing)


if __name__ == "__main__":
    unittest.main()
