# Music Lab Point native-boundary checkpoint

Status: diagnostic build prepared; deployment and live acceptance pending explicit user approval. Task 7 must not begin from this record alone. Production AP effective-score replacement remains disabled.

## Offline evidence

The installed interop metadata was inspected read-only with Mono.Cecil. It contains the exact global owner `Hub06MedalScoreRewardChest`, instance method `Hub06MedalScoreRewardChestState BuildState()` with zero arguments, and declared `DefinedInt unlockRequirement` property. The exact getter is static `System.Int32 CurrentPlayerSaveEnquiries.GetMedalScore()` with zero arguments. Runtime validation repeats these checks before patching and requires every live chest instance and threshold below to match.

Focused test command:

```powershell
dotnet run --project .\client\tests\MusicLabPoints\MusicLabPoints.Tests.csproj -c Release
```

Result: PASS, including default-off configuration, thread-local scope cleanup, exact owner/method validation, 64-record tuple deduplication, diagnostic mutation guards, and disabled production score replacement. These are source-level safety checks for the Unity boundary, not proof of live IL2CPP hook behavior.

Build command:

```powershell
powershell -ExecutionPolicy Bypass -File .\client\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\Titus" -SkipInstall
```

Result: build succeeded, 0 errors and 7 existing nullable warnings outside the changed diagnostic code; `Build complete; installation skipped.` No game launch or deployment was performed.

| Artifact | SHA-256 |
| --- | --- |
| Inspected `BepInEx/interop/Assembly-CSharp.dll` | `5FE14AAAA2599BFBC775A3E2ACC06E0994049C901A96F5124CEE57264DA16FD9` |
| Built `client/bin/Release/net6.0/RhythmCastleAP.dll` (v0.68.0, F5 routing fix) | `04D9B6F0DDC328F8DBF956C48725E5034E8BAE609DC78397D5AD415FB0D5267D` |
| Installed client observed after build; not replaced by this task | `3E6A983D6F4A95FD35AFCAEAD49902FC074F4A8515EFC663D7BFBA39871AFD4C` |

## Approved live-run procedure — not yet executed

After explicit deployment approval, verify the game is closed; install only the approved client output into `D:\SteamLibrary\steamapps\common\Titus\BepInEx\plugins\RhythmCastleAP`; record the installed hash. Use `Developer.EnableTestHarness=true` and `Developer.EnableMusicLabPointBoundaryDiagnostics=true`, then launch normally. Confirm BepInEx and the intended client hash/version. Do not use Shift+F4; its existing developer score override must remain OFF.

Enter live Music Lab / `GameRoom_Hub6`, wait for scene objects to be ready, and press plain F5 once. With the new key enabled, this invokes a separate read-only checkpoint instead of the ordinary F5 scan that may queue collected AP chest checks. The checkpoint validates all nine exact paths/thresholds before installing the exact chest builder prefix/postfix/finalizer. It reads the original score before/after validation and installation. It never invokes `BuildState`, forces a chest interaction, or changes native storage.

The enabled checkpoint consumes plain F5 at the hotkey entry before the legacy identity scan or room-specific scan. Repeated and unavailable attempts are consumed too; modified F5 shortcuts keep their existing routing. A focused source regression covers this entry order and enabled-state fallthrough.

Allow normal game refreshes to request chest/display state. Correlation logs are deduplicated by `(room, threshold, path, caller)` and capped at 64 records for the process; the F5 checkpoint runs once in Hub6. Restart after an unavailable marker to begin a new diagnostic run. Failure or ambiguity must stop acceptance, not trigger a guessed hook.

All paths below are relative to `Root/GameRoom_Hub6_Logic/Objects/RewardChests/`; every qualifying observation must name `Hub06MedalScoreRewardChest.BuildState()` and `CurrentPlayerSaveEnquiries.GetMedalScore()`.

| Native threshold | Native path suffix | Getter correlation |
| --- | --- | --- |
| 5 | `ManiacMemoryCardChest/Interaction` | PENDING |
| 10 | `GarageCartridgeChest_Gradius/Interaction` | PENDING |
| 20 | `CatBatteryChest/Interaction` | PENDING |
| 32 | `SongChest_QuickSand/Interaction` | PENDING |
| 46 | `GarageCartridgeChest_BloodyTears/Interaction` | PENDING |
| 64 | `SongChest_Flamenco/Interaction` | PENDING |
| 89 | `SongChest_TenFourGoodBuddy/Interaction` | PENDING |
| 111 | `SongChest_Zen/Interaction` | PENDING |
| 140 | `SongChest_Wiggle/Interaction` | PENDING |

| Required live evidence | Result |
| --- | --- |
| Explicit deployment approval and game-closed check | PENDING |
| Installed client hash, BepInEx startup, client version | PENDING |
| All nine exact chest correlations | PENDING |
| Display reads the same getter, with verified display ownership | PENDING |
| Native score before scan | PENDING |
| Native score after scan, identical to before | PENDING |
| No diagnostic exception, native write, forced chest, or unrelated result mutation | PENDING |
| Focused log excerpt and local log hash | PENDING |

An out-of-chest Hub6 observation is deliberately labeled `displayCandidate-unverified`, with path `<unscoped>`. It is one observation, not proof that a particular display called the getter. If live evidence cannot establish display ownership, stop before Task 7 and revise the diagnostic design. The build also does not prove what replacing the score would do to unrelated consumers; it does not replace the score.

Focused live log excerpt: PENDING — no fabricated gameplay evidence.

Acceptance decision: PENDING — diagnostics only; no production effective-score integration authorized by this checkpoint.
