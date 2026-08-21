# Progression Design

Historical gameplay and native mapping evidence from the full discovery playthrough is indexed in `docs/HISTORICAL_GAMEPLAY_EVIDENCE.md`. Use that evidence as a starting point, while treating its former individual-level access design as superseded by this document's Area Access model.

## Hub model

**Home:** Hub6 / Music Lab phone hub.

Always available from Home:

- Music Lab
- Game Garage

Major AP-routed areas:

- Roots → `GameRoom_Hub2`
- Lobby → `GameRoom_Hub1A` / Hub1B
- Meat Dimension → `GameRoom_Hub4`
- Cell Tower → Hub5A/B
- Tower of Fear → `GameRoom_Hub3`
- Royal Corridor → `GameRoom_Hub7`

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

## Roots — implemented chain

```text
Roots Access
    ↓
Roots traversal baseline
  - first arrival cutscene bypass
  - FirstAreaGate suppressed
  - StarEaterBlockade/Blockade suppressed
    ↓
Gecko source
    ↓ AP location
Roots - Gecko's Weed Killer
    ↓
Weed Killer [randomized AP item]
    ↓ native WEED_KILLER_BAG_ITEM
Vanilla consumes Weed Killer
    ↓
Level 3 becomes enterable
    ↓
Frog + Hippo source inside Level 3
    ↓ AP location
Roots - Level 3 - Frog and Hippo
    ↓
Plant Pipes [randomized AP item]
    ↓ native WEED_KILLER_ABILITY
Level 3 can be completed
    ↓
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
- AP location: `Roots - Level 3 - Frog and Hippo`.
- AP item: `Plant Pipes`.

The source marker remains vanilla while the actual ability grant is suppressed. Therefore the Frog/Hippo check is reachable before Plant Pipes is owned.

### Intentional partial-level state

Without Plant Pipes, Level 3 is intentionally enterable but not completable. The player can:

1. Enter Level 3 using Weed Killer progression.
2. Reach Frog/Hippo and send the AP check.
3. Use the normal menu to exit back to the hub.
4. Return after Plant Pipes is received from Archipelago.

APWorld logic must never require Plant Pipes to reach its own Frog/Hippo source check.

## Roots — confirmed Level 4 to Lift Quest observations

The earlier full-game discovery playthrough recorded this vanilla sequence:

```text
Level 4 (`Level_08`)
    ↓ Hip Glasses awarded immediately before completion
Bucket Minion trade
    ↓ Hip Glasses exchanged for Chicken Bucket
bucket blockade removed
    ↓ King lift conversation
Lift Quest / Level 5 (`Level_09`) becomes available
    ↓ Chicken Bucket used during the level
Combo Bucket ability earned
    ↓ Level 5 completed
lobby progression begins
```

Confirmed historical identifiers and objects:

- Level 4 is internal `Level_08`.
- Lift Quest / Level 5 is internal `Level_09`.
- Its hub entrance was identified as `Placeholder_Level_09_Entrance`.
- The vanilla entrance includes `KingLiftChatWitnessed_Condition`.
- Using the Chicken Bucket produces `COMBO_BUCKET_ABILITY`.
- The ability-award marker is `LEVEL_09_COMBO_ABILITY_EARNED`.
- Level completion records `LEVEL_09`.

These are gameplay observations and recovered log conclusions from the project conversation **Archipelago Game Implementation**. That conversation contains the logged discovery playthrough for the wider game and should be treated as a primary historical evidence source when repository documentation is incomplete.

### Design-pivot warning

The historical playthrough predates the switch from individual level-unlock items to the current **Area Access** model. Its observed vanilla events, scene names, native identifiers, and ordering remain useful evidence. Its former Archipelago design decisions do not automatically remain valid.

In particular, the old implementation used a separate `Level 5 Access` item to gate only `Placeholder_Level_09_Entrance` while preserving the Hip Glasses trade and `KingLiftChatWitnessed_Condition`. That per-level access design is superseded and must not be restored without a new design decision. Current work must reinterpret the confirmed vanilla chain under Roots Access, meaningful randomized items, generated Star requirements, and unavoidable vanilla story state.

The precise AP checks/items for Hip Glasses, the Bucket Minion trade, and Chicken Bucket still require reconciliation with the Area Access design before implementation. Do not allocate permanent IDs, guess an unrecorded native flag, or treat an older per-level gate as current intent.
