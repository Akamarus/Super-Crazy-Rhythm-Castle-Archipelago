# Final-level transition crash candidate

Status: first candidate installed but safely refused its incorrect event-size assumption at startup; corrected candidate built, not installed, native acceptance pending.

## Evidence
The final-level transition from GameRoom_28A to GameRoom_28B crashed repeatedly, including with the temporary Bunker staging/capture helper disabled. The small dump lacked callback metadata. A first-chance access-violation full dump identified DynamicClass.(il2cpp -> managed) HandleEvent under GameRoomManager.UpdateRoomChangeTasks. Its dynamic IL resolves TurnOffObjectAfterRegistered and GameRoomRegisteredExistingEntitiesEvent, and calls il2cpp_value_box before reaching the patched callback. This is the Cell Tower raised-path registration hook.

The initial unhandled-exception monitor observed immediate process exit without a dump; the corrected first-chance C0000005 filter was validated in a disposable process before the full game dump was captured. Raw dumps and logs are private local artifacts, not repository content.

## Repair
Replace the Harmony event bridge with a native callback for the pointer-sized GameObject reference carried by this event. Validate the native event size, backing field type/offset and exact instance method signature before installation. Retain the trampoline and delegate for process lifetime, call the original handler first for every object, and apply the existing exact Old Tom path policy afterward. No APWorld, network IDs, save flags or victory rules change in this repair.

## Verification
- Regression guard failed against the original Harmony bridge, passes against the native bridge; it prevents reintroducing this known unsafe event boxing but does not emulate the game.
- Release build: zero warnings/errors, installation skipped.
- HubEscape tests: exit, Cell Tower path ownership/exact-path guards and Lobby door policy pass.
- APStars: 69 assertions pass.
- LevelCompletion: transport/session/goal policy tests pass from repository root.
- Repository validation passes.

Native acceptance still required: native hook startup validation, final-level transition, and preserved Old Tom path traversal. Do not describe this as a confirmed gameplay fix until tested. The four Gecko steps are observed; final True Ending victory detection remains unverified.

## Startup correction
The first candidate incorrectly assumed this event was empty like GameRoomReadyToRunEvent. Its runtime size guard rejected installation. Offline inspection of the actual interop assembly shows GameRoomRegisteredExistingEntitiesEvent contains a RootGameObject reference. The corrected delegate forwards that pointer-sized value unchanged. The full dump identifies the Harmony bridge failure, but does not itself establish an empty event. The earlier empty-event premise is superseded.

## Corrected candidate native result
Installed with user approval; SHA256 C29FD87C172F31A969EEACF3F177D68FBBBD09D6B9CB1B91DA98C8872466D9CE. Startup confirmed native event ABI validation and hook installation. User completed the boss; logs confirm the previously crashing 28A-to-28B transition, persisted successful result and return to Royal Corridor. The existing AP goal message was sent. This targeted crash reproduction now passes. Old Tom route recheck and separately reading the saved True Ending completion flag remain outstanding. No new True Ending goal has shipped.
