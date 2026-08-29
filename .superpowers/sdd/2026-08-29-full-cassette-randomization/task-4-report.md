# Task 4 Report — Generalize Client Cassette Source Interception

## Status

COMPLETE. The client now consumes an immutable 30-entry cassette catalog, intercepts every one of the 37 verified level/variant triggers through the existing exact level evaluator, and routes the five verified point-chest cassette sources through their existing AP locations. No install, launch, merge, push, publish, or release was performed.

## Changes

- Added `client/CassetteCatalog.cs` with the exact 30 reviewed entries, all aliases, immutable collections, lookups by item/native song, level-source lookup, and startup uniqueness/count validation.
- Added `client/CassetteRandomizationPolicy.cs` with a pure fail-safe decision API. Failed, unrelated, missing-status, and unreadable-status evaluations preserve native behavior. Complete newly-earned events queue exact AP sources and suppress only the cassette evaluator.
- Replaced the Money-only source hook with generic level/variant/status evaluation while preserving Money's tested source identity and the disabled crashing selected-save event hook.
- Added exact `RecordSongCassetteStatusInSaveDataRequest` interception for the five point-chest `HAVE_IN_BAG` grants. Their progression flags and all other chest rewards remain native; AP-origin grants are explicitly exempted at the policy boundary for Task 5.
- Routed existing 32/64/89/111/140 point chest checks through catalog-validated identities; non-cassette chest checks retain their existing fallback.
- Removed the replaced Money-only policy and test project after generic Money parity and runtime reconciliation compatibility were green.
- Added a focused console test project covering exact 30-entry parity, uniqueness, all 37 aliases, multi-reward sources, status safety, point-chest suppression, and the selected-save crash regression.

## TDD Evidence

### RED 1 — missing generic catalog/policy

Command:

`dotnet run --project client\tests\CassetteRandomization\CassetteRandomization.Tests.csproj -c Release`

Observed: build failed with `CS2001` because `CassetteCatalog.cs` and `CassetteRandomizationPolicy.cs` did not exist.

### GREEN 1 — generic catalog/policy

Same command.

Observed: `Cassette randomization catalog and source policy tests passed.`

### RED 2 — missing point-chest grant policy

Same command after adding point-chest expectations.

Observed: four `CS0117` errors because `ShouldSuppressNativePointChestGrant` did not exist.

### GREEN 2 — point-chest grant policy and exact request hook

Same command.

Observed: `Cassette randomization catalog and source policy tests passed.`

## Final Verification

- Client test batch 1: 9/9 projects passed.
- Client test batch 2: 8/8 projects passed.
- Total: 17/17 client test projects passed.
- `dotnet build client\RhythmCastleAP.csproj -c Release -p:GameDir='D:\SteamLibrary\steamapps\common\Titus'`: succeeded with 0 warnings and 0 errors.
- `git diff --check`: no whitespace errors (Git emitted only line-ending conversion notices).

## Commits

- `299a6f0c854903420a9c093319002fbaa6c2f39a` — `feat(client): intercept all cassette sources`
- The task report is committed separately as the commit containing this file.

## Self-review

- Compared all C# catalog rows and trigger aliases against `apworld/scrc/cassettes.py` and the reviewed evidence audit.
- Confirmed Money remains `Money Cassette` / `I_GOT_MONEY` / `Level 2 - Money Cassette` and includes both default and Bee Mode aliases.
- Confirmed Secret Bunker Devil Mode aliases remain client-interceptable.
- Confirmed the Level 4 physical event queues both Badass and Heavy Metal only when all mapped native statuses are readable.
- Confirmed unsafe partial/unreadable events queue nothing and allow native behavior.
- Confirmed the exact five reused point-chest names and no broad scene/object-name scan was added.
- Confirmed `SelectedPlayerSaveSlotChangedEvent.HandleEvent` remains unpatched.
- Confirmed unrelated worktrees and `WordFactori/` were untouched.

## Concerns / Follow-up

- Task 5 must replace the retained Money-only receipt reconciler with catalog-wide native `HAVE_IN_BAG` reconciliation and use the point-chest policy's AP-origin exemption while submitting native receipt requests.
- The 24 newly activated level-earned routes, their aliases, and the five point-chest request sequence still require the approved representative/manual gameplay verification before release claims.
