# Cell Tower Access path candidate

Approved scope: let Cell Tower Access open the physical route to Old Tom's note and the Star Eater without completing the nectar quest. Keep Violance/Stars and native note interaction requirements.

Native evidence: BeesEscape's ApplyEscapeEffects hides locked cells (3754), hides quest notes (3760), and shows unlocked cells (3368). Old Tom's target GameObjects are HangingCell_Lowered (873) and HangingCell_Raised (1202). Bees cells are separate targets. TurnOffObjectAfterRegistered 3807 disables the raised cell after GameRoomRegisteredExistingEntitiesEvent.

Candidate uses that exact post-registration boundary to swap only Old Tom's two cell objects for an authenticated Cell Tower Access owner. No full escape sequence, progression flag, inventory change or AP check is invoked. Nectar delivery remains available.

The pre-nectar note/Star Eater/onward-door sequence has user acceptance below. Normal nectar delivery after using the early path remains the outstanding native regression. Cold Storage opening was previously tested in the post-nectar branch; retain its other conservative requirements.

## Gameplay acceptance — October 22, 2026

The user confirmed the pre-nectar route works like vanilla: read the note in front of Old Tom, leave and re-enter the area, feed the now-accessible Star Eater, then reach the onward door after it moves. Three screenshots show the connected path, feed prompt, and onward door prompt. This validates that sequence, not skipping the note. Subsequent normal nectar delivery remains a separate untested regression.

Newly generated seeds use `cell_tower_open_route: true` and the `cell-tower-path-1` implementation marker. The marker is inserted before the existing generation-foundation portion so unchanged subsystem suffix contracts remain intact; the AP Star contract validates the exact whole identity and matching Boolean capability. Old clients reject that identity. Tracker preserves both values across reconstruction; legacy seeds without the capability retain the old predecessor graph. No item/location IDs change. Thief Prince retains Cell Tower Access and its own Star threshold; Star Eater retains the achievable Thief Prince/note route, Violance and its threshold. Cold Storage retains its other requirements; nectar delivery retains its separate requirements.

## Final automated validation

All 191 APWorld/repository tests and 10 packaging tests passed. Stale assertions about archived documentation versions were updated to check current metadata and public seed contracts; the packaging subprocess now uses the test runner's Python executable. All 45 client test projects passed (the production-connection fixture was updated and rerun after the new readiness dependency). The Release build completed with zero warnings/errors and repository validation passed.

A real Archipelago 0.6.8 three-player generation with Normal/individual, Hard/pairs and Expert/triples completed and passed beatability. Each player's slot data and Tracker interpretation retained the new route capability. This was an isolated automated generation; the installed playtest seed/save/config were not changed.

Code review identified missing native-hook readiness enforcement. The client now records successful path-hook installation and passes readiness explicitly through production login; new route seeds reject an unavailable hook, while legacy contracts remain supported. Tests exercise both rejection and successful login with the new identity, including expanded check activation. Runtime opening on compatible legacy seeds is an intentional additive escape/access repair; their Tracker reconstruction retains the original conservative requirements.

The matching contract changes are built but not installed or published. The installed physical path candidate already has the user's native acceptance. Normal nectar delivery after using the early route remains the targeted in-game regression before release preparation.
