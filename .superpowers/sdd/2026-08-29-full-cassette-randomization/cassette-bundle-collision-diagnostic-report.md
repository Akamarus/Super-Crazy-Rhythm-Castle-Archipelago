# Cassette bundle-collision diagnostic report

## Scope and result

Implemented only the architecture audit's smallest one-run falsification diagnostic. The existing AP-origin `RecordSongCassetteStatusInSaveDataRequest` remains the only request. Its requested bundle remains unchanged, no bundle is persisted or flushed, and the save-change bundle dictionary is not enumerated.

The exact `PlayerSaveRequestProcessor.ProcessRequest(RecordSongCassetteStatusInSaveDataRequest)` Harmony hook now has a logging-only AP-request prefix/postfix pair. The prefix reads the request, resolves its effective bundle, calls `__instance.ObtainState()`, reads the selected-enquiry state pointer when available, invokes `PlayerSaveFileState.HasSongCassetteStatusBeenChangedByAnyBundle` with the effective bundle excluded, and reads `PlayerSaveFileState.GetCassetteStatusForSong`. The postfix reads the same state's cassette status again and emits one coherent line.

Vanilla-origin cassette requests do not create diagnostic state and do not log this line. Existing source-suppression behavior is unchanged.

## TDD evidence

The focused test was added before production instrumentation. It names and checks the three decisive outcomes and requires a single-line diagnostic containing the predicate plus before/after status.

RED command:

```powershell
dotnet run --project client\tests\CassetteRandomization\CassetteRandomization.Tests.csproj -c Release
```

Observed RED: exit 1 with `CS0246` for missing `CassetteBundleCollisionObservation` and `CS0103` for missing `CassetteBundleCollisionDiagnosticPolicy`. This was the expected failure because the diagnostic contract did not exist.

GREEN command:

```powershell
dotnet run --project client\tests\CassetteRandomization\CassetteRandomization.Tests.csproj -c Release --disable-build-servers
```

Observed GREEN: exit 0, `Cassette randomization catalog and source policy tests passed.`

The mutation check is direct: changing a true-and-unchanged observation away from `cross-bundle-rejection-indicated`, changing a false-and-unchanged observation away from `predicate-false-unchanged`, changing a status transition away from `stage-observed`, omitting the predicate, omitting before/after, or adding a newline fails the focused test.

## Automated verification

- Focused cassette test: passed.
- Complete client suite: all 17 test projects passed; the runner asserted the project count was exactly 17.
- No-install release build against `D:\SteamLibrary\steamapps\common\Titus`: passed with 0 errors and 7 nullable warnings outside the new diagnostic.
- Repository validator: passed.
- `git diff --check`: passed.

No client was installed, no game was launched, and no live save or log was changed.

## Exact live log signatures

Every AP-origin attempt emits exactly one line beginning:

```text
[SCRC-AP] CASSETTE BUNDLE COLLISION DIAGNOSTIC song='<song>' requestedStatus='<status>' rawBundle='<raw-or-null>' effectiveBundle='<bundle>' processorState=<pointer> enquiryState=<pointer-or-unavailable> sameState=<True|False|unavailable> crossBundle=<True|False|unavailable> crossBundleStatus='<native-out-status>' before='<native-status>' after='<native-status>' outcome='<outcome>' error='<none-or-message>' readOnly=True extraRequest=False persistenceChanged=False.
```

For the failing Quieres Bailar reproduction, the decisive signatures are:

### Hypothesis confirmed: silent cross-bundle rejection indicated

```text
[SCRC-AP] CASSETTE BUNDLE COLLISION DIAGNOSTIC song='QUIERES_BAILAR' requestedStatus='HAVE_IN_BAG' rawBundle='DEFAULT' effectiveBundle='DEFAULT' processorState=0x... enquiryState=0x... sameState=True crossBundle=True crossBundleStatus='<native-out-status>' before='HAVE_NOT_EARNED' after='HAVE_NOT_EARNED' outcome='cross-bundle-rejection-indicated' error='<none>' readOnly=True extraRequest=False persistenceChanged=False.
```

Only after this signature should a later diagnostic enumerate bundles once to identify the competing key and producer. This change deliberately does not perform that enumeration.

### Hypothesis falsified: predicate false and native status unchanged

```text
[SCRC-AP] CASSETTE BUNDLE COLLISION DIAGNOSTIC song='QUIERES_BAILAR' requestedStatus='HAVE_IN_BAG' rawBundle='DEFAULT' effectiveBundle='DEFAULT' processorState=0x... enquiryState=0x... sameState=True crossBundle=False crossBundleStatus='<null>' before='HAVE_NOT_EARNED' after='HAVE_NOT_EARNED' outcome='predicate-false-unchanged' error='<none>' readOnly=True extraRequest=False persistenceChanged=False.
```

This directs the next investigation to state identity or IL2CPP request invocation/boxing, not persistence.

### AddChange staging observed

```text
[SCRC-AP] CASSETTE BUNDLE COLLISION DIAGNOSTIC song='QUIERES_BAILAR' requestedStatus='HAVE_IN_BAG' rawBundle='DEFAULT' effectiveBundle='DEFAULT' processorState=0x... enquiryState=0x... sameState=True crossBundle=False crossBundleStatus='<null>' before='HAVE_NOT_EARNED' after='HAVE_IN_BAG' outcome='stage-observed' error='<none>' readOnly=True extraRequest=False persistenceChanged=False.
```

This proves the native stage became immediately visible; only then is the separate matching-bundle persistence/write lifecycle the remaining issue.

### Diagnostic could not obtain all native evidence

```text
[SCRC-AP] CASSETTE BUNDLE COLLISION DIAGNOSTIC ... outcome='diagnostic-error' error='<message>' readOnly=True extraRequest=False persistenceChanged=False.
```

An unavailable selected-enquiry pointer alone is reported as `enquiryState=<unavailable> sameState=<unavailable>` and does not prevent the predicate or before/after checks.
