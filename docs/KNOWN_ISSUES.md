# Known issues — v0.28.1 / Client v0.75.13

Victory is gameplay verified: Hard difficulty, 40 AP Star goal, fresh-save run,
server CLIENT_GOAL (30). The previous general statement that Victory is unverified
is superseded. See the [release notes](releases/v0.28.1.md) for the current summary.

Current open issues: intermittent player/NPC movement faults; windowed/song and
Combo Bucket hitches; exclusive-fullscreen retry warnings at startup; delayed Zen
cassette delivery requiring restart in one report; already-owned Hypno Pan crafting.
Broader native cassette-source, seed/difficulty and multiplayer acceptance remains
incomplete. Tower entrance routing is regression/build-tested only, by user choice.
Existing leaked cassette ownership is not automatically removed.

## Historical investigation records

The entries below retain their original dates and candidate statuses. Use the
current release notes and later acceptance entries for present status.

# Known issues — v0.28.0 / Client v0.75.4

Current release status; older testing checklists are historical evidence.

## Known bugs

- **Temporary severe lag around Combo Bucket conversion / Lift Quest:** reported during the broad playtest, recovering partway through the level. The cause and a complete fix are not established.
- **Hypno Pan crafting when already owned:** pre-granting Hypno Pan can make Frog and Hippo's crafting interaction unavailable. Ability use was confirmed working. A separate crafting check has not been added because its independent completion marker and safe reward replacement remain unresolved.

## Follow-ups deferred until after the full playthrough

Recorded 2026-09-18 during the Hard / 40 AP Star run on Client v0.75.2. The Quicksand investigation was resumed in the September 20 full-mod audit. The player subsequently paused the playthrough to prioritize lag throughout the game; v0.75.4 performance changes are installed and substantially improved live play; residual hitches remain under investigation.

- **Quicksand cassette appears to leak its vanilla reward:** the 32-point chest correctly sent its randomized Gradius Remix Cartridge, but Quicksand was already marked `HAVE_DEPOSITED` when its actual AP item arrived later from Fumblin Around - Bronze. Logs support the reported early native grant; the September 20 native trace identified inlined request dispatch bypassing the managed hook. Review the shared reward path for all five Music Lab cassette chests, preserving legitimate AP receipt and insertion behavior. Keep the current seed/save.
- **General gameplay lag, including Music Lab songs (active high-priority scoring issue):** the player reports lag everywhere, with Music Lab and 10-4 Good Buddy as specific examples, and warns that it makes good scores impractical at higher difficulties. Client v0.75.4 removes the measured repeated cassette scan cost (roughly 9.7 ms to 0.007 ms per frame), and live testing is substantially smoother. Occasional hitches still cost notes; their remaining cause is not established. Profile multiple Music Lab songs, investigate shared gameplay overhead, and verify that timing is stable enough for higher-difficulty medal requirements. Do not assume it shares the earlier Combo Bucket lag cause.

## Testing limits and remaining work

- The new AP Star HUD, native entry gates, new Star Eater thresholds, Cell Tower feed check, King unlock check and post-threshold victory still need final in-game acceptance together on a fresh seed/save. Generation success does not prove every native prerequisite or UI boundary.
- 33 of the previous 37 supplemental checks were confirmed in the broad gameplay pass. Demonic Tower, Demonic Escape, Demonic Lockers and Demolition Certificate are implemented but were not manually played in that pass. Royal/Bunker feeding was tested with temporary 25-star thresholds, not the original 40/66 thresholds.
- Failed special-level attempts, full save/seed-isolation gameplay, true cross-player notification coverage and the complete native campaign/cassette route matrix remain incompletely verified. Automated tests cover relevant policy and isolation cases.
- Local co-op and online co-op are not verified release features.
- Remaining source investigations: separate Hypno Pan crafting; Bizzle/Clive switches; independent Super Nectar component awards; Demon Key acquisition/use; final demon interaction versus its existing cartridge source. Bean Trumpet is still native and shares the Letters pickup; Notepad/Data Stick share the Gecko interaction; Bizzle/Clive acquisition shares Meet the Bees. These are not additional independent checks in this release.

## Fixed in the September 18 hotfix

The Roots-entry/Star Eater interaction crash was traced to unsafe native reference assignment. Client v0.75.4 includes the direct object-reference setter and verified rollback repair, along with the tested metadata-cache performance fixes. Continue reporting any new crash with the client version and log.

## September 20 audit candidate (not yet accepted in game)

Client v0.75.6 intercepts the five audited cassette chest reward reactors, fixes missing cassette source checks after early AP receipt, suspends invalid AP Star history, defers uncertain first-save binding, and improves notification history/replay. Automatic save/member diagnostics and unrelated character-event allocations are reduced. [Full audit](testing/2026-09-20-full-mod-audit.md).

Quicksand's bypass is fixed in the candidate code and regression tests; live chest/native insertion acceptance remains pending. Existing leaked cassette ownership is unchanged. Remaining song hitches, Combo Bucket lag, Hypno Pan crafting and unverified gameplay routes are still open, rather than being declared resolved by code review alone.

## Fullscreen comparison — September 20, Client v0.75.6

The player repeated the affected Fumblin Around section in fullscreen and reported no issues. Fullscreen is a confirmed workaround for this reproduction, not proof that all performance issues are resolved. The preceding windowed trace showed continued game frame submission during a roughly4.3-second pause in displayed-frame statistics, without a corresponding long CPU scheduling pause. The comparison supports investigating the windowed presentation/compositor path; the exact driver, overlay or compositor cause remains unproven.

Keep fullscreen for the current playthrough. Windowed-mode stutter remains open. Combo Bucket conversion and other songs still require separate acceptance. Temporary performance diagnostics are disabled in configuration for the next launch; the running game is unchanged. No driver/registry changes, save repair, restart or release publication was performed.

## Pending cassette receipt — September 20, Client v0.75.7 candidate

Zen from Wiggle Bronze was received by AP but remained staged in the default native save bundle. Cassette Lord could be interacted with but would not deposit it: the native deposit bundle rejects a song already changed by another pending bundle. Restarting recovered the player's cassette and the fresh session confirmed persistence.

The candidate removes an incorrect persistence gate that treated `RequiresWriteToDisk` as an in-flight lock. Native code uses it as a pending urgent-write flag and permits ordinary bundle persistence while it is set. The existing selected-slot, pointer, ownership, readiness and receipt checks remain. A regression covers pending-write admission and selected-save mismatch rejection. Build and automated validation do not replace live acceptance of receiving and depositing a cassette without restarting. This candidate is not installed or published yet.

## Movement interruptions across areas and NPCs — September 20, Clients v0.75.6–v0.75.7

The player reports repeated movement stops resembling collisions after completing a Music Lab song. Animations remain smooth; leaving Music Lab and returning restores normal movement. The captured session returned from Zen (silver, 564322) with no associated exception. Cause is unconfirmed and tracked separately from windowed presentation stutter. Existing logs do not capture movement/collision state. On recurrence, compare whether stops occur at fixed floor locations and whether keyboard and controller both reproduce before modifying movement or colliders. Workaround: leave and re-enter Music Lab.

## Royal Corridor Star Eater traversal — Client v0.75.7 candidate

Royal Corridor Access now opens the exact native BridgeAcrossGap route and disables only StarEater/BlockingCollision, following the Roots area-access traversal rule. The native feeding requirement, interaction, fed flag, and AP check remain unchanged. Asset inspection confirmed the native RemoveHoleBlocker target and alternate bridge route. Runtime uses exact child bindings including the inactive bridge and throttled retries; no global scene scans or save mutations. Live acceptance remains pending: cross before feeding, leave/re-enter, then feed at the generated requirement and verify the check. Existing seed placements are unchanged.

Royal Corridor traversal live acceptance: v0.75.7 installed September 20; player confirmed crossing works with the current below-threshold star count. Feeding at the generated threshold and receipt of its check remain separate acceptance steps. The cassette persistence fix is installed in the same build; a new receipt/deposit without restart still needs confirmation.

Expanded movement report on v0.75.7: the player confirms this is not limited to Music Lab and also affects NPC movement, especially in Meat Dimension. The earlier Music Lab report had smooth animations and recovered after leaving/re-entering; those properties are not yet confirmed for Meat Dimension. Treat this as a shared movement investigation, not a proven Music Lab return bug or a confirmed physics defect. The captured Meat Dimension session logs escort recovery as skipped, with no associated exception found. No movement fix has been applied. NPC identity, fixed-location versus open-space interruption, and recovery after room re-entry remain requested reproduction details.

Movement reproduction clarified: primarily Act 3: Montage (Level_22 / GameRoom_22), not just the Meat Dimension hub. Meat dogs and the cat move slowly while facing the wrong direction; Hypno Minims drift off the stage as though floating. Player observes irregular movement while overall rendering remains smooth, explicitly unlike frame drops. The hub-only mouse-escort recovery investigation does not cover this level. Current log confirms entry to GameRoom_22 and Hypno Pan ownership verified at login, but records no NPC steering, collision, facing or simulation-step data. Root cause and relation to the earlier Music Lab movement report remain unconfirmed. Prioritize controlled Act 3 reproduction and shared movement/native-hook investigation; do not mark this resolved by fullscreen or the Royal Corridor fix.

Hypno Pan investigation: native leadership feeds DirectableCharacterMotor.UpdateMovement, then FollowingAIControlCharacterMotor target-position and floor-following calculations. The reviewed hooks do not directly patch these functions, and AP ability reconciliation only sets PIED_PIPER_ABILITY. No repeated grants were observed in the captured session. This does not rule out indirect mod effects. A separate address-sharing risk was found in the Roots-only DifficultyTogglerState.get_CurrentPhase override (shared with 24 other getters); investigate an exact instance guard separately, without claiming it explains Meat Dimension.

Client v0.75.8 diagnostic candidate adds an F8-triggered 12-second movement capture under the existing developer harness, available in any gameplay room. It scans active rigidbodies once per explicit capture, prioritizes player-character bodies and records at most 12 bodies and 120 samples at 0.1-second intervals, stops on room change, and records transform/body position, velocity, facing, body settings, frame and physics counts, time settings, focus, maximum frame delta between samples, and native accepted/Rewired player movement inputs when readable. Files are local under BepInEx/diagnostics; no gameplay/save/physics state is written. Captured bodies are capped and may not include every NPC. This is instrumentation, not a movement fix; live reproduction and comparison remain required. Build passed with only the existing NU1900 package-audit connectivity warning. Not installed yet.

September 20 live diagnostic: v0.75.8 captured the reported faulty player movement in Music Lab for 12.375 seconds (120 samples). Frame count advanced 1552 and FixedUpdate advanced 620 times; maximum recorded frame delta was 34.15 ms. Rigidbody settings remained dynamic, gravity/collision enabled, rotation frozen. This supports the player's distinction from a sustained frame freeze, but does not establish why movement feels wrong. Player input was unavailable because the diagnostic consulted network-player records. v0.75.9 candidate switches to PlayerEnquiries.GetAllLocalPlayers and records input vectors explicitly. The body-root forward vector is not proof of visual character facing. Input/native runtime acceptance remains pending; movement is not fixed.

Launch lag on v0.75.8: Unity Player.log recorded 187 SetFullscreenState failures (887a0022), repeated fallback from ExclusiveFullscreen to FullscreenWindow, and refresh-rate timing drift. The mod log contained no exceptions/errors and 121 warnings, predominantly existing status diagnostics. Treat the exclusive-fullscreen retry loop as a concrete startup finding and a likely contributor, not proof of the Hypno Pan movement cause. Movement capture had not run during startup. No display settings or registry values were changed.

v0.75.9 live movement capture succeeded, including native single-player input. Player reports faulty movement only at the beginning, then normal movement in the same capture. Music Lab capture duration 12.261 seconds: 1900 rendered frames (~155 FPS), 614 FixedUpdate ticks (~50 Hz), maximum sampled frame delta 19.03 ms. Accepted and Rewired movement input match at all 120 samples. During approximately 0.8–2.8 seconds, nonzero/full-strength input coincides with repeated zero or low horizontal rigidbody velocity; normal speed approaches 5.97 units/s later. A sustained near-right input from ~2.0 seconds continues across the recovery around ~2.9–3.1 seconds. Rigidbody kinematic/gravity/rotation-constraint/collision settings remain unchanged. This localizes the observed interruption downstream of the recorded input, without identifying whether motor restrictions, collision contacts, or another actor is responsible. Current capture lacks desired motor velocity and contact identities; root-body forward is not visual facing. Do not claim a controller failure, frame freeze, or confirmed collision cause. Next investigation boundary is desired versus actual motor velocity and contemporaneous contact identities, using the early faulty section and later normal section as a paired comparison.

Movement research follow-up: native player movement applies a list of PlayerMotorRestrictor objects to intended/actual movement; player and follower motors ultimately share Motor.FixedUpdate. That shared motor can cancel motion when the room is not running, apply queued desired velocity/position, or preserve the current state. No reviewed direct mod hook targets those motor methods. None of those branches or collision contacts were recorded by v0.75.9, so the cause cannot yet be assigned. Published developer notes and targeted issue searches did not establish a matching documented fix.

Uninstalled v0.75.10 candidate extends the same bounded F8 capture with reflected native motor fields (desired velocity/position, intended movement, queued physics action, movement permission, restriction count), room-running state, and recent collision-object identities/normals/separation. Temporary collision observers are attached only during an explicit capture and removed when it ends; they do not alter physics. Native field availability and contact callback counts are reported explicitly. Build succeeds; live diagnostic acceptance remains pending. This is not a movement repair.

## September 21: first unassisted Hard/40 King clear did not report Victory

Client 0.75.10 logs show 40 received Star items before GameRoom_28A and before the Level_28 result. The saved default-variant result scored 111 and one Star; completion, one-Star, character-unlock, keycard and cassette-source checks reached the retained server. Server save client_game_state remained 5, with no local ap-star-goals journal. This is not accepted as AP Victory.

The exact rejected stage is not recoverable from existing logs. Native ProcessRequest(PersistLevelResultRequest) applies the score before emitting LevelResultWasPersistedEvent; required hooks installed. Reflection supports nullable identifiers. The native success request is a two-Boolean value type, whereas runtime tests use anonymous managed objects, so those tests do not validate the live ABI/hook sequence. A separate confirmed code weakness consumes the admitted candidate before attempting the final stable-save callback, dropping it if readiness is temporarily unavailable; causality for this run remains unproven.

Candidate 0.75.11 adds event-only GOAL TRACE messages for admission, success flags, result binding, persisted identifiers, commit/save readiness, and journal write. No qualification rule, server state, save, or installed plugin was changed. Release build succeeded and 27 AP Star runtime checks passed. Deployment and live acceptance pending. Evidence archived in workspace work/audit20260920/victory-missing-client.log and victory-missing.apsave.

## September 21 — v0.75.11 victory retention repair

Matched, qualified persisted result evidence is now retained across temporary stable-save unavailability and retried on the existing one-second goal tick. New level admission clears only the in-flight attempt; explicit native/session boundaries invalidate deferred evidence. A verified different save rejects it. Star qualification remains frozen at result capture; subsequent Star receipt cannot upgrade an early clear. Journal and official StatusUpdate/ClientGoal delivery remain unchanged. Event-only goal tracing is included for live acceptance.

The new recovery regression failed before the repair and passed afterward. 34 AP Star runtime checks, including changed-save rejection, explicit boundary rejection, duplicate persistence, later Stars, and starting another level while pending, passed. Connection/goal-packet tests and Release build passed. Build warning NU1900 reflects unavailable package vulnerability service. Previous discarded clear cannot be recovered automatically. Live King clear remains pending; do not claim the user's original failure cause is proven.


## September 21: v0.75.12 nullable victory event repair (installed; gameplay pending)

The v0.75.11 live replay captured a successful King Ferdinand I result with 41/40
AP Stars, matching admitted/applied Level_28 and LevelVariant_Default. The native
LevelResultWasPersistedEvent exposed an empty nullable LevelVariant, so strict
variant equality rejected the qualified completion before journal/goal delivery.
v0.75.12 accepts an omitted event variant only with a matching level and an already
bound applied-score candidate. Explicit variant conflicts, unbound results, changed
saves, early clears and duplicate delivery remain rejected. Runtime fixtures now
use production ReflectionUtil and cover nullable native-shaped event variants.
No goal is restored from logs or manually granted; native gameplay acceptance is
still required. Existing completion-retention and official StatusUpdate(30) remain.

## September 21: first confirmed unassisted AP Victory

Client v0.75.12 passed live Victory acceptance on seed 23131899417402815337,
player Jack, native slot 4, Hard difficulty, 40 AP Star goal. The final clear
started and persisted with 41 AP Stars. Trace verified successful saved result,
matching Level_28/default applied result, empty optional event variant, same-save
commit, durable goal journal and AP STAR VICTORY send. The server save recorded
client_game_state[(0, 1)] = 30 (CLIENT_GOAL), and the server announced that the team
completed all games. No manual goal, injected victory journal or server assistance
was used to award completion. Automatic release of remaining items followed goal.
The victory regression is gameplay accepted; unrelated movement/performance issues
remain open. No additional boss replay is required. Release publication is pending.
Local evidence retained outside the repository: victory-07512-success.log and
victory-07512-success.apsave in the task workspace work/audit20260920 directory.

## September 21: Tower of Fear exterior softlock (v0.75.13 candidate)

GameRoom_Hub5C is now part of Tower of Fear access alongside GameRoom_Hub3.
Previously Royal Corridor could enter this unmapped exterior without Tower Access,
leaving no usable return door or reachable phone. The existing destination guard
now redirects unowned exterior arrivals (including restore transitions) to Music
Lab. With Tower Access, native travel is preserved. Vanilla mode is unchanged.
No door/story flags, items, checks or APWorld rules are altered. This follows the
user's requested option to classify the entrance room as Tower of Fear.
Regression reproduced the old unguarded destination; native route acceptance is
pending. Installed v0.75.13 with approval. Live route test was explicitly deferred for release.
