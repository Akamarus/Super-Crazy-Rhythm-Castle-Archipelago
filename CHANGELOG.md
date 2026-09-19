# Changelog

## September 18 hotfix — Client v0.75.4

- Fixed the unsafe Star Eater reference assignment that caused crashes entering Roots or interacting with its Star Eater. Uses the native object-reference setter, verifies the assignment, and rolls back a failed write.
- Fixed repeated cassette-save metadata scans and reduced unnecessary scene searches, inventory copying and empty notification work. Live song testing showed cassette reconciliation falling from about 9.7 ms to 0.007 ms per frame. Players report a substantial improvement; occasional note-affecting hitches remain under investigation.
- Level 22 completion now requires Plant Pipes in generator logic. Its star checks, cassette source, King Ferdinand/keycard rewards and Victory inherit that requirement. Luck-dependent no-pipes clears are not assumed.
- Optional performance diagnostics are disabled by default. If enabled during troubleshooting, set `Developer.EnablePerformanceDiagnostics=false` and restart for ordinary play.

**Hotfix verification:** 140 APWorld tests, 34 client regression projects, and 12 real generation/playthrough cases across all difficulties and Star goals 1/40/66 pass.

**Updating:** replace the client DLLs with the v0.75.4 archive. Existing saves and seeds remain compatible with the client fixes. Install the updated `scrc.apworld` before generating a new seed to get the Plant Pipes rule. Updating files does not rewrite existing seed placements; some older seeds rely on the previous no-pipes boss route and would require a new seed or an explicitly agreed recovery to adopt the stricter rule.

**Still experimental:** the full clean-save victory playthrough is unfinished. The Quicksand/Music Lab chest vanilla-cassette leak and occasional gameplay hitches remain open; see [known issues](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/blob/main/docs/KNOWN_ISSUES.md).


## v0.28.0 — First Completable Release

**Client v0.75.0 / APWorld v0.28.0 · September 17, 2026**

This is the first release with the intended AP Star goal and an in-game final-boss victory condition implemented. It includes everything developed since v0.26.0-dev, including the previously unpublished v0.27 check expansion. It remains experimental: automated generation and client tests pass, but a complete clean-save native playthrough of the new Star/victory system has not yet been completed.

## AP Stars and victory

- All 66 individual AP Stars are active. Set `required_stars` from 1 to 66; the default is 50.
- Received AP Stars govern generated normal campaign entry requirements. The first two levels require zero; later requirements increase and cap at half the victory goal to keep progression moving. Every entry requirement stays below the goal, including Level 22.
- Win by successfully clearing normal Level 22 after reaching your Star goal. Clearing it early and receiving the last Star afterward does not grant victory; you must replay and clear it.
- Star counting rebuilds from authoritative item history, retains synchronized totals during temporary disconnects, and avoids duplicate counts on replay. Completed victory delivery can recover after reconnect/restart.
- The five Star Eaters use the new seed's transmitted AP Star thresholds. Stars are checked, not consumed. Old Royal/Bunker testing overrides do not bypass the new contract.
- The client exposes AP Star totals and level-preview requirements without overwriting native performance ratings. Earned level ratings still send the appropriate performance checks; they only increase AP Stars when a randomized reward is a Star.
- Music Lab Points remain separate: 20 items worth 180 points, with the same nine chest thresholds.

## 39 new checks since v0.26

Quest checks observe actual native sources. Ordinary quest rewards remain native unless an existing randomized item system replaces them. King Ferdinand's unlock now sends a check; King ownership itself is not a new randomized AP item.

- Roots - Combo Bucket Conversion
- Lobby - Important Letters Delivery
- Lobby - Plunger Hand-In
- Lobby - Star Eater Fed
- Lobby - Fish Tears Delivery
- Meat Dimension - Act 1 Music Delivery
- Meat Dimension - Act 2 Music Delivery
- Meat Dimension - Act 3 Music Delivery
- Meat Dimension - Act 4 Music Delivery
- Meat Dimension - Return Cat
- Meat Dimension - Return Scruffy
- Meat Dimension - Mouse Revolution
- Cell Tower - Deliver Super Nectar
- Tower of Fear - Restore Eye Statue
- Tower of Fear - Restore Mind Statue
- Tower of Fear - Restore Heart Statue
- Royal Corridor - Star Eater Fed
- Lobby - Important Letters Pickup
- Lobby - Demolition Certificate Award
- Meat Dimension - Wooden Spoon Pickup
- Meat Dimension - Saw Disc Pickup
- Meat Dimension - Frying Pan Pickup
- Meat Dimension - Fish Tears Award
- Cell Tower - Meet the Bees
- Tower of Fear - Eye Pickup
- Tower of Fear - Mind Pickup
- Tower of Fear - Heart Pickup
- Royal Corridor - Bunker Keycard Award
- Lobby - Use Bunker Keycard
- Secret Bunker - Gecko Interaction
- Secret Bunker - Star Eater Fed
- Nectar Party - Completion
- Act 1B: Nectar - Completion
- Demonic Room - Completion
- Demonic Tower - Completion
- Demonic Escape - Completion
- Demonic Lockers - Completion
- Cell Tower - Star Eater Fed
- Royal Corridor - King Ferdinand Unlocked

The two Bee and four Devil completions are binary completion checks; they do not create normal campaign Star-tier checks. Shared conversations and rewards are not counted again as invented extra locations.

Total addressed checks: **Normal 164 / Hard 222 / Expert 280 / Perfection 316** (previously 125 / 183 / 241 / 277).

## Fixes and progression improvements

- Fixed native Meoo/Maniac unlock sequences bypassing randomized character ownership. Hand-ins can send their checks while the characters wait for their AP items.
- Fixed delayed Hip Glasses and Chicken Bucket delivery on newly bound saves, with bounded retry and save-identity isolation. Consumed items remain consumed instead of being restored by received-history replay.
- Fixed the glowing but non-interactable Music Lab 20-point chest path by routing the native interaction through the existing AP point rule.
- Added source journals and result recovery for the expanded checks, with source-versus-ownership separation, duplicate suppression and AP/native-save isolation.
- Modeled campaign, cassette and quest prerequisites to fit the 66 Stars without reducing existing item quantities or adding artificial cache checks. Normal has 152 modeled eligible locations for 137 non-filler items.
- Corrected the Tower statue rules to require both the statue's own branch clearance and the part from another branch, including the relevant ability. Corrected Combo Bucket conversion to include its Level 5 gate.
- Rebalanced the generated Star curve after actual generator tests exposed failures with steeper gates.
- Removed repeated consumed-item diagnostic logging. This reduces log noise; it is not a confirmed fix for the temporary level lag described below.
- Preserved the existing item notifications/F6 history, native Garage cartridge insertion, cumulative medal checks, and older recognized seed contracts.

## Known bugs

- **Temporary severe lag around Combo Bucket conversion / Lift Quest:** reported during the broad playtest, recovering partway through the level. The cause and a complete fix are not established.
- **Hypno Pan crafting when already owned:** pre-granting Hypno Pan can make Frog and Hippo's crafting interaction unavailable. Ability use was confirmed working. A separate crafting check has not been added because its independent completion marker and safe reward replacement remain unresolved.

## Testing limits and remaining work

- The new AP Star HUD, native entry gates, new Star Eater thresholds, Cell Tower feed check, King unlock check and post-threshold victory still need final in-game acceptance together on a fresh seed/save. Generation success does not prove every native prerequisite or UI boundary.
- 33 of the previous 37 supplemental checks were confirmed in the broad gameplay pass. Demonic Tower, Demonic Escape, Demonic Lockers and Demolition Certificate are implemented but were not manually played in that pass. Royal/Bunker feeding was tested with temporary 25-star thresholds, not the original 40/66 thresholds.
- Failed special-level attempts, full save/seed-isolation gameplay, true cross-player notification coverage and the complete native campaign/cassette route matrix remain incompletely verified. Automated tests cover relevant policy and isolation cases.
- Local co-op and online co-op are not verified release features.
- Remaining source investigations: separate Hypno Pan crafting; Bizzle/Clive switches; independent Super Nectar component awards; Demon Key acquisition/use; final demon interaction versus its existing cartridge source. Bean Trumpet is still native and shares the Letters pickup; Notepad/Data Stick share the Gecko interaction; Bizzle/Clive acquisition shares Meet the Bees. These are not additional independent checks in this release.

## Verification

- 32/32 client regression projects passed, including production connection tests for Star history, stale sessions and identity-scoped victory delivery.
- 138/138 APWorld/repository tests and repository validation passed.
- 48/48 actual generated seeds passed fill and playthrough calculation across all four difficulties and goals 1/25/50/66. The matrix uses the final reviewed world rules.
- Release build succeeded with zero errors. Four existing nullable warnings remain; the build environment could not retrieve package vulnerability metadata.

## Install and start

1. Back up your saves. Close the game and extract `RhythmCastleAP-v0.75.0.zip` into `<GameDir>/BepInEx/plugins/RhythmCastleAP`, replacing the previous three DLLs.
2. Install the matching `scrc.apworld` through Archipelago and restart the launcher.
3. Generate a **fresh v0.28 seed** and use a **fresh native save**. Updating files cannot add Stars or the new checks to an already generated seed.
4. **Connect the seed before loading the save**, so the AP start and received-item state are available.

The source uses schema 19 / campaign-mapping schema 1. Recognized older seeds retain their earlier behavior. See the [installation guide](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/blob/v0.28.0/docs/INSTALL.md) and [known issues](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/blob/v0.28.0/docs/KNOWN_ISSUES.md).

## Next

Complete a fresh-seed end-to-end AP Star/victory playthrough, investigate the conversion lag, and finish native route and co-op validation. Continue the remaining distinct-source investigations without duplicating shared interactions.

---

## Testing release v0.26.0-dev / Client v0.73.9

This experimental prerelease adds randomized Old Game Data and Car Battery, in-game item popups and F6 history, corrected location names and cassette prerequisites, performance/reconnect repairs, and native Game Garage cartridge insertion and medal-preview fixes. It also includes new Plunger/Meoo/Maniac AP rewards and four quest checks awaiting fresh-seed gameplay acceptance.

Use Client v0.73.9 and APWorld v0.26.0 together with a fresh v0.26 seed and fresh native save for the new quest features. Existing schema-16 seeds retain their earlier behavior; updating files does not add checks to an old seed. Connect to the AP seed before loading the save.

Gameplay confirmed: Old Game Data/Car Battery hand-ins and persistence under schema 16, improved performance, local cold-start reconnect, and Bloody Tears/Gradius Garage insertion, availability and medal previews. v0.73.9 cleanup passed the Garage entry/exit check. New schema-17 quest flows and true cross-player notifications still need live testing. AP Stars, generated Star gates, final Victory and production Bee/Devil checks remain inactive.

New checks: Lobby - Plunger Pickup; Lobby - Car Battery Hand-In; Game Garage - Old Game Data Hand-In; Roots - Star Eater Fed. Active totals: Normal 125 / Hard 183 / Expert 241 / Perfection 277. Plunger Pickup and Roots Star Eater Fed are Stardust-only pending fuller prerequisite modeling; feeding still checks three native stars without spending them.

Next: validate new quest rewards/checks on a fresh seed, including early receipt and offline/reconnect isolation, then continue remaining quest logic and Star/Victory work.


## Client v0.73.9 — Garage cleanup accepted (2026-09-17)
Removed superseded manual cartridge release/spawning helpers, unused release-visit coordinator and its obsolete tests, ineffective sequence/song diagnostic hooks, repeated saved-score dumps, and room-readiness trace spam. Removed the unused release-state set and dead bag-observation wrapper. Preserved native holder initialization, door consumption tracking, save/connection isolation, readiness guards, saved-medal preview reads, active cartridge check detection, and error reporting.
All 25 client test projects passed during cleanup; affected Garage persistence, Garage availability and Music Lab Points suites passed after diagnostic hook removal. Final bag-wrapper cleanup passed Garage persistence tests and Release build (four existing nullable warnings and one unavailable NuGet audit metadata warning). No new IDs, seed changes, save edits, or server changes. Installed with explicit approval and verified startup/version, native hooks, and retained server connection. User confirmed the Garage entry/exit and medal-preview check; v0.73.9 is the accepted installed baseline.
Next: separate fresh-seed acceptance for schema-17 Plunger, character unlocks/hand-ins, and Roots Star Eater Fed checks.


## APWorld v0.26.0 / Client v0.73.0 — Uninstalled quest-check candidate

- Adds useful Plunger, Meoo, and Maniac AP items at IDs `187256161..187256163` and four quest checks at `187256294..187256297`; previous IDs remain unchanged. The next item/location IDs are `187256164` / `187256298`.
- Old Game Data and Car Battery become progression inputs. Their Music Lab chest sources are unchanged; their ordinary hand-ins send new checks independently of AP character ownership.
- Publishes slot-data schema 17 and quest checks schema 1, retaining original character-item maps. New quests require a fresh v0.26 seed and fresh native save; existing schema-16 seeds retain prior behavior.
- Addressed totals are Normal 125 / Hard 183 / Expert 241 / Perfection 277. Plunger pickup and Roots Star Eater Fed accept only Stardust while their full gates remain unmodeled. The native three-star feed threshold is unchanged; AP Stars remain inactive.
- Includes candidate safeguards for early AP character receipt and early Plunger receipt/use, with offline/restart and save/identity isolation requiring gameplay acceptance.
- Game Garage native door-consumption repair and targeted read-only saved-score detection are candidates, not evidence that score reporting is fixed.
- Not installed or gameplay accepted. The installed client remains v0.72.0. See [the candidate acceptance record](docs/testing/2026-09-16-quest-checks-candidate.md); historical evidence below remains unchanged.

## APWorld v0.24.0 / Client v0.70.0 â€” Full normal-campaign mapping candidate

- Added all 22 normal campaign identities with separate Completion and cumulative 1-Star/2-Star/3-Star checks. Addressed totals are Normal 121 / Hard 179 / Expert 237 / Perfection 273.
- Allocated the 81 new normal-campaign IDs in `187256211..187256291`; the next safe item/location IDs are `187256156` / `187256292`. Historical Development Cache IDs remain reserved but are no longer instantiated.
- Published the schema-15 / campaign-mapping-schema-1 contract and matching client candidate. A fresh v0.24 seed and fresh native save are required.
- Retained the strict AP Music Lab Points economy under v0.24/schema 15 while preserving v0.23/schema 14 compatibility. Unsupported or malformed AP claims fail closed with an immediate field-specific compatibility error.
- Preserved the historical v0.23 Level 22 Completion and cumulative 1/2/3-Star behavior at its enabled difficulty tiers without activating the new full-campaign map for old seeds.
- Fixed temporary-disconnect and failed-reconnect handling so eligible campaign results queue once against the authenticated game/seed/team/slot identity, survive failed retries, flush once after recovery, and cannot leak into a different authenticated identity or unauthenticated replacement transport.
- Bee/Devil handling is diagnostic-only and read-only. Special-mode locations, all 66 AP Stars, generated Star gates, and final Victory remain inactive.
- Automated release verification passed 20/20 client projects, 102/102 APWorld tests, the live repository validator, local APWorld packaging, and an undeployed client build. No broad gameplay replay is claimed; normal first-clear, improved-result, offline/reconnect, diagnostic, and unplayed-mapping evidence remain manual acceptance work.

## APWorld v0.23.0 / Client v0.69.0 â€” Experimental Music Lab Points testing

- Added 10 Music Lab Point items worth 1 each, 3 bundles worth 10 each, and 7 large bundles worth 20 each: 180 points in 20 progression instances replacing 20 Stardust. The existing nine thresholds are 5/10/20/32/46/64/89/111/140; the final threshold leaves 40 slack.
- Added the exact schema-14/point-schema-1 contract and permanent point item IDs `187256153..187256155`. The next item ID is `187256156`; the next location ID remains `187256211`, and active location counts remain 92/129/166/202.
- Weighted point-chest rules allow solver-reachable progression. No point milestones or duplicate cassette/cartridge checks are added; all existing source reuse is retained.
- The managed score getter applies AP totals only in `GameRoom_Hub6`: zero before synchronization, authoritative weighted receipts capped at 180 after synchronization, and the retained total during a temporary disconnect. Reconnect and relaunch rebuild history without double-counting. Native medals never contribute, native medal/save scores are never written, and compatible AP state supersedes the developer override.
- Recognized v0.22 seeds and non-AP play retain native scoring. Malformed v0.23 point contracts report incompatibility and fail closed at zero.
- Diagnostic-first investigation rejected the generated chest hook and native detour. The room-scoped client installs neither and never forces chest interactions. Live testing verified weighted totals, the 180-point cap, exact threshold pairs at 5/10/20/64/89/111/140, all nine one-time chest checks, native medal invariance, disconnected-total retention, automatic reconnect/history rebuild, and full-relaunch restoration. The project owner waived redundant exact pairs at 32 and 46; queued-check recovery and the v0.22/non-AP/malformed-v0.23 compatibility matrix remain pending.
- Fixed a pre-login connection-refused callback stall by evaluating socket state lazily only for established sessions. Repeated server-off retries remained responsive and automatically reconnected when the server returned.
- Advanced candidate metadata/documentation and independent validation, including rejection of changed base-ID assignments. All historical implementation-version prefixes are preserved, ending `full-cassettes-0.22-music-lab-points-0.23`. Prior cassette-route and unrelated known-issue acceptance remains open.

## APWorld v0.22.0 / Client v0.68.0 â€” Experimental full Music Lab cassette testing

- Added an experimental test implementation for 30 Music Lab cassette items and 30 idempotent sources. Five point-chest songs reuse their existing 32/64/89/111/140-point chest locations; broader source-route acceptance remains pending.
- Added exact schema/count/item/source/reused-location compatibility. Any mismatch logs the differing key and leaves native cassette behavior enabled.
- Fixed default receipt dispatch so compatible cassette items are applied even when broad experimental progression grants are disabled.
- Fixed Epical, Hollywood Trailer, and False Data logic so their Cell Tower-return sources require both Lobby Access and Hypno Pan.
- AP receipts persist `HAVE_IN_BAG`; `HAVE_DEPOSITED` remains terminal, so insertion stays a normal player action.
- Added automatic native save persistence for newly received AP cassettes. Live testing verified two sequential cassette writes in one save session and a clean restart with both cassettes restored, no duplicate grants, and no extra save attempt.
- Requires a fresh v0.22 seed. The 24 newly mapped songs and the I Got Money Bee alias remain manual verification pending; no individual live verification is claimed without evidence.
- Removed the temporary INSERT cassette-catalog diagnostic binding from runtime release behavior. The evidence report and extraction tooling remain available to maintainers.
- The separate Game Garage cartridge native-inventory blocker remains open.

## APWorld v0.21.0 / Client v0.67.95 â€” Reliable Game Garage entrance

- Made Vampire Killer the native Game Garage entrance cartridge instead of a randomized AP item. Fresh v0.21 seeds preserve its physical vanilla pickup and normal insertion/entrance sequence, preventing unsupported zero-cartridge Garage entry.
- Kept the other five Game Garage cartridges randomized and kept all 24 cumulative medal checks. Vampire Killer medal checks are immediately reachable; the other songs still require their matching AP cartridge.
- Retired `Cartridge Pickup - Vampire Killer` from v0.21 generation and removed `Vampire Killer Cartridge` from the v0.21 item pool while preserving both permanent datapackage IDs for compatibility.
- Reduced active Normal/Hard/Expert/Perfection location totals to 67/104/141/177. Existing v0.20 seeds retain the prior six-cartridge behavior when used with Client v0.67.95.
- The client never writes Vampire Killer save flags; live acceptance confirmed that its physical pickup, Garage entry, and playable song all work normally.

## APWorld v0.20.0 / Client v0.67.94 â€” Active difficulty filtering

- Activated cumulative AP performance-location filtering: Normal includes campaign Completion/1-Star and Bronze checks; Hard adds 2-Star/Silver; Expert adds 3-Star/Gold; Perfection adds Platinum.
- Sized each player's item pool from that player's instantiated, addressed, unfilled locations, preserving exact Normal/Hard/Expert/Perfection capacities of 68/105/142/178 in shared multiworlds.
- Preserved every permanent item and location ID. Client v0.67.94 remains compatible and unchanged; the native REG/PRO choice remains player-controlled.
- Completed the four-seed real-generator acceptance matrix (seeds 42001â€“42004), including exact active tiers/counts, conservative filler placement, Garage reachability, and generated Victory playthroughs.

## Unreleased â€” APWorld v0.19 / Client v0.67.64 consolidated preview candidate

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

- Added fail-closed v0.18 repair-contract metadata while preserving the existing v0.15â€“v0.17 implementation prefix and live systems.
- Reworked Plant Pipes delivery into received-history reconciliation after the selected save becomes readable. Native save access now runs only on Unity's main thread, uses bounded verified retries, rejects progression-hook re-entry, and does not require an unrelated native save request.
- Verified on 2026-08-21 that an existing AP-owned Plant Pipes item restored `WEED_KILLER_ABILITY=True`, remained usable in Roots, and survived a full game restart without another delivery.
- Kept the broader Plant Pipes durability blocker open until the complete receive â†’ Level 3 â†’ Hub2 â†’ Level 4 route is replayed end to end on the repaired build.
- Added regression coverage for background-thread isolation, recursive native-grant protection, retry timing, duplicate history, compatibility gating, and nested client-test project isolation.
- Added a one-shot new-save redirect that sends newly created AP saves directly to Music Lab instead of requiring the two introductory rooms. Established saves are unchanged, and the exact legacy `GameRoom_04A` fallback remains available.
- Fresh-seed testing on 2026-08-22 verified Frog/Hippo source suppression and AP check delivery, one AP Plant Pipes receipt, immediate native use, Hub2 â†’ Level 4 â†’ Hub2 transitions, clean shutdown/reconnect, and restored use after loading the same save. Level 4 result persistence still needs a completed-level replay before the full durability gate closes.
- Recorded follow-up regressions from that run: the first Roots arrival cutscene can start before the save processor is available to set `ROOTS_HUB_INTRO_WITNESSED`, and the Star counter disappeared from the Roots HUD after Level 3.

## Unreleased â€” Known blocking issue

- Client v0.67.60 can become stuck on a permanent black screen when entering Game Garage with zero AP-owned Garage cartridges; audio continues and the pause/menu exit is unavailable.
- The next client release must include and verify a fix for zero-cartridge Garage entry before this item moves to that release's fixed list.
- Client v0.67.60 captures Level 22 as internal `Level_28` but does not map it to an AP completion location, so no Level 22 check is sent. The next client release must map and verify the ordinary completion separately from future Victory logic.
- The current AP graph exposes all Music Lab cassette checks even though fresh saves retain orange construction barriers that physically block many cassette machines. Barrier conditions and affected groups must be mapped before those locations are considered logically reachable.
- APWorld v0.17 can generate a BK'd seed by placing required Area Access and quest items behind blocked cassette machines, unowned Game Garage cartridges, native point thresholds, or unreasonable Platinum checks. Seed `AP_28223804408101432968` is retained as the regression case; generation must not be considered playable until solver rules match fresh-save physical reachability.
- Plant Pipes passed the fresh receive â†’ Level 3 â†’ Hub2 â†’ Level 4 entry/exit â†’ restart route on Client v0.67.62. A completed Level 4 result still needs to be persisted and replayed before the broader durability gate is called fully closed.

## APWorld v0.17 / Client v0.67.60 â€” Roots bucket progression

- Added permanent randomized items `Hip Glasses` (`187256119`) and `Chicken Bucket` (`187256120`).
- Added Level 4 Hip Glasses and Bucket Minion trade checks (`187256180` and `187256181`).
- Preserved the native trade, blockade, King conversation, Lift Quest, Combo Bucket, completion, and Lobby-arrival lifecycle while suppressing only the two vanilla inventory grants.
- Added fail-closed slot-data compatibility and received-history reconciliation so durable consumed markers prevent items returning after trade/conversion.
- Kept Combo Bucket non-network and kept Star gates/difficulty filtering as inactive previews. A fresh v0.17 seed and fresh-save gameplay acceptance are required.

## APWorld v0.16 â€” Generation foundation

- Added configurable `required_stars`, difficulty, and starting-area options. Random currently selects only the validated Roots starter; unsupported fixed starts fail generation.
- Added deterministic provisional Star requirements for Levels 1â€“22 and cumulative difficulty-location previews.
- Registered Star item ID `187256118` and a pure 66-item inventory/capacity planner.
- Kept Star placement, live difficulty filtering, client Star-gate enforcement, and final victory inactive. The existing Area Access victory milestone remains live until additional validated locations and solver-backed pool construction can safely hold all planned Stars and required progression.
- Added additive slot-data flags that identify preview versus active systems. The implementation tag retains the v0.15 prefix required by the current client and appends the v0.16 foundation marker. Existing v0.15 seeds must be regenerated for v0.16.

## Unreleased â€” Public testing documentation

- Added the public smoke-test and issue-reporting guide, with installation and advanced-diagnostics cross-links.
- Documented the public-testing boundary, expected evidence, safe redaction guidance, and known experimental limitations.

Documentation only: this section does not indicate a client or APWorld version bump, release artifact, or completed randomizer milestone.

## Documentation

- Added `docs/PROJECT_OVERVIEW.md` as the living player/contributor-facing architecture and randomizer design guide.
- Documented the current AP item catalog, Roots progression graph, Area Access routing, Game Garage, Music Lab cassettes/reward chests, difficulty philosophy, planned Star gating, Secret Bunker status, roadmap, and AI-development disclosure.
- Updated repository workflow to require maintaining the overview when player-facing behavior changes.

This file begins at the point the project moved to source control. Earlier experimental versions remain represented by the permanent ID history and existing development notes rather than by imported ZIP history.

## Baseline imported to Git â€” 2026-08-20

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

## Unreleased naming correction

- Rename the check at `187256179` to `Roots - Level 3 - Plant Pipes Pickup`, making its reward source explicit. The ID, source flag, and reachability rule remain unchanged. Updated client and APWorld names must be used together with a newly generated seed.

### Client v0.73.1 candidate

Replaces the unloadable Garage door wrapper hook with a validated native detour,
and removes the redundant unloadable Meoo Switch hook while retaining durable
sequence admission guards. Gameplay validation pending; native score issue open.

### Client v0.73.2 candidate

Replaces new virtual Harmony hooks with native original trampolines after a
slot-load stack overflow. Unreadable quest sequence paths deny admission.
Awaiting deployment and live slot-load acceptance; Garage score issue remains open.

### Client v0.73.3 candidate

Consolidates the two native flag-condition aliases under the existing area hook
to avoid double-patching the same function. Runtime alias verification gates quest
readiness. Stable v0.72 remains installed pending explicit candidate approval.

### Client v0.73.4 candidate

Defers AP Garage cartridge visibility and bag reconciliation until successful
native room preparation, addressing an observed initialization overlap during a
loading hang. Gameplay acceptance pending; preserves v0.73.3 shared-hook repair.

### Client v0.73.5 candidate

Uses native current-room and idle-task evidence to reopen the Garage mutation gate
after loading. Replaces the preparation callback that never reopened in live play.
Cartridge availability, consumption and native score persistence await acceptance.

### v0.73.6 Garage native-root reader candidate (2026-09-17)
The v0.73.5 live readiness log never opened the gate: currentGameRoom was unreadable even with task NONE. This left AP-owned Bloody Tears in the preview/bag while the native room still exposed unowned Gradius. Replace the generated nullable field read with the native nonvirtual FindRootObjectForCurrentGameRoom lookup, called only after task NONE. Require that returned root contains GameRoom_27_Logic/Objects/Cartridges, plus the existing transition epoch guard. Do not use a global scene lookup or remove the loading guard. The native lookup and exact returned hierarchy still require live confirmation. Cartridge consumption and native score persistence are not yet accepted.
Verification: Garage cartridge persistence regression suite passed (including idle-only lookup, absent root, stale epoch and loading guards); Release build passed with five pre-existing warnings. Candidate is not deployed. Next: approve client-only deployment, reconnect same seed/slot 4, enter Garage and verify Bloody Tears available, Gradius absent, then insertion consumption and score persistence.

### v0.73.7 Garage consumption and medal preview candidate (2026-09-17)
Live v0.73.6 accepted the idle room-root gate: Garage opened, owned Bloody Tears released and unowned Gradius hidden. User completed Bloody Tears at 200199/Silver, but bag item and blank preview persisted. Bronze AP check sent; Silver AP location correctly inactive in this Normal seed.
Root-cause evidence from installed native code: Room27LevelEntranceDoor.OpenDoor (RVA 0x5597B0) inlines the bag-removal body, bypassing RemoveBagItemIfOwned (0x559CB0); observing the helper cannot catch that consumption. Replace the helper detour with a zero-argument OpenDoor detour and snapshot/readback all randomized AP-owned bag items around exactly one original call. Existing generation/reset guards and durable insertion coordinator remain.
LevelPreviewUIView.RefreshToMatchState (RVA 0x87AE10, ownership branch around 0x87B48C) inserts NONE medals without calling saved-medal getters when native cartridge ownership is false. The previous ReflectGarageVisuals ownership-only patch exposed an AP cartridge with these blank medals. Populate that preview DTO's regular/pro medals from native saved getters for AP-owned randomized cartridges. Preserve native Vampire Killer and unowned/disabled entries. Never write saved scores or medals.
Correct misleading diagnostics: native saved-score/medal getters return Nullable<T>; IL2CPP boxes these as the underlying T or null, but generated wrappers present them as Nullable<T>. Prior INVALID/0/NONE readbacks are not reliable proof of missing saved results. New read-only getter invocation uses native MethodInfo metadata, validated Int32 enum arguments, native boxed class validation and unboxing; empty results remain absent. Live native integration remains unaccepted until next test.
Regression coverage: AP-owned/native-uncollected Silver/pro medal, no reads for disabled/unowned, missing data preserves display, vanilla cartridge untouched; existing door invocation and persistence tests cover original-call and consumption guards. Build and focused Garage suites passed before final packaging; no deployment or native-save modification performed.
Next: approve client v0.73.7 deployment; connect retained seed before loading slot 4; check whether existing Silver appears in entrance preview, enter Garage and verify Bloody Tears leaves bag and stays playable; exit/re-enter to verify no regrant. Only replay if saved Silver is still absent.

### v0.73.8 native Garage entrance candidate (2026-09-17)
User requirement: retain vanilla cartridge insertion into the room; no visible delayed spawn. v0.73.7 live log confirmed OpenDoor ran, but its immediate readback still held the bag item; native setup later removed it and the mod regranted it. A PlayerSaveRequestProcessor replacement also occurred inside the door call, invalidating the generic reset epoch.
Replace keeper SetActive/release polling and object bindings with native Room27GameCartridgeHolder.HandleEvent(GameRoomReadyToRunEvent) initialization. Original holder callback always runs exactly once; a thread-local scope overrides only its read of the five randomized collected flags through GameProgressionEnquiries.IsFlagSet using AP ownership. No saved collected flag, bag flag, holder placement or reparenting is written by this hook. Vampire Killer remains native. Validate empty event ABI (one byte), exact method signatures, owners and nonzero distinct pointers at install. Native disassembly verifies direct IsFlagSet call inside the holder event; CartridgeIsOwned itself is inlined and is not hooked.
Observe delayed door consumption until the expected Hub6-to-Garage transition completes and native bag readback is available. Pending evidence blocks regrant; a held-to-absent result is recorded through the existing insertion coordinator before grant decisions. Distinguish actual save/configuration boundaries from stateless processor replacement so the native door handoff cannot discard the snapshot. Explicit save change, wrong transition or connection change still invalidates evidence. A new AP cartridge received while already inside waits for the next native room initialization/entry.
Regression cases cover delayed still-held/absent readback, unreadable state, stale connection, wrong transition, actual still-held at ready, AP holder ownership, untouched bag flags/vanilla Vampire and absence of late keeper activation. Garage persistence and availability suites pass; Release build passes with five existing warnings. Review identified the processor-reset timing issue, now corrected. Live native hook/consumption and preview acceptance still pending. No deployment/save reset/new seed performed.
Next: approve v0.73.8, connect retained seed, load slot 4, enter Garage through normal door. Verify bag removal, cartridges already placed on room reveal, continued availability after exit/re-entry and preview Silver.

### v0.73.8 gameplay acceptance (2026-09-17)
User confirmed normal Garage behavior for Bloody Tears, then requested a second cartridge test. Sent exactly one Gradius Remix Cartridge through the retained seed 14964085740301527950 server to Jack (item 187256111); receipt and native bag grant verified. User subsequently confirmed Gradius working correctly. Saved native readbacks verify Bloody Tears 200199 / SILVER (including Pro), Gradius Remix 390567 / GOLD (including Pro), and Vampire Killer 344423 / GOLD. Thus earlier zero/INVALID diagnostic output was misleading; native results were retained. User accepts cartridge consumption, normal room placement and preview behavior on this build. Evidence retained locally as work/v0738-gradius-accepted.log in task workspace. Keep installed v0.73.8; next check is persistence after an ordinary game restart, connecting seed before loading slot 4, with no new grants or save resets.

### v0.73.8 restart persistence accepted (2026-09-17)
User completed an ordinary game restart, reconnected the retained seed before slot 4, and confirmed both Bloody Tears and Gradius remained playable with their medals intact. Garage cartridge consumption, native room placement, preview medals, and restart persistence are now gameplay accepted. Keep installed v0.73.8 as the accepted client baseline. Next work: clean up superseded Garage workaround code and temporary diagnostics in the isolated worktree, preserving accepted native behavior; new schema-17 quest feature acceptance remains separate from this retained schema-16 seed.
