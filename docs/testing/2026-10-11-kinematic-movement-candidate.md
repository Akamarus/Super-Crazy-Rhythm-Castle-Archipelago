## October 11 movement candidate rejected after native crash

The uncapped candidate caused repeated IL2CPP native-to-managed UpdateMovement trampoline failures in Buffer.Memmove before the managed prefix could execute. Managed fixture tests did not cover this boundary. Startup activation has been removed, and the installed DLL was rolled back to SHA256 DF60F8020BEA0A15DDE4FE239AAF0FCF70B040294CB0F942866424E91EC7600C with its candidate setting disabled. Saves were not modified. The uncapped movement fix remains unresolved; do not reinstall the rejected binary. A safe native hook boundary must be established before another live trial.

# Uncapped kinematic movement candidate � October 11

## Evidence and scope

The user confirmed normal Montage movement under the explicit 50 FPS test. Two captures verify test=True, target=50, vsync=0, measured 49.99/49.98 FPS and approximately 50 Hz physics. Faulty runs rendered at 140�158 FPS. This establishes a successful workaround and strong frame-rate dependence; it does not establish every movement defect's cause.

Native code shows DirectableCharacterMotor.UpdateMovement routes following through CalculateFollowMovement using supplied elapsed time. Motor.UpdateControlledMovementForThisUpdate's kinematic branch calls Rigidbody.MovePosition with current position plus frame-sized displacement. This can replace pending position requests before physics consumes them. Capture evidence shows the cat is kinematic and underspeed. This candidate tests that specific mechanism, not the dynamic dogs or unrelated player movement.

## Candidate

Disabled by default: Developer.EnableMontageKinematicTimingCandidate. Exact Harmony prefix on DirectableCharacterMotor.UpdateMovement. Only GameRoom_22, MeatCat(Clone), kinematic body, running room, positive time scale. Collect supplied movement time between physics clock changes and pass the sum into the original native method once per boundary. Native target selection, following, floor checks and speed calculations remain original. No direct transform/body writes, physics settings, or FPS cap. Low FPS retains all supplied time. Invalid input, reversed clock, excessive pending time, mode changes, room/save transitions reset queued time. Boundary exceptions disable the candidate and retain native behavior. Counters in F8 headers/footers verify activation without per-call logging.

Timing aggregation may add up to one physics interval of control-response latency. It still requires live confirmation that the Harmony boundary intercepts actual calls and the native motor behaves correctly with aggregated time. Scripted stop/target changes and Hypno Pan transitions require regression coverage in game. Visual facing is a separate unresolved question; no broad patch has been applied to its tracking logic.

## Verification

Movement clock test failed first on repeated render commands, then passed 15 checks including simulated 30/50/60/120/144/165/240 FPS against 50 Hz physics. Eleven native-adapter fixture checks pass for scope, body modes, room activity, supplied time and reset; these are managed fixtures, not proof of IL2CPP execution. MovementCapture suite passes and frame-rate probe has 11 passing checks. Release build passes (NU1900 feed warning). No installation yet, no native success claim.

## Next acceptance

With explicit installation approval and game closed, back up DLL/config, install candidate and enable only the new developer switch. Keep original rendering settings, do not press F9. Enter Montage, use F8 during cat movement and inspect the repair counters plus actual speed, facing and objective timing. Retain the previous build for rollback. Do not publish or generalize the scope until this test discriminates the mechanism.
