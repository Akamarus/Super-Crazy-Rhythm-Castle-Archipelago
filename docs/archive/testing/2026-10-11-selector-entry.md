> Historical record. Dates, versions and candidate statuses below describe that checkpoint; consult the [current documentation](../../INDEX.md).

# Change menu blocked after direct intro skip — October 11

Live diagnostic on slot4: Music Lab room phase RUNNING, level phase IN_LEVEL, menuRoomAllowed=false, local player's Change action permission=true, difficulty assignment flag verified. This proves the menu entry guard, rather than the assignment flag or input permission, rejects the request.

Native PlayerEnquiries.InARunningRoomThatIsNotALevel at RVA0x7a2e30 requires running room and no active level phase. AllPlayerDetailsSwitcherUI.RunUpdateForPlayer checks it before hold input. Existing intro redirect replaces GameRoom_04A with Hub6 and clears PartOfLoadLevel but leaves level state alive.

Candidate now queues LevelChangeAction.Close through QueueLevelChangeForNextRoomChangeRequest and RequestSystem.SubmitRequest only after a successful intro destination rewrite. Native room transition enacts the closure; no completion, score, Star, AP check, or progression flag is synthesized. Native CloseLevelRequest tears down level systems. No broad phase setter or all-hubs override.

Six focused dispatcher checks pass after the positive case failed before implementation. Build and related direct-start/selector tests pass. Native queued request dispatch and menu behavior still require acceptance after restart; diagnostic remains enabled to distinguish submission failure from a successful native close. No installation in this step.
