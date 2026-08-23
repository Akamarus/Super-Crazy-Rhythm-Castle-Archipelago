# Combined Repair Design

**Date:** 2026-08-22  
**Target:** APWorld v0.18 / Client v0.67.63  
**Status:** Approved design; implementation pending

## Purpose

Produce one combined repair candidate for a single efficient gameplay session. This batch contains only defects and incomplete acceptance boundaries already identified during v0.17/v0.18 testing. It does not add the future AP Star pool, Star-goal Victory, new progression items, or unrelated features.

## Scope

The batch fixes and verifies:

1. new AP saves start directly in Music Lab without the two tutorial rooms;
2. the first Roots arrival cutscene is suppressed reliably;
3. the redundant Roots gate/difficulty presentation after Level 1 is suppressed;
4. Normal and Pro are both available from AP seed start without forcing either choice;
5. the native Star counter remains visible and shows native earned Stars in v0.18;
6. orange Music Lab construction barriers are bypassed during compatible AP games;
7. Game Garage loads and exits normally with zero AP-owned cartridges;
8. native `Level_28` sends Level 22's ordinary completion and cumulative performance checks; and
9. one AP-delivered Plant Pipes item survives a completed Level 4 result, room changes, save load, reconnect, and full restart.

## Compatibility boundary

Runtime changes require all of the following:

- Archipelago integration is enabled;
- login has succeeded;
- slot data advertises the compatible v0.18 repair contract; and
- the relevant native room, object, request, or flag is positively identified.

Before compatibility is known, and whenever identification fails, behavior remains vanilla. Disabling Archipelago restores vanilla save startup, barriers, difficulty progression, cutscenes, Garage behavior, and HUD behavior.

## Client architecture

### AP save bootstrap

Creating a new save during a compatible AP session arms one one-shot redirect. The next transition that would enter the tutorial intro consumes the token and routes to `GameRoom_Hub6`. Established saves load their saved room normally.

The exact `GameRoom_04A` to `GameRoom_Hub6` fallback remains available during compatible AP sessions to rescue a save that would otherwise enter the tutorial. It must not redirect unrelated rooms or run when AP is disabled.

### Roots presentation policy

The initial Roots arrival and the post-Level-1 gate/difficulty presentation are separate policies.

Before the first permitted transition to `GameRoom_Hub2`, the client submits and verifies only `ROOTS_HUB_INTRO_WITNESSED=True`. If a valid `PlayerSaveRequestProcessor` is temporarily unavailable, the client queues the write on Unity's main thread and briefly defers that Hub2 transition. It must not allow the cutscene to begin and write the flag afterward. A bounded timeout or failed verification restores vanilla behavior and logs the failure rather than hanging the game.

After Level 1, the client completes only the verified introductory bookkeeping required to suppress the redundant gate/difficulty presentation. Level 1's completion and performance checks, native result persistence, and every later Roots quest remain intact. No broad Roots story normalization is allowed.

### Difficulty availability

A compatible AP save exposes both native Normal and Pro choices from the beginning. The client does not choose a difficulty, rewrite completed results, or add another difficulty mode. The player's native choice persists normally.

### Star HUD

APWorld v0.18 does not place or synchronize live AP Star items. Client v0.67.63 therefore keeps the native Star counter visible and displays the save's native earned-Star total. Skipped startup presentations must not hide or destroy the HUD state that owns this counter.

When live AP Stars are implemented in a later release, the display source will change under a separate approved design. This repair must not partially implement AP Stars or Victory.

### Music Lab barrier bypass

During a compatible AP session in `GameRoom_Hub6`, the client disables only the verified orange construction barriers that block paths to cassette machines. Exact object paths or verified component identities are required; broad name-based scene scans are forbidden.

Cassette machines, cassette ownership, medal thresholds, reward chests, Music Lab Points, room geometry, and unrelated interactions remain unchanged. When AP is disabled or incompatible, all barriers retain vanilla behavior.

### Game Garage availability

The Game Garage must complete native initialization even when the player owns zero AP cartridges. Native cartridge and song objects required for room loading remain alive. AP ownership controls whether each song can be selected or started, rather than whether its native initialization object exists.

With zero cartridges, the room loads visibly, all six songs remain unavailable, and normal menu or hub exit works. With one cartridge, only its matching song becomes usable. Multiple cartridges expose exactly their corresponding songs. Vanilla behavior is retained outside compatible AP sessions.

### Level 22 mapping

Native `Level_28` maps to Level 22's ordinary completion and cumulative performance locations. A one-Star clear sends completion and the earned one-Star tier; higher results send every newly earned cumulative tier through the existing location rules.

This mapping is independent of the future Star-goal Victory condition. Clearing Level 22 cannot award Victory in this batch, and receiving Stars later cannot retroactively award Victory.

### Plant Pipes durability

Archipelago received-item history remains authoritative for Plant Pipes ownership. The existing main-thread, bounded, verified reconciliation continues to restore `WEED_KILLER_ABILITY=True` whenever AP owns Plant Pipes and the selected save does not.

The remaining acceptance boundary is a completed Level 4 result. After that result persists and the player returns to Hub2, Plant Pipes must remain usable without a duplicate AP delivery. The same ownership must survive scene changes, reconnect, save load, and full process restart.

## APWorld logic

APWorld remains at v0.18. Required progression may use Music Lab cassette locations whose machine paths become physically available through the compatible AP-only barrier bypass, subject to existing cassette ownership, selected AP difficulty, and performance rules. Required items may never be placed on a tier filtered out by the selected difficulty, an unowned Garage song, or an unavailable native point threshold. The retained BK seed and a deterministic seed matrix remain regression inputs.

Game Garage locations remain unreachable without their matching AP cartridge. Level 22's ordinary locations become part of the registered live mapping but do not change Victory. No new network IDs are allocated.

If the client cannot positively identify and bypass every intended Music Lab barrier, the affected locations remain unsafe for required progression until their access is verified. The solver must not assume broader access than the client demonstrably provides.

## Logging and failure handling

Each repair logs:

- compatibility and enabled state;
- current room;
- the exact request, flag, object, level, or cartridge identity;
- the policy decision and whether it changed native behavior;
- verification success, bounded retry, timeout, or vanilla fallback.

No repair may delete a save, duplicate an AP item, complete unrelated story state, award Victory, merge, or push automatically.

## Automated verification

Pure-policy and integration-seam tests cover:

- one-shot new-save redirect, established saves, fallback scope, and disabled/incompatible AP;
- queued Roots intro writes, successful verification, timeout fallback, and separate post-Level-1 suppression;
- Normal/Pro availability without forced selection;
- native Star HUD visibility in v0.18;
- exact AP-only Music Lab barrier selection;
- zero-, one-, and multiple-cartridge Garage availability;
- `Level_28` mapping and cumulative ordinary checks without Victory;
- Plant Pipes reconciliation after a completed Level 4 result; and
- vanilla behavior whenever the compatibility boundary is not satisfied.

All client policy suites, APWorld tests, repository validation, and a production client build must pass before deployment.

## Combined gameplay acceptance

Deploy one v0.67.63 candidate and matching v0.18 APWorld, then use one fresh seed and fresh save to verify:

1. the game starts in Music Lab without either tutorial room;
2. the Star counter is visible and both Normal and Pro are selectable;
3. every intended cassette-machine path is accessible without orange construction barriers;
4. Game Garage loads and exits with zero cartridges, then enables only one manually or naturally received cartridge;
5. the first Roots arrival and post-Level-1 presentations do not play;
6. Weed Killer opens Level 3, Frog/Hippo sends its AP source check, one Plant Pipes receipt allows completion, and Plant Pipes remain usable after completing Level 4;
7. Plant Pipes remain usable after hub transitions, save load, reconnect, and full restart; and
8. a one-Star Level 22 clear sends its ordinary AP check and does not award Victory.

A failed item remains open and must not be described as fixed. Admin sends may be used to shorten this focused acceptance route, but every such send must be recorded so it is not mistaken for solver validation.

## Explicit exclusions

This batch does not implement:

- live AP Star placement or synchronization;
- the configurable Star-goal Victory condition;
- Music Lab Point items or bundle balancing;
- new cassette or cartridge items;
- new areas, quest items, characters, multiplayer, DeathLink, or integrated text UI;
- automatic merge, push, or release publication.
