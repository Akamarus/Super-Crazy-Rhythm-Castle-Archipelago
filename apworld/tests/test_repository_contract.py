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
        for relative in ("apworld/scrc", "client", "docs"):
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
        self.assertIn('"implementation_version": "generation-foundation-0.16"', result.stdout)
        self.assertIn('"star_item_id": 187256118', result.stdout)
        self.assertIn('"next_item_id": 187256119', result.stdout)

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
