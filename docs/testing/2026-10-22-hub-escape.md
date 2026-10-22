# Cell Tower escape acceptance — 2026-10-22

Final status: both reported routes passed user testing; released in v0.29.2 / Client 0.76.2. Earlier failures below are historical investigation steps.

## Scope and native evidence

Hub5A external entrance objects `DoorToHR5B_Open` and `DoorToHR5B_Closed` use SetActiveOnRoomInitialise and opposing conditions for native attempt flag 116001 (internal Level_16, displayed Central Mainframe). The override is restricted to these exact object paths, authenticated compatible sessions and Cell Tower Access ownership. Old Tom's separately conditioned door remains native.

Cell Tower Hub5A exits into either Lobby hub redirect to Music Lab until `LOBBY_HUB_FISH_TEARS_DEPOSITED` reads true; unreadable state uses the safe fallback. The old explicit spawn is cleared. Other origins and completed-story transitions retain existing routing, including ownership checks.

No quest flags, AP checks, item grants, save schema, APWorld rules or display settings change. The original candidate was built in isolation before installation.

## Automated evidence

- New HubEscape regression project initially failed due to missing policy, then passed after implementation.
- Cases cover both gate objects, exact-path rejection, ownership/session guards, incomplete/unknown/completed story state, unrelated destinations and origins.
- Existing RootsPresentation regressions passed.
- Client Release build passed with zero warnings/errors.

## Native acceptance test matrix

1. With Cell Tower Access but no Lobby Access/Plant Pipes and no Central Mainframe attempt, phone into Cell Tower, enter the lower room and return through the external gate to the phone.
2. With Lobby Access but unfinished Fish Tears delivery, use the Hub5A Lobby exit: arrive safely in Music Lab, then use the ordinary Lobby phone route.
3. Verify those transitions send no checks, grant no items, and do not mark Central Mainframe/Old Tom/letters/Fish Tears complete.
4. Repeat after area return; verify normal completed-story Lobby exit still works.

Keep the existing seed and saves; back up the installed client before any separately approved deployment.

### Follow-up: exit interaction

User acceptance: Music Lab fallback passed; gate appeared open but had no interaction. Native Switch 1820 on ExternalEntrance independently requires shared condition 1752, which the initial visual repair did not change. The revised candidate clears only this exact switch's scene-local requiredCondition during the already scoped initialization callback. The shared condition, level-attempt flag, Old Tom door and normal interaction execution remain unchanged. Gate traversal still needs gameplay acceptance.

### Native-wrapper follow-up

The second gameplay test still failed. Runtime log reported the combined Switch/property lookup failure. An offline load of the installed generated `Switch` type reproduced a TypeLoadException in its generic WorldEntity base. The next candidate uses the established native-component and reference-setter approach instead of constructing that managed wrapper. It checks the exact Switch class, requiredCondition class and flag 116001 before clearing the scene-local reference, then reads it back. Native-shaped tests cover correct writes, idempotence, wrong flags/classes and failed writes. No deployment or gameplay success is claimed for this revision yet.

### Gameplay acceptance

The user confirmed the installed native-interaction revision works: the return-gate interaction allows traversal toward the Cell Tower phone. The Music Lab fallback had already passed the earlier test. Both reported soft-lock routes now have targeted user acceptance. Candidate SHA256: 920C35E8DAAEDBED13D86EFA51F80509BBD3A52138A15217612117BEDEEAC5AA. This is not exhaustive testing of all story orders or multiplayer, and publication is tracked separately in the release notes.

