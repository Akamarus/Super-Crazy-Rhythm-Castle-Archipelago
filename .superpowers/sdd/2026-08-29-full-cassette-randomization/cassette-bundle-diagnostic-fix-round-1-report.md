# Cassette bundle diagnostic fix round 1

## Review finding addressed

The original focused test exercised only `CassetteBundleCollisionDiagnosticPolicy.Format`; it could not fail when the Harmony state handoff or reflective capture broke.

The reflection capture is now isolated in `client/CassetteBundleCollisionDiagnostic.cs`, which is compiled by both the production client and the focused cassette test project. `Plugin.cs` retains only the Harmony/AP-origin adapter and the single final log write.

## RED-first evidence

The focused test was changed first to invoke the wished-for production capture API with native-shaped fake request, processor, state, nullable bundle, predicate, and status methods.

```powershell
dotnet run --project client\tests\CassetteRandomization\CassetteRandomization.Tests.csproj -c Release --disable-build-servers
```

Observed RED: exit 1 with `CS0246` for missing `CassetteBundleCollisionPatchState` and `CS0103` for missing `CassetteBundleCollisionDiagnostic`. This proved the production reflection implementation was absent from the test compilation boundary.

After extracting the production capture and linking it into the focused project, the same command exited 0 with `Cassette randomization catalog and source policy tests passed.`

## Behavioral coverage added

The focused test now proves:

- a non-AP request returns no capture and performs zero processor, predicate, or status calls;
- an AP-origin capture invokes `ObtainState` and the exact named cross-bundle predicate;
- a nullable/raw null request bundle is converted to the effective `DEFAULT` exclusion passed to the predicate;
- the predicate runs exactly once;
- `GetCassetteStatusForSong` runs once before and once after the existing request boundary;
- the postfix does not repeat the predicate;
- true predicate plus unchanged status reports `cross-bundle-rejection-indicated`;
- false predicate plus unchanged status reports `predicate-false-unchanged`;
- a simulated native request transition from `HAVE_NOT_EARNED` to `HAVE_IN_BAG` reports `stage-observed`; and
- source-level wiring assertions bind both prefix and postfix, carry Harmony `__state`, pass `CassetteReceiptRandomization.IsApplyingNativeGrant`, and complete that same state.

The native-shaped fake state exposes only the exact method names/signatures expected from the interop. Selecting a wrong method, omitting the predicate, constructing the nullable exclusion incorrectly, or dropping either status read fails the test.

## Runtime scope

No diagnostic behavior was broadened. The existing AP cassette request is still the only request. The extraction adds no save mutation API, bundle enumeration, alternate bundle choice, persistence request, or flush. `Complete` returns one formatted line; `Plugin.cs` writes it once from the postfix. Vanilla-origin requests remain gated before any reflective read.

## Verification

- Focused cassette test: passed.
- Complete client suite: all 17 test projects passed; count asserted as exactly 17.
- No-install release build against `D:\SteamLibrary\steamapps\common\Titus`: passed with 0 errors and the same 7 unrelated nullable warnings.
- Repository validator: passed using the bundled Python runtime.
- `git diff --check`: passed.

No install, game launch, merge, push, live request, save mutation, or log mutation was performed.

## Remaining concern

The automated test uses managed native-shaped fakes, so it validates reflection selection, nullable/default-bundle construction, call ordering/counts, outcome derivation, and source wiring without loading the IL2CPP runtime. The no-install build verifies compatibility with the actual generated interop assemblies. A live run is still required to obtain the decisive native evidence; it was intentionally outside this task.
