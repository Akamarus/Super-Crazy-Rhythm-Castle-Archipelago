# Public Development Roadmap

This is an unofficial, experimental development build rather than a release. The current candidate is Client v0.69.0 / APWorld v0.23.0. Music Lab Points requires manual acceptance with matching builds and a fresh v0.23 seed/save; automated readiness does not establish the live display or chest boundary.

Before the next client/APWorld build is presented for gameplay testing, complete the release-blocking checklist in [NEXT_RELEASE_BUG_FIXES.md](NEXT_RELEASE_BUG_FIXES.md). Items may be announced as fixed only after their listed acceptance tests pass.

## Status terms

| Status | Meaning |
| --- | --- |
| Implemented | Present in the current public prototype. |
| Implemented / needs more testing | Present, but still needs broader clean-save and gameplay testing. |
| Design approved / not implemented | Approved direction that is not in the current public prototype. |
| Discovery required | Native behavior or mappings must be confirmed before implementation. |
| Deferred | Intentionally postponed beyond the current prototype. |
| Experimental candidate / manual acceptance pending | Implemented and available for review; gameplay acceptance is still required. |

## Current implementation

| Work | Status | Current v0.69.0 / v0.23.0 candidate boundary |
| --- | --- | --- |
| Hub6 home with Area Access phone routing | Implemented / needs more testing | New AP saves redirect directly to Music Lab; random start currently selects only validated Roots Access. The first Roots arrival cutscene still has a save-processor timing regression. |
| Generation options and deterministic previews | Implemented | Required Stars (1–66, default 50) and starting area are exported with deterministic Level 1–22 requirements. |
| Active AP performance difficulty filtering | Implemented | Normal/Hard/Expert/Perfection address 92/129/166/202 locations after adding 24 cassette sources. |
| Star item registration and pool-capacity helper | Implemented | Star owns permanent ID 187256118 and the planner represents 66 individual items. They are not placed because the current modeled locations cannot yet fit Stars plus existing required progression. |
| Roots traversal baseline | Implemented / needs more testing | The first-arrival cutscene, FirstAreaGate, and StarEaterBlockade handling support the Roots-first prototype. |
| Weed Killer source and item | Implemented / needs more testing | Gecko sends an AP check; AP receipt grants the native consumable for the normal Level 3 route. |
| Plant Pipes source and item | Implemented / needs more testing | Fresh testing verified Frog/Hippo AP delivery plus Plant Pipes use across Level 3, Hub2/Level 4 scene changes, restart, and save load. Completed-Level-4 persistence remains pending. |
| Hip Glasses and Chicken Bucket chain | Implemented / needs gameplay acceptance | Level 4 and the Bucket Minion trade are AP checks. AP delivers both held items; native interactions consume them and produce non-network Combo Bucket. |
| Levels 1–3 campaign completion checks | Implemented / needs more testing | These are the only current campaign completion checks. |
| Game Garage cartridges and sticker checks | Implemented / live entrance accepted | Vampire Killer remains a physical vanilla pickup; five cartridges are AP items. All six songs retain cumulative Bronze through Platinum checks. |
| Music Lab cassette medal checks | Implemented / needs more testing | Thirty recognized cassette songs have cumulative medal checks. The four I Got Money medal checks require Money Cassette. |
| Level 2 Money Cassette pilot | Implemented / needs gameplay acceptance | The verified `Level_06 -> 110 -> I_GOT_MONEY` source sends `Level 2 - Money Cassette`; AP receipt reconciles `HAVE_IN_BAG` and normal Music Lab insertion deposits it. Fresh-save and save-switch IL2CPP acceptance remain pending. |
| Music Lab reward chests | Experimental candidate / manual acceptance pending | Nine existing checks now have weighted AP point rules. Only the managed getter in `GameRoom_Hub6` substitutes AP totals; recognized v0.22/non-AP sessions retain native score. Display and all thresholds need live acceptance. |
| Current development completion condition | Implemented / needs more testing | The APWorld currently validates the Area Access routing milestone; it is not the approved Level 22 victory design. |

## Native-discovery gate

| Work | Status | Why it must wait |
| --- | --- | --- |
| Remaining major-area routes and starter safety | Discovery required | Each phone arrival, return route, blocker, and clean-save opening needs validation. |
| Hip Glasses, Bucket Minion trade, and Chicken Bucket lifecycle | Implemented / needs gameplay acceptance | Source, held-item, trade, consumption, conversion, blockade, King-chat, and Lobby-arrival mappings are implemented. Fresh-save reload/reconnect acceptance remains required. |
| Remaining meaningful quest items and source checks | Discovery required | Native source, ownership, consumption, reload, and reconnection behavior must be proven before randomization. |
| Full Music Lab cassette randomization | Experimental test candidate / broad manual verification pending | All 30 items and sources are implemented for v0.22 testing. Five point-chest songs reuse their chest checks; 25 level-earned songs use evidence-backed trigger mappings. I Got Money's default route is live verified, its Bee alias and 24 newly mapped songs remain manual verification pending. |
| Character, versus, special-mode, and multiplayer behavior | Discovery required | Their eligibility, shared-result behavior, and safe native mappings are not yet confirmed. |

## Generation gate

| Work | Status | Planned result |
| --- | --- | --- |
| Live individual AP Stars and enforced level requirements | Design approved / not implemented | The v0.16 planner and seed-stable preview exist; pool placement and client gates remain inactive until capacity and solver validation are complete. |
| Hip Glasses, Bucket Minion trade, and Chicken Bucket | Implemented / needs gameplay acceptance | Hip Glasses are randomized at Level 4; the normal trade is the Chicken Bucket source check; normal Chicken Bucket use produces Combo Bucket. |
| Music Lab Point AP inventory | Experimental candidate / manual acceptance pending | 10/3/7 items worth 1/10/20 replace 20 Stardust: 180 points, final threshold 140, slack 40. No milestones or new locations; solver-reachable point chests may hold progression. |
| Live difficulty-based performance location sets | Implemented | Normal, Hard, Expert, and Perfection retain 92/129/166/202 locations. Historical four-seed filtering acceptance is retained. Active Level-22 2/3-Star checks stay filler-only; v0.23 models weighted point-chest reachability. |
| Complete item pool and solver validation | Design approved / not implemented | Generation must prove opening spheres, item capacity, and no self-locks. |
| Full campaign and meaningful-item logic | Design approved / not implemented | Every later area and level will combine Area Access, Stars, meaningful items, and vanilla story state. |

## Client/system gate

| Work | Status | Planned result |
| --- | --- | --- |
| AP connection and item delivery for the prototype | Implemented / needs more testing | Client v0.67.94 recognizes the retained implementation prefix; v0.20 adds APWorld-only difficulty filtering while native REG/PRO remains player-controlled. |
| Seed/save binding, robust synchronization, and offline reconciliation | Design approved / not implemented | Future sessions must validate compatibility, rebuild inventory safely, and queue checks across reconnects. |
| Player-facing AP notifications and integrated text log | Deferred | A later optional in-game text client will expose items, checks, connection state, and errors. |
| Local co-op verification | Design approved / not implemented | The architecture supports it by design, but shared-result behavior has not been smoke-tested. |

## Gameplay acceptance gate

| Work | Status | Acceptance boundary |
| --- | --- | --- |
| Difficulty-filtering generator matrix | Complete for v0.22 automated matrix | Normal, Hard, Expert, and Perfection pass the eight-option cassette/solver matrix at 92/129/166/202 addressed locations; representative gameplay remains pending. |
| Music Lab Points native boundary | Experimental candidate / manual acceptance pending | Diagnostic-first review rejected the chest/native detour. Prove the existing display, all nine below/at thresholds, existing check reuse, native medal invariance, disconnect/reconnect, relaunch, v0.22 fallback, and malformed-v0.23 zero behavior using the acceptance record. |
| Roots prototype smoke testing | Implemented / needs more testing | Test a fresh v0.20.0 seed/save through Weed Killer, Plant Pipes, Hip Glasses, Bucket Minion, Chicken Bucket, Lift Quest, and representative active campaign/Garage/Music Lab checks. |
| Full generated-seed matrix | Design approved / not implemented | Validate every supported start, difficulty, Star goal, and optional-area route. |
| Level 22 victory | Design approved / not implemented | Victory will require the synchronized AP Star goal and a subsequent Level 22 completion. |
| Full single-player acceptance run | Design approved / not implemented | Includes a post-threshold Level 22 clear, chest checks, representative difficulty tiers, and optional routes. |
| Local co-op smoke test | Design approved / not implemented | Required before describing local co-op as verified. |

## Deferred features

| Work | Status | Notes |
| --- | --- | --- |
| DeathLink and traps | Deferred | DeathLink is planned off by default only after safe failure behavior is tested. |
| Online co-op | Deferred | Intended as a later, low-priority host-authoritative feature. |
| Release packaging | Deferred | Public testers currently build both components from source. |
| Full release claim | Deferred | This project remains an experimental development build until the gates above are complete. |

For detailed current behavior, see the [project overview](PROJECT_OVERVIEW.md), [progression reference](PROGRESSION.md), and [historical gameplay evidence](HISTORICAL_GAMEPLAY_EVIDENCE.md). The [approved randomizer design](superpowers/specs/2026-08-20-randomizer-logic-design.md) describes future architecture, not a claim that it is implemented.

The [Music Lab Points acceptance record](testing/2026-09-09-music-lab-points-acceptance.md) owns the v0.23 live gate. Exact compatibility is required; the hub total remains zero before sync or on malformed v0.23 data, retains its last synchronized value on a temporary disconnect, and never receives native medal contributions. Existing five cassette/two cartridge chest sources remain single checks. No native score/save writes or chest/native detours are part of this candidate.
