import random
import unittest

from support import load_scrc_module


requirements = load_scrc_module("star_requirements")


class StarRequirementTests(unittest.TestCase):
    def test_generates_all_22_levels_with_monotonic_bounded_values(self):
        result = requirements.generate_star_requirements(50, random.Random(12345))

        self.assertEqual(tuple(result), tuple(f"Level {number}" for number in range(1, 23)))
        values = tuple(result.values())
        self.assertEqual(values, tuple(sorted(values)))
        self.assertTrue(all(0 <= value < 50 for value in values))

    def test_same_seed_is_reproducible(self):
        first = requirements.generate_star_requirements(50, random.Random(7))
        second = requirements.generate_star_requirements(50, random.Random(7))

        self.assertEqual(first, second)

    def test_different_seeds_can_change_requirements(self):
        first = requirements.generate_star_requirements(50, random.Random(7))
        second = requirements.generate_star_requirements(50, random.Random(8))

        self.assertNotEqual(first, second)

    def test_small_goals_remain_valid(self):
        for goal in (1, 2, 10, 50, 66):
            with self.subTest(goal=goal):
                result = requirements.generate_star_requirements(goal, random.Random(99))
                requirements.validate_star_requirements(result, goal)
                self.assertLess(result["Level 22"], goal)

    def test_requirements_never_drop_when_goal_increases(self):
        for seed in (0, 1, 22, 77, 999):
            previous = None
            for goal in range(1, 67):
                current = requirements.generate_star_requirements(goal, random.Random(seed))
                if previous is not None:
                    self.assertTrue(
                        all(current[level] >= previous[level] for level in requirements.LEVEL_NAMES),
                        f"requirements dropped for seed {seed} at goal {goal}",
                    )
                previous = current

    def test_rejects_goal_outside_supported_range(self):
        for goal in (0, 67, True):
            with self.subTest(goal=goal):
                with self.assertRaisesRegex(ValueError, "required_stars"):
                    requirements.generate_star_requirements(goal, random.Random(1))

    def test_rejects_missing_or_extra_levels(self):
        valid = {f"Level {number}": 0 for number in range(1, 23)}
        missing = dict(valid)
        del missing["Level 22"]
        extra = dict(valid)
        extra["Level 23"] = 0

        with self.assertRaisesRegex(ValueError, "exactly Levels 1 through 22"):
            requirements.validate_star_requirements(missing, 50)
        with self.assertRaisesRegex(ValueError, "exactly Levels 1 through 22"):
            requirements.validate_star_requirements(extra, 50)

    def test_rejects_non_integer_negative_and_goal_thresholds(self):
        valid = {f"Level {number}": 0 for number in range(1, 23)}
        cases = (("Level 4", True, "integer"), ("Level 4", -1, "non-negative"), ("Level 22", 50, "below"))

        for level, value, message in cases:
            with self.subTest(level=level, value=value):
                candidate = dict(valid)
                candidate[level] = value
                with self.assertRaisesRegex(ValueError, message):
                    requirements.validate_star_requirements(candidate, 50)

    def test_rejects_decreasing_sequence(self):
        candidate = {f"Level {number}": 10 for number in range(1, 23)}
        candidate["Level 8"] = 9

        with self.assertRaisesRegex(ValueError, "nondecreasing"):
            requirements.validate_star_requirements(candidate, 50)


if __name__ == "__main__":
    unittest.main()
