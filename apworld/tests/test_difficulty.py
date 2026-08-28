import unittest

from support import load_scrc_module


difficulty = load_scrc_module("difficulty")


class DifficultyTests(unittest.TestCase):
    ALL_LOCATIONS = (
        "Level 1 - Completion",
        "Game Garage - Smooch - Bronze",
        "Game Garage - Smooch - Silver",
        "Game Garage - Smooch - Gold",
        "Game Garage - Smooch - Platinum",
        "Music Lab Cassette - Zen - Bronze",
        "Music Lab Cassette - Zen - Silver",
        "Music Lab Cassette - Zen - Gold",
        "Music Lab Cassette - Zen - Platinum",
        "Music Lab - 5 Point Chest",
        "Roots - Gecko's Weed Killer",
    )

    def test_normal_keeps_bronze_and_non_tier_locations(self):
        result = difficulty.filter_locations_for_difficulty(self.ALL_LOCATIONS, 0)

        self.assertEqual(
            result,
            (
                "Level 1 - Completion",
                "Game Garage - Smooch - Bronze",
                "Music Lab Cassette - Zen - Bronze",
                "Music Lab - 5 Point Chest",
                "Roots - Gecko's Weed Killer",
            ),
        )

    def test_hard_keeps_bronze_and_silver(self):
        result = difficulty.filter_locations_for_difficulty(self.ALL_LOCATIONS, 1)

        self.assertIn("Game Garage - Smooch - Silver", result)
        self.assertIn("Music Lab Cassette - Zen - Silver", result)
        self.assertNotIn("Game Garage - Smooch - Gold", result)

    def test_expert_keeps_through_gold(self):
        result = difficulty.filter_locations_for_difficulty(self.ALL_LOCATIONS, 2)

        self.assertIn("Game Garage - Smooch - Gold", result)
        self.assertIn("Music Lab Cassette - Zen - Gold", result)
        self.assertNotIn("Game Garage - Smooch - Platinum", result)

    def test_perfection_keeps_every_location(self):
        result = difficulty.filter_locations_for_difficulty(self.ALL_LOCATIONS, 3)

        self.assertEqual(result, self.ALL_LOCATIONS)

    def test_campaign_star_tiers_are_cumulative(self):
        self.assertEqual(difficulty.campaign_star_tiers(0), frozenset({1}))
        self.assertEqual(difficulty.campaign_star_tiers(1), frozenset({1, 2}))
        self.assertEqual(
            difficulty.campaign_star_tiers(2), frozenset({1, 2, 3})
        )
        self.assertEqual(
            difficulty.campaign_star_tiers(3), frozenset({1, 2, 3})
        )

    def test_medal_tiers_are_cumulative(self):
        self.assertEqual(difficulty.medal_tiers(0), frozenset({"Bronze"}))
        self.assertEqual(
            difficulty.medal_tiers(1), frozenset({"Bronze", "Silver"})
        )
        self.assertEqual(
            difficulty.medal_tiers(2), frozenset({"Bronze", "Silver", "Gold"})
        )
        self.assertEqual(
            difficulty.medal_tiers(3),
            frozenset({"Bronze", "Silver", "Gold", "Platinum"}),
        )

    def test_campaign_location_filter_uses_existing_star_locations(self):
        names = (
            "Level 22 - Completion",
            "Level 22 - 1 Star",
            "Level 22 - 2 Stars",
            "Level 22 - 3 Stars",
        )
        self.assertEqual(
            difficulty.filter_locations_for_difficulty(names, 0),
            names[:2],
        )
        self.assertEqual(
            difficulty.filter_locations_for_difficulty(names, 1),
            names[:3],
        )
        self.assertEqual(
            difficulty.filter_locations_for_difficulty(names, 2),
            names,
        )
        self.assertEqual(
            difficulty.filter_locations_for_difficulty(names, 3),
            names,
        )

    def test_is_location_active_recognizes_only_registered_style_campaign_stars(self):
        self.assertTrue(difficulty.is_location_active("Level 22 - 1 Star", 0))
        self.assertFalse(difficulty.is_location_active("Level 22 - 2 Stars", 0))
        self.assertTrue(difficulty.is_location_active("Level 22 - 2 Stars", 1))
        self.assertTrue(difficulty.is_location_active("Other - 2 Star", 0))

    def test_returns_tuple_and_preserves_input_order(self):
        source = ["Music Lab - 5 Point Chest", "Game Garage - Smooch - Bronze"]

        result = difficulty.filter_locations_for_difficulty(source, 0)

        self.assertIsInstance(result, tuple)
        self.assertEqual(result, tuple(source))

    def test_rejects_invalid_difficulty_values(self):
        for value in (-1, 4, True, "normal"):
            with self.subTest(value=value):
                selectors = (
                    lambda: difficulty.campaign_star_tiers(value),
                    lambda: difficulty.medal_tiers(value),
                    lambda: difficulty.is_location_active("Level 1 - Completion", value),
                    lambda: difficulty.filter_locations_for_difficulty(
                        self.ALL_LOCATIONS, value
                    ),
                )
                for selector in selectors:
                    with self.assertRaisesRegex(ValueError, "difficulty"):
                        selector()


if __name__ == "__main__":
    unittest.main()
