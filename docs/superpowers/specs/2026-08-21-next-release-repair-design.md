# Next Release Repair Design

**Status:** Approved for implementation planning  
**Date:** 2026-08-21  
**Target:** APWorld v0.18 / Client v0.67.61

## Purpose

Turn the v0.17 / Client v0.67.60 development prototype into a conservative, testable build after fresh-save testing exposed a BK'd seed, client persistence failures, incorrect logical reachability, and two native room/completion defects. The repair release prioritizes truthful solver logic and durable received-item behavior over exposing every registered location.

The failed seed `AP_28223804408101432968` remains a regression fixture. It is evidence, not a playable baseline.

## Scope

This release fixes:

- BK'd generation caused by progression placement on physically unavailable or unreasonable checks;
- Plant Pipes ownership loss after Level 3, scene changes, save reload, and restart;
- Plant Pipes reconciliation depending on an unrelated native progression request;
- the Game Garage black screen when no AP Garage cartridge is owned;
- missing Level 22 mapping for native `Level_28`;
- Music Lab construction barriers being absent from AP logic;
- Royal Corridor phone-side access incorrectly implying Star Eater, bridge, and Level 21 reachability;
- Pro difficulty being unavailable at AP seed start; and
- the redundant Roots gate/difficulty cutscene after Level 1.

This release does not activate AP Stars, AP Music Lab Point inventory, cassette-item randomization, the final Level 22 Victory rule, or unverified quest chains. It does not claim full-game logic.

## Delivery strategy

The work is implemented as independently tested internal batches and released together:

1. client save/ownership reconciliation;
2. client native-room and completion corrections;
3. APWorld conservative reachability and progression-placement policy;
4. startup quality-of-life behavior;
5. fresh-seed regression acceptance.

Each batch preserves compatibility with the last known working behavior outside its stated boundary. The implementation tag retains the existing client-recognized prefix and appends a v0.18 repair marker. Client and APWorld version checks must fail closed when a seed claims behavior the installed client does not support.

## Plant Pipes ownership and reconciliation

Archipelago received-item history is authoritative for Plant Pipes ownership. A one-time in-memory “grant applied” flag is not authoritative because the native game may rebuild or clear usable ability state during level-result persistence, scene transitions, save selection, or restart.

The client introduces a focused Plant Pipes reconciler with these rules:

- receiving or replaying at least one Plant Pipes item marks AP ownership;
- after a selected save exists, the reconciler reads the native Plant Pipes ability state;
- if AP owns Plant Pipes and native state is absent, it submits the native grant through a valid save processor and verifies the result;
- lack of a save processor leaves a retry pending without requiring another item or unrelated progression event;
- reconciliation runs at bounded lifecycle points: save availability, AP login/history replay, relevant scene entry, level-result completion, reconnect, and a short bounded retry while pending;
- repeated history entries and admin-assisted duplicates remain idempotent and never create multiple native abilities;
- the reconciler does not suppress a legitimate native clear/reset unless AP ownership is active; and
- diagnostic logging distinguishes ownership observed, save unavailable, processor unavailable, grant attempted, verification failed, and verified durable state.

The implementation must first determine whether `WEED_KILLER_ABILITY` alone is the durable native state or whether an additional verified native inventory/save marker participates in reconstruction. It may not guess or continuously write a flag every frame. A focused diagnostic test establishes the correct native state transition before the final grant path is committed.

## Game Garage zero-cartridge safety

The Game Garage scene must load normally with zero AP-owned cartridges. Cartridge routing must not deactivate a GameObject that the native room uses to finish initialization, clear the loading fade, build interaction state, or support menu exit.

The repair keeps native holder/lifecycle objects alive and gates only the smallest verified interaction or release surface for each unowned cartridge. If that safe surface cannot be verified during implementation, the release fails closed by disabling live Garage cartridge randomization and excluding all Garage checks from progression placement rather than shipping another black-screen risk.

Acceptance covers:

- entering and exiting with zero cartridges;
- all six unowned songs inaccessible;
- receiving one AP cartridge;
- only its matching song becoming usable;
- leaving and re-entering without duplication or black screen; and
- a full restart preserving ownership.

## Level 22 mapping and victory separation

Native `Level_28` maps to player-facing Level 22. Its default-variant completion sends the ordinary Level 22 completion/performance locations exactly like other campaign levels.

This mapping is separate from Victory. A one-Star clear without abilities is valid and sends the ordinary check. Note Pad and Data Stick do not gate one-Star completion; any effect on higher-Star results remains unimplemented until targeted evidence exists. Because AP Stars and the final goal are inactive in this release, no Level 22 clear awards the approved future Victory condition.

## Music Lab barriers and conservative cassette policy

The orange construction barriers create physical subregions inside Music Lab. The current single-region model is false. The repair introduces an explicit reachability classification for each cassette machine:

- **verified open:** physically reachable on the supported fresh-save AP start;
- **verified gated:** mapped to a confirmed native barrier condition;
- **unverified:** not safe for progression placement.

Only verified-open checks may hold required progression in v0.18. Verified-gated checks may become solver-reachable only through their confirmed rule. Unverified, Platinum, and otherwise unsupported high-skill checks may remain registered as optional locations, but their item rules permit filler or other non-required items only. They cannot hold Area Access, Weed Killer, Plant Pipes, Hip Glasses, Chicken Bucket, future goal items, or any item classified as required progression.

If the verified-open catalog lacks capacity for the required pool, generation fails with a clear capacity error. It must never relax the policy or silently place progression on unsafe checks.

Music Lab Point chests continue to use native medal score. A chest threshold may hold required progression only when the solver proves enough currently reachable native medal points under the selected difficulty and verified-open cassette catalog. Until that accounting exists, thresholds beyond proven fresh-save reach are non-progression-only.

## Conservative Game Garage and performance policy

Each Game Garage song requires its corresponding AP cartridge before its medal checks are reachable. The solver and client use the same six-item mapping. If live Garage routing is disabled by the safety fallback, every Garage medal check is non-progression-only for that seed schema.

Platinum checks remain available for players who want them but are never required for seed completion in this repair release. Lower performance tiers may hold progression only when their content and prerequisite items are solver-reachable. This policy prevents skill-heavy checks from becoming the only path forward while preserving optional challenge checks.

## Royal Corridor route split

`Royal Corridor Access` opens the Hub6 phone and reaches only the phone-side Level 22 subregion. It does not imply access to:

- the Royal Star Eater interaction;
- the completed Star Eater bridge; or
- the Level 21 side.

Those states remain separate logical nodes. Because their native requirements are not implemented in v0.18, the Royal Star Eater and Level 21 side are non-progression-only/unreachable in solver logic. Level 22 remains available from the phone side and uses its ordinary entrance behavior.

## Seed-generation safety

APWorld generation builds a required-progression set and a safe-location set before fill. Every required item must fit within and be placeable through solver-reachable safe locations. Validation must cover:

- sphere-by-sphere reachability of every required item;
- no required item behind its own item, area, cartridge, barrier, or point threshold;
- no required item on Platinum;
- no required item on an unverified Music Lab or disabled Garage check;
- the Royal phone-side split;
- sufficient safe capacity at every supported difficulty; and
- deterministic reproduction of the failed v0.17 placement pattern as a rejected or corrected seed.

The internal development Victory placeholder may not make an otherwise BK'd seed appear valid. Generation validation must prove the intended required progression routes independently of that placeholder.

## Difficulty availability at AP start

Normal and Pro native play difficulty are both available after an AP save is selected. The client does not force a choice and does not confuse native Normal/Pro with the APWorld's performance-location difficulty option.

The unlock is reconciled on save availability and restart. It changes only the native difficulty-selection availability needed for player choice. Existing score evaluation continues to report the difficulty actually used.

## Roots post-Level 1 cutscene suppression

For AP sessions, the client completes only the introductory Roots gate/difficulty bookkeeping before the first Roots visit:

- `ROOTS_HUB_GATE_OPENED`; and
- `ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE`.

This prevents the redundant post-Level 1 gate/difficulty cutscene. It preserves Level 1 completion and AP checks, Gecko/Weed Killer, Star Eater, Bucket Minion, Lift Quest, and all later Roots quest state. The existing `ROOTS_HUB_INTRO_WITNESSED` first-arrival handling remains narrowly scoped and is tested alongside the two new flags.

## Versioning and slot data

The APWorld advances to v0.18 and the client to v0.67.61. Slot data adds explicit fields for:

- repair schema/version;
- Plant Pipes durable reconciliation support;
- safe Music Lab location classification;
- Garage routing mode (`interaction-gated` or `disabled-safe-fallback`);
- Royal phone-side route split;
- Level 22 `Level_28` mapping support;
- native difficulty choice availability; and
- Roots post-Level 1 intro suppression.

Fields describing inactive Stars, Music Lab Point items, final Victory, and cassette items remain false. Documentation and logs must not claim those systems are live.

## Testing strategy

Implementation follows test-driven development.

Client pure-policy tests cover Plant Pipes ownership/retry state, idempotence, scene/save lifecycle triggers, Level 22 mapping, Garage ownership gating, Royal subregion classification, and startup-flag scope. Integration seams use fakes for native save availability and verified state reads; gameplay remains the authority for IL2CPP behavior.

APWorld tests cover safe-location classification, required-item restrictions, cartridge prerequisites, point-threshold conservatism, Platinum optionality, Royal route splitting, capacity failures, deterministic generation, and rejected/corrected reproduction of the v0.17 BK placement.

Repository validation, client build, APWorld packaging, installed-artifact hashes, and clean worktree checks run before gameplay. No automated result substitutes for the fresh-save acceptance run.

## Acceptance criteria

The repair release is ready only when:

1. every automated client, APWorld, and repository test passes;
2. the failed v0.17 seed pattern is rejected or generated without unsafe progression placement;
3. one Plant Pipes receipt survives Level 3 completion, Hub2 return, Level 4, save reload, reconnect, and restart;
4. Game Garage loads and exits with zero cartridges and gates individual songs correctly;
5. `Level_28` sends ordinary Level 22 checks without awarding inactive Victory;
6. Music Lab barrier groups and native point thresholds match solver reachability;
7. Royal Access exposes Level 22 but not Star Eater/bridge/Level 21 logic;
8. Normal and Pro are both selectable on a fresh AP save;
9. the post-Level 1 Roots cutscene is absent while later Roots progression remains intact;
10. a fresh seed completes the Weed Killer -> Frog/Hippo -> Plant Pipes -> Level 4 -> Hip Glasses -> Lift Quest -> Bucket Minion -> Chicken Bucket route without admin commands; and
11. the next release changelog moves only verified items from known issues into a **Fixed** section.

## Release discipline

No automatic merge or push is authorized. Build deployment remains restricted to `BepInEx\plugins\RhythmCastleAP`, APWorld installation uses the normal custom-world location, and the project owner reviews the completed branch before integration. Any acceptance failure returns its checklist item to open status and blocks release claims.
