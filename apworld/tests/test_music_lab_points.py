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
        self.assertEqual(len(self.points.MUSIC_LAB_POINT_POOL), 20)
        self.assertEqual(
            sum(
                entry.value * entry.count
                for entry in self.points.MUSIC_LAB_POINT_ITEMS
            ),
            180,
        )
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


if __name__ == "__main__":
    unittest.main()
