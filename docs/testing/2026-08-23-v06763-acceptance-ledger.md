# v0.67.63 Acceptance Evidence Ledger

This is the source of truth for the active `fix/new-save-direct-start` acceptance run. Do not ask the tester to repeat an item in **Verified — do not retest** unless a later code change directly overlaps that behavior or the tester reports a regression.

## Verified — do not retest

- A fresh AP save starts directly in the Music Lab.
- The Music Lab Points HUD is visible in the Music Lab. The normal Star HUD is not expected there.
- Music Lab orange progression barriers are bypassed.
- REG and PRO are available during initial character selection.
- The first Roots-arrival cutscene is suppressed.
- The post-Level-1 Roots cutscene is suppressed.
- After Level 1, the campaign Star counter appears in Roots.
- Plant Pipes work inside the relevant level and after returning to the Roots hub.
- Plant Pipes persist through save reload, reconnect, and a complete game restart.
- Game Garage loads correctly after collecting the physical entrance cartridge.

## Newly verified in this run

- The acceptance seed places `Royal Corridor Access` at `Roots - Level 3 - Frog and Hippo`.
- Repeating Level 3 is not required for this run. The server queued `Royal Corridor Access` for Jack with `/send Jack Royal Corridor Access` and saved the server state. After one full game relaunch, the client received item `187256109`, changed Royal Corridor to unlocked, activated the Royal phone and trigger, and removed its cover. A hub reload alone did not reconnect the client after the server restart.
- The server delivered one requested `Plant Pipes` item. The client received item `187256117`, submitted the native grant, verified AP ownership, and reported `WEED_KILLER_ABILITY=True` in Royal Corridor.

## Unresolved — test or investigate

- Before the first Level-1 completion, the campaign Star HUD is absent in Roots.
- Before the first Level-1 completion, the combined in-hub character/difficulty control is not interactive. Evidence points to shared native Level-1 initialization. This is not related to Bizzle and Clive.
- The combined bottom character/difficulty HUD became interactive after Level 1, then stopped responding again after a full game relaunch. Its availability is therefore not reliably fixed and remains a release bug.
- Verify that completing Level 22 sends its completion and earned Star checks exactly once.
- Verify that completing Level 22 with fewer than the configured goal Stars does not declare victory.
- A failed Level-22 attempt incorrectly sent `Level 22 - Completion` (`187256182`) and awarded the placed Wag the Dog Cartridge. This is a confirmed false-positive completion bug; that check is not valid acceptance evidence.
- A second Level-22 attempt also failed after Plant Pipes was received and verified active. It sent no additional Level-22 performance check. The tester declined further attempts for this session; do not request another Level-22 playthrough before the false-positive mapping is fixed and a better test setup is available.
- Automated repair added after the session: Level 22 now emits no checks for missing or zero-Star result data and emits completion plus cumulative earned tiers only for one-to-three-Star results. The focused regression completed a red/green cycle; all 12 client suites, all 57 APWorld tests, repository validation, and the release build passed. Live acceptance remains deferred.

## Corrections to prior guidance

- Do not retest Level 3 or Plant Pipes for this release candidate unless overlapping code changes.
- Do not describe Bizzle and Clive as character unlocks or as prerequisites for the hub character/difficulty control. They are a single inventory item used for Bee Mode levels and their nearby door interaction.
- Do not expect the normal Star counter in the Music Lab; that area intentionally displays Music Lab Points instead.

## Deferred gameplay test

After repairing the false-positive completion mapping, validate Level 22 with a controlled setup that does not require repeated player attempts. A successful one-Star clear must send completion plus the one-Star check and must not declare victory below the configured goal threshold.
