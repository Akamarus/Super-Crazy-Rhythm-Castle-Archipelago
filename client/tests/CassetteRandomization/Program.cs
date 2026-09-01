using RhythmCastleAP;
using Newtonsoft.Json.Linq;
using System.Text.Json;

static void Equal<T>(T expected, T actual, string scenario) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}"); }
static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual, string scenario) { var e=expected.ToArray(); var a=actual.ToArray(); if (!e.SequenceEqual(a, StringComparer.Ordinal)) throw new InvalidOperationException($"{scenario}: expected [{string.Join(", ",e)}], got [{string.Join(", ",a)}]"); }
static DirectLookupRegistryFixture BuildTargetRegistry(object processor, int otherEntries = 0)
{
    var entries = new Dictionary<PublicTypeKeyFixture, RegisteredRequestProcessor>();
    for (int index = 0; index < otherEntries; index++)
        entries[new PublicTypeKeyFixture($"OtherRequest{index:D3}")] = new(processor);
    entries[new PublicTypeKeyFixture(nameof(PersistSaveChangeBundleRequest))] = new(processor);
    return new(entries);
}
static IReadOnlyList<string> ExtractMethods(string source, string signaturePrefix)
{
    var methods = new List<string>();
    int searchFrom = 0;
    while ((searchFrom = source.IndexOf(signaturePrefix, searchFrom, StringComparison.Ordinal)) >= 0)
    {
        int bodyStart = source.IndexOf('{', searchFrom);
        if (bodyStart < 0) throw new InvalidOperationException($"method body missing for {signaturePrefix}");
        int depth = 0;
        int bodyEnd = bodyStart;
        for (; bodyEnd < source.Length; bodyEnd++)
        {
            if (source[bodyEnd] == '{') depth++;
            else if (source[bodyEnd] == '}' && --depth == 0) { bodyEnd++; break; }
        }
        if (depth != 0) throw new InvalidOperationException($"method body unbalanced for {signaturePrefix}");
        methods.Add(source[searchFrom..bodyEnd]);
        searchFrom = bodyEnd;
    }
    return methods;
}

TestCassetteRequest.ResetCounters();
var constructedRequest = CassetteNativeRequestFactory.TryCreateHaveInBagRequest(
    typeof(TestCassetteRequest),
    typeof(TestSong),
    typeof(TestCassetteStatus),
    typeof(TestBundle),
    nameof(TestSong.QUIERES_BAILAR),
    out object? request,
    out string requestDetail);
Equal(true, constructedRequest, "cassette request uses the public absent-bundle construction path");
var typedRequest = (TestCassetteRequest)request!;
Equal(TestSong.QUIERES_BAILAR, typedRequest.Song, "public Song setter receives the song");
Equal(TestCassetteStatus.HAVE_IN_BAG, typedRequest.CassetteStatus, "public CassetteStatus setter receives bag status");
Equal(false, typedRequest.Bundle.HasValue, "verified request leaves Bundle absent");
Equal(true, typedRequest.ParameterlessConstructorUsed, "request uses the public parameterless constructor");
Equal(0, TestCassetteRequest.SemanticConstructorCalls, "broken semantic constructor is never called");
Equal(0, typedRequest.BundleSetterCalls, "broken nullable Bundle setter is never called");
Equal(TestBundle.DEFAULT, typedRequest.Bundle.HasValue ? typedRequest.Bundle.Value : TestBundle.DEFAULT, "native HasValue ? Value : DEFAULT semantics resolve the absent payload to DEFAULT");
Equal("actualSong='QUIERES_BAILAR' actualStatus='HAVE_IN_BAG' actualBundle='present=False value=<null>' effectiveBundle='DEFAULT/1(native absent fallback)'", requestDetail, "construction detail distinguishes actual absence from the native DEFAULT fallback");

Equal(true, CassetteNativeRequestFactory.TryCreateHaveInBagRequest(
    typeof(EmptyInteropConstructionFixture.RecordSongCassetteStatusInSaveDataRequest),
    typeof(TestSong),
    typeof(TestCassetteStatus),
    typeof(TestBundle),
    nameof(TestSong.QUIERES_BAILAR),
    out object? emptyInteropRequest,
    out string emptyInteropDetail), "proven IL2CPP empty-nullable getter NRE is normalized as absent");
Equal(true, emptyInteropRequest != null, "exact empty-nullable normalization returns a verified request");
Equal("actualSong='QUIERES_BAILAR' actualStatus='HAVE_IN_BAG' actualBundle='present=False value=<null>' effectiveBundle='DEFAULT/1(native absent fallback)'", emptyInteropDetail, "exact empty-nullable path reports the native fallback honestly");

foreach (var rejectedFactory in new[]
{
    (Request: typeof(MissingParameterlessConstructorFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "public parameterless cassette request constructor unavailable"),
    (Request: typeof(PrivateParameterlessConstructorFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "public parameterless cassette request constructor unavailable"),
    (Request: typeof(ThrowingParameterlessConstructorFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request parameterless-constructor-invocation:InvalidOperationException:parameterless-constructor"),
    (Request: typeof(MissingSongSetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request public Song getter/setter unavailable"),
    (Request: typeof(PrivateSongSetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request public Song getter/setter unavailable"),
    (Request: typeof(ThrowingSongSetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request song-set-invocation:InvalidOperationException:song-set"),
    (Request: typeof(MissingSongGetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request public Song getter/setter unavailable"),
    (Request: typeof(PrivateSongGetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request public Song getter/setter unavailable"),
    (Request: typeof(ThrowingSongGetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request song-readback-invocation:InvalidOperationException:song-get"),
    (Request: typeof(MissingStatusSetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request public CassetteStatus getter/setter unavailable"),
    (Request: typeof(PrivateStatusSetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request public CassetteStatus getter/setter unavailable"),
    (Request: typeof(ThrowingStatusSetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request status-set-invocation:InvalidOperationException:status-set"),
    (Request: typeof(MissingStatusGetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request public CassetteStatus getter/setter unavailable"),
    (Request: typeof(PrivateStatusGetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request public CassetteStatus getter/setter unavailable"),
    (Request: typeof(ThrowingStatusGetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request status-readback-invocation:InvalidOperationException:status-get"),
    (Request: typeof(PrivateBundleGetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request public Bundle getter unavailable"),
    (Request: typeof(ThrowingBundleGetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request bundle-readback-invocation:InvalidOperationException:bundle-get"),
    (Request: typeof(UnrelatedNullBundleGetterFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request bundle-readback-invocation:NullReferenceException:unrelated-null"),
    (Request: typeof(NullBundleReadbackFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request bundle readback was null"),
    (Request: typeof(MalformedBundleFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request public Bundle nullable contract unavailable"),
    (Request: typeof(MismatchedSongReadbackFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request song readback mismatch:expected=QUIERES_BAILAR:actual=INVALID"),
    (Request: typeof(MismatchedStatusReadbackFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request status readback mismatch:expected=HAVE_IN_BAG:actual=INVALID"),
    (Request: typeof(PresentInvalidBundleFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request bundle readback mismatch:expected=<absent>:actual=INVALID/0"),
    (Request: typeof(PresentDefaultBundleFixture.RecordSongCassetteStatusInSaveDataRequest), Stage: "cassette request bundle readback mismatch:expected=<absent>:actual=DEFAULT/1"),
})
{
    Equal(false, CassetteNativeRequestFactory.TryCreateHaveInBagRequest(
        rejectedFactory.Request,
        typeof(TestSong),
        typeof(TestCassetteStatus),
        typeof(TestBundle),
        nameof(TestSong.QUIERES_BAILAR),
        out object? rejectedRequest,
        out string rejectedDetail), $"{rejectedFactory.Stage} fails closed");
    Equal<object?>(null, rejectedRequest, $"{rejectedFactory.Stage} cannot return a request for processing");
    Equal(rejectedFactory.Stage, rejectedDetail, $"{rejectedFactory.Stage} is reported exactly");
}

PersistSaveChangeBundleRequest.ResetConstructionCounters();
Equal(true, CassetteNativeRequestFactory.TryCreateDefaultUrgentPersistRequest(
    typeof(PersistSaveChangeBundleRequest),
    typeof(ePlayerSaveChangeBundleKey),
    typeof(eSaveFileWriteType),
    out object? persistConstruction,
    out string persistConstructionDetail), "persist request uses the public absent-bundle construction path");
var typedPersistConstruction = (PersistSaveChangeBundleRequest)persistConstruction!;
Equal(true, typedPersistConstruction.ParameterlessConstructorUsed, "persist request uses the public parameterless constructor");
Equal(false, typedPersistConstruction.Bundle.HasValue, "verified persist request leaves Bundle absent");
Equal(eSaveFileWriteType.URGENT, typedPersistConstruction.WriteTypeToRequest, "public WriteTypeToRequest setter receives URGENT");
Equal(1, typedPersistConstruction.WriteTypeSetterCalls, "persist construction explicitly invokes the public plain-enum write-type setter");
Equal(0, PersistSaveChangeBundleRequest.SemanticConstructorCalls, "broken two-argument persist constructor is never called");
Equal(0, typedPersistConstruction.BundleSetterCalls, "broken nullable persist Bundle setter is never called");
Equal(ePlayerSaveChangeBundleKey.DEFAULT,
    typedPersistConstruction.Bundle.HasValue ? typedPersistConstruction.Bundle.Value : ePlayerSaveChangeBundleKey.DEFAULT,
    "native persist HasValue ? Value : DEFAULT semantics resolve the absent payload to DEFAULT");
Equal("actualBundle='present=False value=<null>' effectiveBundle='DEFAULT/1(native absent fallback)' actualWriteType='URGENT/0'", persistConstructionDetail, "persist construction detail reports actual payload semantics");

Equal(true, CassetteNativeRequestFactory.TryCreateDefaultUrgentPersistRequest(
    typeof(EmptyInteropPersistConstructionFixture.PersistSaveChangeBundleRequest),
    typeof(ePlayerSaveChangeBundleKey),
    typeof(eSaveFileWriteType),
    out object? emptyInteropPersist,
    out string emptyInteropPersistDetail), "proven IL2CPP empty persist Bundle getter NRE is normalized as absent");
Equal(true, emptyInteropPersist != null, "exact empty persist nullable normalization returns a verified request");
Equal("actualBundle='present=False value=<null>' effectiveBundle='DEFAULT/1(native absent fallback)' actualWriteType='URGENT/0'", emptyInteropPersistDetail, "exact empty persist nullable path reports the native fallback honestly");

foreach (var rejectedPersistFactory in new[]
{
    (Request: typeof(MissingPersistParameterlessConstructorFixture.PersistSaveChangeBundleRequest), Stage: "public parameterless persist request constructor unavailable"),
    (Request: typeof(PrivatePersistParameterlessConstructorFixture.PersistSaveChangeBundleRequest), Stage: "public parameterless persist request constructor unavailable"),
    (Request: typeof(ThrowingPersistParameterlessConstructorFixture.PersistSaveChangeBundleRequest), Stage: "persist request parameterless-constructor-invocation:InvalidOperationException:persist-parameterless-constructor"),
    (Request: typeof(MissingPersistBundleGetterFixture.PersistSaveChangeBundleRequest), Stage: "public persist Bundle getter unavailable"),
    (Request: typeof(PrivatePersistBundleGetterFixture.PersistSaveChangeBundleRequest), Stage: "public persist Bundle getter unavailable"),
    (Request: typeof(ThrowingPersistBundleGetterFixture.PersistSaveChangeBundleRequest), Stage: "persist request bundle-readback-invocation:InvalidOperationException:persist-bundle-get"),
    (Request: typeof(UnrelatedNullPersistBundleGetterFixture.PersistSaveChangeBundleRequest), Stage: "persist request bundle-readback-invocation:NullReferenceException:unrelated-persist-null"),
    (Request: typeof(NullPersistBundleReadbackFixture.PersistSaveChangeBundleRequest), Stage: "persist request bundle readback was null"),
    (Request: typeof(MalformedPersistBundleFixture.PersistSaveChangeBundleRequest), Stage: "public persist Bundle nullable contract unavailable"),
    (Request: typeof(PresentInvalidPersistBundleFixture.PersistSaveChangeBundleRequest), Stage: "persist request bundle readback mismatch:expected=<absent>:actual=INVALID/0"),
    (Request: typeof(PresentDefaultPersistBundleFixture.PersistSaveChangeBundleRequest), Stage: "persist request bundle readback mismatch:expected=<absent>:actual=DEFAULT/1"),
    (Request: typeof(MissingPersistWriteTypeSetterFixture.PersistSaveChangeBundleRequest), Stage: "public persist WriteTypeToRequest getter/setter unavailable"),
    (Request: typeof(PrivatePersistWriteTypeSetterFixture.PersistSaveChangeBundleRequest), Stage: "public persist WriteTypeToRequest getter/setter unavailable"),
    (Request: typeof(ThrowingPersistWriteTypeSetterFixture.PersistSaveChangeBundleRequest), Stage: "persist request write-type-set-invocation:InvalidOperationException:persist-write-set"),
    (Request: typeof(MissingPersistWriteTypeGetterFixture.PersistSaveChangeBundleRequest), Stage: "public persist WriteTypeToRequest getter/setter unavailable"),
    (Request: typeof(PrivatePersistWriteTypeGetterFixture.PersistSaveChangeBundleRequest), Stage: "public persist WriteTypeToRequest getter/setter unavailable"),
    (Request: typeof(ThrowingPersistWriteTypeGetterFixture.PersistSaveChangeBundleRequest), Stage: "persist request write-type-readback-invocation:InvalidOperationException:persist-write-get"),
    (Request: typeof(MismatchedPersistWriteTypeFixture.PersistSaveChangeBundleRequest), Stage: "persist request write-type readback mismatch:expected=URGENT/0:actual=NON_URGENT/1"),
})
{
    Equal(false, CassetteNativeRequestFactory.TryCreateDefaultUrgentPersistRequest(
        rejectedPersistFactory.Request,
        typeof(ePlayerSaveChangeBundleKey),
        typeof(eSaveFileWriteType),
        out object? rejectedPersistRequest,
        out string rejectedPersistDetail), $"{rejectedPersistFactory.Stage} fails closed");
    Equal<object?>(null, rejectedPersistRequest, $"{rejectedPersistFactory.Stage} cannot return a request for submission");
    Equal(rejectedPersistFactory.Stage, rejectedPersistDetail, $"{rejectedPersistFactory.Stage} is reported exactly");
}
Equal(0, RequestSystem.SubmitCount, "persist construction failures never reach RequestSystem.SubmitRequest");
string pluginSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));

Equal(true, pluginSource.Contains("PatchExactMethod(\"SaveDataRequestProcessor\", \"ChangeSelectedPlayerSaveSlot\", \"Int32\", nameof(CassetteSaveTransactionPatches.SelectedSlotMutationPostfix))", StringComparison.Ordinal), "exact selected-slot mutation hook installed");
Equal(true, pluginSource.Contains("PatchExactMethod(\"SaveDataRequestProcessor\", \"CreateNewPlayerSaveFileInEmptySlot\", \"Int32\", nameof(CassetteSaveTransactionPatches.SelectedSlotMutationPostfix))", StringComparison.Ordinal), "exact empty-slot creation hook installed");
Equal(true, pluginSource.Contains("PatchExactMethodWithPrefixAndPostfix(\"SaveDataRequestProcessor\", \"ProcessRequest\", \"BuildPlayerSaveStateFromFileRequest\", nameof(CassetteSaveTransactionPatches.BuildPlayerSaveStatePrefix), nameof(CassetteSaveTransactionPatches.BuiltPlayerSaveStatePostfix))", StringComparison.Ordinal), "exact save-state build hook installs bounded lifecycle prefix and existing authoritative postfix");
Equal(true, pluginSource.Contains("PatchExactMethodPrefix(\"SaveDataRequestProcessor\", \"ProcessRequest\", \"SelectMostRecentlyUsedRegularPlayerSaveSlotRequest\", nameof(CassetteSaveTransactionPatches.MostRecentSelectionPrefix))", StringComparison.Ordinal), "most-recent authority is installed as an exact prefix");
foreach (string obsoleteDiagnostic in new[] { "PublicSelectionDiagnosticPostfix", "SelectedSlotSetterDiagnosticPostfix", "SelectPlayerSaveSlotRequest", "EnsureAPlayerSaveSlotIsSelectedRequest", "EnsurePlayerSaveFileExistsInSelectedSlotRequest" })
    Equal(false, pluginSource.Contains(obsoleteDiagnostic, StringComparison.Ordinal), $"temporary diagnostic removed: {obsoleteDiagnostic}");

static string ExtractClass(string source, string className)
{
    int start = source.IndexOf($"class {className}", StringComparison.Ordinal);
    if (start < 0) throw new InvalidOperationException($"class missing: {className}");
    int bodyStart = source.IndexOf('{', start);
    int depth = 0;
    for (int i = bodyStart; i < source.Length; i++)
    {
        if (source[i] == '{') depth++;
        else if (source[i] == '}' && --depth == 0) return source[start..(i + 1)];
    }
    throw new InvalidOperationException($"class body unbalanced: {className}");
}
Equal(false, pluginSource.Contains("nameof(CassetteSaveTransactionPatches.PersistPrefix)", StringComparison.Ordinal), "absent Persist boundary is not hooked");
Equal(false, pluginSource.Contains("nameof(CassetteSaveTransactionPatches.PersistPostfix)", StringComparison.Ordinal), "absent Persist postfix is not hooked");
Equal(true, pluginSource.Contains("PatchExactMethodWithPrefixAndPostfix(\"SaveDataRequestProcessor\", \"ProcessRequest\", \"PersistSaveChangeBundleRequest\", nameof(CassetteSaveTransactionPatches.PersistBundleRoutingPrefix), nameof(CassetteSaveTransactionPatches.PersistBundleRoutingPostfix))", StringComparison.Ordinal), "exact narrow-persist routing diagnostic is installed");
Equal(true, pluginSource.Contains("PatchExactMethodWithPrefixAndPostfix(\"SaveDataRequestProcessor\", \"ProcessRequest\", \"PersistAllSaveChangeBundlesRequest\", nameof(CassetteSaveTransactionPatches.PersistAllRoutingPrefix), nameof(CassetteSaveTransactionPatches.PersistAllRoutingPostfix))", StringComparison.Ordinal), "exact persist-all routing diagnostic is installed");
Equal(true, pluginSource.Contains("PatchExactMethodWithPrefixAndPostfix(\"SaveDataRequestProcessor\", \"ProcessRequest\", \"DiscardAllUnstagedSaveStateChangesRequest\", nameof(CassetteSaveTransactionPatches.DiscardAllRoutingPrefix), nameof(CassetteSaveTransactionPatches.DiscardAllRoutingPostfix))", StringComparison.Ordinal), "exact discard-all-unstaged routing diagnostic is installed");

Equal(false, pluginSource.Contains("nameof(CassetteSaveTransactionPatches.SaveSelectionPostfix)", StringComparison.Ordinal), "broad selection postfix is non-authoritative");
string selectedMutationPostfix = ExtractMethods(pluginSource, "public static void SelectedSlotMutationPostfix(").Single();
Equal(true, selectedMutationPostfix.Contains("QueueSaveBoundarySignal", StringComparison.Ordinal), "slot mutation callback only queues boundary signal");
Equal(true, selectedMutationPostfix.Contains("LogExtractionFailureOnce", StringComparison.Ordinal), "slot mutation extraction failure is logged once");
Equal(true, selectedMutationPostfix.Contains("__originalMethod?.DeclaringType?.Name", StringComparison.Ordinal), "slot mutation diagnostic includes safe method owner identity");
Equal(false, selectedMutationPostfix.Contains("TryGetLoadedSave", StringComparison.Ordinal), "slot mutation callback performs no native enquiry");
Equal(false, selectedMutationPostfix.Contains("ActivateLoadedSave", StringComparison.Ordinal), "slot mutation callback cannot activate epoch");
string buildPostfix = ExtractMethods(pluginSource, "public static void BuiltPlayerSaveStatePostfix(").Single();
Equal(true, buildPostfix.Contains("QueueSaveBoundarySignal", StringComparison.Ordinal), "build callback only queues boundary signal");
Equal(true, buildPostfix.Contains("LogExtractionFailureOnce", StringComparison.Ordinal), "build extraction failure is logged once");
Equal(true, buildPostfix.Contains("BuildPlayerSaveStateFromFileRequest", StringComparison.Ordinal), "build diagnostic identifies exact request type safely");
Equal(false, buildPostfix.Contains("TryGetLoadedSave", StringComparison.Ordinal), "build callback performs no native enquiry");
Equal(true, buildPostfix.Contains("CompleteSaveStateLifecycleDiagnostic", StringComparison.Ordinal), "build postfix completes the exact bounded lifecycle diagnostic pair");
Equal(true, buildPostfix.Contains("try", StringComparison.Ordinal) && buildPostfix.Contains("catch", StringComparison.Ordinal), "diagnostic completion is exception-isolated from the authoritative build boundary");
Equal(true, buildPostfix.LastIndexOf("QueueSaveBoundarySignal", StringComparison.Ordinal) > buildPostfix.LastIndexOf("catch", StringComparison.Ordinal), "authoritative build boundary executes after isolated diagnostics");
string buildPrefix = ExtractMethods(pluginSource, "public static void BuildPlayerSaveStatePrefix(").Single();
Equal(true, buildPrefix.Contains("BeginSaveStateLifecycleDiagnostic", StringComparison.Ordinal), "build prefix captures the bounded before snapshot");
Equal(true, buildPrefix.Contains("BindingFlags.Public", StringComparison.Ordinal), "new lifecycle prefix reads only the public SlotNumber contract");
Equal(false, buildPrefix.Contains("ReflectionUtil.ReadInt", StringComparison.Ordinal), "new lifecycle prefix cannot bind private slot members");
Equal(true, buildPrefix.Contains("try", StringComparison.Ordinal) && buildPrefix.Contains("catch", StringComparison.Ordinal), "diagnostic prefix cannot suppress the original build request");
string extractionDiagnostic = ExtractMethods(pluginSource, "private static void LogExtractionFailureOnce(").Single();
Equal(true, extractionDiagnostic.Contains("ExtractionFailures.Add", StringComparison.Ordinal), "extraction diagnostics are bounded by a one-time key set");
Equal(false, extractionDiagnostic.Contains("ReflectionUtil.ReadMember", StringComparison.Ordinal), "diagnostic logger performs no unsafe object traversal");
string recordRoutingPrefix = ExtractMethods(pluginSource, "public static bool CassetteStatusRequestPrefix(").Single();
Equal(true, recordRoutingPrefix.Contains("BeginBundleRoutingDiagnostic", StringComparison.Ordinal), "record-song processor entry captures actual request routing diagnostic");
Equal(true, pluginSource.Contains("public static void CassetteStatusRequestPostfix(", StringComparison.Ordinal), "record-song processor exit callback is compiled into the patch seam");
foreach (string callback in new[] { "PersistBundleRoutingPrefix", "PersistAllRoutingPrefix", "DiscardAllRoutingPrefix" })
{
    string callbackSource = ExtractMethods(pluginSource, $"public static void {callback}(").Single();
    Equal(true, callbackSource.Contains("BeginBundleRoutingDiagnostic", StringComparison.Ordinal), $"{callback} captures a bounded entry diagnostic");
    Equal(true, callbackSource.Contains("LogRoutingExtractionFailureSafely", StringComparison.Ordinal), $"{callback} exception fallback cannot escape into the native prefix");
    Equal(false, callbackSource.Contains("GetBaseException", StringComparison.Ordinal), $"{callback} performs no throwable exception formatting outside the safe fallback");
    Equal(false, callbackSource.Contains("TrySubmit", StringComparison.Ordinal), $"{callback} cannot submit save work");
}
foreach (string callback in new[] { "PersistBundleRoutingPostfix", "PersistAllRoutingPostfix", "DiscardAllRoutingPostfix" })
{
    string callbackSource = ExtractMethods(pluginSource, $"public static void {callback}(").Single();
    Equal(true, callbackSource.Contains("CompleteBundleRoutingDiagnostic", StringComparison.Ordinal), $"{callback} captures the correlated exit diagnostic");
    Equal(true, callbackSource.Contains("LogRoutingExtractionFailureSafely", StringComparison.Ordinal), $"{callback} exception fallback cannot escape from the native postfix");
    Equal(false, callbackSource.Contains("GetBaseException", StringComparison.Ordinal), $"{callback} performs no throwable exception formatting outside the safe fallback");
    Equal(false, callbackSource.Contains("TrySubmit", StringComparison.Ordinal), $"{callback} cannot submit save work");
}
string routingFallbackLogger = ExtractMethods(pluginSource, "private static void LogRoutingExtractionFailureSafely(").Single();
Equal(true, routingFallbackLogger.Contains("try", StringComparison.Ordinal) && routingFallbackLogger.Contains("catch", StringComparison.Ordinal), "routing callback fallback logger is strictly no-throw");
Equal(true, routingFallbackLogger.Contains("Exception exception", StringComparison.Ordinal) && routingFallbackLogger.Contains("GetBaseException", StringComparison.Ordinal), "routing callback fallback performs exception formatting only inside its no-throw boundary");
string beginBundleRouting = ExtractMethods(ExtractClass(pluginSource, "CassetteReceiptRandomization"), "internal static void BeginBundleRoutingDiagnostic(").Single();
Equal(true, beginBundleRouting.Contains("_slotDataSynchronized", StringComparison.Ordinal) && beginBundleRouting.Contains("Enabled", StringComparison.Ordinal), "routing diagnostics are inactive before compatible AP slot synchronization");
Equal(true, beginBundleRouting.Contains("TryResolveGlobalAttempt", StringComparison.Ordinal), "global bundle diagnostics require a relevant AP candidate or real active attempt");
Equal(true, beginBundleRouting.Contains("RegisterRelevantSong", StringComparison.Ordinal), "record request songs remain available to nested global snapshots before verification");
Equal(true, beginBundleRouting.Contains("GetRelevantSongs", StringComparison.Ordinal), "every boundary unions retained record candidates into its coherent status snapshot");
string mostRecentPrefix = ExtractMethods(pluginSource, "public static void MostRecentSelectionPrefix(").Single();
Equal(true, mostRecentPrefix.Contains("BeginMostRecentSelectionBoundary(__instance)", StringComparison.Ordinal), "most-recent prefix immediately enters unresolved authority boundary");
string beginMostRecentBoundary = ExtractMethods(pluginSource, "internal static void BeginMostRecentSelectionBoundary(").Single();
int suspendIndex = beginMostRecentBoundary.IndexOf("_saveIdentity.SuspendUnresolved()", StringComparison.Ordinal);
Equal(true, suspendIndex >= 0, "MostRecent immediately suspends prior epoch without inventing a slot");
Equal(true, suspendIndex < beginMostRecentBoundary.IndexOf("saveDataProcessor == null", StringComparison.Ordinal), "null processor validation occurs only after prior epoch suspension");
Equal(true, suspendIndex < beginMostRecentBoundary.IndexOf("saveDataProcessor.GetType().Name", StringComparison.Ordinal), "wrong processor validation occurs only after prior epoch suspension");
Equal(true, beginMostRecentBoundary.Contains("_unityReconciliationRequested = false", StringComparison.Ordinal), "MostRecent clears prior reconciliation intent");
Equal(true, beginMostRecentBoundary.Contains("_mostRecentQueueLogDeduper.ShouldLog(stage)", StringComparison.Ordinal), "identical most-recent queue outcomes are deduplicated");
foreach (string prohibitedCall in new[] { "_saveIdentity.SignalSelection", "ActivateLoadedSave", "DeactivateLoadedSave", "TryGetLoadedSave", "TryReconcile", "TrySubmitHaveInBag" })
    Equal(false, beginMostRecentBoundary.Contains(prohibitedCall, StringComparison.Ordinal), $"most-recent boundary entry forbids semantic call {prohibitedCall}");
string activateLoadedSave = ExtractMethods(pluginSource, "internal static void ActivateLoadedSave(").Single();
Equal(false, activateLoadedSave.Contains("if (!_slotDataSynchronized || !Enabled) return", StringComparison.Ordinal), "save selection establishes its epoch even before AP slot data arrives");

string reconcileSource = ExtractMethods(pluginSource, "internal static void TryReconcile(").Single();
Equal(true, reconcileSource.Contains("TryReconcileSong(song", StringComparison.Ordinal), "reconciliation delegates each epoch-pending song");
string reconcileSongSource = ExtractMethods(pluginSource, "private static void TryReconcileSong(").Single();
Equal(true, reconcileSongSource.Contains("TryReadCassetteStatus(processor!, nativeSong", StringComparison.Ordinal), "reconciliation reads before writing");
Equal(true, reconcileSongSource.Contains("TrySubmitHaveInBag(processor!, nativeSong", StringComparison.Ordinal), "reconciliation uses exact semantic request adapter");
Equal(false, reconcileSongSource.Contains("HAVE_DEPOSITED", StringComparison.Ordinal), "reconciliation never submits deposited status");

string cassetteProcessorCapture = ExtractMethods(pluginSource, "internal static void CapturePlayerSaveRequestProcessor(object? instance, bool reconcileNow = true)").Single();
Equal(true, cassetteProcessorCapture.Contains("CASSETTE PLAYER PROCESSOR CAPTURED", StringComparison.Ordinal), "compatible player processor capture is observable");
Equal(false, cassetteProcessorCapture.Contains("TrySubmitHaveInBag", StringComparison.Ordinal), "processor diagnostic performs no native cassette write");
string cassetteProcessorFallback = ExtractMethods(pluginSource, "private static bool EnsureCassetteProcessorAvailable(").Single();
Equal(true, cassetteProcessorFallback.Contains("CassetteSaveTransactionAdapter.TryGetLoadedSave(out _)", StringComparison.Ordinal), "stateless processor fallback requires readable selected save");
Equal(true, cassetteProcessorFallback.Contains("PlayerSaveRequestProcessor", StringComparison.Ordinal), "stateless fallback constructs only the proven processor type");

string receiptRandomizationSource = ExtractClass(pluginSource, "CassetteReceiptRandomization");
string receiptApplySlotData = ExtractMethods(receiptRandomizationSource, "internal static void ApplySlotData(").Single();
Equal(true, receiptApplySlotData.Contains("RequestUnityReconciliation(\"slot data synchronized\")", StringComparison.Ordinal), "slot-data synchronization queues Unity-thread work");
Equal(false, receiptApplySlotData.Contains("TryReconcile(", StringComparison.Ordinal), "slot-data synchronization performs no native reconciliation");
string receiptTryApply = ExtractMethods(receiptRandomizationSource, "internal static bool TryApplyItem(").Single();
Equal(true, receiptTryApply.Contains("RequestUnityReconciliation(\"AP cassette receipt\")", StringComparison.Ordinal), "active-save AP receipt schedules prompt Unity-thread reconciliation");
Equal(false, receiptTryApply.Contains("TryReconcile(", StringComparison.Ordinal), "network receipt performs no native reconciliation");
string unityTick = ExtractMethods(receiptRandomizationSource, "internal static void TickUnity(").Single();
Equal(true, unityTick.Contains("_runtime.Tick(elapsed)", StringComparison.Ordinal), "Unity keeper advances bounded verification timers with actual elapsed time");
Equal(true, unityTick.IndexOf("if (!_saveSynchronizationReady) return", StringComparison.Ordinal) < unityTick.IndexOf("_runtime.Tick(elapsed)", StringComparison.Ordinal), "deferred synchronization freezes verification and disk-attempt timers until an observation proves readiness");
Equal(true, unityTick.Contains("TickDiskCommit(elapsed)", StringComparison.Ordinal), "Unity keeper advances bounded disk completion on the same monotonic elapsed time");
Equal(true, unityTick.Contains("TryReconcile(reason)", StringComparison.Ordinal), "Unity keeper drains queued reconciliation intent");
Equal(true, unityTick.Contains("TryGetProcessorSaveIdentity", StringComparison.Ordinal), "Unity keeper observes the processor-local save-state pointer");
Equal(true, unityTick.Contains("TryMatchRegularSaveSlot", StringComparison.Ordinal), "Unity keeper performs bounded public regular-save pointer join");
Equal(true, unityTick.Contains("_saveIdentity.Signal(joinedSlot, CassetteSaveBoundarySignalKind.Selection, joinSnapshot.PlayerSaveProcessor)", StringComparison.Ordinal), "only unique join resolves real UI slot into processor stabilizer");
Equal(true, unityTick.Contains("awaiting two stable processor observations", StringComparison.Ordinal), "join cannot activate before processor pointer confirmation");
Equal(true, unityTick.Contains("_mostRecentResultLogDeduper.ShouldLog(resultSignature)", StringComparison.Ordinal), "identical pointer-join results are deduplicated");
string joinBlock = unityTick[..unityTick.IndexOf("CassetteSaveActivation? activation", StringComparison.Ordinal)];
foreach (string prohibitedCall in new[] { "ActivateLoadedSave", "TryReconcile", "TrySubmitHaveInBag" })
    Equal(false, joinBlock.Contains(prohibitedCall, StringComparison.Ordinal), $"pointer join cannot directly perform semantic call {prohibitedCall}");
Equal(false, unityTick.Contains("TryGetLoadedSaveIdentity", StringComparison.Ordinal), "Unity keeper never authorizes from stale static selected-save enquiry");
Equal(true, unityTick.Contains("_saveIdentity.Observe", StringComparison.Ordinal), "Unity keeper feeds processor identity into authoritative stabilizer");
Equal(true, unityTick.Contains("LogIdentityDiagnosticOnChange", StringComparison.Ordinal), "Unity keeper logs identity stabilization only on change");
Equal(true, unityTick.Contains("identitySnapshot.ExpectedSlot", StringComparison.Ordinal) && unityTick.Contains("selectedStatePointer", StringComparison.Ordinal), "Unity diagnostic includes exact UI slot and processor-local pointer");
string queueBoundary = ExtractMethods(receiptRandomizationSource, "internal static void QueueSaveBoundarySignal(").Single();
Equal(true, queueBoundary.Contains("_saveIdentity.Signal(expectedSlot, kind, processor)", StringComparison.Ordinal), "every exact boundary kind records an authoritative managed signal");
Equal(true, queueBoundary.Contains("_regularSavePointerJoinProbe.Cancel", StringComparison.Ordinal), "every exact boundary supersedes pending MostRecent generation");
Equal(true, queueBoundary.Contains("kind is CassetteSaveBoundarySignalKind.Selection", StringComparison.Ordinal), "a new Selection clears retained target diagnostics while Creation and Build preserve the joined SaveData processor identity");
Equal(false, queueBoundary.Contains("if (kind == CassetteSaveBoundarySignalKind.Selection)", StringComparison.Ordinal), "Creation and Build cannot bypass stabilizer signaling");
Equal(true, queueBoundary.Contains("_unityReconciliationRequested = false", StringComparison.Ordinal), "exact boundary callback suspends prior reconciliation immediately");
Equal(true, queueBoundary.Contains("IsCompatiblePlayerSaveRequestProcessor(_playerSaveRequestProcessor)", StringComparison.Ordinal), "selection binds only a compatible preexisting processor");
string captureProcessor = ExtractMethods(receiptRandomizationSource, "internal static void CapturePlayerSaveRequestProcessor(").Single();
Equal(true, captureProcessor.Contains("_saveIdentity.CaptureProcessor", StringComparison.Ordinal), "compatible post-selection processor can bind pending selection");
Equal(false, queueBoundary.Contains("TryGetLoadedSave", StringComparison.Ordinal), "boundary callback performs no native selected-save read");
string diskCommitSource = ExtractMethods(receiptRandomizationSource, "private static void TickDiskCommit(").Single();
Equal(true, reconcileSongSource.Contains("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal), "cassette reconciliation requires the public save synchronization gate");
Equal(true, reconcileSongSource.IndexOf("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal) < reconcileSongSource.IndexOf("TryReadCassetteStatus", StringComparison.Ordinal), "synchronization is proven before reconciliation reads or mutates cassette status");
Equal(true, reconcileSongSource.IndexOf("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal) < reconcileSongSource.IndexOf("TrySubmitHaveInBag", StringComparison.Ordinal), "synchronization is proven before any cassette grant request");
string synchronizationGateSource = ExtractMethods(receiptRandomizationSource, "private static bool TryConfirmSaveSynchronizationReady(").Single();
Equal(true, synchronizationGateSource.Contains("LogSaveSynchronizationObservationDiagnostic", StringComparison.Ordinal), "deferred lifecycle observation emits the bounded public comparison diagnostic");
Equal(true, synchronizationGateSource.Contains("allowObservationProbe", StringComparison.Ordinal), "frame-only paths cannot trigger new diagnostic probes");
Equal(false, synchronizationGateSource.Contains("TrySubmit", StringComparison.Ordinal), "synchronization diagnostic wiring cannot submit grant or persistence requests");
Equal(true, diskCommitSource.Contains("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal), "disk commit work requires the same public save synchronization gate");
Equal(true, diskCommitSource.IndexOf("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal) < diskCommitSource.IndexOf("TryReadPublicWriteState", StringComparison.Ordinal), "synchronization is proven before a disk-commit attempt can be prepared or consumed");
Equal(true, diskCommitSource.Contains("TrySubmitDefaultUrgentPersist", StringComparison.Ordinal), "verified cassette wave submits the narrow public persist transaction");
Equal(true, diskCommitSource.Contains("TryReadPublicWriteState", StringComparison.Ordinal), "disk transaction polls public write completion state");
Equal(true, diskCommitSource.Contains("ObserveUnavailable", StringComparison.Ordinal), "unreadable public write state still advances bounded fail-closed completion");
foreach (string diagnosticPhase in new[] { "CassetteDiskCommitDiagnosticPhase.Pre", "CassetteDiskCommitDiagnosticPhase.Post", "CassetteDiskCommitDiagnosticPhase.StillPending", "CassetteDiskCommitDiagnosticPhase.HardTimeout" })
    Equal(true, diskCommitSource.Contains(diagnosticPhase, StringComparison.Ordinal), $"disk transaction wires bounded diagnostic phase {diagnosticPhase}");
Equal(true, diskCommitSource.Contains("CassetteDiskCommitDiagnosticPhase.Failure", StringComparison.Ordinal), "disk transaction emits a bounded terminal failure snapshot");
Equal(true, diskCommitSource.Contains("failureKind", StringComparison.Ordinal), "terminal failure snapshot carries its exact classified cause");
Equal(true, diskCommitSource.Contains("postFailureKind", StringComparison.Ordinal), "post-submit baseline rejection retains its precise diagnostic cause");
Equal(true, diskCommitSource.Contains("MarkSubmissionIndeterminate(preparedAttempt, postFailureKind)", StringComparison.Ordinal), "post-submit terminal transition preserves the selected precise failure kind");
int preTargetPhase = diskCommitSource.IndexOf("CassetteDiskCommitDiagnosticPhase.PreTarget", StringComparison.Ordinal);
int persistSubmission = diskCommitSource.IndexOf("TrySubmitDefaultUrgentPersist", StringComparison.Ordinal);
int postTargetPhase = diskCommitSource.IndexOf("CassetteDiskCommitDiagnosticPhase.PostTarget", StringComparison.Ordinal);
Equal(true, preTargetPhase >= 0 && preTargetPhase < persistSubmission, "PRE_TARGET snapshot is captured immediately before the existing persist submission in the same Unity update");
Equal(true, postTargetPhase > persistSubmission, "POST_TARGET snapshot is captured immediately after the existing persist submission in the same Unity update");
Equal(true, diskCommitSource.Contains("LogDiskCommitTargetDiagnostic", StringComparison.Ordinal), "target snapshots use the behavior-neutral public target logger");
foreach (string prohibited in new[] { "PersistAllSaveChangeBundlesRequest", "TriggerUrgentSaveWriteIfAnyChangesRequest", "RequestWriteForPlayerSave", "SaveDataManager", "WritePlayerSaveFile" })
    Equal(false, diskCommitSource.Contains(prohibited, StringComparison.Ordinal), $"disk transaction avoids prohibited broad/private path {prohibited}");
int writeEventPatch = pluginSource.IndexOf("\"HandleEvent\", \"PlayerSaveWriteCompletedEvent\"", StringComparison.Ordinal);
int receiptConfigure = pluginSource.IndexOf("CassetteReceiptRandomization.Configure();", StringComparison.Ordinal);
Equal(true, writeEventPatch >= 0, "public player-save write completion event is subscribed through the established event-handler hook");
Equal(true, writeEventPatch < receiptConfigure, "write completion subscription is installed before cassette transactions can be configured or submitted");
string writeEventPostfix = ExtractMethods(pluginSource, "public static void PlayerSaveWriteCompletedEventPostfix(").Single();
Equal(true, writeEventPostfix.Contains("OnPlayerSaveWriteCompletedEvent", StringComparison.Ordinal), "public write-completed event is routed to the cassette transaction boundary");
string writeEventConsumer = ExtractMethods(receiptRandomizationSource, "internal static void OnPlayerSaveWriteCompletedEvent(").Single();
Equal(true, writeEventConsumer.Contains("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal), "write-completion handling requires the same public save synchronization gate");
Equal(true, writeEventConsumer.IndexOf("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal) < writeEventConsumer.IndexOf("ObserveWriteCompletedEvent", StringComparison.Ordinal), "synchronization is proven before an active disk-commit attempt consumes an event");
Equal(true, writeEventConsumer.Contains("ObserveWriteCompletedEvent", StringComparison.Ordinal), "write event is correlated by the transaction runtime");
Equal(true, writeEventConsumer.Contains("TryClaimLateEvent", StringComparison.Ordinal), "inactive terminal attempt admits at most one header-only late-event record");
Equal(true, writeEventConsumer.Contains("CASSETTE DISK COMMIT LATE_EVENT", StringComparison.Ordinal), "late-event record is explicitly labeled");
Equal(true, writeEventConsumer.Contains("CassetteDiskCommitDiagnosticPhase.Failure", StringComparison.Ordinal), "failed completion event emits the bounded terminal FAILURE snapshot");
Equal(true, writeEventConsumer.Contains("TickDiskCommit(TimeSpan.Zero)", StringComparison.Ordinal), "successful write event wakes immediate public-state verification");
Equal(false, writeEventConsumer.Contains("CassetteDiskCommitOutcome.Success", StringComparison.Ordinal), "event callback cannot declare disk success by itself");
Equal(true, writeEventConsumer.IndexOf("TryReadPlayerSaveWriteCompletedEventHeader", StringComparison.Ordinal) < writeEventConsumer.IndexOf("ObserveWriteCompletedEvent", StringComparison.Ordinal), "compiled callback decodes mandatory event header before correlation");
Equal(true, writeEventConsumer.IndexOf("ObserveWriteCompletedEvent", StringComparison.Ordinal) < writeEventConsumer.IndexOf("TryReadPlayerSaveWriteCompletedEventFailureReason", StringComparison.Ordinal), "compiled callback reads optional failure reason only after correlation");
Equal(true, writeEventConsumer.Contains("CassetteDiskCommitDiagnosticPhase.PreparedEventIgnored", StringComparison.Ordinal), "prepared-phase event receives one behavior-neutral ignored-event record");
Equal(true, writeEventConsumer.Contains("CassetteDiskCommitDiagnosticPhase.Event", StringComparison.Ordinal), "accepted event receives one bounded state snapshot");
string diskDiagnosticLogger = ExtractMethods(receiptRandomizationSource, "private static void LogDiskCommitDiagnostic(").Single();
foreach (string requiredField in new[] { "attempt=", "phase=", "eventOrdinal=", "elapsedSeconds=", "generation=", "epoch=", "slot=", "pointer=", "successTime=", "failureTime=", "currentGameTime=", "HasChanges=", "RequiresWriteToDisk=", "HasUnstagedChanges=", "redundancyIndex=", "redundancyRevision=", "statuses=", "activeSongs=", "queuedSongs=" })
    Equal(true, diskDiagnosticLogger.Contains(requiredField, StringComparison.Ordinal), $"disk diagnostic snapshot includes {requiredField}");
Equal(true, diskDiagnosticLogger.Contains("TryReadPublicWriteDiagnosticState", StringComparison.Ordinal), "disk diagnostic snapshot reads public write and GameTime state");
Equal(true, diskDiagnosticLogger.Contains("context.Attempt.Pointer", StringComparison.Ordinal), "disk diagnostic snapshot verifies the exact attempt pointer");
Equal(true, diskDiagnosticLogger.Contains("transactionBaseline", StringComparison.Ordinal), "disk diagnostic snapshot preserves the actual transaction baseline separately from later observation");
Equal(true, diskDiagnosticLogger.Contains("failureKind", StringComparison.Ordinal), "terminal snapshot prints classified failure kind and detail");
string diskTargetDiagnosticLogger = ExtractMethods(receiptRandomizationSource, "private static void LogDiskCommitTargetDiagnostic(").Single();
Equal(true, diskTargetDiagnosticLogger.Contains("TryReadDiskCommitTargetDiagnostic", StringComparison.Ordinal), "target diagnostic logger uses only the bounded public target reader");
Equal(true, diskTargetDiagnosticLogger.IndexOf("try", StringComparison.Ordinal) < diskTargetDiagnosticLogger.IndexOf("TryReadDiskCommitTargetDiagnostic", StringComparison.Ordinal), "target diagnostic establishes a no-throw boundary before any observational read or formatting");
Equal(true, diskTargetDiagnosticLogger.LastIndexOf("catch", StringComparison.Ordinal) > diskTargetDiagnosticLogger.IndexOf("LogWarning", StringComparison.Ordinal), "target diagnostic catches failures through the final logger call so PRE/POST control flow always continues");
foreach (string requiredField in new[] { "registryCount=", "playerProcessorPointer=", "registeredPersistProcessorPointer=", "retainedSaveDataProcessorPointer=", "saveDataStatePointer=", "selectedSlot=", "selectedEntryPointer=", "expectedSlotEntryPointer=", "effectiveStatuses=", "canonicalStatuses=", "defaultBundlePresent=", "defaultBundleChangeCount=" })
    Equal(true, diskTargetDiagnosticLogger.Contains(requiredField, StringComparison.Ordinal), $"target diagnostic snapshot includes {requiredField}");
foreach (string availabilityFlag in new[] { "HasPlayerProcessorPointer", "HasRegisteredPersistProcessorPointer", "HasRetainedSaveDataProcessorPointer", "HasSaveDataStatePointer", "HasSelectedPlayerSaveSlot", "HasSelectedEntryPointer", "HasExpectedSlotEntryPointer", "HasPlayer", "HasSelected" })
    Equal(true, diskTargetDiagnosticLogger.Contains($"state.{availabilityFlag}", StringComparison.Ordinal), $"target diagnostic formats {availabilityFlag} independently of final readability");
foreach (string formerlyGlobalGate in new[] { "playerProcessorPointer = readable ?", "registeredPersistProcessorPointer = readable ?", "retainedSaveDataProcessorPointer = readable ?", "saveDataStatePointer = readable ?", "selectedSlot = readable ?", "playerEffectiveStatuses = readable", "selectedEffectiveStatuses = readable" })
    Equal(false, diskTargetDiagnosticLogger.Contains(formerlyGlobalGate, StringComparison.Ordinal), $"target diagnostic no longer hides partial values behind {formerlyGlobalGate}");
foreach (string baselineField in new[] { "baselineSuccessTime=", "baselineFailureTime=", "baselineHasChanges=", "baselineRequiresWriteToDisk=" })
    Equal(true, diskDiagnosticLogger.Contains(baselineField, StringComparison.Ordinal), $"disk diagnostic snapshot includes exact transaction {baselineField}");
string lifecycleDiagnosticLogger = ExtractMethods(receiptRandomizationSource, "private static void LogSaveStateLifecycleDiagnostic(").Single();
foreach (string requiredField in new[] { "StatePointer", "RedundancyBundleIndex", "RedundancyBundleRevision", "LastSuccessTime", "LastFailureTime", "Statuses" })
    Equal(true, lifecycleDiagnosticLogger.Contains(requiredField, StringComparison.Ordinal), $"save lifecycle snapshot includes {requiredField}");
Equal(true, lifecycleDiagnosticLogger.Contains("TryReadPublicWriteLifecycleDiagnosticState", StringComparison.Ordinal), "save lifecycle snapshot reads one public state object without imposing the old pointer");
Equal(false, lifecycleDiagnosticLogger.Contains("TrySubmit", StringComparison.Ordinal), "save lifecycle diagnostic cannot submit persistence or reconciliation requests");
string keeperSource = ExtractClass(pluginSource, "CassetteReceiptReconciliationKeeper");
Equal(true, keeperSource.Contains("Stopwatch.GetTimestamp()", StringComparison.Ordinal), "keeper uses a monotonic production clock");
Equal(true, keeperSource.Contains("CassetteReceiptRandomization.TickUnity(elapsed)", StringComparison.Ordinal), "keeper passes actual elapsed time every Unity update");
Equal(false, keeperSource.Contains("TimeSpan.FromSeconds(1)", StringComparison.Ordinal), "keeper does not substitute a frame-count interval for elapsed time");
foreach (string obsolete in new[] { "CassetteReceiptRuntime", "CassetteReceiptScheduler", "Level2MoneyCassetteRuntime", "_terminalSongs" })
    Equal(false, pluginSource.Contains(obsolete, StringComparison.Ordinal) || File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "CassetteRandomizationPolicy.cs")).Contains(obsolete, StringComparison.Ordinal), $"obsolete process-wide cassette state removed: {obsolete}");
Equal(false, pluginSource.Contains("PatchMethodsByParameter(\n                \"HandleEvent\",\n                \"SelectedPlayerSaveSlotChangedEvent\"", StringComparison.Ordinal), "unsafe selected-save event hook remains absent");
Console.WriteLine("PASS: safe_save_lifecycle_production_wiring");

var processorStabilizer = new CassetteProcessorSaveIdentityStabilizer();
long unresolvedGeneration = processorStabilizer.SuspendUnresolved();
Equal(true, processorStabilizer.Capture().Pending, "MostRecent immediately suspends prior epoch");
Equal<int?>(null, processorStabilizer.Capture().ExpectedSlot, "MostRecent starts without trusting a UI slot");
Equal(false, processorStabilizer.CaptureProcessor(new object()), "unresolved boundary cannot bind player processor before unique join");
foreach (string invalidEntry in new[] { "processor-null", "processor-wrong-type", "processor-extraction-failed" })
{
    var priorEpoch = new CassetteSaveEpochRuntime();
    priorEpoch.ActivateSave(4);
    var invalidBoundary = new CassetteProcessorSaveIdentityStabilizer();
    invalidBoundary.Signal(4, CassetteSaveBoundarySignalKind.Selection, new object());
    invalidBoundary.Observe(invalidBoundary.Capture().Generation, invalidBoundary.Capture().Processor, true, 400, "success");
    invalidBoundary.Observe(invalidBoundary.Capture().Generation, invalidBoundary.Capture().Processor, true, 400, "success");
    invalidBoundary.SuspendUnresolved();
    Equal(true, priorEpoch.HasActiveSave, $"{invalidEntry} retains prior epoch data for later safe replacement");
    Equal(true, invalidBoundary.Capture().Pending, $"{invalidEntry} suspends all old-epoch native work");
    Equal<int?>(null, invalidBoundary.Capture().ExpectedSlot, $"{invalidEntry} cannot leave old slot authoritative");
}
object slot3Processor = new();
long slot3Generation = processorStabilizer.Signal(3, CassetteSaveBoundarySignalKind.Selection, slot3Processor);
var processorBoundarySnapshot = processorStabilizer.Capture();
Equal(true, processorBoundarySnapshot.Pending, "selection suspends prior epoch");
Equal(3, processorBoundarySnapshot.ExpectedSlot, "selection binds exact UI slot");
Equal(true, ReferenceEquals(slot3Processor, processorBoundarySnapshot.Processor), "preexisting processor binds at selection");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(slot3Generation, slot3Processor, readable: true, pointer: 300, "success"), "first processor-local pointer observation cannot activate");
var slot3Activation = processorStabilizer.Observe(slot3Generation, slot3Processor, readable: true, pointer: 300, "success");
Equal(3, slot3Activation!.Value.Slot, "slot 3 activates after two matching processor-local observations");
Equal(300L, slot3Activation.Value.Pointer, "slot 3 activation uses processor-local state pointer");

object slot4Processor = new();
long slot4Generation = processorStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection, null);
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(slot4Generation, null, readable: false, pointer: 0, "processor-missing"), "missing processor remains suspended");
Equal(true, processorStabilizer.CaptureProcessor(slot4Processor), "later compatible processor capture binds pending selection");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(slot4Generation, slot4Processor, readable: true, pointer: 400, "success"), "post-selection processor first observation cannot activate");
var slot4Activation = processorStabilizer.Observe(slot4Generation, slot4Processor, readable: true, pointer: 400, "success");
Equal(4, slot4Activation!.Value.Slot, "slot 4 creates a distinct epoch");
Equal(400L, slot4Activation.Value.Pointer, "slot 4 uses distinct processor-local pointer");

long pointerChangeGeneration = processorStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection, slot4Processor);
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(pointerChangeGeneration, slot4Processor, true, 401, "success"), "pointer candidate starts with first observation");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(pointerChangeGeneration, slot4Processor, true, 402, "success"), "pointer change resets stability");
var pointerChangeActivation = processorStabilizer.Observe(pointerChangeGeneration, slot4Processor, true, 402, "success");
Equal(402L, pointerChangeActivation!.Value.Pointer, "only repeated replacement pointer activates");

long staleGeneration = processorStabilizer.Signal(3, CassetteSaveBoundarySignalKind.Selection, slot3Processor);
long currentGeneration = processorStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection, slot4Processor);
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(staleGeneration, slot3Processor, true, 333, "success"), "stale generation cannot activate");
Equal(true, processorStabilizer.Capture().Pending, "stale observation leaves current selection suspended");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(currentGeneration, new object(), true, 444, "success"), "wrong processor cannot activate");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(currentGeneration, slot4Processor, false, 0, "obtain-state-failed"), "failed processor read remains suspended");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(currentGeneration, slot4Processor, true, 444, "success"), "successful read after failures still requires two observations");
Equal(4, processorStabilizer.Observe(currentGeneration, slot4Processor, true, 444, "success")!.Value.Slot, "current generation eventually activates");

long failedInterruptionGeneration = processorStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection, slot4Processor);
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(failedInterruptionGeneration, slot4Processor, true, 450, "success"), "failed-interruption sequence records first sample");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(failedInterruptionGeneration, slot4Processor, false, 0, "obtain-state-failed"), "failed read interrupts confirmation");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(failedInterruptionGeneration, slot4Processor, true, 450, "success"), "sample after failed read is a new first sample");
Equal(4, processorStabilizer.Observe(failedInterruptionGeneration, slot4Processor, true, 450, "success")!.Value.Slot, "two consecutive samples after failed read activate");

long wrongProcessorGeneration = processorStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection, slot4Processor);
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(wrongProcessorGeneration, slot4Processor, true, 460, "success"), "wrong-processor sequence records first sample");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(wrongProcessorGeneration, new object(), true, 460, "success"), "wrong processor interrupts confirmation");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(wrongProcessorGeneration, slot4Processor, true, 460, "success"), "sample after wrong processor is a new first sample");
Equal(4, processorStabilizer.Observe(wrongProcessorGeneration, slot4Processor, true, 460, "success")!.Value.Slot, "two consecutive samples after wrong processor activate");

long oldInterruptionGeneration = processorStabilizer.Signal(3, CassetteSaveBoundarySignalKind.Selection, slot3Processor);
long staleInterruptionGeneration = processorStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection, slot4Processor);
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(staleInterruptionGeneration, slot4Processor, true, 470, "success"), "stale-generation sequence records first sample");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(oldInterruptionGeneration, slot3Processor, true, 370, "success"), "stale generation interrupts current confirmation");
Equal<CassetteSaveActivation?>(null, processorStabilizer.Observe(staleInterruptionGeneration, slot4Processor, true, 470, "success"), "sample after stale generation is a new first sample");
Equal(4, processorStabilizer.Observe(staleInterruptionGeneration, slot4Processor, true, 470, "success")!.Value.Slot, "two consecutive samples after stale generation activate");
Console.WriteLine("PASS: processor_local_save_identity_stabilization");

var creationStabilizer = new CassetteProcessorSaveIdentityStabilizer();
object freshProcessor = new();
long mostRecentGeneration = creationStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection, freshProcessor);
Equal<CassetteSaveActivation?>(null, creationStabilizer.Observe(mostRecentGeneration, freshProcessor, true, 400, "success"), "MostRecent first observation waits");
Equal(4, creationStabilizer.Observe(mostRecentGeneration, freshProcessor, true, 400, "success")!.Value.Slot, "MostRecent establishes initial fresh-save epoch");
long creationGeneration = creationStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Creation, freshProcessor);
Equal(true, creationStabilizer.Capture().Pending, "Creation immediately suspends the MostRecent epoch");
Equal<CassetteSaveActivation?>(null, creationStabilizer.Observe(creationGeneration, freshProcessor, true, 400, "success"), "Creation requires a first post-signal observation");
var creationActivation = creationStabilizer.Observe(creationGeneration, freshProcessor, true, 400, "success");
Equal(CassetteSaveBoundarySignalKind.Creation, creationActivation!.Value.Reason, "Creation preserves activation reason");
Equal(false, creationActivation.Value.IncludesBuild, "Creation does not invent Build inclusion");

long buildGeneration = creationStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Build, freshProcessor);
Equal<CassetteSaveActivation?>(null, creationStabilizer.Observe(buildGeneration, freshProcessor, true, 400, "success"), "Build requires a first post-signal observation even when pointer is reused");
var buildActivation = creationStabilizer.Observe(buildGeneration, freshProcessor, true, 400, "success");
Equal(CassetteSaveBoundarySignalKind.Build, buildActivation!.Value.Reason, "Build preserves activation reason");
Equal(true, buildActivation.Value.IncludesBuild, "Build activation retains Build inclusion");
Equal<CassetteSaveActivation?>(null, creationStabilizer.Observe(buildGeneration, freshProcessor, true, 400, "success"), "one Build generation activates at most once");

long coalescedBuildGeneration = creationStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Build, freshProcessor);
long coalescedCreationGeneration = creationStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Creation, freshProcessor);
Equal(true, coalescedCreationGeneration > coalescedBuildGeneration, "newer compatible signal advances generation");
Equal<CassetteSaveActivation?>(null, creationStabilizer.Observe(coalescedBuildGeneration, freshProcessor, true, 400, "success"), "stale coalesced Build cannot activate");
Equal<CassetteSaveActivation?>(null, creationStabilizer.Observe(coalescedCreationGeneration, freshProcessor, true, 400, "success"), "coalesced generation still requires two fresh observations");
var coalescedActivation = creationStabilizer.Observe(coalescedCreationGeneration, freshProcessor, true, 400, "success");
Equal(true, coalescedActivation!.Value.IncludesBuild, "same-slot coalescing retains Build semantics");
Equal(CassetteSaveBoundarySignalKind.Creation, coalescedActivation.Value.Reason, "coalesced activation preserves latest exact reason");

long duplicateSelectionGeneration = creationStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection, freshProcessor);
Equal<CassetteSaveActivation?>(null, creationStabilizer.Observe(duplicateSelectionGeneration, freshProcessor, true, 400, "success"), "duplicate Selection first observation waits");
Equal<CassetteSaveActivation?>(null, creationStabilizer.Observe(duplicateSelectionGeneration, freshProcessor, true, 400, "success"), "duplicate Selection for activated identity does not create another epoch");
Equal(false, creationStabilizer.Capture().Pending, "confirmed duplicate Selection resumes without duplicate activation");

long replacedBuildGeneration = creationStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Build, freshProcessor);
long differentSlotGeneration = creationStabilizer.Signal(3, CassetteSaveBoundarySignalKind.Selection, freshProcessor);
Equal<CassetteSaveActivation?>(null, creationStabilizer.Observe(replacedBuildGeneration, freshProcessor, true, 400, "success"), "different-slot signal rejects stale Build generation");
Equal<CassetteSaveActivation?>(null, creationStabilizer.Observe(differentSlotGeneration, freshProcessor, true, 300, "success"), "different slot first observation waits");
var differentSlotActivation = creationStabilizer.Observe(differentSlotGeneration, freshProcessor, true, 300, "success");
Equal(false, differentSlotActivation!.Value.IncludesBuild, "different slot replaces stale Build inclusion");
Equal(CassetteSaveBoundarySignalKind.Selection, differentSlotActivation.Value.Reason, "different slot preserves Selection reason");

var laterCaptureStabilizer = new CassetteProcessorSaveIdentityStabilizer();
long laterCaptureGeneration = laterCaptureStabilizer.Signal(4, CassetteSaveBoundarySignalKind.Creation, null);
Equal(true, laterCaptureStabilizer.CaptureProcessor(freshProcessor), "Creation can bind a compatible later processor capture");
Equal<CassetteSaveActivation?>(null, laterCaptureStabilizer.Observe(laterCaptureGeneration, freshProcessor, true, 401, "success"), "later capture first observation waits");
Equal(CassetteSaveBoundarySignalKind.Creation, laterCaptureStabilizer.Observe(laterCaptureGeneration, freshProcessor, true, 401, "success")!.Value.Reason, "later capture completes Creation generation");
Console.WriteLine("PASS: creation_and_build_boundaries_reactivate_once");

var queueLogDeduper = new CassetteDiagnosticSignatureDeduplicator();
Equal(true, queueLogDeduper.ShouldLog("queued"), "first MostRecent queue outcome logs");
Equal(false, queueLogDeduper.ShouldLog("queued"), "identical MostRecent queue outcome is suppressed");
Equal(true, queueLogDeduper.ShouldLog("processor-null"), "changed queue stage logs");
var resultLogDeduper = new CassetteDiagnosticSignatureDeduplicator();
Equal(true, resultLogDeduper.ShouldLog("success|slot=3|pointer=700"), "first resolved result logs");
Equal(false, resultLogDeduper.ShouldLog("success|slot=3|pointer=700"), "identical resolved result is suppressed");
Equal(true, resultLogDeduper.ShouldLog("success|slot=4|pointer=800"), "changed resolved slot logs");
Equal(true, resultLogDeduper.ShouldLog("failure|match-count:0"), "changed unresolved stage logs");
var synchronizationDiagnosticDeduper = new CassetteBoundedDiagnosticSignatureDeduplicator(capacity: 8);
Equal(true, synchronizationDiagnosticDeduper.ShouldLog("enquiry=unavailable|global=0"), "initial unavailable synchronization comparison logs");
Equal(false, synchronizationDiagnosticDeduper.ShouldLog("enquiry=unavailable|global=0"), "repeated unavailable synchronization comparison is deduplicated");
Equal(true, synchronizationDiagnosticDeduper.ShouldLog("enquiry=slot4/pointer700|global=0"), "changed loaded-save enquiry comparison logs once");
Equal(false, synchronizationDiagnosticDeduper.ShouldLog("enquiry=slot4/pointer700|global=0"), "repeated loaded-save comparison is deduplicated");
Equal(true, synchronizationDiagnosticDeduper.ShouldLog("enquiry=slot3/pointer900|global=0"), "changed mismatch comparison logs once");
for (int signatureIndex = 0; signatureIndex < 5; signatureIndex++)
    Equal(true, synchronizationDiagnosticDeduper.ShouldLog($"bounded-{signatureIndex}"), $"unique signature {signatureIndex} fits the fixed diagnostic budget");
Equal(false, synchronizationDiagnosticDeduper.ShouldLog("over-budget"), "fixed synchronization diagnostic budget prevents unbounded logging");
Equal(false, synchronizationDiagnosticDeduper.ShouldLog("enquiry=unavailable|global=0"), "previous signature remains deduplicated after intervening states");
Console.WriteLine("PASS: most_recent_logs_are_bounded_and_deduplicated");

var joinProbe = new CassetteRegularSavePointerJoinProbe();
object saveDataProcessorA = new();
object playerProcessorA = new();
long joinGenerationA = joinProbe.Queue(saveDataProcessorA);
Equal(true, joinProbe.Pending, "MostRecent queues bounded pointer-join diagnostic");
Equal(false, joinProbe.CapturePlayerProcessor(null), "null player processor cannot arm join");
Equal(true, joinProbe.CapturePlayerProcessor(playerProcessorA), "compatible later player processor arms join");
var joinSnapshotA = joinProbe.Capture();
Equal(joinGenerationA, joinSnapshotA.Generation, "join snapshot captures generation");
Equal(true, ReferenceEquals(saveDataProcessorA, joinSnapshotA.SaveDataProcessor), "join snapshot pairs save-data processor");
Equal(true, ReferenceEquals(playerProcessorA, joinSnapshotA.PlayerSaveProcessor), "join snapshot pairs player processor");
object saveDataProcessorB = new();
long joinGenerationB = joinProbe.Queue(saveDataProcessorB);
Equal(false, joinProbe.TryConsume(joinSnapshotA), "newer generation rejects stale join result");
Equal(true, joinProbe.Pending, "stale result leaves current diagnostic pending");
object playerProcessorB = new();
joinProbe.CapturePlayerProcessor(playerProcessorB);
var joinSnapshotB = joinProbe.Capture();
Equal(joinGenerationB, joinSnapshotB.Generation, "replacement snapshot uses current generation");
Equal(true, joinProbe.TryConsume(joinSnapshotB), "current diagnostic consumes exactly once");
Equal(false, joinProbe.Pending, "consumed diagnostic is bounded");
Equal(false, joinProbe.TryConsume(joinSnapshotB), "diagnostic cannot consume twice");
long cancelledJoinGeneration = joinProbe.Queue(saveDataProcessorA);
joinProbe.CapturePlayerProcessor(playerProcessorA);
var cancelledJoinSnapshot = joinProbe.Capture();
joinProbe.Cancel();
Equal(false, joinProbe.TryConsume(cancelledJoinSnapshot), "exact Selection cancels pending MostRecent generation");
Equal(false, joinProbe.Pending, "cancelled MostRecent no longer resolves");
Console.WriteLine("PASS: regular_save_pointer_join_probe_is_bounded_and_generation_safe");

IReadOnlyList<string> submitMethods = ExtractMethods(pluginSource, "private static bool TrySubmitHaveInBag(");
Equal(0, submitMethods.Count, "obsolete direct Money cassette submission path is removed");
foreach (string submitSource in submitMethods)
{
    Equal(true, submitSource.Contains("CassetteNativeRequestFactory.TryCreateHaveInBagRequest(", StringComparison.Ordinal), "every cassette submission uses semantic request adapter");
    Equal(true, submitSource.Contains("requestType, songType, statusType, bundleType, nativeSong", StringComparison.Ordinal), "every cassette submission supplies all semantic request types");
    Equal(false, submitSource.Contains("Activator.CreateInstance", StringComparison.Ordinal), "cassette submission forbids parameterless request allocation");
    Equal(false, submitSource.Contains("TryWriteMember(request, \"Song\"", StringComparison.Ordinal), "cassette submission forbids post-construction song writes");
    Equal(false, submitSource.Contains("TryWriteMember(request, \"CassetteStatus\"", StringComparison.Ordinal), "cassette submission forbids post-construction status writes");
}

IReadOnlyList<string> processorFactories =
    ExtractMethods(pluginSource, "private static bool EnsureProcessorAvailable()");
Equal(1, processorFactories.Count,
    "the restart-safe player-save processor factory is uniquely inspectable");

string processorFactory = processorFactories.Single();
int processorStored = processorFactory.IndexOf(
    "_playerSaveRequestProcessor = processor;",
    StringComparison.Ordinal);
int cassetteHandoff = processorFactory.IndexOf(
    "CassetteReceiptRandomization.CapturePlayerSaveRequestProcessor(processor);",
    StringComparison.Ordinal);
Equal(true,
    processorStored >= 0 && cassetteHandoff > processorStored,
    "the restart-safe stateless processor is handed to cassette reconciliation after construction");

string[] expectedCatalog =
{
 "The Little Things|THE_LITTLE_THINGS|The Little Things Cassette|Cassette Source - The Little Things|Level_24:LevelVariant_Default",
 "No Plan B|NO_PLAN_B|No Plan B Cassette|Cassette Source - No Plan B|Level_22:LevelVariant_Default",
 "Jolt City|JOLT_CITY|Jolt City Cassette|Cassette Source - Jolt City|Level_23:LevelVariant_Default",
 "Quieres Bailar|QUIERES_BAILAR|Quieres Bailar Cassette|Cassette Source - Quieres Bailar|Level_16:LevelVariant_Default",
 "Quicksand|QUICKSAND|Quicksand Cassette|Music Lab - 32 Point Chest|",
 "Gold|GOLD|Gold Cassette|Cassette Source - Gold|Level_05:LevelVariant_Default,Level_11:LevelVariant_Default,Level_11:LevelVariant_DevilMode",
 "I Got Money|I_GOT_MONEY|Money Cassette|Level 2 - Money Cassette|Level_06:LevelVariant_Default,Level_06:LevelVariant_BeeMode",
 "Hippo and Frog|HIPPO_AND_FROG|Hippo and Frog Cassette|Cassette Source - Hippo and Frog|Level_07:LevelVariant_Default",
 "On the Way|ON_THE_WAY|On the Way Cassette|Cassette Source - On the Way|Level_08:LevelVariant_Default,Level_11:LevelVariant_Default,Level_11:LevelVariant_DevilMode",
 "Badass|BADASS|Badass Cassette|Cassette Source - Badass|Level_09:LevelVariant_Default",
 "Heavy Metal|HEAVY_METAL|Heavy Metal Cassette|Cassette Source - Heavy Metal|Level_09:LevelVariant_Default",
 "AOK|AOK|AOK Cassette|Cassette Source - AOK|Level_02:LevelVariant_Default,Level_02:LevelVariant_DevilMode",
 "Rainbow Melodies|RAINBOW_MELODIES|Rainbow Melodies Cassette|Cassette Source - Rainbow Melodies|Level_11:LevelVariant_Default,Level_11:LevelVariant_DevilMode,Level_19:LevelVariant_Default",
 "Sneaking|SNEAKING_LOOP|Sneaking Cassette|Cassette Source - Sneaking|Level_19:LevelVariant_Default,Level_20:LevelVariant_Default",
 "The Heist|THE_HEIST|The Heist Cassette|Cassette Source - The Heist|Level_20:LevelVariant_Default",
 "Money|MONEY_DUB|Money Dub Cassette|Cassette Source - Money|Level_01:LevelVariant_Default",
 "Lets Go|LETS_GO|Lets Go Cassette|Cassette Source - Lets Go|Level_12:LevelVariant_Default,Level_12:LevelVariant_BeeMode",
 "Bounce|BOUNCE|Bounce Cassette|Cassette Source - Bounce|Level_15:LevelVariant_Default",
 "Epical|THE_EPICAL|Epical Cassette|Cassette Source - Epical|Level_21:LevelVariant_Default",
 "Hollywood Trailer|HOLLYWOOD_TRAILER|Hollywood Trailer Cassette|Cassette Source - Hollywood Trailer|Level_21:LevelVariant_Default",
 "False Data|FALSE_DATA|False Data Cassette|Cassette Source - False Data|Level_21:LevelVariant_Default",
 "Gotta Get Up|GOTTA_GET_UP|Gotta Get Up Cassette|Cassette Source - Gotta Get Up|Level_03:LevelVariant_Default",
 "Fumblin Around|FUMBLIN_AROUND|Fumblin Around Cassette|Cassette Source - Fumblin Around|Level_13:LevelVariant_Default,Level_13:LevelVariant_DevilMode",
 "Party Non Stop|PARTY_NON_STOP|Party Non Stop Cassette|Cassette Source - Party Non Stop|Level_25:LevelVariant_Default",
 "Keep On Hustlin|KEEP_ON_HUSTLIN|Keep On Hustlin Cassette|Cassette Source - Keep On Hustlin|Level_14:LevelVariant_Default,Level_14:LevelVariant_DevilMode",
 "Another Day In Paradise|ANOTHER_DAY_IN_PARADISE|Another Day In Paradise Cassette|Cassette Source - Another Day In Paradise|Level_28:LevelVariant_Default",
 "Flamenco|FLAMENCO|Flamenco Cassette|Music Lab - 64 Point Chest|",
 "Ten-Four Good Buddy|TEN_FOUR_GOOD_BUDDY|Ten-Four Good Buddy Cassette|Music Lab - 89 Point Chest|",
 "Zen|ZEN|Zen Cassette|Music Lab - 111 Point Chest|",
 "Wiggle|WIGGLE|Wiggle Cassette|Music Lab - 140 Point Chest|",
};

Equal(30, CassetteCatalog.All.Count, "catalog count");

using JsonDocument contractDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "cassette-contract-v1.json")));
JsonElement contractRoot = contractDocument.RootElement;
Equal(1, contractRoot.GetProperty("schema").GetInt32(), "neutral fixture schema");
Equal(30, contractRoot.GetProperty("count").GetInt32(), "neutral fixture count");
string[] fixtureMappings = contractRoot.GetProperty("entries").EnumerateArray().Select(entry =>
    $"{entry.GetProperty("display_song").GetString()}|{entry.GetProperty("item_name").GetString()}|{entry.GetProperty("source_name").GetString()}|{entry.GetProperty("reused_location").GetBoolean()}").ToArray();
string[] clientMappings = CassetteCatalog.All.Select(entry => $"{entry.DisplaySong}|{entry.ItemName}|{entry.SourceName}|{entry.ReusesExistingLocation}").ToArray();
SequenceEqual(fixtureMappings, clientMappings, "neutral fixture matches complete client mapping");

var compatibleItems = CassetteCatalog.All.ToDictionary(x => x.DisplaySong, x => x.ItemName, StringComparer.Ordinal);
var compatibleSources = CassetteCatalog.All.ToDictionary(x => x.DisplaySong, x => x.SourceName, StringComparer.Ordinal);
var compatibleReused = CassetteCatalog.All.Where(x => x.ReusesExistingLocation).ToDictionary(x => x.DisplaySong, x => x.SourceName, StringComparer.Ordinal);
var networkSlotMap = JObject.Parse("{\"The Little Things\":\"The Little Things Cassette\"}");
var parsedNetworkSlotMap = CassetteSlotMapReader.Read(networkSlotMap);
Equal(1, parsedNetworkSlotMap.Count, "network JObject slot map count");
Equal("The Little Things Cassette", parsedNetworkSlotMap["The Little Things"], "network JObject slot map value");
var compatibility = CassetteSlotCompatibility.Validate(1, true, 30, compatibleItems, compatibleSources, compatibleReused);
Equal(true, compatibility.Compatible, "exact v0.22 cassette contract is compatible");
Equal("compatible", compatibility.Detail, "compatible detail");
Equal(false, CassetteSlotCompatibility.Validate(0, true, 30, compatibleItems, compatibleSources, compatibleReused).Compatible, "old schema fails closed");
Equal(false, CassetteSlotCompatibility.Validate(1, false, 30, compatibleItems, compatibleSources, compatibleReused).Compatible, "feature off fails closed");
Equal(false, CassetteSlotCompatibility.Validate(1, true, 29, compatibleItems, compatibleSources, compatibleReused).Compatible, "wrong count fails closed");
var wrongItems = new Dictionary<string,string>(compatibleItems, StringComparer.Ordinal) { ["Quicksand"] = "Wrong Cassette" };
compatibility = CassetteSlotCompatibility.Validate(1, true, 30, wrongItems, compatibleSources, compatibleReused);
Equal(false, compatibility.Compatible, "wrong item mapping fails closed");
Equal(true, compatibility.Detail.Contains("cassette_items.Quicksand", StringComparison.Ordinal), "item mismatch names exact key");
var missingSources = new Dictionary<string,string>(compatibleSources, StringComparer.Ordinal); missingSources.Remove("Zen");
compatibility = CassetteSlotCompatibility.Validate(1, true, 30, compatibleItems, missingSources, compatibleReused);
Equal(false, compatibility.Compatible, "missing source mapping fails closed");
Equal(true, compatibility.Detail.Contains("cassette_sources.Zen", StringComparison.Ordinal), "source mismatch names exact key");
var wrongReused = new Dictionary<string,string>(compatibleReused, StringComparer.Ordinal) { ["Quicksand"] = "Wrong Chest" };
compatibility = CassetteSlotCompatibility.Validate(1, true, 30, compatibleItems, compatibleSources, wrongReused);
Equal(false, compatibility.Compatible, "wrong reused location fails closed");
Equal(30, CassetteCatalog.All.Select(x=>x.DisplaySong).Distinct(StringComparer.Ordinal).Count(), "unique display songs");
Equal(30, CassetteCatalog.ByNativeSong.Count, "unique native songs");
Equal(30, CassetteCatalog.ByItemName.Count, "unique items");
Equal(30, CassetteCatalog.All.Select(x=>x.SourceName).Distinct(StringComparer.Ordinal).Count(), "unique sources");
SequenceEqual(expectedCatalog, CassetteCatalog.All.Select(x=>$"{x.DisplaySong}|{x.NativeSong}|{x.ItemName}|{x.SourceName}|{string.Join(",",x.Triggers.Select(t=>$"{t.Level}:{t.Variant}"))}"), "exact reviewed catalog");
var money=CassetteCatalog.ByItemName["Money Cassette"];
Equal("I_GOT_MONEY",money.NativeSong,"Money native identity"); Equal("Level 2 - Money Cassette",money.SourceName,"Money source"); Equal(true,money.ReusesExistingLocation,"Money reused location");
SequenceEqual(new[]{"Music Lab - 32 Point Chest","Music Lab - 64 Point Chest","Music Lab - 89 Point Chest","Music Lab - 111 Point Chest","Music Lab - 140 Point Chest"},CassetteCatalog.All.Where(x=>x.SourceType==CassetteSourceType.MusicLabPointChest).Select(x=>x.SourceName),"exact point chests");

var d=CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_Default",true,new Dictionary<string,string>{{"I_GOT_MONEY",CassetteRandomizationPolicy.Invalid}});
Equal(false,d.AllowNative,"Money suppresses native"); SequenceEqual(new[]{"Level 2 - Money Cassette"},d.SourceLocationsToQueue,"Money source queued"); SequenceEqual(new[]{"I_GOT_MONEY"},d.NativeSongsToSuppress,"Money suppressed");
foreach(var status in new[]{CassetteRandomizationPolicy.Invalid,CassetteRandomizationPolicy.HaveNotEarned}) Equal(false,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_BeeMode",true,new Dictionary<string,string>{{"I_GOT_MONEY",status}}).AllowNative,$"Bee alias {status}");
foreach(var status in new[]{CassetteRandomizationPolicy.HaveInBag,CassetteRandomizationPolicy.HaveDeposited}) { d=CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_Default",true,new Dictionary<string,string>{{"I_GOT_MONEY",status}}); Equal(true,d.AllowNative,$"owned {status}"); Equal(0,d.SourceLocationsToQueue.Count,"owned queues none"); }
Equal(true,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_Default",false,new Dictionary<string,string>{{"I_GOT_MONEY",CassetteRandomizationPolicy.Invalid}}).AllowNative,"failure native");
Equal(true,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_DevilMode",true,new Dictionary<string,string>{{"I_GOT_MONEY",CassetteRandomizationPolicy.Invalid}}).AllowNative,"wrong variant native");
Equal(true,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_99","LevelVariant_Default",true,new Dictionary<string,string>()).AllowNative,"unrelated native");
Equal(true,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_Default",true,new Dictionary<string,string>{{"I_GOT_MONEY","UNREADABLE"}}).AllowNative,"unreadable native");
Equal(true,CassetteRandomizationPolicy.DecideLevelEvaluation("Level_06","LevelVariant_Default",true,new Dictionary<string,string>()).AllowNative,"missing native");
d=CassetteRandomizationPolicy.DecideLevelEvaluation("Level_09","LevelVariant_Default",true,new Dictionary<string,string>{{"BADASS",CassetteRandomizationPolicy.Invalid},{"HEAVY_METAL",CassetteRandomizationPolicy.HaveNotEarned}});
Equal(false,d.AllowNative,"multi suppresses"); SequenceEqual(new[]{"Cassette Source - Badass","Cassette Source - Heavy Metal"},d.SourceLocationsToQueue,"multi sources"); SequenceEqual(new[]{"BADASS","HEAVY_METAL"},d.NativeSongsToSuppress,"multi songs");
d=CassetteRandomizationPolicy.DecideLevelEvaluation("Level_09","LevelVariant_Default",true,new Dictionary<string,string>{{"BADASS",CassetteRandomizationPolicy.Invalid}}); Equal(true,d.AllowNative,"partial unreadable native"); Equal(0,d.SourceLocationsToQueue.Count,"unsafe partial queues none");
Equal(false,CassetteRandomizationPolicy.UseSelectedSaveChangedEventHook,"crashing hook disabled");
Equal(true, CassetteRandomizationPolicy.ShouldSuppressNativePointChestGrant("QUICKSAND", CassetteRandomizationPolicy.HaveInBag, fromArchipelago:false), "native point-chest Quicksand bag grant is randomized");
Equal(false, CassetteRandomizationPolicy.ShouldSuppressNativePointChestGrant("QUICKSAND", CassetteRandomizationPolicy.HaveInBag, fromArchipelago:true), "AP receipt may grant Quicksand later");
Equal(false, CassetteRandomizationPolicy.ShouldSuppressNativePointChestGrant("I_GOT_MONEY", CassetteRandomizationPolicy.HaveInBag, fromArchipelago:false), "level cassette grants are handled by the evaluator hook");
Equal(false, CassetteRandomizationPolicy.ShouldSuppressNativePointChestGrant("QUICKSAND", CassetteRandomizationPolicy.HaveDeposited, fromArchipelago:false), "deposited state is never intercepted");



var authoritativeState = new AuthoritativeSaveState(TestCassetteStatus.HAVE_IN_BAG);
var authoritativeProcessor = new AuthoritativeProcessor(authoritativeState);
Equal(true,
    CassetteAuthoritativeStateReader.TryRead(
        authoritativeProcessor,
        typeof(TestSong),
        nameof(TestSong.QUIERES_BAILAR),
        out string? authoritativeStatus,
        out string authoritativeDetail),
    "processor-selected save status is readable without the private bundle predicate");
Equal(nameof(TestCassetteStatus.HAVE_IN_BAG), authoritativeStatus, "authoritative reader returns processor-selected status");
Equal(1, authoritativeProcessor.ObtainStateCalls, "authoritative reader obtains current processor state once");
Equal(1, authoritativeState.StatusReads, "authoritative reader reads current cassette status once");
Equal(true, authoritativeDetail.Contains("processor-selected save returned HAVE_IN_BAG", StringComparison.Ordinal), "authoritative detail names the read source and status");


var preSelection = new CassetteSaveEpochRuntime();
preSelection.Receive("BADASS");
preSelection.Observe("BADASS", CassetteRandomizationPolicy.HaveInBag);
Equal(false, preSelection.HasActiveSave, "receipt before save selection remains inactive");
Equal(false, preSelection.IsPending("BADASS"), "pre-selection observation cannot make a cassette pending in a save");
Equal(false, preSelection.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true), "no semantic request before a confirmed save load");
Console.WriteLine("PASS: receipt_before_save_selection_never_reads_writes_or_satisfies");

var preSlotBag = new CassetteSaveEpochRuntime();
preSlotBag.Receive("BADASS");
preSlotBag.Observe("BADASS", CassetteRandomizationPolicy.HaveInBag);
preSlotBag.ActivateSave(2);
Equal(1L, preSlotBag.Epoch, "first selected save creates epoch one");
Equal(true, preSlotBag.IsPending("BADASS"), "pre-slot bag cannot suppress the later loaded save");
Console.WriteLine("PASS: pre_slot_bag_cannot_suppress_the_later_loaded_save");

var sameSlotReload = new CassetteSaveEpochRuntime();
sameSlotReload.Receive("BADASS");
sameSlotReload.ActivateSave(2);
sameSlotReload.Observe("BADASS", CassetteRandomizationPolicy.HaveInBag);
sameSlotReload.ActivateSave(2);
Equal(2L, sameSlotReload.Epoch, "same-slot reload creates a new epoch");
Equal(true, sameSlotReload.IsPending("BADASS"), "same-slot reload revalidates native state");
Console.WriteLine("PASS: same_numeric_slot_reload_creates_a_new_epoch_and_revalidates");

var depositedSaveA = new CassetteSaveEpochRuntime();
depositedSaveA.Receive("BADASS");
depositedSaveA.ActivateSave(1);
depositedSaveA.Observe("BADASS", CassetteRandomizationPolicy.HaveDeposited);
Equal(false, depositedSaveA.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveDeposited, true), "deposited save A is not rewritten");
depositedSaveA.ActivateSave(2);
Equal(2L, depositedSaveA.Epoch, "switching to a different valid slot creates a new epoch");
Equal(true, depositedSaveA.IsPending("BADASS"), "deposited save A does not terminal-cache unowned save B");
Equal(true, depositedSaveA.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true), "unowned save B can receive its owned cassette");
Console.WriteLine("PASS: deposited_in_save_a_does_not_terminal_cache_save_b");

var depositedRevalidation = new CassetteSaveEpochRuntime();
depositedRevalidation.Receive("ZEN");
depositedRevalidation.ActivateSave(1);
depositedRevalidation.Observe("ZEN", CassetteRandomizationPolicy.HaveDeposited);
depositedRevalidation.ActivateSave(1);
depositedRevalidation.Observe("ZEN", CassetteRandomizationPolicy.HaveDeposited);
Equal(false, depositedRevalidation.CanSubmit("ZEN", CassetteRandomizationPolicy.HaveDeposited, true), "deposited cassette remains unwritten after reloading its save");
Console.WriteLine("PASS: deposited_is_never_rewritten_within_or_after_reloading_its_save");

var lifecycleNeutral = new CassetteSaveEpochRuntime();
lifecycleNeutral.Receive("BADASS");
lifecycleNeutral.ActivateSave(4);
lifecycleNeutral.Observe("BADASS", CassetteRandomizationPolicy.HaveInBag);
lifecycleNeutral.Observe("BADASS", CassetteRandomizationPolicy.HaveInBag);
Equal(1L, lifecycleNeutral.Epoch, "observations do not create or replace a save epoch");
Equal(false, lifecycleNeutral.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveInBag, true), "broad lifecycle-neutral observations do not rewrite a satisfied cassette");
Console.WriteLine("PASS: broad_lifecycle_notifications_do_not_create_or_replace_a_save_epoch");

var semantic = new CassetteSaveEpochRuntime();
semantic.Receive("BADASS");
semantic.ActivateSave(3);
Equal(false, semantic.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, false), "missing processor fails closed");
Equal(true, semantic.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true), "active epoch and unearned read permit semantic request");
semantic.RecordSubmission("BADASS");
Equal(false, semantic.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true), "submitted request waits for delayed verification");
Equal(0, semantic.Tick(TimeSpan.FromMilliseconds(249)).Count, "verification is delayed");
SequenceEqual(new[] { "BADASS" }, semantic.Tick(TimeSpan.FromMilliseconds(1)), "first bounded delay schedules authoritative verification");
semantic.RecordVerification("BADASS", CassetteRandomizationPolicy.HaveInBag);
Equal(false, semantic.IsPending("BADASS"), "later authoritative bag read satisfies this epoch");
Console.WriteLine("PASS: post_epoch_semantic_request_requires_delayed_verification");

var boundedSchedule = new CassetteSaveEpochRuntime();
boundedSchedule.Receive("BADASS");
boundedSchedule.ActivateSave(3);
boundedSchedule.RecordSubmission("BADASS");
Equal(0, boundedSchedule.Tick(TimeSpan.FromMilliseconds(249)).Count, "attempt one cannot verify before 250ms");
SequenceEqual(new[] { "BADASS" }, boundedSchedule.Tick(TimeSpan.FromMilliseconds(1)), "attempt one verifies at 250ms");
boundedSchedule.RecordVerification("BADASS", CassetteRandomizationPolicy.HaveNotEarned);
Equal(true, boundedSchedule.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true), "unearned first verification permits bounded retry");
boundedSchedule.RecordSubmission("BADASS");
Equal(0, boundedSchedule.Tick(TimeSpan.FromMilliseconds(999)).Count, "attempt two cannot verify before 1s");
SequenceEqual(new[] { "BADASS" }, boundedSchedule.Tick(TimeSpan.FromMilliseconds(1)), "attempt two verifies at 1s");
boundedSchedule.RecordVerification("BADASS", CassetteRandomizationPolicy.HaveNotEarned);
Equal(true, boundedSchedule.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true), "unearned second verification permits final bounded retry");
boundedSchedule.RecordSubmission("BADASS");
Equal(0, boundedSchedule.Tick(TimeSpan.FromMilliseconds(2999)).Count, "attempt three cannot verify before 3s");
SequenceEqual(new[] { "BADASS" }, boundedSchedule.Tick(TimeSpan.FromMilliseconds(1)), "attempt three verifies at 3s");
boundedSchedule.RecordVerification("BADASS", CassetteRandomizationPolicy.HaveNotEarned);
Equal(false, boundedSchedule.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true), "three attempts exhaust the current epoch retry budget");
Console.WriteLine("PASS: bounded_verification_schedule_uses_250ms_1s_3s");

var staleVerification = new CassetteSaveEpochRuntime();
staleVerification.Receive("BADASS");
staleVerification.ActivateSave(1);
staleVerification.RecordSubmission("BADASS");
staleVerification.ActivateSave(2);
Equal(0, staleVerification.Tick(TimeSpan.FromSeconds(3)).Count, "reload invalidates stale verification timer from prior epoch");
staleVerification.RecordVerification("BADASS", null);
Equal(true, staleVerification.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true), "reload clears prior attempt and revalidates new epoch");
Console.WriteLine("PASS: reload_clears_attempt_without_process_wide_satisfaction");

var diskCommit = new CassetteDiskCommitRuntime();
Equal(TimeSpan.Zero, CassetteDiskCommitRuntime.CapActiveUpdateElapsed(TimeSpan.FromSeconds(-1)), "negative keeper elapsed contributes no active write time");
Equal(TimeSpan.FromMilliseconds(500), CassetteDiskCommitRuntime.CapActiveUpdateElapsed(TimeSpan.FromMilliseconds(500)), "normal update elapsed is preserved");
Equal(TimeSpan.FromSeconds(1), CassetteDiskCommitRuntime.CapActiveUpdateElapsed(TimeSpan.FromMinutes(5)), "suspension-sized wall elapsed is capped to one active update second");
diskCommit.Stage("BADASS"); diskCommit.Stage("THE_HEIST"); diskCommit.Stage("BADASS");
Equal(2, diskCommit.Songs.Count, "one reconciliation wave coalesces cassette grants");
var dirtyWrite = new CassettePublicWriteState(true, false, 10, null, null);
var phaseRuntime = new CassetteDiskCommitRuntime();
phaseRuntime.Stage("BADASS");
Equal(true, phaseRuntime.TryPrepare(7, 1, 4, 400, dirtyWrite, out CassetteDiskCommitAttempt phaseAttempt), "verified wave captures a distinct prepared transaction");
Equal(CassetteDiskCommitEventOutcome.Ignored, phaseRuntime.ObserveWriteCompletedEvent(phaseAttempt, 4, true), "late prior success event between baseline and submit is ignored");
Equal(CassetteDiskCommitEventOutcome.Ignored, phaseRuntime.ObserveWriteCompletedEvent(phaseAttempt, 4, false), "late prior failure event between baseline and submit is ignored");
Equal(CassetteDiskCommitOutcome.None, phaseRuntime.Observe(phaseAttempt, new(false, false, 11, null, null), true, TimeSpan.Zero, out bool phasePending), "prior advanced timestamp cannot complete a merely prepared transaction");
Equal(false, phasePending, "prepared transaction emits no submitted-write notice");
Equal(true, phaseRuntime.MarkSubmitted(phaseAttempt, new(false, false, 11, null, null)), "successful public submit transitions the exact prepared attempt with a refreshed post-submit baseline");
Equal(CassetteDiskCommitEventOutcome.SuccessWake, phaseRuntime.ObserveWriteCompletedEvent(phaseAttempt, 4, true), "event is accepted only after exact attempt is submitted");
Equal(CassetteDiskCommitOutcome.Pending, phaseRuntime.Observe(phaseAttempt, new(false, false, 11, null, null), true, TimeSpan.Zero, out phasePending), "timestamp that advanced before submit cannot prove the submitted attempt");
Equal(CassetteDiskCommitOutcome.Success, phaseRuntime.Observe(phaseAttempt, new(false, false, 12, null, null), true, TimeSpan.Zero, out phasePending), "submitted attempt requires a later public-state advance");

var diagnosticRuntime = new CassetteDiskCommitRuntime();
diagnosticRuntime.Stage("ON_THE_WAY");
Equal(true, diagnosticRuntime.TryPrepare(17, 2, 4, 0x444, dirtyWrite, out CassetteDiskCommitAttempt diagnosticAttempt), "diagnostic attempt prepares without changing transaction semantics");
Equal(true, diagnosticRuntime.TryGetPreparedAttempt(out CassetteDiskCommitAttempt capturedPreparedAttempt), "prepared attempt is observable for bounded ignored-event diagnostics");
Equal(diagnosticAttempt, capturedPreparedAttempt, "prepared diagnostic capture preserves the exact attempt token");
Equal(true, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.Pre, out CassetteDiskCommitDiagnosticContext preDiagnostic), "PRE diagnostic is admitted once for the exact attempt");
Equal(false, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.Pre, out _), "PRE diagnostic has a one-snapshot budget");
Equal(true, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.PreTarget, out CassetteDiskCommitDiagnosticContext preTargetDiagnostic), "PRE_TARGET diagnostic is admitted once for the prepared attempt");
Equal(CassetteDiskCommitDiagnosticPhase.PreTarget, preTargetDiagnostic.Phase, "PRE_TARGET diagnostic preserves its phase");
Equal(false, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.PreTarget, out _), "PRE_TARGET diagnostic has a one-snapshot budget");
Equal(0, preDiagnostic.EventOrdinal, "PRE snapshot precedes all accepted events");
Equal(TimeSpan.Zero, preDiagnostic.Elapsed, "PRE snapshot starts at zero active-update elapsed");
SequenceEqual(new[] { "ON_THE_WAY" }, preDiagnostic.ActiveSongs, "PRE snapshot captures frozen active songs");
Equal(0, preDiagnostic.QueuedSongs.Count, "PRE snapshot has no later queued songs");
Equal(true, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.PreparedEventIgnored, out CassetteDiskCommitDiagnosticContext ignoredPreparedEvent), "one prepared-phase ignored event record is admitted");
Equal(1, ignoredPreparedEvent.EventOrdinal, "prepared-phase ignored event receives the first diagnostic event ordinal");
Equal(false, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.PreparedEventIgnored, out _), "prepared-phase ignored event record is bounded");
Equal(false, diagnosticRuntime.Active, "diagnostic admission cannot mark a prepared attempt submitted");
Equal(true, diagnosticRuntime.MarkSubmitted(diagnosticAttempt, dirtyWrite), "normal submission transition remains authoritative after diagnostics");
Equal(true, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.Post, out _), "POST diagnostic is admitted once after submission");
Equal(false, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.Post, out _), "POST diagnostic has a one-snapshot budget");
Equal(true, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.PostTarget, out CassetteDiskCommitDiagnosticContext postTargetDiagnostic), "POST_TARGET diagnostic is admitted once for the submitted attempt");
Equal(CassetteDiskCommitDiagnosticPhase.PostTarget, postTargetDiagnostic.Phase, "POST_TARGET diagnostic preserves its phase");
Equal(false, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.PostTarget, out _), "POST_TARGET diagnostic has a one-snapshot budget");
diagnosticRuntime.Stage("KEEP_ON_HUSTLIN");
Equal(CassetteDiskCommitEventOutcome.SuccessWake, diagnosticRuntime.ObserveWriteCompletedEvent(diagnosticAttempt, 4, true), "matching event is accepted independently of diagnostics");
Equal(true, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.Event, out CassetteDiskCommitDiagnosticContext eventDiagnostic), "accepted EVENT diagnostic is admitted once");
Equal(false, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.Event, out _), "accepted EVENT diagnostic has a one-snapshot budget");
Equal(2, eventDiagnostic.EventOrdinal, "accepted EVENT snapshot follows the prepared-phase ignored event ordinal");
SequenceEqual(new[] { "ON_THE_WAY" }, eventDiagnostic.ActiveSongs, "EVENT snapshot retains the frozen submitted wave");
SequenceEqual(new[] { "KEEP_ON_HUSTLIN" }, eventDiagnostic.QueuedSongs, "EVENT snapshot distinguishes songs queued after submission");
Equal(CassetteDiskCommitOutcome.Pending, diagnosticRuntime.Observe(diagnosticAttempt, dirtyWrite, true, TimeSpan.FromSeconds(11), out bool diagnosticStillPending), "diagnostic transaction reaches the unchanged still-pending threshold");
Equal(true, diagnosticStillPending, "normal runtime reports the unchanged still-pending transition");
Equal(true, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.StillPending, out CassetteDiskCommitDiagnosticContext stillPendingDiagnostic), "STILL_PENDING diagnostic is admitted once");
Equal(TimeSpan.FromSeconds(11), stillPendingDiagnostic.Elapsed, "STILL_PENDING snapshot carries exact active-update elapsed");
Equal(false, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.StillPending, out _), "STILL_PENDING diagnostic has a one-snapshot budget");
Equal(CassetteDiskCommitOutcome.HardTimeout, diagnosticRuntime.Observe(diagnosticAttempt, dirtyWrite, true, TimeSpan.FromSeconds(119), out diagnosticStillPending), "diagnostic transaction reaches the unchanged hard watchdog");
Equal(true, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.HardTimeout, out CassetteDiskCommitDiagnosticContext hardTimeoutDiagnostic), "HARD_TIMEOUT diagnostic remains claimable after the terminal transition");
Equal(TimeSpan.FromSeconds(130), hardTimeoutDiagnostic.Elapsed, "HARD_TIMEOUT snapshot carries exact active-update elapsed");
Equal(false, diagnosticRuntime.TryClaimDiagnostic(diagnosticAttempt, CassetteDiskCommitDiagnosticPhase.HardTimeout, out _), "HARD_TIMEOUT diagnostic has a one-snapshot budget");
Equal(false, diagnosticRuntime.HasWork, "diagnostic snapshot claims cannot unblock an indeterminate transaction");
Equal(true, diagnosticRuntime.TryGetLastTerminalAttempt(out CassetteDiskCommitAttempt watchdogTombstone), "hard watchdog preserves a bounded terminal-attempt tombstone");
Equal(diagnosticAttempt, watchdogTombstone, "watchdog tombstone preserves exact attempt identity");
Equal(false, diagnosticRuntime.TryClaimLateEvent(diagnosticAttempt, 3), "wrong-slot event cannot consume late-event budget");
Equal(true, diagnosticRuntime.TryClaimLateEvent(diagnosticAttempt, 4), "matching inactive event claims one header-only late-event record");
Equal(false, diagnosticRuntime.TryClaimLateEvent(diagnosticAttempt, 4), "late-event record is bounded once per terminal tombstone");
Equal(false, diagnosticRuntime.TryBeginSaveStateLifecycleDiagnostic(3, out _, out _), "wrong-slot rebuild cannot consume lifecycle diagnostic pair");
Equal(true, diagnosticRuntime.TryBeginSaveStateLifecycleDiagnostic(4, out long lifecycleToken, out CassetteDiskCommitDiagnosticContext lifecycleBefore), "matching-slot terminal tombstone admits one lifecycle BEFORE snapshot");
Equal(CassetteDiskCommitDiagnosticPhase.LifecycleBefore, lifecycleBefore.Phase, "lifecycle prefix is labeled BEFORE");
SequenceEqual(new[] { "ON_THE_WAY" }, lifecycleBefore.ActiveSongs, "lifecycle snapshot preserves terminal active songs");
Equal(false, diagnosticRuntime.TryCompleteSaveStateLifecycleDiagnostic(lifecycleToken, 3, out _), "wrong-slot postfix cannot consume the correlated lifecycle AFTER snapshot");
Equal(true, diagnosticRuntime.TryCompleteSaveStateLifecycleDiagnostic(lifecycleToken, 4, out CassetteDiskCommitDiagnosticContext lifecycleAfter), "matching lifecycle postfix admits one AFTER snapshot");
Equal(CassetteDiskCommitDiagnosticPhase.LifecycleAfter, lifecycleAfter.Phase, "lifecycle postfix is labeled AFTER");
Equal(false, diagnosticRuntime.TryBeginSaveStateLifecycleDiagnostic(4, out _, out _), "terminal attempt admits only one lifecycle pair");
diagnosticRuntime.Reset();
Equal(false, diagnosticRuntime.TryGetLastTerminalAttempt(out _), "identity reset clears terminal tombstone");
Equal(false, diagnosticRuntime.TryClaimLateEvent(diagnosticAttempt, 4), "identity reset clears late-event diagnostic eligibility");
Equal("present=False value=<null> bits=<null>", CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(null), "nullable formatter identifies absent GameTime exactly");
Equal("present=True value=10.25 bits=0x4024800000000000", CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(10.25), "nullable formatter preserves round-trip value and IEEE bits");
Equal(true, CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(double.NaN).StartsWith("present=True value=NaN bits=0x", StringComparison.Ordinal), "nullable formatter preserves special double value and IEEE bits");

var routingCommit = new CassetteDiskCommitRuntime();
var routingDiagnostics = new CassetteBundleRoutingDiagnosticRuntime();
CassetteDiskCommitAttempt routingCandidate = routingCommit.PreviewRoutingDiagnosticAttempt(31, 6, 4, 0x444);
Equal(1L, routingCandidate.Id, "pre-grant routing diagnostics preview the next monotonic disk attempt without starting it");
Equal(false, routingCommit.Active, "routing preview is behavior-neutral and cannot activate persistence");
Equal(false, routingCommit.HasWork, "routing preview cannot stage disk work");
Equal(false, routingDiagnostics.TryResolveGlobalAttempt(null, routingCandidate, out _), "unrelated global request before an AP record candidate cannot consume the future attempt budget");
routingDiagnostics.RegisterRelevantSong(routingCandidate, "BADASS", establishCandidate: true);
Equal(true, routingDiagnostics.TryResolveGlobalAttempt(null, routingCandidate, out CassetteDiskCommitAttempt resolvedCandidate), "relevant AP record establishes the only pre-attempt global diagnostic candidate");
Equal(routingCandidate, resolvedCandidate, "pre-attempt global diagnostic resolves the exact AP record candidate");
SequenceEqual(new[] { "BADASS" }, routingDiagnostics.GetRelevantSongs(routingCandidate), "nested global boundary retains AP request song before delayed verification stages it");
Equal(true, routingDiagnostics.TryBegin(routingCandidate, CassetteBundleRoutingBoundary.RecordSong, "BADASS", new[] { "BADASS" }, out CassetteBundleRoutingDiagnosticToken recordRoutingToken), "record-song entry is admitted once per attempt/song/boundary");
Equal(false, routingDiagnostics.TryBegin(routingCandidate, CassetteBundleRoutingBoundary.RecordSong, "BADASS", new[] { "BADASS" }, out _), "duplicate record-song entry is bounded");
Equal(true, routingDiagnostics.TryComplete(recordRoutingToken, out CassetteBundleRoutingDiagnosticToken recordRoutingExit), "record-song exit is correlated to its admitted entry");
Equal(CassetteBundleRoutingPhase.Exit, recordRoutingExit.Phase, "correlated routing completion is labeled EXIT");
Equal(false, routingDiagnostics.TryComplete(recordRoutingToken, out _), "duplicate record-song exit is bounded");
Equal(true, routingDiagnostics.TryBegin(routingCandidate, CassetteBundleRoutingBoundary.RecordSong, "ON_THE_WAY", new[] { "ON_THE_WAY" }, out _), "another song retains its own bounded record diagnostic");
Equal(true, routingDiagnostics.TryBegin(routingCandidate, CassetteBundleRoutingBoundary.PersistBundle, "<bundle>", new[] { "BADASS", "ON_THE_WAY" }, out CassetteBundleRoutingDiagnosticToken persistRoutingToken), "persist boundary has an independent budget for the same attempt");
routingCommit.Stage("BADASS");
Equal(true, routingCommit.TryPrepare(31, 6, 4, 0x444, new(true, false, null, null, null), out CassetteDiskCommitAttempt preparedRoutingAttempt), "routing candidate becomes the real disk attempt only through the existing prepare path");
Equal(routingCandidate, preparedRoutingAttempt, "pre-grant routing candidate correlates to the later disk attempt exactly");
Equal(preparedRoutingAttempt, routingCommit.PreviewRoutingDiagnosticAttempt(31, 6, 4, 0x444), "active routing diagnostics use the exact current attempt token");
routingDiagnostics.RegisterRelevantSong(preparedRoutingAttempt, "ON_THE_WAY", establishCandidate: false);
SequenceEqual(new[] { "BADASS", "ON_THE_WAY" }, routingDiagnostics.GetRelevantSongs(preparedRoutingAttempt), "queued record song joins nested global snapshots during another active attempt");
Equal(true, routingDiagnostics.TryResolveGlobalAttempt(preparedRoutingAttempt, routingCommit.PreviewRoutingDiagnosticAttempt(31, 6, 4, 0x444), out CassetteDiskCommitAttempt resolvedActive), "real active attempt authorizes global diagnostic without speculative admission");
Equal(preparedRoutingAttempt, resolvedActive, "global diagnostic prefers the real prepared attempt");
Equal(true, routingDiagnostics.TryComplete(persistRoutingToken, out _), "persist exit remains correlated after the existing prepare transition");
routingDiagnostics.Reset();
Equal(false, routingDiagnostics.TryComplete(recordRoutingToken, out _), "identity reset invalidates stale routing callbacks");

phaseRuntime.Stage("BADASS");
Equal(true, phaseRuntime.TryPrepare(7, 1, 4, 400, dirtyWrite, out CassetteDiskCommitAttempt newerAttempt), "same identity can prepare a later monotonic attempt");
Equal(true, phaseRuntime.MarkSubmitted(newerAttempt, dirtyWrite), "later attempt is submitted");
Equal(true, newerAttempt.Id > phaseAttempt.Id, "attempt token increases monotonically");
Equal(CassetteDiskCommitOutcome.None, phaseRuntime.Observe(phaseAttempt, new(false, false, 12, null, null), true, TimeSpan.FromSeconds(130), out phasePending), "stale overlapping poll cannot mutate later same-identity attempt");
Equal(true, phaseRuntime.Active, "later attempt remains active after stale poll returns");
Equal(true, phaseRuntime.TryReportRejectedEvent(newerAttempt), "first rejected event diagnostic is admitted for an attempt");
Equal(false, phaseRuntime.TryReportRejectedEvent(newerAttempt), "alternating rejected event stages share a fixed one-log budget");
Equal(CassetteDiskCommitEventOutcome.Ignored, diskCommit.ObserveWriteCompletedEvent(new(1, 7, 1, 4, 400), 4, true), "pre-submit write event is ignored");
Equal(true, diskCommit.TryPrepare(7, 1, 4, 400, dirtyWrite, out CassetteDiskCommitAttempt diskAttempt), "dirty verified wave prepares one disk transaction");
Equal(false, diskCommit.TryPrepare(7, 1, 4, 400, dirtyWrite, out _), "prepared wave cannot submit a duplicate persist");
Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "dirty verified wave records successful request submission");
Equal(CassetteDiskCommitEventOutcome.Ignored, diskCommit.ObserveWriteCompletedEvent(diskAttempt, 3, true), "wrong-slot write event is ignored");
Equal(CassetteDiskCommitEventOutcome.Ignored, diskCommit.ObserveWriteCompletedEvent(diskAttempt with { Generation = 8 }, 4, true), "wrong-generation write event is ignored");
Equal(CassetteDiskCommitEventOutcome.Ignored, diskCommit.ObserveWriteCompletedEvent(diskAttempt with { Epoch = 2 }, 4, true), "wrong-epoch write event is ignored");
Equal(CassetteDiskCommitEventOutcome.Ignored, diskCommit.ObserveWriteCompletedEvent(diskAttempt with { Pointer = 401 }, 4, true), "wrong-pointer write event is ignored");
Equal(CassetteDiskCommitEventOutcome.SuccessWake, diskCommit.ObserveWriteCompletedEvent(diskAttempt, 4, true), "matching successful event wakes verification");
Equal(CassetteDiskCommitEventOutcome.Ignored, diskCommit.ObserveWriteCompletedEvent(diskAttempt, 4, true), "duplicate successful event is bounded");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.Observe(diskAttempt, new(true, true, 10, null, null), true, TimeSpan.FromSeconds(1), out bool reportStillPending), "event cannot complete without public state proof");
Equal(false, reportStillPending, "one second does not emit a still-pending notice");
Equal(CassetteDiskCommitOutcome.Success, diskCommit.Observe(diskAttempt, new(false, false, 11, null, null), true, TimeSpan.FromSeconds(1), out reportStillPending), "advanced successful write completes transaction");
Equal(false, diskCommit.HasWork, "successful disk commit clears batched songs");

diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(8, 2, 4, 401, dirtyWrite, out diskAttempt), "first verified wave prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "first verified wave submits");
diskCommit.Stage("THE_HEIST");
Equal(CassetteDiskCommitOutcome.Success, diskCommit.Observe(diskAttempt, new(false, false, 11, null, null), true, TimeSpan.Zero, out reportStillPending), "first wave can complete after a later grant verifies");
Equal(true, diskCommit.HasWork, "grant verified after submission remains queued for its own disk transaction");
Equal(1, diskCommit.Songs.Count, "successful write clears only the submitted wave");
Equal("THE_HEIST", diskCommit.Songs[0], "later verified grant is never attributed to the earlier write");

diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(8, 2, 4, 401, dirtyWrite, out diskAttempt), "failure case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "failure case submits");
Equal(CassetteDiskCommitOutcome.Failure, diskCommit.Observe(diskAttempt, new(true, false, 10, 12, "IO_ERROR"), true, TimeSpan.Zero, out reportStillPending), "advanced failure fails transaction");
Equal(false, diskCommit.HasWork, "failed wave is bounded until a new epoch or newly verified song");
Equal(true, diskCommit.TryClaimDiagnostic(diskAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out CassetteDiskCommitDiagnosticContext failureTimeDiagnostic), "ordinary poll failure retains one terminal snapshot");
Equal(CassetteDiskCommitFailureKind.FailureTimeAdvanced, failureTimeDiagnostic.FailureKind, "advanced failure time has a distinct failure kind");
Equal(true, failureTimeDiagnostic.FailureDetail.Contains("baselineBits=<null>", StringComparison.Ordinal) && failureTimeDiagnostic.FailureDetail.Contains("currentBits=0x4028000000000000", StringComparison.Ordinal), "failure-time diagnostic includes exact baseline/current IEEE bits");
Equal(false, diskCommit.TryClaimDiagnostic(diskAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out _), "terminal FAILURE snapshot is bounded once");
Equal(true, diskCommit.TryGetLastTerminalAttempt(out CassetteDiskCommitAttempt ordinaryFailureTombstone), "ordinary failure preserves terminal tombstone");
Equal(diskAttempt, ordinaryFailureTombstone, "ordinary failure tombstone preserves attempt token");
diskCommit.Stage("KEEP_ON_HUSTLIN");
Equal(true, diskCommit.TryPrepare(8, 2, 4, 401, dirtyWrite, out CassetteDiskCommitAttempt postFailureAttempt), "new revision may preserve existing retry semantics after ordinary failure");
Equal(true, diskCommit.TryGetLastTerminalAttempt(out ordinaryFailureTombstone), "later preparation does not erase prior terminal tombstone");
Equal(diskAttempt, ordinaryFailureTombstone, "prior terminal tombstone remains until identity reset or later terminal outcome");
Equal(false, diskCommit.TryClaimLateEvent(diskAttempt, 4), "newer same-identity attempt permanently invalidates ambiguous late-event attribution to older tombstone");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(9, 2, 4, 401, dirtyWrite, out diskAttempt), "failure-reason case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "failure-reason case submits");
Equal(CassetteDiskCommitOutcome.Failure, diskCommit.Observe(diskAttempt, dirtyWrite with { FailureReason = "VALIDATION_FAILED" }, true, TimeSpan.Zero, out reportStillPending), "changed failure reason retains existing fail-closed outcome");
Equal(true, diskCommit.TryClaimDiagnostic(diskAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out CassetteDiskCommitDiagnosticContext failureReasonDiagnostic), "failure-reason transition retains terminal snapshot");
Equal(CassetteDiskCommitFailureKind.FailureReasonChanged, failureReasonDiagnostic.FailureKind, "changed failure reason has a distinct failure kind");
Equal(true, failureReasonDiagnostic.FailureDetail.Contains("current='VALIDATION_FAILED'", StringComparison.Ordinal), "failure-reason diagnostic includes current public reason");
foreach (CassetteDiskCommitFailureDiagnostic postFailure in new[]
{
    CassetteDiskCommitFailureDiagnostic.IdentityUnreadable("post-identity"),
    CassetteDiskCommitFailureDiagnostic.PointerMismatch(0x401, 0x402),
    CassetteDiskCommitFailureDiagnostic.StatusUnreadable("BADASS", "post-status"),
    CassetteDiskCommitFailureDiagnostic.StatusRegression("BADASS", "HAVE_NOT_EARNED"),
    CassetteDiskCommitFailureDiagnostic.FailureTimeAdvanced(8, 9),
    CassetteDiskCommitFailureDiagnostic.FailureReasonChanged("IO_ERROR", "VALIDATION_FAILED"),
})
{
    var postSubmitRuntime = new CassetteDiskCommitRuntime();
    postSubmitRuntime.Stage("BADASS");
    Equal(true, postSubmitRuntime.TryPrepare(9, 2, 4, 401, dirtyWrite, out CassetteDiskCommitAttempt postSubmitAttempt), $"post-submit {postFailure.Kind} case prepares");
    Equal(CassetteDiskCommitOutcome.HardTimeout, postSubmitRuntime.MarkSubmissionIndeterminate(postSubmitAttempt, postFailure), $"post-submit {postFailure.Kind} preserves indeterminate blocking outcome");
    Equal(true, postSubmitRuntime.TryClaimDiagnostic(postSubmitAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out CassetteDiskCommitDiagnosticContext postSubmitFailureDiagnostic), $"post-submit {postFailure.Kind} exposes one terminal FAILURE snapshot");
    Equal(postFailure.Kind, postSubmitFailureDiagnostic.FailureKind, $"post-submit {postFailure.Kind} preserves exact cause");
    Equal(postFailure.Detail, postSubmitFailureDiagnostic.FailureDetail, $"post-submit {postFailure.Kind} preserves exact detail");
    Equal(false, postSubmitRuntime.TryClaimDiagnostic(postSubmitAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out _), $"post-submit {postFailure.Kind} terminal snapshot is bounded once");
    Equal(false, postSubmitRuntime.HasWork, $"post-submit {postFailure.Kind} remains indeterminately blocked");
}
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(9, 2, 4, 401, dirtyWrite, out diskAttempt), "failed event case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "failed event case submits");
Equal(CassetteDiskCommitEventOutcome.Failure, diskCommit.ObserveWriteCompletedEvent(diskAttempt, 4, false), "matching failed event fails closed immediately");
Equal(false, diskCommit.HasWork, "failed event blocks the submitted wave");
Equal(true, diskCommit.TryClaimDiagnostic(diskAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out CassetteDiskCommitDiagnosticContext eventFailureDiagnostic), "failed completion event retains one terminal FAILURE snapshot");
Equal(CassetteDiskCommitFailureKind.EventFailure, eventFailureDiagnostic.FailureKind, "failed completion event has an exact failure kind");
Equal(false, diskCommit.TryClaimDiagnostic(diskAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out _), "failed completion event terminal snapshot is bounded once");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(9, 2, 4, 401, dirtyWrite, out diskAttempt), "hard-timeout case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "hard-timeout case submits");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.Observe(diskAttempt, dirtyWrite, true, TimeSpan.FromSeconds(10), out reportStillPending), "native write is not terminated at ten seconds");
Equal(false, reportStillPending, "ten seconds remains below the still-pending notice");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.Observe(diskAttempt, dirtyWrite, true, TimeSpan.FromSeconds(1), out reportStillPending), "native write remains pending at eleven seconds");
Equal(true, reportStillPending, "eleven seconds emits the one still-pending notice");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.Observe(diskAttempt, dirtyWrite, true, TimeSpan.FromSeconds(118.999), out reportStillPending), "transaction remains pending immediately before hard watchdog");
Equal(false, reportStillPending, "still-pending notice is emitted at most once");
diskCommit.Stage("THE_HEIST");
Equal(CassetteDiskCommitOutcome.HardTimeout, diskCommit.Observe(diskAttempt, dirtyWrite, true, TimeSpan.FromMilliseconds(1), out reportStillPending), "active-update hard watchdog expires at 130 seconds");
Equal(false, reportStillPending, "hard timeout does not duplicate the pending notice");
Equal(false, diskCommit.HasWork, "indeterminate hard-timeout wave cannot resubmit despite a newer active revision");
diskCommit.Stage("KEEP_ON_HUSTLIN");
Equal(false, diskCommit.HasWork, "newly staged work cannot unblock an indeterminate hard-timeout wave");
diskCommit.Reset();
Equal(false, diskCommit.HasWork, "identity reset clears the indeterminate transaction");
diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(10, 2, 4, 401, dirtyWrite, out diskAttempt), "epoch-switch case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "epoch-switch case submits");
Equal(CassetteDiskCommitOutcome.None, diskCommit.Observe(diskAttempt with { Epoch = 3 }, dirtyWrite, true, TimeSpan.Zero, out reportStillPending), "stale epoch token cannot mutate transaction");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(10, 3, 4, 401, dirtyWrite, out diskAttempt), "pointer-switch case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "pointer-switch case submits");
Equal(CassetteDiskCommitOutcome.None, diskCommit.Observe(diskAttempt with { Pointer = 402 }, dirtyWrite, true, TimeSpan.Zero, out reportStillPending), "stale pointer token cannot mutate transaction");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(10, 3, 4, 402, dirtyWrite, out diskAttempt), "status-regression case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "status-regression case submits");
Equal(CassetteDiskCommitOutcome.Failure, diskCommit.Observe(diskAttempt, dirtyWrite, false, CassetteDiskCommitFailureDiagnostic.StatusRegression("BADASS", "HAVE_NOT_EARNED"), TimeSpan.Zero, out reportStillPending), "cassette status regression fails closed");
Equal(true, diskCommit.TryClaimDiagnostic(diskAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out CassetteDiskCommitDiagnosticContext statusRegressionDiagnostic), "status regression retains terminal snapshot");
Equal(CassetteDiskCommitFailureKind.StatusRegression, statusRegressionDiagnostic.FailureKind, "status regression has a distinct failure kind");
Equal("song='BADASS' status='HAVE_NOT_EARNED'", statusRegressionDiagnostic.FailureDetail, "status regression records exact song and status");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(10, 3, 4, 402, dirtyWrite, out diskAttempt), "status-unreadable case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "status-unreadable case submits");
Equal(CassetteDiskCommitOutcome.Failure, diskCommit.ObserveUnavailable(diskAttempt, false, CassetteDiskCommitFailureDiagnostic.StatusUnreadable("BADASS", "status-invoke:InvalidOperationException"), TimeSpan.Zero, out reportStillPending), "unreadable status preserves existing fail-closed outcome");
Equal(true, diskCommit.TryClaimDiagnostic(diskAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out CassetteDiskCommitDiagnosticContext statusUnreadableDiagnostic), "unreadable status retains terminal snapshot");
Equal(CassetteDiskCommitFailureKind.StatusUnreadable, statusUnreadableDiagnostic.FailureKind, "unreadable status has a distinct failure kind");
Equal("song='BADASS' stage='status-invoke:InvalidOperationException'", statusUnreadableDiagnostic.FailureDetail, "unreadable status records exact song and stage");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(10, 3, 4, 402, dirtyWrite, out diskAttempt), "identity-unreadable case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "identity-unreadable case submits");
Equal(CassetteDiskCommitOutcome.Failure, diskCommit.ObserveUnavailable(diskAttempt, false, CassetteDiskCommitFailureDiagnostic.IdentityUnreadable("processor-identity-obtain-state-invoke"), TimeSpan.Zero, out reportStillPending), "unreadable identity preserves existing fail-closed outcome");
Equal(true, diskCommit.TryClaimDiagnostic(diskAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out CassetteDiskCommitDiagnosticContext identityUnreadableDiagnostic), "unreadable identity retains terminal snapshot");
Equal(CassetteDiskCommitFailureKind.IdentityUnreadable, identityUnreadableDiagnostic.FailureKind, "unreadable identity has a distinct failure kind");
Equal("stage='processor-identity-obtain-state-invoke'", identityUnreadableDiagnostic.FailureDetail, "unreadable identity records exact stage");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(10, 3, 4, 402, dirtyWrite, out diskAttempt), "pointer-mismatch case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "pointer-mismatch case submits");
Equal(CassetteDiskCommitOutcome.Failure, diskCommit.ObserveUnavailable(diskAttempt, false, CassetteDiskCommitFailureDiagnostic.PointerMismatch(0x402, 0x403), TimeSpan.Zero, out reportStillPending), "pointer mismatch preserves existing fail-closed outcome");
Equal(true, diskCommit.TryClaimDiagnostic(diskAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out CassetteDiskCommitDiagnosticContext pointerMismatchDiagnostic), "pointer mismatch retains terminal snapshot");
Equal(CassetteDiskCommitFailureKind.PointerMismatch, pointerMismatchDiagnostic.FailureKind, "pointer mismatch has a distinct failure kind");
Equal("expected=0x402 observed=0x403", pointerMismatchDiagnostic.FailureDetail, "pointer mismatch records exact pointers");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(10, 3, 4, 402, dirtyWrite, out diskAttempt), "unreadable-state timeout case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "unreadable-state timeout case submits");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.ObserveUnavailable(diskAttempt, true, TimeSpan.FromSeconds(11), out reportStillPending), "temporarily unreadable write state remains pending within the hard bound");
Equal(true, reportStillPending, "unreadable public state emits the same bounded pending notice");
Equal(CassetteDiskCommitOutcome.HardTimeout, diskCommit.ObserveUnavailable(diskAttempt, true, TimeSpan.FromSeconds(119), out reportStillPending), "unreadable write state cannot postpone hard timeout indefinitely");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryPrepare(10, 3, 4, 402, dirtyWrite, out diskAttempt), "unreadable-state identity case prepares"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "unreadable-state identity case submits");
Equal(CassetteDiskCommitOutcome.None, diskCommit.ObserveUnavailable(diskAttempt with { Pointer = 403 }, true, TimeSpan.Zero, out reportStillPending), "stale identity token cannot mutate transaction when write state is unreadable");
diskCommit.Reset(); diskCommit.Stage("BADASS");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.ObservePreSubmitUnavailable(4, 4, 404, false, 0, false, TimeSpan.FromSeconds(9), out bool reportDeferred), "pre-submit state acquisition remains pending within its bound");
Equal(true, reportDeferred, "pre-submit acquisition reports its first deferred read");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.ObservePreSubmitUnavailable(4, 4, 404, false, 0, false, TimeSpan.FromMilliseconds(999), out reportDeferred), "pre-submit acquisition remains pending immediately before timeout");
Equal(false, reportDeferred, "repeated pre-submit failure is not reported again");
Equal(CassetteDiskCommitOutcome.Timeout, diskCommit.ObservePreSubmitUnavailable(4, 4, 404, false, 0, false, TimeSpan.FromMilliseconds(1), out reportDeferred), "pre-submit acquisition times out after ten seconds");
Equal(false, reportDeferred, "terminal timeout does not duplicate the initial deferred report");
Equal(false, diskCommit.HasWork, "timed-out pre-submit wave is blocked");
Equal(CassetteDiskCommitOutcome.None, diskCommit.ObservePreSubmitUnavailable(4, 4, 404, false, 0, false, TimeSpan.FromSeconds(10), out reportDeferred), "blocked pre-submit wave cannot restart itself");
Equal(false, reportDeferred, "blocked pre-submit wave emits no further deferred reports");
diskCommit.Stage("THE_HEIST");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.ObservePreSubmitUnavailable(4, 4, 404, false, 0, false, TimeSpan.Zero, out reportDeferred), "new revision unblocks pre-submit acquisition");
Equal(true, reportDeferred, "new work revision receives one new deferred report");
diskCommit.Reset(); diskCommit.Stage("BADASS");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.ObservePreSubmitUnavailable(5, 4, 405, true, 405, true, TimeSpan.Zero, out reportDeferred), "matching identity begins pre-submit acquisition");
Equal(CassetteDiskCommitOutcome.Cancelled, diskCommit.ObservePreSubmitUnavailable(5, 4, 405, true, 406, false, TimeSpan.Zero, out reportDeferred), "readable pointer change fails closed during pre-submit acquisition");
Equal(false, diskCommit.HasWork, "identity-changed pre-submit wave is blocked until reset");
diskCommit.Reset(); diskCommit.Stage("BADASS");
Equal(CassetteDiskCommitOutcome.Failure, diskCommit.ObservePreSubmitUnavailable(6, 4, 406, true, 406, false, TimeSpan.Zero, out reportDeferred), "status regression fails closed before submission");
diskCommit.Reset(); diskCommit.Stage("BADASS");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.ObservePreSubmitUnavailable(7, 4, 407, true, 407, true, TimeSpan.FromSeconds(9), out reportDeferred), "identity reset starts a fresh acquisition bound");
Equal(true, diskCommit.TryPrepare(11, 7, 4, 407, dirtyWrite, out diskAttempt), "readable state prepares transaction after pre-submit acquisition"); Equal(true, diskCommit.MarkSubmitted(diskAttempt, dirtyWrite), "readable state submits after acquisition");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.Observe(diskAttempt, dirtyWrite, true, TimeSpan.FromSeconds(9), out reportStillPending), "pre-submit acquisition time is not charged to submitted write confirmation");
diskCommit.Reset();
Equal(false, diskCommit.TryPrepare(12, 3, 4, 402, dirtyWrite, out _), "no verified songs means no persist");
var alreadyDurableEpoch = new CassetteSaveEpochRuntime(); alreadyDurableEpoch.Receive("BADASS"); alreadyDurableEpoch.ActivateSave(4);
alreadyDurableEpoch.Observe("BADASS", CassetteRandomizationPolicy.HaveInBag);
Equal(false, alreadyDurableEpoch.IsPending("BADASS"), "already durable cassette status queues no grant or disk transaction");
Console.WriteLine("PASS: cassette_disk_commit_transaction_is_bounded");


foreach (var triggerGroup in CassetteCatalog.All
             .Where(x => x.SourceType == CassetteSourceType.LevelEarnedReward)
             .SelectMany(x => x.Triggers)
             .Distinct()
             .OrderBy(x => x.Level, StringComparer.Ordinal)
             .ThenBy(x => x.Variant, StringComparer.Ordinal))
{
    var mapped = CassetteCatalog.ForLevelSource(triggerGroup.Level, triggerGroup.Variant);
    var statuses = mapped.ToDictionary(x => x.NativeSong, _ => CassetteRandomizationPolicy.HaveNotEarned, StringComparer.Ordinal);
    var aliasDecision = CassetteRandomizationPolicy.DecideLevelEvaluation(triggerGroup.Level, triggerGroup.Variant, true, statuses);
    Equal(false, aliasDecision.AllowNative, $"verified alias {triggerGroup.Level}/{triggerGroup.Variant} intercepts");
    SequenceEqual(mapped.Select(x => x.SourceName), aliasDecision.SourceLocationsToQueue, $"verified alias {triggerGroup.Level}/{triggerGroup.Variant} queues exact sources");
}
PlayerSaveManagementEnquiries.SelectedSlot = new FakeIl2CppNullable<int>(hasValue: false, value: 0);
PlayerSaveManagementEnquiries.SelectedState = new object();
Equal(false, CassetteSaveTransactionAdapter.TryGetLoadedSave(out _), "loaded save requires selected slot number");
PlayerSaveManagementEnquiries.SelectedSlot = new FakeIl2CppNullable<int>(hasValue: true, value: 4);
PlayerSaveManagementEnquiries.SelectedState = null;
Equal(false, CassetteSaveTransactionAdapter.TryGetLoadedSave(out _), "loaded save requires selected slot state");
PlayerSaveManagementEnquiries.SelectedState = new FakePlayerSavePublicState(new IntPtr(0x400));
Equal(true, CassetteSaveTransactionAdapter.TryGetLoadedSave(out int selectedSlot), "loaded save accepts matching slot and state enquiries");
Equal(4, selectedSlot, "loaded save returns selected slot number");
Equal(true, CassetteSaveTransactionAdapter.TryGetLoadedSaveIdentity(out int identitySlot, out long identityPointer), "loaded save identity reads public native pointer");
Equal(4, identitySlot, "loaded save identity preserves slot");
Equal(0x400L, identityPointer, "loaded save identity preserves pointer");
Equal(true, CassetteSaveTransactionAdapter.TryGetLoadedSaveIdentity(out _, out _, out string identityStage), "diagnostic identity overload succeeds");
Equal("success", identityStage, "identity success stage is bounded");
PlayerSaveManagementEnquiries.SelectedSlot = new FakeIl2CppNullable<int>(hasValue: false, value: 0);
Equal(false, CassetteSaveTransactionAdapter.TryGetLoadedSaveIdentity(out _, out _, out identityStage), "empty slot fails closed");
Equal("slot-empty", identityStage, "empty slot has exact stage");
PlayerSaveManagementEnquiries.SelectedSlot = new FakeIl2CppNullable<int>(hasValue: true, value: 4);
PlayerSaveManagementEnquiries.SelectedState = null;
Equal(false, CassetteSaveTransactionAdapter.TryGetLoadedSaveIdentity(out _, out _, out identityStage), "null state fails closed");
Equal("state-null", identityStage, "null state has exact stage");
PlayerSaveManagementEnquiries.SelectedState = new object();
Equal(false, CassetteSaveTransactionAdapter.TryGetLoadedSaveIdentity(out _, out _, out identityStage), "missing pointer fails closed");
Equal("pointer-missing", identityStage, "missing pointer has exact stage");
PlayerSaveManagementEnquiries.SelectedState = new FakePlayerSavePublicState(IntPtr.Zero);
Equal(false, CassetteSaveTransactionAdapter.TryGetLoadedSaveIdentity(out _, out _, out identityStage), "zero pointer fails closed");
Equal("pointer-zero", identityStage, "zero pointer has exact stage");

var transactionState = new TransactionSaveState(eSongCassetteStatus.HAVE_IN_BAG);
var transactionProcessor = new PlayerSaveRequestProcessor(transactionState);
Equal(true, CassetteSaveTransactionAdapter.TryReadCassetteStatus(transactionProcessor, nameof(ePlayableSong.QUIERES_BAILAR), out string? transactionStatus), "transaction adapter reads current processor state");
Equal(nameof(eSongCassetteStatus.HAVE_IN_BAG), transactionStatus, "transaction adapter returns authoritative cassette status");
Equal(1, transactionProcessor.ObtainStateCalls, "transaction read obtains current state for each call");
Equal(true, CassetteSaveTransactionAdapter.TryGetProcessorSaveIdentity(transactionProcessor, out long authorityPointer, out string authorityStage), "authoritative identity reads processor-local public state");
Equal(0x700L, authorityPointer, "authoritative identity returns processor-local pointer");
Equal("success", authorityStage, "authoritative identity reports success");
Equal(false, CassetteSaveTransactionAdapter.TryGetProcessorSaveIdentity(new PrivateOnlyFixture.PlayerSaveRequestProcessor(), out _, out authorityStage), "authoritative identity rejects private-only ObtainState");
Equal("processor-identity-obtain-state-missing", authorityStage, "authoritative identity fails closed at public method boundary");

var slot3SaveProcessor = new SaveDataProcessorFixture(new SaveDataStateFixture(
    new Dictionary<int, RegularSaveStateFixture?>
    {
        [3] = new(new IntPtr(0x700), 3000),
        [4] = new(new IntPtr(0x800), 4000),
    }));
Equal(true, CassetteSaveTransactionAdapter.TryMatchRegularSaveSlot(slot3SaveProcessor, transactionProcessor, out int matchedSlot, out IReadOnlyList<CassetteRegularSavePointerEntry> regularEntries, out string joinStage), "pointer join finds unique processor-local regular save");
Equal(3, matchedSlot, "pointer join identifies UI slot 3");
Equal(2, regularEntries.Count, "pointer join returns bounded regular-save fingerprints");
Equal("success", joinStage, "unique pointer join succeeds");
var slot4JoinProcessor = new PlayerSaveRequestProcessor(new TransactionSaveState(eSongCassetteStatus.HAVE_IN_BAG, new IntPtr(0x800)));
Equal(true, CassetteSaveTransactionAdapter.TryMatchRegularSaveSlot(slot3SaveProcessor, slot4JoinProcessor, out matchedSlot, out _, out joinStage), "pointer join supports a distinct slot pointer");
Equal(4, matchedSlot, "pointer join identifies UI slot 4");
var startupAuthority = new CassetteProcessorSaveIdentityStabilizer();
startupAuthority.SuspendUnresolved();
long resolvedStartupGeneration = startupAuthority.Signal(matchedSlot, CassetteSaveBoundarySignalKind.Selection, slot4JoinProcessor);
Equal<CassetteSaveActivation?>(null, startupAuthority.Observe(resolvedStartupGeneration, slot4JoinProcessor, true, 0x800, "success"), "startup slot 4 cannot activate on join alone or first pointer sample");
var startupSlot4Activation = startupAuthority.Observe(resolvedStartupGeneration, slot4JoinProcessor, true, 0x800, "success");
Equal(4, startupSlot4Activation!.Value.Slot, "startup slot 4 activates only after unique join and two stable samples");
var zeroMatchProcessor = new PlayerSaveRequestProcessor(new TransactionSaveState(eSongCassetteStatus.HAVE_IN_BAG, new IntPtr(0x900)));
Equal(false, CassetteSaveTransactionAdapter.TryMatchRegularSaveSlot(slot3SaveProcessor, zeroMatchProcessor, out _, out _, out joinStage), "zero pointer matches fail closed");
Equal("match-count:0", joinStage, "zero match has explicit stage");
startupAuthority.SuspendUnresolved();
Equal(true, startupAuthority.Capture().Pending, "failed startup join leaves prior epoch suspended");
Equal<int?>(null, startupAuthority.Capture().ExpectedSlot, "failed startup join never invents UI slot");
var duplicateSaveProcessor = new SaveDataProcessorFixture(new SaveDataStateFixture(new Dictionary<int, RegularSaveStateFixture?>
{
    [3] = new(new IntPtr(0x700), 3000),
    [4] = new(new IntPtr(0x700), 4000),
}));
Equal(false, CassetteSaveTransactionAdapter.TryMatchRegularSaveSlot(duplicateSaveProcessor, transactionProcessor, out _, out _, out joinStage), "duplicate pointer matches fail closed");
Equal("match-count:2", joinStage, "duplicate match has explicit stage");
var nullEntryProcessor = new SaveDataProcessorFixture(new SaveDataStateFixture(new Dictionary<int, RegularSaveStateFixture?> { [3] = null }));
Equal(false, CassetteSaveTransactionAdapter.TryMatchRegularSaveSlot(nullEntryProcessor, transactionProcessor, out _, out _, out joinStage), "null regular save fails closed");
Equal("entry-value-null", joinStage, "null entry has explicit stage");
Equal(false, CassetteSaveTransactionAdapter.TryMatchRegularSaveSlot(new SaveDataProcessorFixture(new BadKeySaveDataStateFixture()), transactionProcessor, out _, out _, out joinStage), "slot conversion failure fails closed");
Equal(true, joinStage.StartsWith("entry-key-convert:", StringComparison.Ordinal), "slot conversion failure has explicit stage");
Equal(false, CassetteSaveTransactionAdapter.TryMatchRegularSaveSlot(new SaveDataProcessorFixture(new ThrowingSaveDataStateFixture()), transactionProcessor, out _, out _, out joinStage), "enumeration failure fails closed");
Equal(true, joinStage.StartsWith("enumeration:", StringComparison.Ordinal), "enumeration failure has explicit stage");
foreach (var malformed in new (object State, string Stage)[]
{
    (new MissingKeySaveDataStateFixture(), "entry-key-missing"),
    (new NullKeySaveDataStateFixture(), "entry-key-null"),
    (new MissingGameStatsSaveDataStateFixture(), "entry-3-game-stats-missing"),
    (new NullGameStatsSaveDataStateFixture(), "entry-3-game-stats-null"),
    (new MissingLastPlaySaveDataStateFixture(), "entry-3-last-play-missing"),
    (new NullLastPlaySaveDataStateFixture(), "entry-3-last-play-null"),
    (new MissingTicksSaveDataStateFixture(), "entry-3-ticks-missing"),
    (new NullTicksSaveDataStateFixture(), "entry-3-ticks-null"),
})
{
    Equal(false, CassetteSaveTransactionAdapter.TryMatchRegularSaveSlot(new SaveDataProcessorFixture(malformed.State), transactionProcessor, out _, out _, out joinStage), $"{malformed.Stage} fails closed");
    Equal(malformed.Stage, joinStage, $"{malformed.Stage} is reported exactly");
}

var semanticProcessor = new PlayerSaveRequestProcessor(new TransactionSaveState(eSongCassetteStatus.INVALID));
Equal(true, CassetteSaveTransactionAdapter.IsCompatiblePlayerSaveRequestProcessor(semanticProcessor), "exact player processor is compatible");
Equal(true, CassetteSaveTransactionAdapter.TrySubmitHaveInBag(semanticProcessor, nameof(ePlayableSong.QUIERES_BAILAR), out string submitDetail), "adapter submits semantic cassette request");
Equal(1, semanticProcessor.ProcessRequestCalls, "submission processes exactly one semantic request");
Equal(ePlayableSong.QUIERES_BAILAR, semanticProcessor.LastRequest!.Song, "request receives native song");
Equal(eSongCassetteStatus.HAVE_IN_BAG, semanticProcessor.LastRequest.CassetteStatus, "request receives bag status");
Equal(false, semanticProcessor.LastRequest.Bundle.HasValue, "submitted request carries a verified absent native bundle");
Equal(ePlayerSaveChangeBundleKey.DEFAULT, semanticProcessor.EffectiveBundle, "processor-shaped native fallback resolves absent bundle to DEFAULT");
Equal(true, semanticProcessor.LastRequest.ParameterlessConstructorUsed, "submission uses the public parameterless cassette constructor");
Equal(0, RecordSongCassetteStatusInSaveDataRequest.SemanticConstructorCalls, "submission never invokes the broken three-argument constructor");
Equal(0, semanticProcessor.LastRequest.BundleSetterCalls, "submission never invokes the broken nullable setter");
Equal("actualSong='QUIERES_BAILAR' actualStatus='HAVE_IN_BAG' actualBundle='present=False value=<null>' effectiveBundle='DEFAULT/1(native absent fallback)'", submitDetail, "submission detail reports verified absence and the native fallback");
var publicWriteProcessor = new PublicWriteProcessorFixture(new PublicWriteStateFixture());
Equal(true, CassetteSaveTransactionAdapter.TryReadPublicWriteState(publicWriteProcessor, out CassettePublicWriteState publicWriteState, out string publicWriteStage), "adapter reads the public write-completion contract");
Equal(new CassettePublicWriteState(true, false, 10, 8, "IO_ERROR"), publicWriteState, "adapter returns exact public write-completion values");
Equal("success", publicWriteStage, "public write-completion read reports success");
GameTimeEnquiries.CurrentGameTime = new FakeGameTime(10.5);
Equal(true, CassetteSaveTransactionAdapter.TryReadPublicWriteDiagnosticState(publicWriteProcessor, 0x700, new[] { nameof(ePlayableSong.QUIERES_BAILAR) }, out CassettePublicWriteDiagnosticState publicDiagnosticState, out string publicDiagnosticStage), "adapter reads one public diagnostic save-state snapshot");
Equal(publicWriteState, publicDiagnosticState.WriteState, "diagnostic state preserves the exact public write state");
Equal(0x700L, publicDiagnosticState.StatePointer, "diagnostic state preserves the public state pointer");
Equal(nameof(eSongCassetteStatus.HAVE_IN_BAG), publicDiagnosticState.Statuses[nameof(ePlayableSong.QUIERES_BAILAR)], "diagnostic state reads statuses from the same public state object");
Equal(10.5, publicDiagnosticState.CurrentGameTime, "diagnostic state reads public current GameTime");
Equal(true, publicDiagnosticState.HasUnstagedChanges, "diagnostic state reads public unstaged-change flag");
Equal(2, publicDiagnosticState.RedundancyBundleIndex, "diagnostic state reads public redundancy index");
Equal(7, publicDiagnosticState.RedundancyBundleRevision, "diagnostic state reads public redundancy revision");
Equal("success", publicDiagnosticStage, "public diagnostic state reports success");
Equal(true, CassetteSaveTransactionAdapter.TryReadPublicWriteLifecycleDiagnosticState(publicWriteProcessor, new[] { nameof(ePlayableSong.QUIERES_BAILAR) }, out CassettePublicWriteDiagnosticState lifecycleDiagnosticState, out string lifecycleDiagnosticStage), "lifecycle diagnostic reads one public state object without rejecting a rebuilt pointer");
Equal(0x700L, lifecycleDiagnosticState.StatePointer, "lifecycle diagnostic reports the observed public pointer");
Equal(2, lifecycleDiagnosticState.RedundancyBundleIndex, "lifecycle diagnostic reports observed redundancy index");
Equal(7, lifecycleDiagnosticState.RedundancyBundleRevision, "lifecycle diagnostic reports observed redundancy revision");
Equal(10d, lifecycleDiagnosticState.WriteState.LastSuccessTime, "lifecycle diagnostic reports nullable public success time");
Equal(8d, lifecycleDiagnosticState.WriteState.LastFailureTime, "lifecycle diagnostic reports nullable public failure time");
Equal(nameof(eSongCassetteStatus.HAVE_IN_BAG), lifecycleDiagnosticState.Statuses[nameof(ePlayableSong.QUIERES_BAILAR)], "lifecycle diagnostic reports active-song status from the same public state object");
Equal("success", lifecycleDiagnosticStage, "lifecycle diagnostic reports success");
var recordRoutingRequest = new RoutingFixture.RecordSongCassetteStatusInSaveDataRequest(
    ePlayableSong.BADASS,
    eSongCassetteStatus.HAVE_IN_BAG,
    new(true, ePlayerSaveChangeBundleKey.DEFAULT));
Equal(true, CassetteSaveTransactionAdapter.TryReadRecordSongCassetteStatusRequest(recordRoutingRequest, out CassetteRecordSongRoutingPayload recordPayload, out string routingStage), "record-song diagnostic reads the exact public request payload");
Equal(new CassetteRecordSongRoutingPayload(nameof(ePlayableSong.BADASS), nameof(eSongCassetteStatus.HAVE_IN_BAG), true, nameof(ePlayerSaveChangeBundleKey.DEFAULT)), recordPayload, "record-song payload preserves song, status, and nullable bundle");
Equal("success", routingStage, "record-song payload reports success");
Equal(true, CassetteSaveTransactionAdapter.TryReadRecordSongCassetteStatusRequest(new RoutingFixture.RecordSongCassetteStatusInSaveDataRequest(ePlayableSong.BADASS, eSongCassetteStatus.HAVE_IN_BAG, new(false, default)), out recordPayload, out routingStage), "record-song diagnostic preserves an empty public nullable bundle");
Equal(false, recordPayload.BundlePresent, "empty record-song bundle remains absent");
Equal<string?>(null, recordPayload.Bundle, "empty record-song bundle does not invent DEFAULT");
Equal(true, CassetteSaveTransactionAdapter.TryReadRecordSongCassetteStatusRequest(new EmptyInteropRoutingFixture.RecordSongCassetteStatusInSaveDataRequest(), out recordPayload, out routingStage), "proven IL2CPP empty record-song bundle is normalized diagnostically");
Equal(false, recordPayload.BundlePresent, "IL2CPP empty record-song bundle remains absent");
Equal(false, CassetteSaveTransactionAdapter.TryReadRecordSongCassetteStatusRequest(new ThrowingRoutingFixture.RecordSongCassetteStatusInSaveDataRequest(), out _, out routingStage), "throwing public record-song status fails diagnostically");
Equal("routing-record-status-get-invocation:InvalidOperationException:record-status", routingStage, "throwing record-song status identifies its exact stage");
var persistRoutingRequest = new RoutingFixture.PersistSaveChangeBundleRequest(new(true, ePlayerSaveChangeBundleKey.DEFAULT), eSaveFileWriteType.URGENT);
Equal(true, CassetteSaveTransactionAdapter.TryReadPersistSaveChangeBundleRequest(persistRoutingRequest, out CassettePersistBundleRoutingPayload persistPayload, out routingStage), "narrow persist diagnostic reads the exact public request payload");
Equal(new CassettePersistBundleRoutingPayload(true, nameof(ePlayerSaveChangeBundleKey.DEFAULT), nameof(eSaveFileWriteType.URGENT)), persistPayload, "narrow persist payload preserves nullable bundle and write type");
Equal(true, CassetteSaveTransactionAdapter.TryReadPersistSaveChangeBundleRequest(new RoutingFixture.PersistSaveChangeBundleRequest(new(false, default), eSaveFileWriteType.URGENT), out persistPayload, out routingStage), "narrow persist diagnostic preserves an absent managed nullable bundle");
Equal(false, persistPayload.BundlePresent, "absent managed narrow-persist bundle remains absent");
Equal<string?>(null, persistPayload.Bundle, "absent managed narrow-persist bundle does not invent DEFAULT");
Equal(true, CassetteSaveTransactionAdapter.TryReadPersistSaveChangeBundleRequest(new EmptyInteropRoutingFixture.PersistSaveChangeBundleRequest(), out persistPayload, out routingStage), "proven IL2CPP empty narrow-persist bundle is normalized diagnostically");
Equal(false, persistPayload.BundlePresent, "IL2CPP empty narrow-persist bundle remains absent");
Equal(false, CassetteSaveTransactionAdapter.TryReadPersistSaveChangeBundleRequest(new ThrowingRoutingFixture.PersistSaveChangeBundleRequest(), out _, out routingStage), "throwing public narrow-persist bundle fails diagnostically");
Equal("routing-persist-bundle-get-invocation:InvalidOperationException:persist-bundle", routingStage, "throwing narrow-persist bundle identifies its exact stage");
Equal(false, CassetteSaveTransactionAdapter.TryReadPersistSaveChangeBundleRequest(new ThrowingWriteTypeRoutingFixture.PersistSaveChangeBundleRequest(), out _, out routingStage), "throwing public narrow-persist write type fails diagnostically");
Equal("routing-persist-write-type-get-invocation:InvalidOperationException:persist-write-type", routingStage, "throwing narrow-persist write type identifies its exact stage");
Equal(false, CassetteSaveTransactionAdapter.TryReadPersistSaveChangeBundleRequest(new EmptyRoutingFixture.PersistSaveChangeBundleRequest(), out _, out routingStage), "empty public narrow-persist write type fails diagnostically");
Equal("routing-persist-write-type-empty", routingStage, "empty narrow-persist write type identifies its exact stage");
Equal(true, CassetteSaveTransactionAdapter.TryReadPersistAllSaveChangeBundlesRequest(new PersistAllSaveChangeBundlesRequest(eSaveFileWriteType.NON_URGENT), out string? persistAllWriteType, out routingStage), "persist-all diagnostic reads its public write type");
Equal(nameof(eSaveFileWriteType.NON_URGENT), persistAllWriteType, "persist-all diagnostic preserves actual write type");
Equal(false, CassetteSaveTransactionAdapter.TryReadPersistAllSaveChangeBundlesRequest(new ThrowingRoutingFixture.PersistAllSaveChangeBundlesRequest(), out _, out routingStage), "throwing public persist-all write type fails diagnostically");
Equal("routing-persist-all-write-type-get-invocation:InvalidOperationException:persist-all-write-type", routingStage, "throwing persist-all write type identifies its exact stage");
Equal(false, CassetteSaveTransactionAdapter.TryReadPersistAllSaveChangeBundlesRequest(new EmptyRoutingFixture.PersistAllSaveChangeBundlesRequest(), out _, out routingStage), "empty public persist-all write type fails diagnostically");
Equal("routing-persist-all-write-type-empty", routingStage, "empty persist-all write type identifies its exact stage");
Equal(true, CassetteSaveTransactionAdapter.TryReadPlayerSaveBundleRoutingState(publicWriteProcessor, 0x700, new[] { nameof(ePlayableSong.QUIERES_BAILAR) }, out CassetteBundleRoutingState playerRoutingState, out routingStage), "record-song boundary reads one coherent public player state");
Equal(0x700L, playerRoutingState.StatePointer, "record-song boundary preserves exact public state pointer");
Equal(true, playerRoutingState.HasUnstagedChanges, "record-song boundary preserves public unstaged flag");
Equal(nameof(eSongCassetteStatus.HAVE_IN_BAG), playerRoutingState.Statuses[nameof(ePlayableSong.QUIERES_BAILAR)], "record-song boundary reads status from the same state object");
var saveDataRoutingProcessor = new SaveDataBundleRoutingProcessorFixture(
    new SaveDataBundleRoutingStateFixture(new(true, 4), new() { [4] = new PublicWriteStateFixture() }));
Equal(true, CassetteSaveTransactionAdapter.TryReadSaveDataBundleRoutingState(saveDataRoutingProcessor, 0x700, new[] { nameof(ePlayableSong.QUIERES_BAILAR) }, out int routingSlot, out CassetteBundleRoutingState saveDataRoutingState, out routingStage), "save-data boundary reads its own selected public player state");
Equal(4, routingSlot, "save-data boundary preserves actual public selected slot");
Equal(0x700L, saveDataRoutingState.StatePointer, "save-data boundary correlates the expected player-state pointer");
Equal(true, saveDataRoutingState.HasUnstagedChanges, "save-data boundary preserves public unstaged flag");
Equal(false, CassetteSaveTransactionAdapter.TryReadSaveDataBundleRoutingState(new SaveDataBundleRoutingProcessorFixture(new MissingSelectedSlotSaveDataBundleRoutingStateFixture()), 0x700, Array.Empty<string>(), out _, out _, out routingStage), "missing public selected slot fails at the exact routing stage");
Equal("routing-save-data-selected-slot-missing", routingStage, "missing public selected slot has an exact stage");
var playerTargetState = new DiskCommitTargetPlayerStateFixture(
    new IntPtr(0x700), eSongCassetteStatus.HAVE_IN_BAG, eSongCassetteStatus.INVALID,
    hasUnstagedChanges: true, hasChanges: true, requiresWriteToDisk: true, defaultBundleChanges: 2);
var selectedTargetState = new DiskCommitTargetPlayerStateFixture(
    new IntPtr(0x900), eSongCassetteStatus.INVALID, eSongCassetteStatus.HAVE_IN_BAG,
    hasUnstagedChanges: false, hasChanges: true, requiresWriteToDisk: true, defaultBundleChanges: null);
var expectedTargetState = new DiskCommitTargetPlayerStateFixture(
    new IntPtr(0x700), eSongCassetteStatus.HAVE_IN_BAG, eSongCassetteStatus.INVALID,
    hasUnstagedChanges: true, hasChanges: true, requiresWriteToDisk: true, defaultBundleChanges: 2);
var targetPlayerProcessor = new DiskCommitTargetPlayerProcessorFixture(new IntPtr(0x111), playerTargetState);
var targetSaveDataState = new DiskCommitTargetSaveDataStateFixture(
    new IntPtr(0x333), new(true, 3), new() { [3] = selectedTargetState, [4] = expectedTargetState });
var retainedSaveDataProcessor = new SaveDataRequestProcessor(new IntPtr(0x222), targetSaveDataState);
var registeredPersistProcessor = new SaveDataRequestProcessor(new IntPtr(0x444), targetSaveDataState);
var registeredPersistProcessorWrapper = new RequestProcessor(new IntPtr(0x444), registeredPersistProcessor);
RequestSystem.SetRegisteredProcessors(new DirectLookupRegistryFixture(new()
{
    [new PublicTypeKeyFixture(nameof(PersistSaveChangeBundleRequest))] = new RegisteredRequestProcessor(registeredPersistProcessorWrapper),
}));
bool targetReadable = CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    new[] { nameof(ePlayableSong.QUIERES_BAILAR) },
    out CassetteDiskCommitTargetDiagnosticState targetDiagnostic, out int targetRegistryCount, out string targetStage);
Equal("success", targetStage, "target diagnostic reports success");
Equal(true, targetReadable, "target diagnostic reads player and global selected save targets without treating their mismatch as a transaction failure");
Equal(1, targetRegistryCount, "target diagnostic preserves the public registry Count");
Equal(typeof(PersistSaveChangeBundleRequest), Il2CppType.LastConvertedType, "target diagnostic converts the exact PersistSaveChangeBundleRequest System.Type");
Equal(1, targetPlayerProcessor.ObtainStateCalls, "target diagnostic obtains the player-side state exactly once");
Equal(0, retainedSaveDataProcessor.ObtainStateCalls, "retained joined SaveData processor is identity-only and is not substituted for the registered persist target");
Equal(1, registeredPersistProcessor.ObtainStateCalls, "target diagnostic obtains SaveData state exactly once from the actually registered persist processor");
Equal(1, registeredPersistProcessorWrapper.TryCastCalls, "target diagnostic rewraps the registered base Processor exactly once");
Equal(typeof(SaveDataRequestProcessor), registeredPersistProcessorWrapper.LastTryCastType, "target diagnostic requests the exact public SaveDataRequestProcessor cast type");
Equal(0, RequestSystem.SubmitCount, "target diagnostic does not submit any request");
Equal(0x111L, targetDiagnostic.PlayerProcessorPointer, "target diagnostic reads the retained player processor pointer");
Equal(0x444L, targetDiagnostic.RegisteredPersistProcessorPointer, "target diagnostic reads the actually registered persist processor pointer");
Equal(0x222L, targetDiagnostic.RetainedSaveDataProcessorPointer, "target diagnostic reads the retained joined save-data processor pointer");
Equal(0x333L, targetDiagnostic.SaveDataStatePointer, "target diagnostic reads the save-data state pointer");
Equal(3, targetDiagnostic.SelectedPlayerSaveSlot, "target diagnostic preserves the global selected slot even when it differs from the attempt slot");
Equal(0x900L, targetDiagnostic.SelectedEntryPointer, "target diagnostic preserves the selected entry pointer");
Equal(0x700L, targetDiagnostic.ExpectedSlotEntryPointer, "target diagnostic independently preserves the expected-slot entry pointer");
Equal(0x700L, targetDiagnostic.Player.StatePointer, "target diagnostic reads player ObtainState once and preserves its pointer");
Equal(nameof(eSongCassetteStatus.HAVE_IN_BAG), targetDiagnostic.Player.EffectiveStatuses[nameof(ePlayableSong.QUIERES_BAILAR)], "player-side effective cassette status comes from the coherent player state");
Equal(nameof(eSongCassetteStatus.INVALID), targetDiagnostic.Player.CanonicalStatuses[nameof(ePlayableSong.QUIERES_BAILAR)], "player-side canonical cassette status comes from GameProgression on the same state");
Equal(true, targetDiagnostic.Player.DefaultBundlePresent, "player-side target diagnostic observes the DEFAULT bundle");
Equal(2, targetDiagnostic.Player.DefaultBundleChangeCount, "player-side target diagnostic observes the DEFAULT bundle change count");
Equal(false, targetDiagnostic.Selected.DefaultBundlePresent, "selected-side target diagnostic observes an absent DEFAULT bundle");
Equal(0, targetDiagnostic.Selected.DefaultBundleChangeCount, "selected-side absent DEFAULT bundle has zero changes");
Equal(nameof(eSongCassetteStatus.INVALID), targetDiagnostic.Selected.EffectiveStatuses[nameof(ePlayableSong.QUIERES_BAILAR)], "selected-side effective cassette status comes from the coherent selected state");
Equal(nameof(eSongCassetteStatus.HAVE_IN_BAG), targetDiagnostic.Selected.CanonicalStatuses[nameof(ePlayableSong.QUIERES_BAILAR)], "selected-side canonical cassette status comes from GameProgression on the same state");
var missingSelectedTargetSaveDataState = new DiskCommitTargetSaveDataStateFixture(
    new IntPtr(0x333), new(true, 3), new() { [4] = expectedTargetState });
var missingSelectedRegisteredProcessor = new SaveDataRequestProcessor(new IntPtr(0x444), missingSelectedTargetSaveDataState);
var missingSelectedRegisteredWrapper = new RequestProcessor(new IntPtr(0x444), missingSelectedRegisteredProcessor);
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(missingSelectedRegisteredWrapper));
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    new[] { nameof(ePlayableSong.QUIERES_BAILAR) },
    out CassetteDiskCommitTargetDiagnosticState missingSelectedDiagnostic,
    out int missingSelectedRegistryCount,
    out string missingSelectedStage),
    "missing selected save entry fails only the later observational lookup");
Equal("target-selected-entry-missing", missingSelectedStage, "missing selected save entry retains its exact failure stage");
Equal(1, missingSelectedRegistryCount, "missing selected entry preserves the already-read registry Count");
Equal(0x111L, missingSelectedDiagnostic.PlayerProcessorPointer, "missing selected entry preserves the already-read player processor pointer");
Equal(0x444L, missingSelectedDiagnostic.RegisteredPersistProcessorPointer, "missing selected entry preserves the already-read registered Persist processor pointer");
Equal(0x222L, missingSelectedDiagnostic.RetainedSaveDataProcessorPointer, "missing selected entry preserves the already-read retained SaveData processor pointer");
Equal(0x333L, missingSelectedDiagnostic.SaveDataStatePointer, "missing selected entry preserves the already-read SaveData state pointer");
Equal(3, missingSelectedDiagnostic.SelectedPlayerSaveSlot, "missing selected entry preserves the already-read selected slot");
Equal(0x700L, missingSelectedDiagnostic.Player.StatePointer, "missing selected entry preserves the coherent player-side snapshot");
Equal(nameof(eSongCassetteStatus.HAVE_IN_BAG), missingSelectedDiagnostic.Player.EffectiveStatuses[nameof(ePlayableSong.QUIERES_BAILAR)], "missing selected entry preserves player-side statuses");
Equal(0L, missingSelectedDiagnostic.SelectedEntryPointer, "missing selected entry cannot invent a selected-entry pointer");
Equal(0L, missingSelectedDiagnostic.ExpectedSlotEntryPointer, "unreached expected-entry lookup cannot invent an expected-entry pointer");
Equal(0, RequestSystem.SubmitCount, "partial target diagnostics never submit a save request");

var synchronizationDiagnosticGlobalState = new DiskCommitTargetSaveDataStateFixture(
    new IntPtr(0x333), new(true, 0), new());
var synchronizationDiagnosticProcessor = new SaveDataRequestProcessor(new IntPtr(0x222), synchronizationDiagnosticGlobalState);
var synchronizationDiagnosticWrapper = new RequestProcessor(new IntPtr(0x222), synchronizationDiagnosticProcessor);
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(synchronizationDiagnosticWrapper));
PlayerSaveManagementEnquiries.ResetDiagnosticState();
Equal(false, CassetteSaveTransactionAdapter.TryReadSaveSynchronizationObservationDiagnostic(
    synchronizationDiagnosticProcessor,
    out CassetteSaveSynchronizationObservationState preLoadSynchronizationDiagnostic,
    out string preLoadSynchronizationStage),
    "pre-load unavailable enquiries remain diagnostic-only and unreadable");
Equal(false, preLoadSynchronizationDiagnostic.HasEnquirySlot, "pre-load diagnostic cannot invent an enquiry slot");
Equal(false, preLoadSynchronizationDiagnostic.HasEnquiryStatePointer, "pre-load diagnostic cannot invent an enquiry state pointer");
Equal(true, preLoadSynchronizationDiagnostic.HasValidExistingSaveSelected, "public validity enquiry remains independently observable before load");
Equal(false, preLoadSynchronizationDiagnostic.IsAValidExistingSaveSelected, "pre-load validity enquiry reports false");
Equal(true, preLoadSynchronizationDiagnostic.HasRegisteredPersistProcessorPointer, "pre-load diagnostic preserves registered processor identity");
Equal(0x222L, preLoadSynchronizationDiagnostic.RegisteredPersistProcessorPointer, "pre-load diagnostic reads the exact registered processor pointer");
Equal(true, preLoadSynchronizationDiagnostic.HasRetainedSaveDataProcessorPointer, "pre-load diagnostic preserves retained processor identity");
Equal(true, preLoadSynchronizationDiagnostic.ProcessorPointersMatch, "pre-load diagnostic proves registered and retained processor identity match");
Equal(true, preLoadSynchronizationDiagnostic.HasRegisteredSelectedSlot, "pre-load diagnostic preserves the registered SaveData selected slot");
Equal(0, preLoadSynchronizationDiagnostic.RegisteredSelectedSlot, "pre-load diagnostic exposes the unreliable global slot zero");
Equal("enquiry-slot-empty", preLoadSynchronizationDiagnostic.EnquiryStage, "pre-load diagnostic preserves the first exact enquiry failure");
Equal("readiness-selected-slot-observed", preLoadSynchronizationDiagnostic.RegisteredStateStage, "pre-load diagnostic records the exact registered-state observation stage");
Equal(false, string.IsNullOrWhiteSpace(preLoadSynchronizationStage), "pre-load diagnostic returns a bounded combined stage");

PlayerSaveManagementEnquiries.SelectedSlot = new FakeIl2CppNullable<int>(true, 4);
PlayerSaveManagementEnquiries.SelectedState = new FakePlayerSavePublicState(new IntPtr(0x700));
PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
Equal(true, CassetteSaveTransactionAdapter.TryReadSaveSynchronizationObservationDiagnostic(
    synchronizationDiagnosticProcessor,
    out CassetteSaveSynchronizationObservationState loadedSynchronizationDiagnostic,
    out string loadedSynchronizationStage),
    "loaded-save enquiries remain readable while registered SaveData still reports slot zero");
Equal(4, loadedSynchronizationDiagnostic.EnquirySlot, "loaded diagnostic preserves public enquiry slot four");
Equal(0x700L, loadedSynchronizationDiagnostic.EnquiryStatePointer, "loaded diagnostic preserves public enquiry state pointer");
Equal(true, loadedSynchronizationDiagnostic.IsAValidExistingSaveSelected, "loaded diagnostic preserves public validity result");
Equal(0, loadedSynchronizationDiagnostic.RegisteredSelectedSlot, "loaded diagnostic independently preserves registered global slot zero");
Equal(true, loadedSynchronizationDiagnostic.ProcessorPointersMatch, "loaded diagnostic keeps registered-retained identity proof separate from slot readiness");
Equal("success", loadedSynchronizationStage, "fully readable comparison reports success without authorizing the gate");

PlayerSaveManagementEnquiries.SelectedSlot = new FakeIl2CppNullable<int>(true, 3);
PlayerSaveManagementEnquiries.SelectedState = new FakePlayerSavePublicState(new IntPtr(0x900));
Equal(true, CassetteSaveTransactionAdapter.TryReadSaveSynchronizationObservationDiagnostic(
    synchronizationDiagnosticProcessor,
    out CassetteSaveSynchronizationObservationState mismatchedSynchronizationDiagnostic,
    out _),
    "mismatched loaded-save enquiries remain observable diagnostics");
Equal(3, mismatchedSynchronizationDiagnostic.EnquirySlot, "mismatch diagnostic preserves the observed enquiry slot");
Equal(0x900L, mismatchedSynchronizationDiagnostic.EnquiryStatePointer, "mismatch diagnostic preserves the observed enquiry pointer");

PlayerSaveManagementEnquiries.ThrowOnSlotRead = true;
Equal(false, CassetteSaveTransactionAdapter.TryReadSaveSynchronizationObservationDiagnostic(
    synchronizationDiagnosticProcessor,
    out CassetteSaveSynchronizationObservationState throwingSynchronizationDiagnostic,
    out string throwingSynchronizationStage),
    "throwing public enquiry is contained inside the diagnostic boundary");
Equal("enquiry-slot-get-invocation:InvalidOperationException:diagnostic-slot-read", throwingSynchronizationDiagnostic.EnquiryStage, "throwing slot enquiry has an exact stage");
Equal(true, throwingSynchronizationDiagnostic.HasRegisteredSelectedSlot, "throwing enquiry does not discard the independently readable registered selected slot");
Equal(false, string.IsNullOrWhiteSpace(throwingSynchronizationStage), "throwing diagnostic returns a bounded combined stage");
PlayerSaveManagementEnquiries.ResetDiagnosticState();
Equal(0, RequestSystem.SubmitCount, "synchronization comparison diagnostics never submit a request");
Console.WriteLine("PASS: save_synchronization_observation_diagnostics_are_public_bounded_and_behavior_neutral");

var selectedZeroReadinessState = new DiskCommitTargetSaveDataStateFixture(
    new IntPtr(0x333), new(true, 0), new());
var selectedZeroReadinessProcessor = new SaveDataRequestProcessor(new IntPtr(0x222), selectedZeroReadinessState);
var selectedZeroReadinessWrapper = new RequestProcessor(new IntPtr(0x222), selectedZeroReadinessProcessor);
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(selectedZeroReadinessWrapper));
Equal(false, CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
    selectedZeroReadinessProcessor, expectedSlot: 4, expectedPointer: 0x700, out string selectedZeroReadinessStage),
    "native selected slot zero defers cassette reconciliation before mutation");
Equal("readiness-selected-slot-mismatch:expected=4:actual=0", selectedZeroReadinessStage, "selected-slot zero has an exact bounded defer stage");
Equal(0, RequestSystem.SubmitCount, "selected-slot zero readiness probe cannot submit a save request");

var wrongEntryReadinessState = new DiskCommitTargetSaveDataStateFixture(
    new IntPtr(0x333), new(true, 4), new() { [4] = selectedTargetState });
var wrongEntryReadinessProcessor = new SaveDataRequestProcessor(new IntPtr(0x222), wrongEntryReadinessState);
var wrongEntryReadinessWrapper = new RequestProcessor(new IntPtr(0x222), wrongEntryReadinessProcessor);
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(wrongEntryReadinessWrapper));
Equal(false, CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
    wrongEntryReadinessProcessor, expectedSlot: 4, expectedPointer: 0x700, out string wrongEntryReadinessStage),
    "correct selected slot with the wrong player-state pointer defers cassette reconciliation");
Equal("readiness-selected-entry-pointer-mismatch:expected=0x700:actual=0x900", wrongEntryReadinessStage, "selected-entry pointer mismatch has an exact defer stage");
Equal(0, RequestSystem.SubmitCount, "pointer mismatch readiness probe cannot submit a save request");

var mismatchedRegisteredReadinessProcessor = new SaveDataRequestProcessor(new IntPtr(0x444), targetSaveDataState);
var mismatchedRegisteredReadinessWrapper = new RequestProcessor(new IntPtr(0x444), mismatchedRegisteredReadinessProcessor);
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(mismatchedRegisteredReadinessWrapper));
Equal(false, CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
    retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700, out string processorMismatchReadinessStage),
    "registered Persist and retained joined SaveData processor mismatch defers reconciliation");
Equal("readiness-save-data-processor-pointer-mismatch:expected=0x222:actual=0x444", processorMismatchReadinessStage, "processor pointer mismatch has an exact defer stage");
Equal(0, mismatchedRegisteredReadinessProcessor.ObtainStateCalls, "processor mismatch is rejected before obtaining the wrong registered state");
Equal(0, RequestSystem.SubmitCount, "processor mismatch readiness probe cannot submit a save request");

var exactReadinessState = new DiskCommitTargetSaveDataStateFixture(
    new IntPtr(0x333), new(true, 4), new() { [4] = expectedTargetState });
var exactReadinessProcessor = new SaveDataRequestProcessor(new IntPtr(0x222), exactReadinessState);
var exactReadinessWrapper = new RequestProcessor(new IntPtr(0x222), exactReadinessProcessor);
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(exactReadinessWrapper));
Equal(true, CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
    exactReadinessProcessor, expectedSlot: 4, expectedPointer: 0x700, out string exactReadinessStage),
    "exact registered processor, selected slot, and player-state pointer admit reconciliation");
Equal("success", exactReadinessStage, "exact synchronization reports success");
Equal(1, exactReadinessProcessor.ObtainStateCalls, "exact readiness obtains one coherent SaveData state");
Equal(0, RequestSystem.SubmitCount, "successful readiness probe remains side-effect free");

var readinessTransitionRuntime = new CassetteSaveEpochRuntime();
readinessTransitionRuntime.Receive("BADASS");
readinessTransitionRuntime.ActivateSave(4);
int simulatedGrantRequests = 0;
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(selectedZeroReadinessWrapper));
bool initiallyReady = CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
    selectedZeroReadinessProcessor, expectedSlot: 4, expectedPointer: 0x700, out _);
if (initiallyReady && readinessTransitionRuntime.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true))
{
    simulatedGrantRequests++;
    readinessTransitionRuntime.RecordSubmission("BADASS");
}
Equal(0, simulatedGrantRequests, "deferred readiness preserves pending AP ownership without granting");
Equal(true, readinessTransitionRuntime.IsPending("BADASS"), "deferred readiness retains the pending cassette for a later observation");
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(exactReadinessWrapper));
for (int observation = 0; observation < 2; observation++)
{
    bool ready = CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
        exactReadinessProcessor, expectedSlot: 4, expectedPointer: 0x700, out _);
    if (ready && readinessTransitionRuntime.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true))
    {
        simulatedGrantRequests++;
        readinessTransitionRuntime.RecordSubmission("BADASS");
    }
}
Equal(1, simulatedGrantRequests, "readiness transition admits the pending grant exactly once across repeated observations");
Equal(0, RequestSystem.SubmitCount, "readiness transition itself never submits a disk commit");
Console.WriteLine("PASS: cassette_save_synchronization_gate_defers_then_resumes_once");

RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(registeredPersistProcessorWrapper));
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, new MissingSelectedSlotSaveDataBundleRoutingStateFixture(),
    expectedSlot: 4, expectedPointer: 0x700, Array.Empty<string>(), out _, out _, out string targetFailureStage),
    "target diagnostic fails only its observational read when the retained save-data processor contract is missing");
Equal("target-save-data-processor-pointer-missing", targetFailureStage, "target diagnostic exposes the exact missing retained-processor pointer stage");
selectedTargetState.ThrowHasChanges = true;
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out _, out string targetThrowingStage),
    "throwing target-side public member remains isolated to the diagnostic read");
Equal("target-selected-has-changes-get-invocation:InvalidOperationException:target-has-changes", targetThrowingStage, "throwing target-side public member retains its exact stage");
Equal(0, RequestSystem.SubmitCount, "throwing target diagnostic cannot submit any request");
selectedTargetState.ThrowHasChanges = false;
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    new PrivateGetterPointerTargetProcessorFixture(playerTargetState), retainedSaveDataProcessor,
    expectedSlot: 4, expectedPointer: 0x700, Array.Empty<string>(), out _, out _, out string privatePointerStage),
    "a property with a non-public Pointer getter is rejected before reflection readback");
Equal("target-player-processor-pointer-getter-non-public", privatePointerStage, "private Pointer getter has an exact public-boundary stage");
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    new DiskCommitTargetPlayerProcessorFixture(new IntPtr(0x111), new PrivateGetterHasChangesTargetStateFixture()),
    retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700, Array.Empty<string>(),
    out _, out _, out string privateHasChangesStage),
    "a property with a public setter and non-public HasChanges getter is rejected before reflection readback");
Equal("target-player-has-changes-getter-non-public", privateHasChangesStage, "private HasChanges getter has an exact public-boundary stage");
selectedTargetState.ThrowPointer = true;
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out _, out string throwingPointerStage),
    "a throwing selected-entry Pointer getter is isolated from the transaction");
Equal("target-selected-entry-pointer-get-invocation:InvalidOperationException:target-pointer", throwingPointerStage, "throwing selected-entry Pointer getter retains its exact stage");
selectedTargetState.ThrowPointer = false;
var oversizedEntries = new Dictionary<PublicTypeKeyFixture, RegisteredRequestProcessor>();
for (int registryIndex = 0; registryIndex < 128; registryIndex++)
    oversizedEntries[new PublicTypeKeyFixture($"OtherRequest{registryIndex:D3}")] = new(registeredPersistProcessorWrapper);
oversizedEntries[new PublicTypeKeyFixture(nameof(PersistSaveChangeBundleRequest))] = new(registeredPersistProcessorWrapper);
var oversizedRegistry = new DirectLookupRegistryFixture(oversizedEntries);
RequestSystem.SetRegisteredProcessors(oversizedRegistry);
Equal(true, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int oversizedRegistryCount, out string oversizedRegistryStage),
    "a registry larger than 128 entries resolves Persist by exact direct lookup");
Equal("success", oversizedRegistryStage, "oversized direct lookup reports success");
Equal(129, oversizedRegistryCount, "oversized direct lookup preserves public Count");
Equal(0, oversizedRegistry.GetEnumeratorCalls, "direct Persist lookup never enumerates the registry");
var missingEntries = Enumerable.Range(0, 150).ToDictionary(
    index => new PublicTypeKeyFixture($"MissingRequest{index:D3}"),
    _ => new RegisteredRequestProcessor(registeredPersistProcessorWrapper));
var missingRegistry = new DirectLookupRegistryFixture(missingEntries);
RequestSystem.SetRegisteredProcessors(missingRegistry);
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int missingRegistryCount, out string missingRegistryStage),
    "missing exact Persist key fails without enumeration");
Equal(150, missingRegistryCount, "missing-key failure preserves public Count");
Equal("target-registry-persist-not-found", missingRegistryStage, "missing exact Persist key has an exact stage");
Equal(0, missingRegistry.GetEnumeratorCalls, "missing exact Persist key does not fall back to enumeration");
RequestSystem.SetRegisteredProcessors(oversizedRegistry);
Il2CppType.ThrowOnFrom = true;
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int conversionRegistryCount, out string conversionStage),
    "throwing public Il2CppType conversion fails diagnostically");
Equal(129, conversionRegistryCount, "conversion failure preserves public Count");
Equal("target-registry-key-convert-invocation:InvalidOperationException:il2cpp-type-convert", conversionStage, "conversion failure retains its exact stage");
Il2CppType.ThrowOnFrom = false;
Il2CppType.ConvertedNameOverride = "WrongPersistRequest";
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int keyMismatchRegistryCount, out string keyMismatchStage),
    "converted key with the wrong public Name fails before lookup");
Equal(129, keyMismatchRegistryCount, "converted-key mismatch preserves public Count");
Equal("target-registry-key-name-mismatch:expected=PersistSaveChangeBundleRequest:actual=WrongPersistRequest", keyMismatchStage, "converted-key mismatch has an exact stage");
Il2CppType.ConvertedNameOverride = null;
var missingLookupRegistry = new MissingTryGetRegistryFixture(37);
RequestSystem.SetRegisteredProcessors(missingLookupRegistry);
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int missingLookupRegistryCount, out string missingLookupStage),
    "registry without public exact TryGetValue fails diagnostically without enumeration");
Equal(37, missingLookupRegistryCount, "missing TryGetValue failure preserves public Count");
Equal("target-registry-try-get-missing", missingLookupStage, "missing public exact TryGetValue has the recommended exact stage");
Equal(0, missingLookupRegistry.GetEnumeratorCalls, "missing TryGetValue never falls back to enumeration");
var broadKeyRegistry = new BroadKeyRegistryFixture(registeredPersistProcessorWrapper);
RequestSystem.SetRegisteredProcessors(broadKeyRegistry);
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int broadKeyRegistryCount, out string broadKeyStage),
    "broad object-key TryGetValue is rejected instead of treated as the exact registry contract");
Equal(1, broadKeyRegistryCount, "broad-key rejection preserves public Count");
Equal("target-registry-try-get-missing", broadKeyStage, "broad-key overload does not satisfy exact TryGetValue lookup");
var wrongOutRegistry = new WrongOutRegistryFixture(registeredPersistProcessorWrapper);
RequestSystem.SetRegisteredProcessors(wrongOutRegistry);
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int wrongOutRegistryCount, out string wrongOutStage),
    "wrong out-registration TryGetValue is rejected before invocation");
Equal(1, wrongOutRegistryCount, "wrong-out rejection preserves public Count");
Equal("target-registry-try-get-missing", wrongOutStage, "wrong out-registration overload does not satisfy exact TryGetValue lookup");
var wrongNativeWrapper = new RequestProcessor(new IntPtr(0x444));
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(wrongNativeWrapper, otherEntries: 128));
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int wrongNativeRegistryCount, out string wrongNativeStage),
    "registered base Processor with the wrong native class fails closed before state acquisition");
Equal(129, wrongNativeRegistryCount, "wrong native class preserves the large public registry Count");
Equal("target-registry-persist-processor-native-class-mismatch", wrongNativeStage, "wrong native class has an exact cast stage");
Equal(1, wrongNativeWrapper.TryCastCalls, "wrong native class attempts exactly one public TryCast");
var throwingCastTarget = new SaveDataRequestProcessor(new IntPtr(0x444), targetSaveDataState);
var throwingCastWrapper = new RequestProcessor(new IntPtr(0x444), throwingCastTarget) { ThrowOnTryCast = true };
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(throwingCastWrapper));
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int throwingCastRegistryCount, out string throwingCastStage),
    "throwing public TryCast remains isolated to the diagnostic read");
Equal(1, throwingCastRegistryCount, "throwing cast preserves public registry Count");
Equal("target-registry-persist-processor-try-cast-invoke-invocation:InvalidOperationException:il2cpp-try-cast", throwingCastStage, "throwing cast retains its exact invocation stage");
Equal(0, throwingCastTarget.ObtainStateCalls, "throwing cast never reaches SaveData ObtainState");
var derivedCastTarget = new DerivedSaveDataRequestProcessor(new IntPtr(0x444), targetSaveDataState);
var derivedCastWrapper = new RequestProcessor(new IntPtr(0x444), derivedCastTarget);
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(derivedCastWrapper));
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int derivedCastRegistryCount, out string derivedCastStage),
    "cast result with a non-exact managed wrapper type fails closed");
Equal(1, derivedCastRegistryCount, "wrong managed cast type preserves public registry Count");
Equal("target-registry-persist-processor-cast-type-mismatch:expected=SaveDataRequestProcessor:actual=DerivedSaveDataRequestProcessor", derivedCastStage, "wrong managed cast type has an exact stage");
Equal(0, derivedCastTarget.ObtainStateCalls, "wrong managed cast type never reaches SaveData ObtainState");
var mismatchedPointerTarget = new SaveDataRequestProcessor(new IntPtr(0x445), targetSaveDataState);
var mismatchedPointerWrapper = new RequestProcessor(new IntPtr(0x444), mismatchedPointerTarget);
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(mismatchedPointerWrapper));
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int mismatchedPointerRegistryCount, out string mismatchedPointerStage),
    "cast wrapper at a different native pointer fails closed");
Equal(1, mismatchedPointerRegistryCount, "cast pointer mismatch preserves public registry Count");
Equal("target-registry-persist-processor-cast-pointer-mismatch:expected=0x444:actual=0x445", mismatchedPointerStage, "cast pointer mismatch has an exact stage");
Equal(0, mismatchedPointerTarget.ObtainStateCalls, "cast pointer mismatch never reaches SaveData ObtainState");
Equal(false, CassetteSaveTransactionAdapter.TryRewrapPublicSaveDataProcessorForDiagnostic(
    new MissingTryCastBaseFixture(new IntPtr(0x501)), typeof(MissingTryCastTargetFixture), typeof(MissingTryCastBaseFixture),
    out _, out _, out string missingCastStage),
    "missing public TryCast contract fails closed");
Equal("target-registry-persist-processor-try-cast-missing", missingCastStage, "missing TryCast has an exact stage");
Equal(false, CassetteSaveTransactionAdapter.TryRewrapPublicSaveDataProcessorForDiagnostic(
    new PrivateTryCastBaseFixture(new IntPtr(0x502)), typeof(PrivateTryCastTargetFixture), typeof(PrivateTryCastBaseFixture),
    out _, out _, out string privateCastStage),
    "private TryCast contract fails closed");
Equal("target-registry-persist-processor-try-cast-non-public", privateCastStage, "private TryCast has an exact stage");
var ambiguousBase = new PublicTryCastBaseFixture(new IntPtr(0x503), new PublicTryCastTargetFixture(new IntPtr(0x503)));
Equal(false, CassetteSaveTransactionAdapter.TryRewrapPublicSaveDataProcessorForDiagnostic(
    ambiguousBase, typeof(PublicTryCastTargetFixture), new DuplicatePublicTryCastTypeFixture(typeof(PublicTryCastBaseFixture)),
    out _, out _, out string ambiguousCastStage),
    "ambiguous public TryCast contract fails closed");
Equal("target-registry-persist-processor-try-cast-ambiguous:2", ambiguousCastStage, "ambiguous TryCast has an exact stage");
RequestSystem.SetRegisteredProcessors(new ThrowingCountRegistryFixture());
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    Array.Empty<string>(), out _, out int throwingCount, out string throwingCountStage),
    "throwing public registry Count remains isolated to diagnostics");
Equal(-1, throwingCount, "throwing Count cannot invent a registry size");
Equal("target-registry-count-get-invocation:InvalidOperationException:registry-count", throwingCountStage, "throwing Count retains its exact stage");
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteDiagnosticState(publicWriteProcessor, 0x701, Array.Empty<string>(), out _, out publicDiagnosticStage), "diagnostic snapshot rejects a public state pointer from another save");
Equal("write-diagnostic-pointer-mismatch:expected=0x701:actual=0x700", publicDiagnosticStage, "diagnostic snapshot reports the exact identity mismatch");
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteDiagnosticState(new PublicWriteProcessorFixture(new MissingDiagnosticHasChangesWriteStateFixture()), 0x700, Array.Empty<string>(), out _, out publicDiagnosticStage), "missing core public write property fails at its exact field");
Equal("write-diagnostic-has-changes-missing", publicDiagnosticStage, "missing core public write property has an exact stage");
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteDiagnosticState(new PublicWriteProcessorFixture(new MissingDiagnosticRevisionWriteStateFixture()), 0x700, Array.Empty<string>(), out _, out publicDiagnosticStage), "missing public redundancy revision fails the diagnostic read only");
Equal("write-diagnostic-redundancy-revision-missing", publicDiagnosticStage, "missing public redundancy revision has an exact stage");
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteDiagnosticState(new PublicWriteProcessorFixture(new ThrowingDiagnosticUnstagedWriteStateFixture()), 0x700, Array.Empty<string>(), out _, out publicDiagnosticStage), "throwing public unstaged getter fails the diagnostic read only");
Equal("write-diagnostic-has-unstaged-get-invocation:InvalidOperationException:unstaged-diagnostic", publicDiagnosticStage, "throwing public unstaged getter has an exact invocation stage");
GameTimeEnquiries.ThrowOnRead = true;
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteDiagnosticState(publicWriteProcessor, 0x700, Array.Empty<string>(), out _, out publicDiagnosticStage), "throwing public CurrentGameTime getter fails the diagnostic read only");
Equal("write-diagnostic-current-game-time-get-invocation:InvalidOperationException:current-game-time-diagnostic", publicDiagnosticStage, "throwing public CurrentGameTime getter has an exact invocation stage");
GameTimeEnquiries.ThrowOnRead = false;
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteState(new PublicWriteProcessorFixture(new PrivateSuccessNullableWriteStateFixture()), out _, out publicWriteStage), "private success-time nullable contract fails closed");
Equal("write-success-time-nullable-contract-missing", publicWriteStage, "private success-time wrapper reports the exact public boundary failure");
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteState(new PublicWriteProcessorFixture(new PrivateReasonNullableWriteStateFixture()), out _, out publicWriteStage), "private failure-reason nullable contract fails closed");
Equal("write-failure-reason-nullable-contract-missing", publicWriteStage, "private failure-reason wrapper reports the exact public boundary failure");
RequestSystem.Reset();
PersistSaveChangeBundleRequest.ResetConstructionCounters();
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteState(new ThrowingObtainPublicWriteProcessorFixture(), out CassettePublicWriteState failedWriteState, out publicWriteStage), "throwing ObtainState fails closed");
Equal(default(CassettePublicWriteState), failedWriteState, "throwing ObtainState exposes no partial write state");
Equal("write-state-obtain-state-invoke-invocation:NullReferenceException:obtain-state", publicWriteStage, "throwing ObtainState identifies its exact invocation stage");
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteState(new PublicWriteProcessorFixture(new ThrowingHasChangesWriteStateFixture()), out failedWriteState, out publicWriteStage), "throwing HasChanges fails closed");
Equal(default(CassettePublicWriteState), failedWriteState, "throwing HasChanges exposes no partial write state");
Equal("write-state-has-changes-get-invocation:NullReferenceException:has-changes", publicWriteStage, "throwing HasChanges identifies its exact getter stage");
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteState(new PublicWriteProcessorFixture(new ThrowingSuccessNullableWriteStateFixture()), out failedWriteState, out publicWriteStage), "throwing success nullable fails closed");
Equal(default(CassettePublicWriteState), failedWriteState, "throwing success nullable exposes no partial write state");
Equal("write-success-time-nullable-has-value-get-invocation:NullReferenceException:success-has-value", publicWriteStage, "throwing success nullable identifies its exact wrapper stage");
Equal(true, CassetteSaveTransactionAdapter.TryReadPublicWriteState(new PublicWriteProcessorFixture(new EmptyInteropNullableWriteStateFixture()), out CassettePublicWriteState emptyNullableWriteState, out publicWriteStage), "IL2CPP empty nullable getter returns are normalized");
Equal(new CassettePublicWriteState(true, true, null, null, null), emptyNullableWriteState, "all three empty IL2CPP write-state nullable values remain absent");
Equal("success", publicWriteStage, "empty IL2CPP nullable write state reports success");
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteState(new PublicWriteProcessorFixture(new UnrelatedThrowingInteropNullableWriteStateFixture()), out failedWriteState, out publicWriteStage), "unrelated NRE from an IL2CPP nullable getter fails closed");
Equal(default(CassettePublicWriteState), failedWriteState, "unrelated nullable getter NRE exposes no partial write state");
Equal("write-state-success-time-get-invocation:NullReferenceException:not-interop-empty", publicWriteStage, "unrelated nullable getter NRE retains its exact failure stage");
var failedWriteEvent = new PlayerSaveWriteCompletedEvent(4, false, new(true, eSaveFileWriterFailureReason.VALIDATION_FAILED));
Equal(true, CassetteSaveTransactionAdapter.TryReadPlayerSaveWriteCompletedEvent(failedWriteEvent, out int writeEventSlot, out bool writeEventSucceeded, out string? writeEventFailure, out string writeEventStage), "adapter reads the public player-save write-completed event");
Equal(4, writeEventSlot, "write-completed event preserves exact slot");
Equal(false, writeEventSucceeded, "write-completed event preserves failed result");
Equal(nameof(eSaveFileWriterFailureReason.VALIDATION_FAILED), writeEventFailure, "write-completed event preserves public failure reason");
Equal("success", writeEventStage, "write-completed event read reports success");
Equal(true, CassetteSaveTransactionAdapter.TryReadPlayerSaveWriteCompletedEvent(new PlayerSaveWriteCompletedEvent(4, true, new(false, default)), out writeEventSlot, out writeEventSucceeded, out writeEventFailure, out writeEventStage), "successful write-completed event permits an empty failure reason");
Equal(true, writeEventSucceeded, "successful event result remains true");
Equal<string?>(null, writeEventFailure, "successful event has no invented failure reason");
Equal(true, CassetteSaveTransactionAdapter.TryReadPlayerSaveWriteCompletedEvent(new MissingFailureReasonFixture.PlayerSaveWriteCompletedEvent(4, true), out writeEventSlot, out writeEventSucceeded, out writeEventFailure, out writeEventStage), "missing optional failure reason cannot suppress a success wake");
Equal(true, writeEventSucceeded, "mandatory success header survives missing optional reason");
Equal<string?>(null, writeEventFailure, "missing optional reason remains unavailable");
Equal(true, CassetteSaveTransactionAdapter.TryReadPlayerSaveWriteCompletedEvent(new MissingFailureReasonFixture.PlayerSaveWriteCompletedEvent(4, false), out writeEventSlot, out writeEventSucceeded, out writeEventFailure, out writeEventStage), "missing optional failure reason cannot suppress terminal failure");
Equal(false, writeEventSucceeded, "mandatory failure header survives missing optional reason");
Equal(true, CassetteSaveTransactionAdapter.TryReadPlayerSaveWriteCompletedEvent(new ThrowingFailureReasonFixture.PlayerSaveWriteCompletedEvent(4, false), out writeEventSlot, out writeEventSucceeded, out writeEventFailure, out writeEventStage), "throwing optional failure reason cannot suppress terminal failure");
Equal(false, writeEventSucceeded, "mandatory failure header survives throwing optional reason");
Equal<string?>(null, writeEventFailure, "throwing optional reason remains unavailable");
Equal(true, CassetteSaveTransactionAdapter.TryReadPlayerSaveWriteCompletedEvent(new ThrowingFailureReasonFixture.PlayerSaveWriteCompletedEvent(4, true), out writeEventSlot, out writeEventSucceeded, out writeEventFailure, out writeEventStage), "throwing optional failure reason cannot suppress a success wake");
Equal(true, writeEventSucceeded, "mandatory success header survives throwing optional reason");
Equal(true, CassetteSaveTransactionAdapter.TryReadPlayerSaveWriteCompletedEvent(new PlayerSaveWriteCompletedEvent(4, false, new(false, default)), out writeEventSlot, out writeEventSucceeded, out writeEventFailure, out writeEventStage), "empty optional failure reason cannot suppress terminal failure");
Equal(false, writeEventSucceeded, "empty-reason failed event preserves mandatory result");
Equal(false, CassetteSaveTransactionAdapter.TryReadPlayerSaveWriteCompletedEvent(new object(), out _, out _, out _, out writeEventStage), "wrong event type fails closed");
Equal("write-event-incompatible", writeEventStage, "wrong event type has an exact failure stage");
Equal(0, RequestSystem.SubmitCount, "write-state diagnostics never submit a persist request");
Equal(true, CassetteSaveTransactionAdapter.TrySubmitDefaultUrgentPersist(out string persistDetail), "adapter submits narrow public persist request");
Equal(1, RequestSystem.SubmitCount, "batched transaction submits one persist request");
Equal(false, RequestSystem.LastRequest!.Bundle.HasValue, "submitted persist request carries a verified absent bundle");
Equal(ePlayerSaveChangeBundleKey.DEFAULT, RequestSystem.EffectiveBundle, "native-shaped persist fallback resolves absent bundle to DEFAULT");
Equal(eSaveFileWriteType.URGENT, RequestSystem.LastRequest.WriteTypeToRequest, "persist request uses exact URGENT write type");
Equal(eSaveFileWriteType.URGENT, RequestSystem.EffectiveWriteType, "native-shaped persist processor receives numeric URGENT");
Equal(true, RequestSystem.LastRequest.ParameterlessConstructorUsed, "submission uses the public parameterless persist constructor");
Equal(0, PersistSaveChangeBundleRequest.SemanticConstructorCalls, "submission never invokes the broken two-argument persist constructor");
Equal(0, RequestSystem.LastRequest.BundleSetterCalls, "submission never invokes the broken nullable persist setter");
Equal(1, RequestSystem.LastRequest.WriteTypeSetterCalls, "submission explicitly assigns URGENT through the public plain-enum setter");
Equal("actualBundle='present=False value=<null>' effectiveBundle='DEFAULT/1(native absent fallback)' actualWriteType='URGENT/0' route='RequestSystem.SubmitRequest<T>'", persistDetail, "persist detail records verified actual and effective semantics");

string adapterSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "CassetteSaveTransactionAdapter.cs"));
string targetCastAdapter = ExtractMethods(adapterSource, "internal static bool TryRewrapPublicSaveDataProcessorForDiagnostic(").Single();
Equal(true, targetCastAdapter.Contains("MakeGenericMethod", StringComparison.Ordinal), "target diagnostic rewrap closes the public generic TryCast API over exact SaveDataRequestProcessor");
foreach (string prohibitedRawConstructorPath in new[] { "GetConstructor", "ConstructorInfo", "Activator.CreateInstance" })
    Equal(false, targetCastAdapter.Contains(prohibitedRawConstructorPath, StringComparison.Ordinal), $"target diagnostic rewrap never uses raw IntPtr constructor path {prohibitedRawConstructorPath}");
string factorySource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "CassetteNativeRequestFactory.cs"));
string verifiedFactory = ExtractMethods(factorySource, "private static bool TryCreateVerified(").Single();
Equal(true, verifiedFactory.Contains("Type.EmptyTypes", StringComparison.Ordinal), "request factory resolves the exact public parameterless constructor");
Equal(false, verifiedFactory.Contains("bundleSetter", StringComparison.Ordinal), "request factory never invokes the broken nullable Bundle setter");
Equal(false, verifiedFactory.Contains("nullableConstructor", StringComparison.Ordinal), "request factory never constructs an IL2CPP nullable bundle");
Equal(false, verifiedFactory.Contains("new[] { songType, statusType, bundleType }", StringComparison.Ordinal), "request factory never resolves the broken semantic three-argument constructor");
Equal(true, verifiedFactory.Contains("bundleGetter?.IsPublic", StringComparison.Ordinal), "request factory requires a public Bundle getter before readback");
Equal(true, verifiedFactory.Contains("actualSong", StringComparison.Ordinal) && verifiedFactory.Contains("actualStatus", StringComparison.Ordinal) && verifiedFactory.Contains("present", StringComparison.Ordinal), "request factory verifies exact public song/status and absent-bundle readback before returning a request");
Equal(false, verifiedFactory.Contains("BindingFlags.NonPublic", StringComparison.Ordinal), "request repair and verification cannot bind non-public members");
string grantAdapter = ExtractMethods(adapterSource, "internal static bool TrySubmitHaveInBag(").Single();
Equal(true, grantAdapter.Contains("TryCreateHaveInBagRequest", StringComparison.Ordinal), "grant adapter processes only a factory-verified request");
Equal(false, grantAdapter.Contains("nativeBundle", StringComparison.Ordinal), "grant detail cannot be derived from pre-construction bundle intent");
foreach (string obsoleteDiagnostic in new[] { "CassetteSaveFingerprint", "TryGetLoadedSaveFingerprint", "TryGetProcessorSaveFingerprint", "TryBuildFingerprint", "UnwrapNullablePublic" })
    Equal(false, adapterSource.Contains(obsoleteDiagnostic, StringComparison.Ordinal), $"temporary adapter diagnostic removed: {obsoleteDiagnostic}");
string policySource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "CassetteRandomizationPolicy.cs"));
Equal(false, policySource.Contains("required ", StringComparison.Ordinal), "diagnostic tombstone remains compatible with the net6 client target");
foreach (string obsoleteDiagnostic in new[] { "CassetteSaveIdentityStabilizer", "CassetteMostRecentIdentityProbe", "CassetteProcessorIdentityProbe", "CassetteSaveFingerprint" })
    Equal(false, policySource.Contains(obsoleteDiagnostic, StringComparison.Ordinal), $"temporary policy diagnostic removed: {obsoleteDiagnostic}");
string processorIdentityAdapter = ExtractMethods(adapterSource, "internal static bool TryGetProcessorSaveIdentity(").Single();
Equal(true, processorIdentityAdapter.Contains("PublicInstance", StringComparison.Ordinal), "authoritative processor identity uses public-only state lookup");
Equal(false, processorIdentityAdapter.Contains("AllInstance", StringComparison.Ordinal), "authoritative processor identity cannot invoke private state access");
string pointerJoinAdapter = ExtractMethods(adapterSource, "internal static bool TryMatchRegularSaveSlot(").Single();
Equal(true, pointerJoinAdapter.Contains("PublicInstance", StringComparison.Ordinal), "regular-save pointer join uses public-only APIs");
Equal(false, pointerJoinAdapter.Contains("AllInstance", StringComparison.Ordinal) || pointerJoinAdapter.Contains("AllStatic", StringComparison.Ordinal), "regular-save pointer join cannot bind non-public members");
string persistAdapter = ExtractMethods(adapterSource, "internal static bool TrySubmitDefaultUrgentPersist(").Single();
string persistFactory = ExtractMethods(factorySource, "internal static bool TryCreateDefaultUrgentPersistRequest(").Single();
Equal(true, persistAdapter.Contains("PublicStatic", StringComparison.Ordinal) && persistFactory.Contains("PublicInstance", StringComparison.Ordinal), "disk persist uses public-only request construction and routing");
Equal(false, persistAdapter.Contains("AllStatic", StringComparison.Ordinal) || persistAdapter.Contains("AllInstance", StringComparison.Ordinal), "disk persist never resolves private APIs");
Equal(false, persistFactory.Contains("BindingFlags.NonPublic", StringComparison.Ordinal), "persist request construction and readback never resolve private APIs");
Equal(true, persistAdapter.Contains("TryCreateDefaultUrgentPersistRequest", StringComparison.Ordinal), "disk persist submits only a factory-verified absent-bundle request");
Equal(false, adapterSource.Contains("BuildPublicNullable", StringComparison.Ordinal), "disk persist no longer constructs a reflected nullable bundle");
Equal(true, persistFactory.Contains("Convert.ToInt32(nativeBundle) != 1", StringComparison.Ordinal) && persistFactory.Contains("Convert.ToInt32(nativeWriteType) != 0", StringComparison.Ordinal), "disk persist validates exact DEFAULT=1/URGENT=0 semantics");
string diagnosticAdapter = ExtractMethods(adapterSource, "private static bool TryReadPublicWriteDiagnosticStateCore(").Single();
Equal(true, diagnosticAdapter.Contains("PublicInstance", StringComparison.Ordinal) && diagnosticAdapter.Contains("PublicStatic", StringComparison.Ordinal), "disk diagnostics use public-only state and enquiry APIs");
Equal(false, diagnosticAdapter.Contains("AllStatic", StringComparison.Ordinal) || diagnosticAdapter.Contains("AllInstance", StringComparison.Ordinal), "disk diagnostics cannot bind non-public members");
Equal(true, diagnosticAdapter.Contains("GetCassetteStatusForSong", StringComparison.Ordinal), "disk diagnostic status map is derived from the same obtained public state");
string lifecycleDiagnosticAdapter = ExtractMethods(adapterSource, "internal static bool TryReadPublicWriteLifecycleDiagnosticState(").Single();
Equal(true, lifecycleDiagnosticAdapter.Contains("TryReadPublicWriteDiagnosticStateCore", StringComparison.Ordinal), "lifecycle diagnostic shares the public same-object snapshot reader");
Equal(false, lifecycleDiagnosticAdapter.Contains("AllStatic", StringComparison.Ordinal) || lifecycleDiagnosticAdapter.Contains("AllInstance", StringComparison.Ordinal), "lifecycle diagnostics cannot bind non-public members");
foreach (string routingReader in new[] { "TryReadRecordSongCassetteStatusRequest", "TryReadPersistSaveChangeBundleRequest", "TryReadPersistAllSaveChangeBundlesRequest", "TryReadPlayerSaveBundleRoutingState", "TryReadSaveDataBundleRoutingState" })
{
    string routingReaderSource = ExtractMethods(adapterSource, $"internal static bool {routingReader}(").Single();
    Equal(true, routingReaderSource.Contains("PublicInstance", StringComparison.Ordinal) || routingReaderSource.Contains("TryRead", StringComparison.Ordinal), $"{routingReader} uses the public diagnostic boundary");
    Equal(false, routingReaderSource.Contains("AllInstance", StringComparison.Ordinal) || routingReaderSource.Contains("AllStatic", StringComparison.Ordinal), $"{routingReader} cannot bind non-public members");
}
foreach (string prohibited in new[] { "PersistAllChangesInBundle", "RequestWriteForPlayerSave", "SaveDataManager", "WritePlayerSaveFile", "SelectedPlayerSaveSlotChangedEvent", "TriggerUrgentSaveWriteIfAnyChangesRequest" })
    Equal(false, adapterSource.Contains(prohibited, StringComparison.Ordinal), $"transaction adapter prohibits {prohibited}");
Console.WriteLine("PASS: native_save_selection_and_persistence_adapters");
Console.WriteLine("Cassette randomization catalog and source policy tests passed.");

enum TestSong { INVALID, QUIERES_BAILAR }
enum TestCassetteStatus { INVALID, HAVE_IN_BAG }
enum TestBundle { INVALID, DEFAULT }

sealed class AuthoritativeProcessor
{
    private readonly AuthoritativeSaveState _state;
    public int ObtainStateCalls { get; private set; }

    public AuthoritativeProcessor(AuthoritativeSaveState state) => _state = state;

    private AuthoritativeSaveState ObtainState()
    {
        ObtainStateCalls++;
        return _state;
    }
}

sealed class AuthoritativeSaveState
{
    private readonly TestCassetteStatus _status;
    public int StatusReads { get; private set; }

    public AuthoritativeSaveState(TestCassetteStatus status) => _status = status;

    public TestCassetteStatus GetCassetteStatusForSong(TestSong song)
    {
        StatusReads++;
        return _status;
    }
}

sealed class TestCassetteRequest
{
    private FakeIl2CppNullable<TestBundle> _bundle = new(false, default);

    public TestSong Song { get; set; }
    public TestCassetteStatus CassetteStatus { get; set; }
    public FakeIl2CppNullable<TestBundle> Bundle
    {
        get => _bundle;
        set
        {
            BundleSetterCalls++;
            throw new InvalidOperationException("bundle-set-must-not-run");
        }
    }
    public bool ParameterlessConstructorUsed { get; }
    public int BundleSetterCalls { get; private set; }
    public static int SemanticConstructorCalls { get; private set; }

    public TestCassetteRequest() => ParameterlessConstructorUsed = true;

    public TestCassetteRequest(TestSong song, TestCassetteStatus cassetteStatus, TestBundle bundle)
    {
        SemanticConstructorCalls++;
        throw new InvalidOperationException("semantic-constructor-must-not-run");
    }

    public static void ResetCounters() => SemanticConstructorCalls = 0;
}

namespace MissingParameterlessConstructorFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest(TestSong song, TestCassetteStatus status, TestBundle bundle)
        { Song = song; CassetteStatus = status; }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace PrivateParameterlessConstructorFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        private RecordSongCassetteStatusInSaveDataRequest() { }
        public RecordSongCassetteStatusInSaveDataRequest(TestSong song, TestCassetteStatus status, TestBundle bundle)
        { Song = song; CassetteStatus = status; }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace ThrowingParameterlessConstructorFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() => throw new InvalidOperationException("parameterless-constructor");
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace MissingSongSetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song => TestSong.INVALID;
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace PrivateSongSetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; private set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace ThrowingSongSetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get => TestSong.INVALID; set => throw new InvalidOperationException("song-set"); }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace MissingSongGetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { set { } }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace PrivateSongGetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { private get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace ThrowingSongGetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get => throw new InvalidOperationException("song-get"); set { } }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace MissingStatusSetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus => TestCassetteStatus.INVALID;
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace PrivateStatusSetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; private set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace ThrowingStatusSetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get => TestCassetteStatus.INVALID; set => throw new InvalidOperationException("status-set"); }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace MissingStatusGetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { set { } }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace PrivateStatusGetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { private get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace ThrowingStatusGetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get => throw new InvalidOperationException("status-get"); set { } }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace PrivateBundleGetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle { private get; set; } = new(false, default);
    }
}

namespace ThrowingBundleGetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => throw new InvalidOperationException("bundle-get");
    }
}

namespace UnrelatedNullBundleGetterFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public Il2CppSystem.Nullable<TestBundle> Bundle => throw new NullReferenceException("unrelated-null");
    }
}

namespace NullBundleReadbackFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle>? Bundle => null;
    }
}

sealed class MalformedNullable<T>
{
    public bool HasValue => false;
}

namespace MalformedBundleFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public MalformedNullable<TestBundle> Bundle => new();
    }
}

namespace MismatchedSongReadbackFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get => TestSong.INVALID; set { } }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace MismatchedStatusReadbackFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get => TestCassetteStatus.INVALID; set { } }
        public FakeIl2CppNullable<TestBundle> Bundle => new(false, default);
    }
}

namespace PresentInvalidBundleFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(true, TestBundle.INVALID);
    }
}

namespace PresentDefaultBundleFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public FakeIl2CppNullable<TestBundle> Bundle => new(true, TestBundle.DEFAULT);
    }
}

namespace EmptyInteropConstructionFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest() { }
        public TestSong Song { get; set; }
        public TestCassetteStatus CassetteStatus { get; set; }
        public Il2CppSystem.Nullable<TestBundle> Bundle => Il2CppSystem.Nullable<TestBundle>.EmptyFromInterop();
    }
}

static class PlayerSaveManagementEnquiries
{
    public static FakeIl2CppNullable<int>? SelectedSlot { get; set; }
    public static object? SelectedState { get; set; }
    public static bool ValidExistingSaveSelected { get; set; }
    public static bool ThrowOnSlotRead { get; set; }
    public static bool ThrowOnStateRead { get; set; }
    public static bool ThrowOnValidityRead { get; set; }
    public static FakeIl2CppNullable<int>? GetSelectedSaveFileSlotNumber() => ThrowOnSlotRead
        ? throw new InvalidOperationException("diagnostic-slot-read")
        : SelectedSlot;
    public static object? TryGetSelectedSlotSaveFileState()
    {
        if (ThrowOnStateRead) throw new InvalidOperationException("diagnostic-state-read");
        return SelectedState == null ? null : new FakeIl2CppNullable<object>(true, SelectedState);
    }
    public static bool IsAValidExistingSaveSelected() => ThrowOnValidityRead
        ? throw new InvalidOperationException("diagnostic-validity-read")
        : ValidExistingSaveSelected;
    public static void ResetDiagnosticState()
    {
        SelectedSlot = new FakeIl2CppNullable<int>(false, 0);
        SelectedState = null;
        ValidExistingSaveSelected = false;
        ThrowOnSlotRead = false;
        ThrowOnStateRead = false;
        ThrowOnValidityRead = false;
    }
}

sealed class FakeIl2CppNullable<T>
{
    public FakeIl2CppNullable(T value)
    {
        HasValue = true;
        Value = value;
    }

    public FakeIl2CppNullable(bool hasValue, T value)
    {
        HasValue = hasValue;
        Value = value;
    }

    public bool HasValue { get; }
    public T Value { get; }
}

sealed class FakePlayerSavePublicState
{
    public FakePlayerSavePublicState(IntPtr pointer)
    {
        Pointer = pointer;
        GameStats = new FakeGameStats(
            1234,
            new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc));
    }
    public IntPtr Pointer { get; }
    public FakeGameStats GameStats { get; }
    public eSongCassetteStatus GetCassetteStatusForSong(ePlayableSong song) =>
        song == ePlayableSong.I_GOT_MONEY ? eSongCassetteStatus.HAVE_IN_BAG : eSongCassetteStatus.INVALID;
}

sealed record FakeGameStats(long PlayTimeInSeconds, DateTime LastPlayDateTimeUtc);

sealed record FakeGameTime(double RawTime);

static class GameTimeEnquiries
{
    private static FakeGameTime _currentGameTime = new(0);
    public static bool ThrowOnRead { get; set; }
    public static FakeGameTime CurrentGameTime
    {
        get => ThrowOnRead ? throw new InvalidOperationException("current-game-time-diagnostic") : _currentGameTime;
        set => _currentGameTime = value;
    }
}

sealed class PublicWriteProcessorFixture
{
    private readonly object _state;
    public PublicWriteProcessorFixture(object state) => _state = state;
    public object ObtainState() => _state;
}

sealed class ThrowingObtainPublicWriteProcessorFixture
{
    public object ObtainState() => throw new NullReferenceException("obtain-state");
}

sealed class PublicWriteStateFixture
{
    public IntPtr Pointer => new(0x700);
    public bool HasChanges => true;
    public bool RequiresWriteToDisk => false;
    public bool HasUnstagedChanges => true;
    public int SaveFileRedundancyBundleIndex => 2;
    public int SaveFileRedundancyBundleRevision => 7;
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastWriteToDisk => new(true, new(10));
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastFailedAttemptToWriteToDisk => new(true, new(8));
    public FakeIl2CppNullable<string> FailureReasonOfLastFailedAttemptToWriteToDisk => new(true, "IO_ERROR");
    public eSongCassetteStatus GetCassetteStatusForSong(ePlayableSong song) => eSongCassetteStatus.HAVE_IN_BAG;
}

sealed class MissingDiagnosticHasChangesWriteStateFixture
{
    public IntPtr Pointer => new(0x700);
    public bool RequiresWriteToDisk => false;
    public bool HasUnstagedChanges => true;
    public int SaveFileRedundancyBundleIndex => 2;
    public int SaveFileRedundancyBundleRevision => 7;
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastWriteToDisk => new(true, new(10));
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastFailedAttemptToWriteToDisk => new(true, new(8));
    public FakeIl2CppNullable<string> FailureReasonOfLastFailedAttemptToWriteToDisk => new(true, "IO_ERROR");
}

sealed class MissingDiagnosticRevisionWriteStateFixture
{
    public IntPtr Pointer => new(0x700);
    public bool HasChanges => true;
    public bool RequiresWriteToDisk => false;
    public bool HasUnstagedChanges => true;
    public int SaveFileRedundancyBundleIndex => 2;
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastWriteToDisk => new(true, new(10));
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastFailedAttemptToWriteToDisk => new(true, new(8));
    public FakeIl2CppNullable<string> FailureReasonOfLastFailedAttemptToWriteToDisk => new(true, "IO_ERROR");
}

sealed class ThrowingDiagnosticUnstagedWriteStateFixture
{
    public IntPtr Pointer => new(0x700);
    public bool HasChanges => true;
    public bool RequiresWriteToDisk => false;
    public bool HasUnstagedChanges => throw new InvalidOperationException("unstaged-diagnostic");
    public int SaveFileRedundancyBundleIndex => 2;
    public int SaveFileRedundancyBundleRevision => 7;
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastWriteToDisk => new(true, new(10));
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastFailedAttemptToWriteToDisk => new(true, new(8));
    public FakeIl2CppNullable<string> FailureReasonOfLastFailedAttemptToWriteToDisk => new(true, "IO_ERROR");
}

sealed class PrivateSuccessNullableWriteStateFixture
{
    public bool HasChanges => true;
    public bool RequiresWriteToDisk => false;
    public object GameTimeOfLastWriteToDisk => new PrivateNullable<FakeGameTime>(new(10));
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastFailedAttemptToWriteToDisk => new(false, new(0));
    public FakeIl2CppNullable<string> FailureReasonOfLastFailedAttemptToWriteToDisk => new(false, string.Empty);
}

sealed class PrivateReasonNullableWriteStateFixture
{
    public bool HasChanges => true;
    public bool RequiresWriteToDisk => false;
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastWriteToDisk => new(true, new(10));
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastFailedAttemptToWriteToDisk => new(false, new(0));
    public object FailureReasonOfLastFailedAttemptToWriteToDisk => new PrivateNullable<string>("IO_ERROR");
}

sealed class ThrowingHasChangesWriteStateFixture
{
    public bool HasChanges => throw new NullReferenceException("has-changes");
    public bool RequiresWriteToDisk => false;
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastWriteToDisk => new(false, new(0));
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastFailedAttemptToWriteToDisk => new(false, new(0));
    public FakeIl2CppNullable<string> FailureReasonOfLastFailedAttemptToWriteToDisk => new(false, string.Empty);
}

sealed class ThrowingSuccessNullableWriteStateFixture
{
    public bool HasChanges => true;
    public bool RequiresWriteToDisk => false;
    public ThrowingNullable<FakeGameTime> GameTimeOfLastWriteToDisk => new(new(10));
    public FakeIl2CppNullable<FakeGameTime> GameTimeOfLastFailedAttemptToWriteToDisk => new(false, new(0));
    public FakeIl2CppNullable<string> FailureReasonOfLastFailedAttemptToWriteToDisk => new(false, string.Empty);
}

sealed class EmptyInteropNullableWriteStateFixture
{
    public bool HasChanges => true;
    public bool RequiresWriteToDisk => true;
    public Il2CppSystem.Nullable<FakeGameTime> GameTimeOfLastWriteToDisk => Il2CppSystem.Nullable<FakeGameTime>.EmptyFromInterop();
    public Il2CppSystem.Nullable<FakeGameTime> GameTimeOfLastFailedAttemptToWriteToDisk => Il2CppSystem.Nullable<FakeGameTime>.EmptyFromInterop();
    public Il2CppSystem.Nullable<string> FailureReasonOfLastFailedAttemptToWriteToDisk => Il2CppSystem.Nullable<string>.EmptyFromInterop();
}

sealed class UnrelatedThrowingInteropNullableWriteStateFixture
{
    public bool HasChanges => true;
    public bool RequiresWriteToDisk => true;
    public Il2CppSystem.Nullable<FakeGameTime> GameTimeOfLastWriteToDisk => throw new NullReferenceException("not-interop-empty");
    public Il2CppSystem.Nullable<FakeGameTime> GameTimeOfLastFailedAttemptToWriteToDisk => new(false, new(0));
    public Il2CppSystem.Nullable<string> FailureReasonOfLastFailedAttemptToWriteToDisk => new(false, string.Empty);
}

sealed class ThrowingNullable<T>
{
    private readonly T _value;
    public ThrowingNullable(T value) => _value = value;
    public bool HasValue => throw new NullReferenceException("success-has-value");
    public T Value => _value;
}

sealed class PrivateNullable<T>
{
    private readonly T _value;
    public PrivateNullable(T value) => _value = value;
    private bool HasValue => true;
    private T Value => _value;
}

enum ePlayerSaveChangeBundleKey { INVALID, DEFAULT, CAMPAIGN }
enum eSaveFileWriteType { URGENT, NON_URGENT }
enum eSaveFileWriterFailureReason { VALIDATION_FAILED }
enum ePlayableSong { INVALID, QUIERES_BAILAR, I_GOT_MONEY, BADASS }
enum eSongCassetteStatus { INVALID, HAVE_IN_BAG }

sealed class PlayerSaveWriteCompletedEvent
{
    public PlayerSaveWriteCompletedEvent(int slotNumber, bool succeeded, FakeIl2CppNullable<eSaveFileWriterFailureReason> failureReason)
    { SlotNumber = slotNumber; Succeeded = succeeded; FailureReason = failureReason; }
    public int SlotNumber { get; }
    public bool Succeeded { get; }
    public FakeIl2CppNullable<eSaveFileWriterFailureReason> FailureReason { get; }
}

sealed class PersistSaveChangeBundleRequest
{
    private readonly FakeIl2CppNullable<ePlayerSaveChangeBundleKey> _bundle = new(false, default);
    private eSaveFileWriteType _writeType = eSaveFileWriteType.NON_URGENT;

    public PersistSaveChangeBundleRequest() => ParameterlessConstructorUsed = true;

    public PersistSaveChangeBundleRequest(FakeIl2CppNullable<ePlayerSaveChangeBundleKey> bundle, eSaveFileWriteType writeType)
    {
        SemanticConstructorCalls++;
        throw new InvalidOperationException("persist-semantic-constructor-must-not-run");
    }

    public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle
    {
        get => _bundle;
        set
        {
            BundleSetterCalls++;
            throw new InvalidOperationException("persist-bundle-set-must-not-run");
        }
    }

    public eSaveFileWriteType WriteTypeToRequest
    {
        get => _writeType;
        set
        {
            WriteTypeSetterCalls++;
            _writeType = value;
        }
    }

    public bool ParameterlessConstructorUsed { get; }
    public int BundleSetterCalls { get; private set; }
    public int WriteTypeSetterCalls { get; private set; }
    public static int SemanticConstructorCalls { get; private set; }

    public static void ResetConstructionCounters() => SemanticConstructorCalls = 0;
}

namespace MissingPersistParameterlessConstructorFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest(FakeIl2CppNullable<ePlayerSaveChangeBundleKey> bundle, eSaveFileWriteType writeType) { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(false, default);
        public eSaveFileWriteType WriteTypeToRequest { get; set; }
    }
}

namespace PrivatePersistParameterlessConstructorFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        private PersistSaveChangeBundleRequest() { }
        public PersistSaveChangeBundleRequest(FakeIl2CppNullable<ePlayerSaveChangeBundleKey> bundle, eSaveFileWriteType writeType) { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(false, default);
        public eSaveFileWriteType WriteTypeToRequest { get; set; }
    }
}

namespace ThrowingPersistParameterlessConstructorFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() => throw new InvalidOperationException("persist-parameterless-constructor");
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(false, default);
        public eSaveFileWriteType WriteTypeToRequest { get; set; }
    }
}

namespace MissingPersistBundleGetterFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public eSaveFileWriteType WriteTypeToRequest { get; set; }
    }
}

namespace PrivatePersistBundleGetterFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle { private get; set; } = new(false, default);
        public eSaveFileWriteType WriteTypeToRequest { get; set; }
    }
}

namespace ThrowingPersistBundleGetterFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => throw new InvalidOperationException("persist-bundle-get");
        public eSaveFileWriteType WriteTypeToRequest { get; set; }
    }
}

namespace UnrelatedNullPersistBundleGetterFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public Il2CppSystem.Nullable<ePlayerSaveChangeBundleKey> Bundle => throw new NullReferenceException("unrelated-persist-null");
        public eSaveFileWriteType WriteTypeToRequest { get; set; }
    }
}

namespace NullPersistBundleReadbackFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey>? Bundle => null;
        public eSaveFileWriteType WriteTypeToRequest { get; set; }
    }
}

sealed class MalformedPersistNullable<T>
{
    public bool HasValue => false;
}

namespace MalformedPersistBundleFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public MalformedPersistNullable<ePlayerSaveChangeBundleKey> Bundle => new();
        public eSaveFileWriteType WriteTypeToRequest { get; set; }
    }
}

namespace PresentInvalidPersistBundleFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(true, ePlayerSaveChangeBundleKey.INVALID);
        public eSaveFileWriteType WriteTypeToRequest { get; set; }
    }
}

namespace PresentDefaultPersistBundleFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(true, ePlayerSaveChangeBundleKey.DEFAULT);
        public eSaveFileWriteType WriteTypeToRequest { get; set; }
    }
}

namespace MissingPersistWriteTypeSetterFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(false, default);
        public eSaveFileWriteType WriteTypeToRequest => eSaveFileWriteType.NON_URGENT;
    }
}

namespace PrivatePersistWriteTypeSetterFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(false, default);
        public eSaveFileWriteType WriteTypeToRequest { get; private set; } = eSaveFileWriteType.NON_URGENT;
    }
}

namespace ThrowingPersistWriteTypeSetterFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(false, default);
        public eSaveFileWriteType WriteTypeToRequest
        {
            get => eSaveFileWriteType.NON_URGENT;
            set => throw new InvalidOperationException("persist-write-set");
        }
    }
}

namespace MissingPersistWriteTypeGetterFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(false, default);
        public eSaveFileWriteType WriteTypeToRequest { set { } }
    }
}

namespace PrivatePersistWriteTypeGetterFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(false, default);
        public eSaveFileWriteType WriteTypeToRequest { private get; set; }
    }
}

namespace ThrowingPersistWriteTypeGetterFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(false, default);
        public eSaveFileWriteType WriteTypeToRequest
        {
            get => throw new InvalidOperationException("persist-write-get");
            set { }
        }
    }
}

namespace MismatchedPersistWriteTypeFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(false, default);
        public eSaveFileWriteType WriteTypeToRequest
        {
            get => eSaveFileWriteType.NON_URGENT;
            set { }
        }
    }
}

namespace EmptyInteropPersistConstructionFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest() { }
        public Il2CppSystem.Nullable<ePlayerSaveChangeBundleKey> Bundle =>
            Il2CppSystem.Nullable<ePlayerSaveChangeBundleKey>.EmptyFromInterop();
        public eSaveFileWriteType WriteTypeToRequest { get; set; } = eSaveFileWriteType.NON_URGENT;
    }
}

namespace RoutingFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public RecordSongCassetteStatusInSaveDataRequest(
            ePlayableSong song,
            eSongCassetteStatus cassetteStatus,
            FakeIl2CppNullable<ePlayerSaveChangeBundleKey> bundle)
        {
            Song = song;
            CassetteStatus = cassetteStatus;
            Bundle = bundle;
        }

        public ePlayableSong Song { get; }
        public eSongCassetteStatus CassetteStatus { get; }
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle { get; }
    }

    sealed class PersistSaveChangeBundleRequest
    {
        public PersistSaveChangeBundleRequest(
            FakeIl2CppNullable<ePlayerSaveChangeBundleKey> bundle,
            eSaveFileWriteType writeType)
        {
            Bundle = bundle;
            WriteTypeToRequest = writeType;
        }

        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle { get; }
        public eSaveFileWriteType WriteTypeToRequest { get; }
    }

}

namespace ThrowingRoutingFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public ePlayableSong Song => ePlayableSong.BADASS;
        public eSongCassetteStatus CassetteStatus => throw new InvalidOperationException("record-status");
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle => new(true, ePlayerSaveChangeBundleKey.DEFAULT);
    }

    sealed class PersistSaveChangeBundleRequest
    {
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle =>
            throw new InvalidOperationException("persist-bundle");
        public eSaveFileWriteType WriteTypeToRequest => eSaveFileWriteType.URGENT;
    }

    sealed class PersistAllSaveChangeBundlesRequest
    {
        public eSaveFileWriteType WriteTypeToRequest =>
            throw new InvalidOperationException("persist-all-write-type");
    }
}

namespace ThrowingWriteTypeRoutingFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle =>
            new(true, ePlayerSaveChangeBundleKey.DEFAULT);
        public eSaveFileWriteType WriteTypeToRequest =>
            throw new InvalidOperationException("persist-write-type");
    }
}

namespace EmptyInteropRoutingFixture
{
    sealed class RecordSongCassetteStatusInSaveDataRequest
    {
        public ePlayableSong Song => ePlayableSong.BADASS;
        public eSongCassetteStatus CassetteStatus => eSongCassetteStatus.HAVE_IN_BAG;
        public Il2CppSystem.Nullable<ePlayerSaveChangeBundleKey> Bundle =>
            Il2CppSystem.Nullable<ePlayerSaveChangeBundleKey>.EmptyFromInterop();
    }

    sealed class PersistSaveChangeBundleRequest
    {
        public Il2CppSystem.Nullable<ePlayerSaveChangeBundleKey> Bundle =>
            Il2CppSystem.Nullable<ePlayerSaveChangeBundleKey>.EmptyFromInterop();
        public eSaveFileWriteType WriteTypeToRequest => eSaveFileWriteType.URGENT;
    }
}

namespace EmptyRoutingFixture
{
    sealed class PersistSaveChangeBundleRequest
    {
        public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle =>
            new(true, ePlayerSaveChangeBundleKey.DEFAULT);
        public string WriteTypeToRequest => string.Empty;
    }

    sealed class PersistAllSaveChangeBundlesRequest
    {
        public string WriteTypeToRequest => string.Empty;
    }
}

sealed class PersistAllSaveChangeBundlesRequest
{
    public PersistAllSaveChangeBundlesRequest(eSaveFileWriteType writeTypeToRequest) =>
        WriteTypeToRequest = writeTypeToRequest;
    public eSaveFileWriteType WriteTypeToRequest { get; }
}

sealed class SaveDataBundleRoutingProcessorFixture
{
    private readonly object _state;
    public SaveDataBundleRoutingProcessorFixture(object state) => _state = state;
    public object ObtainState() => _state;
}

sealed record SaveDataBundleRoutingStateFixture(
    FakeIl2CppNullable<int> SelectedPlayerSaveSlot,
    Dictionary<int, PublicWriteStateFixture> RegularPlayerSaves);

sealed class MissingSelectedSlotSaveDataBundleRoutingStateFixture
{
    public Dictionary<int, PublicWriteStateFixture> RegularPlayerSaves { get; } =
        new() { [4] = new PublicWriteStateFixture() };
}

sealed record PublicTypeKeyFixture(string Name);
public sealed record RegisteredRequestProcessor(object Processor);

static class Il2CppType
{
    public static bool ThrowOnFrom { get; set; }
    public static string? ConvertedNameOverride { get; set; }
    public static Type? LastConvertedType { get; private set; }
    public static PublicTypeKeyFixture From(Type type)
    {
        LastConvertedType = type;
        if (ThrowOnFrom) throw new InvalidOperationException("il2cpp-type-convert");
        return new(ConvertedNameOverride ?? type.Name);
    }
}

sealed class DirectLookupRegistryFixture
{
    private readonly Dictionary<PublicTypeKeyFixture, RegisteredRequestProcessor> _entries;
    public DirectLookupRegistryFixture(Dictionary<PublicTypeKeyFixture, RegisteredRequestProcessor> entries) => _entries = entries;
    public int Count => _entries.Count;
    public int GetEnumeratorCalls { get; private set; }
    public bool TryGetValue(PublicTypeKeyFixture key, out RegisteredRequestProcessor value) =>
        _entries.TryGetValue(key, out value!);
    public Dictionary<PublicTypeKeyFixture, RegisteredRequestProcessor>.Enumerator GetEnumerator()
    {
        GetEnumeratorCalls++;
        throw new InvalidOperationException("registry-enumeration-prohibited");
    }
}

sealed class BroadKeyRegistryFixture
{
    private readonly object _processor;
    public BroadKeyRegistryFixture(object processor) => _processor = processor;
    public int Count => 1;
    public bool TryGetValue(object key, out RegisteredRequestProcessor value)
    {
        value = new(_processor);
        return true;
    }
}

sealed record WrongRegisteredRequestProcessor(object Processor);

sealed class WrongOutRegistryFixture
{
    private readonly object _processor;
    public WrongOutRegistryFixture(object processor) => _processor = processor;
    public int Count => 1;
    public bool TryGetValue(PublicTypeKeyFixture key, out WrongRegisteredRequestProcessor value)
    {
        value = new(_processor);
        return true;
    }
}

sealed class ThrowingCountRegistryFixture
{
    public int Count => throw new InvalidOperationException("registry-count");
    public bool TryGetValue(PublicTypeKeyFixture key, out RegisteredRequestProcessor value)
    {
        value = null!;
        return false;
    }
}

sealed class MissingTryGetRegistryFixture
{
    public MissingTryGetRegistryFixture(int count) => Count = count;
    public int Count { get; }
    public int GetEnumeratorCalls { get; private set; }
    public System.Collections.IEnumerator GetEnumerator()
    {
        GetEnumeratorCalls++;
        throw new InvalidOperationException("registry-enumeration-prohibited");
    }
}

sealed class DiskCommitTargetPlayerProcessorFixture
{
    private readonly object _state;
    public DiskCommitTargetPlayerProcessorFixture(IntPtr pointer, object state) { Pointer = pointer; _state = state; }
    public IntPtr Pointer { get; }
    public int ObtainStateCalls { get; private set; }
    public object ObtainState() { ObtainStateCalls++; return _state; }
}

sealed class PrivateGetterPointerTargetProcessorFixture
{
    private readonly object _state;
    public PrivateGetterPointerTargetProcessorFixture(object state) { Pointer = new IntPtr(0x111); _state = state; }
    public IntPtr Pointer { private get; set; }
    public object ObtainState() => _state;
}

sealed class PrivateGetterHasChangesTargetStateFixture
{
    public PrivateGetterHasChangesTargetStateFixture() => HasChanges = true;
    public IntPtr Pointer => new(0x700);
    public bool HasUnstagedChanges => true;
    public bool HasChanges { private get; set; }
    public bool RequiresWriteToDisk => true;
    public DiskCommitTargetGameProgressionFixture GameProgression { get; } = new(eSongCassetteStatus.HAVE_IN_BAG);
    public Dictionary<ePlayerSaveChangeBundleKey, DiskCommitTargetBundleFixture> saveChangeBundles { get; } = new();
    public eSongCassetteStatus GetCassetteStatusForSong(ePlayableSong song) => eSongCassetteStatus.HAVE_IN_BAG;
}

public sealed class RequestProcessor : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
{
    public RequestProcessor(IntPtr pointer, object? tryCastResult = null) : base(pointer, tryCastResult) { }
}

public class MissingTryCastBaseFixture
{
    public MissingTryCastBaseFixture(IntPtr pointer) => Pointer = pointer;
    public IntPtr Pointer { get; }
}

public sealed class MissingTryCastTargetFixture : MissingTryCastBaseFixture
{
    public MissingTryCastTargetFixture(IntPtr pointer) : base(pointer) { }
}

public class PrivateTryCastBaseFixture
{
    public PrivateTryCastBaseFixture(IntPtr pointer) => Pointer = pointer;
    public IntPtr Pointer { get; }
    private T? TryCast<T>() where T : PrivateTryCastBaseFixture => null;
}

public sealed class PrivateTryCastTargetFixture : PrivateTryCastBaseFixture
{
    public PrivateTryCastTargetFixture(IntPtr pointer) : base(pointer) { }
}

public class PublicTryCastBaseFixture
{
    private readonly object? _castResult;
    public PublicTryCastBaseFixture(IntPtr pointer, object? castResult = null) { Pointer = pointer; _castResult = castResult; }
    public IntPtr Pointer { get; }
    public T? TryCast<T>() where T : PublicTryCastBaseFixture => _castResult as T;
}

public sealed class PublicTryCastTargetFixture : PublicTryCastBaseFixture
{
    public PublicTryCastTargetFixture(IntPtr pointer) : base(pointer) { }
}

sealed class DuplicatePublicTryCastTypeFixture : System.Reflection.TypeDelegator
{
    public DuplicatePublicTryCastTypeFixture(Type delegatingType) : base(delegatingType) { }
    public override System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingAttr)
    {
        System.Reflection.MethodInfo[] methods = base.GetMethods(bindingAttr);
        System.Reflection.MethodInfo? tryCast = methods.SingleOrDefault(method =>
            method.Name == "TryCast" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
        return tryCast == null ? methods : methods.Concat(new[] { tryCast }).ToArray();
    }
}

public class SaveDataRequestProcessor : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
{
    private readonly object _state;
    public SaveDataRequestProcessor(IntPtr pointer, object state) : base(pointer) => _state = state;
    public int ObtainStateCalls { get; private set; }
    public object ObtainState() { ObtainStateCalls++; return _state; }
}

sealed class DerivedSaveDataRequestProcessor : SaveDataRequestProcessor
{
    public DerivedSaveDataRequestProcessor(IntPtr pointer, object state) : base(pointer, state) { }
}

sealed record DiskCommitTargetSaveDataStateFixture(
    IntPtr Pointer,
    FakeIl2CppNullable<int> SelectedPlayerSaveSlot,
    Dictionary<int, DiskCommitTargetPlayerStateFixture> RegularPlayerSaves);

sealed class DiskCommitTargetPlayerStateFixture
{
    private readonly eSongCassetteStatus _effectiveStatus;
    private readonly IntPtr _pointer;
    public DiskCommitTargetPlayerStateFixture(
        IntPtr pointer, eSongCassetteStatus effectiveStatus, eSongCassetteStatus canonicalStatus,
        bool hasUnstagedChanges, bool hasChanges, bool requiresWriteToDisk, int? defaultBundleChanges)
    {
        _pointer = pointer; _effectiveStatus = effectiveStatus; GameProgression = new(canonicalStatus);
        HasUnstagedChanges = hasUnstagedChanges; _hasChanges = hasChanges; RequiresWriteToDisk = requiresWriteToDisk;
        saveChangeBundles = defaultBundleChanges.HasValue
            ? new() { [ePlayerSaveChangeBundleKey.DEFAULT] = new(defaultBundleChanges.Value) }
            : new();
    }
    public bool ThrowPointer { get; set; }
    public IntPtr Pointer => ThrowPointer ? throw new InvalidOperationException("target-pointer") : _pointer;
    public bool HasUnstagedChanges { get; }
    private readonly bool _hasChanges;
    public bool ThrowHasChanges { get; set; }
    public bool HasChanges => ThrowHasChanges ? throw new InvalidOperationException("target-has-changes") : _hasChanges;
    public bool RequiresWriteToDisk { get; }
    public DiskCommitTargetGameProgressionFixture GameProgression { get; }
    public Dictionary<ePlayerSaveChangeBundleKey, DiskCommitTargetBundleFixture> saveChangeBundles { get; }
    public eSongCassetteStatus GetCassetteStatusForSong(ePlayableSong song) => _effectiveStatus;
}

sealed record DiskCommitTargetGameProgressionFixture(eSongCassetteStatus Status)
{
    public FakeIl2CppNullable<eSongCassetteStatus> GetSongCassetteStatus(ePlayableSong song) => new(true, Status);
}

sealed record DiskCommitTargetBundleFixture(int ChangeCount)
{
    public List<int> Changes { get; } = Enumerable.Range(0, ChangeCount).ToList();
}

static class RequestSystem
{
    public static object registeredProcessorsWrapped { get; private set; } = new DirectLookupRegistryFixture(new());
    public static int SubmitCount { get; private set; }
    public static PersistSaveChangeBundleRequest? LastRequest { get; private set; }
    public static ePlayerSaveChangeBundleKey EffectiveBundle { get; private set; }
    public static eSaveFileWriteType EffectiveWriteType { get; private set; }
    public static void SubmitRequest<T>(T request)
    {
        SubmitCount++;
        LastRequest = request as PersistSaveChangeBundleRequest;
        if (LastRequest != null)
        {
            EffectiveBundle = LastRequest.Bundle.HasValue
                ? LastRequest.Bundle.Value
                : ePlayerSaveChangeBundleKey.DEFAULT;
            EffectiveWriteType = LastRequest.WriteTypeToRequest;
        }
    }
    public static void Reset()
    {
        SubmitCount = 0;
        LastRequest = null;
        EffectiveBundle = ePlayerSaveChangeBundleKey.INVALID;
        EffectiveWriteType = eSaveFileWriteType.NON_URGENT;
        registeredProcessorsWrapped = new DirectLookupRegistryFixture(new());
    }
    public static void SetRegisteredProcessors(object processors) =>
        registeredProcessorsWrapped = processors;
}

sealed class PlayerSaveRequestProcessor
{
    private readonly TransactionSaveState _state;
    public int ObtainStateCalls { get; private set; }
    public int ProcessRequestCalls { get; private set; }
    public RecordSongCassetteStatusInSaveDataRequest? LastRequest { get; private set; }
    public ePlayerSaveChangeBundleKey EffectiveBundle { get; private set; }

    public PlayerSaveRequestProcessor(TransactionSaveState state) => _state = state;

    public TransactionSaveState ObtainState()
    {
        ObtainStateCalls++;
        return _state;
    }

    public void ProcessRequest(RecordSongCassetteStatusInSaveDataRequest request)
    {
        ProcessRequestCalls++;
        LastRequest = request;
        EffectiveBundle = request.Bundle.HasValue
            ? request.Bundle.Value
            : ePlayerSaveChangeBundleKey.DEFAULT;
    }
}

static class PrivateOnlyFixture
{
    public sealed class PlayerSaveRequestProcessor
    {
        private TransactionSaveState ObtainState() => new(eSongCassetteStatus.HAVE_IN_BAG);
    }
}

sealed class TransactionSaveState
{
    private readonly eSongCassetteStatus _status;
    public TransactionSaveState(eSongCassetteStatus status, IntPtr? pointer = null)
    {
        _status = status;
        Pointer = pointer ?? new IntPtr(0x700);
        GameStats = new FakeGameStats(777, new DateTime(2026, 8, 31, 13, 0, 0, DateTimeKind.Utc));
    }
    public IntPtr Pointer { get; }
    public FakeGameStats GameStats { get; }
    public eSongCassetteStatus GetCassetteStatusForSong(ePlayableSong song) => _status;
}

sealed class SaveDataProcessorFixture
{
    private readonly object _state;
    public SaveDataProcessorFixture(object state) => _state = state;
    public object ObtainState() => _state;
}

sealed record RegularSaveStateFixture(IntPtr Pointer, long LastPlayTicks)
{
    public FakeGameStats GameStats { get; } = new(1, new DateTime(LastPlayTicks, DateTimeKind.Utc));
}

sealed record SaveDataStateFixture(Dictionary<int, RegularSaveStateFixture?> RegularPlayerSaves);

sealed class BadKeySaveDataStateFixture
{
    public Dictionary<string, RegularSaveStateFixture> RegularPlayerSaves { get; } =
        new() { ["not-a-slot"] = new(new IntPtr(0x700), 3000) };
}

sealed class ThrowingSaveDataStateFixture
{
    public ThrowingEnumerable RegularPlayerSaves { get; } = new();
}

sealed class ThrowingEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => throw new InvalidOperationException("enumeration failed");
}

sealed class PairEnumerable
{
    private readonly object _pair;
    public PairEnumerable(object pair) => _pair = pair;
    public System.Collections.IEnumerator GetEnumerator() => new List<object> { _pair }.GetEnumerator();
}

abstract class SingleEntrySaveDataStateFixture
{
    protected SingleEntrySaveDataStateFixture(object pair) => RegularPlayerSaves = new PairEnumerable(pair);
    public PairEnumerable RegularPlayerSaves { get; }
}

sealed class MissingKeySaveDataStateFixture : SingleEntrySaveDataStateFixture
{
    public MissingKeySaveDataStateFixture() : base(new PairWithoutKey(new RegularSaveStateFixture(new IntPtr(0x700), 3000))) { }
}
sealed record PairWithoutKey(object Value);

sealed class NullKeySaveDataStateFixture : SingleEntrySaveDataStateFixture
{
    public NullKeySaveDataStateFixture() : base(new PairWithNullableKey(null, new RegularSaveStateFixture(new IntPtr(0x700), 3000))) { }
}
sealed record PairWithNullableKey(object? Key, object Value);

sealed class MissingGameStatsSaveDataStateFixture : SingleEntrySaveDataStateFixture
{
    public MissingGameStatsSaveDataStateFixture() : base(new KeyValuePair<int, object>(3, new PointerOnlyState())) { }
}
sealed record PointerOnlyState { public IntPtr Pointer => new(0x700); }

sealed class NullGameStatsSaveDataStateFixture : SingleEntrySaveDataStateFixture
{
    public NullGameStatsSaveDataStateFixture() : base(new KeyValuePair<int, object>(3, new NullGameStatsState())) { }
}
sealed record NullGameStatsState { public IntPtr Pointer => new(0x700); public object? GameStats => null; }

sealed class MissingLastPlaySaveDataStateFixture : SingleEntrySaveDataStateFixture
{
    public MissingLastPlaySaveDataStateFixture() : base(new KeyValuePair<int, object>(3, new MissingLastPlayState())) { }
}
sealed record MissingLastPlayState { public IntPtr Pointer => new(0x700); public object GameStats => new object(); }

sealed class NullLastPlaySaveDataStateFixture : SingleEntrySaveDataStateFixture
{
    public NullLastPlaySaveDataStateFixture() : base(new KeyValuePair<int, object>(3, new NullLastPlayState())) { }
}
sealed record NullLastPlayState { public IntPtr Pointer => new(0x700); public NullLastPlayStats GameStats => new(); }
sealed record NullLastPlayStats { public object? LastPlayDateTimeUtc => null; }

sealed class MissingTicksSaveDataStateFixture : SingleEntrySaveDataStateFixture
{
    public MissingTicksSaveDataStateFixture() : base(new KeyValuePair<int, object>(3, new MissingTicksState())) { }
}
sealed record MissingTicksState { public IntPtr Pointer => new(0x700); public MissingTicksStats GameStats => new(); }
sealed record MissingTicksStats { public object LastPlayDateTimeUtc => new object(); }

sealed class NullTicksSaveDataStateFixture : SingleEntrySaveDataStateFixture
{
    public NullTicksSaveDataStateFixture() : base(new KeyValuePair<int, object>(3, new NullTicksState())) { }
}
sealed record NullTicksState { public IntPtr Pointer => new(0x700); public NullTicksStats GameStats => new(); }
sealed record NullTicksStats { public NullTicksDate LastPlayDateTimeUtc => new(); }
sealed record NullTicksDate { public object? Ticks => null; }

sealed class RecordSongCassetteStatusInSaveDataRequest
{
    private FakeIl2CppNullable<ePlayerSaveChangeBundleKey> _bundle = new(false, default);

    public ePlayableSong Song { get; set; }
    public eSongCassetteStatus CassetteStatus { get; set; }
    public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle
    {
        get => _bundle;
        set
        {
            BundleSetterCalls++;
            throw new InvalidOperationException("bundle-set-must-not-run");
        }
    }
    public bool ParameterlessConstructorUsed { get; }
    public int BundleSetterCalls { get; private set; }
    public static int SemanticConstructorCalls { get; private set; }

    public RecordSongCassetteStatusInSaveDataRequest() => ParameterlessConstructorUsed = true;

    public RecordSongCassetteStatusInSaveDataRequest(ePlayableSong song, eSongCassetteStatus cassetteStatus, ePlayerSaveChangeBundleKey bundle)
    {
        SemanticConstructorCalls++;
        throw new InvalidOperationException("semantic-constructor-must-not-run");
    }
}

namespace MissingFailureReasonFixture
{
    sealed class PlayerSaveWriteCompletedEvent
    {
        public PlayerSaveWriteCompletedEvent(int slotNumber, bool succeeded)
        { SlotNumber = slotNumber; Succeeded = succeeded; }
        public int SlotNumber { get; }
        public bool Succeeded { get; }
    }
}

namespace ThrowingFailureReasonFixture
{
    sealed class PlayerSaveWriteCompletedEvent
    {
        public PlayerSaveWriteCompletedEvent(int slotNumber, bool succeeded)
        { SlotNumber = slotNumber; Succeeded = succeeded; }
        public int SlotNumber { get; }
        public bool Succeeded { get; }
        public object FailureReason => throw new NullReferenceException("optional-reason-unavailable");
    }
}
