# Public development testing and issue reports

> [!WARNING]
> This is an unofficial, experimental development build—not a release. Back up your save and expect incomplete logic. The project uses AI-generated code under human direction; gameplay testing remains the acceptance criterion.

Use this guide for a focused public smoke test and for reporting a problem. For installation, see the [public installation guide](INSTALL.md). For the broader implementation boundary, see the [roadmap](ROADMAP.md). The [advanced developer testing checklist](TESTING.md) is available when a maintainer asks for targeted diagnostics.

## Before starting a smoke test

Use matching source builds and record the versions you actually use:

- Client: `0.67.60`; confirm `<GameDir>\BepInEx\LogOutput.log` contains `[SCRC-AP] v0.67.60 loading.` (the first `[SCRC-AP]` version line should identify this client version).
- APWorld: `0.15`.
- Slot-data implementation tag: `area-routing-plant-pipes-0.15`.
- A **freshly generated seed** after any APWorld replacement or update. Replacing an installed `.apworld` does not change an existing seed.
- A **fresh in-game save** for the first pass, especially when testing first arrivals, story scenes, or source checks.
- For the reward-chest reconciliation portion below, leave `[Developer] EnableTestHarness = true`. This is the generated client configuration's current default. If it has been changed to `false`, normal chest collection still works, but the automatic Hub6 reconciliation and the Hub6 `F5` diagnostic are unavailable.

Follow the [installation guide](INSTALL.md) to build and install both components, generate the seed, and configure the client. Use the room's real host, port, slot name, and password locally; do not publish a password or a complete config file.

## Public smoke test

Run these steps in order where the seed allows. A received progression item may belong to a different player or be placed later in your own world, so a source check need not deliver its matching item immediately. Record the exact step, level, or song at which a result differs from the expectation.

1. **Start and connect.** Launch the game with the new save and connect to the room. Confirm the client version line above, normal connection/login lines, and the `area-routing-plant-pipes-0.15` slot-data implementation in `LogOutput.log`.
2. **Confirm the Hub6 start.** Verify that the save starts at the Hub6 / Music Lab phone hub and that Music Lab and Game Garage are usable. APWorld v0.17 random currently selects only validated Roots Access.
3. **Travel to Roots.** Use the Roots phone. The first trip should not leave the player unable to move because of the displaced arrival cutscene. The current prototype also permits the Roots traversal baseline around the first area gate and Star Eater blockade.
4. **Test Gecko's source.** Reach Gecko in Roots. The interaction should send `Roots - Gecko's Weed Killer`; it must not directly give the native Weed Killer reward. Look for `ROOTS WEED KILLER SOURCE AP CHECK` in the log.
5. **Test delivered Weed Killer.** When the room delivers `Weed Killer`, verify that the client applies the native item and that vanilla progression can use it to open Level 3. The relevant confirmation is `ROOTS WEED KILLER NATIVE GRANT APPLIED`.
6. **Test the Level 3 partial route.** Enter Level 3 and reach Frog/Hippo. Their interaction should send `Roots - Level 3 - Frog and Hippo`, without directly granting Plant Pipes. If `Plant Pipes` has not arrived yet, use the normal menu exit: leaving the level at this point is intentional and is the expected partial-level behavior.
7. **Test Plant Pipes and Level 3 completion.** After the room delivers `Plant Pipes`, verify `ROOTS PLANT PIPES NATIVE GRANT APPLIED`, return to Level 3, and complete it. This confirms that Plant Pipes is required for completion, not for reaching the Frog/Hippo source.
8. **Test one Game Garage cartridge.** When you own one of the six AP cartridge items, insert its matching cartridge in Game Garage and complete that song at least at Bronze. Confirm the matching `Game Garage - {song} - Bronze` check; a higher sticker should also satisfy its lower cumulative tiers. Record the exact cartridge, song, and sticker tier.
9. **Test one Music Lab cassette medal.** Play one cassette song that is already natively unlocked on the save and earn a medal. Confirm the matching cumulative `Music Lab Cassette - {song} - {tier}` check and the `MUSIC LAB CASSETTE MEDAL` log line. Cassettes themselves are not randomized in the current v0.15 APWorld.
10. **Test one Music Lab reward chest and its reconciliation.** Keep `EnableTestHarness = true` for all parts of this check.
   - **Initial collection event:** Open one native Music Lab reward chest that the save qualifies for (the 5-point chest is the first threshold). The initial progression request/event may queue `Music Lab - {threshold} Point Chest`; confirm the `MUSIC LAB {threshold}-POINT CHEST COLLECTED` and `QUEUED CHECK` lines.
   - **Same-session duplicate suppression:** Without restarting the game process, return to Hub6 and wait for its reconciliation pass, or press plain `F5` in Hub6. The log should show `MUSIC LAB REWARD CHEST RECONCILE collected` for the saved chest and `Duplicate local check ignored` for the already queued/sent AP location. This is expected: the client prevents a second local submission in the same session.
   - **Clean saved-state reconciliation:** Use a safe test save in which a qualifying native reward chest was collected **before** launching the AP-enabled client/session. Enter Hub6 with the client connected and confirm `MUSIC LAB REWARD CHEST RECONCILE collected`, the corresponding `MUSIC LAB {threshold}-POINT CHEST COLLECTED source='save reconciliation'` line, and the queued AP location. After the Hub6 map is available, the log also records `MUSIC LAB REWARD CHEST RECONCILE COMPLETE`.

   Plain Hub6 `F5` is read-only with respect to the native save: it does not open a chest, add native medal points, or alter a native collection flag. It may queue an AP check when the native save already marks that reward chest as collected.

If a step cannot be attempted because the seed has not delivered the needed item, report the completed steps and the item/slot state instead of editing save data or using an untrusted workaround.

## Confirmed blocking bugs

### Game Garage black screen with zero AP cartridges

In Client v0.67.60, entering `GameRoom_27` with Game Garage cartridge randomization enabled and no AP-owned Garage cartridges can leave the game on a permanent black loading screen while Garage audio continues. The pause/menu exit is unavailable, so the player must close the game manually.

The captured reproduction reached `GameRoom_27`, resolved all six native cartridge objects, and marked all six inactive; the log contained no crash or unhandled exception. Treat the all-locked cartridge state as the leading cause until a controlled comparison confirms the exact failing native dependency.

Do not test Game Garage in this build unless specifically requested by a maintainer. The next client release must not claim this issue as fixed until a fresh-save test proves that entering and exiting Game Garage with zero AP-owned cartridges works normally while every unowned song remains inaccessible.

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

- APWorld v0.17 random currently selects only the validated Roots Access starter; other fixed starters remain unavailable until their routes are tested.
- Full-game logic and the final victory condition are incomplete. The approved 66-Star / Level 22 victory design is not implemented.
- Generated AP Stars and randomized Music Lab Point inventory are design-only. Current Music Lab reward chests use the game's native medal-score currency.
- Cassette-item randomization and most remaining quest-item chains are not implemented. Hip Glasses and Chicken Bucket are implemented but still require fresh-save gameplay acceptance.
- Native difficulty-toggle changes and the approved Normal/Hard/Expert/Perfection location model are not implemented.
- Local co-op is unverified. Online co-op, DeathLink, and an integrated overlay/text client are deferred.
- Developer diagnostics and hotkeys may exist in development builds. Do not rely on them for normal play, and say exactly which one you used in a report.

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
