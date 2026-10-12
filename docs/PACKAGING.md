# Release packaging

Original project code is MIT licensed; third-party software retains its own terms.
Always distribute LICENSE, THIRD_PARTY_NOTICES.md and the complete licenses folder.
The standard-library Python packager validates the actual finished archive before
returning success. Run with Python 3.10 or later.

```powershell
python tools/package_release.py --kind client --input client/bin/Release/net6.0 --output dist/client.zip --source-revision YOUR_COMMIT --release YOUR_VERSION
python tools/package_release.py --kind apworld --input apworld/scrc --output dist/scrc.apworld --source-revision YOUR_COMMIT --release YOUR_VERSION
python tools/package_release.py --validate dist/client.zip
python -m unittest discover -s tools/tests
```

Use a fresh output directory: existing artifacts are never overwritten. Source
references must describe the actual inputs; label a dirty/local build explicitly.
A caller-supplied commit is not proof of reproducibility. Client dependency hashes
are intentionally pinned. Re-audit versions, licenses and provenance before
changing them. Newly added APWorld files require explicit allowlist review.

The client accepts only the three approved DLLs (the plugin's PDB and deps.json
are ignored when reading a build directory). The APWorld accepts only reviewed
source/docs filenames; Python cache directories are ignored. Extra game files,
unexpected dependencies and unsafe archive paths fail validation. Validation is
an inventory/hash/notice check, not an originality or legal-compliance guarantee.

## Correcting older downloads

The same tool accepts an original ZIP/APWorld as input. Every original payload
byte is preserved; only licensing documents and DISTRIBUTION.json are added.
Record the original archive hash, correct source reference and new artifact hash.
Keep originals locally for comparison. Updating public downloads is a separate
approved publication step; update checksums and describe the packaging-only change.
Do not compile a development candidate into a historical release.

All 8 historical client releases use the same two dependency binaries. Historical
APWorld source references may differ from their release tags; consult the audit.
Runtime behavior, IDs, versions, seed placements and save formats are unchanged
by adding notices. This does not establish acceptance by Archipelago moderators.
