> Historical record. Dates, versions and candidate statuses below describe that checkpoint; consult the [current documentation](../../INDEX.md).

# Montage movement comparison — October 11, 2026

## New evidence

User supplied a 440.36-second Medal video and three F8 captures, saved at 20:49:11, 20:49:57, and 20:50:36 America/New_York (filenames use October 12 UTC). Report: cat/dogs are too slow for scripted objectives; Hypno Pan movement was normal this run. Preserved local evidence is under work/movement-20261011 in the task workspace, not repository artifacts.

All three captures have 120 samples. Render rates are 146.26, 158.10, and 150.68 FPS; physics stays around 50 Hz with timeScale 1. Second/third captures have maximum frame deltas 18.63/18.21 ms. The first has a 55.86-ms maximum, insufficient to explain sustained slow movement. Actor positions and root transforms match at sampled instants. Facing/controller bindings remain unavailable, so facing-state causality is not established.

In the second capture's first 1.44 seconds, kinematic MeatCat body -58838 moves horizontally about 1.2–1.96 units/s with speed configured to 5 and recorded per-update commands around 0.0215–0.0363 units. Dogs -59126/-59364 are dynamic and move about 2.38–2.89 units/s with speed configured to 2.5. Do not pool whole-capture speeds: scripted resets and the cat falling below the scene produce large discontinuities; idle rows do not establish underspeed. The sampled dogs do not establish a speed-setting defect; their reported objective timing/facing problem remains open.

Native investigation confirms Motor.UpdateControlledMovementForThisUpdate (0x91cfd0) checks Rigidbody.isKinematic (resolved icall at string RVA 0x2ae6950) and its kinematic branch calls Rigidbody.MovePosition_Injected (string RVA 0x2ae6e80) using current body position plus supplied displacement. Multiple render-sized commands before a physics update are a plausible lost-displacement mechanism. The recorder still does not measure actual command counts or supplied motor time, so this remains a hypothesis, not a confirmed fix or proof the mod is innocent.

## Approved comparison probe

User approved a temporary 50 FPS comparison. F9 in a gameplay room snapshots Application.targetFrameRate and QualitySettings.vSyncCount, sets vsync 0 and target 50, and restores both on F9, after 120 seconds of real time, a room change, component disable/destruction, or application quit. It writes no saved preferences, fullscreen settings, time scale, fixedDeltaTime, motor fields, or progression. It is available only with the existing developer movement component. No setting changes occur on startup. F8 retains its bounded 12-second diagnostic; headers record cap state and rendering settings, and a cap transition ends an active capture to avoid mixing conditions.

Unit tests first failed for the absent cap, then all 11 passed, including repeated-start snapshot preservation, timeout/manual/room-change restoration, and rollback after partial apply failure. MovementCapture tests pass. Release build passes with 0 errors and an NU1900 vulnerability-feed warning. Not installed; comparison result pending.

Next: install after user confirms the game is closed; enter Montage, activate F9 near the cat/dog sequence, then F8 while they move without Hypno Pan. Compare traversal and facing with the preserved uncapped run. If improved, follow up with a narrowly scoped movement-call investigation before choosing a permanent solution; do not silently impose a global frame cap.
