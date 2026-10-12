# Early Regular/Pro availability candidate — October 6, 2026

Status: offline candidate; no installation or live-save mutation performed. The parent integration wires the Unity tick and records combined-suite results.

## Native evidence

Installed native metadata maps `ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE` to 205 (0xcd), and `ROOTS_HUB_GATE_OPENED` to 210. `GameProgressionEnquiries.HavePlayersBeenAssignedMusicDifficulty` at RVA 0x7d8ec0 reads flag205. `AllPlayerDetailsSwitcherUI.CycleEditModeDown` at RVA 0x8cd470 inlines that flag read, ORs it with the room's forced difficulty-select permission, and only then enters difficulty-edit mode 3. `CycleEditModeUp` also inlines flag205. Hooking the convenience enquiry alone would miss those native callers.

`StartEditing` enters character-edit mode 2. `RunUpdateForPlayer` separately checks a running non-level room and the player's action permission before accepting the hold-to-edit input. These guards remain native. Left/right difficulty changes use the existing native selection request pathway.

`RootsStartupBootstrap` is configured and receives slot data but its Tick/processor-capture methods are unused. The active `RootsIntroCutsceneBypass` submits assignment completion during Roots entry. Consequently that path does not establish early availability immediately after direct start in Music Lab. `DifficultyAvailabilityPolicy` is also unreferenced by production runtime.

The prior v0.67.94 acceptance established the Roots computer works, while the bottom shortcut remained noninteractive. This candidate addresses the demonstrated missing startup flag path; the older report of noninteraction with flags already true means it cannot establish that every shortcut issue is fixed without gameplay testing.

## Narrow correction

`DifficultySelectionAvailability.TickUnity` uses the existing stable selected-save gate. It requires enabled area routing, compatible slot data, and an authenticated compatible session. It polls at most once per second and submits only assignment-complete=true through the existing native save request processor if the flag was readable and false. It verifies native readback, stops resubmission when already true, and permits at most four submissions per selected-save identity. Unknown state is left untouched; a new selected-save identity has a fresh retry budget.

This intentionally persists the existing **assignment-complete bookkeeping marker**. Disabling the client does not undo that marker, and a later native assignment presentation may regard its assignment bookkeeping as complete. That matches the authorized skipped-startup behavior and the flag already written by Roots pre-entry. It does not select Regular or Pro, set gate210, synthesize Level1 completion, award Stars, or emit AP checks. There are no new native hooks, phase mutations, scripted input, UI activation overrides, or progression sequences.

## Verification and remaining acceptance

The production-file test project checks stable-save gating, disabled/incompatible/disconnected isolation, unreadable state, exact processor/flag/value submission, readback, delayed retry, one-second cadence, already-owned behavior, four-attempt limit, and save-identity isolation. Eight positive behavior checks failed before the implementation, then the full project passed. Release build without installation passed with the existing NU1900 vulnerability-feed connectivity warning only.

After a separately authorized installation, use a fresh compatible AP save before Level1 completion or touching the Roots computer. Confirm the native flag is verified, the bottom control accepts its normal input, and both Regular and Pro can be selected without changing the already-selected choice automatically. Confirm no false Level1 check/score/Star, and compare disabled/incompatible sessions. If the flag verifies but the shortcut still fails, inspect native room-running/action permission and UI lifecycle rather than writing additional story flags. The current async installation remains unchanged.
