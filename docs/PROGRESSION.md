> Current published scope: **v0.28.0 / Client v0.75.0 — First Completable Release**. See the [release notes](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/blob/v0.28.0/docs/releases/v0.28.0.md) and [known issues](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/blob/v0.28.0/docs/KNOWN_ISSUES.md). Native AP Star/victory acceptance remains pending; older candidate and deployment statements below record historical checkpoints.

# AP Stars combined candidate — native playthrough acceptance pending

**Client v0.75.0 / APWorld v0.28.0** uses schema 19 / campaign-mapping schema 1.
Use a fresh v0.28 seed and fresh native save for candidate testing. This implementation
has not been deployed; installed client v0.74.2 and the running test seed remain unchanged.
The published v0.26.0-dev release is a separate historical baseline.

Active totals: Normal 164 / Hard 222 / Expert 280 / Perfection 316.
All 66 AP Stars are active, with generated campaign gates and a Level 22 clear after
reaching the configured goal (1–66, default 50). An early boss clear followed by later
Star receipt does not win; another clear is required. Native result stars still report
performance checks and are never overwritten by AP Stars.

The 39 supplemental sources include Cell Tower Star Eater Fed and King Ferdinand
Unlocked. The six Bee/Devil completions remain separate binary checks. The user
confirmed 33 of the prior 37 checks; three Devil runs and the Certificate were skipped
and remain unverified. No replay of those skipped tests is required in this pass.

Normal has 152 modeled non-filler slots for 137 non-filler items (135 progression plus
two useful characters), not merely 164 raw checks. Existing item quantities and the
180-point Music Lab economy are unchanged. Native prerequisite closures are conservative
candidate rules; full native playthrough validation remains pending. The final world
passed a 48-case actual generation/playthrough matrix across four difficulties and
goals 1/25/50/66. A prior near-goal gate curve failed; the corrected curve caps entry
gates at half the goal while Victory still requires the full goal.

Detailed rules: `apworld/scrc/docs/ap-stars-logic.md`. Native acceptance, deployment
and release are separate steps. The older version/status sections below are historical
and do not override this candidate checkpoint.

---

# Progression Design

## Current quest-check candidate (v0.26)

Client v0.73.0 / APWorld v0.26.0 is an uninstalled schema-17 candidate. New quests
require a fresh v0.26 seed and fresh native save; schema-16 seeds retain prior
behavior. Current counts are Normal 125 / Hard 183 / Expert 241 / Perfection 277.

- Lobby Access plus Car Battery reaches `Lobby - Car Battery Hand-In`.
- Existing Garage access plus Old Game Data reaches `Game Garage - Old Game Data Hand-In`.
- Both hand-ins may hold progression; their inputs are progression items. Meoo and Maniac are independent useful rewards, never prerequisites for hand-in.
- `Lobby - Plunger Pickup` is Lobby-bound and Stardust-only pending phone/button-route modeling. Plunger is useful; no Vault/cassette gate is inferred.
- `Roots - Star Eater Fed` is Roots-bound and Stardust-only. Its native three earned-star threshold remains intact but is not modeled by AP logic. The logical region alone does not prove gameplay reachability.

Next safe item/location IDs are `187256164` / `187256298`. All AP Stars, generated
Star gates, and final Star Victory remain inactive. See the
[candidate acceptance record](testing/2026-09-16-quest-checks-candidate.md).
Historical design and acceptance notes below describe their original milestones.

Historical gameplay and native mapping evidence from the full discovery playthrough is indexed in `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md`. Use that evidence as a starting point, while treating its former individual-level access design as superseded by this document's Area Access model.

## Full normal-campaign mapping candidate (v0.24)

Client v0.70.0 / APWorld v0.24.0 is experimental and requires manual acceptance with a fresh v0.24 seed and fresh native save. All 22 normal campaign identities are mapped to separate Completion and cumulative Star checks. The exact addressed totals are Normal 121 / Hard 179 / Expert 237 / Perfection 273. Development Caches are retired from new seeds but their historical IDs remain reserved; the pre-v0.26 safe item/location frontiers, including v0.25 character items and inactive Lobby reservations, were `187256161` / `187256294`.

Normal results send Completion and every newly earned enabled cumulative Star tier. A first 2-Star result therefore sends Completion, 1 Star, and 2 Stars when those checks are present; an improvement sends only the remaining unchecked tier. Bee and Devil variants are diagnostic-only and cannot send normal checks or special locations. All 66 AP Stars, generated Star gates, and final Victory remain inactive. The candidate does not claim gameplay verification for every mapped level: broad replay is deferred and unplayed mappings remain manual-testing pending.

The retained Music Lab Point pool replaces 20 Stardust with 10 one-point items, 3 ten-point bundles, and 7 twenty-point large bundles: 180 points total. All three items are progression, at permanent IDs `187256153..187256155`. The existing nine chests require weighted totals of 5/10/20/32/46/64/89/111/140; the final threshold leaves 40 points of slack. Ordinary solver reachability permits progression behind earlier chests without self-locking.

No point milestones or duplicate source checks are added. The five chest cassettes retain their 32/64/89/111/140-point checks; Gradius Remix and Bloody Tears retain the 10/46-point checks. Each physical chest sends only its existing location once.

Only the exact schema-14/point-schema-1 contract enables AP points. In `GameRoom_Hub6`, the existing managed score getter returns zero before synchronization, then the weighted authoritative receipt total capped at 180; temporary disconnects retain the last synchronized total. Reconnect and relaunch rebuild history without double-counting; identity changes clear it. Native campaign/cassette/Garage medals never contribute AP points, and no native medal/save score is written. Recognized v0.22 seeds and non-AP play retain native scoring; malformed v0.23 contracts report incompatibility and remain zero. Other rooms preserve native/developer behavior; compatible AP points override the developer score cheat.

Diagnostic-first investigation rejected the generated chest hook and native detour. Only the room-scoped managed getter and read-only chest metadata remain. Existing display, all nine below/at thresholds, check IDs, disconnect/reconnect, relaunch, and compatibility require [live acceptance](testing/2026-09-09-music-lab-points-acceptance.md); a failed native boundary requires design revision.

## Full Music Lab cassette contract (retained from v0.22)

Each of the 30 Music Lab songs has exactly one cassette item and one idempotent source. Twenty-five sources are earned from successful level results; aliases such as Bee/Devil variants resolve to the same AP location. Quicksand, Flamenco, Ten-Four Good Buddy, Zen, and Wiggle reuse the existing 32/64/89/111/140-point chest locations. An AP receipt requests native `HAVE_IN_BAG`; a native `HAVE_DEPOSITED` state is terminal and is never changed back, so players unlock songs by inserting cassettes normally.

Client v0.69.0 enables this routing only when a fresh seed supplies cassette schema 1, count 30, and exact item/source/reused-location mappings. Any cassette-contract mismatch preserves native cassette behavior. Secret Bunker Devil aliases are cataloged but conservatively inactive in solver reachability until Bunker access is implemented and validated. The 24 newly mapped songs and the I Got Money Bee alias remain manual verification pending.

## Hub model

**Home:** Hub6 / Music Lab phone hub.

Always available from Home:

- Music Lab
- Game Garage

Major AP-routed areas:

- Roots â†’ `GameRoom_Hub2`
- Lobby â†’ `GameRoom_Hub1A` / Hub1B
- Meat Dimension â†’ `GameRoom_Hub4`
- Cell Tower â†’ Hub5A/B
- Tower of Fear â†’ `GameRoom_Hub3`
- Royal Corridor â†’ `GameRoom_Hub7`

`Royal Corridor Access` currently means only the phone-side subregion containing the Level 22 entrance. It does not imply access to Level 21, the Royal Star Eater interaction, or the completed bridge back across the corridor. Those states remain deliberately absent from the AP graph until their native events and requirements are mapped.

During the current Roots-development phase, **Roots Access is forced as the precollected starter**. The other five Area Access items remain randomized.

## General rule

A level should ultimately be reachable only when all relevant layers are satisfied:

```text
area reachable
+ generated AP Star requirement
+ meaningful vanilla/AP item prerequisites
+ unavoidable vanilla story state
```

Meaningful vanilla items should be randomized as real AP progression rather than bypassed by Stars. Physical source interactions become AP locations; receiving the AP item grants the corresponding native item/ability state. Vanilla consumption and story consequences are preserved when practical.

Randomized Star requirements are planned after the meaningful item/story graph is mapped reliably.

## Roots â€” implemented chain

```text
Roots Access
    â†“
Roots traversal baseline
  - first arrival cutscene bypass
  - FirstAreaGate suppressed
  - StarEaterBlockade/Blockade suppressed
    â†“
Gecko source
    â†“ AP location
Roots - Gecko's Weed Killer
    â†“
Weed Killer [randomized AP item]
    â†“ native WEED_KILLER_BAG_ITEM
Vanilla consumes Weed Killer
    â†“
Level 3 becomes enterable
    â†“
Frog + Hippo source inside Level 3
    â†“ AP location
Roots - Level 3 - Plant Pipes Pickup
    â†“
Plant Pipes [randomized AP item]
    â†“ native WEED_KILLER_ABILITY
Level 3 can be completed
    â†“
post-Level-3 / Level 4 progression
```

### Weed Killer

- Vanilla source: Gecko in Roots.
- Native item flag: `WEED_KILLER_BAG_ITEM`.
- Native source/story marker: `ROOTS_HUB_WEED_KILLER_COLLECTED`.
- AP location: `Roots - Gecko's Weed Killer`.
- AP item: `Weed Killer`.
- Vanilla is allowed to consume the native bag item to reveal Level 3.

### Plant Pipes

- Vanilla source: Frog and Hippo inside Level 3.
- Native usable ability: `WEED_KILLER_ABILITY`.
- Native source/story marker: `LEVEL_07_WK_ABILITY_EARNED`.
- AP location: `Roots - Level 3 - Plant Pipes Pickup`.
- AP item: `Plant Pipes`.

The source marker remains vanilla while the actual ability grant is suppressed. Therefore the Frog/Hippo check is reachable before Plant Pipes is owned.

### Intentional partial-level state

Without Plant Pipes, Level 3 is intentionally enterable but not completable. The player can:

1. Enter Level 3 using Weed Killer progression.
2. Reach Frog/Hippo and send the AP check.
3. Use the normal menu to exit back to the hub.
4. Return after Plant Pipes is received from Archipelago.

APWorld logic must never require Plant Pipes to reach its own Frog/Hippo source check.

## Roots â€” confirmed Level 4 to Lift Quest observations

The earlier full-game discovery playthrough recorded this vanilla sequence:

```text
Level 4 (`Level_08`)
    â†“ Hip Glasses awarded immediately before completion
Bucket Minion trade
    â†“ Hip Glasses exchanged for Chicken Bucket
bucket blockade removed
    â†“ King lift conversation
Lift Quest / Level 5 (`Level_09`) becomes available
    â†“ Chicken Bucket used during the level
Combo Bucket ability earned
    â†“ Level 5 completed
lobby progression begins
```

Confirmed identifiers and lifecycle, revalidated in the focused 2026-08-21 gameplay trace:

- Level 4 is internal `Level_08`.
- The Hip Glasses inventory flag is `HIP_GLASSES_BAG_ITEM`.
- The Level 4 source marker is `LEVEL_08_GLASSES_COLLECTED`.
- The Bucket Minion trade marker is `ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES`.
- The trade also sets `ROOTS_HUB_BUCKET_MINION_DIALOGUE_PROGRESSION` and `ROOTS_HUB_BUCKET_MINION_BLOCKADE_REMOVED`.
- The trade sets `CHICKEN_BUCKET_BAG_ITEM` true and consumes `HIP_GLASSES_BAG_ITEM` back to false.
- The King conversation sets `ROOTS_HUB_KING_LIFT_CHAT_WITNESSED`.
- Lift Quest / Level 5 is internal `Level_09`.
- Its hub entrance was identified as `Placeholder_Level_09_Entrance`.
- The vanilla entrance includes `KingLiftChatWitnessed_Condition`.
- Using the Chicken Bucket sets `COMBO_BUCKET_ABILITY` true and consumes `CHICKEN_BUCKET_BAG_ITEM` back to false.
- The ability-award marker is `LEVEL_09_COMBO_ABILITY_EARNED`.
- Level completion sets `LEVEL_09_COMPLETED`.
- The forced post-level Lobby arrival sets `OVERALL_PROGRESS_REACHED_LOBBY_HUB` and transitions to `GameRoom_Hub1A`.

These are gameplay observations and recovered log conclusions from the project conversation **Archipelago Game Implementation**. That conversation contains the logged discovery playthrough for the wider game and should be treated as a primary historical evidence source when repository documentation is incomplete.

### Design-pivot warning

The historical playthrough predates the switch from individual level-unlock items to the current **Area Access** model. Its observed vanilla events, scene names, native identifiers, and ordering remain useful evidence. Its former Archipelago design decisions do not automatically remain valid.

In particular, the old implementation used a separate `Level 5 Access` item to gate only `Placeholder_Level_09_Entrance` while preserving the Hip Glasses trade and `KingLiftChatWitnessed_Condition`. That per-level access design is superseded and must not be restored. The approved future model randomizes Hip Glasses at the Level 4 source, keeps the normal Bucket Minion trade as the Chicken Bucket source, randomizes Chicken Bucket, and preserves Combo Bucket as the vanilla consequence.

The approved source/item/trade model is implemented in APWorld v0.17 and client v0.67.60, pending fresh-save gameplay acceptance. `Hip Glasses` is item `187256119`, `Chicken Bucket` is item `187256120`, the Level 4 source is location `187256180`, and the Bucket Minion trade is location `187256181`.

The client suppresses only the two native inventory grants. It retains Level 4's source marker and all normal trade, blockade, King-chat, Lift Quest, completion, Combo Bucket, and Lobby-transition behavior. Received-item history restores an owned item only while its durable consumed marker is false; reconnecting after the trade or conversion must not resurrect consumed inventory. Incompatible seeds and unsynchronized saves fail closed to vanilla behavior.


## Character quest item candidate (2026-09-15)

Client v0.71.0 / APWorld v0.25.0, schema 16, adds Old Game Data and Car Battery
as useful randomized inventory items, replacing two Stardust. Their existing
5-point and 20-point Music Lab chest checks are reused; character rewards remain
vanilla and are not new checks. Normal hand-ins unlock Maniac in Game Garage and
Meoo in the Lobby. Received items are queued from complete authenticated history,
then reconciled on the Unity thread only against the proven selected save.
Maniac's native saved character-unlock predicate and Meoo's unlock flag prevent
consumed items being re-granted. Unknown native state defers the grant.

A newly generated v0.25 seed is required to test these items. Existing v0.24 seeds
retain vanilla item behavior. Validate chest grants suppressed with checks intact,
received inventory, both hand-ins, reload/reconnect/offline and new-save isolation
before gameplay acceptance. No live deployment or new-seed replacement is implied.

### Cassette completion prerequisites (2026-09-16 repair)

Every normal-level cassette award inherits its campaign level's area and item
requirements, plus any cassette-route-specific restrictions. This prevents an
ability from being placed on a completion reward behind that same ability.
Alternative native award routes remain alternatives; special variants retain
separate prerequisites. The Level 3 in-level Plant Pipes pickup is intentionally
separate and still requires Weed Killer without requiring Plant Pipes.

## v0.74.2 starting-item repair candidate

Hip Glasses and Chicken Bucket now retry on the Unity update against the verified selected save, even without a native progression request. Grant suppression is scoped to the selected-save identity, retaining native consumption guards. Flag reflection metadata is cached; native values are read fresh. The same update retries pending Weed Killer delivery. Runtime regression covers initial delivery, save changes, duplicate prevention and consumed items. Gameplay acceptance pending.

## Temporary Star Eater threshold test — v0.74.2

Developer.RoyalStarRequirement defaults to native40; existing QualityOfLife.BunkerStarRequirement controls the Bunker. The user requested25 for both on the retained batch seed to avoid star grinding. The existing proximity-scoped override now selects only the audited Royal or Bunker root, restores native patches on target/scene change, and leaves earned stars, level scores and source flags untouched. The player must perform the native feed interaction; only its resulting native flag sends the AP check. This test does not validate vanilla40/66 thresholds or AP Star progression. Restore Royal40/Bunker66 after the grouped run.
