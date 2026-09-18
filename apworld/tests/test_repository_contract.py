import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
import zipfile


REPO_ROOT = Path(__file__).resolve().parents[2]
VALIDATOR = REPO_ROOT / "tools" / "validate-repo.py"


class RepositoryContractTests(unittest.TestCase):
    def test_current_roots_post_level_one_presentation_remains_wired(self):
        source = (REPO_ROOT / "client/Plugin.cs").read_text(encoding="utf-8")
        for call in ("patched += PatchRootsPostLevelOnePresentation();",
                     "nameof(GamePatches.RootsPostLevelOneSequencePrefix)",
                     "RootsPostLevelOnePresentation.BeforeSequence(",
                     "RootsPostLevelOnePresentation.RecordLevelPersisted(level);",
                     "RootsPostLevelOnePresentation.NewSaveCreated();"):
            self.assertIn(call, source)

    def test_progression_callbacks_keep_sources_without_generic_property_dumps(self):
        source = (REPO_ROOT / "client/Plugin.cs").read_text(encoding="utf-8")
        self.assertNotIn("DumpAllSimpleMembers", source)
        self.assertNotIn("Level5Discovery", source)
        for call in ("QuestActionDiscovery.RecordProgressionRequest(req, flag)",
                     "QuestActionDiscovery.RecordProgressionFlagUpdated(evt, flag)",
                     "QuestActionDiscovery.SnapshotKnownFlags()",
                     "SpecialModeDiscovery.ScanCurrentScene()",
                     "GarageCartridgeAccess.RecordVanillaSourceCollected(req, flag)",
                     "WeedKillerRandomization.RecordGeckoSourceCollected(req, flag)",
                     "PlantPipesRandomization.RecordFrogHippoSourceCollected(req, flag)",
                     "RootsBucketRandomization.RecordSourceCollected(req, flag)"):
            self.assertIn(call, source)

    def test_area_access_client_cannot_restore_retired_per_level_gates(self):
        source = (REPO_ROOT / "client/Plugin.cs").read_text(encoding="utf-8")
        self.assertNotIn("RandomizeEarlyProgression", source)
        self.assertNotIn("EarlySequenceBlockerPatches", source)
        self.assertNotIn("NativeLevel4DoorBridge", source)
        self.assertNotIn("NativeLevel6DoorBridge", source)
        for number in range(2, 23):
            self.assertNotIn(f'"Level {number} Access"', source)
            self.assertNotIn(f"GrantLevel{number}Locally", source)
            self.assertNotIn(f"DeveloperResetLevel{number}Access", source)
        for call in ("AreaAccessPrototype.TryApplyItem(itemName)",
                     "CassetteReceiptRandomization.TryApplyItem(itemName)",
                     "NativeProgression.CapturePlayerSaveRequestProcessor(__instance)",
                     "RootsBucketRandomization.ShouldSuppressVanillaGrant(req, flag)",
                     "QuestActionDiscovery.SnapshotKnownFlags()"):
            self.assertIn(call, source)

    def test_retired_automatic_diagnostics_preserve_gameplay_and_acceptance_hooks(self):
        source = (REPO_ROOT / "client/Plugin.cs").read_text(encoding="utf-8")
        keeper = source.split("internal sealed class MusicLabDiagnosticKeeper", 1)[1].split(
            "internal static class MusicLabDiscovery", 1)[0]
        self.assertIn("MusicLabDiscovery.ReconcileCollectedRewardChests()", keeper)
        self.assertIn("MusicLabDiscovery.PollGarageCartridgeSelection()", keeper)
        for obsolete in ("PollCassetteStartState", "ResolveCassetteStartGetterMethods",
                         "PollGarageCurrentSong", "BottomHudInputDiagnostic"):
            self.assertNotIn(obsolete, source)
        for required in ("_getCurrentSongMethod.Invoke(null, null)",
                         "QuestActionDiscovery.SnapshotKnownFlags()",
                         "QuestActionDiscovery.RecordProgressionRequest(",
                         "QuestActionDiscovery.RecordProgressionFlagUpdated("):
            self.assertIn(required, source)
        self.assertFalse((REPO_ROOT / "client/CassetteCatalogDiagnostic.cs").exists())
        self.assertFalse((REPO_ROOT / "client/CassetteCatalogDiagnosticPolicy.cs").exists())

    def assert_public_counts(self, normal, hard, expert, perfection):
        expected = f"Normal {normal} / Hard {hard} / Expert {expert} / Perfection {perfection}"
        for relative in (
            "docs/PROJECT_OVERVIEW.md",
            "docs/PROGRESSION.md",
            "docs/TESTING.md",
            "README.md",
        ):
            text = (REPO_ROOT / relative).read_text(encoding="utf-8")
            with self.subTest(document=relative):
                self.assertIn(expected, text)

    def test_public_docs_report_full_level_mapping_candidate_status(self):
        self.assert_public_counts("164", "222", "280", "316")
        overview = (REPO_ROOT / "docs/PROJECT_OVERVIEW.md").read_text(encoding="utf-8")
        self.assertIn("Client v0.75.0 / APWorld v0.28.0", overview)
        self.assertIn("six bee/devil completions", overview.lower())
        self.assertIn("66 AP Stars are active", overview)

        for relative in ("apworld/README.md", "apworld/scrc/docs/setup_en.md"):
            text = (REPO_ROOT / relative).read_text(encoding="utf-8")
            with self.subTest(document=relative):
                self.assertIn("Client v0.75.0 / APWorld v0.28.0", text)
                self.assertIn("schema 19", text.lower())
                self.assertIn("campaign-mapping schema 1", text.lower())
                self.assertIn("fresh v0.28 seed", text.lower())

        testing = (REPO_ROOT / "docs/TESTING.md").read_text(encoding="utf-8")
        self.assertIn("schema 19 / campaign-mapping schema 1", testing.lower())
        self.assertNotIn("The exact schema-14/point-schema-1 contract is required", testing)

    def run_validator(self, root=REPO_ROOT, *, validate_live=None):
        environment = os.environ.copy()
        environment["SCRC_REPO_ROOT"] = str(root)
        if validate_live is None:
            validate_live = root == REPO_ROOT
        if not validate_live:
            environment["SCRC_VALIDATE_LIVE_WORLD"] = "0"
        return subprocess.run(
            [sys.executable, str(VALIDATOR)],
            cwd=REPO_ROOT,
            env=environment,
            text=True,
            capture_output=True,
            check=False,
        )

    def make_fixture(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)
        for relative in ("apworld/scrc", "client", "docs"):
            source = REPO_ROOT / relative
            destination = root / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            if source.is_dir():
                shutil.copytree(
                    source, destination,
                    ignore=shutil.ignore_patterns("bin", "obj", "__pycache__"),
                )
        support_source = REPO_ROOT / "apworld" / "tests" / "support.py"
        support_destination = root / "apworld" / "tests" / "support.py"
        support_destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(support_source, support_destination)
        return root

    def test_validator_reports_full_level_mapping_slot_contract(self):
        result = self.run_validator()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Client:  v0.75.0", result.stdout)
        self.assertIn("APWorld: v0.28.0", result.stdout)
        self.assertIn('"world_version": "0.28.0"', result.stdout)
        self.assertIn(
            '"implementation_version": "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23-full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28"',
            result.stdout,
        )
        self.assertIn('"generation_foundation_version": "generation-foundation-0.16"', result.stdout)
        self.assertIn('"slot_data_schema": 19', result.stdout)
        self.assertIn('"expanded_check_count": 39', result.stdout)
        self.assertIn('"star_item_id": 187256118', result.stdout)
        self.assertIn('"hip_glasses_item_id": 187256119', result.stdout)
        self.assertIn('"chicken_bucket_item_id": 187256120', result.stdout)
        self.assertIn('"hypno_pan_item_id": 187256121', result.stdout)
        self.assertIn('"violance_item_id": 187256122', result.stdout)
        self.assertIn('"money_cassette_item_id": 187256123', result.stdout)
        self.assertIn('"hip_glasses_location_id": 187256180', result.stdout)
        self.assertIn('"bucket_trade_location_id": 187256181', result.stdout)
        self.assertIn('"money_cassette_location_id": 187256186', result.stdout)
        self.assertIn('"music_lab_point_schema": 1', result.stdout)
        self.assertIn('"music_lab_point_item_ids": [', result.stdout)
        self.assertIn('187256153', result.stdout)
        self.assertIn('187256154', result.stdout)
        self.assertIn('187256155', result.stdout)
        self.assertIn('"music_lab_point_counts": [', result.stdout)
        self.assertIn('"music_lab_point_total_value": 180', result.stdout)
        self.assertIn(
            '"music_lab_point_thresholds": {\n'
            '    "5": "Music Lab - 5 Point Chest",\n'
            '    "10": "Music Lab - 10 Point Chest",\n'
            '    "20": "Music Lab - 20 Point Chest",\n'
            '    "32": "Music Lab - 32 Point Chest",\n'
            '    "46": "Music Lab - 46 Point Chest",\n'
            '    "64": "Music Lab - 64 Point Chest",\n'
            '    "89": "Music Lab - 89 Point Chest",\n'
            '    "111": "Music Lab - 111 Point Chest",\n'
            '    "140": "Music Lab - 140 Point Chest"\n'
            '  }',
            result.stdout,
        )
        self.assertIn('"next_item_id": 187256164', result.stdout)
        self.assertIn('"next_location_id": 187256336', result.stdout)
        self.assertIn('"campaign_location_count": 88', result.stdout)
        self.assertIn('"new_campaign_location_count": 81', result.stdout)
        self.assertIn('"active_location_totals": {', result.stdout)
        self.assertIn('"live_world_structure_checked": true', result.stdout)
        self.assertIn('"Normal": 164', result.stdout)
        self.assertIn('"Perfection": 316', result.stdout)

    def test_validator_rejects_active_star_contract_drift(self):
        for before,after in (('"star_items_active": True','"star_items_active": False'),
                             ('"star_victory_schema": 1','"star_victory_schema": 2'),
                             ('"victory_level_internal_id": "Level_28"','"victory_level_internal_id": "Level_14"')):
            root=self.make_fixture();path=root/'apworld/scrc/__init__.py'
            text=path.read_text();self.assertIn(before,text);path.write_text(text.replace(before,after,1))
            result=self.run_validator(root)
            self.assertNotEqual(result.returncode,0,result.stdout+result.stderr)
            self.assertIn('star',result.stderr.lower())

    def test_validator_rejects_expanded_catalog_and_contract_drift(self):
        mutations = (
            ("client/ExpandedCheckCatalog.cs", "187256298", "187256334"),
            ("client/ExpandedCheckCatalog.cs", "LEVEL_09_COMBO_ABILITY_EARNED", "LEVEL_09_COMPLETED"),
            ("apworld/scrc/expanded_checks.py", "187256298", "187256334"),
            ("apworld/scrc/__init__.py", '"expanded_checks_schema": 1', '"expanded_checks_schema": 2'),
            ("client/Plugin.cs", "ExpandedChecks.Observe(evt, flag);", "// supplemental callback removed"),
            ("client/ExpandedChecksPolicy.cs", "map.Count != entries.Count", "false"),
        )
        for relative, old, new in mutations:
            with self.subTest(path=relative, marker=old):
                root = self.make_fixture()
                path = root / relative
                original = path.read_text(encoding="utf-8")
                self.assertIn(old, original)
                path.write_text(original.replace(old, new, 1), encoding="utf-8")
                result = self.run_validator(root)
                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertIn("expanded", result.stderr.lower())

    def test_validator_rejects_expanded_progression_placement_drift(self):
        root = self.make_fixture()
        path = root / "apworld/scrc/__init__.py"
        original = path.read_text(encoding="utf-8")
        self.assertIn("if not entry.progression_safe:", original)
        path.write_text(original.replace("if not entry.progression_safe:", "if False:"), encoding="utf-8")
        result = self.run_validator(root, validate_live=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("expanded", result.stderr.lower())

    def test_validator_rejects_quest_wire_contract_drift(self):
        for path, before, after in (
            ("apworld/scrc/items.py", '"Meoo": BASE_ID + 162', '"Meoo": BASE_ID + 164'),
            ("apworld/scrc/__init__.py", '"Roots - Star Eater Fed": BASE_ID + 297', '"Roots - Star Eater Fed": BASE_ID + 298'),
            ("apworld/scrc/__init__.py", '"quest_checks_schema": 1', '"quest_checks_schema": 2'),
        ):
            with self.subTest(mutation=before):
                root = self.make_fixture()
                source = root / path
                original = source.read_text(encoding="utf-8")
                self.assertIn(before, original)
                source.write_text(original.replace(before, after), encoding="utf-8")
                result = self.run_validator(root)
                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertIn("quest", result.stderr.lower())

    def test_validator_rejects_quest_filler_only_regression(self):
        root = self.make_fixture()
        source = root / "apworld/scrc/__init__.py"
        original = source.read_text(encoding="utf-8")
        before = 'source.item_rule = lambda item: item.name == "Stardust"'
        self.assertIn(before, original)
        source.write_text(original.replace(before, 'source.item_rule = lambda item: True'), encoding="utf-8")
        result = self.run_validator(root, validate_live=True)
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("filler-only", result.stderr.lower())

    def test_validator_rejects_changed_character_quest_native_flag(self):
        root = self.make_fixture()
        items = root / "apworld/scrc/items.py"
        items.write_text(items.read_text(encoding="utf-8").replace(
            "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM", "WRONG_CHARACTER_UNLOCK_FLAG"), encoding="utf-8")
        result = self.run_validator(root)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("character quest item contract changed", result.stderr)

    def test_validator_rejects_changed_base_id(self):
        for relative in (
            "apworld/scrc/music_lab_points.py",
            "apworld/scrc/items.py",
            "apworld/scrc/__init__.py",
        ):
            with self.subTest(module=relative):
                root = self.make_fixture()
                path = root / relative
                original = path.read_text(encoding="utf-8")
                self.assertIn("BASE_ID = 187256000", original)
                path.write_text(
                    original.replace("BASE_ID = 187256000", "BASE_ID = 187257000", 1),
                    encoding="utf-8",
                )
                result = self.run_validator(root)
                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertIn("BASE_ID changed", result.stdout + result.stderr)
                self.assertIn(relative, result.stderr)

    def test_validator_rejects_changed_music_lab_point_contract(self):
        mutations = {
            "Music Lab Point ID changed": (
                "apworld/scrc/music_lab_points.py",
                'MusicLabPointItem("Music Lab Point", BASE_ID + 153, 1, 10)',
                'MusicLabPointItem("Music Lab Point", BASE_ID + 156, 1, 10)',
            ),
            "Music Lab Point value changed": (
                "apworld/scrc/music_lab_points.py",
                'MusicLabPointItem("Music Lab Point", BASE_ID + 153, 1, 10)',
                'MusicLabPointItem("Music Lab Point", BASE_ID + 153, 2, 10)',
            ),
            "Music Lab Point count changed": (
                "apworld/scrc/music_lab_points.py",
                'MusicLabPointItem("Music Lab Point", BASE_ID + 153, 1, 10)',
                'MusicLabPointItem("Music Lab Point", BASE_ID + 153, 1, 9)',
            ),
            "Music Lab Point total changed": (
                "apworld/scrc/music_lab_points.py",
                "_EXPECTED_MUSIC_LAB_POINT_TOTAL_VALUE = 180",
                "_EXPECTED_MUSIC_LAB_POINT_TOTAL_VALUE = 179",
            ),
            "Music Lab Point thresholds changed": (
                "apworld/scrc/music_lab_points.py",
                '    5: "Music Lab - 5 Point Chest",',
                '    6: "Music Lab - 5 Point Chest",',
            ),
            "Music Lab Point threshold locations changed": (
                "apworld/scrc/music_lab_points.py",
                '    5: "Music Lab - 5 Point Chest",',
                '    5: "Music Lab - 10 Point Chest",',
            ),
            "Music Lab Point exported total instances changed": (
                "apworld/scrc/music_lab_points.py",
                "MUSIC_LAB_POINT_TOTAL_INSTANCES = len(MUSIC_LAB_POINT_POOL)",
                "MUSIC_LAB_POINT_TOTAL_INSTANCES = 19",
            ),
            "Music Lab Point exported total value changed": (
                "apworld/scrc/music_lab_points.py",
                "MUSIC_LAB_POINT_TOTAL_VALUE = sum(\n"
                "    entry.value * entry.count for entry in MUSIC_LAB_POINT_ITEMS\n"
                ")",
                "MUSIC_LAB_POINT_TOTAL_VALUE = 179",
            ),
            "Music Lab Point exported total value expression changed": (
                "apworld/scrc/music_lab_points.py",
                "MUSIC_LAB_POINT_TOTAL_VALUE = sum(\n"
                "    entry.value * entry.count for entry in MUSIC_LAB_POINT_ITEMS\n"
                ")",
                "MUSIC_LAB_POINT_TOTAL_VALUE = sum(\n"
                "    entry.value * entry.count\n"
                "    for entry in MUSIC_LAB_POINT_ITEMS if entry.value > 1\n"
                ")",
            ),
            "Music Lab Point exported max effective changed": (
                "apworld/scrc/music_lab_points.py",
                "MUSIC_LAB_POINT_MAX_EFFECTIVE = sum(\n"
                "    entry.value * entry.count for entry in MUSIC_LAB_POINT_ITEMS\n"
                ")",
                "MUSIC_LAB_POINT_MAX_EFFECTIVE = 179",
            ),
        }
        for label, (relative, old, new) in mutations.items():
            with self.subTest(label=label):
                root = self.make_fixture()
                path = root / relative
                original = path.read_text(encoding="utf-8")
                self.assertIn(old, original)
                path.write_text(original.replace(old, new, 1), encoding="utf-8")

                result = self.run_validator(root)

                self.assertNotEqual(result.returncode, 0)
                self.assertIn(label, result.stdout + result.stderr)
    def test_validator_rejects_changed_roots_bucket_contract(self):
        mutations = {
            "Hip Glasses item ID": (
                "apworld/scrc/__init__.py",
                '"Hip Glasses": BASE_ID + 119',
                '"Hip Glasses": BASE_ID + 121',
            ),
            "Chicken Bucket item ID": (
                "apworld/scrc/__init__.py",
                '"Chicken Bucket": BASE_ID + 120',
                '"Chicken Bucket": BASE_ID + 121',
            ),
            "Level 4 source ID": (
                "apworld/scrc/__init__.py",
                "LOCATION_NAME_TO_ID[ROOTS_LEVEL4_HIP_GLASSES] = BASE_ID + 180",
                "LOCATION_NAME_TO_ID[ROOTS_LEVEL4_HIP_GLASSES] = BASE_ID + 182",
            ),
            "Bucket trade ID": (
                "apworld/scrc/__init__.py",
                "LOCATION_NAME_TO_ID[ROOTS_BUCKET_MINION_TRADE] = BASE_ID + 181",
                "LOCATION_NAME_TO_ID[ROOTS_BUCKET_MINION_TRADE] = BASE_ID + 182",
            ),
            "feature flag": (
                "apworld/scrc/__init__.py",
                '"randomize_hip_glasses_chicken_bucket": True',
                '"randomize_hip_glasses_chicken_bucket": False',
            ),
            "native consumed marker": (
                "client/RootsBucketRandomization.cs",
                'internal const string ChickenConsumedFlag = "LEVEL_09_COMBO_ABILITY_EARNED";',
                'internal const string ChickenConsumedFlag = "LEVEL_09_COMPLETED";',
            ),
        }
        for label, (relative, old, new) in mutations.items():
            with self.subTest(label=label):
                root = self.make_fixture()
                path = root / relative
                original = path.read_text(encoding="utf-8")
                self.assertIn(old, original)
                path.write_text(original.replace(old, new, 1), encoding="utf-8")

                result = self.run_validator(root)

                self.assertNotEqual(result.returncode, 0)
                self.assertIn(label, result.stdout + result.stderr)

    def test_validator_checks_every_world_python_file_for_syntax(self):
        root = self.make_fixture()
        (root / "apworld/scrc/difficulty.py").write_text("this is invalid syntax !!!", encoding="utf-8")
        result = self.run_validator(root)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("difficulty.py", result.stdout + result.stderr)

    def test_validator_rejects_changed_star_id(self):
        root = self.make_fixture()
        items = root / "apworld/scrc/items.py"
        items.write_text(items.read_text(encoding="utf-8").replace("BASE_ID + 118", "BASE_ID + 119"), encoding="utf-8")
        result = self.run_validator(root)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Star ID changed", result.stdout + result.stderr)

    def test_validator_rejects_live_world_structure_regressions(self):
        mutations = {
            "active location total": (
                "apworld/scrc/difficulty.py",
                '0: frozenset(("Completion", "1 Star")),',
                '0: frozenset(("Completion",)),',
                "live active-location count changed for Normal",
            ),
            "duplicate addressed location instance": (
                "apworld/scrc/__init__.py",
                'phone_hub = Region("Phone Hub", self.player, self.multiworld)\n',
                'phone_hub = Region("Phone Hub", self.player, self.multiworld)\n'
                '        phone_hub.locations.append(\n'
                '            SCRCLocation(\n'
                '                self.player,\n'
                '                ROOTS_GECKO_WEED_KILLER,\n'
                '                LOCATION_NAME_TO_ID[ROOTS_GECKO_WEED_KILLER],\n'
                '                phone_hub,\n'
                '            )\n'
                '        )\n',
                "live active-location count changed for Normal",
            ),
            "unaddressed Development Cache": (
                "apworld/scrc/__init__.py",
                'phone_hub = Region("Phone Hub", self.player, self.multiworld)\n',
                'phone_hub = Region("Phone Hub", self.player, self.multiworld)\n'
                '        phone_hub.locations.append(\n'
                '            SCRCLocation(\n'
                '                self.player,\n'
                '                "Development Cache 01",\n'
                '                None,\n'
                '                phone_hub,\n'
                '            )\n'
                '        )\n',
                "instantiated Development Cache",
            ),
        }
        for label, (relative, old, new, expected_error) in mutations.items():
            with self.subTest(label=label):
                root = self.make_fixture()
                path = root / relative
                original = path.read_text(encoding="utf-8")
                self.assertIn(old, original)
                path.write_text(original.replace(old, new, 1), encoding="utf-8")

                result = self.run_validator(root, validate_live=True)

                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertIn(expected_error, result.stdout + result.stderr)

    def test_packaged_world_contains_only_distribution_sources(self):
        with tempfile.TemporaryDirectory() as output_dir:
            result = subprocess.run(
                [
                    "powershell.exe",
                    "-NoProfile",
                    "-ExecutionPolicy",
                    "Bypass",
                    "-File",
                    str(REPO_ROOT / "tools/build-apworld.ps1"),
                    "-OutputDir",
                    output_dir,
                ],
                cwd=REPO_ROOT,
                text=True,
                capture_output=True,
                check=False,
            )
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            with zipfile.ZipFile(Path(output_dir) / "scrc.apworld") as archive:
                names = set(archive.namelist())
                manifest = json.loads(archive.read("scrc/archipelago.json"))

        required = {
            "scrc/__init__.py",
            "scrc/options.py",
            "scrc/items.py",
            "scrc/expanded_checks.py",
            "scrc/difficulty.py",
            "scrc/starting_areas.py",
            "scrc/star_requirements.py",
            "scrc/archipelago.json",
        }
        self.assertTrue(required <= names)
        self.assertFalse(any("__pycache__" in name or name.endswith(".pyc") for name in names))
        self.assertEqual(manifest["world_version"], "0.28.0")
        self.assertEqual(manifest["version"], 7)
        self.assertEqual(manifest["compatible_version"], 7)

    def test_example_yaml_selects_the_validated_roots_start(self):
        example = (REPO_ROOT / "apworld/examples/SCRC-AreaRouting-PlantPipes.yaml").read_text(
            encoding="utf-8"
        )
        self.assertIn(
            "  starting_area: roots\n",
            example,
            "Archipelago treats the scalar 'random' as a directive to roll every Choice value",
        )


if __name__ == "__main__":
    unittest.main()
