# SCRC APWorld

Current APWorld: **0.30.0**, adding Bunker/schema21 progression and optional True Ending. Requires Client 0.77.0 for new seeds; legacy rules are preserved.

The Python package under `scrc/` defines generation, items, locations, options and Universal Tracker reconstruction. Individual cassette seeds use schema 19; pairs/triples use schema 20 and require Client 0.76.0 or newer.

Archipelago 0.6.8 generation and Universal Tracker 0.2.32 were tested. Install the APWorld on the generator/tracker computer, restart those programs, and retain the normal player YAML setup. Updating a local APWorld does not change existing seed placements.

From the repository root, `./tools/build-apworld.ps1` creates the allowlisted distribution with licenses. See [packaging](../docs/PACKAGING.md).

[Install and connect](../docs/INSTALL.md) · [Progression](../docs/PROGRESSION.md) · [Tracker](../docs/UNIVERSAL_TRACKER.md) · [Historical APWorld README](../docs/archive/checkpoints/apworld-README.md)
