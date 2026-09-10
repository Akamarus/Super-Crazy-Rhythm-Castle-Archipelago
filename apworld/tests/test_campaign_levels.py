from dataclasses import replace
import unittest

from support import load_scrc_module


class CampaignLevelCatalogTests(unittest.TestCase):
    def setUp(self):
        self.campaign = load_scrc_module("campaign_levels")

    def test_catalog_locks_all_native_campaign_identities_and_location_tiers(self):
        expected = (
            (1, "The Little Things", "Level_05", "Roots"),
            (2, "Pop Party", "Level_06", "Roots"),
            (3, "Jolt City", "Level_07", "Roots"),
            (4, "Quieres Bailar", "Level_08", "Roots"),
            (5, "Lift Quest", "Level_09", "Roots"),
            (6, "Boring Room", "Level_02", "Lobby"),
            (7, "Demolition Training", "Level_19", "Lobby"),
            (8, "Minim Tower", "Level_11", "Lobby"),
            (9, "School Trip", "Level_20", "Lobby"),
            (10, "The Vault", "Level_01", "Lobby"),
            (11, "Act 1: Flavor", "Level_12", "Meat Dimension"),
            (12, "Act 2", "Level_15", "Meat Dimension"),
            (13, "Act 3", "Level_22", "Meat Dimension"),
            (14, "Act 4", "Level_23", "Meat Dimension"),
            (15, "Central Mainframe", "Level_16", "Cell Tower"),
            (16, "Thief Prince", "Level_24", "Cell Tower"),
            (17, "Cold Storage", "Level_21", "Lobby"),
            (18, "Darkness", "Level_03", "Tower of Fear"),
            (19, "Escape", "Level_13", "Tower of Fear"),
            (20, "Loneliness", "Level_25", "Tower of Fear"),
            (21, "Locker Room", "Level_14", "Royal Corridor"),
            (22, "King Ferdinand I", "Level_28", "Royal Corridor"),
        )

        actual = tuple(
            (level.number, level.display_name, level.internal_id, level.area)
            for level in self.campaign.CAMPAIGN_LEVELS
        )

        self.assertEqual(actual, expected)
        self.assertEqual(len({level.number for level in self.campaign.CAMPAIGN_LEVELS}), 22)
        self.assertEqual(len({level.internal_id for level in self.campaign.CAMPAIGN_LEVELS}), 22)
        self.assertEqual(
            self.campaign.CAMPAIGN_LEVELS_BY_INTERNAL_ID,
            {level.internal_id: level for level in self.campaign.CAMPAIGN_LEVELS},
        )
        self.assertEqual(len(self.campaign.CAMPAIGN_LOCATION_NAMES), 88)
        self.assertEqual(len(set(self.campaign.CAMPAIGN_LOCATION_NAMES)), 88)
        self.assertEqual(
            self.campaign.campaign_location_names(),
            self.campaign.CAMPAIGN_LOCATION_NAMES,
        )
        for level in self.campaign.CAMPAIGN_LEVELS:
            self.assertEqual(
                tuple(
                    name.rsplit(" - ", 1)[1]
                    for name in self.campaign.campaign_location_names(level)
                ),
                ("Completion", "1 Star", "2 Stars", "3 Stars"),
            )

    def test_catalog_preserves_existing_ids_and_appends_the_new_block(self):
        legacy = {
            "Level 1 - Completion": 187256001,
            "Level 2 - Completion": 187256002,
            "Level 3 - Completion": 187256003,
            "Level 22 - Completion": 187256182,
            "Level 22 - 1 Star": 187256183,
            "Level 22 - 2 Stars": 187256184,
            "Level 22 - 3 Stars": 187256185,
        }

        self.assertEqual(
            {
                name: self.campaign.CAMPAIGN_LOCATION_NAME_TO_ID[name]
                for name in legacy
            },
            legacy,
        )
        self.assertEqual(len(self.campaign.NEW_CAMPAIGN_LOCATION_NAME_TO_ID), 81)
        self.assertEqual(
            min(self.campaign.NEW_CAMPAIGN_LOCATION_NAME_TO_ID.values()),
            187256211,
        )
        self.assertEqual(
            max(self.campaign.NEW_CAMPAIGN_LOCATION_NAME_TO_ID.values()),
            187256291,
        )
        self.assertEqual(
            len(set(self.campaign.CAMPAIGN_LOCATION_NAME_TO_ID.values())),
            88,
        )

    def test_validation_rejects_catalog_and_permanent_id_drift(self):
        first = self.campaign.CAMPAIGN_LEVELS[0]
        second = self.campaign.CAMPAIGN_LEVELS[1]
        duplicate_location_names = list(self.campaign.CAMPAIGN_LOCATION_NAMES)
        duplicate_location_names[-1] = duplicate_location_names[0]
        missing_tier_names = tuple(
            name
            for name in self.campaign.CAMPAIGN_LOCATION_NAMES
            if name != "Level 22 - 3 Stars"
        )
        duplicate_ids = dict(self.campaign.NEW_CAMPAIGN_LOCATION_NAME_TO_ID)
        new_names = tuple(duplicate_ids)
        duplicate_ids[new_names[1]] = duplicate_ids[new_names[0]]
        duplicate_id_locations = {
            **self.campaign.LEGACY_CAMPAIGN_LOCATION_IDS,
            **duplicate_ids,
        }
        reordered_names = list(self.campaign.NEW_CAMPAIGN_LOCATION_NAMES)
        reordered_names[0], reordered_names[1] = reordered_names[1], reordered_names[0]
        reordered_ids = {
            name: self.campaign.NEW_CAMPAIGN_LOCATION_START + index
            for index, name in enumerate(reordered_names)
        }
        reordered_locations = {
            **self.campaign.LEGACY_CAMPAIGN_LOCATION_IDS,
            **reordered_ids,
        }
        non_contiguous_ids = dict(self.campaign.NEW_CAMPAIGN_LOCATION_NAME_TO_ID)
        non_contiguous_ids[new_names[-1]] += 1
        non_contiguous_locations = {
            **self.campaign.LEGACY_CAMPAIGN_LOCATION_IDS,
            **non_contiguous_ids,
        }

        fixtures = (
            (
                "duplicate level number",
                {
                    "catalog": self.campaign.CAMPAIGN_LEVELS[:1]
                    + (replace(second, number=first.number),)
                    + self.campaign.CAMPAIGN_LEVELS[2:]
                },
                "level number",
            ),
            (
                "duplicate internal id",
                {
                    "catalog": self.campaign.CAMPAIGN_LEVELS[:1]
                    + (replace(second, internal_id=first.internal_id),)
                    + self.campaign.CAMPAIGN_LEVELS[2:]
                },
                "internal ID",
            ),
            (
                "duplicate location name",
                {"location_names": tuple(duplicate_location_names)},
                "location name",
            ),
            (
                "duplicate location id",
                {
                    "new_location_name_to_id": duplicate_ids,
                    "location_name_to_id": duplicate_id_locations,
                },
                "location ID",
            ),
            (
                "missing location tier",
                {"location_names": missing_tier_names},
                "location tier",
            ),
            (
                "reordered new block",
                {
                    "new_location_names": tuple(reordered_names),
                    "new_location_name_to_id": reordered_ids,
                    "location_name_to_id": reordered_locations,
                },
                "new campaign location order",
            ),
            (
                "non-contiguous new block",
                {
                    "new_location_name_to_id": non_contiguous_ids,
                    "location_name_to_id": non_contiguous_locations,
                },
                "new campaign location IDs",
            ),
        )

        for label, kwargs, diagnostic in fixtures:
            with self.subTest(label=label):
                with self.assertRaisesRegex(ValueError, diagnostic):
                    self.campaign.validate_campaign_catalog(**kwargs)


if __name__ == "__main__":
    unittest.main()
