# v0.67.65 / v0.19 Area-Arrival Repair Acceptance

**Status:** Installed and partially gameplay-verified; bottom-control follow-up required

Use the existing v0.19 seed and tested save. This focused run does not require replaying levels.

## Setup

- [x] Confirm the first client line is `[SCRC-AP] v0.67.65 loading.`
- [x] Confirm the client reconnects to the existing compatible room without an error.

## Roots AP-phone arrival

- [x] Start in Music Lab and travel to Roots through the AP-unlocked phone.
- [x] Confirm `AREA ARRIVAL OVERRIDE ARMED kind='RootsPhone'` appears.
- [x] Confirm no Roots first-arrival cutscene plays and player control is retained. The save already held the arrival flag from the prior run, so a future fresh-state test is still needed to exercise the condition-bypass line itself.
- [x] Confirm the campaign Star counter appears. The save held one Star from Level 22; repeat only if a later fresh-state run is already otherwise necessary.
- [ ] Confirm the bottom character/difficulty control is interactive. **Failed:** visible but non-interactive even though `ROOTS_HUB_GATE_OPENED=True` and `ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE=True`.
- [ ] Return to Music Lab, revisit Roots, and confirm normal travel remains stable.

## Lobby AP-phone arrival

- [x] From Music Lab, travel to Lobby through the AP-unlocked phone.
- [x] Confirm `AREA ARRIVAL OVERRIDE ARMED kind='LobbyPhone'` appears.
- [x] Confirm the Lift Quest first-arrival presentation does not play. The prior run may already have persisted its arrival marker, so this is not yet a clean exercise of the condition-bypass line.
- [x] Confirm player control and Lobby routing remain normal. The Roots bottom-control failure remains open.

## Safety boundary

- [ ] Confirm logs show bypasses only for `ROOTS_HUB_INTRO_WITNESSED`, `ROOTS_HUB_GATE_OPENED`, `ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE`, or `OVERALL_PROGRESS_REACHED_LOBBY_HUB`.
- [ ] Confirm each bypass line says `saveFlagUnchanged=True`.
- [ ] Do not replay Lift Quest solely for this candidate. Its vanilla Lobby arrival remains protected by automated origin-route tests.

Retain `BepInEx/LogOutput.log` from startup through the final Lobby check.

## Follow-up diagnosis

The run emitted no `AREA ARRIVAL CONDITION BYPASSED` line because the reused save already reported the targeted native flags as true. The transition scoping and both observed no-cutscene arrivals passed, but fresh-state proof remains pending. Bottom-HUD snapshots resolved the object path but not its managed proxy and showed both previously suspected Roots bookkeeping flags already true. Treat the bottom control as a separate native `DifficultyToggler` initialization/interaction defect rather than extending the progression-flag override by guesswork.

### Failed native phase experiment

A separately versioned v0.67.66 diagnostic candidate read the exact Roots `DifficultyToggler` through its native `BuildState().CurrentPhase` path and confirmed phase `INVALID(0)`. This explains the unavailable control and its later recovery after broader vanilla progression. Direct native `SetPhase(IDLE)` was then rejected by an IL2CPP exception; subsequent verification continued to report phase `0`. The game remained running, but the candidate retried and was immediately withdrawn. The installed client and branch were restored to v0.67.65, and the v0.67.66 implementation was reverted so it cannot ship accidentally.

Future work must use the game's normal state-transition/request pipeline that owns `DifficultyToggler.SetPhase`, not direct native invocation. Do not repeat this experiment or ask the player to replay levels for it.

### Failed serialized stop-step experiment

Read-only v0.67.67/v0.67.68 diagnostics located two exact native sequence steps beneath `GateAndDifficultyRecommendation`: `AmproCalulatingAnimation` and `AmproCalulatingAnimationStop`. The explicitly approved v0.67.69 candidate invoked only the latter step's native `Trigger()` once while the Roots control was `INVALID(0)`. The call returned without an IL2CPP exception, but immediate verification still reported phase `0`; the bottom character/difficulty HUD remained non-interactive. The candidate logged `retry=False` and made no second attempt.

Do not ship or retry this repair. The parent sequence supplies additional runtime context that is absent when its child stop step is invoked alone. Further diagnosis must be read-only and confirm the step's serialized `phase` value and linked `DifficultyToggler` identity before considering any broader sequence operation.

### Failed corrected idle-value experiment

The read-only v0.67.70 diagnostic proved the enum constants are `INVALID=0`, `IDLE=10`, and `CALCULATING=20`. It also proved both serialized phase steps reference the exact active Roots `DifficultyToggler`; the calculating step holds `20` and the stop step holds `10`. The explicitly approved v0.67.71 candidate therefore called `DifficultyToggler.SetPhase(10)` once while the current phase was `0`. The call returned without an exception, but immediate `BuildState().CurrentPhase` verification remained `0`, and the HUD remained non-interactive. No retry occurred.

The correct enum value alone is insufficient: `SetPhase` also depends on broader game-owned state-machine context. Remove v0.67.71 and do not attempt further direct phase mutation. Investigate the complete `GateAndDifficultyRecommendation` parent sequence offline before requesting more player testing.

### Failed parent-sequence condition experiment

The v0.67.72 candidate stopped mutating the native `DifficultyToggler` directly. It instead attempted to let the complete vanilla `GateAndDifficultyRecommendation` sequence run by temporarily reporting its completion condition false, while suppressing only `AssignAndCommentOnMusicDifficultySequenceStep.Begin`. A fresh-save run reached Music Lab through direct start, then entered Roots. The Roots arrival cutscene still played, the bottom character/difficulty HUD did not respond, and the native phase remained `INVALID(0)`. Neither the condition-override log nor the difficulty-comment suppression log appeared, proving the candidate did not intercept the path controlling this presentation.

The same run established an additional acceptance requirement: in vanilla, the covered box beside the Roots crown door becomes uncovered when difficulty selection is unlocked. A complete repair must suppress the unwanted arrival commentary while reproducing the full vanilla unlock state: responsive bottom HUD, native `IDLE(10)` phase, and uncovered box. The v0.67.72 candidate was withdrawn. Do not request another gameplay replay until read-only investigation identifies the actual sequence entry and conditions.

### Exact owner and v0.67.75 acceptance

Read-only v0.67.73/v0.67.74 identified `Root/GameRoom_Hub2_Logic/ProgressionLogic/PlayGateAndDifficultyScene` as the exact `OneTimeGameProgressionLogic` owner. It persists flag `210` (`ROOTS_HUB_GATE_OPENED`), waits on the compound Level-1 score condition, and owns the full `GateAndDifficultyRecommendation` sequence. On the failed AP arrival the sequence had never started.

Client v0.67.75 moves the three approved Roots bookkeeping writes before the Hub2 transition and lets this native owner take its serialized skip path during room initialization. Test on the same fresh save without playing Level 1:

- [ ] Confirm `ROOTS PRE-ENTRY BOOTSTRAP VERIFIED` appears before the Hub2 transition.
- [ ] Confirm the Roots arrival cutscene does not play and control is retained.
- [ ] Confirm the box beside the crown door is uncovered.
- [ ] Confirm the bottom character/difficulty HUD responds and both Normal and Pro remain selectable.
- [ ] Confirm no Level-1 completion, Star, or AP check was granted.

The v0.67.75 run reached `ROOTS PRE-ENTRY BOOTSTRAP FALLBACK`: Plant Pipes reconciliation had constructed a stateless `PlayerSaveRequestProcessor`, but that instance was not shared with this subsystem. All three flags remained false. The no-cutscene and no-false-reward checks passed; the cover and HUD checks failed. v0.67.76 adds only that missing processor handoff and repeats the same acceptance list.

The explicitly approved v0.67.80 occurrence-path candidate replayed the unwanted post-Level-1 cutscene but did not uncover the Sophisticated Computer or activate the bottom HUD. No Star or AP check was emitted. This disproves both the skip and occurrence reactors as a usable AP initialization path. The occurrence invocation was removed and must not be retried.

### v0.67.94 accepted Roots presentation

The clean v0.67.94 candidate retained the narrowly scoped `Cover-tofb` hide and exact `DifficultyTogglerState.CurrentPhase` normalization established in v0.67.85. Live acceptance on 2026-08-28 confirmed that the Roots arrival cutscene did not play and the Sophisticated Computer was uncovered and usable. The client emitted no false Star or AP check. All 15 client regression projects passed, the game-aware release build succeeded, the installed DLL matched the verified build hash, BepInEx loaded v0.67.94, and the retired bottom-HUD fallback hooks were absent.

The separate bottom character/difficulty shortcut remains non-interactive and is still an open issue. The Sophisticated Computer is the supported Roots difficulty control until that shortcut can be repaired without replaying progression or synthesizing input.
