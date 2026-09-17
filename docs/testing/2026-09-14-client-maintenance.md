# Client performance, reconnect and obsolete-code cleanup

Date: 2026-09-14. Local candidate on `docs/star-victory-quest-capacity`.
Client v0.70.0 / APWorld v0.24.0 contracts and permanent IDs remain unchanged.
This is not a release or gameplay sign-off. See E51-E54 in the
[Star-victory acceptance ledger](2026-09-11-star-victory-and-quest-capacity-acceptance.md).

## Observed defects

- Restoring the retained seed into a recreated slot 4 produced sustained low frame
  rate and stutters. Restart helped hub movement, but level play remained laggy.
- A verified cassette persistence batch was deferred because the retained
  save-data processor was absent after explicit save selection/creation. The
  deferred batch stayed queued and native ownership scans ran again every frame;
  only the log message was deduplicated.
- A cold offline game launch repeatedly timed out after the local server became
  available. An independent WebSocket handshake returned the correct seed, but
  the game did not reconnect until another game restart with the server online.
- Cold offline startup also reached the tutorial before authenticated AP routing
  was available. No unsafe local substitute for authenticated Area Access is added.

## Candidate changes

- Exact selection, creation and build callbacks retain their own
  `SaveDataRequestProcessor` instance for the existing pointer-bound ownership
  proof. Malformed owners clear the reference; the callback does not read or
  mutate native save state. All existing selected-pointer, registered-owner,
  identity, cassette-status and write-state requirements remain mandatory.
- Missing owner or closed gameplay readiness returns before expensive ownership
  reads. Deferred batches retry at most once per second; a new batch or save
  identity gets an immediate attempt. No queued batch is discarded by this limit.
- Scheme-less loopback endpoints explicitly use `ws://`, avoiding the installed
  library's WSS-first probe. Explicit schemes and remote endpoints are preserved.
  Prelogin failures now report one compact underlying cause per attempt;
  asynchronous cleanup faults are observed without changing retry/identity rules.
- Superseded per-level Access blockers, item-to-native-flag maps, grant/reset
  hotkeys and their exclusive helpers are removed. These paths were already
  disabled under current Area Access. Historical IDs remain reserved.
  Review identified one current behavior embedded before a legacy guard: the
  post-Level-1 Roots difficulty presentation suppression. A focused replacement
  preserves its exact sequence/compatibility guards, persisted-result tracking,
  new-save reset and completion marking, with behavioral and registration tests.
- Obsolete automatic cassette-start polling, uncalled runtime diagnostics,
  solved Lift Quest discovery and generic per-event property dumps are removed.
  Current source-check callbacks, bounded QuestActions snapshots/events, native
  identity/special-mode scans and Music Lab/Garage tracking remain.
- Unfinished Lobby item staging is preserved. AP Stars, Star gates, Victory and
  special-variant production checks remain inactive.

## Verification and remaining acceptance

The initial cassette regression failed on unbounded deferred native reads before
the fix. The repaired production scheduler is exercised for 1,000 render ticks,
new waves/identities, negative time, long pauses and invalid identities. Existing
save pointer isolation, persistence and actual connection-class tests are retained.

Isolated transport tests with installed Archipelago.MultiClient.Net 6.7.1 and the
game's exact .NET 6.0.7 runtime recovered after initial offline attempts using both
bare and explicit WS endpoints. The final WS fixture reached four successive
post-offline protocol exchanges; it intentionally refused the slot after Connect.
This proves transport recovery in that fixture, not authenticated in-game recovery.
The original persistent reconnect failure is not reproduced or proven fixed yet.

Final automated verification:

- All 21 retained client test projects passed. Cleanup exposed stale source-region
  markers in Garage, SpecialVariant and Reconnect tests; these now follow retained
  methods and observed disconnect cleanup. The final Reconnect marker correction
  was verified with its targeted suite after the full run.
- APWorld: 109 tests passed. Repository contract validation and whitespace checks
  passed; permanent IDs and inactive Star/Victory contracts remain unchanged.
- Release build against the installed game: zero errors, four pre-existing
  nullable warnings and one unavailable NuGet vulnerability-feed warning.
- Independent review found no outstanding concrete regression after the focused
  Roots presentation restoration. This is not gameplay acceptance.
- Candidate DLL SHA-256:
  `B44E7EA937347EF4ED43C07EF2ADC332256F67C9F2B78DC2AF5044EAFAB20F2B`.
  Candidate is built but has not been installed. No saves or retained server state
  were changed by this maintenance pass.

Before acceptance: install the reviewed candidate only with explicit approval;
check ordinary movement and one short level segment on the retained save, then
cold-offline startup followed by bringing the same server online. Verify automatic
reconnect, source/check preservation, and cassette held/deposited persistence.
Do not require a fresh save or repeat quest actions solely for this smoke test.
If reconnect still fails, use the newly exposed underlying error before further fixes.

## Installed live check — 2026-09-15

User approved installation and launch. Installed DLL hash matches the candidate
above; the former DLL was retained outside the game plugin directory for rollback.
Game launched through Steam with native slot 4 activated. The SCRC server was not
running (an unrelated server was on a different port and was left untouched).
The client logged connection-refused errors while retrying. Starting the retained
AP_21483200610759512211 zip/apsave on 127.0.0.1:38281 allowed generation 11 to
connect automatically in the same game process. Both server join and client
`NET reconnect succeeded` confirmed recovery; the server loaded 51 received items,
client history synchronized, and Garage insertion sync became ready.
This cold-offline-start recovery check passed once. Movement, level performance
and post-load cassette persistence remain awaiting user gameplay observations.

User gameplay feedback after the installed candidate: asked whether walking and a
short level segment were still laggy, the user replied, "No it feels better."
The reported performance smoke test is therefore improved/passed for this run.
Together with the observed automatic cold-start reconnect, both reported issues
passed this live smoke test. This does not establish fresh-save stress coverage or
post-load cassette held/deposited persistence; those remain separate checks.

Post-update Roots persistence observation: user reports Star Eater absent.
Current log confirms native slot 4, GameRoom_Hub2 F5 snapshot with
ROOTS_HUB_STAR_EATER_FED=True, and StarEaterNPC activeSelf=False /
activeInHierarchy=False. The completed feed marker survived the update;
absence alone is not evidence of lost progress. Star total was not reconfirmed.
