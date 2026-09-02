# Changelog

## APWorld v0.22.0 / Client v0.68.0 — Experimental full Music Lab cassette testing

- Added an experimental test implementation for 30 Music Lab cassette items and 30 idempotent sources. Five point-chest songs reuse their existing 32/64/89/111/140-point chest locations; broader source-route acceptance remains pending.
- Added exact schema/count/item/source/reused-location compatibility. Any mismatch logs the differing key and leaves native cassette behavior enabled.
- Fixed default receipt dispatch so compatible cassette items are applied even when broad experimental progression grants are disabled.
- Fixed Epical, Hollywood Trailer, and False Data logic so their Cell Tower-return sources require both Lobby Access and Hypno Pan.
- AP receipts persist `HAVE_IN_BAG`; `HAVE_DEPOSITED` remains terminal, so insertion stays a normal player action.
- Added automatic native save persistence for newly received AP cassettes. Live testing verified two sequential cassette writes in one save session and a clean restart with both cassettes restored, no duplicate grants, and no extra save attempt.
- Requires a fresh v0.22 seed. The 24 newly mapped songs and the I Got Money Bee alias remain manual verification pending; no individual live verification is claimed without evidence.
- Removed the temporary INSERT cassette-catalog diagnostic binding from runtime release behavior. The evidence report and extraction tooling remain available to maintainers.
- The separate Game Garage cartridge native-inventory blocker remains open.

## APWorld v0.21.0 / Client v0.67.95 — Reliable Game Garage entrance

- Made Vampire Killer the native Game Garage entrance cartridge instead of a randomized AP item. Fresh v0.21 seeds preserve its physical vanilla pickup and normal insertion/entrance sequence, preventing unsupported zero-cartridge Garage entry.
- Kept the other five Game Garage cartridges randomized and kept all 24 cumulative medal checks. Vampire Killer medal checks are immediately reachable; the other songs still require their matching AP cartridge.
- Retired `Cartridge Pickup - Vampire Killer` from v0.21 generation and removed `Vampire Killer Cartridge` from the v0.21 item pool while preserving both permanent datapackage IDs for compatibility.
- Reduced active Normal/Hard/Expert/Perfection location totals to 67/104/141/177. Existing v0.20 seeds retain the prior six-cartridge behavior when used with Client v0.67.95.
- The client never writes Vampire Killer save flags; live acceptance confirmed that its physical pickup, Garage entry, and playable song all work normally.

## APWorld v0.20.0 / Client v0.67.94 — Active difficulty filtering

- Activated cumulative AP performance-location filtering: Normal includes campaign Completion/1-Star and Bronze checks; Hard adds 2-Star/Silver; Expert adds 3-Star/Gold; Perfection adds Platinum.
- Sized each player's item pool from that player's instantiated, addressed, unfilled locations, preserving exact Normal/Hard/Expert/Perfection capacities of 68/105/142/178 in shared multiworlds.
- Preserved every permanent item and location ID. Client v0.67.94 remains compatible and unchanged; the native REG/PRO choice remains player-controlled.
- Completed the four-seed real-generator acceptance matrix (seeds 42001–42004), including exact active tiers/counts, conservative filler placement, Garage reachability, and generated Victory playthroughs.

## Unreleased — APWorld v0.19 / Client v0.67.64 consolidated preview candidate

- Added bounded automatic AP reconnection with session replacement, queued-check retention, received-history deduplication, and deliberate-shutdown cancellation.
- Registered permanent preview items `Hypno Pan` (`187256121`) and `Violance` (`187256122`) without adding either to generated seeds; manual AP delivery reconciles and verifies their native abilities.
- Rejected failed or zero-Star Level 22 results so ordinary completion tiers are emitted only for successful one-to-three-Star clears and never imply Victory.
- Separated campaign Star HUD initialization from the Music Lab Points HUD context.
- Removed the failed bottom character/difficulty phase-forcing experiment and added exact-path, read-only lifecycle snapshots for later manual diagnosis.
- Added explicit Royal phone-side routing assertions plus an 800-case seed/difficulty/start reachability matrix.
- All entries remain development candidates until the consolidated manual acceptance passes; no release claim is made.

## APWorld v0.18 / Client v0.67.63 repair candidate

- Hardened fresh-save direct start so only a synchronized compatible AP slot and exact intro-room transition can redirect to Music Lab.
- Added verified-before-entry Roots intro suppression and exact post-Level-1 presentation suppression.
- Restored native Normal/Pro selection and campaign Star HUD bootstrap while preserving the Music Lab Points HUD.
- Bypassed only the two verified Music Lab construction-barrier roots in compatible AP sessions.
- Preserved the required physical Game Garage entrance pickup and holder/play initialization; AP ownership controls only song-cartridge children.
- Added ordinary Level 22 completion and cumulative one-to-three-Star checks at permanent IDs `187256182..187256185`; Victory remains inactive.
- Added completed-Level-4 Plant Pipes result reconciliation and conservative required-item placement rules for unmodeled point/tier checks.
- Automated verification is complete; combined gameplay acceptance remains pending.

- Added fail-closed v0.18 repair-contract metadata while preserving the existing v0.15–v0.17 implementation prefix and live systems.
- Reworked Plant Pipes delivery into received-history reconciliation after the selected save becomes readable. Native save access now runs only on Unity's main thread, uses bounded verified retries, rejects progression-hook re-entry, and does not require an unrelated native save request.
- Verified on 2026-08-21 that an existing AP-owned Plant Pipes item restored `WEED_KILLER_ABILITY=True`, remained usable in Roots, and survived a full game restart without another delivery.
- Kept the broader Plant Pipes durability blocker open until the complete receive → Level 3 → Hub2 → Level 4 route is replayed end to end on the repaired build.
- Added regression coverage for background-thread isolation, recursive native-grant protection, retry timing, duplicate history, compatibility gating, and nested client-test project isolation.
- Added a one-shot new-save redirect that sends newly created AP saves directly to Music Lab instead of requiring the two introductory rooms. Established saves are unchanged, and the exact legacy `GameRoom_04A` fallback remains available.
- Fresh-seed testing on 2026-08-22 verified Frog/Hippo source suppression and AP check delivery, one AP Plant Pipes receipt, immediate native use, Hub2 → Level 4 → Hub2 transitions, clean shutdown/reconnect, and restored use after loading the same save. Level 4 result persistence still needs a completed-level replay before the full durability gate closes.
- Recorded follow-up regressions from that run: the first Roots arrival cutscene can start before the save processor is available to set `ROOTS_HUB_INTRO_WITNESSED`, and the Star counter disappeared from the Roots HUD after Level 3.

## Unreleased — Known blocking issue

- Client v0.67.60 can become stuck on a permanent black screen when entering Game Garage with zero AP-owned Garage cartridges; audio continues and the pause/menu exit is unavailable.
- The next client release must include and verify a fix for zero-cartridge Garage entry before this item moves to that release's fixed list.
- Client v0.67.60 captures Level 22 as internal `Level_28` but does not map it to an AP completion location, so no Level 22 check is sent. The next client release must map and verify the ordinary completion separately from future Victory logic.
- The current AP graph exposes all Music Lab cassette checks even though fresh saves retain orange construction barriers that physically block many cassette machines. Barrier conditions and affected groups must be mapped before those locations are considered logically reachable.
- APWorld v0.17 can generate a BK'd seed by placing required Area Access and quest items behind blocked cassette machines, unowned Game Garage cartridges, native point thresholds, or unreasonable Platinum checks. Seed `AP_28223804408101432968` is retained as the regression case; generation must not be considered playable until solver rules match fresh-save physical reachability.
- Plant Pipes passed the fresh receive → Level 3 → Hub2 → Level 4 entry/exit → restart route on Client v0.67.62. A completed Level 4 result still needs to be persisted and replayed before the broader durability gate is called fully closed.

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
