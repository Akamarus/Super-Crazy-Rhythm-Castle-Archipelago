# v0.67.64 / v0.19 Consolidated Preview Acceptance

**Status:** Installed and manually exercised; focused repair follow-up required
**Client:** `0.67.64`
**APWorld:** `0.19`
**Implementation:** `area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19`

Use one fresh generated seed and one fresh in-game save. Retain the server console and `BepInEx/LogOutput.log`. Do not mark a release blocker fixed merely because its automated test passes.

## Before play

- [x] Install only the matching APWorld v0.19 package and Client v0.67.64 plugin after explicit owner approval.
- [x] Confirm the first client version line is `[SCRC-AP] v0.67.64 loading.`
- [x] Confirm the slot-data implementation string exactly matches the value above.
- [x] Confirm `CONNECTED` appears and no compatibility warning follows it.

## Ordered acceptance route

1. **Fresh direct start**
   - [x] Create a fresh save and verify it reaches Music Lab without either tutorial mini-level.
   - [x] Confirm Music Lab shows Music Lab Points, not the campaign Star counter.
   - [x] Confirm both REG and PRO are available in native character selection; do not force either choice.

2. **Roots presentation and HUD context**
   - [ ] Enter Roots and confirm the first-arrival and redundant post-Level-1 presentations do not lock or replay unnecessarily. **Failed:** the first-arrival scene replayed after a full restart; the redundant post-Level-1 scene remained suppressed.
   - [ ] Confirm the campaign Star counter appears in Roots after synchronized startup bookkeeping. **Failed:** the Star counter remained absent during the focused Roots checks.
   - [ ] Check the combined bottom character/difficulty control at direct start, after Level 1, after a hub reload, and after a full restart. **Failed early / recovered later:** it was visible but non-interactive in Music Lab and Roots, then became interactive after Level 22 completion.
   - [ ] If it is unavailable, preserve the matching `BOTTOM HUD SNAPSHOT` lines. Do not add or test a guessed state mutation during this run.

3. **Connection recovery**
   - [ ] With the game connected, stop and restart only the AP server.
   - [x] Confirm the client reconnects without a game relaunch using the 1/2/5/10/30-second schedule.
   - [x] Confirm already received Stars/items are not duplicated after recovery. The queued-during-outage subcase was not exercised.

4. **Ability ownership**
   - [ ] Preserve the prior verified Level 3 and Plant Pipes route; do not replay Level 3 solely for this suite.
   - [ ] Confirm existing AP-owned Plant Pipes remains usable after reconnect and full restart. Replay completed-Level-4 persistence only if the owner elects to close that remaining durability boundary.
   - [ ] Manually send `Hypno Pan`; confirm one `PREVIEW ABILITY RECEIVED` and one `PREVIEW ABILITY VERIFIED`, then validate a representative Hypno Pan interaction.
   - [ ] Manually send `Violance`; confirm the same two log stages, then validate a representative Violance interaction.
   - [x] Restart once and confirm both delivered preview abilities remain available without duplicate sends. Native availability was confirmed; representative ability-specific interactions remain untested.

5. **Music Lab and Game Garage overlap**
   - [x] Confirm both known orange construction-barrier roots are bypassed only in the compatible AP session.
   - [ ] Confirm cassette checks follow the selected AP difficulty and unsafe tiers do not hold required progression.
   - [x] Enter Game Garage through its required physical entrance cartridge, first with zero AP-owned Garage songs and then after receiving one cartridge.
   - [x] Confirm the room loads/exits normally and only the matching AP-owned song becomes usable. Vampire Killer was the one-owned-cartridge case.

6. **Royal split and Level 22**
   - [x] Receive Royal Corridor Access and confirm the phone-side Level 22 route is available.
   - [x] Confirm Level 21, the Royal Star Eater interaction, and the completed bridge are not treated as unlocked by Royal Access alone.
   - [ ] Perform one controlled failed Level 22 result and confirm no AP location is sent.
   - [x] Only when the owner is ready, perform one successful one-Star clear and confirm exactly `Level 22 - Completion` plus `Level 22 - 1 Star`; do not expect Victory.

7. **Level 22 native rewards and Bunker route (supplemental evidence)**
   - [x] Confirm Level 22 sets `OVERALL_PROGRESS_BEAT_KING_ONE` and King Ferdinand appears in character selection.
   - [x] Confirm Level 22 grants `LIFT_KEY_BAG_ITEM` and `LEVEL_28_KEY_CARD_COLLECTED`.
   - [x] Enter the Secret Bunker from Lobby, confirm it loads correctly, and confirm the Bunker Keycard is consumed.
   - [ ] Enter Lobby through AP-granted Lobby Access without replaying the vanilla Lift Quest arrival presentation. **Failed:** the first-arrival cutscene played. Preserve the native Lift Quest arrival path; suppress only early AP-phone arrival.

## Evidence to retain

- `BepInEx/LogOutput.log` from startup through shutdown.
- AP server output covering disconnect, reconnect, item sends, and checks.
- Generated seed name, YAML, APWorld version, client version, and save slot.
- Screenshots for any incorrect HUD, blocked route, loading failure, or unexpected vanilla inventory state.
- A short note for every unchecked step explaining whether it failed, was skipped, or remains untested.

## Stop conditions

Stop the run and preserve evidence if the save enters the tutorial unexpectedly, the game cannot exit a room, progression inventory disappears, AP history duplicates after reconnect, or the player becomes BK'd with only unsupported performance checks available.
