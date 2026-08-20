# Progression Design

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

## Roots — next planned chain

The next discovery/implementation milestone is expected to be:

```text
Level 4
    ↓
glasses pickup near completion
    ↓ AP source check
Glasses [exact game name/native flag still to be confirmed]
    ↓
Minim blocking Level 5 / Life Quest
    ↓ glasses trade interaction = AP check
Chicken Bucket [planned randomized AP item]
```

The glasses' exact in-game/native name and the Minim trade / Chicken Bucket progression flags are not yet committed. Do not allocate permanent IDs until the source mapping is confirmed and implementation is ready.
