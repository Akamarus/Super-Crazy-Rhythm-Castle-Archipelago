# Public development testing and issue reports

> [!WARNING]
> This is an unofficial, experimental development build—not a release. Back up your save and expect incomplete logic. The project uses AI-generated code under human direction; gameplay testing remains the acceptance criterion.

Use this guide for a focused public smoke test and for reporting a problem. For installation, see the [public installation guide](INSTALL.md). For the broader implementation boundary, see the [roadmap](ROADMAP.md). The [advanced developer testing checklist](TESTING.md) is available when a maintainer asks for targeted diagnostics.

## Before starting a smoke test

Use matching source builds and record the versions you actually use:

- Client: `0.67.59`; confirm `<GameDir>\BepInEx\LogOutput.log` starts with `[SCRC-AP] v0.67.59 loading.`
- APWorld: `0.15`.
- Slot-data implementation tag: `area-routing-plant-pipes-0.15`.
- A **freshly generated seed** after any APWorld replacement or update. Replacing an installed `.apworld` does not change an existing seed.
- A **fresh in-game save** for the first pass, especially when testing first arrivals, story scenes, or source checks.

Follow the [installation guide](INSTALL.md) to build and install both components, generate the seed, and configure the client. Use the room's real host, port, slot name, and password locally; do not publish a password or a complete config file.

## Public smoke test

Run these steps in order where the seed allows. A received progression item may belong to a different player or be placed later in your own world, so a source check need not deliver its matching item immediately. Record the exact step, level, or song at which a result differs from the expectation.

1. **Start and connect.** Launch the game with the new save and connect to the room. Confirm the client version line above, normal connection/login lines, and the `area-routing-plant-pipes-0.15` slot-data implementation in `LogOutput.log`.
2. **Confirm the Hub6 start.** Verify that the save starts at the Hub6 / Music Lab phone hub and that Music Lab and Game Garage are usable. Roots Access is intentionally forced as the starter in APWorld v0.15.
3. **Travel to Roots.** Use the Roots phone. The first trip should not leave the player unable to move because of the displaced arrival cutscene. The current prototype also permits the Roots traversal baseline around the first area gate and Star Eater blockade.
4. **Test Gecko's source.** Reach Gecko in Roots. The interaction should send `Roots - Gecko's Weed Killer`; it must not directly give the native Weed Killer reward. Look for `ROOTS WEED KILLER SOURCE AP CHECK` in the log.
5. **Test delivered Weed Killer.** When the room delivers `Weed Killer`, verify that the client applies the native item and that vanilla progression can use it to open Level 3. The relevant confirmation is `ROOTS WEED KILLER NATIVE GRANT APPLIED`.
6. **Test the Level 3 partial route.** Enter Level 3 and reach Frog/Hippo. Their interaction should send `Roots - Level 3 - Frog and Hippo`, without directly granting Plant Pipes. If `Plant Pipes` has not arrived yet, use the normal menu exit: leaving the level at this point is intentional and is the expected partial-level behavior.
7. **Test Plant Pipes and Level 3 completion.** After the room delivers `Plant Pipes`, verify `ROOTS PLANT PIPES NATIVE GRANT APPLIED`, return to Level 3, and complete it. This confirms that Plant Pipes is required for completion, not for reaching the Frog/Hippo source.
8. **Test one Game Garage cartridge.** When you own one of the six AP cartridge items, insert its matching cartridge in Game Garage and complete that song at least at Bronze. Confirm the matching `Game Garage - {song} - Bronze` check; a higher sticker should also satisfy its lower cumulative tiers. Record the exact cartridge, song, and sticker tier.
9. **Test one Music Lab cassette medal.** Play one cassette song that is already natively unlocked on the save and earn a medal. Confirm the matching cumulative `Music Lab Cassette - {song} - {tier}` check and the `MUSIC LAB CASSETTE MEDAL` log line. Cassettes themselves are not randomized in the current v0.15 APWorld.
10. **Test one Music Lab reward-chest reconciliation.** Open one native Music Lab reward chest that the save qualifies for (the 5-point chest is the first threshold), then reload or return to Hub6 and confirm that its corresponding `Music Lab - {threshold} Point Chest` check is sent once from the native saved chest state. The log should include `MUSIC LAB REWARD CHEST RECONCILE` and, after the full Hub6 map is available, `MUSIC LAB REWARD CHEST RECONCILE COMPLETE`. If a developer diagnostic hotkey is enabled, Hub6 `F5` performs this reconciliation read-only; it does not create chest progress.

If a step cannot be attempted because the seed has not delivered the needed item, report the completed steps and the item/slot state instead of editing save data or using an untrusted workaround.

## Known limitations

- Roots Access is forced as the APWorld v0.15 starter; random safe starters are not implemented.
- Full-game logic and the final victory condition are incomplete. The approved 66-Star / Level 22 victory design is not implemented.
- Generated AP Stars and randomized Music Lab Point inventory are design-only. Current Music Lab reward chests use the game's native medal-score currency.
- Cassette-item randomization and the remaining quest-item chains are not implemented.
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
