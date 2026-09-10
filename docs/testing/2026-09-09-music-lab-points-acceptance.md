# Music Lab Points acceptance record

Status: architecture revised after the first diagnostic failed closed. The native chest detour candidate is rejected and must not be deployed. Production AP effective-score replacement remains disabled until Task 7 is implemented, reviewed, built, and explicitly approved for live testing.

Task 6 cleanup is complete: the rejected boundary class/callback, temporary configuration and F5 interception, native method resolver, temporary dependency, and optional installed-interop probe have been removed. The existing managed getter Harmony postfix and exact nine-chest metadata mapping are preserved. Task 7's room gate and AP result replacement are not implemented by this cleanup.

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
| Final Task 7 candidate | PENDING | Requires fresh build, review, and hash-specific approval |

Task 6 verification: `dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release` passed after an observed RED for the rejected `INativeDetour` path. The `client/build.ps1` release build with `-SkipInstall` passed with 0 errors and the same 7 existing nullable warnings. `Plugin.cs` and `RhythmCastleAP.csproj` match the pre-diagnostic Task 5 baseline (`51e1510`) exactly. Before/after snapshots of all game-file paths, sizes and last-write times matched, and the installed client SHA-256 remained unchanged. No game launch or deployment occurred during cleanup.

## Offline acceptance required before deployment

- Focused Music Lab Point policy/wiring tests pass.
- Every client regression project passes.
- The release client build succeeds with `-SkipInstall`.
- Source and project files contain no Music Lab Point `INativeDetour`, native `BuildState` callback, generated chest-owner lookup, or temporary `MonoMod.RuntimeDetour` dependency.
- The postfix routes compatible AP totals only when the established current room is exactly `GameRoom_Hub6`.
- Legacy/non-AP behavior remains native, malformed/awaiting compatible sessions remain zero inside the Music Lab, and compatible developer overrides cannot supersede AP points.
- The production path contains no native field/save write, chest invocation, forced interaction, or recursive getter call.

## Live-run procedure — pending explicit approval

Use a fresh v0.23 seed and fresh native save. Before replacing any installed file, verify the game is closed and record the exact approved client hash. Install only to `D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP`, launch normally, and confirm the intended BepInEx client version/hash in the log.

Do not use the developer Shift+F4 score override during compatible AP testing. It must not supersede AP Music Lab Points.

### Score and room-scope checks

1. Enter `GameRoom_Hub6` with the compatible contract awaiting history; confirm the Music Lab display is zero and all nine chests are locked.
2. Complete history synchronization with no point items; confirm the display remains zero.
3. Deliver controlled 1-, 10-, and 20-point items; confirm the display changes by the exact weighted total and never exceeds 180.
4. Leave `GameRoom_Hub6`; confirm unrelated rooms preserve native score behavior.
5. Return to `GameRoom_Hub6`; confirm the synchronized AP total is restored without native medal/save changes.

### Threshold matrix

For every row, test one point below and exactly at the threshold. The chest must remain locked below the threshold and become interactable at the threshold. The client must not open it automatically.

| AP total | Existing location | Below threshold | At threshold | Opens only by player | Check sends once |
| ---: | --- | --- | --- | --- | --- |
| 5 | Music Lab - 5 Point Chest | PENDING | PENDING | PENDING | PENDING |
| 10 | Music Lab - 10 Point Chest | PENDING | PENDING | PENDING | PENDING |
| 20 | Music Lab - 20 Point Chest | PENDING | PENDING | PENDING | PENDING |
| 32 | Music Lab - 32 Point Chest | PENDING | PENDING | PENDING | PENDING |
| 46 | Music Lab - 46 Point Chest | PENDING | PENDING | PENDING | PENDING |
| 64 | Music Lab - 64 Point Chest | PENDING | PENDING | PENDING | PENDING |
| 89 | Music Lab - 89 Point Chest | PENDING | PENDING | PENDING | PENDING |
| 111 | Music Lab - 111 Point Chest | PENDING | PENDING | PENDING | PENDING |
| 140 | Music Lab - 140 Point Chest | PENDING | PENDING | PENDING | PENDING |

### Persistence and compatibility

- The five reused chest cassettes and two reused Garage cartridges follow their existing AP receipt/use paths with no duplicate locations.
- Cassette and Game Garage medals persist natively but do not add AP Music Lab Points.
- A temporary disconnect retains the last synchronized total; an available chest check queues and sends once after reconnect.
- Reconnect rebuilds the same total from authoritative history without double-counting.
- Relaunching with the same AP identity rebuilds the correct total.
- A recognized v0.22 seed uses the native Music Lab medal economy.
- A malformed v0.23 contract shows incompatibility and keeps the Music Lab effective total at zero.

## Acceptance decision

PENDING. The native-detour diagnostic is rejected. The feature becomes live-verified only after the room-scoped candidate passes the complete offline and gameplay matrix above.
