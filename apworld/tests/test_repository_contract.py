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

    def test_validator_reports_generation_foundation_contract(self):
        result = self.run_validator()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn('"world_version": "0.16"', result.stdout)
        self.assertIn(
            '"implementation_version": "area-routing-plant-pipes-0.15-generation-foundation-0.16"',
            result.stdout,
        )
        self.assertIn('"generation_foundation_version": "generation-foundation-0.16"', result.stdout)
        self.assertIn('"star_item_id": 187256118', result.stdout)
        self.assertIn('"next_item_id": 187256119', result.stdout)
        self.assertIn(
            '"local_ai_allowed_models": [\n    "jacks-assistant",\n    "jacks-assistant-fast"\n  ]',
            result.stdout,
        )
        self.assertIn('"local_ai_decision_schema": 1', result.stdout)
        self.assertIn('"local_ai_evaluation_schema": 1', result.stdout)

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


if __name__ == "__main__":
    unittest.main()
