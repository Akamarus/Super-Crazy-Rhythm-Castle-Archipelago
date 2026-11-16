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
are ignored when reading a build directory). Building or validating an APWorld
requires the reviewed file inventory for the `world_version` in its own
`archipelago.json`. APWorld 0.30.0 includes `scrc/bunker.py`. Older releases
use their own inventories, even when a newer client release reuses the same
APWorld. A new version starts with the full current inventory. Review the
inventory when adding or retiring files. The 0.25.0 character-item candidates
share the 13-file 0.24.0 inventory. The 0.27.0 batch-check candidate adds
`scrc/expanded_checks.py` for 14 files. The next additions first appear in 0.28.0.
Python cache directories are ignored when reading a source directory. Extra game files,
unexpected dependencies and unsafe archive paths fail validation. Validation is
an inventory/hash/notice check, not an originality or legal-compliance guarantee.

Archive validation limits the combined uncompressed contents to 50,000,000 bytes,
checking both declared sizes and bytes read. Client builds use a NuGet lockfile.
The GitLab restore job checks the locked dependency graph against NuGet advisories.
MultiClient.Net embeds its own Newtonsoft.Json DLL as a package asset, so a NuGet
dependency scan cannot identify it as a separate package.

The reviewed net6.0 MultiClient.Net 6.7.1 DLL references an unsigned Newtonsoft
assembly with assembly version 11.0.0.0. Its bundled parser defaults to a maximum
depth of 64 despite its historical 11.0.1-beta2 product label. The NetworkReceive
test exercises the actual SDK packet handler at depths 64 and 65. Run it offline
with `dotnet run --project client/tests/NetworkReceive -c Release -- --offline`.
The development client checks both loaded module IDs before connecting. A
dependency upgrade requires reviewing the new binaries and keeping the historical
repackaging hashes and notices tied to their original releases.

## Correcting older downloads

To validate an existing release archive, run `tools/package_release.py --validate`
from the checkout that supplied its notices. The validator compares the archive's
notice text with that checkout. For example, v0.30.1 reuses the v0.30.0 APWorld,
so use the v0.30.0 checkout to verify that asset.

The same tool accepts an original ZIP/APWorld as input. Every original payload
byte is preserved; only licensing documents and DISTRIBUTION.json are added.
Record the original archive hash, correct source reference and new artifact hash.
Keep originals locally for comparison. Updating public downloads is a separate
approved publication step; update checksums and describe the packaging-only change.
Do not compile a development candidate into a historical release.

The eight client releases in the October 2026 packaging audit used the same
two dependency binaries. The audit records the source reference and before/after
hashes for each corrected archive. [Historical packaging audit](releases/packaging-corrections-2026-10-11.json)
Runtime behavior, IDs, versions, seed placements and save formats are unchanged
by adding notices. This does not establish acceptance by Archipelago moderators.
