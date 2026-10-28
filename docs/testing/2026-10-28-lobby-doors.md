# Lobby doors candidate

User requested always-accessible Demolition Training and School Trip doors in Lobby, while retaining gameplay rewards and Gecko quest progression.

Native asset evidence: Training door `Level19Door` references `IsGameProgressionFlagSetCondition` flag649; School door `Level20Door` references `LevelCompleteCondition` for Level19/default. Separate covers are `LevelLockedCover-tofb` and `LevelLockedCover`. Exact paths are encoded in LobbyDoorPolicy. No certificate, mail or level-completion flag is granted by the repair.

Runtime patches run before door state construction and interaction availability evaluation. They clear only the validated scene-local requiredCondition reference and hide the matching sibling cover. Original door logic and AP Star gate callbacks still run. No global condition bypass or per-frame scene scan is added.

Acceptance pending: enter both doors without Boring Room/mail/cutscene/Training completion, check subthreshold Star blocking, and verify School Trip Gecko variant and later normal completion/certificate award. Existing async saves and installed main client remain untouched during preparation.

Validation: all 192 APWorld tests passed; HubEscape/Lobby door policy regressions passed; 69 AP Star assertions and LevelCompletion connection/session regressions passed; Release build succeeded with zero warnings/errors. Real AP 0.6.8 three-player generation and beatability passed for Normal/individual, Hard/pairs and Expert/triples. Independent code review found no actionable correctness issues under the documented legacy compatibility choice. Native acceptance remains pending; candidate not installed or published.


Runtime correction: initial installation did not activate because loading generated LevelEntranceDoor reproduces a generic-constraint TypeLoadException. Replaced its hook with verified-loadable LevelEntranceDoorView.Awake and RefreshToMatchState, locating the native LevelEntranceDoor component by class name and using the existing guarded native setter. This supersedes the earlier callback description. Native acceptance remains pending.

