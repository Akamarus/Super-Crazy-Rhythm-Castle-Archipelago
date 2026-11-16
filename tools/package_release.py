"""Build notice-complete releases without executing or altering their payloads.

Python standard library only. Approved dependency hashes are intentionally pinned;
upgrades require a fresh license/provenance review before changing this file.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import re
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
CLIENT_FILES = {'RhythmCastleAP.dll', 'Archipelago.MultiClient.Net.dll', 'Newtonsoft.Json.dll'}
DEPENDENCIES = {
    'Archipelago.MultiClient.Net.dll': {
        'sha256': '2a561974213be32fb62f778b4cce256ae7661dca08595f7896b3e35df684c23e',
        'version': '6.7.1', 'license': 'MIT',
        'source': 'https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net/tree/0c57591db30f2497b0b4fef87164aa2bbe2e51b2',
    },
    'Newtonsoft.Json.dll': {
        'sha256': '0cc6b1d3f4e8c938edfe1f02f2167d1cbf596a4df6ac0dbd82fe89ed67e45eb0',
        'version': '11.0.1-beta2 (file version 11.0.1), bundled in MultiClient 6.7.1',
        'license': 'MIT',
        'source': 'https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net/tree/0c57591db30f2497b0b4fef87164aa2bbe2e51b2/Newtonsoft.Json',
    },
}
# Released APWorld inventories grew cumulatively. Use the version in archipelago.json
# because a client-only release can ship an older, unchanged world.
WORLD_LAYOUT_ADDITIONS = (
    (('0.15',), ('__init__.py', 'archipelago.json', 'docs/en_Super Crazy Rhythm Castle.md',
                 'docs/setup_en.md')),
    (('0.16', '0.17', '0.18'), ('difficulty.py', 'items.py', 'options.py',
                                 'star_requirements.py', 'starting_areas.py')),
    (('0.19', '0.19.0', '0.20.0', '0.21.0'), ('placement.py',)),
    (('0.22.0',), ('cassettes.py',)),
    (('0.23.0',), ('music_lab_points.py',)),
    (('0.24.0', '0.25.0', '0.26.0'), ('campaign_levels.py',)),
    (('0.27.0',), ('expanded_checks.py',)),
    (('0.28.0',), ('native_logic.py', 'docs/ap-stars-logic.md')),
    (('0.28.1', '0.28.2'), ('universal_tracker.py',)),
    (('0.29.0',), ('cassette_bundles.py', 'optional_routes.py', 'pool_capacity.py')),
    (('0.30.0', '0.30.1'), ('bunker.py',)),
)
NOTICE_FILES = ('LICENSE', 'THIRD_PARTY_NOTICES.md', 'licenses/Archipelago.MultiClient.Net-MIT.txt',
                'licenses/Newtonsoft.Json-MIT.txt', 'licenses/BepInEx-LGPL-2.1.txt',
                'licenses/Il2CppInterop-LGPL-3.0-and-GPL-3.0.txt')
MANIFEST = 'DISTRIBUTION.json'
MAX_ARCHIVE_BYTES = 50_000_000


def digest(data):
    return hashlib.sha256(data).hexdigest()


def read_zip(path):
    with zipfile.ZipFile(path) as archive:
        entries = archive.infolist()
        names = [e.filename for e in entries]
        if len(names) != len(set(n.casefold() for n in names)):
            raise ValueError('Duplicate or case-colliding archive paths')
        if sum(e.file_size for e in entries) > MAX_ARCHIVE_BYTES:
            raise ValueError('Unexpected archive size')
        result = {}
        remaining = MAX_ARCHIVE_BYTES
        for entry in entries:
            name = entry.filename
            if '\\' in name or ':' in name or PurePosixPath(name).is_absolute() or '..' in PurePosixPath(name).parts:
                raise ValueError('Unsafe archive path: ' + name)
            if entry.is_dir():
                continue
            with archive.open(entry) as member, io.BytesIO() as data:
                while block := member.read(min(65536, remaining + 1)):
                    if len(block) > remaining:
                        raise ValueError('Unexpected archive size')
                    remaining -= len(block)
                    data.write(block)
                result[name] = data.getvalue()
        return result


def world_inventory(metadata):
    metadata = json.loads(metadata)
    version = metadata.get('world_version') if isinstance(metadata, dict) else None
    required = set()
    for versions, additions in WORLD_LAYOUT_ADDITIONS:
        required.update('scrc/' + name for name in additions)
        if version in versions:
            return required
    # Archipelago expects major.minor.patch. Only the original historical
    # archives above used shorter identifiers that we still repackage.
    if not isinstance(version, str) or not re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+', version):
        raise ValueError('APWorld world_version must be a reviewed historical version or major.minor.patch')
    return required


def check_payload(payload, kind):
    if kind == 'client':
        if set(payload) != CLIENT_FILES:
            raise ValueError('Client must contain exactly the three approved DLLs')
        for name, expected in DEPENDENCIES.items():
            if digest(payload[name]) != expected['sha256']:
                raise ValueError('Unreviewed dependency binary: ' + name)
        if not payload['RhythmCastleAP.dll'].startswith(b'MZ'):
            raise ValueError('Plugin is not a PE binary')
    elif kind == 'apworld':
        if not {'scrc/__init__.py', 'scrc/archipelago.json'} <= payload.keys():
            raise ValueError('Missing APWorld entry points')
        required = world_inventory(payload['scrc/archipelago.json'])
        if missing := required - payload.keys():
            raise ValueError('Missing APWorld contents: ' + ', '.join(sorted(missing)))
        if extra := payload.keys() - required:
            raise ValueError('Unapproved APWorld contents: ' + ', '.join(sorted(extra)))
    else:
        raise ValueError('Unknown package kind')


def read_payload(source, kind):
    if source.is_file(): return read_zip(source)
    result = {}
    for path in sorted(source.rglob('*')):
        if path.is_symlink(): raise ValueError('Symlink inputs are not allowed')
        if not path.is_file(): continue
        relative = path.relative_to(source).as_posix()
        if '__pycache__' in path.relative_to(source).parts: continue
        # Always take licensing documents from the reviewed repository copies.
        if relative in NOTICE_FILES: continue
        if kind == 'client' and relative in {'RhythmCastleAP.pdb', 'RhythmCastleAP.deps.json'}: continue
        result[('scrc/' if kind == 'apworld' else '') + relative] = path.read_bytes()
    return result


def validate(path):
    entries = read_zip(path)
    manifests = [n for n in entries if n in {MANIFEST, 'scrc/' + MANIFEST}]
    if len(manifests) != 1: raise ValueError('Missing or ambiguous distribution manifest')
    manifest = json.loads(entries.pop(manifests[0]))
    kind = manifest.get('kind')
    prefix = 'scrc/' if kind == 'apworld' else ''
    if manifests[0] != prefix + MANIFEST or manifest.get('schema') != 1:
        raise ValueError('Invalid distribution manifest')
    expected = manifest.get('files', {})
    if set(expected) != set(entries): raise ValueError('Manifest/archive file mismatch')
    for name, data in entries.items():
        if expected[name] != digest(data): raise ValueError('Hash mismatch: ' + name)
    for name in NOTICE_FILES:
        actual = entries.pop(prefix + name, None)
        expected_notice = (ROOT / name).read_bytes()
        # Git checkouts may normalize line endings; the manifest still verifies exact archive bytes.
        if actual is None or actual.replace(b'\r\n', b'\n') != expected_notice.replace(b'\r\n', b'\n'):
            raise ValueError('Missing or altered required notice: ' + name)
    check_payload(entries, kind)
    if kind == 'client' and manifest.get('dependencies') != DEPENDENCIES:
        raise ValueError('Incorrect dependency provenance')
    if not manifest.get('source_revision') or not manifest.get('release'):
        raise ValueError('Missing source/release provenance')
    return manifest


def build(source, output, kind, source_revision, release):
    source, output = Path(source), Path(output)
    if source.resolve() == output.resolve() or (source.is_dir() and source.resolve() in output.resolve().parents):
        raise ValueError('Output must be separate from input')
    if output.exists(): raise ValueError('Refusing to overwrite an existing artifact')
    payload = read_payload(source, kind)
    check_payload(payload, kind)
    prefix = 'scrc/' if kind == 'apworld' else ''
    for name in NOTICE_FILES: payload[prefix + name] = (ROOT / name).read_bytes()
    manifest = {
        'schema': 1, 'kind': kind, 'release': release,
        'source_revision': source_revision,
        'source_revision_note': 'Caller-supplied source reference; not a reproducible-build attestation.',
        'original_archive_sha256': digest(source.read_bytes()) if source.is_file() else None,
        'dependencies': DEPENDENCIES if kind == 'client' else {},
        'files': {name: digest(data) for name, data in sorted(payload.items())},
    }
    payload[prefix + MANIFEST] = (json.dumps(manifest, indent=2) + '\n').encode()
    output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(dir=output.parent, suffix='.zip', delete=False) as temp:
        temporary = Path(temp.name)
    try:
        with zipfile.ZipFile(temporary, 'w', zipfile.ZIP_DEFLATED) as archive:
            for name, data in sorted(payload.items()):
                info = zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0))
                info.compress_type = zipfile.ZIP_DEFLATED
                archive.writestr(info, data)
        validate(temporary)
        # Do not overwrite an artifact created concurrently.
        with output.open('xb') as destination: destination.write(temporary.read_bytes())
    finally:
        temporary.unlink(missing_ok=True)
    return manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--validate', type=Path)
    parser.add_argument('--input', type=Path)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--kind', choices=('client', 'apworld'))
    parser.add_argument('--source-revision')
    parser.add_argument('--release')
    args = parser.parse_args()
    if args.validate:
        validate(args.validate)
        print('Validated:', args.validate)
    else:
        if not all((args.input, args.output, args.kind, args.source_revision, args.release)):
            parser.error('Build requires input, output, kind, source-revision and release')
        build(args.input, args.output, args.kind, args.source_revision, args.release)
        print('Prepared:', args.output)


if __name__ == '__main__': main()
