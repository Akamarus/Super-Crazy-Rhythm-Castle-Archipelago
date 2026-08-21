# Next release bug-fix gate

This checklist records release-blocking defects confirmed during the v0.17 / Client v0.67.60 fresh-save run. An item stays open until its acceptance test passes. Only then may it move into the next release's **Fixed** changelog section.

The failed regression seed is `AP_28223804408101432968`. It is useful for reproducing solver mistakes but must not be presented as playable.

## Open release blockers

- [ ] **Prevent BK'd seed generation.** The solver must use the same fresh-save reachability enforced by the client and native game. It must not place required progression behind blocked Music Lab machines, unavailable native point thresholds, unowned Game Garage songs, or unsupported performance tiers. Re-run the failed seed scenario plus a deterministic seed matrix and prove every required item has a reachable sphere-by-sphere route.
- [ ] **Fix zero-cartridge Game Garage entry.** Entering `GameRoom_27` with no AP-owned Garage cartridges must finish loading, display the room, allow normal menu/hub exit, and keep all six unowned songs inaccessible. Then receive one cartridge and verify only its matching song becomes usable.
- [ ] **Map Level 22 completion.** Map native `Level_28` to Level 22's ordinary AP completion/performance locations. A one-Star clear must send its normal check even below the future Victory Star goal; receiving the final required Star later must not retroactively award Victory.
- [ ] **Model Music Lab construction barriers.** Identify each orange barrier's native unlock condition and affected cassette-machine group. Either represent those conditions in AP logic or implement a narrowly scoped AP normalization rule. No blocked cassette check may appear reachable to the solver.
- [ ] **Make Plant Pipes durable.** One AP-delivered Plant Pipes item must persist after Level 3 completion, return to Hub2, entry into and completion of Level 4, save reload, scene changes, disconnect/reconnect, and full game restart. Reconciliation must use received-item ownership and must never require a duplicate AP delivery.
- [ ] **Remove Plant Pipes' save-processor timing dependency.** Received-item history must reconcile after the selected save is available even when no unrelated native progression request occurs. The client must retry safely until it can verify the native ability state, then remain idempotent.
- [ ] **Split Royal Corridor reachability.** `Royal Corridor Access` reaches only the phone-side Level 22 route. The Royal Star Eater interaction, completed bridge, and Level 21 side must remain separate logical states until their native requirements are satisfied.

## Approved next-release behavior work

- [ ] Unlock both Normal and Pro difficulty choices when an AP seed starts; do not force either choice.
- [ ] Suppress the redundant Roots post-Level 1 gate/difficulty cutscene by completing only its introductory bookkeeping. Preserve Level 1's AP check and all later Roots quests.

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
