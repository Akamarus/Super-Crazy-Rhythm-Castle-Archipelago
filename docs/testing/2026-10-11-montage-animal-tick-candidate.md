# Montage animal update candidate — October 11, 2026

Status: installed with explicit approval and gameplay accepted by the user on October 11, 2026; source option remains disabled by default pending release integration.

## Evidence and scope

A temporary 50 FPS comparison was accepted by the player; uncapped captures showed the cat advancing too slowly while rendering exceeded physics cadence. The previous nullable motor-method Harmony bridge crashed before its decision code and remains uninstalled.

This candidate uses a primitive native void(instance, float elapsed, methodInfo) boundary at MeatAnimalCharacter.RunUpdateWithDeltaTime, whose local native trace matches that signature. In GameRoom_22 only, each animal is admitted once per physics clock value, carrying forward supplied elapsed time. Rendering and physics settings are unchanged. No transform writes or global frame cap are introduced.

The hook gates the whole animal update, including conflict and sleep behavior; this is an experiment and must be accepted in gameplay. Native method discovery and real IL2CPP dispatch have not been validated by the managed signature fixture.

## Review corrections

Unload disables admission before disposing the detour; delegates remain rooted, including on disposal failure. Save selection, save rebuild, and room changes clear timing state. The bounded actor cache replaces inactive entries at capacity rather than accumulating lifetime actors indefinitely. Decision failures pass through the native call; exceptions from the original update are never caught and retried.

## Verification

- 28 managed timing checks passed, including 30–240 FPS simulations, elapsed time, actor independence, pauses, rewinds, stale actors and bounded storage.
- Three unmanaged primitive signature round trips passed; this is ABI coverage, not native gameplay validation.
- Release build passed with zero errors; two NU1900 warnings reflect an unavailable package vulnerability feed.
- Installation was skipped and the installed working DLL was left unchanged.

## Acceptance and rollback

After explicit deployment approval, back up installed DLL/config, install the candidate, enable Developer.EnableMontageAnimalTickCandidate, and launch without an FPS cap. Check launch and entry first, then capture a short Montage segment containing the cat and dogs. Verify facing, objective timing, conflict/sleep states and Hypno Pan interactions. F8 captures diagnostics; do not use F9 for this uncapped comparison. If launch or entry crashes, restore the backed-up working DLL/config before further play. No seed, save or server modifications are required.

## Live acceptance

The user reported “Worked much better” and signed off on this Montage movement fix. Both captures show montageAnimalTick=True with three animals; measured render rates were 170.99 and 164.98 FPS, confirming this trial did not rely on the temporary 50 FPS cap. The second capture ended with supplied and executed elapsed time both 297.3366 seconds. The launch log confirms primitive-hook attachment and contains no matched Exception/error entries in the acceptance inspection. These observations support this Montage sign-off, not a claim that all previously reported movement defects in other rooms are resolved.

Accepted installed SHA256: 073F23AD70040215D98EFD1E609D6C4BD367327B53E1BB787E89ED4E372D3DA3. Local evidence is retained outside Git in work/movement-animal-accepted-20261011 (two captures, launch log, acceptance summary); rollback remains in work/movement-animal-install-20261011-223533. Next: integrate the accepted behavior into the release configuration and remove obsolete experimental machinery, with regression verification before packaging. No release was published by this acceptance step.

## Release integration cleanup

The same accepted timing policy and primitive native hook now initialize without a developer opt-in. The rejected nullable motor hook and its clock are removed, along with the temporary F9 cap and their obsolete test projects. F8 captures remain. Cleanup does not expand the hook beyond Montage or alter its gameplay timing policy. The previously accepted installed DLL is retained until a separate deployment.
