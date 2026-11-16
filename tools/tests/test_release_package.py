import importlib.util
import io
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch
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
        # The original 0.22.0 APWorld shipped these eleven members.
        files = (
            '__init__.py', 'cassettes.py', 'difficulty.py', 'items.py', 'options.py',
            'placement.py', 'star_requirements.py', 'starting_areas.py',
            'docs/en_Super Crazy Rhythm Castle.md', 'docs/setup_en.md',
        )
        self.payload = {'scrc/' + name: b'# original\r\n' for name in files}
        self.payload['scrc/archipelago.json'] = b'{"world_version":"0.22.0"}'

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

    def test_build_rejects_incomplete_historical_archive(self):
        del self.payload['scrc/cassettes.py']
        self.write()
        with self.assertRaisesRegex(ValueError, r'scrc/cassettes\.py'):
            self.build()
        self.assertFalse(self.output.exists())

    def test_read_zip_limits_actual_combined_member_bytes(self):
        self.write({'first': b'abc', 'second': b'def'})
        with patch.object(pkg, 'MAX_ARCHIVE_BYTES', 6):
            self.assertEqual(pkg.read_zip(self.source), {'first': b'abc', 'second': b'def'})
            # A synthetic stream returned by the ZIP reader exceeds its declared length.
            # Use a four-byte fixture to exercise the independent cumulative budget.
            with patch.object(zipfile.ZipFile, 'open', side_effect=[io.BytesIO(b'abc'), io.BytesIO(b'defg')]):
                with self.assertRaisesRegex(ValueError, 'archive size'):
                    pkg.read_zip(self.source)

    def test_character_items_and_batch_checks_original_inventories(self):
        common = set(self.payload) | {'scrc/music_lab_points.py', 'scrc/campaign_levels.py'}
        for version, expected in [('0.25.0', common), ('0.27.0', common | {'scrc/expanded_checks.py'})]:
            with self.subTest(version=version):
                self.assertEqual(expected, pkg.world_inventory(json.dumps({'world_version': version})))

    def test_build_rejects_module_from_a_later_world_version(self):
        self.write({**self.payload, 'scrc/bunker.py': b'# unexpected'})
        with self.assertRaisesRegex(ValueError, r'scrc/bunker\.py'):
            self.build()

    def test_build_requires_current_module_in_source_directory(self):
        world = self.root / 'scrc'
        shutil.copytree(pkg.ROOT / 'apworld/scrc', world,
                        ignore=shutil.ignore_patterns('__pycache__', 'bunker.py'))
        with self.assertRaisesRegex(ValueError, r'scrc/bunker\.py'):
            pkg.build(world, self.output, 'apworld', 'test-source', 'test-release')
        self.assertFalse(self.output.exists())

    def test_validate_rejects_incomplete_current_archive_with_matching_manifest(self):
        pkg.build(pkg.ROOT / 'apworld/scrc', self.output, 'apworld', 'test-source', 'test-release')
        pkg.validate(self.output)
        with zipfile.ZipFile(self.output) as archive:
            entries = {name: archive.read(name) for name in archive.namelist()}
        del entries['scrc/bunker.py']
        manifest = json.loads(entries['scrc/DISTRIBUTION.json'])
        del manifest['files']['scrc/bunker.py']
        entries['scrc/DISTRIBUTION.json'] = json.dumps(manifest).encode()
        self.write(entries)
        with self.assertRaisesRegex(ValueError, r'scrc/bunker\.py'):
            pkg.validate(self.source)

    def test_build_requires_embedded_world_version(self):
        self.write({**self.payload, 'scrc/archipelago.json': b'{}'})
        with self.assertRaisesRegex(ValueError, 'world_version'):
            self.build()

    def test_unlisted_world_version_must_be_loadable_even_with_a_complete_inventory(self):
        world = self.root / 'scrc'
        shutil.copytree(pkg.ROOT / 'apworld/scrc', world, ignore=shutil.ignore_patterns('__pycache__'))
        metadata_path = world / 'archipelago.json'
        metadata = json.loads(metadata_path.read_bytes())
        for version in ('n/a', '0.31'):
            with self.subTest(version=version):
                metadata['world_version'] = version
                metadata_path.write_text(json.dumps(metadata), encoding='utf-8')
                with self.assertRaisesRegex(ValueError, 'world_version'):
                    pkg.build(world, self.output, 'apworld', 'test-source', 'test-release')
                self.assertFalse(self.output.exists())

        metadata['world_version'] = '0.31.0'
        metadata_path.write_text(json.dumps(metadata), encoding='utf-8')
        pkg.build(world, self.output, 'apworld', 'test-source', 'test-release')
        pkg.validate(self.output)

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
