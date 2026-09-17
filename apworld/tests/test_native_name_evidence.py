"""Compare authored catalogs against independently extracted game facts."""
import json
from pathlib import Path
import unittest
from support import load_scrc_module

class NativeNameEvidenceTests(unittest.TestCase):
    def test_campaign_names_numbers_and_ids_match_native_assets(self):
        evidence = json.loads((Path(__file__).parent / "fixtures/native_name_evidence.json").read_text())
        catalog = load_scrc_module("campaign_levels")
        self.assertEqual(
            [(c.number, c.internal_id, c.display_name) for c in catalog.CAMPAIGN_LEVELS],
            [(c["number"], c["internal_id"], c["title"]) for c in evidence["campaign"]],
        )

    def test_all_cassette_award_routes_and_documented_protocol_aliases_match(self):
        evidence = json.loads((Path(__file__).parent / "fixtures/native_name_evidence.json").read_text())
        catalog = load_scrc_module("cassettes")
        self.assertEqual(len(catalog.CASSETTES), len(evidence["songs"]))
        for c, native in zip(catalog.CASSETTES, evidence["songs"]):
            with self.subTest(song=c.native_song):
                self.assertEqual(c.native_song, native["enum"])
                self.assertEqual(c.display_song, native["ap_name"])
                self.assertEqual(sorted((c.native_song, t.level, t.variant) for t in c.triggers),
                                 sorted(tuple(t) for t in native["triggers"]))
