# Quest checks and Garage follow-up candidate — 2026-09-16

## Status and compatibility

Current release is Client v0.73.9 / APWorld v0.26.0. The client is installed and
Garage behavior is accepted on the retained schema-16 seed; the new schema-17
quest features still need fresh v0.26 seed and native-save gameplay acceptance.
Existing schema-16 seeds retain their original character-item behavior and do
not gain new quest checks or rewards. Versioned entries below are historical
candidate and acceptance records; their installation statements refer to that time.

Slot data uses schema 17, campaign mapping schema 1, character quest item schema
1, and `quest_checks_schema: 1`. The historical implementation string is retained
and gains suffix `-quest-checks-0.26`. The original character input maps remain:

| Input | Native bag flag | Existing source |
| --- | --- | --- |
| Old Game Data | `LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM` | Music Lab - 5 Point Chest (`187256169`) |
| Car Battery | `CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM` | Music Lab - 20 Point Chest (`187256171`) |

`quest_items` exports these exact name-to-ID pairs:

| AP item | ID | Classification |
| --- | ---: | --- |
| Plunger | 187256161 | Useful |
| Meoo | 187256162 | Useful |
| Maniac | 187256163 | Useful |

`quest_locations` exports these exact name-to-ID pairs:

| AP location | ID | Solver rule / placement |
| --- | ---: | --- |
| Lobby - Plunger Pickup | 187256294 | Lobby Access; Stardust-only |
| Lobby - Car Battery Hand-In | 187256295 | Lobby region + Car Battery; progression allowed |
| Game Garage - Old Game Data Hand-In | 187256296 | Existing Garage access + Old Game Data; progression allowed |
| Roots - Star Eater Fed | 187256297 | Roots region; Stardust-only |

Old Game Data and Car Battery become progression inputs. Existing chest checks
are reused once; no duplicate chest locations are added. Meoo and Maniac are
independent AP rewards, never prerequisites for hand-in. Active totals are
Normal 125 / Hard 183 / Expert 241 / Perfection 277; each pool has 71 non-Stardust
items. Next safe item/location IDs are `187256164` / `187256298`. Reserved Lobby
IDs remain inactive. AP Stars, generated Star gates, and final Star Victory are
inactive.

## Native mapping evidence and limitations

Read-only metadata and Unity asset audit: local workspace
`work/new-quest-checks-native-map-20260916.md` (not a shipped game-asset artifact).
This metadata evidence does not replace gameplay acceptance.

- Plunger inventory is `PLUNGER_BAG_ITEM` (flag 106), source is `OUTSIDE_HUB_PLUNGER_COLLECTED` (700), and consumption terminal is `LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED` (601). The source is in Hub1B. Exact phone/button traversal prerequisites remain unmodeled, so its check holds only Stardust. No Vault/cassette prerequisite is inferred.
- Meoo is native character `GHOST_CAT` (5); the hand-in consumes Car Battery in Hub1A. Its native interaction checks the original chest flag instead of inventory. The candidate scopes its override to the audited condition so received Battery can work before opening the chest and opening the chest alone cannot substitute for Battery.
- Maniac is native character `GUITAR_MANIAC` (7); the hand-in consumes Old Game Data in `GameRoom_27`. Native record-unlock and popup sequence steps are separated from consumption.
- Roots feeding uses `ROOTS_HUB_STAR_EATER_FED`, independently of later blockade removal. Native threshold asset `StarEaterThreshold_RootsHub` is exactly 3. The threshold remains unchanged, no stars are consumed, and AP Star items do not supply it. Native earned-star availability is not modeled in the solver, hence Stardust-only placement. The exact live 2/3 boundary remains untested.

For the new contract, character unlock ownership must not stand in for hand-in
completion. Early AP character receipt must neither hide the hand-in nor prevent
its input from appearing. Completion is tracked independently through confirmed
checks and pending source state, scoped to AP identity and selected native save.
Old-schema native predicates retain their prior behavior. Plunger source
availability is scoped to collected source state, independently of early AP
ownership or consumption. Native rewards/popups must not produce duplicate or
misleading character unlocks. Narrow condition overrides and persistence remain
candidate safeguards until the following gameplay tests pass.

## Acceptance still required

- [ ] Suppress each native reward while preserving exactly one authoritative source check; AP receipt grants the corresponding inventory or character.
- [ ] Receive Meoo/Maniac before the quest input and before its Music Lab chest; the later input and normal hand-in remain available.
- [ ] Receive input before opening its original chest; hand-in works. Opening the chest without receiving input cannot satisfy hand-in.
- [ ] Complete each hand-in before receiving its character reward; no native unlock/popup leakage, no duplicate check, and no bag refill after reload/reconnect/restart.
- [ ] Verify the Maniac console/card remains interactable after early AP Maniac receipt and the native sequence completes without stalling.
- [ ] Receive/use Plunger before visiting its source; source remains collectible once, and consumed Plunger never refills after restart.
- [ ] Roots feed fails at 2 and succeeds at 3 native earned stars; threshold and star total are not altered. Blockade traversal alone never sends the check.
- [ ] Complete offline, restart, reconnect, and verify pending checks survive only for the matching AP identity and native save; changed identity/save does not inherit completion.
- [ ] Schema-16 and non-AP regressions preserve existing behavior; malformed schema-17 quest claims cannot enable partial randomized behavior.

## Garage follow-up — separate unresolved acceptance

The same client candidate includes native door-consumption handling and targeted
read-only saved-score detection. Neither change proves the Garage score-reporting
issue is fixed. Verify cartridge insertion/door transition and persistence, then
capture one controlled score attempt and compare native saved score, detected
tier, and AP check. No native score edits or invented medal results are part of
this acceptance. Existing evidence and unresolved issues remain in their dated
records.

## Automated verification

APWorld quest tests exercise exact maps, input classification, pool capacity,
hand-in rules, and conservative placement. The repository validator independently
checks maps/schema and builds each difficulty against lightweight AP boundaries.
These checks are source validation, not generation with an installed APWorld and
not gameplay acceptance. The full APWorld suite passed 126/126 tests, including
packaging and validator mutation checks; the repository validator passed for
Client 0.73.0 / APWorld 0.26.0 and all four expected difficulty totals. Quest
feature tests and validator mutation tests were observed failing before their
implementations and passing afterward. These runs did not install a world,
generate with an installed world, or launch the game.

## Client candidate and deployment checkpoint

Final client v0.73.0 DLL SHA256:
2043DEC7F573D85AA6D2FC3CA6C0FFA7DF51E58D0F2130FC29D9EEF52C425CD9.
Candidate archived under task outputs/quest-checks-v0.73.0-world-v0.26.0 with
three-DLL client ZIP, scrc.apworld, checksums and install/test notes. Not installed.

All 25 client test projects passed in the full maintenance run. Subsequent added
admission/storage failure runtime coverage passed 29 assertions; quest contract,
journal and isolation coverage passed 38 assertions plus malformed-history tests.
The final build passed with zero errors, four pre-existing nullable warnings and
an unavailable NuGet audit metadata warning. Final review found no further concrete
issues; live native hook/sequence behavior remains an acceptance requirement.

Hand-in intent is persisted before native interaction/sequence admission. Disk
failure rejects admission; completion-write failure retains durable pending state
and retries. Journals bind AP identity to one native slot. Different selected slots
cannot supply source flags. New binding rejects pre-existing pickup/feed markers.
A first Plunger pickup binds before source inventory suppression. Early character
ownership is independent of hand-in completion and native source availability.
The exact Meoo interaction and both hand-in sequence entry points are guarded;
only the two native character popup Trigger methods are suppressed, preserving the
base sequence lifecycle. Legacy schema-16 does not enable these new behaviors.

Garage candidate observes the exact native RemoveBagItemIfOwned call. It records
insertion only after verified held-to-absent native consumption with matching AP
and save epoch. Native score and medals are read after results without writes.
Normal difficulty's absent Silver check is explicitly skipped in the sender.
Next: explicit approval to install this exact client, then controlled Bloody Tears
entry/result/re-entry test on the current retained seed. New quest tests need a
separate freshly generated v0.26 seed and fresh native save later.

## v0.73.1 native hook repair candidate (2026-09-16)

v0.73.0 was installed with approval and connected to the retained schema-16 seed.
Startup failed to attach Switch.ProcessInteraction and the Garage consumption hook.
Installed interop reflection reproduces TypeLoadException for both classes: their
WorldEntity generic StateClass constraints reject the generated state types.
The native methods are present; changing reflected method names cannot repair this.

v0.73.1 removes the redundant Switch hook. Both exact hand-in sequences remain
guarded at Begin and ResetAndBegin, persisting pending intent before their bodies.
All seven remaining managed hook signatures resolve against installed interop.

The Garage hook resolves native Room27LevelEntranceDoor metadata directly and uses
BepInEx INativeDetour. It validates the declaring class, nongeneric instance method,
void return, single eBagItemType argument, enum width and nonzero code pointer.
UnityVersionHandler supplies the version-specific method layout. Delegates and the
detour remain rooted. The native method runs exactly once even if observation or
diagnostic logging fails. No generated door wrapper or modified game DLL is needed.

Focused Garage persistence and quest tests pass. Build passes with the existing
four nullable warnings and unavailable NuGet audit metadata warning. Native hook
installation and cartridge/score persistence still require gameplay acceptance.
v0.73.1 is a client-only candidate; no server, seed, save, APWorld or IDs changed.

## v0.73.2 crash repair candidate (2026-09-16)

v0.73.1 installed with approval and connected, but crashed loading slot 4. Windows
recorded stack overflow c00000fd. Read-only dump stack analysis found about 1266
repeated IL2CPP runtime invocation frames. Game frame mapping via installed
MethodAddressToToken.db identifies SetInactiveOnRoomInitialise.HandleEvent and
IsGameProgressionFlagSetCondition.CheckIfMet. The generated condition wrapper
redispatches virtually; a Harmony original path can re-enter the patched method.

v0.73.2 replaces all five newly added virtual wrapper hooks with validated native
trampolines: three CheckIfMet methods, character-popup Trigger, Sequence.Begin.
The nonvirtual character save request and Sequence.ResetAndBegin retain Harmony.
Native bool return is explicitly marshalled as I1; delegates/detours remain rooted.
Legacy and unrelated conditions return the original native result directly. Unknown
or unreadable quest sequence paths fail closed before mutations; errors are logged.
Garage consumption hook remains unchanged and had not been reached at the crash.

Build and focused Garage persistence, QuestRuntime and QuestChecks tests pass,
including eight original/override condition cases and three sequence admission cases.
Static review found no ABI/lifetime defects; its path-error finding was corrected.
This candidate is NOT installed. Next requires explicit candidate deployment approval,
then slot-4 loading acceptance before any Bloody Tears gameplay test.

## v0.73.3 shared native hook repair candidate (2026-09-16)

Rollback to verified v0.72 succeeded: user confirmed slot 4 no longer crashes.
Installed MethodAddressToToken.db, filtered to Assembly-CSharp, maps both
IsGameProgressionFlagTrueCondition.CheckIfMet and
IsGameProgressionFlagSetCondition.CheckIfMet to native RVA 0x7D94D0. These
distinct managed wrappers alias the same native function. v0.72 already patches
the True wrapper for area routing; v0.73 additionally patched the Set wrapper.
v0.73.2 changed that second patch to a native detour but still patched the same
address twice. This conflict explains why changing wrapper dispatch alone failed.

v0.73.3 keeps the existing area hook as sole owner. Its exact CheckIfMet prefix
dispatches the narrow quest override before area routing. Set is removed from the
dedicated native hook plan. Native alias equality and signature are verified before
patching, and quest readiness requires successful installation of this shared hook
plus six dedicated hooks. The quest installer runs after area-hook installation.
No extra Set detour is installed, even for legacy seeds.

An alias-collision regression test fails with the previous three-condition plan and
passes with the two-condition plan plus the shared owner. Static review found no
concrete integration defects. Music Lab score interception guards remain intact;
the obsolete project-wide detour-dependency ban was removed because unrelated
quest/Garage hooks now use the game-provided detour runtime.

v0.72 remains installed. v0.73.3 is not live-accepted; next is explicit deployment
approval followed only by slot-4 loading/movement stability before Garage testing.

## v0.73.4 Garage initialization candidate (2026-09-16)

User confirmed v0.73.3 slot-load stability, then reported a Garage loading hang.
Player.log records NullReferenceException in CarriableObject.HandlePutOnHolder ->
CarriableObjectHolder.StartHoldingObject -> Room27GameCartridgeHolder.HandleEvent
(GameRoomReadyToRunEvent) -> GameRoomManager.PrepareFreshlyAddedGameRoom.
The AP keeper had already changed cartridge visibility/release and reconciled bag
items before native preparation completed. This establishes overlapping mutations,
not proof that the timing conflict is the only cause of the native null reference.

v0.73.4 gates both Garage bag reconciliation and all keeper SetActive/release calls
until successful synchronous native room preparation completion. The exact installed
PrepareFreshlyAddedGameRoom(GameRoomIdentifier) method is loadable, nonvirtual and
returns void. Gate epochs reset at every observed transition. Old completion cannot
release a newer visit; failed preparation stays closed. Outside-Garage AP receipt
reconciliation remains available so ownership is established before entry.

All 25 client test projects passed, including initialization, missing-completion and
stale-visit regression cases. Build passes with the existing four nullable warnings
and NuGet audit metadata warning. Review found no missed cartridge mutation path.
No installation yet. Next: explicit candidate deployment approval; verify Garage
entry and exit/re-entry before the still-pending Bloody Tears score/consumption test.

## v0.73.5 Garage readiness correction candidate (2026-09-17)

v0.73.4 entered and re-entered Garage without hanging, but user confirmed Bloody
Tears stayed in inventory and unavailable. Logs show no preparation-completed
notification and no in-Garage AP reconciliation: the readiness gate never reopened.
Thus successful room loading alone did not validate the prior loading repair.

v0.73.5 replaces the missed preparation callback with a postfix on nonvirtual
GameRoomManager.UpdateRoomChangeTasks(). It reads currentGameRoom and currentTask
after the native update and opens only for native GameRoom_27 plus task NONE in the
same observed transition epoch. Missing/old-room/loading observations stay closed.
The observer logs changed readiness states while waiting and stops reads once open.
No preparation-completion callback is required. Both cartridge mutation guards remain.

Installed interop verifies void/no-argument/nonvirtual target, readable properties,
and enum NONE=0. Build and Garage persistence tests pass, including eight readiness
cases for initial blocking, idle current room, missed preparation callback, loading,
unreadable data, stale completion and re-entry. Bounded review found no concrete
defect. Native callback execution and cartridge availability remain unaccepted.
Next: explicit approval to install, verify native readiness opened=True and Bloody
Tears availability before any song replay. Consumption/score persistence still open.

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

### v0.73.9 Garage cleanup candidate (2026-09-17)
Removed superseded manual cartridge release/spawning helpers, unused release-visit coordinator and its obsolete tests, ineffective sequence/song diagnostic hooks, repeated saved-score dumps, and room-readiness trace spam. Removed the unused release-state set and dead bag-observation wrapper. Preserved native holder initialization, door consumption tracking, save/connection isolation, readiness guards, saved-medal preview reads, active cartridge check detection, and error reporting.
All 25 client test projects passed during cleanup; affected Garage persistence, Garage availability and Music Lab Points suites passed after diagnostic hook removal. Final bag-wrapper cleanup passed Garage persistence tests and Release build (four existing nullable warnings and one unavailable NuGet audit metadata warning). No new IDs, seed changes, save edits, or server changes. Packaged candidate only; installed v0.73.8 remains the gameplay-accepted baseline.
Next: explicit approval to install v0.73.9, followed by a brief Garage entry/exit and medal-preview check on retained seed/slot 4. New schema-17 quest acceptance remains separate.

### v0.73.9 installed and gameplay accepted (2026-09-17)
Installed with explicit user approval; DLL SHA256 16278AF9AE8CD10C6F8FD8BBD4AF34430AEFAB9FC54A0CE534C7912C331E5D74 verified. Startup confirmed v0.73.9, native OpenDoor/holder hooks, and Jack connected to retained localhost server PID 876. User confirmed the requested Garage entry/exit check: Bloody Tears and Gradius remain playable, consumed cartridges stay out of inventory, and preview medals remain intact. v0.73.9 is now the accepted installed client baseline. Previous v0.73.8 restart acceptance remains recorded above; no additional restart test is claimed for v0.73.9.
Local deployment evidence: work/v0739-deployment-result.txt and work/v0739-startup-verified.log in the task workspace. Existing seed and save retained. Next: prepare separate fresh-seed/native-save acceptance for schema-17 quest checks and randomized Plunger/character unlocks; no reset or new-seed switch performed.

### v0.73.10 native character reward suppression candidate (2026-09-17)
Fresh schema-17 test seed51523264072706873316/slot4 confirmed early Maniac and Plunger receipt, then Light Humor granted Old Game Data and Car Battery. Old Game Data hand-in after early Maniac completed and sent its check/reward without another popup. Battery hand-in sent its check/Rainbow Melodies but incorrectly made Meoo selectable without receiving its AP item.
Native disassembly shows RecordCharacterUnlockedValueInSaveDataRequest.AcceptProcessor (RVA0x7DB3F0) inlines save mutation and bypasses PlayerSaveRequestProcessor.ProcessRequest (0x7E2D30), so the existing request prefix cannot prevent vanilla sequence rewards. The logged Meoo story flag is a separate sequence operation, not sufficient evidence of character ownership storage. Intercept native UnlockPlayableCharacterSequenceStep.Trigger (0x676AA0) only at the two audited Meoo/Maniac hand-in component paths and only under an enabled, bound quest save. Derived Trigger suppression leaves base sequence completion, bag consumption, story flags, popup suppression and persistence intact. AP-delivered character records still use the existing guarded grant route. Hook readiness now requires seven dedicated hooks.
Regression first failed on the native-dispatch bypass simulation; repaired runtime suite passes 37 assertions covering both paths, legacy/unbound/unrelated cases and AP receipts. Quest contract/native dispatch, character item, connection, and Garage persistence suites passed; Release build and repository validator passed. Five pre-existing build warnings remain. No permanent IDs/schema change, no save edits, no automatic removal of an already leaked unlock. Not installed or gameplay accepted. Published v0.26.0-dev remains client0.73.9; do not silently replace its assets.
Next: explicitly approved candidate installation, then fresh test state to exercise a locked Meoo hand-in and subsequent separate AP receipt. Existing leaked Meoo cannot establish successful prevention retroactively.

### v0.73.10 Meoo hand-in prevention accepted
2026-09-17: v0.73.10 installed (E151BDDA3C026B0D027DEEA687C46254FAD18ADE56A25BE02D1D428D9D8FE4BC), native reward Trigger hook and all7 quest hooks verified. Fresh retest seed54652018976962924697, server38284/PID26052, recreated slot4. Sent exactly one requested Car Battery; native inventory grant verified. User confirmed hand-in now awards cassette while Meoo stays locked. Logs confirm one Battery Hand-In check187256295, Hippo and Frog Cassette187256130, Car Battery consumed=True held=False. Prevention and native sequence completion accepted for this live Meoo path. Separate AP Meoo receipt and reload persistence remain pending.

Meoo follow-up: user confirmed title reload retained locked character and absent battery. One Meoo AP item was then sent on retest seed54652018976962924697; receipt and GHOST_CAT unlocked=True were logged. User confirmed Meoo selectable and battery absent. Late AP character receipt after completed hand-in is accepted; full-process restart and other matrix cases remain separate.

Plunger early-use acceptance (v0.73.10): user confirmed the pre-received Plunger cleared the Lobby blocker and was consumed before visiting its source. Original pickup remained available, sent one Lobby - Plunger Pickup check187256294 with Stardust, disappeared, and did not refill inventory. Native flag transitions and server reward verified; full process persistence remains pending. Evidence retained locally in work/meoo07310-retest/plunger-early-use-pickup-accepted.log.

Roots feed acceptance (v0.73.10): user confirmed one Stardust reward, unchanged native star total, and non-repeatability. Native feed flag false->true, check187256297 and server delivery verified on retest seed54652018976962924697/slot4. This confirms the live AP feed check; exact2/3 admission uses the separately recorded earlier native-boundary test. Full client restart and offline/identity tests remain pending.

2026-09-17 full-process restart accepted on v0.73.10: user confirms Meoo selectable, Car Battery and used Plunger absent, original Plunger pickup gone, Roots feeding unavailable. Fresh startup log confirms7/7hooks, reconnect to38284/Jack, new native save identity2:1:4:251BBA13370, Car Battery consumed=True held=False, GHOST_CAT unlocked=True. Server records disconnect/rejoin. No additional quest check deliveries observed in restart tail. Evidence: work/meoo07310-retest/full-restart-accepted.log. Still pending: Maniac hand-in before AP character receipt, early Meoo receipt before hand-in, offline completion recovery, and different-identity isolation.

### v0.73.11 randomized battery chest admission candidate (2026-09-17)
User reported unopened/glowing20-point chest unavailable at20APpoints after separately receiving Meoo. Native asset audit confirms CatBatteryChest interaction blockCondition is a compound containing collected flag1102 and character5 ownership; visuals/CanBeOpened inspect collection/points separately. Thus AP Meoo can block this still-unchecked source. Exact condition path: Root/GameRoom_Hub6_Logic/Objects/RewardChests/CatBatteryChest/Interaction/Condition/AlreadyHaveCatCharacter. The5-point chest blocker has only collected flag1104.
Under enabled schema17 quest state with bound native save, override only that Meoo-owned condition to false using the existing native character-condition hook. Preserve points, chest collection flag, all other conditions, source check and vanilla reward suppression. Non-AP/legacy behavior remains unchanged. No extra detour or forced interaction.
Regression failed before repair and passed afterward; QuestChecks45 assertions, QuestRuntime37, native dispatcher tests and MusicLabPoints suite passed. Release build passed with five existing warnings. Candidate not installed/gameplay accepted. Next: approve installation and retry the same unopened20-point chest on retained seed54652018976962924697/slot4; expect one Stardust reward and no Car Battery restoration. No new seed or save reset needed.

Review correction: both outer native Hook.Invoke and inner ConditionPrefix now share IsConditionRoom including Hub6. Regression checks cover Hub6 routing and both production call sites; policy-only testing initially missed the outer exclusion. Final packaged DLL SHA256 BA49AD561CCAAF6D11D4AA7934364925B92EF0F2431D9BF1C06A2A06F6AB8A31. No deployment yet.

v0.73.11 installed with explicit approval. Installed DLL hash matches BA49AD561CCAAF6D11D4AA7934364925B92EF0F2431D9BF1C06A2A06F6AB8A31; startup confirms7/7 quest hooks and connection to retained seed server38284/Jack. Plugin/config/native saves backed up; server and slot4 preserved. Same unopened20-point chest gameplay retest pending.

2026-09-17 v0.73.11 battery chest repair accepted: user confirms same previously blocked unopened20-point chest opened with AP Meoo owned, awarded Stardust, and did not restore Car Battery. Client verifies check187256171 sent; server records one Stardust delivery. Repeated native event/request observations deduplicated to one AP send. Evidence: work/meoo07310-retest/v07311-chest-accepted.log. Remaining matrix includes Maniac hand-in before character receipt, early Meoo before hand-in, and offline completion recovery/identity isolation.

Maniac hand-in-before-receipt accepted on v0.73.11, fresh seed55770302205770871963/slot4: user confirmed locked Maniac, normal Old Game Data hand-in, no native unlock/popup, and Stardust reward. Log verifies consumed=True held=False and one check187256296; server confirms reward. A separate Maniac AP item is sent next for availability testing; persistence remains pending.

### v0.73.11 character order and offline pickup acceptance (2026-09-17)
On seed55770302205770871963/slot4, the user confirmed separate Maniac AP receipt makes the character selectable while consumed Old Game Data remains absent. Both persisted after a full process restart; native log verifies GUITAR_MANIAC unlocked=True and Old Game Data consumed=True held=False.

Meoo was then delivered before Car Battery. Native Meoo unlock was verified before sending the battery. User confirmed the normal Lobby hand-in consumed the battery, awarded one Music Lab Point, retained Meoo availability, and showed no repeated unlock popup. Client and server verify exactly one check187256295 reward. Evidence: work/maniac07311-test/meoo-early-unlock-handin-accepted.log.

For the offline source test, the same local server shut down normally with /exit while the game remained running. The user collected the original Plunger pickup offline; native source observation queued Lobby - Plunger Pickup. Restarting the same server seed and APSAVE caused automatic client reconnection without restarting the game. Check187256294 sent, the server awarded exactly one Stardust, and a duplicate local observation was ignored. User confirmed the reward notification, absent pickup, and no vanilla Plunger in inventory. Evidence: work/maniac07311-test/offline-plunger-collected.log and offline-plunger-reconnected.log.

These cases are gameplay accepted. Different-identity isolation remains pending live acceptance; existing automated wrong-slot and journal isolation coverage is not a substitute for that gameplay test. Offline collection across a client process restart was not tested in this sequence. Published v0.26.0-dev assets remain unchanged.
