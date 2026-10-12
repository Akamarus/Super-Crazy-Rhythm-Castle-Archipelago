import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

SPEC = importlib.util.spec_from_file_location('package_release', Path(__file__).parents[1] / 'package_release.py')
pkg = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(pkg)


class PackagingTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root / 'input.zip'
        self.output = self.root / 'output.zip'
        self.payload = {'scrc/__init__.py': b'# original\r\n', 'scrc/archipelago.json': b'{}'}

    def write(self, payload=None):
        with zipfile.ZipFile(self.source, 'w') as archive:
            for name, data in (payload or self.payload).items():
                archive.writestr(name, data)

    def build(self):
        return pkg.build(self.source, self.output, 'apworld', 'test-source', 'test-release')

    def test_preserves_payload_and_includes_notices(self):
        self.write()
        self.build()
        pkg.validate(self.output)
        with zipfile.ZipFile(self.output) as archive:
            for name, data in self.payload.items():
                self.assertEqual(archive.read(name), data)
            self.assertIn('scrc/LICENSE', archive.namelist())
            self.assertIn('scrc/THIRD_PARTY_NOTICES.md', archive.namelist())

    def test_rejects_game_assembly(self):
        self.write({**self.payload, 'scrc/Assembly-CSharp.dll': b'MZ'})
        with self.assertRaises(ValueError): self.build()

    def test_rejects_unreviewed_source_file(self):
        self.write({**self.payload, 'scrc/dumped_game.py': b'code'})
        with self.assertRaises(ValueError): self.build()

    def test_rejects_traversal(self):
        self.write({**self.payload, '../secret.txt': b'no'})
        with self.assertRaises(ValueError): self.build()

    def test_rejects_duplicate_names(self):
        self.write()
        with zipfile.ZipFile(self.source, 'a') as archive: archive.writestr('scrc/__init__.py', b'other')
        with self.assertRaises(ValueError): self.build()

    def test_rejects_unknown_client_dependency_hash(self):
        self.write({name: b'MZfake' for name in pkg.CLIENT_FILES})
        with self.assertRaises(ValueError):
            pkg.build(self.source, self.output, 'client', 'test-source', 'test-release')

    def test_rejects_missing_client_dependency(self):
        self.write({'RhythmCastleAP.dll': b'MZfake'})
        with self.assertRaises(ValueError):
            pkg.build(self.source, self.output, 'client', 'test-source', 'test-release')

    def test_rejects_missing_notice(self):
        self.write(); self.build()
        with zipfile.ZipFile(self.output) as archive:
            entries = {n: archive.read(n) for n in archive.namelist() if not n.endswith('LICENSE')}
        with zipfile.ZipFile(self.output, 'w') as archive:
            for n, data in entries.items(): archive.writestr(n, data)
        with self.assertRaises(ValueError): pkg.validate(self.output)

    def test_rejects_tampered_payload(self):
        self.write(); self.build()
        with zipfile.ZipFile(self.output) as archive:
            entries = {n: archive.read(n) for n in archive.namelist()}
        entries['scrc/__init__.py'] = b'changed'
        with zipfile.ZipFile(self.output, 'w') as archive:
            for n, data in entries.items(): archive.writestr(n, data)
        with self.assertRaises(ValueError): pkg.validate(self.output)

    def test_refuses_overwriting_input(self):
        self.write()
        with self.assertRaises(ValueError):
            pkg.build(self.source, self.source, 'apworld', 'test-source', 'test-release')


if __name__ == '__main__': unittest.main()
