# Hip Glasses and Chicken Bucket Randomization Design

**Status:** Approved in chat; awaiting written-spec review
**Date:** 2026-08-21

## 1. Purpose

Randomize the Roots Hip Glasses and Chicken Bucket chain while preserving the game's physical Level 4 reward, Bucket Minion trade, Lift Quest use, Combo Bucket conversion, and Lobby arrival sequence.

This design supersedes the historical per-level `Level 5 Access` experiment. Area Access remains authoritative for travel. No individual Level Access item returns.

## 2. Verified native lifecycle

The focused 2026-08-21 gameplay trace confirmed this exact sequence:

```text
Level 4 automatic reward
→ HIP_GLASSES_BAG_ITEM = true
→ LEVEL_08_GLASSES_COLLECTED = true

Bucket Minion trade
→ CHICKEN_BUCKET_BAG_ITEM = true
→ HIP_GLASSES_BAG_ITEM = false
→ ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES = true
→ ROOTS_HUB_BUCKET_MINION_DIALOGUE_PROGRESSION = true
→ ROOTS_HUB_BUCKET_MINION_BLOCKADE_REMOVED = true
→ ROOTS_HUB_KING_LIFT_CHAT_WITNESSED = true

Lift Quest
→ COMBO_BUCKET_ABILITY = true
→ CHICKEN_BUCKET_BAG_ITEM = false
→ LEVEL_09_COMBO_ABILITY_EARNED = true
→ LEVEL_09_COMPLETED = true
→ OVERALL_PROGRESS_REACHED_LOBBY_HUB = true
→ transition to GameRoom_Hub1A
```

All inventory and source transitions above were observed from false to true or, for consumed items, true to false. Production code must use these identifiers exactly and must not infer additional flags.

## 3. Archipelago model

The APWorld adds two randomized items:

- `Hip Glasses`
- `Chicken Bucket`

It adds two source locations:

- `Roots - Level 4 - Hip Glasses`
- `Roots - Bucket Minion Trade`

The locations do not guarantee their vanilla items. Completing the Level 4 source may send any multiworld item. Completing the Bucket Minion trade may also send any multiworld item.

`Combo Bucket` is not a randomized network item. It remains the native consequence of using Chicken Bucket during Lift Quest.

Permanent item and location IDs are allocated only when implementation begins. The ID registry remains authoritative, and no existing or reserved ID may be reused.

## 4. Client source suppression and checks

### Level 4 source

When the compatible slot-data feature is active in `GameRoom_08`:

1. allow `LEVEL_08_GLASSES_COLLECTED = true` to remain native;
2. send `Roots - Level 4 - Hip Glasses` idempotently from that durable marker; and
3. suppress only the accompanying vanilla request that sets `HIP_GLASSES_BAG_ITEM = true`.

Level completion, performance checks, cutscenes, and every unrelated Level 4 flag remain unchanged.

### Bucket Minion trade

The player must possess the AP-delivered `HIP_GLASSES_BAG_ITEM` and perform the normal interaction. The client does not force the conversation or open the route automatically.

During the compatible randomized flow:

1. allow the game to consume `HIP_GLASSES_BAG_ITEM`;
2. allow `ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES` and the dialogue, blockade, and King-chat flags to remain native;
3. send `Roots - Bucket Minion Trade` idempotently from `ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES`; and
4. suppress only the vanilla request that sets `CHICKEN_BUCKET_BAG_ITEM = true`.

The normal physical blockade and King conversation remain authoritative. AP never sets those downstream story flags merely because Chicken Bucket was received.

### Lift Quest conversion

The player must receive `Chicken Bucket` from AP and use it normally during Lift Quest. The client allows the game to:

- consume `CHICKEN_BUCKET_BAG_ITEM`;
- grant `COMBO_BUCKET_ABILITY`;
- set `LEVEL_09_COMBO_ABILITY_EARNED`;
- complete Level 09; and
- trigger the native Lobby arrival.

No Combo Bucket grant is synthesized by AP.

## 5. AP item delivery and reconciliation

Received-item history is the authority for whether the slot owns Hip Glasses or Chicken Bucket. Native lifecycle markers are the authority for whether a received item has already been consumed or converted.

On initial synchronization and reconnection:

```text
Hip Glasses received
AND ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES is false
→ ensure HIP_GLASSES_BAG_ITEM is true

Hip Glasses received
AND ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES is true
→ do not restore HIP_GLASSES_BAG_ITEM

Chicken Bucket received
AND LEVEL_09_COMBO_ABILITY_EARNED is false
→ ensure CHICKEN_BUCKET_BAG_ITEM is true

Chicken Bucket received
AND LEVEL_09_COMBO_ABILITY_EARNED is true
→ do not restore CHICKEN_BUCKET_BAG_ITEM
```

Native consumed/converted state always wins over replaying received-item history. Reconciliation is idempotent: repeated connection, room transition, or save-load processing cannot duplicate an item, check, trade, or ability.

Before synchronization completes, randomized grants and source checks fail closed. A temporary disconnect retains the last synchronized inventory state and queues new checks for reconnection through the existing client behavior.

Outside a compatible AP-bound save, the complete vanilla chain remains unchanged.

## 6. APWorld logic

The Level 4 Hip Glasses source requires the current approved Level 4 chain, including:

- Roots Access;
- Weed Killer progression needed to open Level 3;
- Plant Pipes; and
- any generated AP Star requirement eventually assigned to Level 4.

The Bucket Minion trade requires:

- access to the Roots hub trade interaction;
- the Level 4 story state needed to expose the interaction; and
- `Hip Glasses`.

Lift Quest completion requires:

- the native Bucket Minion/King route to be available;
- `Chicken Bucket`;
- any generated AP Star requirement eventually assigned to Level 5; and
- the current Area Access rules.

The solver must reject placements that put Hip Glasses behind its own trade or otherwise make either new progression item unobtainable.

## 7. Combo Bucket performance role

Combo Bucket boosts score and objective performance in every later level and applies a 5× effect in Music Lab. Because Area Access can expose later content before Lift Quest is completed, the solver cannot assume every post-Level-5 performance check is possible without it.

The client exposes the durable native state `COMBO_BUCKET_ABILITY` to local behavior. The APWorld may represent completion of the conversion as a non-network progression event for logic purposes. That event is not shuffled, displayed as a received item, or added to the multiworld item pool.

Initially:

- basic campaign clears do not require Combo Bucket merely for score assistance;
- no higher performance check requires Combo Bucket without targeted gameplay evidence;
- validated score or objective checks that are impossible or unreasonable without the boost may require the internal Combo Bucket event; and
- difficulty logic must document each such requirement rather than applying one blanket post-Level-5 rule.

This preserves early area routing while preventing the solver from claiming an unproven high-score route.

Testers are specifically invited to investigate later content without Combo Bucket. A useful report identifies the exact campaign level, objective, Music Lab cassette, or Game Garage song; native play difficulty; AP performance tier when applicable; score, stars, medal, and missed objective; player count; relevant abilities; and whether `COMBO_BUCKET_ABILITY` was absent. A successful result without Combo Bucket proves that result is possible under the reported conditions. An unsuccessful attempt is evidence only, not proof of impossibility; reports should include the number of attempts and the best observed result. Video and a focused log excerpt are preferred when practical.

## 8. Compatibility and slot data

The APWorld exports explicit feature metadata and the exact item/location names required by the client. The client enables suppression only for a recognized compatible implementation version and feature flag. Older seeds retain their existing behavior.

The implementation-version prefix that preserves current v0.15/v0.16 client compatibility must remain intact. This feature is additive and must not disable Weed Killer, Plant Pipes, cartridges, Music Lab checks, Area Access, or the starter grant.

## 9. Testing and acceptance

Automated tests cover:

- both new item and location definitions and unique IDs;
- item-pool/location-capacity balance;
- Level 4 source reachability with Plant Pipes;
- Bucket Minion trade requiring Hip Glasses;
- Lift Quest completion requiring Chicken Bucket;
- no self-locking placement in representative generated seeds;
- compatible slot-data activation and incompatible-seed fail-closed behavior;
- suppression of only the two native item grants;
- idempotent AP delivery;
- consumed Hip Glasses not returning after trade and reconnect;
- consumed Chicken Bucket not returning after conversion and reconnect; and
- existing Roots, Garage, Music Lab, and Area Access behavior remaining enabled.

Gameplay acceptance uses a fresh compatible seed and save to verify:

1. Level 4 sends the Hip Glasses source check without granting the held item.
2. AP-delivered Hip Glasses enables the normal Bucket Minion interaction.
3. The trade consumes Hip Glasses, sends its check, preserves dialogue/blockade/King behavior, and does not grant Chicken Bucket locally.
4. AP-delivered Chicken Bucket enables normal Lift Quest use.
5. Lift Quest consumes Chicken Bucket, grants Combo Bucket, completes normally, and triggers the Lobby cutscene.
6. Save/reload and disconnect/reconnect at each held and consumed state do not duplicate or restore consumed items.
7. Vanilla play outside an AP-bound save is unchanged.
8. Representative later score and objective checks are attempted without Combo Bucket and reported with enough detail to decide whether any individual solver requirement is justified.

No merge or push occurs until gameplay acceptance passes and the project owner approves integration.
