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
    def test_public_apworld_docs_report_v022_location_totals(self):
        expected_rows = (
            "| Normal | Completion / 1-Star | Bronze | 92 |",
            "| Hard | Add 2-Star | Add Silver | 129 |",
            "| Expert | Add 3-Star | Add Gold | 166 |",
            "| Perfection | Same campaign tiers as Expert | Add Platinum | 202 |",
        )
        for relative in ("apworld/README.md", "apworld/scrc/docs/setup_en.md"):
            text = (REPO_ROOT / relative).read_text(encoding="utf-8")
            with self.subTest(document=relative):
                for row in expected_rows:
                    self.assertIn(row, text)

    def run_validator(self, root=REPO_ROOT):
        environment = os.environ.copy()
        environment["SCRC_REPO_ROOT"] = str(root)
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
                shutil.copytree(source, destination)
        return root

    def test_validator_reports_music_lab_point_slot_contract(self):
        result = self.run_validator()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Client:  v0.68.0", result.stdout)
        self.assertIn("APWorld: v0.23.0", result.stdout)
        self.assertIn('"world_version": "0.23.0"', result.stdout)
        self.assertIn(
            '"implementation_version": "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23"',
            result.stdout,
        )
        self.assertIn('"generation_foundation_version": "generation-foundation-0.16"', result.stdout)
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
        self.assertIn('"music_lab_point_thresholds": [', result.stdout)
        self.assertIn('"next_item_id": 187256156', result.stdout)
        self.assertIn('"next_location_id": 187256211', result.stdout)

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
                "client/Plugin.cs",
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
            "scrc/difficulty.py",
            "scrc/starting_areas.py",
            "scrc/star_requirements.py",
            "scrc/archipelago.json",
        }
        self.assertTrue(required <= names)
        self.assertFalse(any("__pycache__" in name or name.endswith(".pyc") for name in names))
        self.assertEqual(manifest["world_version"], "0.23.0")
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
