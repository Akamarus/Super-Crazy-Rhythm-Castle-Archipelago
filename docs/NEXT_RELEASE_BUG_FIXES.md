# Next release bug-fix gate

This checklist records release-blocking defects confirmed during the v0.17 / Client v0.67.60 fresh-save run. An item stays open until its acceptance test passes. Only then may it move into the next release's **Fixed** changelog section.

The failed regression seed is `AP_28223804408101432968`. It is useful for reproducing solver mistakes but must not be presented as playable.

## Open release blockers

- [ ] **Prevent BK'd seed generation.** The solver must use the same fresh-save reachability enforced by the client and native game. It must not place required progression behind blocked Music Lab machines, unavailable native point thresholds, unowned Game Garage songs, or unsupported performance tiers. Re-run the failed seed scenario plus a deterministic seed matrix and prove every required item has a reachable sphere-by-sphere route.
- [ ] **Fix zero-cartridge Game Garage entry.** Entering `GameRoom_27` with no AP-owned Garage cartridges must finish loading, display the room, allow normal menu/hub exit, and keep all six unowned songs inaccessible. Then receive one cartridge and verify only its matching song becomes usable.
- [ ] **Map Level 22 completion.** Map native `Level_28` to Level 22's ordinary AP completion/performance locations. A one-Star clear must send its normal check even below the future Victory Star goal; receiving the final required Star later must not retroactively award Victory.
- [ ] **Model Music Lab construction barriers.** Identify each orange barrier's native unlock condition and affected cassette-machine group. Either represent those conditions in AP logic or implement a narrowly scoped AP normalization rule. No blocked cassette check may appear reachable to the solver.
- [ ] **Make Plant Pipes durable.** One AP-delivered Plant Pipes item must persist after Level 3 completion, return to Hub2, entry into and completion of Level 4, save reload, scene changes, disconnect/reconnect, and full game restart. Reconciliation must use received-item ownership and must never require a duplicate AP delivery.
- [x] **Remove Plant Pipes' save-processor timing dependency.** Client v0.67.61 constructs the stateless native request processor only after selected-save enquiries are readable, queues AP callbacks without touching Unity/native state off-thread, retries on the Unity thread, verifies `WEED_KILLER_ABILITY=True`, and remains idempotent. Live slot-4 testing on 2026-08-21 restored Plant Pipes from received-item history without another AP delivery and retained the usable ability after a full game restart.
- [ ] **Split Royal Corridor reachability.** `Royal Corridor Access` reaches only the phone-side Level 22 route. The Royal Star Eater interaction, completed bridge, and Level 21 side must remain separate logical states until their native requirements are satisfied.

## Approved next-release behavior work

- [ ] Unlock both Normal and Pro difficulty choices when an AP seed starts; do not force either choice.
- [ ] Suppress the redundant Roots post-Level 1 gate/difficulty cutscene by completing only its introductory bookkeeping. Preserve Level 1's AP check and all later Roots quests.
- [ ] **Fix first Roots arrival timing.** Client v0.67.62 can reach Hub2 before `PlayerSaveRequestProcessor` is available, causing the `ROOTS_HUB_INTRO_WITNESSED` write to occur after the arrival cutscene has already started. Queue and verify the flag before the transition completes.
- [ ] **Restore the AP Star HUD.** Fresh v0.18 testing lost the visible Star counter after Level 3 returned to Roots. Identify whether fresh-save direct start, level-result persistence, or AP Star synchronization hides the counter, and keep the AP-owned Star total visible.

## Required regression run

After all blockers above pass automated tests:

1. Build and install the new client only to `BepInEx\plugins\RhythmCastleAP`.
2. Package and install the matching APWorld.
3. Generate a fresh seed and use a fresh in-game save.
4. Verify BepInEx, version compatibility, slot-data, and server connection.
5. Complete the Weed Killer -> Frog/Hippo -> Plant Pipes -> Level 4 -> Hip Glasses -> Lift Quest -> Bucket Minion -> Chicken Bucket route without admin item commands.
6. Enter and exit Game Garage first with zero cartridges and then with one AP-owned cartridge.
7. Verify accessible and barrier-blocked Music Lab groups match solver reachability.
8. Clear Level 22 for one Star without abilities and confirm the ordinary AP check.
9. Restart the game at multiple points and confirm all received progression remains durable.

The release notes must list only the items whose acceptance tests passed. Any remaining unchecked item stays under known issues.

## Verified repair evidence

- **Plant Pipes timing/restart reconciliation:** passed on 2026-08-21 with Client v0.67.61 against the retained v0.17 seed and save slot 4. The selected save already contained the completed Level 3 source marker and previously completed Level 4 route. On load, the client verified `LEVEL_07_WK_ABILITY_EARNED=True`, restored `WEED_KILLER_ABILITY=True` from AP ownership, reached Music Lab normally, and Plant Pipes were usable in Roots. After a normal close and full relaunch, the same save again reached Music Lab and Plant Pipes remained usable. The broader durability blocker remains open until the complete receive -> Level 3 -> Hub2 -> Level 4 sequence is replayed end to end on the repaired build.
- **Fresh Plant Pipes route:** passed through Level 4 entry/exit on 2026-08-22 with Client v0.67.62 and a fresh v0.18 seed/save. Frog/Hippo sent the AP source check while the vanilla ability grant stayed suppressed; one admin-routed AP Plant Pipes receipt set `WEED_KILLER_ABILITY=True`; the ability remained usable through Hub2 -> Level 4 -> Hub2, normal shutdown, reconnect, and save load. Completing Level 4 and persisting its result remains the final unchecked durability step.
