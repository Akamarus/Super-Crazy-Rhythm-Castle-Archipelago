# Old Game Data and Car Battery pickup evidence

User requests both items be randomized. They are currently vanilla chest rewards;
their chest locations already send AP checks. Reuse those locations, do not add
duplicate pickup checks. No implementation or new IDs allocated in this capture.

Current installed maintenance candidate, recreated native slot 4, retained seed
21483200610759512211. User reports opening both chests just before inspection.
Preserved local log: music-lab-character-item-pickups-20260915.log.

| Item | Chest | Native collection flag | Existing AP check |
| --- | --- | --- | --- |
| Old Game Data | ManiacMemoryCardChest, 5 points | CLEAN_HUB_MEMORY_CARD_TAKEN (1104) | Music Lab - 5 Point Chest (187256169) |
| Car Battery | CatBatteryChest, 20 points | CLEAN_HUB_GHOST_CAT_BATTERY_REWARDED (1102) | Music Lab - 20 Point Chest (187256171) |

Log lines 2266/2271 and 2418/2423 respectively show collection event/request;
2269 and 2421 show the corresponding SENT CHECK. Further duplicate observations
are not additional distinct pickups. These messages do not include before/after
values, inventory grant flags, or hand-in consumption state.

Next: establish exact independent inventory grant and terminal hand-in flags for
each item, retain normal player-driven hand-ins, and use the existing chest checks.
Historical character rewards: Maniac from Old Game Data insertion; Meoo from
battery delivery, with PLAYABLE_CHARACTER_UNLOCKED_MEOO historically observed.
Do not substitute collection flags for inventory or replay a consumed item.
Character randomization is separately described in the approved broad design;
this pickup evidence alone does not establish its runtime implementation.

## Old Game Data hand-in

User performed the requested Game Garage hand-in and supplied a screenshot showing
"You got MANIAC". This confirms the visible character reward on current slot 4.
The retained log confirms entry into GameRoom_27, but the installed diagnostic
allowlist did not emit the memory-card consumption or Maniac-unlock flag event.
Do not claim exact native hand-in flags were captured. Preserve the screenshot
and log as old-game-data-maniac-unlock-20260915.png and
old-game-data-handin-20260915.log in local task artifacts.
Further exact mapping requires code/metadata inspection and focused observation;
prepare that before asking the user to consume Car Battery or repeat this event.

## Focused diagnostic candidate

Installed Assembly-CSharp metadata confirms eGameProgressionFlag identities:
CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM=1105,
LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM=127101,
LEVEL_27_MEMORY_CARD_SCGMD_COLLECTED=127102, and
PLAYABLE_CHARACTER_UNLOCKED_MEOO=2001. These names are proven enum members,
not proof of live setter or hand-in semantics. Do not assume COLLECTED means
inserted. No separate Maniac-named progression enum was found.

Extended existing QuestActionDiagnosticPolicy for GameRoom_Hub6 and GameRoom_27
with two item identities. F5 reads the six exact collection/inventory/character
flags once per room/process; existing callbacks capture values and consumption.
Existing per-channel 64-entry limits and deduplication remain. No frame scans,
progression mutations, AP checks or new item IDs added by this diagnostic.

Regression test failed for the missing memory-card event, then all 14 QuestActions
tests passed with the implementation. Release build: zero errors, four existing
nullable warnings and unavailable NuGet audit feed warning. Installation was
blocked by automatic approval review pending explicit approval of this candidate;
no live installation or restart occurred from that attempt.

User explicitly approved candidate 75720B10. Installed verified full SHA-256
75720B1093C2B2ECC8A16904FA1525FEC21DDCDD541DDB02A9709D0CA1DCE078,
with prior DLL backed up outside the plugin directory. Game closed normally and
relaunched through Steam; current native slot 4 activated. Awaiting Music Lab F5
baseline and player-driven battery hand-in capture.

## Lobby correction

User clarified Meoo's battery hand-in occurs in the Lobby, not Music Lab.
Extended only battery candidate events and snapshots to GameRoom_Hub1A/Hub1B;
memory-card coverage remains Music Lab/Game Garage. Observed current log includes
Lobby Hub1A transition. Regression failed on missing Lobby battery capture before
this change, then all 14 tests passed. Build and diff checks pass, same 5 warnings.
Candidate A6C8A08A1F1E1125E6A99C18A24F230A13E33F4BE353776A88C545C432BC05EA
is prepared, not installed; explicit candidate deployment approval is required.

User approved A6C8A08A; installed DLL verified against full candidate hash. Prior DLL backed up, game closed normally and launched through Steam. Awaiting Lobby F5 baseline and battery hand-in.

## Battery hand-in captured

User confirms Meoo unlocked. In GameRoom_Hub1A, baseline snapshot shows
CLEAN_HUB_GHOST_CAT_BATTERY_REWARDED=True,
CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM=True, and
PLAYABLE_CHARACTER_UNLOCKED_MEOO=False. Hand-in emits battery bag True->False,
then Meoo unlock False->True; corresponding requests agree. Preserved log:
car-battery-meoo-handin-20260915.log (snapshot lines 328-330, events 1080/1083).
This establishes distinct chest collection, held item, consumption and character
reward identities for this run, not AP item receipt/replay implementation.

Prior-build post-Maniac snapshot, captured before Lobby logging update, shows
CLEAN_HUB_MEMORY_CARD_TAKEN=True, LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM=False,
and LEVEL_27_MEMORY_CARD_SCGMD_COLLECTED=False after a restart. Thus COLLECTED
is not supported as a terminal Maniac hand-in marker and must not be used as one.
Evidence: character-items-before-lobby-diagnostic-20260915.log lines 403-405.

## Randomization candidate implemented

Client v0.71.0 / APWorld v0.25.0, schema16. Old Game Data187256159 and Car
Battery187256160 are useful items, one each, replacing two Stardust. Existing
Music Lab 5/20-point checks remain; normal character rewards remain vanilla.
Exact inventory flags are suppressed only at Music Lab native true-grant requests
under the new contract. False consumption and collection flags are preserved.

Receipt ownership comes from complete authenticated ReceivedItems packets, with
index-zero replacement and index-gap waiting. Replacement session publication
suspends previous grant readiness before authentication, including failed or
incompatible replacements. Deliberate shutdown clears ownership. Grants execute
on the Unity thread at most once per second, after the existing selected-save
pointer/processor/gameplay readiness gate; receipt revision is held across the
native operation. Already-held items are idempotent; unknown native reads defer.

Old Game Data terminal predicate is the metadata-confirmed public static
CurrentPlayerSaveEnquiries.IsCharacterUnlocked(ePlayableCharacter.GUITAR_MANIAC),
enum value7, backed by GameProgressionSaveData.characterUnlockedValues. It is not
the memory-card COLLECTED flag. Car Battery uses PLAYABLE_CHARACTER_UNLOCKED_MEOO.
A bounded native-save-epoch consumption observer also blocks refills during the
interval between consumption and a later character reward. It does not carry
consumption into a new native save epoch. Normal native persistence/reload of both
terminal predicates still requires live acceptance.

Independent review found a pending/rejected-login ownership leak; an extracted
production-connection test reproduced it, then passed after suspension on session
publication. A production Unity test reproduced refill during a two-second delayed
unlock, then passed with the save-bound consumption observer. Review recheck found
no outstanding concrete regression. Tests cover exact/malformed/legacy contracts,
replay/empty history/gaps, replaced identity, readiness, unknown state, consumed
history, save epoch replacement, and unchanged old contract behavior.

Candidate DLL SHA256 E70235EEFAD99D446106A4FF98503F73C54B31FBFD9B40D86369AF1278992875.
Candidate APWorld SHA256 D5E2FC70CEA5BE38754580BF4E6414EDB92A0CE8170EA52355C6BC6E5A02C5F0.
Both are packaged as local outputs; neither is installed. No retained seed, server
or native save was changed. New seed required for these item-pool changes.

Next live sequence after explicit candidate install approval: prepare separate
v0.25 seed, back up native saves before any fresh-slot reset, test source chest
checks without vanilla inventory, receive both items, verify normal hand-ins,
then reload/reconnect and check consumed items are not restored. Preserve the
previous retained seed/save for earlier evidence.

Final automated verification: all 22 client test projects pass; all 113 APWorld tests pass; repository validator passes. Release build succeeds with zero errors and the same four existing nullable warnings plus unavailable vulnerability-feed warning. Additional schema16 campaign regression passes. Gameplay acceptance remains pending.

## Approved installation and isolated test seed — 2026-09-16

User approved client E70235EE and APWorld D5E2FC70. Both installed hashes match
full candidate hashes above. Game was already closed. Previous installed artifacts
and all Steam native-save files were copied to local rollback directory
work/pre-v071-install-20260916-135758. Retained servers and old seed were not changed.

Generated separate seed 14964085740301527950 with generator seed9162501, normal
performance, Roots start, 20 starting Music Lab Points and Lobby Access. Verified
spoiler placements: 5/20-point chest each Stardust; Level1 Completion Old Game Data;
Level1 1Star Car Battery. Generation completed successfully, archive integrity and
placements inspected. Local archive work/character-items-seed/generated/AP_14964085740301527950.zip.

New seed is prepared but not hosted/connected. Existing slot4 has already completed
both hand-ins. Obtain agreement before resetting that slot, and prevent old-save
chest reconciliation/old seed item replay from contaminating the new test. Establish
fresh native slot while disconnected, then connect to the isolated new server.

Live v0.25 seed14964085740301527950: created native slot4 epoch2. User reports
neither vanilla item received from chests. Both client and server confirm 5-point
and 20-point chest checks each sent Stardust. User also opened10-point chest,
which sent Hippo and Frog Cassette. No Old Game Data/Car Battery AP receipts yet.
Source chest/check smoke test passed on user-observed absent vanilla items plus
verified AP rewards. Next: Level1 Completion + 1Star awards the two quest items.

Level 1 name correction (2026-09-16): user confirms the first campaign level is
Light Humor. Corrected the two campaign display catalogs, their existing tests,
and campaign mapping documentation. Internal Level_05 and numbered AP check IDs
are unchanged. Retained live evidence from 2026-09-12 records Level_05 completion
followed by Level 1 Completion (187256001) and 1 Star (187256211); its cassette
source is Gold, not The Little Things. The Little Things remains a separate valid
cassette/song entry. Current seed placements still target the same two checks;
no seed regeneration or installed-client replacement is needed for this test.
Live Old Game Data hand-in, 2026-09-16, client v0.72.0 / seed14964085740301527950:
user screenshot explicitly shows Maniac unlock. Runtime log records
LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM True -> False in GameRoom_27, followed by
Old Game Data result=consumed, consumed=True. No subsequent regrant appears in
the captured log. This confirms this session's randomized receipt -> native hand-in
-> character unlock path; cross-relaunch terminal persistence remains untested.
Evidence: work/v072-old-game-data-handin.log and user screenshot in conversation.
Next: Car Battery hand-in to Ghost Cat in the Lobby for Meoo.

Live Car Battery hand-in, 2026-09-16, client v0.72.0 / seed14964085740301527950:
user screenshot confirms Meoo unlock. In GameRoom_Hub1A the log records
CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM True -> False and
PLAYABLE_CHARACTER_UNLOCKED_MEOO False -> True, followed by Car Battery
result=consumed held=False consumed=True. Old Game Data remains consumed with
held=False after leaving the Garage. Both randomized-receipt-to-hand-in paths
passed this session. Evidence: work/v072-car-battery-handin.log.
Next acceptance step: finish the unlock screen, return to title normally, reload
existing slot4 while connected, and verify both unlocks persist and neither item
is restored. Process-restart/reconnect persistence and remote-player notifications
remain separate pending checks.

Reload follow-up: user reports Meoo remained available and was selectable.
This confirms user-observed character availability. The current log contains no
new post-reload CHARACTER QUEST ITEM snapshot beyond the earlier consumed states;
do not count this alone as independent post-reload verification for both bag items
or Maniac. Next user check: Maniac still selectable, both handed-in items absent.

Save-reload acceptance confirmed by user (2026-09-16): after returning to title
and reloading slot4, Meoo and Maniac both remain selectable, and Old Game Data
and Car Battery are absent from inventory. User explicitly confirmed all expected
results. Both character-item flows now pass source suppression, AP receipt,
native hand-in, character unlock, and same-process save-reload persistence.
A full process restart/reconnect remains distinct from this confirmed title reload.
Notification history visibility and solo self-reward popups have been exercised;
true cross-player sent/received notifications remain untested in live gameplay.

Full process-restart acceptance passed (2026-09-16). Following instructions to
fully exit/reopen and load slot4, the user confirmed everything works. Fresh v0.72.0
startup log independently confirms connection to retained seed server38282/Jack
and both Old Game Data and Car Battery at consumed=True, held=False on new native
save pointer236CD8046E0. Evidence: work/v072-post-handin-full-restart.log.
Both randomized character-item flows are accepted for this tested seed/save,
including native hand-ins, title reload, full restart and reconnect persistence.
Remaining notification acceptance is true cross-player sending/receiving; this
solo run does not establish that live multiplayer case.
