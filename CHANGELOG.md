# Changelog

## Unreleased — Public testing documentation

- Added the public smoke-test and issue-reporting guide, with installation and advanced-diagnostics cross-links.
- Documented the current v0.67.59 client / v0.15 APWorld testing boundary, expected evidence, safe redaction guidance, and known experimental limitations.

Documentation only: this section does not indicate a client or APWorld version bump, release artifact, or completed randomizer milestone.

## Documentation

- Added `docs/PROJECT_OVERVIEW.md` as the living player/contributor-facing architecture and randomizer design guide.
- Documented the current AP item catalog, Roots progression graph, Area Access routing, Game Garage, Music Lab cassettes/reward chests, difficulty philosophy, planned Star gating, Secret Bunker status, roadmap, and AI-development disclosure.
- Updated repository workflow to require maintaining the overview when player-facing behavior changes.

This file begins at the point the project moved to source control. Earlier experimental versions remain represented by the permanent ID history and existing development notes rather than by imported ZIP history.

## Baseline imported to Git — 2026-08-20

### Client v0.67.59

- Hub6 direct-start / Area Access routing baseline.
- Roots first-arrival cutscene bypass via `ROOTS_HUB_INTRO_WITNESSED` before Hub2 transition.
- Exact Roots traversal normalization for `FirstAreaGate` and `StarEaterBlockade/Blockade`.
- Gecko Weed Killer source randomized.
- AP-delivered Weed Killer grants native `WEED_KILLER_BAG_ITEM`.
- Frog/Hippo Plant Pipes source randomized.
- AP-delivered Plant Pipes grants native `WEED_KILLER_ABILITY`.
- Existing Game Garage, cassette, Music Lab reward-chest, cartridge, and Secret Bunker systems retained.

### APWorld v0.15

- Roots Access forced as the precollected development starter.
- Weed Killer item / Gecko source check retained.
- Added Plant Pipes item `187256117`.
- Added `Roots - Level 3 - Frog and Hippo` location `187256179`.
- Split Level 3 reachability from completion: Weed Killer reaches Frog/Hippo; Plant Pipes is required for Level 3 Completion.
