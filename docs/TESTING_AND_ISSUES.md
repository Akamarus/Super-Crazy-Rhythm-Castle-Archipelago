# Public development testing and issue reports

> [!WARNING]
> This is an unofficial, experimental development build—not a release. Back up your save and expect incomplete logic. The project uses AI-generated code under human direction; gameplay testing remains the acceptance criterion.

Use this guide for a focused public smoke test and for reporting a problem. For installation, see the [public installation guide](INSTALL.md). For the broader implementation boundary, see the [roadmap](ROADMAP.md). The [advanced developer testing checklist](TESTING.md) is available when a maintainer asks for targeted diagnostics.

Confirmed release blockers and their required acceptance tests are tracked in [NEXT_RELEASE_BUG_FIXES.md](NEXT_RELEASE_BUG_FIXES.md).

Client v0.68.0 / APWorld v0.22.0 provides an experimental test implementation for all 30 Music Lab cassette items and sources while preserving the physical vanilla Vampire Killer pickup and normal Game Garage entrance. Many individual routes remain manual verification pending. Use a fresh v0.22 seed.

## Before starting a smoke test

Use matching source builds and record the versions you actually use:

- Client: `0.68.0`; confirm `<GameDir>\BepInEx\LogOutput.log` contains `[SCRC-AP] v0.68.0 loading.` (the first `[SCRC-AP]` version line should identify this client version).
- APWorld: `0.22.0`.
- Slot-data implementation tag: `area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22`.
- A **freshly generated seed** after any APWorld replacement or update. Replacing an installed `.apworld` does not change an existing seed.
- A **fresh in-game save** for the first pass, especially when testing first arrivals, story scenes, or source checks.
- Record the AP YAML `difficulty`. Normal/Hard/Expert/Perfection address 92/129/166/202 locations. Inactive checks are absent, not filler; native REG/PRO remains player-controlled.
- For the reward-chest reconciliation portion below, leave `[Developer] EnableTestHarness = true`. This is the generated client configuration's current default. If it has been changed to `false`, normal chest collection still works, but the automatic Hub6 reconciliation and the Hub6 `F5` diagnostic are unavailable.

Follow the [installation guide](INSTALL.md) to build and install both components, generate the seed, and configure the client. Use the room's real host, port, slot name, and password locally; do not publish a password or a complete config file.

## Public smoke test

Run these steps in order where the seed allows. A received progression item may belong to a different player or be placed later in your own world, so a source check need not deliver its matching item immediately. Record the exact step, level, or song at which a result differs from the expectation.

1. **Start and connect.** Launch the game with the new save and connect to the room. Confirm the client version line above, normal connection/login lines, and the `area-routing-plant-pipes-0.15` slot-data implementation in `LogOutput.log`.
2. **Confirm the Hub6 and Garage start.** Verify that the save starts at Hub6, Vampire Killer is available, Game Garage enters without a black screen, and Vampire Killer is playable. Exit and re-enter once. APWorld v0.22.0 random currently selects only validated Roots Access.
3. **Travel to Roots.** Use the Roots phone. The first trip should not leave the player unable to move because of the displaced arrival cutscene. The current prototype also permits the Roots traversal baseline around the first area gate and Star Eater blockade.
4. **Test Gecko's source.** Reach Gecko in Roots. The interaction should send `Roots - Gecko's Weed Killer`; it must not directly give the native Weed Killer reward. Look for `ROOTS WEED KILLER SOURCE AP CHECK` in the log.
5. **Test delivered Weed Killer.** When the room delivers `Weed Killer`, verify that the client applies the native item and that vanilla progression can use it to open Level 3. The relevant confirmation is `ROOTS WEED KILLER NATIVE GRANT APPLIED`.
6. **Test the Level 3 partial route.** Enter Level 3 and reach Frog/Hippo. Their interaction should send `Roots - Level 3 - Frog and Hippo`, without directly granting Plant Pipes. If `Plant Pipes` has not arrived yet, use the normal menu exit: leaving the level at this point is intentional and is the expected partial-level behavior.
7. **Test Plant Pipes and Level 3 completion.** After the room delivers `Plant Pipes`, verify `ROOTS PLANT PIPES NATIVE GRANT APPLIED`, return to Level 3, and complete it. This confirms that Plant Pipes is required for completion, not for reaching the Frog/Hippo source.
8. **Test one randomized Game Garage cartridge.** When you own one of the five AP cartridge items, insert its matching cartridge in Game Garage and complete that song at least at Bronze. Vampire Killer is native and not one of these five items. Confirm the matching `Game Garage - {song} - Bronze` check; a higher sticker should also satisfy its lower cumulative tiers.
9. **Verify the live I Got Money source baseline.** On the fresh v0.22 save, clear default Level 2 once. Confirm the source check `Level 2 - Money Cassette` is sent and the native cassette award is suppressed. This proves the **source location**, not ownership of the cassette item placed there. Replay default Level 2 and confirm the source does not send again. The Bee Mode alias remains manual verification pending and is not part of this baseline.
10. **Verify a second ordinary cassette source.** Clear one mapped non-I-Got-Money level route listed in the per-song table below. Confirm its `Cassette Source - {song}` check sends once, its native cassette award is suppressed, and replaying the same route sends no duplicate. Record the exact level/variant and song; do not claim untested aliases from this one result.
11. **Verify a reused point-chest cassette source.** Open one of the 32/64/89/111/140-point chests. Confirm only its existing `Music Lab - {threshold} Point Chest` location sends—there must be no second `Cassette Source` location—and confirm other chest rewards/collected state remain native. Re-enter Hub6 or reconnect and confirm reconciliation does not resend the check.
12. **Verify AP receipt and native bag state separately.** When the multiworld delivers a cassette item (which need not match the source just completed), confirm the AP receipt is logged and the corresponding cassette appears as native `HAVE_IN_BAG`. Repeat after a room reload, save reload, reconnect, and full game restart. This is an **item receipt/lifecycle** test, not a source or medal test. A previously deposited cassette must remain `HAVE_DEPOSITED`, never return to the bag.
13. **Verify normal player-driven insertion.** Take that AP-owned cassette to its matching Music Lab machine and interact normally. Confirm the client does not force the machine open, the native state changes from `HAVE_IN_BAG` to `HAVE_DEPOSITED`, and the song becomes playable. Reload/reconnect once more and confirm it stays deposited.
14. **Verify the medal check after insertion.** Play the newly deposited song and earn a medal. Confirm the matching cumulative `Music Lab Cassette - {song} - {tier}` check and `MUSIC LAB CASSETTE MEDAL` log line. A higher medal may send lower cumulative tiers; it must not create another cassette source or item receipt.

If a step cannot be attempted because the seed has not delivered the needed item, report the completed steps and the item/slot state instead of editing save data or using an untrusted workaround.

## Confirmed blocking bugs

### Game Garage black screen with zero AP cartridges

In Client v0.67.60, entering `GameRoom_27` with Game Garage cartridge randomization enabled and no AP-owned Garage cartridges can leave the game on a permanent black loading screen while Garage audio continues. The pause/menu exit is unavailable, so the player must close the game manually.

The captured reproduction reached `GameRoom_27`, resolved all six native cartridge objects, and marked all six inactive; the log contained no crash or unhandled exception. Treat the all-locked cartridge state as the leading cause until a controlled comparison confirms the exact failing native dependency.

Do not test Game Garage in this build unless specifically requested by a maintainer. The next client release must not claim this issue as fixed until a fresh-save test proves that entering and exiting Game Garage with zero AP-owned cartridges works normally while every unowned song remains inaccessible.

### Level 22 completion is not sent

Client v0.67.60 observes King Ferdinand I completion as internal level `Level_28` but does not map that identifier to `Level 22 - Completion`. The captured fresh-save result logged `COMPLETION internal=Level_28`, followed by `Unmapped internal level 'Level_28'`, and no AP location was sent.

The next client release must map `Level_28` and verify that an ordinary Level 22 clear sends its completion/performance locations independently of the future Star-goal Victory condition. Fresh-save testing also confirmed that Level 22 is beatable for one Star without any abilities; Note Pad and Data Stick must therefore not gate one-Star completion and remain candidates only for higher performance tiers pending targeted tests.

### APWorld v0.17 can generate a BK'd seed

Fresh-seed testing of `AP_28223804408101432968` reached a state with no reasonably or physically available checks. The generated spoiler placed `Lobby Access` at `Music Lab Cassette - Lets Go - Platinum`; the other non-Royal Area Access items were placed on cassette checks whose machines were behind native Music Lab barriers. It also placed `Weed Killer` at `Game Garage - Vampire Killer - Platinum` even though no Vampire Killer Cartridge had been received and zero-cartridge Garage entry black-screened, and placed `Plant Pipes` at the native 64-point chest. Hip Glasses and Chicken Bucket were likewise placed on additional cassette checks.

This is a generator/logic failure, not a request for the tester to grind high-skill checks. APWorld must not expose blocked cassette machines, unowned Garage songs, or unavailable native point thresholds to the solver. Required progression must have a solver-proven route through checks that the client and fresh save can actually reach. Keep this seed as a regression fixture; do not use it for further acceptance gameplay.

### AP-delivered Plant Pipes do not persist after Level 3

In an admin-assisted fresh-save test, Client v0.67.60 correctly received Plant Pipes during Level 3 and applied native `WEED_KILLER_ABILITY`, allowing the level to be completed. After the Level 3 result was persisted and the game returned to Roots/Hub2, Plant Pipes were no longer in the player's usable inventory. The log contained no explicit false/unset progression request, so the grant appears to be lost during native level-result or scene-state reconciliation.

The next client release must reconcile AP-owned Plant Pipes after level completion and every relevant scene/save reload, without generating a second AP item. Acceptance requires receiving Plant Pipes once, completing Level 3, returning to Hub2, entering and completing Level 4, and retaining the ability across a full game restart.

Client v0.67.61 now contains a verified repair candidate. Live slot-4 testing on 2026-08-21 restored `WEED_KILLER_ABILITY=True` from received-item history after the selected save became readable, preserved `LEVEL_07_WK_ABILITY_EARNED=True`, and kept Plant Pipes usable in Roots across a full close/relaunch without another AP delivery. The implementation also prevents native save access from the AP background thread and guards native grant submission against progression-hook re-entry. The complete fresh receive -> Level 3 -> Hub2 -> Level 4 route must still be replayed before the broader durability issue is closed.

Client v0.67.62 fresh-seed testing on 2026-08-22 verified the Frog/Hippo AP source, one AP Plant Pipes receipt, immediate use, Hub2 -> Level 4 -> Hub2 transitions, clean shutdown/reconnect, and restored use after loading the same save. The remaining acceptance step is to complete Level 4 and verify that its persisted result does not clear the ability.

### Roots arrival cutscene can outrun its bypass

On the first Roots visit in Client v0.67.62, the pre-transition bypass found `PlayerSaveRequestProcessor` unavailable. The game entered `GameRoom_Hub2` and started the arrival cutscene before the client later captured a processor and wrote `ROOTS_HUB_INTRO_WITNESSED=True`. The fix must queue and verify this one flag before the Hub2 transition completes without spoofing unrelated Roots quest state.

### Star counter disappears after Level 3

During the same fresh v0.18 run, the visible Star total disappeared after Level 3 returned to Roots. Expected AP behavior is a persistent HUD counter backed by synchronized AP Stars. Capture the HUD/native progression state before and after the level result to distinguish a fresh-save bootstrap issue from an AP Star synchronization issue.

## Optional Combo Bucket feasibility testing

Combo Bucket increases score and objective effects in later campaign levels and provides a 5× effect in Music Lab. Area Access may eventually allow some of this content before Lift Quest grants Combo Bucket, so reports about what can be achieved without it are valuable for future solver logic.

When testing without Combo Bucket, report:

- the exact level, objective, Music Lab cassette, or Game Garage song;
- native play difficulty and selected AP performance difficulty, when applicable;
- score, stars, medal, completed objective, and best missed threshold;
- player count and other relevant abilities or items;
- confirmation that Combo Bucket was not owned or active;
- number of attempts and best result; and
- client/APWorld versions plus a short relevant log excerpt or video when practical.

A successful result demonstrates that the reported target is possible without Combo Bucket under those conditions. A failed attempt is still useful evidence, but it does **not** by itself prove the target is impossible. Do not edit the save or use score cheats for these reports; if a developer diagnostic was used to reach the content, name it explicitly.

Use this optional report block in addition to the general issue template below:

```markdown
### Combo Bucket feasibility
- Content and objective:
- Combo Bucket absent: yes/no
- Native difficulty:
- AP performance difficulty:
- Player count:
- Other relevant abilities/items:
- Attempts:
- Best score/stars/medal/objective result:
- Target or missed threshold:
- Diagnostic shortcuts used:
```

## Known limitations

- APWorld v0.22.0 random currently selects only the validated Roots Access starter; other fixed starters remain unavailable until their routes are tested.
- Royal Corridor routing is incomplete. The Hub6 phone lands on the Level 22 side; fresh-save testing confirmed the player cannot approach the Royal Star Eater from that spawn to feed Stars and complete the bridge back toward Level 21. Until the bridge route and logic are implemented, `Royal Corridor Access` exposes only the phone-side Level 22 route and must not make Level 21 or the Royal Star Eater check logically reachable.
- Full-game logic and the final victory condition are incomplete. The approved 66-Star / Level 22 victory design is not implemented.
- Generated AP Stars and randomized Music Lab Point inventory are design-only. Current Music Lab reward chests use the game's native medal-score currency.
- Music Lab construction barriers are bypassed in compatible AP sessions, but most full-cassette source routes still require individual manual verification.
- The 24 newly mapped cassette songs and the I Got Money Bee alias are manual-verification pending. Secret Bunker Devil aliases remain conservatively inactive in solver reachability.
- The separate Game Garage cartridge native-inventory blocker remains open; Music Lab cassette receipt reconciliation does not fix it.
- AP performance difficulty filtering is active in APWorld v0.22.0; it filters existing performance locations and does not change native REG/PRO. The eight-option cassette/solver matrix is complete. Manual gameplay remains part of the broader prototype smoke test; inactive Star previews and live Star gates are separate, unfinished systems.
- Local co-op is unverified. Online co-op, DeathLink, and an integrated overlay/text client are deferred.
- Developer diagnostics and hotkeys may exist in development builds. Do not rely on them for normal play, and say exactly which one you used in a report.

## Full cassette manual status (v0.22)

“Live verified” is reserved for a song with its own recorded gameplay evidence. Point-chest routes use the previously established chest interaction as a representative path. I Got Money's default Level 2 route is live verified; its Bee alias is still manual verification pending.

| Song | Source type | Source location | Status |
| --- | --- | --- | --- |
| The Little Things | Level-earned | Cassette Source - The Little Things | mapped; manual verification pending |
| No Plan B | Level-earned | Cassette Source - No Plan B | mapped; manual verification pending |
| Jolt City | Level-earned | Cassette Source - Jolt City | mapped; manual verification pending |
| Quieres Bailar | Level-earned | Cassette Source - Quieres Bailar | mapped; manual verification pending |
| Quicksand | Point chest | Music Lab - 32 Point Chest | mapped; representative path verified |
| Gold | Level-earned aliases | Cassette Source - Gold | mapped; manual verification pending |
| I Got Money | Level-earned aliases | Level 2 - Money Cassette | live verified |
| Hippo and Frog | Level-earned | Cassette Source - Hippo and Frog | mapped; manual verification pending |
| On the Way | Level-earned aliases | Cassette Source - On the Way | mapped; manual verification pending |
| Badass | Level-earned | Cassette Source - Badass | mapped; manual verification pending |
| Heavy Metal | Level-earned | Cassette Source - Heavy Metal | mapped; manual verification pending |
| AOK | Level-earned aliases | Cassette Source - AOK | mapped; manual verification pending |
| Rainbow Melodies | Level-earned aliases | Cassette Source - Rainbow Melodies | mapped; manual verification pending |
| Sneaking | Level-earned aliases | Cassette Source - Sneaking | mapped; manual verification pending |
| The Heist | Level-earned | Cassette Source - The Heist | mapped; manual verification pending |
| Money | Level-earned | Cassette Source - Money | mapped; manual verification pending |
| Lets Go | Level-earned aliases | Cassette Source - Lets Go | mapped; manual verification pending |
| Bounce | Level-earned | Cassette Source - Bounce | mapped; manual verification pending |
| Epical | Level-earned | Cassette Source - Epical | mapped; manual verification pending |
| Hollywood Trailer | Level-earned | Cassette Source - Hollywood Trailer | mapped; manual verification pending |
| False Data | Level-earned | Cassette Source - False Data | mapped; manual verification pending |
| Gotta Get Up | Level-earned | Cassette Source - Gotta Get Up | mapped; manual verification pending |
| Fumblin Around | Level-earned aliases | Cassette Source - Fumblin Around | mapped; manual verification pending |
| Party Non Stop | Level-earned | Cassette Source - Party Non Stop | mapped; manual verification pending |
| Keep On Hustlin | Level-earned aliases | Cassette Source - Keep On Hustlin | mapped; manual verification pending |
| Another Day In Paradise | Level-earned | Cassette Source - Another Day In Paradise | mapped; manual verification pending |
| Flamenco | Point chest | Music Lab - 64 Point Chest | mapped; representative path verified |
| Ten-Four Good Buddy | Point chest | Music Lab - 89 Point Chest | mapped; representative path verified |
| Zen | Point chest | Music Lab - 111 Point Chest | mapped; representative path verified |
| Wiggle | Point chest | Music Lab - 140 Point Chest | mapped; representative path verified |

## Report a problem safely

Before opening an issue, reproduce the problem once if it is safe to do so. Include the first `[SCRC-AP]` version line, connection/login lines, the action that triggered the problem, and approximately 30 seconds of log output before and after that action. Name the exact level and variant, or Music Lab/Game Garage song, rather than describing it generally.

Redact passwords, API keys, private server addresses when necessary, personal file paths, and unrelated player information. Do **not** commit or publicly attach full save folders, game binaries, game DLLs, BepInEx binaries, proprietary IL2CPP assemblies, or complete unredacted configuration files. A relevant log excerpt or a zipped copy of `BepInEx/LogOutput.log` is useful only after those secrets and private details have been removed.

Copy and complete this template:

```markdown
### Versions
- Client:
- APWorld:
- Archipelago:
- Game platform/store:

### Seed and configuration
- Slot-data implementation tag:
- Fresh seed after APWorld update: yes/no
- Starting area:
- Relevant non-secret config values:

### Reproduction
1.
2.
3.

### Expected behavior

### Actual behavior

### Attachments
- Relevant excerpt or zipped copy of BepInEx/LogOutput.log
- Screenshot/video
- YAML used to generate the slot
- Seed/room link or generated seed name, if safe to share
```

Do not include a room password in the YAML, seed/room link, screenshot, log, or issue text. If the YAML contains a password or personal data, redact it or describe only the relevant non-secret options.

## For advanced diagnostics

Maintainers may ask for the [advanced developer checklist](TESTING.md), including its Roots regression checklist and exact diagnostic log phrases. That document supplements this public smoke test; it does not replace clean-save gameplay testing.
