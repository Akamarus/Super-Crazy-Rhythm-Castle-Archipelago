# Historical Gameplay Evidence

**Sources:** Project conversations **Archipelago Game Implementation** and **Continue Implementation Steps**<br>
**Coverage:** Full main-game discovery playthrough, postgame systems, Music Lab, Game Garage, and iterative client diagnostics<br>
**Purpose:** Preserve gameplay and log evidence that was not fully promoted into the repository during the original development conversation

## How to use this document

The source conversation predates the project's pivot from individual `Level N Access` items to the current **Area Access** model. Evidence from that conversation must therefore be separated into four categories:

1. **Observed gameplay:** what happened during the recorded playthrough.
2. **Log-confirmed evidence:** native identifiers, flags, scene names, variants, and hierarchy paths recovered from an uploaded log or runtime scan.
3. **Historical implementation:** behavior tested in an older prototype build.
4. **Superseded design:** conclusions based on individual level-access items or other assumptions that are no longer current requirements.

Observed gameplay and log-confirmed evidence remain useful unless contradicted by newer evidence. Historical implementation proves that a technique was tested, not that it belongs in the current design. Superseded design must not be restored implicitly.

The conversations are the primary historical record and include uploaded logs throughout the playthrough and subsequent implementation. This document is a durable index of their recovered conclusions, not a replacement for the raw logs. Where a native identifier was not recovered, the entry remains explicitly unresolved.

The second conversation continued from the full-game playthrough and contains later APWorld/client decisions, including Music Lab reward-chest integration, Phone Hub routing, the Area Access pivot, generated Star requirements, and the first Roots item-randomization milestones. When the two conversations differ, the later explicitly approved design takes precedence over an earlier prototype recommendation.

## Current design lens

Current progression should be interpreted as:

```text
Area Access
+ generated AP Star requirement
+ meaningful randomized item prerequisites
+ unavoidable vanilla story state
```

Consequences for historical evidence:

- Old `Level 1 Access` through `Level 22 Access` permissions are superseded design.
- A historical level-door hierarchy remains useful technical evidence.
- A vanilla quest item or ability may become an AP item, but that requires a current design decision and confirmed receive-side native behavior.
- A story flag is not automatically an AP location.
- A historical prototype check or item does not reserve a permanent AP ID.
- Nothing in this document authorizes allocating IDs, bypassing story sequences, or reintroducing per-level access items.

## Later architecture decisions from Continue Implementation Steps

### Hub6 home and Area Access

The approved target structure is:

```text
new randomizer run
→ intro skipped
→ spawn in Hub6 / Music Lab phone hub
→ Music Lab and Game Garage always available
→ exactly one starter-safe major area available
→ remaining major areas unlocked by Area Access items
```

Canonical major-area items are Roots Access, Lobby Access, Meat Dimension Access, Cell Tower Access, Tower of Fear Access, and Royal Corridor Access. The Secret Bunker should use its meaningful vanilla Bunker Keycard instead of a generic area item unless a later design explicitly changes that decision.

The source conversation first experimented with Roots as an unconditional start, then approved Hub6 as the home with one generated/precollected starting Area Access item. During the current Roots-development phase, the APWorld deliberately forces Roots Access as that starter. This is a development constraint, not a reversal of the eventual randomized-starter design.

The Phone Booth system is intended to act as a global unlocked-destination network. An AP-unlocked destination should not require the player to visit its vanilla phone first. Music Lab and Game Garage remain usable from the beginning, but their internal checks still require the appropriate cassettes, cartridges, performance, or Music Lab point total.

Runtime discovery confirmed six Hub6 phone roots: `BoxDoor_Roots`, `BoxDoor_Lobby`, `BoxDoor_Meat`, `BoxDoor_Prison`, `BoxDoor_Madness`, and `BoxDoor_Royal`. Each uses a `PhoneBox`/`Switch` structure with an `OnTrigger` leave-room reactor. Disabling the visible trigger alone was insufficient, so the tested client also enforced permission at the `TransitionToGameRoomRequest` boundary for departures from Hub6. The Page Up/Page Down controls used during discovery remain development-only.

Roots first-arrival testing confirmed that setting `ROOTS_HUB_INTRO_WITNESSED` immediately before an AP-authorized Roots transition avoids the off-screen first-arrival movement lock without advancing Gecko, Weed Killer, Star Eater, or later Roots quest state.

### Generated Star requirements

The approved target is to generate level Star requirements from the seed's actual logical routing depth rather than fixed vanilla area order. Generation should choose the starter and area routing, determine logical depth and reachable checks, assign progressively higher Star bands, and verify that enough Stars exist before every gate before placing remaining progression.

Every possible starter area must provide at least one immediately playable rhythm level and a healthy Sphere 0. The conversation recommended roughly three to five initially reachable checks instead of accepting a technically valid but fragile one-check opening.

Star requirements remain an additional layer:

```text
area reachable
+ generated AP Stars
+ meaningful randomized item/ability
+ unavoidable vanilla story state
```

The old door-blocker techniques may enforce generated Star thresholds, but player-facing `Level N Access` items must not return.

### Meaningful-item classification

The second conversation explicitly divided vanilla prerequisites into:

- **Meaningful item or ability gates:** AP-randomization candidates such as Weed Killer, Plant Pipes, Hypno Pan, Violance, Bunker Keycard, cassettes, and cartridges.
- **Story bookkeeping:** conversations, cutscenes, consumed-item consequences, opened gates, and similar internal state that should normally remain vanilla.
- **Generated Star gates:** the randomizer's variable level-access layer.

Physical reward sources and meaningful trades may become AP locations. Receiving the corresponding AP item should grant the real native inventory/ability state, after which vanilla consumption and environmental consequences should proceed normally where practical.

## Main campaign mapping

| Displayed progression | Internal level / room | Area | Observed or log-confirmed notes |
| --- | --- | --- | --- |
| Level 1 | `Level_05` | Roots | Completion and star-result mapping were tested during early client development. Earlier per-level gate experiments are superseded. |
| Level 2 | `Level_06` | Roots | Completion was repeatedly logged. Shipped assets map `Level_06_Data.variants[*].songCassettes[0]` to `110`, which is `ePlayableSong.I_GOT_MONEY`; the one-cassette AP pilot uses this first default award. A Bee Mode variant later reuses this level as Nectar Party and has no stars. |
| Level 3 | `Level_07` | Roots | Weed Killer opens the entrance. Frog and Hippo provide the Plant Pipes ability during the level. |
| Level 4 | `Level_08` / `GameRoom_08` | Roots | Hip Glasses are awarded immediately before completion. The entrance uses `Level_08_Entrance`; the vine hierarchy includes `BossWeeds-tofb`. |
| Level 5 — Lift Quest | `Level_09` | Roots | Hip Glasses are traded to the Bucket Minion for the Chicken Bucket. The trade removes the bucket blockade and precedes the King lift conversation. The entrance is `Placeholder_Level_09_Entrance` with `KingLiftChatWitnessed_Condition`. |
| Level 6 — Boring Room | `Level_02` | Lobby | Reached through the recurring manual-button passage. The normal `Level02Door` is separate from `Level02Door_DevilMode`. |
| Level 7 — Demolition Training | `Level_19` | Lobby | Awards the Demolition Certificate immediately before completion. Normal entrance: `TrainingCentre: Levels 19&21/Level19/Level19Door`. |
| Level 8 — Minim Tower | `Level_11` / `GameRoom_11` | Lobby | No item or ability was observed on completion. A separate Devil Mode entrance exists. |
| Level 9 — School Trip | `Level_20`; `GameRoom_20A/B/C` | Lobby | Three-room level. No item reward observed. Completion clears a hand blocker. |
| Level 10 — The Vault | `Level_01` / `GameRoom_01` | Lobby | No item reward observed. Normal entrance identified as `GameRoom_Hub1B_Logic/Objects/LevelEntranceDoor_01`. |
| Level 11 — Act 1: Flavor | `Level_12` / `GameRoom_12` | Meat Dimension | Unlocked through the first music-delivery quest. Completion opens the next hub gate. A separate Bee Mode door exists. |
| Level 12 — Act 2: Sauce and Spice | `Level_15` / `GameRoom_15` | Meat Dimension | Requires the Hypno Pan for recurring crowd traversal plus the music, cat, and bouncer quest. No completion item observed. |
| Level 13 — Act 3: Montage | `Level_22` / `GameRoom_22` | Meat Dimension | Requires the Hypno Pan both to reach and complete the level. Unlock also requires music and returning Scruffy. |
| Level 14 — Act 4: Habanero | `Level_23` / `GameRoom_23` | Meat Dimension | Requires the Hypno Pan. Fish Tears are collected inside the level before completion. |
| Level 15 — Central Mainframe | `Level_16` / `GameRoom_16` | Cell Tower | Available after the Cell Tower introduction. Completion unlocks the forward gate into Hub5B. |
| Level 16 — Thief Prince | `Level_24` / `GameRoom_24` | Cell Tower | No item, ability, or immediate progression flag was observed on completion. |
| Level 17 — Cold Storage | `Level_21`; `GameRoom_21A/B` | Lobby/Cell Tower return | Multiple rooms; requires the Hypno Pan. Awards Violance, represented by `VIOLIN_ABILITY`. |
| Level 18 — The Darkness | `Level_03` / `GameRoom_03` | Tower of Fear | Completion drops the Darkness branch shield. Minim's Heart is collected in this branch. |
| Level 19 — Escape | `Level_13` / `GameRoom_13` | Tower of Fear | Completion drops the Complexity branch shield. Minim's Eye is collected in this branch. |
| Level 20 — Loneliness | `Level_25` / `GameRoom_25` | Tower of Fear | Completion drops the Loneliness branch shield. The three branch completions feed a separate hub totem-completion sequence. |
| Level 21 — Locker Room | `Level_14` / `GameRoom_14` | Royal Corridor | Requires Violance and Weed Killer during play. A separate `LevelEntranceDoor_14_DevilMode` exists. |
| Level 22 — King Ferdinand I | `Level_28` | Royal Corridor | Boss completion sets `OVERALL_PROGRESS_BEAT_KING_ONE`, unlocks King Ferdinand as a character, and awards the Bunker Keycard. |

Fresh-save testing on 2026-08-21 completed `Level_28` for one Star at 102 points without any abilities. This proves that neither Note Pad nor Data Stick is required for basic Level 22 completion; their possible effect is limited to results above one Star until further testing. Client v0.67.60 captured the result but did not send an AP check because `Level_28` was missing from its level-name mapping.

The same fresh-save session confirmed that orange construction barriers remain active inside the Music Lab and prevent physical access to substantial groups of cassette machines. Registering all 30 cassette medal sets as AP locations does not make them reachable; the native barrier unlock conditions and affected machine groups still require targeted mapping before final solver logic.

### Roots item chain

Log-confirmed Roots evidence already promoted into `docs/PROGRESSION.md` includes:

- Weed Killer bag item: `WEED_KILLER_BAG_ITEM`.
- Weed Killer source/story marker: `ROOTS_HUB_WEED_KILLER_COLLECTED`.
- Plant Pipes usable ability: `WEED_KILLER_ABILITY`.
- Plant Pipes source/story marker: `LEVEL_07_WK_ABILITY_EARNED`.
- Level 4: `Level_08`.
- Lift Quest: `Level_09`.
- Hip Glasses held item: `HIP_GLASSES_BAG_ITEM`.
- Hip Glasses source marker: `LEVEL_08_GLASSES_COLLECTED`.
- Bucket Minion trade marker: `ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES`.
- Bucket Minion dialogue marker: `ROOTS_HUB_BUCKET_MINION_DIALOGUE_PROGRESSION`.
- Bucket Minion blockade marker: `ROOTS_HUB_BUCKET_MINION_BLOCKADE_REMOVED`.
- King lift conversation marker: `ROOTS_HUB_KING_LIFT_CHAT_WITNESSED`.
- Chicken Bucket held item: `CHICKEN_BUCKET_BAG_ITEM`.
- Combo Bucket ability: `COMBO_BUCKET_ABILITY`.
- Combo Bucket award marker: `LEVEL_09_COMBO_ABILITY_EARNED`.
- Lift Quest completion marker: `LEVEL_09_COMPLETED`.
- Lobby arrival marker: `OVERALL_PROGRESS_REACHED_LOBBY_HUB`.

The 2026-08-21 focused trace confirmed each flag above transitioning from false to true, plus `HIP_GLASSES_BAG_ITEM` transitioning true to false at the trade and `CHICKEN_BUCKET_BAG_ITEM` transitioning true to false when converted into Combo Bucket. The game then transitioned directly to `GameRoom_Hub1A` for the Lobby arrival cutscene.

The confirmed order is:

```text
complete Level 4
→ receive Hip Glasses near its end
→ trade Hip Glasses to Bucket Minion
→ receive Chicken Bucket
→ bucket blockade removed
→ King lift conversation
→ Lift Quest becomes available
→ use Chicken Bucket during Lift Quest
→ gain COMBO_BUCKET_ABILITY
→ complete Level_09
→ enter Lobby progression
```

## Lobby branching and story state

After Lift Quest, the playthrough recorded a deliberately branching lobby rather than a linear level chain:

- Level 5 completion establishes the Lobby.
- The missing-button sequence funnels the player to the phone booth and back.
- `LOBBY_HUB_MANUAL_BUTTON_KING_WITNESSED` records the missing-button King scene.
- The phone-booth transition leads to `GameRoom_Hub6`.
- `CLEAN_HUB_LOBBY_BUTTON_RETURNED` records the button's return.
- The Manual Button remains a recurring traversal interaction and must not be converted into a permanently open route.
- `LOBBY_HUB_ROOM_02_KING_WITNESSED` corresponds to the Boring Room route conversation.
- Completing Boring Room clears one hand, then the Important Letters → Bean Trumpet → mail-delivery story clears the next route.
- `LOBBY_HUB_HEIST_KING_WITNESSED` is associated with the later King scene that enables Demolition Training.
- Demolition Training awards `LOBBY_HUB_TOOLS_CERTIFICATE`.
- Minim Tower, School Trip, and The Vault form parallel objectives associated with the three-hand blockade.
- School Trip completion sets `LOBBY_HUB_HEIST_HEIST_BLOCKER_CLEARED`.
- Approaching the final Vault-related blocker sets `LOBBY_HUB_HEIST_VAULT_BLOCKER_CLEARED`; it is proximity-triggered rather than immediate on Vault completion.

The Plunger/Star Eater route into the Meat Dimension is separate from the three-hand route. The later King scene starts the Fish Tears quest and records `LOBBY_HUB_FISH_TEARS_KING_WITNESSED`. These paths converge without being the same prerequisite.

Fresh-save Area Access testing on 2026-08-21 confirmed that the Royal Corridor phone arrives on the Level 22 side of the broken bridge. From that spawn the player could use the Level 22 entrance but could not get close enough to the Royal Star Eater to feed Stars and complete the bridge back toward Level 21. Treat Royal phone-side Level 22 access, the Star Eater interaction, and the Level 21 side as distinct routing states in AP logic.

## Meat Dimension evidence

The recorded vanilla chain includes:

- Wooden Spoon pickup.
- Saw Disc pickup and repeated music-selection/delivery quests.
- Frying Pan pickup after Act 1.
- Kiss/Smooch Cartridge pickup.
- Frog and Hippo combine the Wooden Spoon and Frying Pan into the Hypno Pan, represented by `PIED_PIPER_ABILITY`.
- The Hypno Pan moves meat dogs, crowds, pets, and mice and is required inside several levels.
- Act 2 additionally requires returning a cat to its bouncer.
- Act 3 additionally requires returning Scruffy.
- Act 4 requires the music quest and the meat-mouse revolution.

Log-confirmed flags include:

- `MEAT_HUB_ACT_ONE_MUSIC_DONE`
- `MEAT_HUB_GATE_OPENED`
- `FRYING_PAN_BAG_ITEM`
- `MEAT_HUB_FRYING_PAN_COLLECTED`
- `MEAT_HUB_ACT_THREE_MUSIC_DONE`
- `MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE`
- `MEAT_HUB_ACT_FOUR_MUSIC_DONE`
- `MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED`
- `MEAT_HUB_ACT_FOUR_BOUNCER_REQUIREMENT_DONE`
- `LOBBY_HUB_FISH_TEARS_TASK_ITEM`
- `LEVEL_23_FISH_TEARS_COLLECTED`

Delivering Fish Tears consumes the task item, sets `LOBBY_HUB_FISH_TEARS_DEPOSITED`, and then sets `LOBBY_HUB_FISH_TEARS_BLOCKER_CLEARED`, opening the route to Cell Tower.

## Cell Tower and Super Nectar evidence

- Cell Tower begins in `GameRoom_Hub5A`; its introduction sets `PRISON_HUB_LOWER_SHAFT_INTRO_WITNESSED`.
- Central Mainframe completion opens the forward route to `GameRoom_Hub5B`.
- The Cells introduction sets `PRISON_HUB_CELLS_INTRO_WITNESSED`.
- Speaking to the bees sets `PRISON_HUB_SPOKEN_TO_BEES`, grants Bizzle and Clive, and starts the two-drop Super Nectar quest.
- Nectar Party is `Level_06 / LevelVariant_BeeMode` and awards `PRISON_HUB_SUPER_NECTAR_BUBBLES_BAG_ITEM`.
- Act 1B: Nectar is `Level_12 / LevelVariant_BeeMode` and awards `PRISON_HUB_SUPER_NECTAR_RECIPES_BAG_ITEM`.
- Returning both drops consumes the two items and sets `PRISON_HUB_BEES_ESCAPED`.
- The later heist sequence sets `PRISON_HUB_HEIST_CUTSCENE_WITNESSED`.
- Bee Mode levels have no stars. They must never generate star-tier locations merely because their internal base level supports stars.

## Tower of Fear and Royal Corridor evidence

Tower of Fear is `GameRoom_Hub3` and has three branches. Each branch level drops a shield, but clearing all three levels is not itself the final hub event. A separate `TotemCompletion` sequence performs area and hub completion.

Recovered shield flags:

- The Darkness: `MADNESS_HUB_DARKNESS_SHIELD_DROPPED`.
- Escape/Complexity: `MADNESS_HUB_COMPLEXITY_SHIELD_DROPPED`.
- Loneliness: a corresponding Loneliness shield-drop flag was observed in the historical scan, but its exact spelling should be rechecked before implementation.

After the Tower of Fear route, entry to Royal Corridor uses `GameRoom_Hub7` and sets `KING_CORRIDOR_FIRST_TIME_INTRO_WITNESSED`.

Royal Corridor progression includes:

- Locker Room completion.
- Star Eater dialogue and feeding: `KING_CORRIDOR_STAR_EATER_DIALOGUE_INTRODUCED` then `KING_CORRIDOR_STAR_EATER_FED`.
- `KING_CORRIDOR_HANGOUT_ROOM_DISCOVERED` after crossing the resulting route.
- The fridge/ice discovery and King cutscene before King Ferdinand I.
- Boss completion: `OVERALL_PROGRESS_BEAT_KING_ONE`.
- Bunker Keycard: `LIFT_KEY_BAG_ITEM` plus `LEVEL_28_KEY_CARD_COLLECTED`.

## Secret Bunker and Devil Seals

Using the Bunker Keycard consumes `LIFT_KEY_BAG_ITEM`, sets `LOBBY_HUB_LIFT_KEY_USED`, and opens The Secret Bunker (`GameRoom_Hub8`).

Recovered bunker state:

- `BUNKER_HUB_FIRST_TIME_INTRO_WITNESSED`
- `BUNKER_HUB_NOTE_PAD`
- `BUNKER_HUB_DATA_STICK`
- `BUNKER_HUB_GECKO_INITIAL_INTERACTION`
- `BUNKER_HUB_DEMON_KEY`
- `BUNKER_HUB_STAR_EATER_INTRODUCED`

The Demon Key unlocks the Devil Mode/Demon Seal route. Recorded levels include:

- Demonic Room — Devil version of Boring Room.
- Demonic Tower — Devil version of Minim Tower; Weed Killer is required.
- Demonic Escape — Devil version of Escape.
- Demonic Lockers — Devil version of Locker Room; Weed Killer is required.

These levels display **Demon Seal**, award no stars, and light one bunker seal apiece. After the final seal, interacting with the demon triggers his escape and drops a cartridge. Exact seal-completion flags and the cartridge's native identifiers should be recovered from the original logs before randomization work.

The bunker also contains a Star Eater with a vanilla 66-star requirement. Historical development builds added testing assistance and later experimented with a configurable lower requirement. Those are historical implementation details, not a current Area Access decision.

## Music Lab and Game Garage

`GameRoom_Hub6` contains the phone hub, Music Lab, and Game Garage.

### Performance rules observed

- Normal story levels award one, two, or three stars.
- Music Lab and Game Garage performances award Bronze, Silver, Gold, or Platinum medals.
- Medal values are one, two, three, and four Music Lab points respectively.
- Bee Mode and Demon Seal variants have no stars.
- The Combo Bucket provides score/combo effects throughout the game and a 5× effect in the Music Lab, so score logic must account for its gameplay role.

The approved future difficulty model uses cumulative generated locations; it is not current v0.15 generation behavior:

| Option | Story levels | Music Lab / Garage |
| --- | --- | --- |
| Normal | clear / 1-star location | Bronze location |
| Hard | clear + 2-star locations | Bronze + Silver locations |
| Expert | clear + 2-star + 3-star locations | Bronze + Silver + Gold locations |
| Perfection | same three story tiers | Bronze + Silver + Gold + Platinum locations |

This historical table matches the current approved design, but it is not proof that every historical prototype implementation is correct or that the design is implemented.

### Cassettes observed

| Cassette | Song | Notes |
| --- | --- | --- |
| 1 | Gold | First recorded Music Lab performance. |
| 2 | Superstar | Recorded Silver performance. |
| 3 | Hippo and Frog | Recorded Gold performance. |
| 4 | On the Way | Recorded Gold performance. |
| 5 | Badass | Recorded Gold performance. |
| 6 | Heavy Metal | Recorded Gold performance. |
| 7 | AOK | Recorded Gold performance. |
| 8 | Rainbow Melodies | Recorded Gold performance. |
| 9 | Sneaking | Recorded Platinum performance. |
| 10 | The Heist | Variant `LevelVariant_10_THE_HEIST`. |
| 11 | Money | Variant `LevelVariant_10_MONEY_DUB`. |
| 12 | Let's Go | Variant `LevelVariant_10_LETS_GO`. |
| 13 | Bounce | Variant `LevelVariant_10_BOUNCE`. |
| 26 | Quicksand | Unlocked by the Quicksand Cassette. |

The approved future design randomizes individual cassette and nonstarting-character items. One bounded exception is now implemented: the Level 2 Money Cassette pilot maps `Level_06 -> 110 -> I_GOT_MONEY` to `Level 2 - Money Cassette` and the `Money Cassette` item. The unrelated `MONEY_DUB = 128` entry is the Music Lab song named Money, not this source. All other cassettes still need verified source, inventory, insertion, and reconciliation mappings; the complete nonstarting-character roster and native unlock mappings still require discovery. Live fresh-save and save-switch IL2CPP acceptance for the pilot remain pending.

### Music Lab point chests

The second conversation completed the AP integration for all nine chests. These chests are **Archipelago locations**, while their unlock currency is currently the game's native Music Lab point total.

The authoritative native total is returned by:

```text
CurrentPlayerSaveEnquiries.GetMedalScore()
```

The tested client reads each live `Hub06MedalScoreRewardChest` object's `unlockRequirement` and `chestUnlockedProgressionFlag`, then reconciles already-collected rewards idempotently against the save. This avoids guessed flags and reports checks collected before the current AP session.

Music Lab Points are not presently implemented as generated AP inventory items. In the current prototype, native Music Lab/Game Garage medals earn the native score that gates the reward chests. Separate AP Music Lab Point inventory is approved future design; implementation still needs AP item handling and reachability analysis, while the final 20-item / 180-point distribution remains provisional pending final location-count validation.

| Points | Observed reward | Evidence status |
| --- | --- | --- |
| 5 | Old Game Data | Gameplay observed; used in Game Garage to unlock Maniac. |
| 10 | Space Cartridge | Gameplay observed; unlocks Gradius Remix. |
| 20 | Car Battery | Gameplay observed; giving it to the Ghost Cat unlocks Meoo. |
| 32 | Quicksand Cassette | Gameplay observed; registers Cassette 26 / Quicksand. |
| 46 | Bloodstained Cartridge | Log-confirmed Bloody Tears flags. |
| 64 | Flamenco cassette | AP check validated end-to-end; native chest `SongChest_Flamenco`. |
| 89 | Ten-Four Good Buddy cassette | Native chest `SongChest_TenFourGoodBuddy`; collection flag confirmed. |
| 111 | Zen cassette | Native chest `SongChest_Zen`; collection flag confirmed. |
| 140 | Wiggle cassette | Native chest `SongChest_Wiggle`; collection flag confirmed. |

Confirmed later collection flags:

- `SONG_CASSETTE_COLLECTED_FLAMENCO`
- `SONG_CASSETTE_COLLECTED_TEN_FOUR_GOOD_BUDDY`
- `SONG_CASSETTE_COLLECTED_ZEN`
- `SONG_CASSETTE_COLLECTED_WIGGLE`

The temporary Shift+F4 override patched the effective result of `GetMedalScore()` to cycle through later thresholds without modifying the saved medal total. It proved that the native point getter drives chest availability, but it is a developer diagnostic and not part of the AP progression design.

Bloodstained Cartridge identifiers:

- `LEVEL_27_CARTRIDGE_BLOODYTEARS_COLLECTED`
- `LEVEL_27_CARTRIDGE_BLOODYTEARS_BAG_ITEM`

Entering the Game Garage consumes the bag-item state and registers the cartridge. Game Garage uses `Level_27`; individual songs require song identity in addition to the shared level ID. Historical diagnostics eventually used `SongStickerEvaluation.PlayableSong` with the sticker quality to identify the selected song and medal.

## Character unlocks

The approved future design randomizes each nonstarting playable character as a useful/cosmetic item, separately from the vanilla item or quest that normally awards it. Observed examples:

- Old Game Data turn-in unlocks Maniac.
- Car Battery turn-in unlocks Meoo; native flag `PLAYABLE_CHARACTER_UNLOCKED_MEOO` was confirmed.
- King Ferdinand I completion unlocks King Ferdinand, but a distinct native character flag was not recovered in the cited trace.

Character items are useful/cosmetic unless a character is proven mechanically required. The complete roster, native source/unlock mappings, and final item-pool accounting still require discovery before implementation.

## Historical implementation that is not current design

The source conversation built and tested individual entrance permissions for Levels 1–22. Those experiments established useful technical facts:

- exact door and mat objects can often be isolated without disabling entire visual hierarchies;
- `LevelEntranceDoor.IsInteractionEnabled()` can provide a native-looking interaction block;
- vanilla conditions should remain authoritative beneath any AP requirement;
- Devil Mode doors and placeholder doors must be distinguished from normal entrances;
- persistent hub scanners caused performance problems when allowed to run outside their owning scene;
- recurring physical mechanisms, such as the Lobby Manual Button, must remain recurring.

The corresponding `AP_LEVEL_N_ACCESS` flags, hotkeys, and per-level item design are superseded. They are migration evidence only.

## Evidence gaps and reconciliation queue

Before implementing the next progression change:

1. Design and validate reload, reconnect, and received-item-history reconciliation for the now-mapped Hip Glasses → Chicken Bucket source/item/trade flow.
3. Reconcile meaningful items across later areas, including Hypno Pan, Fish Tears, Super Nectar, Violance, Bunker Keycard, Demon Key, cassettes, cartridges, and characters.
4. Decide which vanilla Star Eater thresholds remain, become generated AP Star requirements, or are replaced by Area Access routing.
5. Confirm exact flags for the Loneliness shield, Devil Seals, demon cartridge, later Music Lab chests, and any postgame completion goal.
6. Treat `internal level + variant` as the stage identity wherever Bee Mode, Devil Mode, or Game Garage reuse an internal level.
7. Allocate permanent IDs only when the corresponding current-design item or location is ready for implementation.

No replay should be requested merely because this repository lacks a raw log that was already supplied in the historical conversation. Search the conversation and recover its evidence first; request a new gameplay capture only when the prior record is genuinely absent, contradictory, or insufficient for the current design question.
