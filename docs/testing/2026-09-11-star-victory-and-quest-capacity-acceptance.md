# Star Victory and quest-capacity evidence checkpoint

Date: 2026-09-11. Scope: Task 1 only, baseline `ca24bd2`.

**Gate: FAIL — 1 Confirmed / 13 Provisional / 1 Rejected / 3 Deferred.**
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
| E4 | `c077a01d-3a6c-4b62-9c4c-b45beb3ee2b5`: `turn161file0` L119–169, historical Fish Tears award inside Act 4 before its result (the newer E32 live run clarifies that this is automatic, not a separate physical pickup); `3e2afff1-edac-4cd1-81ab-1dd6cdb06ecf`: `turn164file2` L39–101, consumption, deposit, blocker clear; `turn165file0` L33–55, Cell Tower arrival. |
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
| E16 | 2026-09-11 focused Meat Dimension phone-route repair, same seed/AP slot and save slot 3: the AP phone transitioned to `GameRoom_Hub4`; the reviewed v0.70.0 diagnostic build disabled only `Root/GameRoom_Hub4_Logic/Objects/Doors/FirstAreaGate` while Meat Dimension Access was owned. The log explicitly retained `MEAT_HUB_GATE_OPENED` and all native quest/story flags unchanged. The player confirmed backtracking across the former gate was possible. The detached Saw Disc remains visibly suspended and was accepted as a cosmetic side effect. |
| E17 | 2026-09-11 focused Meat Act 1 music delivery, same seed/AP slot and save slot 3: pre-action F5 read `MEAT_HUB_ACT_ONE_MUSIC_DONE=False`; the physical Saw Disc/trader/backstage sequence emitted `MEAT_HUB_ACT_ONE_MUSIC_BAG_ITEM` false→true→false and `MEAT_HUB_ACT_ONE_MUSIC_DONE` false→true. No AP check was dispatched. After a full process restart and same-identity reconnect, F5 read the completion marker true; the delivery NPC was gone/non-repeatable and the Act 1 entrance remained available. |
| E18 | 2026-09-11 focused Hypno Pan creation, same seed/AP slot and save slot 3: the physical recipe interaction emitted `PIED_PIPER_ABILITY` false→true, `FRYING_PAN_BAG_ITEM` true→false, and `WOODEN_SPOON_BAG_ITEM` true→false. The earlier physical Spoon pickup emitted `WOODEN_SPOON_BAG_ITEM` false→true and `MEAT_HUB_WOODEN_SPOON_COLLECTED` false→true. No creation-specific durable marker appeared. Because AP delivery of the registered Hypno Pan item independently sets `PIED_PIPER_ABILITY`, ability ownership cannot safely reconcile this source action. The player also confirmed the post-recipe staging is physically enclosed by dogs that block both the phone and the return path; suppressing the vanilla Hypno Pan grant would therefore create an immediate local softlock unless a separately verified escape treatment exists. |
| E19 | 2026-09-11 focused Meat Act 1 result, same seed/AP slot and save slot 3: completing internal `Level_12 / LevelVariant_Default` on Pro produced a three-star evaluation and dispatched exactly one each of `Cassette Source - Lets Go`, `Level 11 - Completion`, and `Level 11 - 1 Star`. Three Stardust items were received from the local slot. Returning to `GameRoom_Hub4` then changed the vanilla route flag `MEAT_HUB_GATE_OPENED` false→true. This proves the result mapping and preserves the native post-level route progression; it is not evidence for a separate quest-action check. |
| E20 | 2026-09-11 focused Meat cat return, same seed/AP slot and save slot 3: Act 2 setup first emitted `MEAT_HUB_ACT_TWO_MUSIC_DONE` false→true and `MEAT_HUB_ACT_TWO_BOUNCER_REQUIREMENT_STATED` false→true. Escorting the cat to the Act 2 bouncer then emitted the distinct source-specific marker `MEAT_HUB_ACT_TWO_BOUNCER_REQUIREMENT_DONE` false→true. No AP quest-action check was dispatched by the read-only diagnostic build. After a full process restart and same-identity reconnect, the player confirmed the Act 2 bouncer remained cleared and the cat delivery remained unavailable/non-repeatable. Offline completion and different-identity isolation remain untested. |
| E21 | 2026-09-11 existing-save Area Access restoration defect, same seed/AP slot and save slot 3: after a full process restart, the slot contract reported Roots open, Lobby locked and Meat Dimension open, but loading the selected save transitioned directly to `GameRoom_Hub1A`. The player then manually traveled `GameRoom_Hub1A` → `GameRoom_Hub6` → `GameRoom_Hub4`. Code inspection confirms the authoritative Area Access transition blocker is intentionally scoped only to departures whose origin is `GameRoom_Hub6`; no corresponding existing-save destination guard currently redirects an unowned saved major-area hub to the Music Lab. Treat this as a general client bug requiring a no-save-mutation redirect at the load boundary. |
| E22 | 2026-09-11 focused Meat Act 2 result, same seed/AP slot and save slot 3: completing internal `Level_15 / LevelVariant_Default` on Pro produced a two-star evaluation and dispatched exactly one each of `Cassette Source - Bounce`, `Level 12 - Completion`, and `Level 12 - 1 Star`. Two Stardust items were received from the local slot. Returning to `GameRoom_Hub4` produced no Act 3 action marker before player interaction, preserving the boundary between the level result and later Act 3 quest actions. |
| E23 | 2026-09-11 focused Meat Act 3 music delivery, same seed/AP slot and save slot 3: obtaining the correct selection emitted `MEAT_HUB_ACT_THREE_MUSIC_BAG_ITEM` false→true. Immediately before the physical hand-in, the F5 snapshot read `MEAT_HUB_ACT_THREE_MUSIC_DONE=False`; the player's subsequent delivery emitted the exact marker false→true. The player confirmed the music disc remained in inventory afterward and behaves unlike a normal consumable item, so disc removal must not be part of the action identity. No AP quest-action check was dispatched. Reload/reconnect/non-repeatability, offline completion and different-identity isolation remain untested. |
| E24 | 2026-09-11 focused Meat Scruffy return, same seed/AP slot and save slot 3: a just-before-action F5 snapshot read `MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE=False`. The bouncer's separate request then emitted `MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_STATED` false→true, and physically returning Scruffy emitted the source-specific `MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE` false→true transition. No AP quest-action check was dispatched. Reload/reconnect/non-repeatability, offline completion and different-identity isolation remain untested. |
| E25 | 2026-09-11 focused Meat Act 3 result, same seed/AP slot and save slot 3: completing internal `Level_22 / LevelVariant_Default` on Pro produced a three-star evaluation and dispatched exactly one each of `Cassette Source - No Plan B`, `Level 13 - Completion`, and `Level 13 - 1 Star`. Three Stardust items were received from the local slot. Returning to `GameRoom_Hub4` left the independently snapshotted `MEAT_HUB_ACT_FOUR_MUSIC_DONE` marker false. |
| E26 | 2026-09-11 focused Meat Act 4 music delivery, same seed/AP slot and save slot 3: immediately before the physical hand-in, an F5 snapshot read `MEAT_HUB_ACT_FOUR_MUSIC_DONE=False`; the player's subsequent delivery emitted the exact marker false→true. No AP quest-action check was dispatched. The music-selection bag flag produced non-authoritative activity around the route, so reconciliation must use the dedicated completion marker only. Reload/reconnect/non-repeatability, offline completion and different-identity isolation remain untested. |
| E27 | 2026-09-11 interrupted Meat mouse-revolution route, same seed/AP slot and save slot 3: after the Act 4 bouncer stated its requirement, the player accidentally left `GameRoom_Hub4` before completing the escort. The transition trace reached locked Lobby rooms `GameRoom_Hub1A`/`GameRoom_Hub1B`, then returned through `GameRoom_Hub6` to Meat. On return, `MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED` remained false and the leader's `RevolutionNotTriggeredCondition` scene branch remained present, but the player reported the mouse leader itself missing and the active revolution models were not in hierarchy. A full process restart, same-save reload and reconnect did not restore the leader; the post-restart F5 scene scan again showed `SpawnMouseLeader/SpawnCondition/RevolutionNotTriggeredCondition` active in hierarchy while `MeatMouseRevolution_Leader` remained outside the live hierarchy. This exposes both a persistent interrupted-escort softlock and a second Area Access boundary gap: non-phone world exits can enter an unowned major area. The save cannot complete the route normally. |
| E28 | 2026-09-12 correction to E27, same seed/AP slot and save slot 3: after subsequent reloads, the exact native `SpawnMouseLeader/SpawnCondition` compound `CheckIfMet()` returned false while its `RevolutionNotTriggeredCondition` returned true. The recovery log read `MEAT_HUB_ACT_FOUR_BOUNCER_REQUIREMENT_STATED=False` and therefore safely skipped the native spawner call. The F5 hierarchy also showed the sibling `RequirementStatedCondition`. Repeating the normal Act 4 bouncer dialogue emitted `MEAT_HUB_ACT_FOUR_BOUNCER_REQUIREMENT_STATED` false→true, and the player immediately confirmed that the mouse leader appeared with normal control. Thus E27's claim that the save could not recover normally is superseded: re-speaking to the bouncer is a validated recovery route on this save. Whether the stated flag is intentionally transient or unexpectedly lost on room exit/reload remains unproven; the mod's native spawner-recovery branch was not exercised by this test. Completion is recorded separately in E29. |
| E29 | 2026-09-12 focused Meat mouse revolution completion, same seed/AP slot and save slot 3: after the normal Act 4 bouncer dialogue restored the leader, the player completed the Hypno Pan mouse escort. The cutscene played and the route opened. The live progression events recorded `MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED` false→true, followed by the distinct bouncer consequence `MEAT_HUB_ACT_FOUR_BOUNCER_REQUIREMENT_DONE` false→true. No AP quest-action check was dispatched by the diagnostic build, and the mod's native spawner-recovery branch was not used. This confirms the physical quest route can complete after E27's interruption by repeating the bouncer dialogue. The player then left Meat through the phone and returned; the Act 4 route remained open. Full-process-restart persistence is recorded in E30. Repeat/non-repeat behavior, offline completion and different-identity isolation remain untested. |
| E30 | 2026-09-12 Meat mouse revolution restart check, same seed/AP slot and save slot 3: after a full game-process restart, the player returned to `GameRoom_Hub4`, reported the Act 4 route remained open, and pressed F5. The read-only saved-state snapshot returned `MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED=True`; no AP quest-action check was dispatched. This proves persistence across this restart, not offline/different-identity behavior or a second completion attempt. |
| E31 | The same post-restart Hub4 F5 snapshot also read `MEAT_HUB_ACT_THREE_MUSIC_DONE=True`, `MEAT_HUB_ACT_FOUR_MUSIC_DONE=True`, and `MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE=True`. These action markers therefore survived the full game restart and same-slot reconnect. This snapshot does not prove that their interactions cannot be repeated, nor does it exercise offline completion or different-identity isolation. |
| E32 | 2026-09-12 focused Meat Act 4 / Level 14 (`Level_23 / LevelVariant_Default`) result, same seed/AP slot and save slot 3: the player clarified that Fish Tears is given automatically rather than collected through a separate interaction. Immediately before the result callback, live progression events set `LOBBY_HUB_FISH_TEARS_TASK_ITEM` and `LEVEL_23_FISH_TEARS_COLLECTED` from false→true. The Pro result scored 107259, evaluated three Stars, and sent `Cassette Source - Jolt City`, `Level 14 - Completion`, and `Level 14 - 1 Star`; the Jolt City Cassette was received from AP and persisted. The Fish Tears award is an in-level vanilla reward, not the later Lobby delivery action. The exact point within the level's end sequence that grants it remains to be visually characterized. |
| E33 | 2026-09-12 Lobby route correction, same seed/AP slot and save slot 3: `Cassette Source - Gold` sent Lobby Access after Roots Level 1, and the AP phone entered `GameRoom_Hub1A`. Before any Fish Tears delivery, the player reported that Boring Room, Vault and Minim Tower must be completed to clear the other hand blockers. This is a player-reported local order, not yet a verified per-level prerequisite trace. Do not test the Fish Tears hand-in immediately on first Lobby arrival or treat Lobby Access alone as sufficient. |
| E34 | 2026-09-12 focused Lobby Plunger use, same seed/AP slot and save slot 3: an F5 scan in `GameRoom_Hub1A` captured the native `PlungerInteractions/NeedsPlunger` and `Cutscene_SE_UsePlunger` scene paths before the action; the Fish Tears deposit snapshot remained false. The player then used the Plunger once. The live event consumed `PLUNGER_BAG_ITEM` true→false and set `LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED` false→true. The player confirmed the Lobby Star Eater became interactable afterward. No AP quest-action check was dispatched. This proves the exact durable Plunger action marker and its distinction from the subsequent Star Eater feed; repeat/reload/reconnect/offline behavior remains untested. |
| E35 | 2026-09-12 focused Lobby Star Eater interaction immediately after E34, same seed/AP slot and save slot 3: the player clarified that this Star Eater checks whether the current Star total meets a threshold rather than consuming Stars, and that having enough Stars hides the unmet-requirement prompt. One interaction emitted `LOBBY_HUB_STAR_EATER_DIALOGUE_INTRODUCED` false→true, then the distinct `LOBBY_HUB_STAR_EATER_FED` false→true. No AP quest-action check was dispatched. The player confirmed the vanilla Meat entrance opened and was traversable in both directions. The exact threshold remains unconfirmed. This action is not counted in the current quest-capacity ledger pending separate reachability, one-time, reconciliation, and design review. |
| E36 | 2026-09-12 Lobby Letters staging correction, same seed/AP slot and save slot 3: after Boring Room / Level 6 (`Level_02`) completion, the player collected the Bean Trumpet before attempting the Letters hand-in. In `GameRoom_Hub1A`, live events set `LOBBY_HUB_MENIAL_TASK_ITEM`, `LOBBY_HUB_MENIAL_TASK_ITEM_COLLECTED`, and `BEAN_TRUMPET_ABILITY` from false→true. The diagnostic candidate match on `BEAN_TRUMPET_ABILITY` is a broad discovery hint only; it dispatched no AP check and is not evidence that Important Letters were delivered. The actual upstairs hand-in and its durable marker remain unobserved in this run. |
| E37 | 2026-09-12 focused Lobby Important Letters hand-in, same seed/AP slot and save slot 3: an F5 scan before the action captured `Hub1A_Logic_UpperPath/Progression/PlayerDetectionProgressionLogic/MenialTask/MenialTask_Delivery` with `HaveItem` and `KingSeen` conditions. The player then delivered the Letters once. Live events set `LOBBY_HUB_MENIAL_TASK_ITEM` true→false, `LOBBY_HUB_MENIAL_TASK_ITEM_DEPOSITED` false→true, then `LOBBY_HUB_MENIAL_TASK_BLOCKER_CLEARED` false→true. The distinct later scene set `LOBBY_HUB_HEIST_KING_WITNESSED` false→true. The player confirmed the path opened and clarified that Vault and Minim Tower were already available before this delivery, while Demolition Training became available afterward; the remaining hand blockers still need their separate tasks. No AP quest-action check was dispatched by the diagnostic build. The deposited flag is the action identity; neither task-item absence, blocker-clear nor the later King scene should be a second location. Repeat/reload/reconnect/offline behavior remains untested. |
| E38 | 2026-09-12 Lobby Vault completion, same seed/AP slot and save slot 3: the player completed Vault, identified by the live result as `Level_01 / LevelVariant_Default` and AP Level 10, on Pro with score 655471 and three game Stars. The client sent `Cassette Source - Money`, `Level 10 - Completion`, and `Level 10 - 1 Star`. After returning through `GameRoom_Hub1B` to `GameRoom_Hub1A`, the native `LOBBY_HUB_HEIST_VAULT_BLOCKER_CLEARED` flag changed false→true. This confirms the Vault hand blocker was cleared by this run; it is separate from the earlier Letters deposit. The player has not yet reported completing Minim Tower or Demolition Training in this test. |
| E39 | 2026-09-12 Lobby Minim Tower completion, same seed/AP slot and save slot 3: the player completed Minim Tower, identified by the live result as `Level_11 / LevelVariant_Default` and AP Level 8, on Pro. The client sent `Cassette Source - Gold`, `Cassette Source - On the Way`, `Cassette Source - Rainbow Melodies`, `Level 8 - Completion`, and `Level 8 - 1 Star`. After returning through `GameRoom_Hub1B` to `GameRoom_Hub1A`, the native `LOBBY_HUB_HEIST_TOWER_BLOCKER_CLEARED` flag changed false→true. This confirms the Tower hand blocker cleared independently of the earlier Vault and Letters transitions. Demolition Training and any later School Trip/Fish Tears steps remain untested in this run. |
| E40 | 2026-09-12 Lobby Demolition Training completion, same seed/AP slot and save slot 3: the player completed the three-room training sequence `GameRoom_19A/B/C`, identified by the live result as `Level_19 / LevelVariant_Default` and AP Level 7, on Pro. Before the result, `LOBBY_HUB_TOOLS_CERTIFICATE` changed false→true; the player confirmed receipt of the Demolition Certificate. A second true→true event did not represent another award. The client sent `Cassette Source - Sneaking`, `Level 7 - Completion`, and `Level 7 - 1 Star`, then returned to `GameRoom_Hub1A`. The player clarified that this certificate unlocks School Trip and that School Trip completion clears the last hand blocker. School Trip entry and completion remain to be verified in this run; do not treat the certificate itself as the last hand-clearing action. |
| E41 | 2026-09-12 Lobby School Trip completion, same seed/AP slot and save slot 3: after receiving the Demolition Certificate, the player entered and completed the three-room `GameRoom_20A/B/C` sequence, identified by the live result as `Level_20 / LevelVariant_Default` and AP Level 9, on Pro. The result scored 44000 and evaluated two game Stars. The client sent `Cassette Source - The Heist`, `Level 9 - Completion`, and `Level 9 - 1 Star`. On return to `GameRoom_Hub1A`, `LOBBY_HUB_HEIST_HEIST_BLOCKER_CLEARED` changed false→true, confirming the School Trip hand blocker was cleared separately from the earlier Vault and Tower blockers. Fish Tears hand-in has not yet been attempted in this run. |
| E42 | Lobby item-design clarification from the player during this test: Bean Trumpet grants the dash ability. The player does not know of an express mandatory use; it makes levels easier. Randomization was part of the prior design, but treating it as a hard logic requirement merely because it is awarded before the Letters delivery would be unsupported. Its necessity for any specific high-tier objective still needs targeted testing. |
| E43 | 2026-09-12 focused Lobby Fish Tears hand-in, same seed/AP slot and save slot 3: an F5 scan before the action read `LOBBY_HUB_FISH_TEARS_DEPOSITED=False` and captured the nearby `FishTears_Delivery` detector with `HaveItem` and `SeenKing` conditions in `GameRoom_Hub1A`. The player delivered Fish Tears once. Live events set `LOBBY_HUB_FISH_TEARS_TASK_ITEM` true→false, `LOBBY_HUB_FISH_TEARS_DEPOSITED` false→true, then `LOBBY_HUB_FISH_TEARS_BLOCKER_CLEARED` false→true. `LOBBY_HUB_FISH_TEARS_KING_WITNESSED` changed false→true later in the same sequence; this is not the deposit identity. The player confirmed the vanilla Cell Tower entrance opened. No AP quest-action check was dispatched by the diagnostic build. Repeat/reload/reconnect/offline and actual entrance traversal remain untested in this run. |
| E44 | 2026-09-12 Cell Tower vanilla-entrance round-trip immediately after E43, same seed/AP slot and save slot 3: the player entered the newly opened route and returned. The transition trace recorded `GameRoom_Hub1A` → `GameRoom_Hub5A` → `GameRoom_Hub1A`; the Cell Tower introduction flag `PRISON_HUB_LOWER_SHAFT_INTRO_WITNESSED` changed false→true on first entry. The player clarified that Cell Tower's AP phone booth is positioned after the first vanilla gate/transition, as with Meat Dimension. This is a routing-boundary observation to test in an early-access seed. This seed had already received `Cell Tower Access`, so the round-trip confirms that the opened entrance traversed correctly here but does not isolate behavior without that AP item. |
| E45 | 2026-09-13 full-process restart of the same seed/AP slot and game save slot 3 after the Letters and Fish Tears hand-ins: the local AP server restored its saved state, BepInEx loaded client v0.70.0, the client connected to `127.0.0.1:38281` as Jack, and the game selected slot 3 before entering `GameRoom_Hub1A`. The read-only F5 snapshot reported `LOBBY_HUB_FISH_TEARS_DEPOSITED=True`, confirming that exact deposit marker survived the restart. The player confirmed that both the Letters-opened passage and vanilla Cell Tower entrance remained visually open after the reload. The current F5 policy does not snapshot `LOBBY_HUB_MENIAL_TASK_ITEM_DEPOSITED`, so exact Letters-marker persistence is inferred from the visible route, not directly read. No AP quest-action check was dispatched. |
| E46 | 2026-09-14 static inspection of the installed managed game assembly found three distinct Roots flags: `ROOTS_HUB_STAR_EATER_DIALOGUE_INTRODUCED`, `ROOTS_HUB_STAR_EATER_FED`, and `ROOTS_HUB_STAR_EATER_BLOCKADE_REMOVED`. This proves only that separate names exist in metadata, not that feeding is reachable, triggered, durable, or independent of AP blockade suppression. The client already suppresses the Roots Star Eater blockade object under Roots Access without writing the native Star Eater/Trevor quest flags. No live feed action or AP check was observed. |
| E47 | 2026-09-14 retained seed `AP_21483200610759512211`, AP slot `Jack`, native save slot 3: after a full game launch with the read-only Roots diagnostic client installed (DLL SHA-256 `4C12E5FD6B6E7257C0DE79E8389BAC1A5345D7C49FD7A74FE9DA268922FAEDF7`), the client connected to the local server and selected slot 3. In `GameRoom_Hub2`, the one-time F5 snapshot read `ROOTS_HUB_STAR_EATER_FED=True`. The player thought they had already fed this Star Eater on this save. This is a true-state baseline on a retained save, not a witnessed feed transition, threshold test, replay test, or AP check. |
| E48 | 2026-09-14 same retained seed/AP slot with native save slot 4 selected: before interacting with the Roots Star Eater, a read-only F5 snapshot in `GameRoom_Hub2` returned `ROOTS_HUB_STAR_EATER_FED=False`. The player independently confirmed this Star Eater was unfed and reported a displayed requirement of 3 Stars. This establishes a clean before-state and an observed UI threshold, not yet a successful feed transition or proof of the underlying admission rule. The AP-suppressed `StarEaterBlockade/Blockade` object was inactive, separately from the false feed flag. |
| E49 | 2026-09-14 immediate follow-up on the same seed/AP slot and native save slot 4: the player fed the Roots Star Eater once. The read-only diagnostic captured `ROOTS_HUB_STAR_EATER_FED` false→true, followed by a native request for the same flag. Only afterward did the distinct consequence flag `ROOTS_HUB_STAR_EATER_BLOCKADE_REMOVED` change false→true. The player confirmed their Star total did not decrease and explained that the vanilla interaction normally opens the path toward the phone booth/Weed Killer source, which AP Roots routing already bypasses for navigation. The diagnostic dispatched no AP check. This proves a reachable one-time action event and separation from physical blocker suppression; repeat, reload, reconnect, offline, and exact threshold-boundary behavior remain untested. |
| E50 | 2026-09-14 full game-process restart after E49, retained seed `AP_21483200610759512211`, AP slot `Jack`, native save slot 4: the local server loaded its existing `.apsave`; the client log resolved and activated save slot 4 (lines 91-94), confirmed connection as Jack (line 316), and the read-only F5 snapshot in `GameRoom_Hub2` read `ROOTS_HUB_STAR_EATER_FED=True` (line 480). The player reported that the Star Eater was no longer available when checking whether feeding remained offered. This establishes same-save restart persistence, same-identity reconnect, and feeding remaining unavailable on this save. The snapshot dispatched no AP check. Offline completion followed by restart/reconnect, different-identity isolation, and the exact threshold/local-prerequisite boundary remain untested; the candidate remains Provisional and contributes no capacity. |
| E51 | 2026-09-14 same retained seed/AP slot, newly recreated native slot 4 (the player erased the earlier slot 4 after E50): F5 established `ROOTS_HUB_STAR_EATER_FED=False`. The player reported a 3-Star requirement and no available interaction at 0 Stars. After earning 5 Stars, a fresh post-restart F5 baseline again read false. The local server then exited via its normal `/exit` command; the client logged retained-disconnected state and failed login attempts. While offline, feeding emitted `ROOTS_HUB_STAR_EATER_FED` false→true (log line 779), followed separately by `ROOTS_HUB_STAR_EATER_BLOCKADE_REMOVED`. The player confirmed the total stayed at 5. No AP action check was dispatched. This proves offline feeding with previously synchronized AP access and no Star consumption; exact admission at 3 Stars and refusal at 2 were not tested. |
| E52 | 2026-09-14 restart immediately after E51 with the server still off: the client activated native slot 4, but the player spawned in the tutorial. AP compatibility and Area Access had not synchronized on this cold offline launch; code inspection shows the intro redirect requires compatible slot data. The feed flag was not read in Roots during that offline session, so tutorial arrival does not prove save loss. After the retained server restarted, the game continued reporting login timeouts and did not reconnect; an independent read-only WebSocket handshake returned the expected seed `21483200610759512211`. The automatic reconnect attempt failed. Its cause remains unresolved. |
| E53 | 2026-09-14 recovery after E52: a normal game restart with the same retained server already online connected as Jack (log line 191) and activated slot 4 (line 247). In Roots, F5 read `ROOTS_HUB_STAR_EATER_FED=True` (line 347); the player confirmed the Star Eater remained unavailable. This proves the E51 offline completion survived process restarts and same-identity reconnection after a manual game restart. It does not turn E52's failed automatic reconnect into a pass or prove cold-offline Roots reachability. The diagnostic sent no AP action check; different-identity isolation and complete threshold/local-prerequisite proof remain pending. |
| E54 | 2026-09-14 testing interruption on the recreated slot 4: the player reported both sustained low frame rate and stutters after restoring the retained seed into a new save, severe enough to prevent scoring 2 or 3 Stars. Restarting the same save made ordinary movement smooth, but the player subsequently reported level play remained laggy though playable. The retained pre-restart log contains native save-processing null references during item restoration and a cassette persistence wave deferred with `acceptance-processor-ownership-mismatch`. Static inspection found that the queued wave can repeat expensive ownership reads every frame while only its log is deduplicated. This is a performance lead, not measured causal proof or a verified fix. Performance remains unresolved; no extra campaign replay is required to reproduce the reported symptom. |

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
| `meat_hypno_pan_creation` | Meat Dimension - Hypno Pan Creation / Hub4 | Deferred |
| `meat_act_1_music_delivery` | Meat Dimension - Act 1 Music Delivery / Hub4 | Provisional |
| `meat_act_3_music_delivery` | Meat Dimension - Act 3 Music Delivery / Hub4 | Provisional |
| `meat_act_4_music_delivery` | Meat Dimension - Act 4 Music Delivery / Hub4 | Provisional |
| `meat_cat_return` | Meat Dimension - Cat Return / Hub4 | Provisional |
| `meat_scruffy_return` | Meat Dimension - Scruffy Return / Hub4 | Provisional |
| `meat_mouse_revolution` | Meat Dimension - Mouse Revolution / Hub4 | Deferred |
| `cell_super_nectar_delivery` | Cell Tower - Super Nectar Delivery / Hub5B | Provisional |
| `tower_minim_eye_restoration` | Tower of Fear - Minim Eye Restoration / Hub3, Darkness | Provisional |
| `tower_minim_mind_restoration` | Tower of Fear - Minim Mind Restoration / Hub3, Complexity | Provisional |
| `tower_minim_heart_restoration` | Tower of Fear - Minim Heart Restoration / Hub3, Loneliness | Provisional |
| `tower_totem_completion` | Tower of Fear - Totem Completion / Hub3→Hub5C | Rejected |
| `royal_star_eater_feed` | Royal Corridor - Star Eater Feed / Hub7, Level 21 side | Deferred |
| `roots_star_eater_feed` | Roots - Star Eater Feed / Hub2 | Provisional |

### 1. roots_combo_bucket_conversion

- Action/event: use Chicken Bucket during Lift Quest; `LEVEL_09_COMBO_ABILITY_EARNED` false→true, `CHICKEN_BUCKET_BAG_ITEM` true→false, `COMBO_BUCKET_ABILITY` false→true (E1). Exact marker exists independently of `LEVEL_09_COMPLETED`.
- Access/items/events: Roots Access; Chicken Bucket; reachable Lift Quest entrance, preceded in the observed route by Hip Glasses trade, blockade removal and `ROOTS_HUB_KING_LIFT_CHAT_WITNESSED`. Item receipt alone is not permission to bypass native entrance conditions.
- Reward/distinctness: native Combo Bucket stays vanilla; this action is separate from the already registered Bucket Minion trade, both Level 5 results and its cassette sources. No second item is added.
- Repeat/reload/reconnect/offline: confirmed by E15. Slot 4 captured `LEVEL_09_COMBO_ABILITY_EARNED` false in Hub2 and `GameRoom_09`, then the distinct in-level sequence `COMBO_BUCKET_ABILITY` false→true, `CHICKEN_BUCKET_BAG_ITEM` true→false and `LEVEL_09_COMBO_ABILITY_EARNED` false→true. After completion/autosave and a full restart, the marker remained true; replay started with Combo Bucket already present and emitted no second conversion. Fresh slot 3 remained false despite slot 4 being complete. Slot 3 then followed the native Hip Glasses trade, blockade/King dialogue and Lift Quest entrance, converted while the AP server was offline, reconnected to the same seed/slot without restoring either consumed item, completed/autosaved, and retained the true marker with Chicken Bucket absent after a final restart. Reconciliation identity is `LEVEL_09_COMBO_ABILITY_EARNED`; the action must not derive from `LEVEL_09_COMPLETED` or AP item ownership.

### 2. lobby_important_letters_delivery

- Action/event: carry Important Letters upstairs after receiving Bean Trumpet; hand-blocker mail cutscene opens route (E2/E37). E36 confirms that collecting the Lobby task item and receiving `BEAN_TRUMPET_ABILITY` precede the hand-in. E37 identifies `LOBBY_HUB_MENIAL_TASK_ITEM_DEPOSITED` false→true as the distinct delivery marker; the task item is consumed and `LOBBY_HUB_MENIAL_TASK_BLOCKER_CLEARED` follows. `LOBBY_HUB_HEIST_KING_WITNESSED` is the later King scene, not the delivery identity.
- Access/items/events: Lobby Access, recurring Manual Button route, observed Boring Room clear, Letters and Bean Trumpet. Exact necessary local story predicates remain unverified under phone entry; no Level 6 Access item is restored.
- Reward/distinctness: mail clears a route; Bean Trumpet was granted earlier. Distinct physical action from Boring Room and Letters pickup. E37 observed `LOBBY_HUB_MENIAL_TASK_ITEM` true→false, deposit false→true, and blocker-clear false→true. The player clarified that Vault and Minim Tower were already available; Demolition Training became available after the hand-in, while other hand blockers remained.
- Repeat/reload/reconnect/offline: E45 confirms the Letters-opened passage remained visually open after a full game/server restart on the same seed/AP slot and game save slot 3. The exact deposited marker was observed false→true at the action (E37) but was not directly snapshotted after reload; repeat, offline and different-identity behavior remain pending. Proposed R is `LOBBY_HUB_MENIAL_TASK_ITEM_DEPOSITED`; do not substitute item absence, blocker-clear, or a heist/cutscene flag.

### 3. lobby_plunger_hand_in

- Action/event: use Plunger to pull Minim out of a hole (E3/E34), followed by a separate Star Eater interaction. E34 proves the action-correlated `LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED` false→true transition and `PLUNGER_BAG_ITEM` consumption true→false; the player confirmed the Star Eater became interactable only afterward.
- Access/items/events: Lobby Access and Plunger (`PLUNGER_BAG_ITEM` exists in metadata); exact hand-in room and local prerequisites unresolved. Plunger pickup is accessible in Hub1B after the button route (E1).
- Reward/distinctness: extraction is separate from pickup and subsequent Star Eater total check. E35 proves the separate `LOBBY_HUB_STAR_EATER_FED` transition; the player clarified that this Star Eater tests whether enough Stars are already held and does not spend them. Its unknown threshold must not be imported as a Plunger requirement. Meat Dimension Access independently opens its phone route; this hand-in cannot become a universal Meat prerequisite.
- Repeat/reload/reconnect/offline: E34 proves the source marker and consumption; repeat behavior, saved-state durability, same-identity reconnect, offline completion and different-identity isolation remain pending. Proposed R is `LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED`, never Plunger bag absence alone.

### 4. lobby_fish_tears_delivery

- Action/event: deliver Fish Tears at the Lobby hand; E43 proves `LOBBY_HUB_FISH_TEARS_TASK_ITEM` true→false, `LOBBY_HUB_FISH_TEARS_DEPOSITED` false→true, followed by `LOBBY_HUB_FISH_TEARS_BLOCKER_CLEARED` false→true. The later `LOBBY_HUB_FISH_TEARS_KING_WITNESSED` transition is not the delivery identity.
- Access/items/events: Lobby Access and the native Fish Tears quest/King scene. E32 confirms the game automatically awarded Tears during the Act 4 (`Level_23`) reward sequence, with `LOBBY_HUB_FISH_TEARS_TASK_ITEM` and `LEVEL_23_FISH_TEARS_COLLECTED` both false→true just before the result callback; there was no separate player pickup interaction. The observed route required Meat Dimension Access, Hypno Pan and Act 4 access. In this live run the player completed Boring Room, Letters delivery, Vault, Minim Tower, Demolition Training and School Trip before the hand-in. That sequence proves one viable route, not that every step is a minimal prerequisite. Do not claim Act 4 completion is unnecessary for the award without a finer-grained live boundary test.
- Reward/distinctness: the deposit opened the vanilla Cell Tower entrance according to the player's in-game observation, not an item grant; E44 confirmed a successful round-trip on a seed that already owned `Cell Tower Access`. Pickup, deposit, and blocker-clear are one source/use chain, with only the deposit proposed as this action. Cell Tower Access bypasses travel order, not this optional hand-in. Without-AP-Access vanilla traversal remains untested.
- Repeat/reload/reconnect/offline: E45 confirms the exact `LOBBY_HUB_FISH_TEARS_DEPOSITED=True` marker after a full game/server restart and same seed/AP slot/game save slot 3. Repeat, offline completion, different-identity isolation and any subsequent AP check dispatch remain untested. Proposed R is this deposited marker, never a second check on blocker-cleared.

### 5. meat_hypno_pan_creation

- Action/event: Frog and Hippo combine Wooden Spoon and Frying Pan into Hypno Pan (E6/E18). The live physical interaction sets `PIED_PIPER_ABILITY` false→true and consumes both ingredient bag flags true→false. No separate source-only creation marker was observed.
- Access/items/events: Meat Dimension Access, Wooden Spoon, Frying Pan; observed pan pickup follows Act 1 completion and `MEAT_HUB_GATE_OPENED`. Ability is not a prerequisite of its own creation.
- Reward/distinctness: Hypno Pan is the vanilla result; current preview/AP ability delivery can set the ability independently. An ability-true observation cannot prove physical creation. Distinct from pan pickup and Act 1 result if a source identity is found.
- Repeat/reload/reconnect/offline: Deferred. The live event sequence is suitable for immediate observation, but Task 10 requires a safe durable reconciliation identity. `PIED_PIPER_ABILITY` cannot serve because AP delivery of Hypno Pan sets the same flag. In addition, the recipe cutscene leaves the player enclosed by Hypno-controlled dogs blocking both exits, so replacing the vanilla ability reward with an arbitrary AP item would softlock the player immediately. Do not activate this source or generated Hypno Pan until both a distinct durable identity and a verified post-recipe escape are designed; do not infer physical creation merely from ability ownership or consumed bags.

### 6. meat_act_1_music_delivery

- Action/event: obtain the correct selection from the music trader and give it to the sound NPC; `MEAT_HUB_ACT_ONE_MUSIC_DONE=True` before entry (E5). The historical message does not preserve both bool payloads.
- Access/items/events: Meat Dimension Access, initial cutscene, Saw Disc/trader setup and correct Act 1 music; exact inventory `MEAT_HUB_ACT_ONE_MUSIC_BAG_ITEM` exists in metadata but its consumption is not recovered. No Act 1 completion prerequisite.
- Reward/distinctness: moves sound NPC/bouncer and makes Act 1 available; separate from its result and later `MEAT_HUB_GATE_OPENED`.
- Repeat/reload/reconnect/offline: E17 proves the exact false→true completion transition, the temporary bag-item consumption sequence, full-restart durability, same-identity reconnect, and non-repeatability while preserving the Act 1 entrance. Proposed R is now specifically proven readable on `MEAT_HUB_ACT_ONE_MUSIC_DONE`. Offline completion and different-identity isolation remain pending, so the row stays Provisional.

### 7. meat_act_3_music_delivery

- Action/event: deliver the correct Act 3 selection; `MEAT_HUB_ACT_THREE_MUSIC_DONE` changes false→true (E7/E23), separate from bouncer satisfaction. E23 also captures the correct selection entering `MEAT_HUB_ACT_THREE_MUSIC_BAG_ITEM` false→true. The disc remains in the player's inventory after delivery and is not a normal consumable; do not require or synthesize a false transition for that bag flag.
- Access/items/events: Meat Dimension Access, native trader/music quest, Act 2 completion in the observed route; Hypno Pan for recurring crowd traversal. Exact mandatory local order needs validation under Area Access; Scruffy return occurs after music, not a proven prerequisite to delivery.
- Reward/distinctness: advances Act 3 music requirement, no separate item reward; Scruffy and Act 3 result are distinct events.
- Repeat/reload/reconnect/offline: E23 proves the exact pre-action false snapshot and live source transition; E31 confirms the completion marker true after full restart and same-slot reconnect. Non-repeatability, offline completion and different-identity isolation remain pending; proposed R is specifically grounded on `MEAT_HUB_ACT_THREE_MUSIC_DONE`.

### 8. meat_act_4_music_delivery

- Action/event: deliver the correct Act 4 selection; `MEAT_HUB_ACT_FOUR_MUSIC_DONE` changes false→true before the mouse sequence (E8/E26). E26 provides an exact pre-action false snapshot and the physical-delivery transition.
- Access/items/events: Meat Dimension Access, music trader/setup, observed Act 3 completion and new bouncer; Hypno Pan for hub traversal. Mouse revolution is later, not a delivery prerequisite.
- Reward/distinctness: music requirement advances without a new item; distinct from revolution, bouncer satisfaction and Act 4 result.
- Repeat/reload/reconnect/offline: E26 proves the live source transition; E31 confirms the completion marker true after full restart and same-slot reconnect. Non-repeatability, offline completion and different-identity isolation remain pending; proposed R is specifically grounded on `MEAT_HUB_ACT_FOUR_MUSIC_DONE`, never the non-authoritative music-selection bag flag.

### 9. meat_cat_return

- Action/event: escort cat to Act 2 bouncer with Hypno Pan (E6/E20). The live physical delivery emitted the exact `MEAT_HUB_ACT_TWO_BOUNCER_REQUIREMENT_DONE` false→true transition.
- Access/items/events: Meat Dimension Access, Hypno Pan, Act 1 route/gate, Act 2 music and bouncer request. E20 proves `MEAT_HUB_ACT_TWO_MUSIC_DONE` and `MEAT_HUB_ACT_TWO_BOUNCER_REQUIREMENT_STATED` were already true before the distinct cat-return completion marker changed.
- Reward/distinctness: bouncer moves; separate from music delivery and Act 2 result. No new item reward.
- Repeat/reload/reconnect/offline: E20 proves the live source transition and its separation from music delivery, plus full-restart durability and same-identity reconnect/non-repeatability. Offline completion and different-identity isolation remain pending; proposed R is specifically grounded on `MEAT_HUB_ACT_TWO_BOUNCER_REQUIREMENT_DONE`.

### 10. meat_scruffy_return

- Action/event: escort Scruffy to Act 3 bouncer; `MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE` changes false→true (E7/E24). A just-before-action F5 snapshot proves the false state, and the live event proves the transition after the distinct bouncer request flag was set.
- Access/items/events: Meat Dimension Access, Hypno Pan, observed Act 2 clear, Act 3 music and bouncer request. Crowd traversal remains recurring even once a door unlocks.
- Reward/distinctness: bouncer moves; distinct from music and Act 3 result; no item reward.
- Repeat/reload/reconnect/offline: E24 proves the exact pre-action false snapshot, live source transition, and separation from music and the bouncer's stated-requirement flag; E31 confirms the completion marker true after full restart and same-slot reconnect. Non-repeatability, offline completion and different-identity isolation remain pending; proposed R is specifically grounded on `MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE`.

### 11. meat_mouse_revolution

- Action/event: escort meat mice with Hypno Pan to the leader near the open window; cutscene sets `MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED` false→true, then the bouncer interaction sets `MEAT_HUB_ACT_FOUR_BOUNCER_REQUIREMENT_DONE` false→true (E8/E29).
- Access/items/events: Meat Dimension Access, Hypno Pan, observed Act 3 clear, Act 4 music, bouncer hint and leader interaction. Exact necessity/order of all setup predicates remains to be verified.
- Reward/distinctness: mouse cutscene advances route; no item reward. Propose one revolution check, not a second check for the bouncer's consequence or the Act 4 result.
- Repeat/reload/reconnect/offline: E27's apparent softlock was recovered in E28 by repeating the Act 4 bouncer dialogue, which restored the native requirement-stated flag and mouse leader; E29 then completed the route and confirmed the Act 4 path stayed open after a phone trip out of Meat and back. E30 confirms the route remained open and the completion flag persisted across a full game restart. The mod's separate native spawner-recovery branch was not exercised. Non-repeatability, offline completion and different-identity isolation remain pending. A future R should use the distinct `MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED` transition, not the earlier bouncer-stated flag or later bouncer-done consequence.

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

### 18. roots_star_eater_feed

- Action/event: player feed of the Roots Star Eater. After a false F5 baseline (E48), the live action set the exact `ROOTS_HUB_STAR_EATER_FED` marker false→true and then produced its native request (E49). The separate `ROOTS_HUB_STAR_EATER_BLOCKADE_REMOVED` consequence followed; dialogue introduction and blockade removal are not the feed identity. Read-only event and F5 snapshot diagnostics target the feed flag in `GameRoom_Hub2` only.
- Access/items/events: the native interaction was reachable with Roots Access in this AP-routed save (E49). E48/E51 show a displayed 3-Star requirement; E51 records interaction unavailable at 0 and successful offline feeding at 5, with no Stars spent. Exact admission at 3, refusal at 2, and complete local prerequisites remain untested. Offline feeding followed an authenticated session; cold-offline startup without AP synchronization instead reached the tutorial (E52). The current AP Roots routing suppresses a blockade object but deliberately leaves Star Eater/Trevor quest flags unchanged.
- Reward/distinctness: a separate player action, not a level completion or AP blockade bypass. The player confirmed that feeding does not spend Stars; its vanilla consequence is the path toward the phone booth/Weed Killer source already bypassed for AP navigation (E49). Do not count it as a Star-capacity location, allocate an ID, or dispatch a check while Provisional.
- Repeat/reload/reconnect/offline: E48–E50 prove online feeding and same-save restart persistence on the earlier slot 4. On the recreated slot 4, E51 proves offline feeding and E53 proves its flag survived restarts and same-identity reconnection, with feeding still unavailable. E52 separately records cold-offline tutorial arrival and failed automatic reconnect; manual restart recovery does not close those limitations. Different-identity isolation and exact threshold/local-prerequisite proof remain pending. Proposed R is the exact `ROOTS_HUB_STAR_EATER_FED` marker after native save and AP identity are validated. No AP reconciliation/check pipeline is activated or proven by these read-only observations.

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
route and actual threshold. Roots feeding has a witnessed feed transition and
same-save restart/reconnect persistence with feeding unavailable afterward
(E48–E53), including offline feeding followed by restart and authenticated
recovery. It still needs different-identity isolation and exact threshold/local-
prerequisite proof; cold-offline routing and automatic reconnect remain unresolved
(E52), as does performance (E54). The AP-suppressed
blockade is not a check. No broad campaign replay is requested by this
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
- 2026-09-14 Roots Star Eater diagnostic addition: the new feed-specific tests
  failed before the policy entry (11/13 passing), then all 13/13 passed after
  adding the exact `ROOTS_HUB_STAR_EATER_FED` candidate. Repository validation
  passed, APWorld unittest discovery passed 105/105, and the client build
  succeeded with existing nullable warnings and an unavailable NuGet
  vulnerability-service warning. No live feeding interaction was tested.
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
  `5AECA189F0F38AEE28EB8BA39DCC4D4DB6D084DF8A1D45C9B6B7E01F353FD2E3`.
  APWorld v0.24 SHA-256
  `2A3AC0830D4D2DA1BD4E773F671ABFA7B05CB4B4BBE6BABE532B3C195D837253`
  hosted the focused seed. E15 confirms the Combo Bucket action; E16 confirms
  the bounded Meat phone-route gate repair and its accepted cosmetic artifact. The
  remaining native Harmony callback/scene behavior is still a live-testing
  boundary.

## Follow-up threshold evidence — 2026-09-15

After verified backup, the user recreated native slot 4. Log confirms Creation
activated epoch 2 / slot 4 and cassette persistence verified. In response to the
instruction to try at exactly 2 stars, user reported "it is non interactable."
The displayed 2-star total was not separately confirmed before proceeding, so
this is a reported below-threshold result rather than an independently captured
exact-2 HUD observation.

User then explicitly reported "Worked at 3 stars and stayed at 3 stars after".
The live log records ROOTS_HUB_STAR_EATER_FED before=False after=True in
GameRoom_Hub2 (event, followed by request). Exact admission at 3 and no star
consumption are now supported by user observation plus the native completion
transition. Previous current-process snapshot=True belonged to the prior save
epoch and is not used as this new save's baseline.

Roots feeding remains Provisional: different-identity isolation and complete
local prerequisite proof are still outstanding. No AP ID or check was activated.
Local capture: roots-exact-three-threshold-20260915.log. The maintenance record
separately documents the repaired build's successful reconnect and improved play.

Threshold clarification: the user subsequently explicitly confirmed the blocked
interaction was at exactly 2 stars. The user-observed boundary test is complete:
blocked at 2, successful at 3, and total remains 3 after feeding. This supersedes
the outstanding displayed-total clarification above; it does not change the
remaining isolation/local-prerequisite requirements or Provisional status.
