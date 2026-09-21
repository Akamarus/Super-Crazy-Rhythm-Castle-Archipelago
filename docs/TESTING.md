# Current release: v0.28.1 / Client v0.75.13

Victory is gameplay verified on a fresh-save Hard / 40 AP Star run, without server assistance. Tower entrance access mapping is regression-tested; live acceptance was deferred. APWorld remains v0.28.0 / schema 19. See [current release notes](releases/v0.28.1.md). Earlier candidate statuses below are historical.

> Current published scope: **v0.28.0 / Client v0.75.0 — First Completable Release**. See the [release notes](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/blob/v0.28.0/docs/releases/v0.28.0.md) and [known issues](https://github.com/Akamarus/Super-Crazy-Rhythm-Castle-Archipelago/blob/v0.28.0/docs/KNOWN_ISSUES.md). Native AP Star/victory acceptance remains pending; older candidate and deployment statements below record historical checkpoints.

# AP Stars combined candidate — native playthrough acceptance pending

**Client v0.75.0 / APWorld v0.28.0** uses schema 19 / campaign-mapping schema 1.
Use a fresh v0.28 seed and fresh native save for candidate testing. This implementation
has not been deployed; installed client v0.74.2 and the running test seed remain unchanged.
The published v0.26.0-dev release is a separate historical baseline.

Active totals: Normal 164 / Hard 222 / Expert 280 / Perfection 316.
All 66 AP Stars are active, with generated campaign gates and a Level 22 clear after
reaching the configured goal (1–66, default 50). An early boss clear followed by later
Star receipt does not win; another clear is required. Native result stars still report
performance checks and are never overwritten by AP Stars.

The 39 supplemental sources include Cell Tower Star Eater Fed and King Ferdinand
Unlocked. The six Bee/Devil completions remain separate binary checks. The user
confirmed 33 of the prior 37 checks; three Devil runs and the Certificate were skipped
and remain unverified. No replay of those skipped tests is required in this pass.

Normal has 152 modeled non-filler slots for 137 non-filler items (135 progression plus
two useful characters), not merely 164 raw checks. Existing item quantities and the
180-point Music Lab economy are unchanged. Native prerequisite closures are conservative
candidate rules; full native playthrough validation remains pending. The final world
passed a 48-case actual generation/playthrough matrix across four difficulties and
goals 1/25/50/66. A prior near-goal gate curve failed; the corrected curve caps entry
gates at half the goal while Victory still requires the full goal.

Detailed rules: `apworld/scrc/docs/ap-stars-logic.md`. Native acceptance, deployment
and release are separate steps. The older version/status sections below are historical
and do not override this candidate checkpoint.

---

# Current release checkpoint

## Testing release v0.26.0-dev / Client v0.73.9

This experimental prerelease adds randomized Old Game Data and Car Battery, in-game item popups and F6 history, corrected location names and cassette prerequisites, performance/reconnect repairs, and native Game Garage cartridge insertion and medal-preview fixes. It also includes new Plunger/Meoo/Maniac AP rewards and four quest checks awaiting fresh-seed gameplay acceptance.

Use Client v0.73.9 and APWorld v0.26.0 together with a fresh v0.26 seed and fresh native save for the new quest features. Existing schema-16 seeds retain their earlier behavior; updating files does not add checks to an old seed. Connect to the AP seed before loading the save.

Gameplay confirmed: Old Game Data/Car Battery hand-ins and persistence under schema 16, improved performance, local cold-start reconnect, and Bloody Tears/Gradius Garage insertion, availability and medal previews. v0.73.9 cleanup passed the Garage entry/exit check. New schema-17 quest flows and true cross-player notifications still need live testing. AP Stars, generated Star gates, final Victory and production Bee/Devil checks remain inactive.

New checks: Lobby - Plunger Pickup; Lobby - Car Battery Hand-In; Game Garage - Old Game Data Hand-In; Roots - Star Eater Fed. Active totals: Normal 125 / Hard 183 / Expert 241 / Perfection 277. Plunger Pickup and Roots Star Eater Fed are Stardust-only pending fuller prerequisite modeling; feeding still checks three native stars without spending them.

Next: validate new quest rewards/checks on a fresh seed, including early receipt and offline/reconnect isolation, then continue remaining quest logic and Star/Victory work.

---

Historical development and test guidance follows. For current release versions and acceptance boundaries use the checkpoint above.

# Testing Workflow

## New quest acceptance still required

Client v0.73.9 / APWorld v0.26.0 uses schema 17 / campaign-mapping schema 1,
with quest checks schema 1. Follow [the quest checklist](testing/2026-09-16-quest-checks-candidate.md)
for early/late character receipt, item-before-chest, source suppression,
offline/restart and identity isolation, and Plunger source availability after early
receipt/use. Native Roots admission was confirmed blocked at 2 stars and allowed
at 3 without consumption; its new AP check still needs fresh-seed testing.
Garage consumption and medal previews are accepted on the retained schema-16 seed.

> [!NOTE]
> For the public, end-to-end development-test smoke test and safe issue-report template, start with [Testing and issue reports](TESTING_AND_ISSUES.md). This document is the advanced developer checklist for targeted regression and diagnostics.

## Runtime environment baseline

Known working baseline:

- BepInEx `6.0.0-be.785`
- Unity `2021.3.26f1`
- .NET runtime `6.0.7`
- Game executable: `Rhythm Castle.exe`

## Client build

```powershell
cd .\client
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus" -SkipInstall
```

This builds without deployment. After review and explicit approval of the exact candidate artifact, install for live testing, launch normally, and verify `BepInEx\LogOutput.log` contains the expected client version.

## Full level mapping v0.24 candidate acceptance

Client v0.70.0 / APWorld v0.24.0 is an experimental candidate requiring manual acceptance with a fresh v0.24 seed and fresh native save. The expected addressed totals are Normal 121 / Hard 179 / Expert 237 / Perfection 273. Use the [full-level-mapping acceptance record](testing/2026-09-10-full-level-mapping-acceptance.md) for artifact hashes, exact run identity, first-clear and improved-result checks, reconnect evidence, diagnostics, unplayed mappings, and tester verdict. Existing automated coverage does not prove every native level identity in live gameplay.

Verify one normal first clear, one improved Star result, and an offline/reconnect result. Capture Bee and Devil diagnostic logs only; special variants are observation-only and must not queue checks. All 66 AP Stars, generated Star gates, and final Victory remain inactive. Broad campaign replay is deferred, so manual evidence fields must stay unchecked until a tester supplies them.

The expected pool is 10/3/7 items worth 1/10/20, totaling 180 and replacing 20 Stardust. The nine thresholds are 5/10/20/32/46/64/89/111/140, leaving 40 slack after the final chest. No point milestones are added; five cassette and two cartridge sources reuse their existing chest IDs.

Verify zero before authoritative synchronization, correct weighted display, each threshold immediately below/at, player-driven opening, and one existing AP check per chest. Confirm native medals persist without contributing AP points, disconnect retains the synchronized total and queues checks, reconnect/relaunch rebuild history without double-counting, recognized v0.22/non-AP sessions retain native scoring, and malformed point contracts report incompatibility and stay at zero. The candidate requires schema 16 / campaign-mapping schema 1; its retained Music Lab Point sub-contract uses point schema 1. Do not use the developer Shift+F4 override to satisfy thresholds.

The diagnostic-first review rejected the generated chest hook and native detour. The candidate uses only the existing managed getter in exact room `GameRoom_Hub6`, writes no native score/save state, and never invokes or forces a chest. Verify unrelated rooms preserve native behavior; failure of the display or any threshold fails acceptance and requires revising the design.

## APWorld changes

Whenever items, locations, rules, slot data, or datapackage IDs change:

1. Build a new `.apworld` with `tools/build-apworld.ps1`.
2. Replace the installed custom world.
3. Generate a **fresh seed**.
4. Prefer a fresh in-game save when testing first-time story/cutscene behavior.

Client-only fixes that do not change the APWorld can usually reuse an existing compatible seed.

## Current Roots regression checklist

For the accepted Roots repair evidence, use [the v0.67.94 / v0.19 area-arrival repair acceptance](testing/2026-08-27-area-arrival-repair-acceptance.md). The completed [v0.67.64 consolidated preview record](testing/2026-08-23-consolidated-preview-acceptance.md) remains the broader gameplay evidence baseline; verified items must not be repeated unless overlapping changes could invalidate them.

Before accepting a Roots milestone, confirm:

- Roots is the precollected starter in the seed.
- Hub6 â†’ Roots phone works.
- First Roots arrival does not steal movement with the displaced vanilla intro cutscene.
- `FirstAreaGate` is traversable.
- `StarEaterBlockade/Blockade` is traversable.
- Gecko's Weed Killer source sends `Roots - Gecko's Weed Killer`.
- Gecko does not directly grant the randomized Weed Killer item.
- Receiving `Weed Killer` creates the native item and vanilla consumes it for Level 3 access.
- Frog/Hippo sends `Roots - Level 3 - Plant Pipes Pickup`.
- Frog/Hippo does not directly grant the Plant Pipes ability.
- Without Plant Pipes, menu exit from Level 3 works.
- Receiving `Plant Pipes` grants `WEED_KILLER_ABILITY`.
- With Plant Pipes, Level 3 can be completed.
- Level 4 sets `LEVEL_08_GLASSES_COLLECTED` and sends `Roots - Level 4 - Hip Glasses` exactly once without locally granting Hip Glasses.
- AP-delivered Hip Glasses enables the normal Bucket Minion interaction.
- The normal trade consumes Hip Glasses, sends `Roots - Bucket Minion Trade`, preserves dialogue/blockade/King flags, and does not locally grant Chicken Bucket.
- AP-delivered Chicken Bucket enables the normal Lift Quest interaction.
- Lift Quest consumes Chicken Bucket, grants native Combo Bucket, completes Level 09, and transitions to the Lobby normally.
- Reloading/reconnecting while held preserves each item; reloading/reconnecting after consumption does not restore it or duplicate a check.
- An old/incompatible seed and a non-AP save retain the complete vanilla chain.
- Stable unrelated systems remain intact: Game Garage, Music Lab reward chests, cassettes, Secret Bunker.

## Game Garage cartridge persistence checklist

Use a compatible fresh v0.24 seed for the current candidate. Keep Vampire Killer on its physical vanilla route. Superstar passed the full persistence, insertion, restart/reconnect, save-switch, and entrance-preview acceptance on 2026-09-09 with v0.22. Repeat the same matrix for the remaining randomized cartridgesâ€”Bloody Tears, Gradius Remix, Smooch, and Wag the Dogâ€”before closing the broader blocker:

- the AP receipt appears in the native inventory;
- the matching cartridge is available for normal insertion in Game Garage;
- inserting it unlocks the matching song and removes the bag item normally;
- leaving and re-entering Game Garage preserves the registered song;
- save reload, AP reconnect, and switching local save slots on the same AP player slot preserve either the held or inserted state;
- received-item history never restores an already registered cartridge to the bag;
- no unreceived cartridge or song becomes available.

## Logs

`LogOutput.log` is a test artifact, not source. Do not commit it. For public reports, follow the redaction and excerpt guidance in [Testing and issue reports](TESTING_AND_ISSUES.md); do not attach credentials, private addresses, personal paths, game files, or proprietary assemblies.

Useful exact log phrases for the current Roots chain include:

```text
AREA ARRIVAL OVERRIDE ARMED
AREA ARRIVAL CONDITION BYPASSED
ROOTS FIRST AREA GATE SUPPRESSED
ROOTS STAR EATER BLOCKADE SUPPRESSED
ROOTS WEED KILLER VANILLA GRANT SUPPRESSED
ROOTS WEED KILLER SOURCE AP CHECK
ROOTS WEED KILLER NATIVE GRANT APPLIED
ROOTS PLANT PIPES VANILLA GRANT SUPPRESSED
ROOTS PLANT PIPES SOURCE AP CHECK
ROOTS PLANT PIPES NATIVE GRANT APPLIED
ROOTS BUCKET RANDOMIZATION ENABLED
ROOTS BUCKET VANILLA GRANT SUPPRESSED
ROOTS BUCKET SOURCE AP CHECK
ROOTS BUCKET NATIVE GRANT APPLIED
GAME GARAGE INSERTION SYNC pending|ready|failed
GAME GARAGE CARTRIDGE NATIVE GRANT APPLIED
GAME GARAGE INSERTION CANDIDATE ARMED
GAME GARAGE NATIVE CONSUMPTION OBSERVED
GAME GARAGE SERVER INSERTION WRITE PENDING
GAME GARAGE SERVER INSERTION CONFIRMED DURABLE
GAME GARAGE ALREADY INSERTED NO REGRANT
```

When testing later score or objective checks without Combo Bucket, report the exact level/song, native difficulty, AP performance tier, score, stars, medal or missed objective, player count, abilities, attempt count, best result, and whether `COMBO_BUCKET_ABILITY` was absent. Include a focused log excerpt and video when practical. A success proves feasibility under those conditions; a failed attempt alone does not prove impossibility and must not create a solver requirement by itself.


## Character quest item candidate (2026-09-15)

Client v0.71.0 / APWorld v0.25.0, schema 16, adds Old Game Data and Car Battery
as useful randomized inventory items, replacing two Stardust. Their existing
5-point and 20-point Music Lab chest checks are reused; character rewards remain
vanilla and are not new checks. Normal hand-ins unlock Maniac in Game Garage and
Meoo in the Lobby. Received items are queued from complete authenticated history,
then reconciled on the Unity thread only against the proven selected save.
Maniac's native saved character-unlock predicate and Meoo's unlock flag prevent
consumed items being re-granted. Unknown native state defers the grant.

A newly generated v0.25 seed is required to test these items. Existing v0.24 seeds
retain vanilla item behavior. Validate chest grants suppressed with checks intact,
received inventory, both hand-ins, reload/reconnect/offline and new-save isolation
before gameplay acceptance. No live deployment or new-seed replacement is implied.

## v0.74.2 starting-item repair candidate

Hip Glasses and Chicken Bucket now retry on the Unity update against the verified selected save, even without a native progression request. Grant suppression is scoped to the selected-save identity, retaining native consumption guards. Flag reflection metadata is cached; native values are read fresh. The same update retries pending Weed Killer delivery. Runtime regression covers initial delivery, save changes, duplicate prevention and consumed items. Gameplay acceptance pending.

## Temporary Star Eater threshold test — v0.74.2

Developer.RoyalStarRequirement defaults to native40; existing QualityOfLife.BunkerStarRequirement controls the Bunker. The user requested25 for both on the retained batch seed to avoid star grinding. The existing proximity-scoped override now selects only the audited Royal or Bunker root, restores native patches on target/scene change, and leaves earned stars, level scores and source flags untouched. The player must perform the native feed interaction; only its resulting native flag sends the AP check. This test does not validate vanilla40/66 thresholds or AP Star progression. Restore Royal40/Bunker66 after the grouped run.

## Client v0.75.1 Roots crash repair - 2026-09-18

Two v0.75.0 Windows crash dumps identify `ApStarEaterThresholds.SetReference` called by `Tick`. Installed Il2CppInterop.Runtime metadata confirms the setter ABI is object, field, value, whereas the getter is field, object. Corrected both threshold application and restoration. The focused ABI regression check fails against v0.75.0 before the fix and passes afterward. Build succeeds with zero errors. Native Roots entry and threshold application remain pending; retain the existing Hard / 40-star seed and slot 4 for acceptance.

## Client v0.75.2 Star Eater interaction repair - 2026-09-18

v0.75.1 failed live interaction acceptance after its threshold readback reported a mismatch. The new native crash is at GameAssembly+0xa706 while reading the threshold, with the StarEaterInteraction path on the stack. Read-only export disassembly confirms il2cpp_field_set_value_object at RVA 0x2f8de0 writes R8 directly to instance + field offset and applies the GC write barrier. The old ref IntPtr supplied an address of a temporary instead of the reference itself. Replaced both writes with the installed Il2CppInterop API, removed custom P/Invoke declarations, and added synchronous rollback on failed verification. Nine lifecycle fixture checks cover apply, stable tick, room change, contract removal, foreign ownership, failed readback rollback and retry. Native gameplay acceptance remains pending.

## Full-playthrough deferred follow-ups - 2026-09-18

Player requested that the Quicksand early native cassette grant and lag during Music Lab songs generally (including 10-4 Good Buddy) be recorded for investigation after the full Hard / 40 AP Star playthrough. Track both in KNOWN_ISSUES.md; treat the song lag as a high-priority scoring concern for higher difficulties. Continue the retained seed/save without deploying changes for these reports during this run.

## Client v0.75.3 performance candidate

The full playthrough is paused at the player's request for general gameplay lag. Metadata caches, bounded Hub6-only readiness discovery, and reduced idle allocations are implemented; optional aggregate timings support native diagnosis. Existing APWorld v0.28 seed/save remains compatible. See [performance review and acceptance plan](testing/2026-09-18-client-performance.md). Native smoothness acceptance is pending; no public release or deployment is implied.

Client v0.75.4 performance follow-up is built and locally validated, awaiting installation and native retest. Live v0.75.3 cassette persistence polling cost about 9.7 ms per frame; the remaining uncached adapter type enumeration is now routed through the shared catalog. See [performance review](testing/2026-09-18-client-performance.md).

## Level 22 Plant Pipes logic correction — 2026-09-18

The shared Level 22 completion predicate now requires Plant Pipes. Missing-item tests cover every enabled boss tier, cassette source, character/keycard rewards and Victory on all four difficulties. All 140 APWorld tests pass. The freshly packaged candidate passed 12 actual restrictive-fill/playthrough generations: Normal/Hard/Expert/Perfection at goals 1, 40 and 66. A separate exact-placement replay found the retained Hard/40 seed incompatible with this stricter rule; its original placement/save remains untouched. See the unreleased progression note for the dependency loop. Performance diagnostics-off gameplay comparison remains pending.

## Client v0.75.5 candidate — visible blocked-entry feedback

AP Star denial now displays a prominent top-center six-second banner with the verified campaign title, required/current AP Stars and remaining amount. Repeated attempts refresh one banner and suppress repeated log lines. Enough Stars, room/identity changes and native save boundaries invalidate stale denial feedback; unsynchronized and incompatible states explain their cause rather than claiming missing Stars. Uses the existing OnGUI path, independent of item-popup settings, without scans or save writes. Existing gate/victory rules are unchanged. Build and automated runtime tests pass; installation and native readability acceptance are pending. Published release remains Client v0.75.4.

### v0.75.5 candidate: readable reward bursts

Reward popups now measure wrapped item/player and location text and fit whole cards within the current screen height, leaving overflow in the queue with a visible remaining count. Only cards drawn in the preceding repaint accrue display time; item-history view, disabled popups and loss of focus pause timers. Long frame gaps are capped for notification aging so a load stall cannot consume a full popup lifetime. The unseen queue no longer silently drops rewards after 100 pending entries; recent history remains capped at 100 and connection/identity replay protection is retained. A 125-reward regression verifies exact arrival order and full display time with only one card fitting at a time. Item notification and real packet adapter tests pass; the combined client builds with existing warnings. Native layout/readability testing is pending. No game install or release publication has been performed for this candidate.

## September 20 full-mod audit candidate — Client v0.75.6

Full regression: 35 client projects pass (34 in the complete sweep; repaired QuestRuntime rerun passes 55 assertions), 141 APWorld tests pass, repository validator passes. Packaged APWorld actual generator/playthrough matrix passes 12/12 (four difficulties, goals 1/40/66). No-install client Release build has 0 errors and 0 C# warnings; NuGet vulnerability-service access remained unavailable. Review found no blocking managed regression; native cassette detour/popup completion and real overlay input remain acceptance requirements. See [audit report](testing/2026-09-20-full-mod-audit.md).

Next native acceptance: restart/reconnect the retained seed, verify an unopened cassette chest does not leak its native cassette while its AP check and animation complete, then legitimate AP receipt/insertion/restart and early-receipt source completion. Check F6 scrolling and burst popup visibility at actual game resolution. Profile full songs/Combo Bucket again; no claim that all residual lag is fixed. Existing leaked Quicksand remains untouched.

## Fullscreen comparison — September 20, Client v0.75.6

The player repeated the affected Fumblin Around section in fullscreen and reported no issues. Fullscreen is a confirmed workaround for this reproduction, not proof that all performance issues are resolved. The preceding windowed trace showed continued game frame submission during a roughly4.3-second pause in displayed-frame statistics, without a corresponding long CPU scheduling pause. The comparison supports investigating the windowed presentation/compositor path; the exact driver, overlay or compositor cause remains unproven.

Keep fullscreen for the current playthrough. Windowed-mode stutter remains open. Combo Bucket conversion and other songs still require separate acceptance. Temporary performance diagnostics are disabled in configuration for the next launch; the running game is unchanged. No driver/registry changes, save repair, restart or release publication was performed.

### Temporary player and follower movement capture (v0.75.8 candidate)

With the developer harness enabled, press F8 in any gameplay room when movement becomes abnormal. Keep moving normally for about 12 seconds and wait for the saved notice. Capture one visibly faulty attempt and, if available, a normal attempt after re-entry/restart. Do not compare frame rate alone: compare accepted movement input with Rewired movement input, then inspect player and nearby follower physics position versus visual position, velocity/facing, body configuration and fixed-update cadence; unavailable native input is explicitly recorded rather than treated as zero. The probe writes only a local diagnostic file and leaves inventory, saves, physics and movement unchanged. Its one-time discovery may briefly hitch when the key is pressed; do not interpret that as a reproduction. Remove this temporary probe after diagnosis.

v0.75.10 extends F8 capture with desired-versus-actual motor state and recent collision contacts. During a faulty-to-normal capture, verify native motor fields bind and contact observations arrive, then compare roomRunning, fixedUpdateAction, desiredVelocity, MovementIntentionLastUpdate, restrictions, and contact normals near each unexpected zero velocity. Empty recent contacts without observed callbacks are not proof that no collision occurred. No movement, collider, or save correction is performed by this diagnostic.

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

Validation: regression failed on v0.75.11, then 40 runtime checks and LevelCompletion production-connection checks passed. Release build succeeded (NU1900 vulnerability-feed warning). Installed DLL hash matched the tested build; game left closed for direct Steam launch. Live Victory acceptance remains pending.

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

## Release verification

All 35 client regression projects and 141 APWorld tests passed for this release.
Repository contract validation and diff checks passed. The client Release build
completed with zero errors; NuGet vulnerability-feed lookup was unavailable.
An independent code review found no critical/important new regression. The packaged
DLL matches the installed v0.75.13 build. Hard40 Victory is live-confirmed; the Tower
exterior test was explicitly deferred. Existing 12-case generation evidence is
historical; no new progression rules or IDs changed in this maintenance release.