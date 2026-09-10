from dataclasses import replace
import unittest

from support import load_scrc_module


class MusicLabPointCatalogTests(unittest.TestCase):
    def setUp(self):
        self.points = load_scrc_module("music_lab_points")

    def test_catalog_defines_the_permanent_point_item_contract(self):
        expected = (
            ("Music Lab Point", 187256153, 1, 10),
            ("Music Lab Point Bundle", 187256154, 10, 3),
            ("Music Lab Point Large Bundle", 187256155, 20, 7),
        )

        self.assertEqual(
            tuple(
                (entry.name, entry.item_id, entry.value, entry.count)
                for entry in self.points.MUSIC_LAB_POINT_ITEMS
            ),
            expected,
        )
        self.assertEqual(self.points.MUSIC_LAB_POINT_TOTAL_INSTANCES, 20)
        self.assertEqual(len(self.points.MUSIC_LAB_POINT_POOL), 20)
        self.assertEqual(
            sum(
                entry.value * entry.count
                for entry in self.points.MUSIC_LAB_POINT_ITEMS
            ),
            180,
        )
        self.assertEqual(self.points.MUSIC_LAB_POINT_TOTAL_VALUE, 180)
        self.assertEqual(self.points.MUSIC_LAB_POINT_MAX_EFFECTIVE, 180)
        self.assertEqual(
            tuple(self.points.MUSIC_LAB_POINT_THRESHOLDS),
            (5, 10, 20, 32, 46, 64, 89, 111, 140),
        )

    def test_validation_rejects_invalid_catalog_fixtures_with_diagnostics(self):
        first = self.points.MUSIC_LAB_POINT_ITEMS[0]
        fixtures = (
            (
                "duplicate names",
                self.points.MUSIC_LAB_POINT_ITEMS[:1]
                + (replace(self.points.MUSIC_LAB_POINT_ITEMS[1], name=first.name),)
                + self.points.MUSIC_LAB_POINT_ITEMS[2:],
                self.points.MUSIC_LAB_POINT_THRESHOLDS,
                "name",
            ),
            (
                "duplicate IDs",
                self.points.MUSIC_LAB_POINT_ITEMS[:1]
                + (replace(self.points.MUSIC_LAB_POINT_ITEMS[1], item_id=first.item_id),)
                + self.points.MUSIC_LAB_POINT_ITEMS[2:],
                self.points.MUSIC_LAB_POINT_THRESHOLDS,
                "item_id",
            ),
            (
                "non-positive value",
                (replace(first, value=0),) + self.points.MUSIC_LAB_POINT_ITEMS[1:],
                self.points.MUSIC_LAB_POINT_THRESHOLDS,
                "value",
            ),
            (
                "non-positive count",
                (replace(first, count=0),) + self.points.MUSIC_LAB_POINT_ITEMS[1:],
                self.points.MUSIC_LAB_POINT_THRESHOLDS,
                "count",
            ),
            (
                "wrong total",
                self.points.MUSIC_LAB_POINT_ITEMS[:2]
                + (replace(self.points.MUSIC_LAB_POINT_ITEMS[2], count=6),),
                self.points.MUSIC_LAB_POINT_THRESHOLDS,
                "total",
            ),
            (
                "non-monotonic thresholds",
                self.points.MUSIC_LAB_POINT_ITEMS,
                {10: "Music Lab - 10 Point Chest", 5: "Music Lab - 5 Point Chest"},
                "threshold",
            ),
        )

        for label, catalog, thresholds, diagnostic in fixtures:
            with self.subTest(label=label):
                with self.assertRaisesRegex(ValueError, diagnostic):
                    self.points.validate_music_lab_point_catalog(catalog, thresholds)

    def test_weighted_points_follow_item_values_for_the_requested_player(self):
        class State:
            def __init__(self, counts):
                self.counts = counts

            def count(self, name, player):
                return self.counts.get((name, player), 0)

        single = "Music Lab Point"
        bundle = "Music Lab Point Bundle"
        large_bundle = "Music Lab Point Large Bundle"
        cases = (
            ("zero", 0, 0, 0),
            ("ten singles", 10, 0, 0),
            ("one of each bundle", 0, 1, 1),
            ("below 32", 1, 1, 1),
            ("at 32", 2, 1, 1),
            ("below 140", 9, 3, 5),
            ("at 140", 10, 3, 5),
        )

        for label, singles, bundles, large_bundles in cases:
            with self.subTest(label=label):
                state = State({
                    (single, 1): singles,
                    (bundle, 1): bundles,
                    (large_bundle, 1): large_bundles,
                    (single, 2): 99,
                    (bundle, 2): 99,
                    (large_bundle, 2): 99,
                })
                self.assertEqual(
                    self.points.weighted_music_lab_points(state, 1),
                    singles * 1 + bundles * 10 + large_bundles * 20,
                )

    def test_weighted_points_ignore_counts_owned_by_a_different_player(self):
        state = type(
            "State",
            (),
            {"count": lambda _self, _name, player: 7 if player == 2 else 0},
        )()

        self.assertEqual(self.points.weighted_music_lab_points(state, 1), 0)


if __name__ == "__main__":
    unittest.main()
