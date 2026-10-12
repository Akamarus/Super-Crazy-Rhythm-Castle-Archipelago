> Historical record. Dates, versions and candidate statuses below describe that checkpoint; consult the [current documentation](../../INDEX.md).

# Star-blocked entrance retry — October 11

User reported that an entrance rejected for insufficient AP Stars stayed unusable after Stars arrived until reloading the area. Live log records Level_12 blocked at 0/7 before receipt of ten Stars.

Native inspection: LevelPreviewUI.TriggerLevelStart (0x878da0) submits the entrance-open request before StartLevelRequest. LevelEntranceDoor.OpenDoor (0xa4eab0) sets LevelOpened, and IsInteractionEnabled (0xa4eb00) rejects that state. The old final request prefix prevents loading but cannot undo those earlier side effects.

Added an exact Harmony prefix on LevelPreviewUI.TriggerLevelStart. It reads the current preview identity and applies the existing AP admission policy before native side effects. Denials use the existing in-game message. Admitted calls retain all native processing; only the later StartLevelRequest records a victory attempt. Hook readiness now requires four boundaries. No periodic scene scans or native score writes.

Regression: hook fixture initially failed because the early boundary was absent, then passed all 13 checks including denial followed by Stars and same-preview retry, retained native restrictions, and no premature admission capture. Release build succeeded with 0 errors (NU1900 vulnerability-feed warning). Not installed; live acceptance pending.

Next test: on a fresh room instance, try a level below its Star threshold, receive enough AP Stars, then retry without leaving. Verify entry, denial message, and normal launch. Already-disabled entrances from the previous build still require a room reload once; this repair prevents recurrence rather than guessing which native door states to reverse.
