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
