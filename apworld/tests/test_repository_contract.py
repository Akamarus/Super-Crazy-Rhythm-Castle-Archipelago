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
        for relative in ("apworld/scrc", "client", "docs", "tools/local-ai"):
            source = REPO_ROOT / relative
            destination = root / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            if source.is_dir():
                shutil.copytree(source, destination)
        return root

    def test_validator_reports_roots_bucket_contract(self):
        result = self.run_validator()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Client:  v0.68.0", result.stdout)
        self.assertIn("APWorld: v0.22.0", result.stdout)
        self.assertIn('"world_version": "0.22.0"', result.stdout)
        self.assertIn(
            '"implementation_version": "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22"',
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
        self.assertIn('"next_item_id": 187256153', result.stdout)
        self.assertIn('"next_location_id": 187256211', result.stdout)
        self.assertIn(
            '"local_ai_allowed_models": [\n    "jacks-assistant",\n    "jacks-assistant-fast"\n  ]',
            result.stdout,
        )
        self.assertIn('"local_ai_decision_schema": 1', result.stdout)
        self.assertIn('"local_ai_evaluation_schema": 1', result.stdout)

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

    def test_validator_rejects_removed_local_ai_decision(self):
        root = self.make_fixture()
        ledger_path = root / "tools/local-ai/evaluation/decisions.json"
        ledger = json.loads(ledger_path.read_text(encoding="utf-8"))
        ledger["decisions"] = [
            decision for decision in ledger["decisions"] if decision["id"] != "native-flag-confidence"
        ]
        ledger_path.write_text(json.dumps(ledger), encoding="utf-8")

        result = self.run_validator(root)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/local-ai/evaluation/decisions.json", result.stdout + result.stderr)

    def test_validator_rejects_case_expected_value_outside_allowed_values(self):
        root = self.make_fixture()
        cases_path = root / "tools/local-ai/evaluation/cases.json"
        cases = json.loads(cases_path.read_text(encoding="utf-8"))
        cases["cases"][0]["questions"][0]["expected"] = "unsupported"
        cases_path.write_text(json.dumps(cases), encoding="utf-8")

        result = self.run_validator(root)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/local-ai/evaluation/cases.json", result.stdout + result.stderr)

    def test_validator_requires_public_local_ai_evaluation_command(self):
        root = self.make_fixture()
        script_path = root / "tools/local-ai/Public/Invoke-LocalAiModelEvaluation.ps1"
        script_path.unlink()

        result = self.run_validator(root)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn(
            "tools/local-ai/Public/Invoke-LocalAiModelEvaluation.ps1",
            result.stdout + result.stderr,
        )

    def test_validator_requires_evaluation_command_in_manifest_functions_to_export(self):
        root = self.make_fixture()
        manifest_path = root / "tools/local-ai/LocalAiBridge.psd1"
        manifest_text = manifest_path.read_text(encoding="utf-8")
        manifest_text = manifest_text.replace(
            "    Description = 'Controlled local AI development bridge for SCRC Archipelago.'",
            "    Description = \"FunctionsToExport = @('Invoke-LocalAiModelEvaluation')\"",
        ).replace(
            "        'Invoke-LocalAiModelEvaluation'\n",
            "",
        ).replace(
            "    FunctionsToExport = @(",
            "    # FunctionsToExport = @('Invoke-LocalAiModelEvaluation')\n"
            "    PrivateData = @{\n"
            "        ExportNote = @\"\n"
            "FunctionsToExport = @('Invoke-LocalAiModelEvaluation')\n"
            "\"@\n"
            "        FunctionsToExport = @(\n"
            "            'Invoke-LocalAiModelEvaluation'\n"
            "        )\n"
            "    }\n"
            "    FunctionsToExport = @(",
        )
        manifest_path.write_text(manifest_text, encoding="utf-8")

        result = self.run_validator(root)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/local-ai/LocalAiBridge.psd1", result.stdout + result.stderr)

    def test_validator_rejects_noncanonical_manifest_with_lexical_and_nested_decoys(self):
        root = self.make_fixture()
        manifest_path = root / "tools/local-ai/LocalAiBridge.psd1"
        manifest_text = manifest_path.read_text(encoding="utf-8")
        manifest_text = manifest_text.replace(
            "    Description = 'Controlled local AI development bridge for SCRC Archipelago.'",
            "    Description = \"FunctionsToExport = @('decoy')\"",
        ).replace(
            "    FunctionsToExport = @(",
            "    # FunctionsToExport = @('decoy')\n"
            "    PrivateData = @{\n"
            "        ExportNote = @\"\n"
            "FunctionsToExport = @('decoy')\n"
            "\"@\n"
            "        FunctionsToExport = @('decoy')\n"
            "    }\n"
            "    FunctionsToExport = @(",
        )
        manifest_path.write_text(manifest_text, encoding="utf-8")

        result = self.run_validator(root)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/local-ai/LocalAiBridge.psd1", result.stdout + result.stderr)

    def test_validator_rejects_sibling_root_manifest_export_decoy(self):
        root = self.make_fixture()
        manifest_path = root / "tools/local-ai/LocalAiBridge.psd1"
        manifest_text = manifest_path.read_text(encoding="utf-8")
        export_start = manifest_text.index("    FunctionsToExport = @(")
        next_field = manifest_text.index("    CmdletsToExport = @()", export_start)
        manifest_without_real_export = manifest_text[:export_start] + manifest_text[next_field:]
        sibling_decoy = """@{
    FunctionsToExport = @(
        'Invoke-LocalAiModelEvaluation'
    )
}
"""
        manifest_path.write_text(sibling_decoy + manifest_without_real_export, encoding="utf-8")

        result = self.run_validator(root)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/local-ai/LocalAiBridge.psd1", result.stdout + result.stderr)

    def test_validator_rejects_non_nesting_block_comment_masking_leading_expression(self):
        root = self.make_fixture()
        manifest_path = root / "tools/local-ai/LocalAiBridge.psd1"
        manifest_text = manifest_path.read_text(encoding="utf-8")
        manifest_path.write_text(
            "<# outer comment\n"
            "<# nested-looking marker #>\n"
            "$leading = 1\n"
            "#>\n"
            + manifest_text,
            encoding="utf-8",
        )

        result = self.run_validator(root)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/local-ai/LocalAiBridge.psd1", result.stdout + result.stderr)

    def test_validator_rejects_block_comment_newline_export_decoy(self):
        root = self.make_fixture()
        manifest_path = root / "tools/local-ai/LocalAiBridge.psd1"
        manifest_text = manifest_path.read_text(encoding="utf-8")
        export_start = manifest_text.index("    FunctionsToExport = @(")
        next_field = manifest_text.index("    CmdletsToExport = @()", export_start)
        comment_newline_decoy = """    Other = 1 <#
comment-internal newlines are not root-entry separators
#> FunctionsToExport = @(
        'Invoke-LocalAiModelEvaluation'
    )
"""
        manifest_path.write_text(
            manifest_text[:export_start]
            + comment_newline_decoy
            + manifest_text[next_field:],
            encoding="utf-8",
        )

        result = self.run_validator(root)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/local-ai/LocalAiBridge.psd1", result.stdout + result.stderr)

    def test_validator_rejects_non_direct_or_malformed_comma_export_arrays(self):
        mutations = {
            "unary comma nests the apparent export": lambda text: text.replace(
                "        'Invoke-LocalAiModelEvaluation'\n",
                "        , 'Invoke-LocalAiModelEvaluation'\n",
            ),
            "trailing comma is malformed": lambda text: text.replace(
                "        'Stop-LocalAiDevelopmentSession'\n    )",
                "        'Stop-LocalAiDevelopmentSession',\n    )",
            ),
        }
        for label, mutate in mutations.items():
            with self.subTest(label=label):
                root = self.make_fixture()
                manifest_path = root / "tools/local-ai/LocalAiBridge.psd1"
                manifest_text = manifest_path.read_text(encoding="utf-8")
                manifest_path.write_text(mutate(manifest_text), encoding="utf-8")

                result = self.run_validator(root)

                self.assertNotEqual(result.returncode, 0)
                self.assertIn(
                    "tools/local-ai/LocalAiBridge.psd1",
                    result.stdout + result.stderr,
                )

    def test_validator_rejects_array_nested_variable_assignment_export_decoy(self):
        root = self.make_fixture()
        manifest_path = root / "tools/local-ai/LocalAiBridge.psd1"
        manifest_text = manifest_path.read_text(encoding="utf-8")
        export_start = manifest_text.index("    FunctionsToExport = @(")
        next_field = manifest_text.index("    CmdletsToExport = @()", export_start)
        nested_assignment_decoy = """    Other = @(
        $FunctionsToExport = @(
            'Invoke-LocalAiModelEvaluation'
        )
    )
"""
        manifest_path.write_text(
            manifest_text[:export_start]
            + nested_assignment_decoy
            + manifest_text[next_field:],
            encoding="utf-8",
        )

        result = self.run_validator(root)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/local-ai/LocalAiBridge.psd1", result.stdout + result.stderr)

    def test_validator_rejects_malformed_here_string_masking_unmatched_delimiter(self):
        root = self.make_fixture()
        manifest_path = root / "tools/local-ai/LocalAiBridge.psd1"
        manifest_text = manifest_path.read_text(encoding="utf-8")
        manifest_text = manifest_text.replace(
            "    Description = 'Controlled local AI development bridge for SCRC Archipelago.'",
            '    Description = @"not-a-valid-here-string-header\n]\n"@',
        )
        manifest_path.write_text(manifest_text, encoding="utf-8")

        result = self.run_validator(root)

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/local-ai/LocalAiBridge.psd1", result.stdout + result.stderr)

    def test_validator_rejects_ambiguous_or_malformed_manifest_root_structure(self):
        mutations = {
            "leading executable expression": lambda text: "$leading = 1\n" + text,
            "trailing data expression": lambda text: text + "\n@('trailing')\n",
            "duplicate top-level export field": lambda text: text.replace(
                "    CmdletsToExport = @()",
                "    FunctionsToExport = @('Invoke-LocalAiModelEvaluation')\n"
                "    CmdletsToExport = @()",
            ),
            "unterminated root hashtable": lambda text: text.rsplit("}", 1)[0],
        }
        for label, mutate in mutations.items():
            with self.subTest(label=label):
                root = self.make_fixture()
                manifest_path = root / "tools/local-ai/LocalAiBridge.psd1"
                manifest_text = manifest_path.read_text(encoding="utf-8")
                manifest_path.write_text(mutate(manifest_text), encoding="utf-8")

                result = self.run_validator(root)

                self.assertNotEqual(result.returncode, 0)
                self.assertIn("tools/local-ai/LocalAiBridge.psd1", result.stdout + result.stderr)

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
        self.assertEqual(manifest["world_version"], "0.22.0")
        self.assertEqual(manifest["version"], 8)
        self.assertEqual(manifest["compatible_version"], 8)

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
