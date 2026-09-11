# Star Victory and quest-capacity evidence checkpoint

Date: 2026-09-11. Scope: Task 1 only, baseline `ca24bd2`.

**Gate: FAIL — 1 Confirmed / 14 Provisional / 1 Rejected / 1 Deferred.**
The required `confirmed_action_count >= 11` is not met. No action IDs are
allocated and no action checks, AP Stars, campaign Star gates, or Victory are
activated. This is an evidence assessment, not a gameplay sign-off.

## Evidence and authority

The historical index is [HISTORICAL_GAMEPLAY_EVIDENCE.md](../HISTORICAL_GAMEPLAY_EVIDENCE.md).
The approved [design](../superpowers/specs/2026-09-11-star-victory-and-quest-capacity-design.md)
requires each Confirmed action to have demonstrated distinctness, reachability,
one-time behavior, reload/reconnect behavior, and a complete reconciliation path.
Known enum spelling or one successful playthrough alone does not satisfy that rule.

Conversation `6a823d3d-f450-83ea-9ad2-890691a86084` (Archipelago Game
Implementation) was recovered in bounded pages of at most 10 turns, spanning
the complete campaign discovery and later Music Lab/Bunker diagnostics (more
than 200 turns). Turn IDs below identify the recovered messages; `turnNNNfileN`
and line ranges identify the original log excerpts cited in those messages.
These are recovered historical log conclusions, not a claim that the unavailable
original uploaded logs were re-read byte for byte. Advice and proposed tests in
those messages are not test results. Their former `Level N Access` rules are
superseded by Area Access plus meaningful items and unavoidable local story.

| Evidence key | Exact historical turn and original excerpt |
| --- | --- |
| E1 | `19b6cf33-74a7-4a7b-8027-850a8ba6b2ef`: `turn110file0` L82–183, bucket consumption, conversion, then Level 09 completion; L184–319, Lobby/phone/button and separate Plunger pickup. Also the focused 2026-08-21 trace in PROGRESSION.md. |
| E2 | `800efadc-e622-44eb-b041-aa3facc67d32`: player describes Letters pickup, Bean Trumpet, mail delivery; `turn122file0` L75–249 places the chain between Boring Room and the later heist King scene. |
| E3 | `16b16f4f-287b-4034-8e3b-aa029c130778`: player explicitly distinguishes Plunger extraction from a second Star feeding interaction; `turn141file0` L11–32 and `turn141file3` L158–178 concern the separate Fish Tears King scene. |
| E4 | `c077a01d-3a6c-4b62-9c4c-b45beb3ee2b5`: `turn161file0` L119–169, Fish Tears picked up inside Act 4, before its result; `3e2afff1-edac-4cd1-81ab-1dd6cdb06ecf`: `turn164file2` L39–101, consumption, deposit, blocker clear; `turn165file0` L33–55, Cell Tower arrival. |
| E5 | `d9d50ecd-160b-4c16-97dd-36ef7facf34e`: player describes Saw Disc and song selection; `turn144file2` L192–223, Act 1 delivery; `turn144file4` L281–312, later result and hub gate. |
| E6 | `14a7cb16-9fb1-48ab-96e4-5ed218b4829f`: player describes spoon/pan conversion, music and cat escort; `turn150file0` L9–33, file1 L43–98, file2 L107–127, file3 L143–161 cover these events; file4 L173–195 identifies Act 2 door. |
| E7 | `90bf5e3a-5eec-4125-87c0-900193a5a1d0`: `turn153file0` L11–31, Act 3 music; `turn153file1` L83–101, Scruffy return; `turn155file0` L129–143, `AnyDogNearbyCondition` and `ScruffyNearbyCondition`. |
| E8 | `7d1b7352-27b0-4de4-b9ab-9d463cf190f6`: `turn160file0` L22–57, Act 4 music; `turn160file1` L98–141, mouse revolution and subsequent bouncer satisfaction. |
| E9 | `1ccfcfdc-f385-4dde-b36d-a80f8f4d5f1d`: `turn173file0` L113–166 and L204–254, separate Bee rewards; `turn175file0` L36–98, both drops consumed and bees escaped. |
| E10 | `b27d269c-00ec-4658-8d96-42beafabd8a7`: `turn185file0` L9–24, file1 L34–50, file2 L59–75 and `turn186file0` L8–24, shield drops and TotemCompletion hierarchy; `turn185file3` L84–105, `turn186file1` L38–59, `turn185file4` L115–137, part inventory. The suggested part-to-statue pairing in this turn was an inference, corrected by E11. |
| E11 | `996a569b-8bd8-4cc3-95bc-353db0817311`: player explicitly gives Eye→Darkness + Plant Pipes, Heart→Loneliness + Hypno Pan, Mind→Escape + Violance. `turn189file0` L5–12, file1 L19–26, file2 L33–40 show consumption; `turn188file0` L13–20, file1 L40–47, file2 L67–74 show area-completion flags; file3 L96–110 and file4 L123–134 show automatic hub completion and Cell Tower wrap-up. |
| E12 | `50dc1dd1-0403-4d43-b0c2-fb487ee66880`: `turn194file0` L6–16, Locker Room result; `turn194file1` L31–60 and `turn195file3` L76–96, Royal feeding/bridge/hangout sequence; `turn196file8` L179–200, Level 22 door. |
| E13 | `1ff63116-0d25-4660-b015-a9ac656f9ddc`: Area Access pivot; later PROGRESSION.md explicitly limits Royal phone access to the Level 22 side. Historical optional modes and illustrative Star costs are not current requirements. |
| E14 | [2026-08-21 acceptance](2026-08-21-hip-glasses-chicken-bucket-acceptance.md): consumption/reload/reconnect and Lift Quest matrix still pending. The later [consolidated record](2026-08-23-consolidated-preview-acceptance.md) does not close conversion-marker durability. Current overview accepts the live Roots route but is not evidence for the missing action-specific durability matrix. |
| E15 | 2026-09-11 focused v0.70.0/v0.24 live capture, seed `91111` / `AP_21483200610759512211.zip`, AP slot `Jack`: save slot 4 established the online before/action/reload/replay path; fresh save slot 3 established false-state isolation, offline conversion, same-identity reconnect, autosave and restart persistence. The diagnostic emitted no action check and made no progression mutation. |

## Candidate ledger

Names below are proposed canonical labels for this checkpoint, not registered
locations. For every Provisional row, repeat behavior is **not proven**,
reload result is **not captured for the action marker**, reconnect result is
**not captured for the action marker**, and offline reconciliation is **proposed,
not accepted**. This applies even where historical play continued across builds:
continued play does not prove the exact saved marker or identity-bound replay.

The proposed reconciliation mode R is: read the exact durable action marker
through the existing `CurrentPlayerSaveEnquiries.GetProgressionFlagValue(eGameProgressionFlag)`
boundary after the correct native save and AP identity are available; associate
it only with that validated identity, reconcile against server checked state,
and use the existing pending queue for disconnected observations. Unknown reads
must remain unknown, never false. Prove reload, repeated event, same-identity
reconnect, and different-identity isolation before acceptance. This checkpoint
does not implement that queue or reconciliation; its snapshots only log reads.

| Key (fixed order) | Proposed canonical location / physical area | Status |
| --- | --- | --- |
| `roots_combo_bucket_conversion` | Roots - Lift Quest - Combo Bucket Conversion / `GameRoom_09` | Confirmed |
| `lobby_important_letters_delivery` | Lobby - Important Letters Delivery / Hub1A | Provisional |
| `lobby_plunger_hand_in` | Lobby - Plunger Hand-In / Lobby Star Eater route | Provisional |
| `lobby_fish_tears_delivery` | Lobby - Fish Tears Delivery / Hub1A | Provisional |
| `meat_hypno_pan_creation` | Meat Dimension - Hypno Pan Creation / Hub4 | Provisional |
| `meat_act_1_music_delivery` | Meat Dimension - Act 1 Music Delivery / Hub4 | Provisional |
| `meat_act_3_music_delivery` | Meat Dimension - Act 3 Music Delivery / Hub4 | Provisional |
| `meat_act_4_music_delivery` | Meat Dimension - Act 4 Music Delivery / Hub4 | Provisional |
| `meat_cat_return` | Meat Dimension - Cat Return / Hub4 | Provisional |
| `meat_scruffy_return` | Meat Dimension - Scruffy Return / Hub4 | Provisional |
| `meat_mouse_revolution` | Meat Dimension - Mouse Revolution / Hub4 | Provisional |
| `cell_super_nectar_delivery` | Cell Tower - Super Nectar Delivery / Hub5B | Provisional |
| `tower_minim_eye_restoration` | Tower of Fear - Minim Eye Restoration / Hub3, Darkness | Provisional |
| `tower_minim_mind_restoration` | Tower of Fear - Minim Mind Restoration / Hub3, Complexity | Provisional |
| `tower_minim_heart_restoration` | Tower of Fear - Minim Heart Restoration / Hub3, Loneliness | Provisional |
| `tower_totem_completion` | Tower of Fear - Totem Completion / Hub3→Hub5C | Rejected |
| `royal_star_eater_feed` | Royal Corridor - Star Eater Feed / Hub7, Level 21 side | Deferred |

### 1. roots_combo_bucket_conversion

- Action/event: use Chicken Bucket during Lift Quest; `LEVEL_09_COMBO_ABILITY_EARNED` false→true, `CHICKEN_BUCKET_BAG_ITEM` true→false, `COMBO_BUCKET_ABILITY` false→true (E1). Exact marker exists independently of `LEVEL_09_COMPLETED`.
- Access/items/events: Roots Access; Chicken Bucket; reachable Lift Quest entrance, preceded in the observed route by Hip Glasses trade, blockade removal and `ROOTS_HUB_KING_LIFT_CHAT_WITNESSED`. Item receipt alone is not permission to bypass native entrance conditions.
- Reward/distinctness: native Combo Bucket stays vanilla; this action is separate from the already registered Bucket Minion trade, both Level 5 results and its cassette sources. No second item is added.
- Repeat/reload/reconnect/offline: confirmed by E15. Slot 4 captured `LEVEL_09_COMBO_ABILITY_EARNED` false in Hub2 and `GameRoom_09`, then the distinct in-level sequence `COMBO_BUCKET_ABILITY` false→true, `CHICKEN_BUCKET_BAG_ITEM` true→false and `LEVEL_09_COMBO_ABILITY_EARNED` false→true. After completion/autosave and a full restart, the marker remained true; replay started with Combo Bucket already present and emitted no second conversion. Fresh slot 3 remained false despite slot 4 being complete. Slot 3 then followed the native Hip Glasses trade, blockade/King dialogue and Lift Quest entrance, converted while the AP server was offline, reconnected to the same seed/slot without restoring either consumed item, completed/autosaved, and retained the true marker with Chicken Bucket absent after a final restart. Reconciliation identity is `LEVEL_09_COMBO_ABILITY_EARNED`; the action must not derive from `LEVEL_09_COMPLETED` or AP item ownership.

### 2. lobby_important_letters_delivery

- Action/event: carry Important Letters upstairs after receiving Bean Trumpet; hand-blocker mail cutscene opens route (E2). Exact durable delivery marker and before/after values unresolved; `LOBBY_HUB_HEIST_KING_WITNESSED` is the later King scene and is not a delivery identity.
- Access/items/events: Lobby Access, recurring Manual Button route, observed Boring Room clear, Letters and Bean Trumpet. Exact necessary local story predicates remain unverified under phone entry; no Level 6 Access item is restored.
- Reward/distinctness: mail clears a route; Bean Trumpet was granted earlier. Distinct physical action from Boring Room and Letters pickup. Exact reward/consumption flags unresolved.
- Repeat/reload/reconnect/offline: pending; R cannot be specified until a delivery marker is identified. No substitution with a heist/cutscene flag.

### 3. lobby_plunger_hand_in

- Action/event: use Plunger to pull Minim out of a hole (E3), followed by a separate Star feed. Metadata contains `LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED`, but no recovered action-correlated transition proves that spelling is this hand-in. Treat it as a diagnostic candidate only.
- Access/items/events: Lobby Access and Plunger (`PLUNGER_BAG_ITEM` exists in metadata); exact hand-in room and local prerequisites unresolved. Plunger pickup is accessible in Hub1B after the button route (E1).
- Reward/distinctness: extraction is separate from pickup and subsequent Star feed; that later feed's threshold must not be imported as the Plunger cost. Meat Dimension Access independently opens its phone route; this hand-in cannot become a universal Meat prerequisite.
- Repeat/reload/reconnect/offline: pending; prove the marker and consumption first, then R.

### 4. lobby_fish_tears_delivery

- Action/event: deliver Fish Tears at the Lobby hand; `LOBBY_HUB_FISH_TEARS_TASK_ITEM` is consumed, `LOBBY_HUB_FISH_TEARS_DEPOSITED` becomes true, followed by `LOBBY_HUB_FISH_TEARS_BLOCKER_CLEARED` (E4). Original bool payloads for the deposit must still be captured.
- Access/items/events: Lobby Access and the native Fish Tears quest/King scene. Tears come from Act 4 (`Level_23`) inside the level, requiring Meat Dimension Access, Hypno Pan and the Act 4 route; Act 4 completion is not itself the pickup prerequisite.
- Reward/distinctness: the deposit opens a route toward Cell Tower, not an item grant; pickup, deposit, and blocker-clear are one source/use chain, with only the deposit proposed as this action. Cell Tower Access bypasses travel order, not this optional hand-in.
- Repeat/reload/reconnect/offline: pending; proposed R on `LOBBY_HUB_FISH_TEARS_DEPOSITED`, never a second check on blocker-cleared.

### 5. meat_hypno_pan_creation

- Action/event: Frog and Hippo combine Wooden Spoon and Frying Pan into Hypno Pan (E6). `PIED_PIPER_ABILITY` is the real ability, not a proven source-only creation marker. Exact source marker and item-consumption transitions unresolved.
- Access/items/events: Meat Dimension Access, Wooden Spoon, Frying Pan; observed pan pickup follows Act 1 completion and `MEAT_HUB_GATE_OPENED`. Ability is not a prerequisite of its own creation.
- Reward/distinctness: Hypno Pan is the vanilla result; current preview/AP ability delivery can set the ability independently. An ability-true observation cannot prove physical creation. Distinct from pan pickup and Act 1 result if a source identity is found.
- Repeat/reload/reconnect/offline: pending; R requires a creation-specific marker or proven durable action journal. Do not reconcile from ability ownership alone.

### 6. meat_act_1_music_delivery

- Action/event: obtain the correct selection from the music trader and give it to the sound NPC; `MEAT_HUB_ACT_ONE_MUSIC_DONE=True` before entry (E5). The historical message does not preserve both bool payloads.
- Access/items/events: Meat Dimension Access, initial cutscene, Saw Disc/trader setup and correct Act 1 music; exact inventory `MEAT_HUB_ACT_ONE_MUSIC_BAG_ITEM` exists in metadata but its consumption is not recovered. No Act 1 completion prerequisite.
- Reward/distinctness: moves sound NPC/bouncer and makes Act 1 available; separate from its result and later `MEAT_HUB_GATE_OPENED`.
- Repeat/reload/reconnect/offline: pending; proposed R on `MEAT_HUB_ACT_ONE_MUSIC_DONE`.

### 7. meat_act_3_music_delivery

- Action/event: deliver the correct Act 3 selection; `MEAT_HUB_ACT_THREE_MUSIC_DONE=True` (E7), separate from bouncer satisfaction. Exact bool payload/consumption capture pending.
- Access/items/events: Meat Dimension Access, native trader/music quest, Act 2 completion in the observed route; Hypno Pan for recurring crowd traversal. Exact mandatory local order needs validation under Area Access; Scruffy return occurs after music, not a proven prerequisite to delivery.
- Reward/distinctness: advances Act 3 music requirement, no separate item reward; Scruffy and Act 3 result are distinct events.
- Repeat/reload/reconnect/offline: pending; proposed R on `MEAT_HUB_ACT_THREE_MUSIC_DONE`.

### 8. meat_act_4_music_delivery

- Action/event: deliver correct Act 4 selection; `MEAT_HUB_ACT_FOUR_MUSIC_DONE` becomes true before the mouse sequence (E8). Exact bool/consumption payload capture pending.
- Access/items/events: Meat Dimension Access, music trader/setup, observed Act 3 completion and new bouncer; Hypno Pan for hub traversal. Mouse revolution is later, not a delivery prerequisite.
- Reward/distinctness: music requirement advances without a new item; distinct from revolution, bouncer satisfaction and Act 4 result.
- Repeat/reload/reconnect/offline: pending; proposed R on `MEAT_HUB_ACT_FOUR_MUSIC_DONE`.

### 9. meat_cat_return

- Action/event: escort cat to Act 2 bouncer with Hypno Pan (E6). Metadata contains `MEAT_HUB_ACT_TWO_BOUNCER_REQUIREMENT_DONE`; the recovered narration does not tie this exact flag's transition exclusively to cat return.
- Access/items/events: Meat Dimension Access, Hypno Pan, Act 1 route/gate, Act 2 music and bouncer request observed before escort. Music flag exists in metadata but ordering/exclusivity need a trace.
- Reward/distinctness: bouncer moves; separate from music delivery and Act 2 result. No new item reward.
- Repeat/reload/reconnect/offline: pending; capture exact return flag and then prove R. Do not confirm from enum naming alone.

### 10. meat_scruffy_return

- Action/event: escort Scruffy to Act 3 bouncer; `MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE=True` (E7). Neighboring scene conditions identify Scruffy specifically. Full bool payload pending.
- Access/items/events: Meat Dimension Access, Hypno Pan, observed Act 2 clear, Act 3 music and bouncer request. Crowd traversal remains recurring even once a door unlocks.
- Reward/distinctness: bouncer moves; distinct from music and Act 3 result; no item reward.
- Repeat/reload/reconnect/offline: pending; proposed R on `MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE`, with exclusivity verified on replay.

### 11. meat_mouse_revolution

- Action/event: escort meat mice with Hypno Pan to the leader near the open window; cutscene sets `MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED`, then a later bouncer interaction sets `MEAT_HUB_ACT_FOUR_BOUNCER_REQUIREMENT_DONE` (E8). Full bool payload pending.
- Access/items/events: Meat Dimension Access, Hypno Pan, observed Act 3 clear, Act 4 music, bouncer hint and leader interaction. Exact necessity/order of all setup predicates remains to be verified.
- Reward/distinctness: mouse cutscene advances route; no item reward. Propose one revolution check, not a second check for the bouncer's consequence or the Act 4 result.
- Repeat/reload/reconnect/offline: pending; proposed R on `MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED`.

### 12. cell_super_nectar_delivery

- Action/event: return both drops to queen; both bag flags are consumed and `PRISON_HUB_BEES_ESCAPED=True` (E9). Full bool payload pending.
- Access/items/events: Cell Tower Access; Hub5B after Central Mainframe/forward gate in observed route; bees conversation; `PRISON_HUB_SUPER_NECTAR_BUBBLES_BAG_ITEM` from Roots `Level_06 / LevelVariant_BeeMode` and `PRISON_HUB_SUPER_NECTAR_RECIPES_BAG_ITEM` from Meat `Level_12 / LevelVariant_BeeMode`. Nectar Party requires Hypno Pan; switches consume Clive/Bizzle. Exact complete Bee access chain remains outside accepted solver mapping.
- Reward/distinctness: bees escape and later heist route starts. Delivery is distinct from both drop pickups and Bee results, which remain diagnostic-only. Do not count Bee locations for capacity.
- Repeat/reload/reconnect/offline: pending; proposed R on `PRISON_HUB_BEES_ESCAPED`; prove reachability through the inactive Bee routes before confirmation.

### 13. tower_minim_eye_restoration

- Action/event: open Darkness statue with Plant Pipes, deposit Eye; `MADNESS_HUB_EYE_BAG_ITEM` consumed and `MADNESS_HUB_DARKNESS_AREA_COMPLETED=True` (E11). This corrects E10's suggested pairing. Full bool payload pending.
- Access/items/events: Tower of Fear Access, Plant Pipes (`WEED_KILLER_ABILITY`), Eye; Darkness (`Level_03`) shield drop `MADNESS_HUB_DARKNESS_SHIELD_DROPPED`. Eye is picked up in Escape/Complexity branch (E10); exact source reachability independent of Escape clear needs confirmation.
- Reward/distinctness: one restored branch/light, no item reward. Separate from Darkness result and Eye pickup.
- Repeat/reload/reconnect/offline: pending; proposed R on `MADNESS_HUB_DARKNESS_AREA_COMPLETED`.

### 14. tower_minim_mind_restoration

- Action/event: open Escape/Complexity statue with Violance, deposit Mind/Brain; `MADNESS_HUB_BRAIN_BAG_ITEM` consumed and `MADNESS_HUB_COMPLEXITY_AREA_COMPLETED=True` (E11). Full bool payload pending.
- Access/items/events: Tower of Fear Access, Violance (`VIOLIN_ABILITY`), Mind/Brain; Escape (`Level_13`) and `MADNESS_HUB_COMPLEXITY_SHIELD_DROPPED`. Mind is in Loneliness branch, whose traversal requires Hypno Pan (E10); Violance comes from the Cold Storage route. Exact item-source graph is not yet complete.
- Reward/distinctness: one restored branch/light, separate from Escape result, Mind pickup and eventual hub completion; no item reward.
- Repeat/reload/reconnect/offline: pending; proposed R on `MADNESS_HUB_COMPLEXITY_AREA_COMPLETED`.

### 15. tower_minim_heart_restoration

- Action/event: open Loneliness statue with Hypno Pan and deposit Heart; `MADNESS_HUB_HEART_BAG_ITEM` consumed and `MADNESS_HUB_LONELINESS_AREA_COMPLETED=True` (E11). Full bool payload pending.
- Access/items/events: Tower of Fear Access, Hypno Pan (`PIED_PIPER_ABILITY`), Heart; Loneliness (`Level_25`) and `MADNESS_HUB_LONELINESS_SHIELD_DROPPED` (exact spelling recovered in E10 and metadata). Heart comes from Darkness branch; exact pickup prerequisites unresolved.
- Reward/distinctness: one restored branch/light, separate from Loneliness result and Heart pickup; no item reward.
- Repeat/reload/reconnect/offline: pending; proposed R on `MADNESS_HUB_LONELINESS_AREA_COMPLETED`.

### 16. tower_totem_completion

- Action/event: E10 names `TotemCompletion`, `SetAreaCompletedFlag`, `OtherAreasAlreadyComplete`, `SetHubCompletedFlag`; E11 says the final deposit immediately starts the freeing cutscene. `OVERALL_PROGRESS_HELPED_SCARED_MINION=True`, then return to Hub5C and `PRISON_HUB_SCARED_MINIM_WRAP_UP_CUTSCENE_WITNESSED=True` are consequences.
- Access/items/events: Tower of Fear Access and all three restorations with their items/abilities/branch shields. No fourth player action is demonstrated.
- Reward/distinctness: opens the exit/Cell Tower route; distinct from three level results, **but not a distinct action from the final statue deposit**. Rejected for net-new action capacity on present evidence. It may later be a free graph event derived from the three addressed checks.
- Repeat/reload/reconnect/offline: not tested; an aggregate marker could be read by R for diagnostics, but does not establish a fourth location. A genuinely separate player interaction would require new evidence and design review.

### 17. royal_star_eater_feed

- Action/event: after Locker Room, introduce/feed Royal Star Eater; `KING_CORRIDOR_STAR_EATER_DIALOGUE_INTRODUCED` then `KING_CORRIDOR_STAR_EATER_FED`, then hangout discovery (E12). Full bool payload pending.
- Access/items/events: Royal Corridor Access is insufficient: its phone lands on Level 22 side, physically unable to reach this NPC (PROGRESSION.md / fresh 2026-08-21 evidence). Observed vanilla route crosses from Tower/Cell Tower to Level 21 side; Locker Room uses Violance and Weed Killer. Exact feed Star threshold and mandatory route predicates unresolved. Do not assume Bunker's 66/50 threshold applies here.
- Reward/distinctness: becomes bridge; separate action from Locker Room and Level 22 results. Optional for phone-side Level 22 and Victory.
- Repeat/reload/reconnect/offline: untested; future R on `KING_CORRIDOR_STAR_EATER_FED` after route/threshold proof. Deferred until that split is modeled safely.

## Native gate and HUD boundary assessment

Read-only PE metadata inspection of the installed `Assembly-CSharp.dll` on
2026-09-11 establishes these exact names/signatures. It does not establish native
caller relationships, Harmony loadability, runtime object identity, or success
of an override. No native method was invoked by the metadata inspection.
Inspected assembly SHA-256:
`5FE14AAAA2599BFBC775A3E2ACC06E0994049C901A96F5124CEE57264DA16FD9`.

| Required boundary | Verified identity and limit |
| --- | --- |
| Campaign required-Star read | **Unresolved.** `LevelEntranceDoor` exposes `level` and `variant`, but no Star requirement field. `LevelData` metadata likewise does not establish a per-level Star entrance requirement. `StarEaterInteraction.feedingThreshold : DefinedInt` and inherited `DefinedValue<int>.GetValue()` are proven historical Bunker threshold identities, not a campaign-door cost mapping. Do not invent `requiredStars` on a level door. |
| Campaign interaction admission | `LevelEntranceDoor.IsInteractionEnabled() : bool` (protected virtual), `ProcessInteraction(CharacterIdentifier)`, `OpenDoor()` and per-instance `level` / `variant` exist. Historical normal-door gating tested IsInteractionEnabled, with door-local `CanEnterCondition` retained. Normal hierarchy example: `Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_03` (E7); boss: `Root/GameRoom_Hub7_Logic/Objects/Doors/LevelEntranceDoor_28_default` (E12). Exact safe managed patch coverage of all campaign doors remains unproven. |
| Campaign current total | `CurrentPlayerSaveEnquiries.GetTotalNumStars() : int` (public static); `StarTotalUIView.RefreshToMatchState()` and `ReflectChunk()` with members `totalLabel`, `starIconVisuals`, `medalIconVisuals`, `chunk`; `StarTotalUIPublicState.ShowAsMedals : bool`. Metadata-only candidates, no proven runtime path or shared policy consumer yet. |
| Campaign required-total display | `StarRequirementPointerIndicatorUIView.OnRefresh(PointerIndicatorUIPublicState, Vector2)`, `RefreshRequirement(int)`, members `requirementLabel`, `valueDisplayed`, `chunk`. A Star Eater pointer exists; it is not proof every campaign door has a numeric requirement or uses this component. Scene identity/caller link unresolved. |
| Hub6 Music Lab Points | Accepted existing `CurrentPlayerSaveEnquiries.GetMedalScore() : int` getter; `MusicLabPointOverridePatches.GetMedalScorePostfix(ref int)` is room-scoped to `GameRoom_Hub6`. Native `Hub06MedalScoreRewardChest.unlockRequirement` and `chestUnlockedProgressionFlag`, nine exact chest roots/thresholds documented in the [point acceptance record](2026-09-09-music-lab-points-acceptance.md). The generated chest wrapper fails to load, so it is not a valid Harmony owner. `StarTotalUIView` metadata has medal visuals/ShowAsMedals, but exact live HUD object path is still not independently captured here. |

**Shared policy boundary: NOT ACCEPTED.** A future policy must produce one
identity-bound decision used by both visible requirement/current total and
actual admission, preserving native prerequisites, variant identity and Hub6
Points. Required proof: exact campaign threshold owner (or an explicitly
approved added requirement), all normal-door identities, runtime HUD identity,
and below/at-threshold display/admission agreement. Text-only edits, collider-only
blocking, native earned-Star writes and unbounded frame logging are rejected.
The historical Bunker patches demonstrate that a changed number can coexist
with a cached blocked interaction; they are not an accepted campaign solution.

## Bounded diagnostic capture and next evidence

Task 1 diagnostics log only allowlisted room/token observations from existing
progression request, flag event, result and manual scene paths. A snapshot may
read known markers once per allowlisted room per process. Values unavailable
through the read boundary remain unknown. Request value is not a transition;
`FlagWasSet` and `FlagIsSet` are recorded independently, including unknowns.
No diagnostic observation is an AP check or durable action journal.

Capture is process-bounded to 64 unique observations in each of six channels
(request, event, snapshot, scene, result-applied, result-persisted), including
at most one copy of each room/token/value combination. Plain F5 invokes the
known-flag snapshot and reuses the existing special-mode manual scene scan;
no frame scanner, extra scene enumeration, native setter or new hotkey is added.
Flag tokens are capped at 256 characters, scene paths at 768 and component
summaries at 1024; control characters and quoted log injection are rejected.
The new scene records include inactive objects, but require the matching room
logic root (the two Lobby rooms share `GameRoom_Hub1_Logic`). Global HUD roots
remain unresolved and are not guessed into this allowlist. A process restart
permits another bounded snapshot; repeat F5 in one room does not re-read flags.

Needed before promotion: for at least 11 distinct rows, capture action-specific
before/after state, a repeated attempt, save reload, offline action plus
same-identity reconnect, different-identity isolation, and the exact local
prerequisite graph. Prioritize the 11 known-marker actions: bucket, Fish Tears,
three music deliveries, Scruffy, mice, nectar and three statues. The remaining candidates need source identity
proof first (Letters, Plunger, Hypno Pan creation, cat). The Totem aggregate
cannot fill a capacity gap. Royal feeding additionally needs the bridge-side
route and actual threshold. No broad campaign replay is requested by this
checkpoint; focus on these missing boundaries only after the user chooses a
diagnostic run. Deployment and launch remain separate authorization gates.

Task 2 remains blocked at 1/11 required Confirmed actions. Versions, contracts,
item/location counts and ID frontiers remain at the accepted baseline. Automated
diagnostics validate capture bounds; only the focused E15 live matrix upgrades
one gameplay evidence status.

## Checkpoint verification

- QuestActions: 11/11 behavior tests passed after an observed 0/11 RED.
  Tests execute the production policy and the extracted production dispatch;
  game reflection inputs and log/native-read boundaries are narrow fixtures.
- Mutation check: changing the per-channel limit from 64 to 65 caused the
  flood test to fail with `expected 64 bounded observations, got 65`; restored.
- All 21 discovered client test projects passed. The new child project initially
  exposed the root DiagnosticHotkeyRouting project's recursive source glob;
  a scoped `QuestActions\**` exclusion corrected that integration failure.
- All 102 APWorld tests and the repository validator passed; the validator
  confirmed Stars/gates/special checks inactive and unchanged ID frontiers.
- Client Release build with `-SkipInstall` passed: zero errors, nine warnings
  (existing nullable warnings and unavailable NuGet vulnerability feed).
- The approved diagnostic client v0.70.0 was deployed only to
  `BepInEx/plugins/RhythmCastleAP` with DLL SHA-256
  `1F0F0A2310C3D75008370466B4E18373674A6C4750495D42E650BEDD7E2ADB43`.
  APWorld v0.24 SHA-256
  `2A3AC0830D4D2DA1BD4E773F671ABFA7B05CB4B4BBE6BABE532B3C195D837253`
  hosted the focused seed. E15 confirms the Combo Bucket action only; the
  remaining native Harmony callback/scene behavior is still a live-testing
  boundary.
