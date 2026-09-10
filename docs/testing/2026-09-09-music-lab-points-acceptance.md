# Music Lab Points acceptance record

Status: **experimental Client v0.69.0 / APWorld v0.23.0 candidate partially live-verified but not accepted for release**. Offline verification and scoped final review passed. Live testing verified weighted receipts, the 180-point cap, the upper four chest thresholds, all nine one-time chest checks, medal invariance, retained disconnected display, full-relaunch restoration, and automatic reconnect/history rebuild. A repeat diagnostic showed that the earlier reconnect failure was a 30-second backoff delay rather than a stopped worker. A responsiveness change and a fix for the separate Game Garage entrance-preview false positive now pass offline regression tests but have not been reviewed, deployed, or live-tested. The lower five threshold pairs, offline queued-check recovery, and compatibility regressions remain incomplete. The native chest detour candidate is rejected and must not be deployed.

Task 6 cleanup removed the rejected boundary class/callback, temporary configuration and F5 interception, native method resolver, temporary dependency, and optional installed-interop probe. Task 7 then implemented AP result replacement through the existing managed getter only in exact room `GameRoom_Hub6`. Final review through commit `9e8c89bb61d7b79778f2d4cf899be2fcbdc42e50` and offline verification passed all 19 client regression projects and a `-SkipInstall` build (0 errors, 7 existing nullable warnings). The final artifact hashes were recorded and explicitly approved for this live run; the partial live results below do not constitute release acceptance.

The fresh v0.23 contract uses top-level schema 14 and point schema 1. Its 10/3/7 items at values 1/10/20 replace 20 Stardust and total 180 points; the final threshold is 140 with 40 slack. No point milestones or duplicate cassette/cartridge source checks are added. Exact names, IDs, values, counts, totals, cap, and threshold-to-location map must match. Before sync the effective hub score is zero; after sync it uses authoritative AP receipts, retains the total through disconnects, and rebuilds on reconnect/relaunch. Native medals contribute no AP points and receive no AP score/save writes. Recognized v0.22/non-AP sessions retain native scoring; malformed v0.23 sessions report incompatibility and stay at zero.

Final-review corrections require the actual server JSON contract to validate, and a complete index-zero received-item packet to establish history readiness, including an authoritative empty history. Login alone and partial replay cannot replace a retained total. Getter unavailability rejects a claimed v0.23 session with an explicit incompatibility reason and zero effective score; recovery requires a new session or identity boundary. Deliberate native-score reads bypass AP replacement, and Shift+F4 cycling does nothing while AP owns the effective score. These automated boundaries still require the live checks below.

## Diagnostic evidence and decision

Read-only interop inspection confirmed the exact managed getter `System.Int32 CurrentPlayerSaveEnquiries.GetMedalScore()` and the nine `Hub06MedalScoreRewardChest` instances with native thresholds `5/10/20/32/46/64/89/111/140`.

Live attempt 1 failed before installing the original Harmony chest hook:

```text
MUSIC LAB POINT BOUNDARY room='GameRoom_Hub6' threshold=-1 path='<unavailable>' caller='unavailable' MUSIC LAB POINT BOUNDARY UNAVAILABLE reason='scan/hook failed: GenericArguments[0], 'Hub06MedalScoreRewardChestState', on 'WorldEntity`3[StateClass,PublicStateInterface,InitialConfig]' violates the constraint of type parameter 'StateClass'.'
```

An offline installed-interop probe reproduced that failure at `Assembly.GetType("Hub06MedalScoreRewardChest")`. The generated chest wrapper cannot be used as a Harmony owner because its generated CLR hierarchy violates a generic constraint.

A native BepInEx/Dobby detour was then investigated but rejected before deployment. The installed API discards native commit/undo status, can commit a native patch before a later managed exception prevents the caller from retaining the handle, and exposes no verified callback-quiescence contract before rooted delegates are released. The project will not assume those lifecycle guarantees, retain an unremovable process-lifetime hook, or attempt another guessed chest callback.

Approved correction: remove the native diagnostic and use only the existing loadable `GetMedalScore()` Harmony postfix. AP replacement is restricted to the Music Lab hub room `GameRoom_Hub6`. All other rooms preserve native/developer behavior. The existing exact-path chest metadata reader remains read-only and does not invoke chest builders, force interactions, or write score/save state.

## Artifact accounting

| Artifact | SHA-256 | Status |
| --- | --- | --- |
| Inspected `BepInEx/interop/Assembly-CSharp.dll` | `5FE14AAAA2599BFBC775A3E2ACC06E0994049C901A96F5124CEE57264DA16FD9` | Read-only evidence |
| Installed client from failed diagnostic attempt | `2E20DBAB30792E28E57D2C0C576C753CD0B8DADB5932F1644A12AFE4947601C6` | Unchanged; production AP score replacement disabled |
| Rejected native-detour candidate | Not applicable | Must not be deployed |
| Task 6 no-detour cleanup build, v0.68.0 | `03608FF06D49AF9FCE596168DB2157BC694A8735D05E0E1D4018A9BDC68D8B81` | Offline baseline only; not deployed; AP score replacement disabled |
| Reviewed Task 7 offline candidate, v0.68.0 (`ba3d664`) | `A711B192599397302EFF44C64AFB6C432EA75909A0B9634F849785623A6A6C83` | Room-scoped AP replacement implemented; not deployed |
| Final v0.69.0 candidate DLL | `8D3DD7EB64130CB08D877F4DF17CD5E98DDF15E396E29FF77BC776E16790D695` | Reviewed and explicitly approved for this live test; installed only in the restricted RhythmCastleAP plugin directory |
| Post-run reconnect/preview fix candidate DLL | `99F3755D28917E9BF02E1296D4BA8468CF46845A499D04006490440B63233967` | Offline only; all 19 client test projects and `-SkipInstall` build passed; not reviewed, approved, or deployed |
| Final v0.23.0 APWorld package | `2651D261276BEA5416476D2376FB4F6F7C31A8A86EB9DC1D165817F9AEEF41DF` | Reviewed and explicitly approved for this live test; installed as the local custom world |

Task 6 verification: `dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release` passed after an observed RED for the rejected `INativeDetour` path. The `client/build.ps1` release build with `-SkipInstall` passed with 0 errors and the same 7 existing nullable warnings. `Plugin.cs` and `RhythmCastleAP.csproj` match the pre-diagnostic Task 5 baseline (`51e1510`) exactly. Before/after snapshots of all game-file paths, sizes and last-write times matched, and the installed client SHA-256 remained unchanged. No game launch or deployment occurred during cleanup.

The preceding Task 6 comparison describes that historical cleanup commit only. Task 7 subsequently changed the managed getter wiring; it did not restore any rejected detour.

## Live-run identity and evidence template

Complete a new copy for each candidate/run. Keep credentials out of this record; use non-secret local labels for private server/slot details.

| Field | Recorded value / evidence |
| --- | --- |
| Test date and tester | 2026-09-10; Jack |
| Exact tested commit | `9e8c89bb61d7b79778f2d4cf899be2fcbdc42e50` |
| Client version and DLL SHA-256 | v0.69.0; `8D3DD7EB64130CB08D877F4DF17CD5E98DDF15E396E29FF77BC776E16790D695` |
| APWorld version and package SHA-256 | v0.23.0; `2651D261276BEA5416476D2376FB4F6F7C31A8A86EB9DC1D165817F9AEEF41DF` |
| Seed name, generation ID, and seed/archive reference | Seed input `91023`; generated archive `AP_46374822206579019479.zip`; SHA-256 `63C26AE9B0B1D25528EE76B9896CACF32F3D64016871A0C15163477E7E70B4B2` |
| Server/room label and AP player slot/name | Local test server `127.0.0.1:38281`; slot `Jack`; no password |
| Native save slot and fresh-save confirmation | UI save slot 4; created fresh for this run |
| Game version/build and platform | Steam on Windows 10 x64; exact game build number not recorded |
| BepInEx, Unity, and runtime versions | BepInEx 6.0.0-be.785; Unity 2021.3.26f1; .NET runtime 6.0.7 |
| Final offline verification / review reference | Scoped final review through `9e8c89b` passed; APWorld 93/93 tests, all 19 client test projects, repository validator, APWorld packaging, and client `-SkipInstall` build passed |
| Exact-artifact deployment approval | Explicitly approved by Jack before deployment; installed hashes matched the reviewed artifacts |
| Diagnostic result and log/video reference | `D:\SteamLibrary\steamapps\common\Titus\BepInEx\LogOutput.log`; live screenshots and tester observations recorded in the 2026-09-10 development task |
| Errors, exceptions, or mismatches | Initial reconnect observation ended during the 30-second backoff; a repeat diagnostic confirmed automatic recovery and exact history rebuild to 180. Game Garage entrance preview showed Bloody Tears and Gradius despite neither being AP-owned or playable. |

## Live results template

| Check | Result / evidence |
| --- | --- |
| Contract accepted; zero before history sync; all chests locked | PARTIAL PASS — contract accepted and awaiting-history display logged as zero; a complete all-chests-locked visual sweep was not recorded before receipts began |
| Synchronized with zero receipts; existing display remains zero | PASS — synchronized empty history remained zero |
| Weighted 1/10/20 receipts and cap 180 | PASS — controlled 0→1→11→31; later 141→161→180, with one cap-anomaly diagnostic and no value above 180 |
| Outside `GameRoom_Hub6` native behavior / return to hub AP total | PARTIAL PASS — leaving/returning and relaunch restoration were observed; a controlled native-score comparison outside Hub6 remains pending |
| Five cassette and two cartridge source reuses; no duplicate check | PARTIAL PASS — all nine chest sources sent their existing check IDs once and duplicate native events were ignored; the full five-cassette/two-cartridge use-path matrix remains incomplete |
| Native campaign/cassette/Garage medal invariance | PASS — Party Non Stop Silver and Vampire Killer Gold sent their normal medal checks while AP total remained 141 |
| Disconnect retained total and queued chest check ID | PARTIAL PASS — disconnected display retained 141; queued-check behavior could not be tested because all nine chests had already been opened |
| Reconnect history rebuild, same total, queued check sent once | PARTIAL PASS — a repeat diagnostic kept the game running, stopped/restarted the same server, automatically reconnected on the next 30-second attempt, and rebuilt 180 without double-counting; queued-check recovery remains untested |
| Delayed/partial reconnect retains total; authoritative empty history corrects to zero | PENDING |
| Getter unavailable reports incompatibility; recovered getter needs new session/identity | PENDING |
| Deliberate native read remains native; Shift+F4 does not change AP eligibility | PENDING |
| Full relaunch with same AP identity | PASS — relaunch connected to the same server/save, rebuilt 141 from complete history, and preserved opened chests |
| AP identity replacement clears prior point state | PENDING |
| Recognized v0.22 regression / native scoring | PENDING |
| Non-AP regression / native scoring | PENDING |
| Malformed-v0.23 regression / incompatibility and zero | PENDING |
| Errors and log/video references | Client log above. Reconnect correctness passed on repeat, but the 30-second maximum wait was too easy to mistake for a stopped worker. The adjacent Garage entrance-preview false positive remains live-unfixed. No crash or point-total corruption observed during this run. |
| Tester approval, date, and any remaining concerns | NOT YET APPROVED — 2026-09-10 run remains partial pending fixes and targeted regression |

## Offline acceptance required before deployment

- Focused Music Lab Point policy/wiring tests pass.
- Every client regression project passes.
- The release client build succeeds with `-SkipInstall`.
- Source and project files contain no Music Lab Point `INativeDetour`, native `BuildState` callback, generated chest-owner lookup, or temporary `MonoMod.RuntimeDetour` dependency.
- The postfix routes compatible AP totals only when the established current room is exactly `GameRoom_Hub6`.
- Legacy/non-AP behavior remains native, malformed/awaiting compatible sessions remain zero inside the Music Lab, and Shift+F4 cycling is a no-op while AP owns the score.
- Real packet/login JSON scalar wrappers pass exact contract validation; invalid scalar kinds, values, and map entries fail closed.
- Complete index-zero history, including an empty array, is required before first synchronization or reconnect correction; replay callbacks never publish prefixes.
- Getter availability is an enforced compatibility prerequisite and deliberate native reads return the untouched getter result.
- The production path contains no native field/save write, chest invocation, forced interaction, or recursive getter call.

## Live-run procedure — pending explicit approval

Use a fresh v0.23 seed and fresh native save. Before replacing any installed file, verify the game is closed and record the exact approved client hash. Install only to `D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP`, launch normally, and confirm the intended BepInEx client version/hash in the log.

Verify Shift+F4 is a no-op during compatible AP testing; it must not change AP Music Lab Points or chest eligibility.

### Score and room-scope checks

1. Enter `GameRoom_Hub6` with the compatible contract awaiting history; confirm the Music Lab display is zero and all nine chests are locked.
2. Complete history synchronization with no point items; confirm the display remains zero.
3. Deliver controlled 1-, 10-, and 20-point items; confirm the display changes by the exact weighted total and never exceeds 180.
4. Leave `GameRoom_Hub6`; confirm unrelated rooms preserve native score behavior.
5. Return to `GameRoom_Hub6`; confirm the synchronized AP total is restored without native medal/save changes.

### Threshold matrix

For every row, test one point below and exactly at the threshold. The chest must remain locked below the threshold and become interactable at the threshold. The client must not open it automatically.

| Threshold | Existing location | Existing check ID | Below: display / locked | At: display / interactable | Opens only by player | Observed check ID / sends once |
| ---: | --- | ---: | --- | --- | --- | --- |
| 5 | Music Lab - 5 Point Chest | 187256169 | Not controlled | Not controlled | PASS | `187256169`; once |
| 10 | Music Lab - 10 Point Chest | 187256170 | Not controlled | Not controlled | PASS | `187256170`; once |
| 20 | Music Lab - 20 Point Chest | 187256171 | Not controlled | Not controlled | PASS | `187256171`; once |
| 32 | Music Lab - 32 Point Chest | 187256172 | Not controlled | Not controlled | PASS | `187256172`; once |
| 46 | Music Lab - 46 Point Chest | 187256173 | Not controlled | Not controlled | PASS | `187256173`; once |
| 64 | Music Lab - 64 Point Chest | 187256045 | 63: PASS | 64: PASS | PASS | `187256045`; once |
| 89 | Music Lab - 89 Point Chest | 187256166 | 88: PASS | 89: PASS | PASS | `187256166`; once |
| 111 | Music Lab - 111 Point Chest | 187256167 | 110: PASS | 111: PASS | PASS | `187256167`; once |
| 140 | Music Lab - 140 Point Chest | 187256168 | 139: PASS | 140: PASS | PASS | `187256168`; once |

### Persistence and compatibility

- The five reused chest cassettes and two reused Garage cartridges follow their existing AP receipt/use paths with no duplicate locations.
- Cassette and Game Garage medals persist natively but do not add AP Music Lab Points.
- A temporary disconnect retains the last synchronized total; an available chest check queues and sends once after reconnect.
- Reconnect rebuilds the same total from authoritative history without double-counting.
- Relaunching with the same AP identity rebuilds the correct total.
- A recognized v0.22 seed uses the native Music Lab medal economy.
- A malformed v0.23 contract shows incompatibility and keeps the Music Lab effective total at zero.

## Acceptance decision

**NOT ACCEPTED — targeted review and regression required.** The reviewed room-scoped implementation passed offline verification and the core weighted-point, cap, upper-threshold, one-time-check, medal-invariance, disconnected-display, automatic-reconnect/history-rebuild, and full-relaunch tests. The repeat reconnect diagnostic proved that the worker had not stopped; it was waiting at its 30-second maximum backoff. The post-run candidate reduces that maximum wait to five seconds while preserving infinite retries and stale-worker protection, but the new artifact is not yet reviewed or live-tested. Queued-check recovery, the lower five exact threshold pairs, and the v0.22/non-AP/malformed-v0.23 compatibility matrix remain unverified.

An adjacent defect was discovered during the same run: the Game Garage entrance preview treated native source-collected flags as ownership for randomized cartridges. Bloody Tears and Gradius appeared in the entrance preview even though the player had not received them through AP and the actual Garage correctly kept them unplayable. The post-run candidate makes AP ownership authoritative for the five randomized preview entries while retaining native behavior for disabled/incompatible sessions and Vampire Killer. This fix requires review and focused live regression before acceptance.
