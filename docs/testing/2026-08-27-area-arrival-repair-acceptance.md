# v0.67.65 / v0.19 Area-Arrival Repair Acceptance

**Status:** Automated candidate; not yet installed or gameplay-verified

Use the existing v0.19 seed and tested save. This focused run does not require replaying levels.

## Setup

- [ ] Confirm the first client line is `[SCRC-AP] v0.67.65 loading.`
- [ ] Confirm the client reconnects to the existing compatible room without an error.

## Roots AP-phone arrival

- [ ] Start in Music Lab and travel to Roots through the AP-unlocked phone.
- [ ] Confirm `AREA ARRIVAL OVERRIDE ARMED kind='RootsPhone'` appears.
- [ ] Confirm no Roots first-arrival cutscene plays and player control is retained.
- [ ] Confirm the campaign Star counter appears.
- [ ] Confirm the bottom character/difficulty control is interactive.
- [ ] Return to Music Lab, revisit Roots, and confirm normal travel remains stable.

## Lobby AP-phone arrival

- [ ] From Music Lab, travel to Lobby through the AP-unlocked phone.
- [ ] Confirm `AREA ARRIVAL OVERRIDE ARMED kind='LobbyPhone'` appears.
- [ ] Confirm the Lift Quest first-arrival presentation does not play.
- [ ] Confirm player control, Lobby routing, and the bottom character/difficulty control remain normal.

## Safety boundary

- [ ] Confirm logs show bypasses only for `ROOTS_HUB_INTRO_WITNESSED`, `ROOTS_HUB_GATE_OPENED`, `ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE`, or `OVERALL_PROGRESS_REACHED_LOBBY_HUB`.
- [ ] Confirm each bypass line says `saveFlagUnchanged=True`.
- [ ] Do not replay Lift Quest solely for this candidate. Its vanilla Lobby arrival remains protected by automated origin-route tests.

Retain `BepInEx/LogOutput.log` from startup through the final Lobby check.
