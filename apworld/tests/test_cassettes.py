from dataclasses import replace
import unittest

from apworld.tests.support import load_scrc_module, load_scrc_world


class CassetteCatalogTests(unittest.TestCase):
    def setUp(self):
        self.catalog = load_scrc_module("cassettes")
        self.world, self.cleanup = load_scrc_world()
        self.addCleanup(self.cleanup)

    def test_catalog_has_every_registered_medal_song_once(self):
        self.assertEqual(len(self.catalog.CASSETTES), 30)
        self.assertEqual(len({entry.display_song for entry in self.catalog.CASSETTES}), 30)
        self.assertEqual(len({entry.native_song for entry in self.catalog.CASSETTES}), 30)
        self.assertEqual(
            {entry.display_song for entry in self.catalog.CASSETTES},
            set(self.world.CASSETTE_SONGS),
        )

    def test_money_preserves_its_permanent_item_and_source_ids(self):
        money = self.catalog.CASSETTE_BY_ITEM["Money Cassette"]
        self.assertEqual(money.item_id, self.catalog.BASE_ID + 123)
        self.assertEqual(money.source_id, self.catalog.BASE_ID + 186)
        self.assertEqual(money.native_song, "I_GOT_MONEY")
        self.assertEqual(
            self.catalog.CASSETTE_BY_ITEM["Money Dub Cassette"].native_song,
            "MONEY_DUB",
        )

    def test_non_money_item_and_new_source_ids_are_consecutive(self):
        non_money_ids = [entry.item_id for entry in self.catalog.CASSETTES if entry.item_name != "Money Cassette"]
        self.assertEqual(non_money_ids, list(range(self.catalog.BASE_ID + 124, self.catalog.BASE_ID + 153)))
        self.assertEqual(self.catalog.NEW_CASSETTE_SOURCE_IDS, {
            entry.source_name: entry.source_id
            for entry in self.catalog.CASSETTES
            if not entry.reused_location
        })
        self.assertEqual(
            list(self.catalog.NEW_CASSETTE_SOURCE_IDS.values()),
            list(range(self.catalog.BASE_ID + 187, self.catalog.BASE_ID + 211)),
        )

    def test_reused_sources_have_no_new_id_and_existing_registration(self):
        reused = [entry for entry in self.catalog.CASSETTES if entry.reused_location]
        self.assertEqual(len(reused), 6)
        for entry in reused:
            self.assertIn(entry.source_name, self.world.LOCATION_NAME_TO_ID)
        self.assertEqual(
            self.catalog.CASSETTE_BY_ITEM["Money Cassette"].source_id,
            self.catalog.BASE_ID + 186,
        )
        for entry in reused:
            if entry.item_name != "Money Cassette":
                self.assertIsNone(entry.source_id)

    def test_aliases_retain_distinct_verified_native_triggers(self):
        gold = self.catalog.CASSETTE_BY_ITEM["Gold Cassette"]
        self.assertEqual(len(gold.triggers), 3)
        self.assertEqual(
            {(trigger.level, trigger.variant) for trigger in gold.triggers},
            {
                ("Level_05", "LevelVariant_Default"),
                ("Level_11", "LevelVariant_Default"),
                ("Level_11", "LevelVariant_DevilMode"),
            },
        )

    def test_alias_metadata_retains_the_evidence_route_groups(self):
        self.assertEqual(
            self.catalog.CASSETTE_BY_ITEM["Money Cassette"].region,
            "Roots OR Cell Tower",
        )
        self.assertEqual(
            self.catalog.CASSETTE_BY_ITEM["Lets Go Cassette"].region,
            "Meat Dimension OR Cell Tower",
        )

    def test_validation_rejects_invalid_catalog_fixtures_with_diagnostics(self):
        fixtures = (
            ("duplicate display song", 1, replace(self.catalog.CASSETTES[1], display_song=self.catalog.CASSETTES[0].display_song), self.catalog.CASSETTES[0].display_song),
            ("duplicate native song", 1, replace(self.catalog.CASSETTES[1], native_song=self.catalog.CASSETTES[0].native_song), self.catalog.CASSETTES[0].native_song),
            ("duplicate item id", 1, replace(self.catalog.CASSETTES[1], item_id=self.catalog.CASSETTES[0].item_id), str(self.catalog.CASSETTES[0].item_id)),
            ("missing medal song", 0, replace(self.catalog.CASSETTES[0], display_song="Not A Medal Song"), "The Little Things"),
            ("reused unknown location", 4, replace(self.catalog.CASSETTES[4], source_name="Unknown Point Chest"), "Unknown Point Chest"),
            ("new source without id", 0, replace(self.catalog.CASSETTES[0], source_id=None), self.catalog.CASSETTES[0].source_name),
            ("unsupported region", 0, replace(self.catalog.CASSETTES[0], region="Moon"), "Moon"),
        )
        for label, index, replacement, diagnostic in fixtures:
            with self.subTest(label=label):
                fixture = self.catalog.CASSETTES[:index] + (replacement,) + self.catalog.CASSETTES[index + 1:]
                with self.assertRaisesRegex(ValueError, diagnostic):
                    self.catalog.validate_cassette_catalog(
                        self.world.CASSETTE_SONGS,
                        self.world.LOCATION_NAME_TO_ID,
                        catalog=fixture,
                    )

    def test_validation_rejects_duplicate_new_source_ids_when_registry_matches(self):
        original = self.catalog.CASSETTES[0]
        replacement = replace(
            self.catalog.CASSETTES[1],
            source_id=original.source_id,
        )
        fixture = self.catalog.CASSETTES[:1] + (replacement,) + self.catalog.CASSETTES[2:]
        registry = dict(self.world.LOCATION_NAME_TO_ID)
        registry[replacement.source_name] = replacement.source_id

        with self.assertRaisesRegex(
            ValueError,
            rf"{replacement.source_name}.*{replacement.source_id}",
        ):
            self.catalog.validate_cassette_catalog(
                self.world.CASSETTE_SONGS,
                registry,
                catalog=fixture,
            )

    def test_validation_rejects_new_source_id_owned_by_another_location(self):
        existing_name = "Level 1 - Completion"
        existing_id = self.world.LOCATION_NAME_TO_ID[existing_name]
        replacement = replace(self.catalog.CASSETTES[1], source_id=existing_id)
        fixture = self.catalog.CASSETTES[:1] + (replacement,) + self.catalog.CASSETTES[2:]
        registry = dict(self.world.LOCATION_NAME_TO_ID)
        registry[replacement.source_name] = existing_id

        with self.assertRaisesRegex(
            ValueError,
            rf"{replacement.source_name}.*{existing_id}",
        ):
            self.catalog.validate_cassette_catalog(
                self.world.CASSETTE_SONGS,
                registry,
                catalog=fixture,
            )
