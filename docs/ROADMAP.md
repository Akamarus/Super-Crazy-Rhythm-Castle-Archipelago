# Public Development Roadmap

This is an unofficial, experimental development build rather than a release. The tables below distinguish the current v0.67.59 client / v0.15 APWorld prototype from the approved future randomizer design. Testers should use a matching source build and generate a fresh seed after APWorld changes.

## Status terms

| Status | Meaning |
| --- | --- |
| Implemented | Present in the current public prototype. |
| Implemented / needs more testing | Present, but still needs broader clean-save and gameplay testing. |
| Design approved / not implemented | Approved direction that is not in the current public prototype. |
| Discovery required | Native behavior or mappings must be confirmed before implementation. |
| Deferred | Intentionally postponed beyond the current prototype. |

## Current implementation

| Work | Status | Current v0.67.59 / v0.15 boundary |
| --- | --- | --- |
| Hub6 home with Area Access phone routing | Implemented / needs more testing | Roots Access is forced as the starter; the five other Area Access items are randomized. |
| Roots traversal baseline | Implemented / needs more testing | The first-arrival cutscene, FirstAreaGate, and StarEaterBlockade handling support the Roots-first prototype. |
| Weed Killer source and item | Implemented / needs more testing | Gecko sends an AP check; AP receipt grants the native consumable for the normal Level 3 route. |
| Plant Pipes source and item | Implemented / needs more testing | Frog/Hippo is reachable after Weed Killer; Plant Pipes is required to finish Level 3. Menu exit before receiving it is intentional. |
| Levels 1–3 campaign completion checks | Implemented / needs more testing | These are the only current campaign completion checks. |
| Game Garage cartridges and sticker checks | Implemented / needs more testing | Six cartridges are AP items and each has cumulative Bronze through Platinum checks. |
| Music Lab cassette medal checks | Implemented / needs more testing | Thirty recognized cassette songs have cumulative medal checks; cassette items are not randomized yet. |
| Music Lab reward chests | Implemented / needs more testing | Nine native point-threshold chests are AP checks and are reconciled from the save. Native medal score remains the current currency. |
| Current development completion condition | Implemented / needs more testing | The APWorld currently validates the Area Access routing milestone; it is not the approved Level 22 victory design. |

## Native-discovery gate

| Work | Status | Why it must wait |
| --- | --- | --- |
| Remaining major-area routes and starter safety | Discovery required | Each phone arrival, return route, blocker, and clean-save opening needs validation. |
| Hip Glasses, Bucket Minion trade, and Chicken Bucket mappings | Discovery required | Historical ordering is preserved, but the exact native source/trade mappings and current Area Access treatment are unresolved. |
| Remaining meaningful quest items and source checks | Discovery required | Native source, ownership, consumption, reload, and reconnection behavior must be proven before randomization. |
| Cassette sources and native inventory mappings | Discovery required | Medal checks exist, but source-to-item randomization has not been mapped for all 30 cassettes. |
| Character, versus, special-mode, and multiplayer behavior | Discovery required | Their eligibility, shared-result behavior, and safe native mappings are not yet confirmed. |

## Generation gate

| Work | Status | Planned result |
| --- | --- | --- |
| Individual AP Stars and generated level requirements | Design approved / not implemented | Seed-stable gates will be derived from validated routing and must remain below the final goal. |
| Music Lab Point AP inventory | Design approved / not implemented | Point items will be separate from native medal score, pending final location-count validation. |
| Difficulty-based performance location sets | Design approved / not implemented | Normal, Hard, Expert, and Perfection will control generated performance checks. |
| Complete item pool and solver validation | Design approved / not implemented | Generation must prove opening spheres, item capacity, and no self-locks. |
| Full campaign and meaningful-item logic | Design approved / not implemented | Every later area and level will combine Area Access, Stars, meaningful items, and vanilla story state. |

## Client/system gate

| Work | Status | Planned result |
| --- | --- | --- |
| AP connection and item delivery for the prototype | Implemented / needs more testing | Current client/APWorld slot data supports the v0.15 prototype. |
| Seed/save binding, robust synchronization, and offline reconciliation | Design approved / not implemented | Future sessions must validate compatibility, rebuild inventory safely, and queue checks across reconnects. |
| Player-facing AP notifications and integrated text log | Deferred | A later optional in-game text client will expose items, checks, connection state, and errors. |
| Local co-op verification | Design approved / not implemented | The architecture supports it by design, but shared-result behavior has not been smoke-tested. |

## Gameplay acceptance gate

| Work | Status | Acceptance boundary |
| --- | --- | --- |
| Roots prototype smoke testing | Implemented / needs more testing | Test the fresh v0.15 seed, Roots route, Weed Killer, Frog/Hippo, Plant Pipes, and representative Garage/Music Lab checks. |
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
