# SCRC APWorld

**APWorld 0.30.0** supports Bunker progression and the optional True Ending. New schema 21 seeds require Client 0.77.0 or later.

The Python package under `scrc/` defines generation, items, locations, options and Universal Tracker reconstruction. With Bunker progression enabled, all cassette modes use schema 21. With Bunker progression disabled, individual cassettes use schema 19 and pairs or triples use schema 20.

Archipelago 0.6.8 generation and Universal Tracker 0.2.32 were tested. Install the APWorld on the generator/tracker computer, restart those programs, and retain the normal player YAML setup. Updating a local APWorld does not change existing seed placements.

From the repository root, `./tools/build-apworld.ps1` creates the allowlisted distribution with licenses. See [packaging](../docs/PACKAGING.md).

[Install and connect](../docs/INSTALL.md) · [Progression](../docs/PROGRESSION.md) · [Tracker](../docs/UNIVERSAL_TRACKER.md) · [Historical APWorld README](../docs/archive/checkpoints/apworld-README.md)
