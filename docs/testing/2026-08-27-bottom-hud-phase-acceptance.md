# v0.67.66 / v0.19 Bottom-HUD Phase Acceptance

**Status:** Automated candidate; not yet installed or gameplay-verified

This is a focused native-interaction test. Use the existing seed and save; do not replay levels.

## Setup and safety

- [ ] Close the game before installation.
- [ ] Confirm `[SCRC-AP] v0.67.66 loading.` and a successful AP connection.
- [ ] Confirm no crash, access violation, or native invocation exception occurs during Music Lab initialization.

## Music Lab and Roots

- [ ] In Music Lab, try the bottom character/difficulty control and confirm character selection opens normally.
- [ ] Cancel without changing anything, reopen it, select the existing difficulty normally, and return to the hub.
- [ ] Travel to Roots and confirm the control remains interactive there.
- [ ] Confirm the Roots Star counter remains visible and no first-arrival cutscene plays.

## Required log evidence

- [ ] For any native `INVALID(0)` state, confirm exactly one `BOTTOM HUD PHASE normalized` line ending in verified `IDLE(1)`.
- [ ] For an already-ready control, confirm `BOTTOM HUD PHASE preserved` and no normalization.
- [ ] Confirm no log claims that a difficulty, character, or progression flag was selected or changed by the repair.

Stop immediately and close the game if it crashes, freezes, opens character selection without input, changes difficulty by itself, or repeatedly alternates phase.
