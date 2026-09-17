# Item notification candidate

User requested sent/received item visibility without the external AP client, and
selected popups plus recent history. Client v0.72.0 adds a bounded managed feed,
a real MultiClient.Net packet/message adapter, and an IL2CPP IMGUI overlay.

- Six-second popups, at most three visible; F6 history holds the latest 100 entries.
- Incoming data uses ReceivedItems history indexes. First synchronization populates
  history silently; subsequent unseen receipts notify, including same-seed reconnect.
- Outgoing data uses actual ItemSendLogMessage only; hints and unrelated sends are
  ignored. Self sends are shown once by the incoming receipt path as Found.
- Existing lifecycle leases reject stale callbacks; authenticated seed/team/slot
  changes clear feed state. Shutdown resets it. UI uses unscaled time.
- Sent-message history is session-local; received history is imported on reconnect.
- No reward dialog is invoked, no native save is changed, and no additional AP
  checks are sent by this feature. Optional presentation errors do not fail login.
- The old Plant Pipes source name is selected for old slot data, preserving current
  seed compatibility while newer APWorld data selects Plant Pipes Pickup.

Initial pure feed tests failed before implementation. Tests cover history import,
new receipt, reconnect/offline arrivals, duplicate suppression, stale generation,
self rewards, bursts, expiry, and identity changes. Adapter tests use real packet
classes. Runtime visuals and true remote-player delivery remain pending acceptance.

Independent review found overlapping history footer/last entry; row capacity was
reduced to reserve the footer. No runtime deployment or visual acceptance yet.

Verification completed 2026-09-16: all 23 client test projects passed. The 115-test
APWorld run had only two documentation-version failures; both were corrected and
the full 15-test repository-contract module passed on rerun. Repository validation,
diff whitespace checks, and the final Release build passed (zero build errors;
four existing nullable warnings plus unavailable NuGet vulnerability-audit warnings).
Real-library adapter tests cover confirmed sends, hint/self/unrelated-message
filtering, buffered login, duplicate sends, and received-packet replay.

Packaged client candidate: outputs/item-notifications-v0.72.0/RhythmCastleAP.dll
SHA-256: FF1CF97A0033BC73162910D87DEDCB489DAD7526F50B60E3314B3BE6DD0B9EB2
No installation, native save changes, or in-game visual acceptance performed.
Next: explicit candidate installation approval, then F6 history and live receipt/send
smoke testing on the retained seed. No seed regeneration is needed for this client.

Approved client installed 2026-09-16. Installed SHA-256 matches
FF1CF97A0033BC73162910D87DEDCB489DAD7526F50B60E3314B3BE6DD0B9EB2.
Prior DLL/config and AP server save backed up in work/pre-v072-install-20260916-151258.
Retained server PID23584/port38282 was already listening and left unchanged.
Steam launch produced visible Rhythm Castle window PID35372. Startup log confirms
v0.72.0, connection to Jack on 127.0.0.1:38282, and activation of existing slot4.
Startup log captured in work/notification-v072-startup.log. User F6 history and
new-item popup acceptance remains pending. No seed or native save reset.

Live test follow-up: user confirmed F6 history is visible/readable and replied
"Good" after the first-clear popup test instruction. The retained runtime log
confirms Level1 Completion 187256001 delivered Old Game Data 187256159 and
Level1 1Star 187256211 delivered Car Battery 187256160. Both native bag states
were subsequently observed held=True on slot4. Evidence captured locally as
work/notification-v072-first-receipts.log. F6 visual acceptance is explicit;
remote-player outgoing/incoming popups remain untested in this solo seed.
Next: normal Old Game Data hand-in in Game Garage, then Car Battery in Lobby,
verifying character unlock and no replacement of consumed quest items.

Full process-restart acceptance passed (2026-09-16). Following instructions to
fully exit/reopen and load slot4, the user confirmed everything works. Fresh v0.72.0
startup log independently confirms connection to retained seed server38282/Jack
and both Old Game Data and Car Battery at consumed=True, held=False on new native
save pointer236CD8046E0. Evidence: work/v072-post-handin-full-restart.log.
Both randomized character-item flows are accepted for this tested seed/save,
including native hand-ins, title reload, full restart and reconnect persistence.
Remaining notification acceptance is true cross-player sending/receiving; this
solo run does not establish that live multiplayer case.

Packaging follow-up (2026-09-16): user deferred live cross-player notification
acceptance and authorized packaging. Created local development bundle
SCRC-client-0.72.0-world-0.25.0-naming-update.zip in the task outputs directory,
including the three-DLL client ZIP, revised scrc.apworld, full location lists,
installation/test notes and SHA256 manifest. No deployment or seed/save changes.
Client DLL remains the exact gameplay-tested FF1CF97A0033BC73162910D87DEDCB489DAD7526F50B60E3314B3BE6DD0B9EB2.
Revised APWorld SHA256: 6D873038F8D83A29260F97612BAD7EA74BA909311011B200F8B0679EB8AD26C2.
APWorld metadata remains 0.25.0; package is explicitly labeled a naming revision.
Fresh full APWorld suite: 115 tests passed; repository validator passed.
Archive integrity and complete APWorld source-byte parity passed. Client archive
contains only RhythmCastleAP.dll, Archipelago.MultiClient.Net.dll and Newtonsoft.Json.dll.
Live cross-player popups remain deferred, not gameplay-accepted. Existing seed can
continue; corrected generated names take effect when a future seed uses this world.

## Cassette prerequisite repair and retained-seed recovery (2026-09-16)

Retained seed AP_14964085740301527950 placed Plant Pipes at Cassette Source - Badass,
which is the Level 5 / Lift Quest completion cassette reward. The older cassette
catalog required Roots Access, Hip Glasses and Chicken Bucket but omitted Weed
Killer and Plant Pipes, although the campaign level already required both.
Normal/default cassette triggers now merge campaign area/item requirements with
existing route-specific requirements. Alternate routes remain OR alternatives;
Bee/Devil routes are not assigned normal-campaign gates. Five sources gain missing
gates: Badass, Heavy Metal, Gotta Get Up, Party Non Stop, and Keep On Hustlin.
The partial Level 3 Plant Pipes pickup remains reachable without owning Plant Pipes.

Two new regression tests reproduced 33 failing subcases before the repair, including
all four difficulty settings. Full APWorld suite after repair: 117 passed. Repository
validator passed. Independent code review found no concrete issues. No client code,
IDs, schema or installed files changed; a new-seed gameplay replay remains pending.

The user approved a one-item recovery grant to Jack. Backed up the retained server
save to AP_14964085740301527950.pre-plant-pipes-recovery.apsave, verified PID23584
owned port38282 and the correct seed, then sent /send Jack Plant Pipes once.
Server confirmed delivery; client logged item187256117 received from Server and
retry result=verified-owned, WEED_KILLER_ABILITY=True in GameRoom_01.
Evidence retained locally: work/plant-pipes-recovery.log. Original source check and
existing native save remain intact; the later duplicate ability is idempotent.

Replacement local APWorld is outputs/scrc-world-0.25.0-cassette-gates-fix/scrc.apworld
in the task workspace. It supersedes the naming-update APWorld for future generation;
metadata stays 0.25.0 and the adjacent SHA256 manifest distinguishes the revision.
Next gameplay checkpoint: complete Level 3 using the recovered Plant Pipes.
