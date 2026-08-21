# Changelog

## Unreleased — Known blocking issue

- Client v0.67.60 can become stuck on a permanent black screen when entering Game Garage with zero AP-owned Garage cartridges; audio continues and the pause/menu exit is unavailable.
- The next client release must include and verify a fix for zero-cartridge Garage entry before this item moves to that release's fixed list.
- Client v0.67.60 captures Level 22 as internal `Level_28` but does not map it to an AP completion location, so no Level 22 check is sent. The next client release must map and verify the ordinary completion separately from future Victory logic.
- The current AP graph exposes all Music Lab cassette checks even though fresh saves retain orange construction barriers that physically block many cassette machines. Barrier conditions and affected groups must be mapped before those locations are considered logically reachable.

## APWorld v0.17 / Client v0.67.60 — Roots bucket progression

- Added permanent randomized items `Hip Glasses` (`187256119`) and `Chicken Bucket` (`187256120`).
- Added Level 4 Hip Glasses and Bucket Minion trade checks (`187256180` and `187256181`).
- Preserved the native trade, blockade, King conversation, Lift Quest, Combo Bucket, completion, and Lobby-arrival lifecycle while suppressing only the two vanilla inventory grants.
- Added fail-closed slot-data compatibility and received-history reconciliation so durable consumed markers prevent items returning after trade/conversion.
- Kept Combo Bucket non-network and kept Star gates/difficulty filtering as inactive previews. A fresh v0.17 seed and fresh-save gameplay acceptance are required.

## APWorld v0.16 — Generation foundation

- Added configurable `required_stars`, difficulty, and starting-area options. Random currently selects only the validated Roots starter; unsupported fixed starts fail generation.
- Added deterministic provisional Star requirements for Levels 1–22 and cumulative difficulty-location previews.
- Registered Star item ID `187256118` and a pure 66-item inventory/capacity planner.
- Kept Star placement, live difficulty filtering, client Star-gate enforcement, and final victory inactive. The existing Area Access victory milestone remains live until additional validated locations and solver-backed pool construction can safely hold all planned Stars and required progression.
- Added additive slot-data flags that identify preview versus active systems. The implementation tag retains the v0.15 prefix required by the current client and appends the v0.16 foundation marker. Existing v0.15 seeds must be regenerated for v0.16.

## Unreleased — Public testing documentation

- Added the public smoke-test and issue-reporting guide, with installation and advanced-diagnostics cross-links.
- Documented the public-testing boundary, expected evidence, safe redaction guidance, and known experimental limitations.

Documentation only: this section does not indicate a client or APWorld version bump, release artifact, or completed randomizer milestone.

## Unreleased — Local AI model evaluation

- Added a deterministic exact-answer comparison for the allowlisted `jacks-assistant` and `jacks-assistant-fast` profiles, with redacted reports beneath ignored `.local-ai/evaluations/` state.
- Documented the manual evaluation and model-selection workflow. `jacks-assistant` remains the default, reports never update local configuration automatically, and human review remains required.
- Added repository validation for the fixed decision ledger, evaluation cases, model allowlist, public evaluation command, and manifest export.

Tooling and documentation only: the evaluation is intentionally narrow and advisory. It does not validate or change gameplay, APWorld generation, client behavior, review requirements, build/deployment containment, or release state.

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
