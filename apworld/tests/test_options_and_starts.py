import dataclasses
import random
import sys
import types
import unittest

from support import load_scrc_module


starts = load_scrc_module("starting_areas")


class StartingAreaTests(unittest.TestCase):
    def test_random_and_roots_resolve_to_validated_roots(self):
        self.assertEqual(starts.resolve_starting_area(0, random.Random(1)), "Roots Access")
        self.assertEqual(starts.resolve_starting_area(1, random.Random(1)), "Roots Access")

    def test_explicit_unvalidated_starts_fail_without_substitution(self):
        cases = ((2, "Lobby"), (3, "Meat Dimension"), (4, "Cell Tower"))

        for value, area_name in cases:
            with self.subTest(value=value):
                with self.assertRaisesRegex(ValueError, f"{area_name}.*not validated"):
                    starts.resolve_starting_area(value, random.Random(1))

    def test_random_is_seed_deterministic_and_uses_only_validated_starters(self):
        self.assertEqual(starts.VALIDATED_STARTING_AREAS, ("Roots Access",))
        first = starts.resolve_starting_area(0, random.Random(44))
        second = starts.resolve_starting_area(0, random.Random(44))

        self.assertEqual(first, second)
        self.assertIn(first, starts.VALIDATED_STARTING_AREAS)

    def test_only_supported_option_values_are_accepted(self):
        self.assertEqual(tuple(starts.STARTING_AREA_NAMES), (0, 1, 2, 3, 4))
        for value in (-1, 5, True, "roots"):
            with self.subTest(value=value):
                with self.assertRaisesRegex(ValueError, "starting_area"):
                    starts.resolve_starting_area(value, random.Random(1))


class OptionDefinitionTests(unittest.TestCase):
    def setUp(self):
        self.previous_options = sys.modules.get("Options")
        options_stub = types.ModuleType("Options")

        class Choice:
            pass

        class Range:
            pass

        @dataclasses.dataclass
        class PerGameCommonOptions:
            accessibility: object

        options_stub.Choice = Choice
        options_stub.Range = Range
        options_stub.PerGameCommonOptions = PerGameCommonOptions
        sys.modules["Options"] = options_stub

    def tearDown(self):
        if self.previous_options is None:
            sys.modules.pop("Options", None)
        else:
            sys.modules["Options"] = self.previous_options

    def test_option_contract_matches_approved_values(self):
        option_definitions = load_scrc_module("options")

        self.assertEqual(
            (option_definitions.RequiredStars.range_start, option_definitions.RequiredStars.range_end),
            (1, 66),
        )
        self.assertEqual(option_definitions.RequiredStars.default, 50)
        self.assertEqual(option_definitions.Difficulty.option_normal, 0)
        self.assertEqual(option_definitions.Difficulty.option_hard, 1)
        self.assertEqual(option_definitions.Difficulty.option_expert, 2)
        self.assertEqual(option_definitions.Difficulty.option_perfection, 3)
        self.assertEqual(option_definitions.Difficulty.default, 0)
        self.assertEqual(option_definitions.StartingArea.option_random, 0)
        self.assertEqual(option_definitions.StartingArea.option_roots, 1)
        self.assertEqual(option_definitions.StartingArea.option_lobby, 2)
        self.assertEqual(option_definitions.StartingArea.option_meat_dimension, 3)
        self.assertEqual(option_definitions.StartingArea.option_cell_tower, 4)
        self.assertEqual(option_definitions.StartingArea.default, 0)

    def test_options_dataclass_adds_three_game_fields(self):
        option_definitions = load_scrc_module("options")

        self.assertEqual(
            tuple(field.name for field in dataclasses.fields(option_definitions.SCRCOptions)),
            ("accessibility", "required_stars", "difficulty", "starting_area"),
        )


if __name__ == "__main__":
    unittest.main()
