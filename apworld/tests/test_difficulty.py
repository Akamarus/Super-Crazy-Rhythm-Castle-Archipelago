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

    def test_returns_tuple_and_preserves_input_order(self):
        source = ["Music Lab - 5 Point Chest", "Game Garage - Smooch - Bronze"]

        result = difficulty.filter_locations_for_difficulty(source, 0)

        self.assertIsInstance(result, tuple)
        self.assertEqual(result, tuple(source))

    def test_rejects_invalid_difficulty_values(self):
        for value in (-1, 4, True, "normal"):
            with self.subTest(value=value):
                with self.assertRaisesRegex(ValueError, "difficulty"):
                    difficulty.filter_locations_for_difficulty(self.ALL_LOCATIONS, value)


if __name__ == "__main__":
    unittest.main()
