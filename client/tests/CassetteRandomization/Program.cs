using RhythmCastleAP;
using Newtonsoft.Json.Linq;
using System.Text.Json;

static void Equal<T>(T expected, T actual, string scenario) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}"); }
static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual, string scenario) { var e=expected.ToArray(); var a=actual.ToArray(); if (!e.SequenceEqual(a, StringComparer.Ordinal)) throw new InvalidOperationException($"{scenario}: expected [{string.Join(", ",e)}], got [{string.Join(", ",a)}]"); }
static string ReadTextNormalized(string path) => File.ReadAllText(path).ReplaceLineEndings("\n");
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

bool cassetteHandlerCalled = false;
bool experimentalFallbackCalled = false;
bool cassetteHandledWithExperimentalProgressionDisabled = ReceivedItemDispatch.TryApply(
    "Heavy Metal Cassette",
    applyReceivedProgression: false,
    areaAccessHandler: _ => false,
    garageCartridgeHandler: _ => false,
    weedKillerHandler: _ => false,
    plantPipesHandler: _ => false,
    previewAbilityHandler: _ => false,
    rootsBucketHandler: _ => false,
    cassetteHandler: itemName =>
    {
        cassetteHandlerCalled = itemName == "Heavy Metal Cassette";
        return true;
    },
    experimentalFallback: _ => experimentalFallbackCalled = true);
Equal(true, cassetteHandledWithExperimentalProgressionDisabled, "cassette dispatch is handled when experimental progression is disabled");
Equal(true, cassetteHandlerCalled, "cassette dispatch invokes the always-on cassette handler");
Equal(false, experimentalFallbackCalled, "cassette dispatch does not invoke experimental fallback");

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

Equal(0, RequestSystem.SubmitCount, "semantic cassette request construction never reaches global persist submission");
string pluginSource = ReadTextNormalized(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));
string requestFactorySource = ReadTextNormalized(Path.Combine(Directory.GetCurrentDirectory(), "client", "CassetteNativeRequestFactory.cs"));
string transactionAdapterSource = ReadTextNormalized(Path.Combine(Directory.GetCurrentDirectory(), "client", "CassetteSaveTransactionAdapter.cs"));
string cassetteSaveDesignSource = ReadTextNormalized(Path.Combine(
    Directory.GetCurrentDirectory(), "docs", "superpowers", "specs", "2026-08-30-cassette-save-transaction-design.md"));
int acceptanceEventGuard = pluginSource.IndexOf("if (cassettePointerBoundPersistenceAcceptance.Value)", StringComparison.Ordinal);
int acceptanceEventHook = pluginSource.IndexOf("PatchMethodsByParameter(\"HandleEvent\", \"PlayerSaveWriteCompletedEvent\"", StringComparison.Ordinal);
Equal(true, acceptanceEventGuard >= 0 && acceptanceEventHook > acceptanceEventGuard, "write-completed event observation is narrowly installed only after the acceptance opt-in guard");
Equal(false, pluginSource.Contains("TickDiskCommit(elapsed);", StringComparison.Ordinal), "Unity reconciliation never advances the retired synthetic disk-commit state machine");
foreach (string prohibitedWritePath in new[] { "TriggerUrgentSaveWriteIfAnyChangesRequest", "RequestWriteForPlayerSave" })
    Equal(false, pluginSource.Contains(prohibitedWritePath, StringComparison.Ordinal) || transactionAdapterSource.Contains(prohibitedWritePath, StringComparison.Ordinal), $"production avoids prohibited forced write path {prohibitedWritePath}");

Equal(true, pluginSource.Contains("PatchExactMethod(\"SaveDataRequestProcessor\", \"ChangeSelectedPlayerSaveSlot\", \"Int32\", nameof(CassetteSaveTransactionPatches.SelectedSlotMutationPostfix))", StringComparison.Ordinal), "exact selected-slot mutation hook installed");
Equal(true, pluginSource.Contains("PatchExactMethod(\"SaveDataRequestProcessor\", \"CreateNewPlayerSaveFileInEmptySlot\", \"Int32\", nameof(CassetteSaveTransactionPatches.SelectedSlotMutationPostfix))", StringComparison.Ordinal), "exact empty-slot creation hook installed");
Equal(true, pluginSource.Contains("PatchExactMethod(\"SaveDataRequestProcessor\", \"ProcessRequest\", \"BuildPlayerSaveStateFromFileRequest\", nameof(CassetteSaveTransactionPatches.BuiltPlayerSaveStatePostfix))", StringComparison.Ordinal), "exact save-state build hook retains the authoritative lifecycle observation");
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
Equal(false, pluginSource.Contains("PersistBundleRoutingPrefix", StringComparison.Ordinal), "cassette production wiring does not observe or drive persist-bundle traffic");
Equal(false, pluginSource.Contains("PersistAllRoutingPrefix", StringComparison.Ordinal), "cassette production wiring does not observe or drive persist-all traffic");
Equal(false, pluginSource.Contains("DiscardAllRoutingPrefix", StringComparison.Ordinal), "cassette production wiring does not consume unstaged-discard traffic");

Equal(false, pluginSource.Contains("nameof(CassetteSaveTransactionPatches.SaveSelectionPostfix)", StringComparison.Ordinal), "broad selection postfix is non-authoritative");
string selectedMutationPostfix = ExtractMethods(pluginSource, "public static void SelectedSlotMutationPostfix(").Single();
Equal(true, selectedMutationPostfix.Contains("QueueSaveBoundarySignal", StringComparison.Ordinal), "slot mutation callback only queues boundary signal");
Equal(true, selectedMutationPostfix.IndexOf("SuspendGameplayReadinessForBoundary", StringComparison.Ordinal) < selectedMutationPostfix.IndexOf("__args == null", StringComparison.Ordinal), "slot mutation suspends gameplay readiness before null argument extraction");
Equal(true, selectedMutationPostfix.IndexOf("SuspendGameplayReadinessForBoundary", StringComparison.Ordinal) < selectedMutationPostfix.IndexOf("__args[0]", StringComparison.Ordinal), "slot mutation suspends gameplay readiness before slot extraction");
Equal(true, selectedMutationPostfix.Contains("LogExtractionFailureOnce", StringComparison.Ordinal), "slot mutation extraction failure is logged once");
Equal(true, selectedMutationPostfix.Contains("__originalMethod?.DeclaringType?.Name", StringComparison.Ordinal), "slot mutation diagnostic includes safe method owner identity");
Equal(false, selectedMutationPostfix.Contains("TryGetLoadedSave", StringComparison.Ordinal), "slot mutation callback performs no native enquiry");
Equal(false, selectedMutationPostfix.Contains("ActivateLoadedSave", StringComparison.Ordinal), "slot mutation callback cannot activate epoch");
string buildPostfix = ExtractMethods(pluginSource, "public static void BuiltPlayerSaveStatePostfix(").Single();
Equal(true, buildPostfix.Contains("QueueSaveBoundarySignal", StringComparison.Ordinal), "build callback only queues boundary signal");
Equal(true, buildPostfix.IndexOf("SuspendGameplayReadinessForBoundary", StringComparison.Ordinal) < buildPostfix.IndexOf("ReflectionUtil.FindArg", StringComparison.Ordinal), "build callback suspends gameplay readiness before request extraction");
Equal(true, buildPostfix.IndexOf("SuspendGameplayReadinessForBoundary", StringComparison.Ordinal) < buildPostfix.IndexOf("ReflectionUtil.ReadInt", StringComparison.Ordinal), "build callback suspends gameplay readiness before slot extraction");
Equal(true, buildPostfix.Contains("LogExtractionFailureOnce", StringComparison.Ordinal), "build extraction failure is logged once");
Equal(true, buildPostfix.Contains("BuildPlayerSaveStateFromFileRequest", StringComparison.Ordinal), "build diagnostic identifies exact request type safely");
Equal(false, buildPostfix.Contains("TryGetLoadedSave", StringComparison.Ordinal), "build callback performs no native enquiry");
Equal(false, buildPostfix.Contains("Persist", StringComparison.Ordinal), "build observation cannot trigger cassette persistence");
string extractionDiagnostic = ExtractMethods(pluginSource, "private static void LogExtractionFailureOnce(").Single();
Equal(true, extractionDiagnostic.Contains("ExtractionFailures.Add", StringComparison.Ordinal), "extraction diagnostics are bounded by a one-time key set");
Equal(true, extractionDiagnostic.Contains("gameplay readiness was suspended before extraction", StringComparison.Ordinal), "bounded malformed-boundary diagnostic states that the old gate already failed closed");
Equal(false, extractionDiagnostic.Contains("ReflectionUtil.ReadMember", StringComparison.Ordinal), "diagnostic logger performs no unsafe object traversal");
string recordRoutingPrefix = ExtractMethods(pluginSource, "public static bool CassetteStatusRequestPrefix(").Single();
Equal(false, recordRoutingPrefix.Contains("BeginBundleRoutingDiagnostic", StringComparison.Ordinal), "semantic record-song interception no longer starts disk-routing diagnostics");
Equal(false, pluginSource.Contains("public static void CassetteStatusRequestPostfix(", StringComparison.Ordinal), "record-song processor needs no persistence postfix");
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
Equal(true, reconcileSongSource.Contains("verificationDue &&", StringComparison.Ordinal) && reconcileSongSource.Contains("_runtime.RecordVerification(nativeSong, null)", StringComparison.Ordinal), "an unreadable scheduled verification is consumed once without enabling a duplicate grant");
Equal(true, reconcileSongSource.Contains("_productionPersistence.CanSubmitNativeCassetteGrant(", StringComparison.Ordinal),
    "semantic grant admission delegates active-write coordination to the executable production coordinator");

string cassetteProcessorCapture = ExtractMethods(pluginSource, "internal static void CapturePlayerSaveRequestProcessor(object? instance, bool reconcileNow = true)").Single();
Equal(true, cassetteProcessorCapture.Contains("CASSETTE PLAYER PROCESSOR CAPTURED", StringComparison.Ordinal), "compatible player processor capture is observable");
Equal(false, cassetteProcessorCapture.Contains("TrySubmitHaveInBag", StringComparison.Ordinal), "processor diagnostic performs no native cassette write");
string cassetteProcessorFallback = ExtractMethods(pluginSource, "private static bool EnsureCassetteProcessorAvailable(").Single();
Equal(true, cassetteProcessorFallback.Contains("CassetteSaveTransactionAdapter.TryGetLoadedSave(out _)", StringComparison.Ordinal), "stateless processor fallback requires readable selected save");
Equal(true, cassetteProcessorFallback.Contains("PlayerSaveRequestProcessor", StringComparison.Ordinal), "stateless fallback constructs only the proven processor type");

string receiptRandomizationSource = ExtractClass(pluginSource, "CassetteReceiptRandomization");
string persistencePolicySource = File.ReadAllText(Path.Combine(
    Directory.GetCurrentDirectory(), "client", "CassetteRandomizationPolicy.cs"));
string persistenceCoordinatorSource = ExtractClass(
    persistencePolicySource, "CassetteProductionPersistenceCoordinator");
string receiptApplySlotData = ExtractMethods(receiptRandomizationSource, "internal static void ApplySlotData(").Single();
Equal(true, receiptApplySlotData.Contains("RequestUnityReconciliation(\"slot data synchronized\")", StringComparison.Ordinal), "slot-data synchronization queues Unity-thread work");
Equal(false, receiptApplySlotData.Contains("TryReconcile(", StringComparison.Ordinal), "slot-data synchronization performs no native reconciliation");
string receiptTryApply = ExtractMethods(receiptRandomizationSource, "internal static bool TryApplyItem(").Single();
Equal(true, receiptTryApply.Contains("RequestUnityReconciliation(\"AP cassette receipt\")", StringComparison.Ordinal), "active-save AP receipt schedules prompt Unity-thread reconciliation");
Equal(false, receiptTryApply.Contains("TryReconcile(", StringComparison.Ordinal), "network receipt performs no native reconciliation");
string unityTick = ExtractMethods(receiptRandomizationSource, "internal static void TickUnity(").Single();
Equal(true, unityTick.Contains("_runtime.Tick(elapsed)", StringComparison.Ordinal), "Unity keeper advances bounded verification timers with actual elapsed time");
Equal(true, unityTick.IndexOf("if (!_saveSynchronizationReady) return", StringComparison.Ordinal) < unityTick.IndexOf("_runtime.Tick(elapsed)", StringComparison.Ordinal), "deferred synchronization freezes semantic grant verification until an observation proves readiness");
Equal(false, unityTick.Contains("TickDiskCommit", StringComparison.Ordinal), "Unity keeper does not run a synthetic disk-commit timer");
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
Equal(true, reconcileSongSource.Contains("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal), "cassette reconciliation requires the public save synchronization gate");
Equal(true, reconcileSongSource.IndexOf("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal) < reconcileSongSource.IndexOf("TryReadCassetteStatus", StringComparison.Ordinal), "synchronization is proven before reconciliation reads or mutates cassette status");
Equal(true, reconcileSongSource.IndexOf("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal) < reconcileSongSource.IndexOf("TrySubmitHaveInBag", StringComparison.Ordinal), "synchronization is proven before any cassette grant request");
Equal(true, reconcileSongSource.Contains("allowObservationProbe: true", StringComparison.Ordinal), "every actual cassette status read, grant, or delayed verification freshly probes public validity and selected-state pointer");
Equal(false, reconcileSongSource.Contains("allowObservationProbe: false", StringComparison.Ordinal), "no native cassette operation may rely on cached readiness");
string synchronizationGateSource = ExtractMethods(receiptRandomizationSource, "private static bool TryConfirmSaveSynchronizationReady(").Single();
Equal(true, synchronizationGateSource.Contains("allowObservationProbe", StringComparison.Ordinal), "synchronization helper distinguishes observation work from frame-only bookkeeping");
Equal(false, unityTick.Contains("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal), "a Unity frame with no reconciliation or due verification performs no public readiness probe");
Equal(false, synchronizationGateSource.Contains("TrySubmit", StringComparison.Ordinal), "synchronization diagnostic wiring cannot submit grant or persistence requests");
Equal(true, synchronizationGateSource.Contains("TryConfirmSaveSynchronizationReady(pointer", StringComparison.Ordinal), "production readiness delegates only the active epoch pointer to the public selection enquiry");
Equal(false, synchronizationGateSource.Contains("_joinedSaveDataRequestProcessor", StringComparison.Ordinal), "production readiness does not depend on the retained global processor's numeric selected slot");
Equal(true, pluginSource.Contains("EnableCassettePointerBoundPersistenceAcceptance\", false", StringComparison.Ordinal), "pointer-bound persistence acceptance config is explicitly default false");
Equal(true, pluginSource.Contains("if (cassettePointerBoundPersistenceAcceptance.Value)", StringComparison.Ordinal), "write-completed event hook is installed only for an explicit startup opt-in");
Equal(true, pluginSource.Contains("CassetteReceiptRandomization.Configure(\n            acceptanceDiagnosticsEnabled: cassettePointerBoundPersistenceAcceptance.Value)", StringComparison.Ordinal),
    "default-false acceptance configuration controls diagnostics only");
string acceptanceConfigUpdate = ExtractMethods(receiptRandomizationSource, "internal static void SetPersistenceAcceptanceDiagnosticsEnabled(").Single();
Equal(true, acceptanceConfigUpdate.Contains("if (enabled)", StringComparison.Ordinal) && acceptanceConfigUpdate.Contains("restart is required", StringComparison.Ordinal),
    "runtime diagnostic opt-in cannot start without the startup-only event hook; enabling requires a restart");
Equal(false, acceptanceConfigUpdate.Contains("_productionPersistence.Cancel", StringComparison.Ordinal),
    "changing acceptance diagnostics cannot cancel or control production durability");
Equal(false, unityTick.Contains("TrySubmitDefaultUrgentPersist", StringComparison.Ordinal), "Unity update cannot submit synthetic Persist requests");
int delayedVerificationLoopIndex = unityTick.IndexOf("foreach (string song in ready) TryReconcileSong", StringComparison.Ordinal);
int diagnosticWaveConsumeIndex = unityTick.IndexOf("TryConsumeNewlyVerifiedGrantDiagnosticWave", StringComparison.Ordinal);
Equal(true, delayedVerificationLoopIndex >= 0 && diagnosticWaveConsumeIndex > delayedVerificationLoopIndex, "Unity update consumes a diagnostic wave only after every due delayed verification has run");
int productionWaveEnqueueIndex = unityTick.IndexOf("_productionPersistence.TryEnqueue", diagnosticWaveConsumeIndex, StringComparison.Ordinal);
int persistenceEligibilityIndex = unityTick.IndexOf("TryStartPointerBoundPersistence", diagnosticWaveConsumeIndex, StringComparison.Ordinal);
Equal(true, productionWaveEnqueueIndex > diagnosticWaveConsumeIndex && persistenceEligibilityIndex > productionWaveEnqueueIndex,
    "newly verified waves are identity-bound and queued before any production eligibility attempt");
Equal(true, unityTick.Contains("LogPersistenceTargetOwnershipDiagnostic", StringComparison.Ordinal), "newly verified grant waves schedule the bounded ownership snapshot immediately afterward");
string persistenceTargetLogger = ExtractMethods(receiptRandomizationSource, "private static void LogPersistenceTargetOwnershipDiagnostic(").Single();
foreach (string requiredRead in new[] { "ReadCassettePostLoadDiagnostic", "TryReadDiskCommitTargetDiagnostic", "TryReadPersistenceTargetActiveState", "TryReadPublicWriteDiagnosticState" })
    Equal(true, persistenceTargetLogger.Contains(requiredRead, StringComparison.Ordinal), $"persistence target snapshot uses existing public diagnostic boundary {requiredRead}");
foreach (string prohibitedMutation in new[] { "TrySubmitHaveInBag", "SubmitRequest", "ProcessRequest", "PersistAllChangesInBundle", "RequestUrgentWriteToDisk", "TriggerUrgentSaveWriteIfAnyChangesRequest", "RequestWriteForPlayerSave" })
    Equal(false, persistenceTargetLogger.Contains(prohibitedMutation, StringComparison.Ordinal), $"persistence target snapshot contains no mutation path {prohibitedMutation}");
Equal(true, persistenceTargetLogger.Contains("CASSETTE PERSISTENCE TARGET SNAPSHOT", StringComparison.Ordinal), "persistence target diagnostic has one exact live log marker");
string persistenceStarter = ExtractMethods(receiptRandomizationSource, "private static void TryStartPointerBoundPersistence(").Single();
int persistencePlanIndex = persistenceStarter.IndexOf("TryPreparePointerBoundPersistenceInvocation", StringComparison.Ordinal);
int persistenceTokenIndex = persistenceStarter.IndexOf("_productionPersistence.TryInvoke", StringComparison.Ordinal);
int persistenceInvokeIndex = persistenceStarter.IndexOf("InvokePointerBoundPersistence", StringComparison.Ordinal);
Equal(true, persistencePlanIndex >= 0 && persistenceTokenIndex > persistencePlanIndex && persistenceInvokeIndex > persistenceTokenIndex,
    "production resolves the immutable public call plan, then delegates token installation before the adapter callback mutates");
Equal(true, persistenceStarter.Contains("CassettePersistenceAcceptanceEligibility.Evaluate", StringComparison.Ordinal), "production starter executes the complete pure gate before preparing");
Equal(true, persistenceStarter.Contains("_productionPersistence.TryPeek", StringComparison.Ordinal),
    "production starts only from the exact identity-bound wave exposed by the coordinator");
Equal(true, persistenceStarter.Contains("OptInEnabled: true", StringComparison.Ordinal) &&
            persistenceStarter.Contains("routingCompatible = _slotDataSynchronized && Enabled", StringComparison.Ordinal),
    "compatible full cassette routing enables production independently of default-false diagnostics");
Equal(false, persistenceStarter.Contains("_acceptanceDiagnosticsEnabled", StringComparison.Ordinal),
    "diagnostic configuration cannot gate or cancel production persistence");
int persistencePrepareAdmissionIndex = persistenceCoordinatorSource.IndexOf("_markers.TryBeginAttempt", StringComparison.Ordinal);
int persistenceAdapterCallbackIndex = persistenceCoordinatorSource.IndexOf("result = invokeAdapter()", persistencePrepareAdmissionIndex, StringComparison.Ordinal);
int persistenceMarkInvokedIndex = persistenceCoordinatorSource.IndexOf("_runtime.MarkInvoked", persistenceAdapterCallbackIndex, StringComparison.Ordinal);
int persistenceInvokedAdmissionIndex = persistenceCoordinatorSource.IndexOf("\"INVOKED\"", persistenceMarkInvokedIndex, StringComparison.Ordinal);
Equal(true, persistencePrepareAdmissionIndex >= 0 &&
            persistenceAdapterCallbackIndex > persistencePrepareAdmissionIndex &&
            persistenceMarkInvokedIndex > persistenceAdapterCallbackIndex &&
            persistenceInvokedAdmissionIndex > persistenceMarkInvokedIndex,
    "coordinator installs PRE before the adapter callback and admits INVOKED only after the still-active token accepts it");
string acceptanceEventObserver = ExtractMethods(
    receiptRandomizationSource, "internal static void ObservePersistenceAcceptanceWriteCompletedEvent(").Single();
Equal(true, acceptanceEventObserver.Contains("_acceptanceDiagnosticsEnabled", StringComparison.Ordinal),
    "uncorrelated native event hints are observed only when startup diagnostics were enabled");
Equal(false, acceptanceEventObserver.Contains("terminal: true", StringComparison.Ordinal),
    "uncorrelated native events never emit a terminal production marker");
string persistenceMarkerDrain = ExtractMethods(
    receiptRandomizationSource, "private static void DrainPersistenceMarkers(").Single();
Equal(true, persistenceMarkerDrain.Contains("_persistenceMarkerEmitter.Drain", StringComparison.Ordinal) &&
            persistenceMarkerDrain.Contains("TryTakePersistenceMarker", StringComparison.Ordinal) &&
            persistenceMarkerDrain.Contains("EmitPersistenceMarker", StringComparison.Ordinal),
    "all admitted markers use one serialized drain/emitter path");
string persistenceMarkerEmitter = ExtractMethods(
    receiptRandomizationSource, "private static void EmitPersistenceMarker(").Single();
Equal(true, persistenceMarkerEmitter.Contains("LogPersistence", StringComparison.Ordinal),
    "only the serialized marker emitter reaches the physical production logger");
Equal(2, receiptRandomizationSource.Split("LogPersistence(", StringSplitOptions.None).Length - 1,
    "production lifecycle has exactly one logger definition and one serialized emitter call; transition callers never log independently");
Equal(true, persistenceCoordinatorSource.Contains("_markers.TryBeginAttempt", StringComparison.Ordinal),
    "PRE admission is atomic with attempt preparation inside the coordinator");
Equal(true, acceptanceEventObserver.Contains("_productionPersistence.ObserveWriteCompletedEvent", StringComparison.Ordinal) &&
            persistenceCoordinatorSource.Contains("_markers.TryAppend", StringComparison.Ordinal),
    "event observation and marker admission delegate atomically to the coordinator");
foreach (string prohibitedAcceptance in new[] { "SubmitRequest", "ProcessRequest", "PersistSaveChangeBundleRequest", "PersistAllSaveChangeBundlesRequest", "TriggerUrgentSaveWriteIfAnyChangesRequest", "RequestWriteForPlayerSave", "SaveDataManager" })
    Equal(false, persistenceStarter.Contains(prohibitedAcceptance, StringComparison.Ordinal), $"production starter cannot use prohibited path {prohibitedAcceptance}");
foreach (string marker in new[] { "PRE", "INVOKED", "IMMEDIATE POST", "VERIFIED", "FAILED", "TIMEOUT", "CANCELLED" })
    Equal(true, receiptRandomizationSource.Contains($"CASSETTE PERSISTENCE {marker}", StringComparison.Ordinal), $"production emits exact bounded marker {marker}");
Equal(false, receiptRandomizationSource.Contains("CASSETTE PERSISTENCE EVENT", StringComparison.Ordinal),
    "normal production has no event lifecycle marker");
Equal(true, persistenceCoordinatorSource.Contains("CassettePersistenceAttemptPolicy.SequentialVerifiedBatches", StringComparison.Ordinal),
    "compatible routing uses the sequential production persistence policy independently of diagnostics");
string persistencePoll = ExtractMethods(receiptRandomizationSource, "private static void PollPointerBoundPersistence(").Single();
Equal(true, persistencePoll.Contains("_productionPersistence.Observe", StringComparison.Ordinal) &&
            persistenceCoordinatorSource.Contains("_waves.Complete", StringComparison.Ordinal) &&
            persistenceCoordinatorSource.Contains("_requestReconciliation", StringComparison.Ordinal),
    "VERIFIED delegates exact-wave completion and later-batch reconciliation to the coordinator");
Equal(true, persistenceCoordinatorSource.Contains("_waves.TombstoneEpoch", StringComparison.Ordinal),
    "FAILED, TIMEOUT, and indeterminate invocation tombstone later same-epoch work inside the coordinator");
string cancelPersistence = ExtractMethods(receiptRandomizationSource, "private static void CancelPointerBoundPersistence(").Single();
Equal(true, cancelPersistence.Contains("_productionPersistence.CancelEpoch", StringComparison.Ordinal),
    "save boundaries delegate cancellation and queue clearing to the coordinator");
Equal(false, receiptRandomizationSource.Contains("new CassettePersistenceWaveQueue", StringComparison.Ordinal) ||
             receiptRandomizationSource.Contains("new CassettePointerBoundPersistenceRuntime", StringComparison.Ordinal) ||
             receiptRandomizationSource.Contains("new CassettePersistenceAcceptanceMarkerJournal", StringComparison.Ordinal),
    "Plugin does not duplicate the coordinator's queue, runtime, or marker state");
string keeperSource = ExtractClass(pluginSource, "CassetteReceiptReconciliationKeeper");
Equal(true, keeperSource.Contains("Stopwatch.GetTimestamp()", StringComparison.Ordinal), "keeper uses a monotonic production clock");
Equal(true, keeperSource.Contains("CassetteReceiptRandomization.TickUnity(elapsed)", StringComparison.Ordinal), "keeper passes actual elapsed time every Unity update");
Equal(true, keeperSource.Contains("Root/GameRoom_Hub6_Logic/Objects/Phones", StringComparison.Ordinal), "keeper observes the exact live Hub6 phone-bank marker");
Equal(true, keeperSource.Contains("TryCaptureGameplayReadyObservation", StringComparison.Ordinal), "keeper only probes the marker while the active epoch is awaiting gameplay readiness");
Equal(true, keeperSource.Contains("ObserveGameplayReady", StringComparison.Ordinal), "keeper returns a correlated marker observation to the cassette gate");
Equal(true, keeperSource.Contains("ObserveGameplayReadyMarker", StringComparison.Ordinal), "always-on keeper records marker lifetime transitions even before epoch activation");
Equal(true, keeperSource.Contains("GetInstanceID()", StringComparison.Ordinal), "marker incarnation uses public Unity scene-object identity");
Equal(true, keeperSource.IndexOf("GameObject.Find(Hub6PhoneBankRootPath)", StringComparison.Ordinal) < keeperSource.IndexOf("ObserveGameplayReadyMarker", StringComparison.Ordinal), "keeper reads exact marker presence before recording its lifetime transition");
Equal(true, keeperSource.IndexOf("ObserveGameplayReadyMarker", StringComparison.Ordinal) < keeperSource.IndexOf("TryCaptureGameplayReadyObservation", StringComparison.Ordinal), "fresh marker lifetime evidence is recorded before an epoch may capture it");
Equal(false, keeperSource.Contains("TimeSpan.FromSeconds(1)", StringComparison.Ordinal), "keeper does not substitute a frame-count interval for elapsed time");
Equal(false, pluginSource.Contains("ChangeFrontendModeRequest", StringComparison.Ordinal), "cassette readiness does not guess an unproven title/frontend hook");
Equal(true, cassetteSaveDesignSource.Contains("There is no proven public title/save-unload callback with safe ordering", StringComparison.Ordinal), "design records the residual title interval explicitly");
Equal(true, cassetteSaveDesignSource.Contains("the next exact boundary closes it before the plugin extracts or processes any new-load identity", StringComparison.Ordinal), "design records the bounded next-boundary safety guarantee");
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

var persistenceDiagnosticWave = new CassetteSaveEpochRuntime();
persistenceDiagnosticWave.Receive("BADASS");
persistenceDiagnosticWave.Receive("HEAVY_METAL");
persistenceDiagnosticWave.ActivateSave(4);
persistenceDiagnosticWave.RecordSubmission("BADASS");
persistenceDiagnosticWave.RecordSubmission("HEAVY_METAL");
Equal(false, persistenceDiagnosticWave.TryConsumeNewlyVerifiedGrantDiagnosticWave(
    generation: 9, statePointer: 0x700, out _),
    "a submitted semantic grant cannot emit diagnostics before delayed authoritative verification");
SequenceEqual(new[] { "BADASS", "HEAVY_METAL" }, persistenceDiagnosticWave.Tick(TimeSpan.FromMilliseconds(250)),
    "both delayed authoritative verifications become due before wave admission");
Equal(true, persistenceDiagnosticWave.RecordVerification("BADASS", CassetteRandomizationPolicy.HaveInBag), "a newly verified mod grant enters the diagnostic wave");
Equal(true, persistenceDiagnosticWave.RecordVerification("HEAVY_METAL", CassetteRandomizationPolicy.HaveInBag), "same-update newly verified grants coalesce into the diagnostic wave");
Equal(true, persistenceDiagnosticWave.TryConsumeNewlyVerifiedGrantDiagnosticWave(
    generation: 9, statePointer: 0x700, out CassettePersistenceTargetDiagnosticWave verifiedWave),
    "one post-loop consume captures the verified wave and its save identity");
SequenceEqual(new[] { "BADASS", "HEAVY_METAL" }, verifiedWave.Songs, "one diagnostic wave contains every newly verified grant in stable order");
Equal(new CassettePersistenceTargetDiagnosticIdentity(9, 1, 4, 0x700), verifiedWave.Identity,
    "the verified wave captures generation, epoch, slot, and pointer atomically");
Equal(true, verifiedWave.IsCurrent(generation: 9, epoch: 1, slot: 4, statePointer: 0x700),
    "the verified wave accepts only its exact save identity");
Equal(false, persistenceDiagnosticWave.TryConsumeNewlyVerifiedGrantDiagnosticWave(
    generation: 9, statePointer: 0x700, out _),
    "a verified wave is consumed at most once");
SequenceEqual(new[] { "BADASS", "HEAVY_METAL" }, persistenceDiagnosticWave.OwnedSongs, "the snapshot retains all AP-owned cassette evidence, not only the last verified song");
persistenceDiagnosticWave.ActivateSave(4);
Equal(false, verifiedWave.IsCurrent(generation: 9, epoch: persistenceDiagnosticWave.Epoch, slot: 4, statePointer: 0x700),
    "a boundary racing after consumption rejects the stale captured wave");

var queuedBoundaryDiagnosticWave = new CassetteSaveEpochRuntime();
queuedBoundaryDiagnosticWave.Receive("BADASS");
queuedBoundaryDiagnosticWave.ActivateSave(4);
queuedBoundaryDiagnosticWave.RecordSubmission("BADASS");
queuedBoundaryDiagnosticWave.Tick(TimeSpan.FromMilliseconds(250));
queuedBoundaryDiagnosticWave.RecordVerification("BADASS", CassetteRandomizationPolicy.HaveInBag);
queuedBoundaryDiagnosticWave.ActivateSave(4);
Equal(false, queuedBoundaryDiagnosticWave.TryConsumeNewlyVerifiedGrantDiagnosticWave(
    generation: 9, statePointer: 0x700, out _),
    "a new save epoch clears a genuinely queued diagnostic wave before it can emit");

var nonDelayedDiagnosticWave = new CassetteSaveEpochRuntime();
nonDelayedDiagnosticWave.Receive("BADASS");
nonDelayedDiagnosticWave.ActivateSave(4);
nonDelayedDiagnosticWave.RecordSubmission("BADASS");
nonDelayedDiagnosticWave.RecordVerification("BADASS", CassetteRandomizationPolicy.HaveInBag);
Equal(false, nonDelayedDiagnosticWave.TryConsumeNewlyVerifiedGrantDiagnosticWave(
    generation: 10, statePointer: 0x701, out _),
    "RecordVerification cannot admit a diagnostic wave until its bounded delay is actually due");

var relaunchPersisted = new CassetteSaveEpochRuntime();
relaunchPersisted.Receive("BADASS");
relaunchPersisted.ActivateSave(4);
relaunchPersisted.Observe("BADASS", CassetteRandomizationPolicy.HaveInBag);
Equal(false, relaunchPersisted.TryConsumeNewlyVerifiedGrantDiagnosticWave(
    generation: 11, statePointer: 0x702, out _),
    "a relaunch that reads an already-persisted cassette produces no mod-grant acceptance wave");

var relaunchMissing = new CassetteSaveEpochRuntime();
relaunchMissing.Receive("BADASS");
relaunchMissing.ActivateSave(4);
Equal(true, relaunchMissing.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, processorAvailable: true),
    "a relaunch where the cassette is missing admits one fresh semantic grant in the new epoch");
relaunchMissing.RecordSubmission("BADASS");
relaunchMissing.Tick(TimeSpan.FromMilliseconds(250));
relaunchMissing.RecordVerification("BADASS", CassetteRandomizationPolicy.HaveInBag);
Equal(true, relaunchMissing.TryConsumeNewlyVerifiedGrantDiagnosticWave(
    generation: 12, statePointer: 0x703, out _),
    "the newly verified relaunch repair produces one fresh acceptance wave");
Equal(false, relaunchMissing.TryConsumeNewlyVerifiedGrantDiagnosticWave(
    generation: 12, statePointer: 0x703, out _),
    "the fresh relaunch repair wave remains one-shot");
Console.WriteLine("PASS: newly_verified_mod_grants_queue_one_identity_scoped_persistence_target_snapshot");

Equal("identity-changed-no-candidate", CassettePersistenceTargetDiagnosticDecision.Classify(
    identityCurrent: false, expectedEntryMatches: true, selectedEntryMatches: true),
    "a snapshot that raced a save-identity change cannot advertise a stale write-route candidate");
Equal("guarded-request-candidate", CassettePersistenceTargetDiagnosticDecision.Classify(
    identityCurrent: true, expectedEntryMatches: true, selectedEntryMatches: true),
    "a current selected-entry pointer match identifies the guarded request candidate");
Equal("pointer-bound-public-state-candidate", CassettePersistenceTargetDiagnosticDecision.Classify(
    identityCurrent: true, expectedEntryMatches: true, selectedEntryMatches: false),
    "a current expected-slot-only match identifies the pointer-bound candidate");
Equal("no-write-candidate", CassettePersistenceTargetDiagnosticDecision.Classify(
    identityCurrent: true, expectedEntryMatches: false, selectedEntryMatches: false),
    "a current snapshot without an entry match remains closed");
Console.WriteLine("PASS: persistence_target_decision_fails_closed_on_identity_race");

var persistenceWaveQueue = new CassettePersistenceWaveQueue();
var persistenceQueueIdentity = new CassettePersistenceAcceptanceIdentity(2, 1, 4, 0x4400);
persistenceWaveQueue.BeginEpoch(persistenceQueueIdentity);
Equal(true, persistenceWaveQueue.TryEnqueue(
    persistenceQueueIdentity, new[] { "ON_THE_WAY", "BADASS", "BADASS" }),
    "a current non-empty verified wave is accepted");
Equal(true, persistenceWaveQueue.TryPeek(
    persistenceQueueIdentity, out CassettePersistenceQueuedWave firstQueuedWave),
    "the accepted wave remains available until verified completion");
SequenceEqual(new[] { "BADASS", "ON_THE_WAY" }, firstQueuedWave.Songs,
    "queued songs are ordinal-deduplicated and sorted");
Equal(false, persistenceWaveQueue.TryEnqueue(
    persistenceQueueIdentity with { Epoch = 2 }, new[] { "HEAVY_METAL" }),
    "a stale save identity cannot enqueue work into the active epoch");
Equal(false, persistenceWaveQueue.TryEnqueue(persistenceQueueIdentity, Array.Empty<string>()),
    "an empty verified wave is rejected");
Equal(true, persistenceWaveQueue.TryEnqueue(
    persistenceQueueIdentity, new[] { "HEAVY_METAL" }),
    "a later wave remains queued while the first wave is still active");
Equal(true, persistenceWaveQueue.TryPeek(
    persistenceQueueIdentity, out CassettePersistenceQueuedWave stillFirstQueuedWave),
    "peeking an active wave never removes it");
Equal(firstQueuedWave.WaveId, stillFirstQueuedWave.WaveId,
    "a later arrival cannot replace the active queued wave");
persistenceWaveQueue.Complete(firstQueuedWave);
Equal(true, persistenceWaveQueue.TryPeek(
    persistenceQueueIdentity, out CassettePersistenceQueuedWave secondQueuedWave),
    "verified completion exposes the next queued batch");
SequenceEqual(new[] { "HEAVY_METAL" }, secondQueuedWave.Songs,
    "the later verified batch is preserved independently");

var replacementQueueIdentity = persistenceQueueIdentity with { Generation = 3, Epoch = 2, Pointer = 0x5500 };
persistenceWaveQueue.BeginEpoch(replacementQueueIdentity);
Equal(false, persistenceWaveQueue.TryPeek(persistenceQueueIdentity, out _),
    "a save-boundary identity replacement clears stale queued work");
Equal(false, persistenceWaveQueue.TryPeek(replacementQueueIdentity, out _),
    "the replacement epoch starts without inherited waves");
Equal(true, persistenceWaveQueue.TryEnqueue(replacementQueueIdentity, new[] { "KEEP_ON_HUSTLIN" }),
    "the replacement identity accepts its own verified batch");
persistenceWaveQueue.TombstoneEpoch(replacementQueueIdentity);
Equal(false, persistenceWaveQueue.TryPeek(replacementQueueIdentity, out _),
    "tombstoning clears every queued wave in the exact epoch");
Equal(false, persistenceWaveQueue.TryEnqueue(replacementQueueIdentity, new[] { "ON_THE_WAY" }),
    "a tombstoned epoch rejects all later verified waves");
persistenceWaveQueue.BeginEpoch(replacementQueueIdentity);
Equal(false, persistenceWaveQueue.TryEnqueue(replacementQueueIdentity, new[] { "ON_THE_WAY" }),
    "repeating the same identity cannot clear its tombstone");
var afterTombstoneIdentity = replacementQueueIdentity with { Generation = 4, Epoch = 3, Pointer = 0x6600 };
persistenceWaveQueue.BeginEpoch(afterTombstoneIdentity);
Equal(true, persistenceWaveQueue.TryEnqueue(afterTombstoneIdentity, new[] { "ON_THE_WAY" }),
    "only a genuinely new epoch clears the prior tombstone");
persistenceWaveQueue.CancelEpoch(afterTombstoneIdentity);
Equal(false, persistenceWaveQueue.TryPeek(afterTombstoneIdentity, out _),
    "boundary cancellation clears queued work");
Equal(false, persistenceWaveQueue.TryEnqueue(afterTombstoneIdentity, new[] { "BADASS" }),
    "a cancelled identity remains inactive until a boundary begins another epoch");
Console.WriteLine("PASS: persistence_wave_queue_is_identity_bound_sorted_and_tombstoned");

var sequentialIdentity = new CassettePersistenceAcceptanceIdentity(60, 12, 4, 0x8800);
var firstSequentialBaseline = new CassettePersistenceAcceptanceBaseline(
    new CassettePublicWriteState(true, false, 20, 7, "IO_ERROR"),
    RedundancyBundleIndex: 0,
    RedundancyBundleRevision: 16,
    new[] { "BADASS" });
var firstSequentialVerifiedState = new CassettePublicWriteDiagnosticState(
    new CassettePublicWriteState(false, false, 21, 7, "IO_ERROR"),
    CurrentGameTime: 21,
    HasUnstagedChanges: false,
    RedundancyBundleIndex: 1,
    RedundancyBundleRevision: 17,
    StatePointer: 0x8800,
    Statuses: new Dictionary<string, string> { ["BADASS"] = CassetteRandomizationPolicy.HaveInBag });
var sequentialRuntime = new CassettePointerBoundPersistenceRuntime(
    CassettePersistenceAttemptPolicy.SequentialVerifiedBatches);
sequentialRuntime.BeginEpoch(sequentialIdentity);
Equal(true, sequentialRuntime.TryPrepare(
    sequentialIdentity, firstSequentialBaseline, out CassettePersistenceAcceptanceAttempt firstSequentialAttempt),
    "sequential mode prepares the first exact batch");
Equal(false, sequentialRuntime.TryPrepare(sequentialIdentity, firstSequentialBaseline, out _),
    "an active sequential attempt prevents overlapping preparation");
Equal(true, sequentialRuntime.MarkInvoked(firstSequentialAttempt),
    "the first sequential batch enters polling");
Equal(CassettePersistenceAcceptanceOutcome.Verified,
    sequentialRuntime.Observe(
        firstSequentialAttempt, sequentialIdentity, firstSequentialVerifiedState,
        statusesRetained: true, stateReadable: true, TimeSpan.FromMilliseconds(16), out _),
    "the unchanged success predicate verifies the first sequential batch");
var secondSequentialBaseline = firstSequentialBaseline with
{
    WriteState = firstSequentialVerifiedState.WriteState with { HasChanges = true },
    RedundancyBundleIndex = 1,
    RedundancyBundleRevision = 17,
    Songs = new[] { "HEAVY_METAL" },
};
Equal(true, sequentialRuntime.TryPrepare(
    sequentialIdentity, secondSequentialBaseline, out CassettePersistenceAcceptanceAttempt secondSequentialAttempt),
    "verified sequential mode admits a later batch in the same exact epoch");
Equal(true, secondSequentialAttempt.Id > firstSequentialAttempt.Id,
    "sequential attempt identifiers increase monotonically");

var indeterminateSequentialRuntime = new CassettePointerBoundPersistenceRuntime(
    CassettePersistenceAttemptPolicy.SequentialVerifiedBatches);
indeterminateSequentialRuntime.BeginEpoch(sequentialIdentity);
indeterminateSequentialRuntime.TryPrepare(
    sequentialIdentity, firstSequentialBaseline, out CassettePersistenceAcceptanceAttempt indeterminateSequentialAttempt);
Equal(CassettePersistenceAcceptanceOutcome.Failed,
    indeterminateSequentialRuntime.MarkIndeterminate(indeterminateSequentialAttempt, "urgency-invocation"),
    "a possibly mutating invocation ambiguity fails the active batch");
Equal(false, indeterminateSequentialRuntime.TryPrepare(sequentialIdentity, firstSequentialBaseline, out _),
    "an indeterminate batch tombstones sequential persistence for the epoch");

var failedSequentialRuntime = new CassettePointerBoundPersistenceRuntime(
    CassettePersistenceAttemptPolicy.SequentialVerifiedBatches);
failedSequentialRuntime.BeginEpoch(sequentialIdentity);
failedSequentialRuntime.TryPrepare(
    sequentialIdentity, firstSequentialBaseline, out CassettePersistenceAcceptanceAttempt failedSequentialAttempt);
failedSequentialRuntime.MarkInvoked(failedSequentialAttempt);
Equal(CassettePersistenceAcceptanceOutcome.Failed,
    failedSequentialRuntime.Observe(
        failedSequentialAttempt, sequentialIdentity, firstSequentialVerifiedState,
        statusesRetained: false, stateReadable: true, TimeSpan.Zero, out _),
    "a polling status regression fails closed in sequential mode");
Equal(false, failedSequentialRuntime.TryPrepare(sequentialIdentity, firstSequentialBaseline, out _),
    "a polling failure tombstones sequential persistence for the epoch");

var timedOutSequentialRuntime = new CassettePointerBoundPersistenceRuntime(
    CassettePersistenceAttemptPolicy.SequentialVerifiedBatches);
timedOutSequentialRuntime.BeginEpoch(sequentialIdentity);
timedOutSequentialRuntime.TryPrepare(
    sequentialIdentity, firstSequentialBaseline, out CassettePersistenceAcceptanceAttempt timedOutSequentialAttempt);
timedOutSequentialRuntime.MarkInvoked(timedOutSequentialAttempt);
var unchangedSequentialState = firstSequentialVerifiedState with
{
    WriteState = firstSequentialBaseline.WriteState,
    CurrentGameTime = 20,
    HasUnstagedChanges = true,
    RedundancyBundleIndex = 0,
    RedundancyBundleRevision = 16,
};
for (int second = 0; second < 129; second++)
    Equal(CassettePersistenceAcceptanceOutcome.Pending,
        timedOutSequentialRuntime.Observe(
            timedOutSequentialAttempt, sequentialIdentity, unchangedSequentialState,
            statusesRetained: true, stateReadable: true, TimeSpan.FromSeconds(1), out _),
        $"sequential batch remains pending through active-update second {second + 1}");
Equal(CassettePersistenceAcceptanceOutcome.Timeout,
    timedOutSequentialRuntime.Observe(
        timedOutSequentialAttempt, sequentialIdentity, unchangedSequentialState,
        statusesRetained: true, stateReadable: true, TimeSpan.FromSeconds(1), out _),
    "sequential mode preserves the 130-second hard timeout");
Equal(false, timedOutSequentialRuntime.TryPrepare(sequentialIdentity, firstSequentialBaseline, out _),
    "a timed-out batch tombstones sequential persistence for the epoch");

var oneShotCompatibilityRuntime = new CassettePointerBoundPersistenceRuntime(
    CassettePersistenceAttemptPolicy.OneShotPerEpoch);
oneShotCompatibilityRuntime.BeginEpoch(sequentialIdentity);
oneShotCompatibilityRuntime.TryPrepare(
    sequentialIdentity, firstSequentialBaseline, out CassettePersistenceAcceptanceAttempt oneShotCompatibilityAttempt);
oneShotCompatibilityRuntime.MarkInvoked(oneShotCompatibilityAttempt);
Equal(CassettePersistenceAcceptanceOutcome.Verified,
    oneShotCompatibilityRuntime.Observe(
        oneShotCompatibilityAttempt, sequentialIdentity, firstSequentialVerifiedState,
        statusesRetained: true, stateReadable: true, TimeSpan.Zero, out _),
    "one-shot mode preserves the acceptance success predicate");
Equal(false, oneShotCompatibilityRuntime.TryPrepare(sequentialIdentity, firstSequentialBaseline, out _),
    "one-shot compatibility still rejects a second verified batch in the epoch");
oneShotCompatibilityRuntime.BeginEpoch(sequentialIdentity with { Generation = 61, Epoch = 13, Pointer = 0x9900 });
Equal(true, oneShotCompatibilityRuntime.TryPrepare(
    sequentialIdentity with { Generation = 61, Epoch = 13, Pointer = 0x9900 },
    firstSequentialBaseline, out CassettePersistenceAcceptanceAttempt nextEpochCompatibilityAttempt),
    "a new one-shot epoch may prepare again");
Equal(true, nextEpochCompatibilityAttempt.Id > oneShotCompatibilityAttempt.Id,
    "attempt identifiers remain monotonic across epoch boundaries");
Console.WriteLine("PASS: pointer_bound_persistence_runtime_supports_sequential_verified_batches");

int productionReconciliationRequests = 0;
int productionAdapterAdmissions = 0;
var productionCoordinator = new CassetteProductionPersistenceCoordinator(
    () => productionReconciliationRequests++);
productionCoordinator.BeginEpoch(sequentialIdentity);
Equal(true, productionCoordinator.TryEnqueue(
    sequentialIdentity, new[] { "ON_THE_WAY", "BADASS", "BADASS" }),
    "production queues the exact verified batch before eligibility is evaluated");
Equal(true, productionCoordinator.TryPeek(
    sequentialIdentity, out CassettePersistenceQueuedWave deferredProductionWave),
    "the queued production wave is available to the gate evaluator");
Equal(CassetteProductionPersistenceStartOutcome.Deferred,
    productionCoordinator.TryInvoke(
        sequentialIdentity,
        deferredProductionWave.WaveId,
        eligible: false,
        firstSequentialBaseline,
        preDetail: "transient-target-unreadable",
        onPrepared: () => { },
        invokeAdapter: () =>
        {
            productionAdapterAdmissions++;
            return new(true, "success", "Invoked");
        }),
    "transient pre-invocation unreadability defers without consuming the wave");
Equal(0, productionAdapterAdmissions,
    "a transient production gate failure invokes the adapter zero times");
Equal(true, productionCoordinator.TryPeek(
    sequentialIdentity, out CassettePersistenceQueuedWave recoveredProductionWave),
    "the exact wave remains queued after transient deferral");
Equal(deferredProductionWave.WaveId, recoveredProductionWave.WaveId,
    "gate recovery observes the same immutable queued wave");
Equal(CassetteProductionPersistenceStartOutcome.Invoked,
    productionCoordinator.TryInvoke(
        sequentialIdentity,
        recoveredProductionWave.WaveId,
        eligible: true,
        firstSequentialBaseline,
        preDetail: "ready",
        onPrepared: () => { },
        invokeAdapter: () =>
        {
            productionAdapterAdmissions++;
            return new(true, "success", "Invoked");
        }),
    "gate recovery invokes the exact queued batch once");
Equal(1, productionAdapterAdmissions,
    "gate recovery admits the adapter exactly once");
Equal(CassetteProductionPersistenceStartOutcome.Stale,
    productionCoordinator.TryInvoke(
        sequentialIdentity,
        recoveredProductionWave.WaveId,
        eligible: true,
        firstSequentialBaseline,
        preDetail: "repeat-frame",
        onPrepared: () => { },
        invokeAdapter: () =>
        {
            productionAdapterAdmissions++;
            return new(true, "success", "Invoked");
        }),
    "a repeated frame cannot admit the already-active wave again");
Equal(1, productionAdapterAdmissions,
    "repeated frame evaluation invokes the adapter zero additional times");
Equal(false, productionCoordinator.CanSubmitNativeCassetteGrant(semanticEligible: true),
    "an active production write blocks only additional semantic grant submission");
Equal(CassettePersistenceAcceptanceOutcome.Verified,
    productionCoordinator.Observe(
        productionCoordinator.Attempt,
        sequentialIdentity,
        firstSequentialVerifiedState,
        statusesRetained: true,
        stateReadable: true,
        TimeSpan.FromMilliseconds(16),
        phase: "UPDATE",
        targetStage: "target-selected-entry-missing",
        writeStage: "success",
        stateDetail: "verified"),
    "public polling verification completes the exact active production wave");
Equal(1, productionReconciliationRequests,
    "VERIFIED requests Unity reconciliation exactly once");
Equal(false, productionCoordinator.TryPeek(sequentialIdentity, out _),
    "VERIFIED removes only the completed production wave");
Equal(true, productionCoordinator.CanSubmitNativeCassetteGrant(semanticEligible: true),
    "VERIFIED releases semantic grant admission for a later batch");
Equal(true, productionCoordinator.TryEnqueue(sequentialIdentity, new[] { "HEAVY_METAL" }),
    "a later verified cassette forms a second batch in the unchanged epoch");
productionCoordinator.TryPeek(sequentialIdentity, out CassettePersistenceQueuedWave secondProductionWave);
Equal(CassetteProductionPersistenceStartOutcome.Invoked,
    productionCoordinator.TryInvoke(
        sequentialIdentity,
        secondProductionWave.WaveId,
        eligible: true,
        secondSequentialBaseline,
        preDetail: "second-ready",
        onPrepared: () => { },
        invokeAdapter: () =>
        {
            productionAdapterAdmissions++;
            return new(true, "success", "Invoked");
        }),
    "a second verified batch may invoke once in the same exact epoch");
Equal(2, productionAdapterAdmissions,
    "two sequential verified batches produce exactly two adapter admissions");

foreach (string terminalCase in new[] { "indeterminate", "failed", "timeout" })
{
    int terminalAdapterAdmissions = 0;
    var terminalCoordinator = new CassetteProductionPersistenceCoordinator(() => { });
    terminalCoordinator.BeginEpoch(sequentialIdentity);
    terminalCoordinator.TryEnqueue(sequentialIdentity, new[] { "BADASS" });
    terminalCoordinator.TryPeek(sequentialIdentity, out CassettePersistenceQueuedWave terminalWave);
    CassetteProductionPersistenceStartOutcome startOutcome = terminalCoordinator.TryInvoke(
        sequentialIdentity,
        terminalWave.WaveId,
        eligible: true,
        firstSequentialBaseline,
        preDetail: terminalCase,
        onPrepared: () => { },
        invokeAdapter: () =>
        {
            terminalAdapterAdmissions++;
            return terminalCase == "indeterminate"
                ? new(false, "urgency-invocation", "UrgencyIndeterminate")
                : new(true, "success", "Invoked");
        });
    if (terminalCase == "indeterminate")
    {
        Equal(CassetteProductionPersistenceStartOutcome.Indeterminate, startOutcome,
            "possibly mutating adapter ambiguity terminally tombstones production");
    }
    else
    {
        Equal(CassetteProductionPersistenceStartOutcome.Invoked, startOutcome,
            $"{terminalCase} fixture reaches public polling");
        if (terminalCase == "failed")
        {
            Equal(CassettePersistenceAcceptanceOutcome.Failed,
                terminalCoordinator.Observe(
                    terminalCoordinator.Attempt,
                    sequentialIdentity,
                    firstSequentialVerifiedState,
                    statusesRetained: false,
                    stateReadable: true,
                    TimeSpan.Zero,
                    phase: "UPDATE",
                    targetStage: "success",
                    writeStage: "success",
                    stateDetail: "status-regression"),
                "polling failure tombstones production");
        }
        else
        {
            for (int second = 0; second < 129; second++)
                Equal(CassettePersistenceAcceptanceOutcome.Pending,
                    terminalCoordinator.Observe(
                        terminalCoordinator.Attempt,
                        sequentialIdentity,
                        unchangedSequentialState,
                        statusesRetained: true,
                        stateReadable: true,
                        TimeSpan.FromSeconds(1),
                        phase: "UPDATE",
                        targetStage: "success",
                        writeStage: "success",
                        stateDetail: "pending"),
                    $"production timeout fixture remains pending through second {second + 1}");
            Equal(CassettePersistenceAcceptanceOutcome.Timeout,
                terminalCoordinator.Observe(
                    terminalCoordinator.Attempt,
                    sequentialIdentity,
                    unchangedSequentialState,
                    statusesRetained: true,
                    stateReadable: true,
                    TimeSpan.FromSeconds(1),
                    phase: "UPDATE",
                    targetStage: "success",
                    writeStage: "success",
                    stateDetail: "timeout"),
                "130-second public polling timeout tombstones production");
        }
    }
    Equal(false, terminalCoordinator.TryEnqueue(sequentialIdentity, new[] { "ON_THE_WAY" }),
        $"{terminalCase} tombstone rejects every later same-epoch wave");
    Equal(false, terminalCoordinator.TryInvoke(
            sequentialIdentity,
            terminalWave.WaveId,
            eligible: true,
            firstSequentialBaseline,
            preDetail: "later",
            onPrepared: () => { },
            invokeAdapter: () =>
            {
                terminalAdapterAdmissions++;
                return new(true, "success", "Invoked");
            }) == CassetteProductionPersistenceStartOutcome.Invoked,
        $"{terminalCase} tombstone rejects later adapter admission");
    Equal(1, terminalAdapterAdmissions,
        $"{terminalCase} allows no adapter call after the terminal tombstone");
}

var boundaryCoordinator = new CassetteProductionPersistenceCoordinator(() => { });
var boundaryIdentity = new CassettePersistenceAcceptanceIdentity(70, 20, 4, 0xA400);
boundaryCoordinator.BeginEpoch(boundaryIdentity);
boundaryCoordinator.TryEnqueue(boundaryIdentity, new[] { "BADASS" });
boundaryCoordinator.TryPeek(boundaryIdentity, out CassettePersistenceQueuedWave boundaryWave);
boundaryCoordinator.TryInvoke(
    boundaryIdentity,
    boundaryWave.WaveId,
    eligible: true,
    firstSequentialBaseline,
    preDetail: "old-ready",
    onPrepared: () => { },
    invokeAdapter: () => new(true, "success", "Invoked"));
Equal(CassettePersistenceAcceptanceOutcome.Cancelled,
    boundaryCoordinator.CancelEpoch(boundaryIdentity, "save-boundary"),
    "a save boundary cancels the old active production attempt");
Equal(false, boundaryCoordinator.TryPeek(boundaryIdentity, out _),
    "boundary cancellation clears the old identity-bound queue");
var freshBoundaryIdentity = boundaryIdentity with { Generation = 71, Epoch = 21, Pointer = 0xA500 };
boundaryCoordinator.BeginEpoch(freshBoundaryIdentity);
boundaryCoordinator.TryEnqueue(freshBoundaryIdentity, new[] { "ON_THE_WAY" });
boundaryCoordinator.TryPeek(freshBoundaryIdentity, out CassettePersistenceQueuedWave freshBoundaryWave);
boundaryCoordinator.TryInvoke(
    freshBoundaryIdentity,
    freshBoundaryWave.WaveId,
    eligible: true,
    firstSequentialBaseline with { Songs = new[] { "ON_THE_WAY" } },
    preDetail: "fresh-ready",
    onPrepared: () => { },
    invokeAdapter: () => new(true, "success", "Invoked"));
var boundaryMarkers = new List<CassettePersistenceAcceptanceMarkerRecord>();
while (boundaryCoordinator.TryDequeueMarker(out CassettePersistenceAcceptanceMarkerRecord boundaryMarker))
    boundaryMarkers.Add(boundaryMarker);
SequenceEqual(new[] { "PRE", "INVOKED", "CANCELLED", "PRE", "INVOKED" },
    boundaryMarkers.Select(marker => marker.Marker),
    "old attempt terminal markers remain globally ordered before the fresh epoch PRE");
SequenceEqual(new[] { "1", "1", "1", "2", "2" },
    boundaryMarkers.Select(marker => marker.Attempt.Id.ToString()),
    "boundary marker ordering never relabels the old attempt as the fresh epoch");
Console.WriteLine("PASS: production_persistence_coordinator_executes_queue_gate_terminal_and_boundary_behavior");

var staleCancelIdentityA = new CassettePersistenceAcceptanceIdentity(80, 30, 4, 0xB400);
var staleCancelIdentityB = new CassettePersistenceAcceptanceIdentity(81, 31, 4, 0xB500);
var repeatedBeginCoordinator = new CassetteProductionPersistenceCoordinator(() => { });
repeatedBeginCoordinator.BeginEpoch(staleCancelIdentityB);
repeatedBeginCoordinator.TryEnqueue(staleCancelIdentityB, new[] { "BADASS" });
repeatedBeginCoordinator.TryPeek(
    staleCancelIdentityB, out CassettePersistenceQueuedWave repeatedBeginWave);
repeatedBeginCoordinator.TryInvoke(
    staleCancelIdentityB,
    repeatedBeginWave.WaveId,
    eligible: true,
    firstSequentialBaseline with { Songs = new[] { "BADASS" } },
    preDetail: "b-ready",
    onPrepared: () => { },
    invokeAdapter: () => new(true, "success", "Invoked"));
repeatedBeginCoordinator.BeginEpoch(staleCancelIdentityB);
Equal(CassettePersistenceAcceptanceOutcome.Verified,
    repeatedBeginCoordinator.Observe(
        repeatedBeginCoordinator.Attempt,
        staleCancelIdentityB,
        firstSequentialVerifiedState with { StatePointer = staleCancelIdentityB.Pointer },
        statusesRetained: true,
        stateReadable: true,
        TimeSpan.Zero,
        phase: "UPDATE",
        targetStage: "target-selected-entry-missing",
        writeStage: "success",
        stateDetail: "b-verified"),
    "re-observing an exact current epoch leaves its active attempt verifiable");
Equal(false, repeatedBeginCoordinator.TryPeek(staleCancelIdentityB, out _),
    "re-observing an exact current epoch preserves active-wave completion ownership");

int staleCancelAdapterAdmissions = 0;
int staleCancelReconciliations = 0;
var staleCancelCoordinator = new CassetteProductionPersistenceCoordinator(
    () => staleCancelReconciliations++);
staleCancelCoordinator.BeginEpoch(staleCancelIdentityA);
Equal(CassettePersistenceAcceptanceOutcome.None,
    staleCancelCoordinator.CancelEpoch(staleCancelIdentityA, "retire-a"),
    "retiring an inactive epoch A leaves no active attempt");
staleCancelCoordinator.BeginEpoch(staleCancelIdentityB);
Equal(true, staleCancelCoordinator.TryEnqueue(staleCancelIdentityB, new[] { "BADASS" }),
    "epoch B queues its exact verified wave");
staleCancelCoordinator.TryPeek(
    staleCancelIdentityB, out CassettePersistenceQueuedWave staleCancelWaveB);
Equal(CassetteProductionPersistenceStartOutcome.Invoked,
    staleCancelCoordinator.TryInvoke(
        staleCancelIdentityB,
        staleCancelWaveB.WaveId,
        eligible: true,
        firstSequentialBaseline with { Songs = new[] { "BADASS" } },
        preDetail: "b-ready",
        onPrepared: () => { },
        invokeAdapter: () =>
        {
            staleCancelAdapterAdmissions++;
            return new(true, "success", "Invoked");
        }),
    "epoch B starts one exact pointer-bound attempt");
Equal(1, staleCancelAdapterAdmissions,
    "epoch B invokes the adapter exactly once before the stale callback");
staleCancelCoordinator.BeginEpoch(staleCancelIdentityB);
Equal(true, staleCancelCoordinator.Active,
    "re-observing the exact current epoch is a no-op for its active attempt");
Equal(true, staleCancelCoordinator.Invoked,
    "re-observing the exact current epoch preserves its invoked phase");
Equal(CassettePersistenceAcceptanceOutcome.None,
    staleCancelCoordinator.CancelEpoch(staleCancelIdentityA, "late-a"),
    "a stale epoch A cancel cannot cancel current epoch B");
Equal(true, staleCancelCoordinator.Active,
    "epoch B remains active after stale epoch A cancellation");
Equal(true, staleCancelCoordinator.Invoked,
    "epoch B remains invoked after stale epoch A cancellation");
var staleCancelMarkers = new List<CassettePersistenceAcceptanceMarkerRecord>();
while (staleCancelCoordinator.TryDequeueMarker(out CassettePersistenceAcceptanceMarkerRecord staleCancelMarker))
    staleCancelMarkers.Add(staleCancelMarker);
SequenceEqual(new[] { "PRE", "INVOKED" },
    staleCancelMarkers.Select(marker => marker.Marker),
    "stale epoch A cancellation emits no CANCELLED marker for epoch B");
Equal(true, staleCancelMarkers.All(marker => marker.Attempt.Identity == staleCancelIdentityB),
    "every admitted marker remains bound to epoch B");
Equal(CassetteProductionPersistenceStartOutcome.Stale,
    staleCancelCoordinator.TryInvoke(
        staleCancelIdentityB,
        staleCancelWaveB.WaveId,
        eligible: true,
        firstSequentialBaseline with { Songs = new[] { "BADASS" } },
        preDetail: "repeat-frame",
        onPrepared: () => { },
        invokeAdapter: () =>
        {
            staleCancelAdapterAdmissions++;
            return new(true, "success", "Invoked");
        }),
    "the active B wave is retained as active rather than duplicated in the queue");
Equal(1, staleCancelAdapterAdmissions,
    "the frame after stale cancellation cannot invoke B a second time");
Equal(CassettePersistenceAcceptanceOutcome.Verified,
    staleCancelCoordinator.Observe(
        staleCancelCoordinator.Attempt,
        staleCancelIdentityB,
        firstSequentialVerifiedState with { StatePointer = staleCancelIdentityB.Pointer },
        statusesRetained: true,
        stateReadable: true,
        TimeSpan.Zero,
        phase: "UPDATE",
        targetStage: "target-selected-entry-missing",
        writeStage: "success",
        stateDetail: "b-verified"),
    "epoch B can still reach VERIFIED after the stale epoch A cancellation");
Equal(1, staleCancelReconciliations,
    "verified epoch B requests reconciliation once");
Equal(false, staleCancelCoordinator.TryPeek(staleCancelIdentityB, out _),
    "verification completes the exact retained B wave");

var queuedStaleCancelCoordinator = new CassetteProductionPersistenceCoordinator(() => { });
queuedStaleCancelCoordinator.BeginEpoch(staleCancelIdentityA);
queuedStaleCancelCoordinator.CancelEpoch(staleCancelIdentityA, "retire-a");
queuedStaleCancelCoordinator.BeginEpoch(staleCancelIdentityB);
Equal(true, queuedStaleCancelCoordinator.TryEnqueue(
    staleCancelIdentityB, new[] { "ON_THE_WAY" }),
    "epoch B may queue before invocation");
queuedStaleCancelCoordinator.TryPeek(
    staleCancelIdentityB, out CassettePersistenceQueuedWave queuedStaleCancelWaveB);
Equal(CassettePersistenceAcceptanceOutcome.None,
    queuedStaleCancelCoordinator.CancelEpoch(staleCancelIdentityA, "late-a-before-start"),
    "stale epoch A cancellation cannot retire queued current epoch B");
Equal(true, queuedStaleCancelCoordinator.TryPeek(
    staleCancelIdentityB, out CassettePersistenceQueuedWave retainedQueuedWaveB),
    "queued epoch B remains available after stale epoch A cancellation");
Equal(queuedStaleCancelWaveB.WaveId, retainedQueuedWaveB.WaveId,
    "stale cancellation preserves the exact immutable queued B wave");
Console.WriteLine("PASS: production_persistence_coordinator_rejects_stale_epoch_cancellation");

var acceptanceRuntime = new CassettePointerBoundPersistenceAcceptanceRuntime();
var acceptanceIdentity = new CassettePersistenceAcceptanceIdentity(41, 7, 4, 0x700);
var acceptanceBaseline = new CassettePersistenceAcceptanceBaseline(
    new CassettePublicWriteState(true, false, 10, 8, "IO_ERROR"),
    RedundancyBundleIndex: 2,
    RedundancyBundleRevision: 5,
    new[] { "BADASS", "HEAVY_METAL" });
acceptanceRuntime.BeginEpoch(acceptanceIdentity);
Equal(true, acceptanceRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out CassettePersistenceAcceptanceAttempt acceptanceAttempt),
    "opt-in acceptance installs an immutable token and PRE baseline before invocation");
Equal(CassettePersistenceAcceptanceEventOutcome.SuccessWake,
    acceptanceRuntime.ObserveWriteCompletedEvent(acceptanceAttempt, eventSlot: 4, succeeded: true),
    "a synchronous matching success event is retained after token installation");
Equal(CassettePersistenceAcceptanceEventOutcome.Ignored,
    acceptanceRuntime.ObserveWriteCompletedEvent(acceptanceAttempt, eventSlot: 3, succeeded: true),
    "a wrong-slot event is ignored");
Equal(CassettePersistenceAcceptanceEventOutcome.Ignored,
    acceptanceRuntime.ObserveWriteCompletedEvent(acceptanceAttempt with { Epoch = 8 }, eventSlot: 4, succeeded: true),
    "a stale identity event is ignored");
Equal(true, acceptanceRuntime.MarkInvoked(acceptanceAttempt), "completed native calls move the prepared token to polling");
Equal(CassettePersistenceAcceptanceOutcome.Pending,
    acceptanceRuntime.Observe(
        acceptanceAttempt, acceptanceIdentity,
        new CassettePublicWriteDiagnosticState(
            acceptanceBaseline.WriteState, 11, true, 2, 5, 0x700,
            new Dictionary<string, string> { ["BADASS"] = CassetteRandomizationPolicy.HaveInBag, ["HEAVY_METAL"] = CassetteRandomizationPolicy.HaveInBag }),
        statusesRetained: true, stateReadable: true, TimeSpan.FromSeconds(1), out _),
    "event alone cannot prove success while write flags and PRE revision remain unchanged");
Equal(CassettePersistenceAcceptanceOutcome.Verified,
    acceptanceRuntime.Observe(
        acceptanceAttempt, acceptanceIdentity,
        new CassettePublicWriteDiagnosticState(
            new CassettePublicWriteState(false, false, 10, 8, "IO_ERROR"), 12, false, 0, 6, 0x700,
            new Dictionary<string, string> { ["BADASS"] = CassetteRandomizationPolicy.HaveInBag, ["HEAVY_METAL"] = CassetteRandomizationPolicy.HaveInBag }),
        statusesRetained: true, stateReadable: true, TimeSpan.Zero, out _),
    "cleared public flags plus advanced redundancy beyond immutable PRE proves success");
Equal(false, acceptanceRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out _),
    "a completed trial cannot retry in the same epoch");

foreach ((string Case, CassettePublicWriteDiagnosticState State, bool Statuses, bool Readable, CassettePersistenceAcceptanceOutcome Expected) failure in new[]
{
    ("failure-time", new CassettePublicWriteDiagnosticState(new(true, false, 10, 9, "IO_ERROR"), 11, true, 2, 5, 0x700, new Dictionary<string, string>()), true, true, CassettePersistenceAcceptanceOutcome.Failed),
    ("failure-reason", new CassettePublicWriteDiagnosticState(new(true, false, 10, 8, "VALIDATION_FAILED"), 11, true, 2, 5, 0x700, new Dictionary<string, string>()), true, true, CassettePersistenceAcceptanceOutcome.Failed),
    ("status-regression", new CassettePublicWriteDiagnosticState(new(true, false, 10, 8, "IO_ERROR"), 11, true, 2, 5, 0x700, new Dictionary<string, string>()), false, true, CassettePersistenceAcceptanceOutcome.Failed),
    ("unreadable", default, true, false, CassettePersistenceAcceptanceOutcome.Failed),
})
{
    var failedRuntime = new CassettePointerBoundPersistenceAcceptanceRuntime();
    failedRuntime.BeginEpoch(acceptanceIdentity);
    failedRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out CassettePersistenceAcceptanceAttempt failedAttempt);
    failedRuntime.MarkInvoked(failedAttempt);
    Equal(failure.Expected, failedRuntime.Observe(
        failedAttempt, acceptanceIdentity, failure.State, failure.Statuses, failure.Readable,
        TimeSpan.Zero, out _), $"acceptance {failure.Case} fails closed");
    Equal(false, failedRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out _),
        $"acceptance {failure.Case} tombstones the epoch");
}

var identityFailureRuntime = new CassettePointerBoundPersistenceAcceptanceRuntime();
identityFailureRuntime.BeginEpoch(acceptanceIdentity);
identityFailureRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out CassettePersistenceAcceptanceAttempt identityFailureAttempt);
identityFailureRuntime.MarkInvoked(identityFailureAttempt);
Equal(CassettePersistenceAcceptanceOutcome.Failed, identityFailureRuntime.Observe(
    identityFailureAttempt, acceptanceIdentity with { Pointer = 0x701 }, default,
    statusesRetained: true, stateReadable: true, TimeSpan.Zero, out _),
    "pointer or save identity change fails the active trial");

var eventFailureRuntime = new CassettePointerBoundPersistenceAcceptanceRuntime();
eventFailureRuntime.BeginEpoch(acceptanceIdentity);
eventFailureRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out CassettePersistenceAcceptanceAttempt eventFailureAttempt);
Equal(CassettePersistenceAcceptanceEventOutcome.FailureWake,
    eventFailureRuntime.ObserveWriteCompletedEvent(eventFailureAttempt, 4, succeeded: false),
    "an uncorrelated same-slot failed event is a wake hint, never terminal");
Equal(true, eventFailureRuntime.Active, "failed event alone leaves the current attempt active");
Equal(true, eventFailureRuntime.MarkInvoked(eventFailureAttempt), "failed event hint cannot block invocation completion");
Equal(CassettePersistenceAcceptanceOutcome.Pending, eventFailureRuntime.Observe(
    eventFailureAttempt, acceptanceIdentity, new CassettePublicWriteDiagnosticState(
        acceptanceBaseline.WriteState, 11, true, 2, 5, 0x700,
        new Dictionary<string, string> { ["BADASS"] = CassetteRandomizationPolicy.HaveInBag, ["HEAVY_METAL"] = CassetteRandomizationPolicy.HaveInBag }),
    statusesRetained: true, stateReadable: true, TimeSpan.Zero, out bool failedEventObserved),
    "failed event alone cannot complete or tombstone the attempt without advanced public failure evidence");
Equal(true, failedEventObserved, "failed event hint is retained for bounded diagnostics");

var staleSameSlotEventRuntime = new CassettePointerBoundPersistenceAcceptanceRuntime();
staleSameSlotEventRuntime.BeginEpoch(acceptanceIdentity);
staleSameSlotEventRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out CassettePersistenceAcceptanceAttempt cancelledSameSlotAttempt);
staleSameSlotEventRuntime.Cancel("save-boundary");
var freshSameSlotIdentity = acceptanceIdentity with { Generation = 42, Epoch = 8 };
staleSameSlotEventRuntime.BeginEpoch(freshSameSlotIdentity);
staleSameSlotEventRuntime.TryPrepare(freshSameSlotIdentity, acceptanceBaseline, out CassettePersistenceAcceptanceAttempt freshSameSlotAttempt);
Equal(CassettePersistenceAcceptanceEventOutcome.FailureWake,
    staleSameSlotEventRuntime.ObserveWriteCompletedEvent(freshSameSlotAttempt, eventSlot: 4, succeeded: false),
    "an old uncorrelated same-slot failure can only wake the fresh trial whose token the handler currently holds");
Equal(true, staleSameSlotEventRuntime.MarkInvoked(freshSameSlotAttempt),
    "old same-slot failure cannot tombstone a fresh epoch's invocation");
Equal(CassettePersistenceAcceptanceOutcome.Pending, staleSameSlotEventRuntime.Observe(
    freshSameSlotAttempt, freshSameSlotIdentity,
    new CassettePublicWriteDiagnosticState(
        acceptanceBaseline.WriteState, 11, true, 2, 5, 0x700,
        new Dictionary<string, string> { ["BADASS"] = CassetteRandomizationPolicy.HaveInBag, ["HEAVY_METAL"] = CassetteRandomizationPolicy.HaveInBag }),
    statusesRetained: true, stateReadable: true, TimeSpan.Zero, out _),
    "cancel then fresh same-slot prepare remains pending after the prior epoch's failed event");

var indeterminateRuntime = new CassettePointerBoundPersistenceAcceptanceRuntime();
indeterminateRuntime.BeginEpoch(acceptanceIdentity);
indeterminateRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out CassettePersistenceAcceptanceAttempt indeterminateAttempt);
Equal(CassettePersistenceAcceptanceOutcome.Failed,
    indeterminateRuntime.MarkIndeterminate(indeterminateAttempt, "urgency-invocation"),
    "promotion success followed by urgency throw is tombstoned indeterminate");
Equal(false, indeterminateRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out _),
    "indeterminate promotion is never retried in the epoch");

var timeoutRuntime = new CassettePointerBoundPersistenceAcceptanceRuntime();
timeoutRuntime.BeginEpoch(acceptanceIdentity);
timeoutRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out CassettePersistenceAcceptanceAttempt timeoutAttempt);
timeoutRuntime.MarkInvoked(timeoutAttempt);
var unchangedAcceptanceState = new CassettePublicWriteDiagnosticState(
    acceptanceBaseline.WriteState, 11, true, 2, 5, 0x700,
    new Dictionary<string, string> { ["BADASS"] = CassetteRandomizationPolicy.HaveInBag, ["HEAVY_METAL"] = CassetteRandomizationPolicy.HaveInBag });
for (int second = 0; second < 129; second++)
    Equal(CassettePersistenceAcceptanceOutcome.Pending, timeoutRuntime.Observe(
        timeoutAttempt, acceptanceIdentity, unchangedAcceptanceState, true, stateReadable: true,
        TimeSpan.FromSeconds(1), out _), $"acceptance remains pending through active-update second {second + 1}");
Equal(CassettePersistenceAcceptanceOutcome.Timeout, timeoutRuntime.Observe(
    timeoutAttempt, acceptanceIdentity, unchangedAcceptanceState, true, stateReadable: true,
    TimeSpan.FromSeconds(1), out _), "acceptance times out at 130 active-update seconds");
Equal(false, timeoutRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out _), "timeout tombstones the epoch");

var cancelledRuntime = new CassettePointerBoundPersistenceAcceptanceRuntime();
cancelledRuntime.BeginEpoch(acceptanceIdentity);
cancelledRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out CassettePersistenceAcceptanceAttempt cancelledAttempt);
Equal(CassettePersistenceAcceptanceOutcome.Cancelled, cancelledRuntime.Cancel("save-boundary"), "save boundary cancels logical acceptance state");
Equal(CassettePersistenceAcceptanceEventOutcome.Ignored,
    cancelledRuntime.ObserveWriteCompletedEvent(cancelledAttempt, 4, true), "late event after cancellation is ignored");
cancelledRuntime.BeginEpoch(acceptanceIdentity with { Generation = 42, Epoch = 8 });
Equal(true, cancelledRuntime.TryPrepare(
    acceptanceIdentity with { Generation = 42, Epoch = 8 }, acceptanceBaseline, out _),
    "a genuinely new save epoch may run one fresh opt-in trial");
Console.WriteLine("PASS: pointer_bound_acceptance_runtime_is_one_shot_identity_safe_and_event_insufficient");

var orderedMarkerJournal = new CassettePersistenceAcceptanceMarkerJournal();
var orderedMarkerEmitter = new CassettePersistenceAcceptanceMarkerEmitter();
var orderedMarkerSync = new object();
var orderedMarkers = new List<CassettePersistenceAcceptanceMarkerRecord>();
bool TakeOrderedMarker(out CassettePersistenceAcceptanceMarkerRecord marker)
{
    lock (orderedMarkerSync) return orderedMarkerJournal.TryDequeue(out marker);
}
void EmitOrderedMarker(CassettePersistenceAcceptanceMarkerRecord marker) => orderedMarkers.Add(marker);

var invocationThenCancelAttempt = new CassettePersistenceAcceptanceAttempt(101, 50, 10, 4, 0x700);
lock (orderedMarkerSync)
{
    Equal(true, orderedMarkerJournal.TryBeginAttempt(invocationThenCancelAttempt, "pre"),
        "PRE begins one marker stream for the exact attempt");
    Equal(true, orderedMarkerJournal.TryAppend(invocationThenCancelAttempt, "INVOKED", "invoked", terminal: false),
        "MarkInvoked transition admits INVOKED before a later config cancellation");
    Equal(true, orderedMarkerJournal.TryAppend(invocationThenCancelAttempt, "CANCELLED", "config-disabled", terminal: true),
        "config cancellation atomically seals the attempt with its terminal marker");
}
using (var delayedEarlierDrain = new ManualResetEventSlim(false))
{
    Task earlierInvocationCaller = Task.Run(() =>
    {
        delayedEarlierDrain.Wait();
        orderedMarkerEmitter.Drain(TakeOrderedMarker, EmitOrderedMarker);
    });
    orderedMarkerEmitter.Drain(TakeOrderedMarker, EmitOrderedMarker);
    delayedEarlierDrain.Set();
    earlierInvocationCaller.GetAwaiter().GetResult();
}
SequenceEqual(new[] { "PRE", "INVOKED", "CANCELLED" }, orderedMarkers.Select(marker => marker.Marker),
    "a later cancellation caller draining first still physically emits admitted INVOKED before terminal CANCELLED");
SequenceEqual(new[] { "1", "2", "3" }, orderedMarkers.Select(marker => marker.Sequence.ToString()),
    "serialized emission preserves the global journal sequence without duplicates or gaps");
Equal(false, orderedMarkerJournal.TryAppend(invocationThenCancelAttempt, "EVENT", "late", terminal: false),
    "terminal cancellation rejects every later marker for that attempt");
orderedMarkerJournal.RetireAttempt(invocationThenCancelAttempt);

orderedMarkers.Clear();
var eventThenPollAttempt = new CassettePersistenceAcceptanceAttempt(102, 51, 11, 4, 0x700);
lock (orderedMarkerSync)
{
    Equal(true, orderedMarkerJournal.TryBeginAttempt(eventThenPollAttempt, "pre"),
        "retirement permits a fresh attempt after the sealed stream");
    Equal(true, orderedMarkerJournal.TryAppend(eventThenPollAttempt, "EVENT", "failure-wake", terminal: false),
        "event hint is admitted before a polling terminal result");
    Equal(true, orderedMarkerJournal.TryAppend(eventThenPollAttempt, "FAILED", "failure-time-advanced", terminal: true),
        "polling terminal atomically seals after the admitted event hint");
}
using (var delayedEventDrain = new ManualResetEventSlim(false))
{
    Task earlierEventCaller = Task.Run(() =>
    {
        delayedEventDrain.Wait();
        orderedMarkerEmitter.Drain(TakeOrderedMarker, EmitOrderedMarker);
    });
    orderedMarkerEmitter.Drain(TakeOrderedMarker, EmitOrderedMarker);
    delayedEventDrain.Set();
    earlierEventCaller.GetAwaiter().GetResult();
}
SequenceEqual(new[] { "PRE", "EVENT", "FAILED" }, orderedMarkers.Select(marker => marker.Marker),
    "poll terminal caller draining first still physically emits EVENT before final FAILED");
Equal(eventThenPollAttempt, orderedMarkers[^1].Attempt,
    "terminal poll marker retains the exact attempt identity");
orderedMarkerJournal.RetireAttempt(eventThenPollAttempt);

orderedMarkers.Clear();
var oldTerminalAttempt = new CassettePersistenceAcceptanceAttempt(103, 52, 12, 4, 0x700);
var freshAfterResetAttempt = new CassettePersistenceAcceptanceAttempt(104, 53, 13, 4, 0x700);
lock (orderedMarkerSync)
{
    orderedMarkerJournal.TryBeginAttempt(oldTerminalAttempt, "old-pre");
    orderedMarkerJournal.TryAppend(oldTerminalAttempt, "TIMEOUT", "old-terminal", terminal: true);
    orderedMarkerJournal.RetireAttempt(oldTerminalAttempt);
    Equal(true, orderedMarkerJournal.TryBeginAttempt(freshAfterResetAttempt, "fresh-pre"),
        "epoch reset permits one fresh attempt after retiring the old identity");
    orderedMarkerJournal.TryAppend(freshAfterResetAttempt, "INVOKED", "fresh-invoked", terminal: false);
}
orderedMarkerEmitter.Drain(TakeOrderedMarker, EmitOrderedMarker);
SequenceEqual(new[] { "PRE", "TIMEOUT", "PRE", "INVOKED" }, orderedMarkers.Select(marker => marker.Marker),
    "old terminal, reset, and fresh attempt retain one global physical emission order");
SequenceEqual(new[] { "103", "103", "104", "104" }, orderedMarkers.Select(marker => marker.Attempt.Id.ToString()),
    "old terminal cannot be mislabeled or emitted after the fresh epoch markers");
SequenceEqual(new[] { "7", "8", "9", "10" }, orderedMarkers.Select(marker => marker.Sequence.ToString()),
    "global marker sequence continues across retired and fresh attempt identities");
Console.WriteLine("PASS: pointer_bound_acceptance_markers_are_globally_ordered_and_terminal");

var eligibleAcceptance = new CassettePersistenceAcceptanceEligibilityEvidence(
    OptInEnabled: true,
    RoutingCompatible: true,
    GameplayReady: true,
    WaveCurrent: true,
    SelectionValid: true,
    SelectedPointerMatches: true,
    RegisteredRetainedProcessorMatch: true,
    ExpectedSlotEntryMatches: true,
    PointerBoundDecision: true,
    DefaultBundlePresent: true,
    DefaultBundleChangeCount: 2,
    StatusesRetainedInBag: true,
    HasUnstagedChanges: true,
    HasChanges: true,
    RequiresWriteToDisk: false);
Equal(true, CassettePersistenceAcceptanceEligibility.Evaluate(eligibleAcceptance, out string eligibleAcceptanceStage),
    "every approved public gate admits exactly one pointer-bound acceptance trial");
Equal("success", eligibleAcceptanceStage, "complete acceptance evidence reports success");
Equal(true, CassettePersistenceAcceptanceOwnershipProof.IsExactPointerBoundOwner(
    hasRegisteredPointer: true, registeredPointer: 0x900,
    hasRetainedPointer: true, retainedPointer: 0x900,
    hasExpectedEntryPointer: true, expectedEntryPointer: 0x700,
    expectedStatePointer: 0x700),
    "acceptance ownership remains provable from the exact required fields when the unrelated numeric selected-entry branch is unreadable");
Equal(false, CassettePersistenceAcceptanceOwnershipProof.IsExactPointerBoundOwner(
    true, 0x900, true, 0x901, true, 0x700, 0x700),
    "registered and retained processor divergence rejects acceptance ownership");
Equal(false, CassettePersistenceAcceptanceOwnershipProof.IsExactPointerBoundOwner(
    true, 0x900, true, 0x900, true, 0x701, 0x700),
    "expected-slot entry pointer divergence rejects acceptance ownership");
Equal(true, CassettePersistenceAcceptanceOwnershipProof.IsKnownExpectedSlotOnlyDiagnostic(
    aggregateReadable: false, stage: "target-selected-entry-missing",
    hasSelectedSlot: true, hasSelectedEntryPointer: false, hasSelectedSide: false),
    "the one known aggregate-false selected-entry-absent shape preserves mandatory expected-slot ownership evidence");
foreach ((string Stage, bool HasSlot, bool HasPointer, bool HasSide) malformed in new[]
{
    ("target-save-data-selected-slot-empty", false, false, false),
    ("target-selected-entry-pointer-get-invocation:InvalidOperationException:target-pointer", true, false, false),
    ("target-selected-entry-missing", false, false, false),
    ("target-selected-entry-missing", true, true, false),
    ("target-selected-entry-missing", true, false, true),
})
    Equal(false, CassettePersistenceAcceptanceOwnershipProof.IsKnownExpectedSlotOnlyDiagnostic(
        aggregateReadable: false, malformed.Stage, malformed.HasSlot, malformed.HasPointer, malformed.HasSide),
        $"malformed aggregate-false target evidence remains rejected: {malformed}");
foreach ((string Case, CassettePersistenceAcceptanceEligibilityEvidence Evidence, string Stage) gateFailure in new[]
{
    ("default-off", eligibleAcceptance with { OptInEnabled = false }, "acceptance-disabled"),
    ("routing", eligibleAcceptance with { RoutingCompatible = false }, "acceptance-routing-incompatible"),
    ("gameplay", eligibleAcceptance with { GameplayReady = false }, "acceptance-gameplay-not-ready"),
    ("wave", eligibleAcceptance with { WaveCurrent = false }, "acceptance-wave-stale"),
    ("validity", eligibleAcceptance with { SelectionValid = false }, "acceptance-selection-invalid"),
    ("selected-pointer", eligibleAcceptance with { SelectedPointerMatches = false }, "acceptance-selected-pointer-mismatch"),
    ("processor-owner", eligibleAcceptance with { RegisteredRetainedProcessorMatch = false }, "acceptance-processor-ownership-mismatch"),
    ("expected-entry", eligibleAcceptance with { ExpectedSlotEntryMatches = false }, "acceptance-expected-entry-mismatch"),
    ("decision", eligibleAcceptance with { PointerBoundDecision = false }, "acceptance-not-pointer-bound-candidate"),
    ("bundle-missing", eligibleAcceptance with { DefaultBundlePresent = false }, "acceptance-default-bundle-missing"),
    ("bundle-empty", eligibleAcceptance with { DefaultBundleChangeCount = 0 }, "acceptance-default-bundle-empty"),
    ("status", eligibleAcceptance with { StatusesRetainedInBag = false }, "acceptance-status-not-retained"),
    ("unstaged", eligibleAcceptance with { HasUnstagedChanges = false }, "acceptance-no-unstaged-changes"),
    ("changes", eligibleAcceptance with { HasChanges = false }, "acceptance-no-changes"),
    ("write-in-flight", eligibleAcceptance with { RequiresWriteToDisk = true }, "acceptance-write-already-required"),
})
{
    Equal(false, CassettePersistenceAcceptanceEligibility.Evaluate(gateFailure.Evidence, out string gateStage),
        $"{gateFailure.Case}: missing acceptance evidence fails closed");
    Equal(gateFailure.Stage, gateStage, $"{gateFailure.Case}: acceptance gate retains an exact stage");
}
Equal(false, CassettePersistenceAcceptanceDefaults.Enabled, "acceptance runtime default is false independently of config binding text");
Console.WriteLine("PASS: pointer_bound_acceptance_requires_every_approved_gate");

var boundedSchedule = new CassetteSaveEpochRuntime();
boundedSchedule.Receive("BADASS");
boundedSchedule.ActivateSave(3);
boundedSchedule.RecordSubmission("BADASS");
Equal(0, boundedSchedule.Tick(TimeSpan.FromMilliseconds(249)).Count, "attempt one cannot verify before 250ms");
SequenceEqual(new[] { "BADASS" }, boundedSchedule.Tick(TimeSpan.FromMilliseconds(1)), "attempt one verifies at 250ms");
boundedSchedule.RecordVerification("BADASS", CassetteRandomizationPolicy.HaveNotEarned);
Equal(false, boundedSchedule.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true), "one semantic grant attempt exhausts the current epoch budget without duplicate grants");
Equal(0, boundedSchedule.Tick(TimeSpan.FromSeconds(10)).Count, "completed unearned verification does not poll every Unity frame");
boundedSchedule.ActivateSave(3);
Equal(true, boundedSchedule.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true), "new epoch revalidates AP ownership and permits one fresh semantic grant");
Console.WriteLine("PASS: semantic_grant_is_once_per_epoch_with_bounded_verification");

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
Console.WriteLine("PASS: retired_disk_commit_runtime_has_no_production_wiring");


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
Equal(true, missingSelectedDiagnostic.HasExpectedSlotEntryPointer, "missing selected entry still proves the independently resolved active epoch entry");
Equal(0x700L, missingSelectedDiagnostic.ExpectedSlotEntryPointer, "active epoch entry is read before the unrelated selected-entry lookup fails");
Equal(0, RequestSystem.SubmitCount, "partial target diagnostics never submit a save request");

var emptySelectedTargetSaveDataState = new DiskCommitTargetSaveDataStateFixture(
    new IntPtr(0x333), new(false, default), new() { [4] = expectedTargetState });
var emptySelectedRegisteredProcessor = new SaveDataRequestProcessor(new IntPtr(0x444), emptySelectedTargetSaveDataState);
var emptySelectedRegisteredWrapper = new RequestProcessor(new IntPtr(0x444), emptySelectedRegisteredProcessor);
RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(emptySelectedRegisteredWrapper));
Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
    targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
    new[] { nameof(ePlayableSong.QUIERES_BAILAR) },
    out CassetteDiskCommitTargetDiagnosticState emptySelectedDiagnostic,
    out _,
    out string emptySelectedStage),
    "an empty numeric selected slot remains unavailable without hiding the active epoch entry");
Equal("target-save-data-selected-slot-empty", emptySelectedStage, "empty numeric selected slot retains its exact stage after the independent active lookup");
Equal(false, emptySelectedDiagnostic.HasSelectedPlayerSaveSlot, "empty numeric selected slot cannot invent a value");
Equal(true, emptySelectedDiagnostic.HasExpectedSlotEntryPointer, "active epoch entry is independently available when numeric selected slot is empty");
Equal(0x700L, emptySelectedDiagnostic.ExpectedSlotEntryPointer, "active epoch pointer survives empty numeric selected-slot evidence");

foreach ((object saveDataState, string expectedStagePrefix, string caseName) in new[]
{
    ((object)new ThrowingSelectedSlotDiskCommitTargetSaveDataStateFixture(
        new IntPtr(0x333), new() { [4] = expectedTargetState }),
        "target-save-data-selected-slot-get-invocation:InvalidOperationException:selected-slot-get", "throwing-getter"),
    ((object)new MalformedSelectedSlotDiskCommitTargetSaveDataStateFixture(
        new IntPtr(0x333), new() { [4] = expectedTargetState }),
        "target-save-data-selected-slot-contract-missing", "malformed-nullable"),
    ((object)new UnconvertibleSelectedSlotDiskCommitTargetSaveDataStateFixture(
        new IntPtr(0x333), new() { [4] = expectedTargetState }),
        "target-save-data-selected-slot-convert-invocation:FormatException", "conversion-failure"),
})
{
    var selectedFailureRegisteredProcessor = new SaveDataRequestProcessor(new IntPtr(0x444), saveDataState);
    var selectedFailureRegisteredWrapper = new RequestProcessor(new IntPtr(0x444), selectedFailureRegisteredProcessor);
    RequestSystem.SetRegisteredProcessors(BuildTargetRegistry(selectedFailureRegisteredWrapper));
    Equal(false, CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
        targetPlayerProcessor, retainedSaveDataProcessor, expectedSlot: 4, expectedPointer: 0x700,
        new[] { nameof(ePlayableSong.QUIERES_BAILAR) },
        out CassetteDiskCommitTargetDiagnosticState selectedFailureDiagnostic,
        out _, out string selectedFailureStage),
        $"{caseName}: selected-slot failure remains an isolated partial diagnostic failure");
    Equal(true, selectedFailureStage.StartsWith(expectedStagePrefix, StringComparison.Ordinal),
        $"{caseName}: selected-slot failure retains its precise stage");
    Equal(true, selectedFailureDiagnostic.HasExpectedSlotEntryPointer,
        $"{caseName}: independently resolved expected-slot evidence survives selected-slot failure");
    Equal(0x700L, selectedFailureDiagnostic.ExpectedSlotEntryPointer,
        $"{caseName}: expected-slot pointer is checkpointed before selected-slot access");
    Equal(false, selectedFailureDiagnostic.HasSelectedEntryPointer,
        $"{caseName}: failed selected evidence cannot invent a selected-entry pointer");
    Equal(0, RequestSystem.SubmitCount, $"{caseName}: partial diagnostics never mutate or submit requests");
}

Equal(true, CassetteSaveTransactionAdapter.TryReadPersistenceTargetActiveState(
    targetPlayerProcessor, expectedPointer: 0x700,
    out CassettePersistenceTargetActiveState activeTargetState,
    out string activeTargetStage),
    "active persistence state reads only pointer, flags, and DEFAULT bundle evidence");
Equal("success", activeTargetStage, "active persistence state reports success");
Equal(new CassettePersistenceTargetActiveState(0x700, true, true, true, true, 2), activeTargetState, "active persistence state preserves the exact public target fields");
Equal(0, RequestSystem.SubmitCount, "active persistence-state diagnostics never submit a request");

var pointerBoundCalls = new List<string>();
var pointerBoundState = new PlayerSaveFileState(new IntPtr(0x700), pointerBoundCalls);
var pointerBoundProcessor = new DiskCommitTargetPlayerProcessorFixture(new IntPtr(0x111), pointerBoundState);
Equal(true, CassetteSaveTransactionAdapter.TryPreparePointerBoundPersistenceInvocation(
    pointerBoundProcessor, expectedPointer: 0x700,
    out CassettePointerBoundPersistenceInvocationPlan pointerBoundPlan,
    out string pointerBoundStage),
    "exact public PlayerSaveFileState/BaseSaveFileState methods prepare a pointer-bound plan without mutation");
Equal("success", pointerBoundStage, "pointer-bound plan preparation reports success");
Equal(0, pointerBoundCalls.Count, "plan resolution performs no persistence mutation");

var synchronousAdapterRuntime = new CassettePointerBoundPersistenceAcceptanceRuntime();
synchronousAdapterRuntime.BeginEpoch(acceptanceIdentity);
synchronousAdapterRuntime.TryPrepare(acceptanceIdentity, acceptanceBaseline, out CassettePersistenceAcceptanceAttempt synchronousAdapterAttempt);
CassettePersistenceAcceptanceEventOutcome synchronousAdapterEvent = CassettePersistenceAcceptanceEventOutcome.Ignored;
var synchronousMarkerOrder = new List<string> { "PRE" };
pointerBoundState.OnUrgentWrite = () =>
{
    synchronousAdapterEvent = synchronousAdapterRuntime.ObserveWriteCompletedEvent(
        synchronousAdapterAttempt, 4, succeeded: false);
    if (synchronousAdapterEvent != CassettePersistenceAcceptanceEventOutcome.Ignored)
        synchronousMarkerOrder.Add("EVENT");
};
Equal(CassettePointerBoundPersistenceInvocationResult.Invoked,
    CassetteSaveTransactionAdapter.InvokePointerBoundPersistence(
        pointerBoundPlan, out string pointerBoundInvokeStage),
    "exact public pointer-bound promotion and urgent write both return normally");
Equal("success", pointerBoundInvokeStage, "successful pointer-bound invocation reports success");
SequenceEqual(new[] { "PersistAllChangesInBundle:DEFAULT:1", "RequestUrgentWriteToDisk" }, pointerBoundCalls,
    "pointer-bound invocation calls DEFAULT promotion then urgent write exactly once on the same state");
if (synchronousAdapterRuntime.MarkInvoked(synchronousAdapterAttempt))
    synchronousMarkerOrder.Add("INVOKED");
Equal(CassettePersistenceAcceptanceEventOutcome.FailureWake, synchronousAdapterEvent,
    "attempt token exists before a synchronous uncorrelated failure callback from the urgency call");
SequenceEqual(new[] { "PRE", "EVENT", "INVOKED" }, synchronousMarkerOrder,
    "synchronous failure hint logs EVENT before INVOKED and emits no terminal FAILED marker");
Equal(true, synchronousAdapterRuntime.Active,
    "synchronous uncorrelated failure event leaves the invoked trial pending for public-state polling");
Equal(0, RequestSystem.SubmitCount, "pointer-bound invocation never constructs or submits a generic request");

Equal(false, CassetteSaveTransactionAdapter.TryPreparePointerBoundPersistenceInvocation(
    pointerBoundProcessor, expectedPointer: 0x701, out _, out string pointerMismatchPlanStage),
    "pointer-bound plan rejects a state pointer mismatch before mutation");
Equal(true, pointerMismatchPlanStage.StartsWith("acceptance-plan-pointer-mismatch", StringComparison.Ordinal),
    "pointer mismatch has a precise plan stage");
Equal(false, CassetteSaveTransactionAdapter.TryPreparePointerBoundPersistenceInvocation(
    new DiskCommitTargetPlayerProcessorFixture(new IntPtr(0x111), new WrongOwnerPointerBoundState(new IntPtr(0x700))),
    expectedPointer: 0x700, out _, out string wrongOwnerPlanStage),
    "lookalike methods on the wrong public owner are rejected");
Equal("acceptance-plan-player-state-type-mismatch", wrongOwnerPlanStage, "wrong owner has an exact stage");

foreach ((bool ThrowPromotion, bool ThrowUrgency, CassettePointerBoundPersistenceInvocationResult Expected, string StagePrefix, string[] Calls) invocationFailure in new[]
{
    (true, false, CassettePointerBoundPersistenceInvocationResult.PromotionIndeterminate,
        "acceptance-invoke-promote-invocation", new[] { "PersistAllChangesInBundle:DEFAULT:1" }),
    (false, true, CassettePointerBoundPersistenceInvocationResult.UrgencyIndeterminate,
        "acceptance-invoke-urgent-invocation", new[] { "PersistAllChangesInBundle:DEFAULT:1", "RequestUrgentWriteToDisk" }),
})
{
    var failureCalls = new List<string>();
    var failureState = new PlayerSaveFileState(new IntPtr(0x700), failureCalls)
    {
        ThrowPromotion = invocationFailure.ThrowPromotion,
        ThrowUrgency = invocationFailure.ThrowUrgency,
    };
    var failureProcessor = new DiskCommitTargetPlayerProcessorFixture(new IntPtr(0x111), failureState);
    Equal(true, CassetteSaveTransactionAdapter.TryPreparePointerBoundPersistenceInvocation(
        failureProcessor, 0x700, out CassettePointerBoundPersistenceInvocationPlan failurePlan, out _),
        "throwing invocation fixture still resolves the exact public contract before mutation");
    Equal(invocationFailure.Expected,
        CassetteSaveTransactionAdapter.InvokePointerBoundPersistence(failurePlan, out string failureInvokeStage),
        "possibly mutating invocation failure is classified indeterminate");
    Equal(true, failureInvokeStage.StartsWith(invocationFailure.StagePrefix, StringComparison.Ordinal),
        "possibly mutating invocation failure retains its exact stage");
    SequenceEqual(invocationFailure.Calls, failureCalls, "invocation stops at the exact throwing boundary without retry");
}
Console.WriteLine("PASS: pointer_bound_acceptance_adapter_uses_exact_public_state_methods_in_order");

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

PlayerSaveManagementEnquiries.SelectedSlot = new FakeIl2CppNullable<int>(true, 0);
PlayerSaveManagementEnquiries.SelectedState = new FakePlayerSavePublicState(new IntPtr(0x700));
PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
Equal(true, CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
    expectedPointer: 0x700, stage: out string authoritativeReadinessStage),
    "numeric slot zero is accepted when public validity and selected-state pointer match the active epoch");
Equal("success", authoritativeReadinessStage, "authoritative pointer readiness reports success");
PlayerSaveManagementEnquiries.ValidExistingSaveSelected = false;
Equal(false, CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
    expectedPointer: 0x700, stage: out string invalidSelectionStage),
    "invalid selected-save enquiry defers semantic reconciliation");
Equal("readiness-selected-save-invalid", invalidSelectionStage, "invalid selection has an exact stage");
PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
PlayerSaveManagementEnquiries.SelectedState = new FakePlayerSavePublicState(new IntPtr(0x701));
Equal(false, CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
    expectedPointer: 0x700, stage: out string enquiryPointerMismatchStage),
    "selected-state pointer mismatch defers semantic reconciliation");
Equal("readiness-selected-state-pointer-mismatch:expected=0x700:actual=0x701", enquiryPointerMismatchStage, "selected-state pointer mismatch has an exact stage");
PlayerSaveManagementEnquiries.ThrowOnStateRead = true;
Equal(false, CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
    expectedPointer: 0x700, stage: out string unreadableSelectionStage),
    "throwing selected-state enquiry defers semantic reconciliation");
Equal("readiness-selected-state-get-invocation:InvalidOperationException:diagnostic-state-read", unreadableSelectionStage, "throwing selected-state enquiry has an exact stage");
PlayerSaveManagementEnquiries.ResetDiagnosticState();
Equal(0, RequestSystem.SubmitCount, "authoritative readiness checks never submit save requests");
Console.WriteLine("PASS: authoritative_selected_save_pointer_gate_accepts_slot_zero_and_fails_closed");

var readinessTransitionRuntime = new CassetteSaveEpochRuntime();
readinessTransitionRuntime.Receive("BADASS");
readinessTransitionRuntime.ActivateSave(4);
int simulatedGrantRequests = 0;
PlayerSaveManagementEnquiries.ValidExistingSaveSelected = false;
PlayerSaveManagementEnquiries.SelectedState = null;
bool initiallyReady = CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
    expectedPointer: 0x700, out _);
if (initiallyReady && readinessTransitionRuntime.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true))
{
    simulatedGrantRequests++;
    readinessTransitionRuntime.RecordSubmission("BADASS");
}
Equal(0, simulatedGrantRequests, "deferred readiness preserves pending AP ownership without granting");
Equal(true, readinessTransitionRuntime.IsPending("BADASS"), "deferred readiness retains the pending cassette for a later observation");
PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
PlayerSaveManagementEnquiries.SelectedState = new FakePlayerSavePublicState(new IntPtr(0x700));
for (int observation = 0; observation < 2; observation++)
{
    bool ready = CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
        expectedPointer: 0x700, out _);
    if (ready && readinessTransitionRuntime.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, true))
    {
        simulatedGrantRequests++;
        readinessTransitionRuntime.RecordSubmission("BADASS");
    }
}
Equal(1, simulatedGrantRequests, "readiness transition admits the pending grant exactly once across repeated observations");
Equal(0, RequestSystem.SubmitCount, "readiness transition itself never submits a disk commit");
PlayerSaveManagementEnquiries.ResetDiagnosticState();
Console.WriteLine("PASS: cassette_save_synchronization_gate_defers_then_resumes_once");

foreach (string staleSelectionMode in new[] { "invalid", "unreadable", "pointer-mismatch" })
{
    PlayerSaveManagementEnquiries.ResetDiagnosticState();
    PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
    PlayerSaveManagementEnquiries.SelectedState = new FakePlayerSavePublicState(new IntPtr(0x700));
    var delayedVerificationRuntime = new CassetteSaveEpochRuntime();
    delayedVerificationRuntime.Receive("BADASS");
    delayedVerificationRuntime.ActivateSave(4);
    int semanticGrantRequests = 0;
    int cassetteStatusReads = 0;
    Equal(true, CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(0x700, out _), $"{staleSelectionMode}: readiness is initially proven");
    if (delayedVerificationRuntime.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, processorAvailable: true))
    {
        semanticGrantRequests++;
        delayedVerificationRuntime.RecordSubmission("BADASS");
    }
    SequenceEqual(new[] { "BADASS" }, delayedVerificationRuntime.Tick(TimeSpan.FromMilliseconds(250)), $"{staleSelectionMode}: delayed verification becomes due once");

    switch (staleSelectionMode)
    {
        case "invalid":
            PlayerSaveManagementEnquiries.ValidExistingSaveSelected = false;
            break;
        case "unreadable":
            PlayerSaveManagementEnquiries.ThrowOnValidityRead = true;
            break;
        default:
            PlayerSaveManagementEnquiries.SelectedState = new FakePlayerSavePublicState(new IntPtr(0x701));
            break;
    }
    bool staleReady = CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(0x700, out _);
    if (staleReady)
    {
        cassetteStatusReads++;
        delayedVerificationRuntime.RecordVerification("BADASS", CassetteRandomizationPolicy.HaveInBag);
    }
    Equal(0, cassetteStatusReads, $"{staleSelectionMode}: stale selection evidence performs no cassette status read");
    Equal(true, delayedVerificationRuntime.IsPending("BADASS"), $"{staleSelectionMode}: stale selection evidence cannot mark satisfaction");
    SequenceEqual(new[] { "BADASS" }, delayedVerificationRuntime.Tick(TimeSpan.FromMilliseconds(1)), $"{staleSelectionMode}: stale selection evidence does not consume the due verification");
    Equal(false, delayedVerificationRuntime.CanSubmit("BADASS", CassetteRandomizationPolicy.HaveNotEarned, processorAvailable: true), $"{staleSelectionMode}: pending verification cannot duplicate the semantic grant");

    PlayerSaveManagementEnquiries.ResetDiagnosticState();
    PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
    PlayerSaveManagementEnquiries.SelectedState = new FakePlayerSavePublicState(new IntPtr(0x700));
    bool restoredReady = CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(0x700, out _);
    if (restoredReady)
    {
        cassetteStatusReads++;
        delayedVerificationRuntime.RecordVerification("BADASS", CassetteRandomizationPolicy.HaveInBag);
    }
    Equal(1, cassetteStatusReads, $"{staleSelectionMode}: restored exact selection permits one bounded verification read");
    Equal(false, delayedVerificationRuntime.IsPending("BADASS"), $"{staleSelectionMode}: restored exact selection may satisfy from authoritative bag state");
    Equal(1, semanticGrantRequests, $"{staleSelectionMode}: recovery performs no duplicate semantic grant");
}
PlayerSaveManagementEnquiries.ResetDiagnosticState();
Console.WriteLine("PASS: delayed_verification_reproves_selected_save_identity_before_status_read");

PlayerSaveManagementEnquiries.ResetDiagnosticState();
SongCassetteEnquiries.ResetDiagnosticState();
var selectedPostLoadState = new PostLoadCassetteState(
    pointer: 0x700,
    hasUnstagedChanges: false,
    effectiveStatuses: new Dictionary<ePlayableSong, eSongCassetteStatus>
    {
        [ePlayableSong.BADASS] = eSongCassetteStatus.HAVE_IN_BAG,
        [ePlayableSong.HEAVY_METAL] = eSongCassetteStatus.HAVE_NOT_EARNED,
        [ePlayableSong.KEEP_ON_HUSTLIN] = eSongCassetteStatus.HAVE_IN_BAG,
        [ePlayableSong.ON_THE_WAY] = eSongCassetteStatus.HAVE_NOT_EARNED,
    },
    canonicalStatuses: new Dictionary<ePlayableSong, eSongCassetteStatus>
    {
        [ePlayableSong.BADASS] = eSongCassetteStatus.HAVE_NOT_EARNED,
        [ePlayableSong.HEAVY_METAL] = eSongCassetteStatus.HAVE_NOT_EARNED,
        [ePlayableSong.KEEP_ON_HUSTLIN] = eSongCassetteStatus.HAVE_IN_BAG,
        [ePlayableSong.ON_THE_WAY] = eSongCassetteStatus.HAVE_NOT_EARNED,
    });
var processorPostLoadState = new PostLoadCassetteState(
    pointer: 0x700,
    hasUnstagedChanges: false,
    effectiveStatuses: new Dictionary<ePlayableSong, eSongCassetteStatus>(),
    canonicalStatuses: new Dictionary<ePlayableSong, eSongCassetteStatus>());
PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
PlayerSaveManagementEnquiries.SelectedState = selectedPostLoadState;
SongCassetteEnquiries.Statuses = new Dictionary<ePlayableSong, eSongCassetteStatus>
{
    [ePlayableSong.BADASS] = eSongCassetteStatus.HAVE_IN_BAG,
    [ePlayableSong.HEAVY_METAL] = eSongCassetteStatus.HAVE_NOT_EARNED,
    [ePlayableSong.KEEP_ON_HUSTLIN] = eSongCassetteStatus.HAVE_IN_BAG,
    [ePlayableSong.ON_THE_WAY] = eSongCassetteStatus.HAVE_NOT_EARNED,
};
SongCassetteEnquiries.BagSongs = new() { ePlayableSong.BADASS, ePlayableSong.KEEP_ON_HUSTLIN };
string[] postLoadSongs = { "BADASS", "HEAVY_METAL", "KEEP_ON_HUSTLIN", "ON_THE_WAY" };
CassettePostLoadDiagnosticState postLoadDiagnostic = CassetteSaveTransactionAdapter.ReadCassettePostLoadDiagnostic(
    new PostLoadCassetteProcessor(processorPostLoadState), postLoadSongs);
Equal(true, postLoadDiagnostic.SelectionValid.Readable, "post-load diagnostic reads public selected-save validity");
Equal(true, postLoadDiagnostic.SelectionValid.Value, "post-load diagnostic preserves valid selected-save result");
Equal("success", postLoadDiagnostic.SelectionValid.Stage, "post-load validity includes its exact stage");
Equal(0x700L, postLoadDiagnostic.SelectedStatePointer.Value, "post-load diagnostic reads selected-state pointer");
Equal(0x700L, postLoadDiagnostic.ProcessorStatePointer.Value, "post-load diagnostic reads processor-state pointer");
Equal(false, postLoadDiagnostic.HasUnstagedChanges.Value, "post-load diagnostic reads public unstaged state");
Equal(2, postLoadDiagnostic.BagCount.Value, "post-load diagnostic reads public UI bag count");
Equal(true, postLoadDiagnostic.HasAnyBagCassettes.Value, "post-load diagnostic reads public UI any-bag state");
SequenceEqual(new[] { "BADASS", "KEEP_ON_HUSTLIN" }, postLoadDiagnostic.BagSongs.Value!, "post-load diagnostic reads public UI bag list");
CassettePostLoadSongDiagnostic badassPostLoad = postLoadDiagnostic.Songs.Single(song => song.Song == "BADASS");
Equal("HAVE_IN_BAG", badassPostLoad.EffectiveStatus.Value, "post-load diagnostic reads effective selected-state status");
Equal("HAVE_NOT_EARNED", badassPostLoad.CanonicalStatus.Value, "post-load diagnostic reads canonical selected-state status");
Equal("HAVE_IN_BAG", badassPostLoad.EnquiryStatus.Value, "post-load diagnostic reads public static enquiry status");
Equal(true, badassPostLoad.InUiBag.Value, "post-load diagnostic derives per-song UI bag membership from fetched list");
Equal(false, postLoadDiagnostic.Songs.Single(song => song.Song == "HEAVY_METAL").InUiBag.Value, "post-load diagnostic reports absent UI bag song");
string formattedPostLoad = CassetteSaveTransactionAdapter.FormatCassettePostLoadDiagnostic(postLoadDiagnostic);
Equal(true, formattedPostLoad.Contains("BADASS{effective=HAVE_IN_BAG@success canonical=HAVE_NOT_EARNED@success enquiry=HAVE_IN_BAG@success uiBag=True@success}", StringComparison.Ordinal), "compact diagnostic preserves effective/canonical/enquiry/UI evidence");
Equal(0, RequestSystem.SubmitCount, "post-load diagnostic never submits a request");
Console.WriteLine("PASS: post_load_cassette_snapshot_reads_matching_public_state_and_ui_bag");

PlayerSaveManagementEnquiries.SelectedState = new PostLoadCassetteState(
    pointer: 0x701,
    hasUnstagedChanges: true,
    effectiveStatuses: selectedPostLoadState.EffectiveStatuses,
    canonicalStatuses: selectedPostLoadState.CanonicalStatuses);
CassettePostLoadDiagnosticState divergentPostLoadDiagnostic = CassetteSaveTransactionAdapter.ReadCassettePostLoadDiagnostic(
    new PostLoadCassetteProcessor(processorPostLoadState), postLoadSongs);
Equal(0x701L, divergentPostLoadDiagnostic.SelectedStatePointer.Value, "post-load diagnostic preserves divergent selected pointer");
Equal(0x700L, divergentPostLoadDiagnostic.ProcessorStatePointer.Value, "post-load diagnostic independently preserves processor pointer");
Equal(0, RequestSystem.SubmitCount, "pointer-divergence observation never submits a request");
Console.WriteLine("PASS: post_load_cassette_snapshot_exposes_pointer_divergence");

PlayerSaveManagementEnquiries.ThrowOnValidityRead = true;
var partialPostLoadState = new PostLoadCassetteState(
    pointer: 0x702,
    hasUnstagedChanges: true,
    effectiveStatuses: selectedPostLoadState.EffectiveStatuses,
    canonicalStatuses: selectedPostLoadState.CanonicalStatuses)
{
    ThrowEffectiveSong = ePlayableSong.HEAVY_METAL,
};
PlayerSaveManagementEnquiries.SelectedState = partialPostLoadState;
SongCassetteEnquiries.ThrowStatusSong = ePlayableSong.KEEP_ON_HUSTLIN;
SongCassetteEnquiries.ThrowOnCount = true;
CassettePostLoadDiagnosticState partialPostLoadDiagnostic = CassetteSaveTransactionAdapter.ReadCassettePostLoadDiagnostic(
    new ThrowingPostLoadCassetteProcessor(), postLoadSongs);
Equal(false, partialPostLoadDiagnostic.SelectionValid.Readable, "throwing validity remains unavailable");
Equal("selection-validity-get-invocation:InvalidOperationException:diagnostic-validity-read", partialPostLoadDiagnostic.SelectionValid.Stage, "throwing validity preserves exact stage");
Equal(0x702L, partialPostLoadDiagnostic.SelectedStatePointer.Value, "later selected pointer survives independent validity failure");
Equal(true, partialPostLoadDiagnostic.HasUnstagedChanges.Value, "later unstaged evidence survives independent validity failure");
Equal(false, partialPostLoadDiagnostic.ProcessorStatePointer.Readable, "throwing processor pointer remains unavailable");
Equal(false, partialPostLoadDiagnostic.BagCount.Readable, "throwing bag count remains unavailable");
Equal(true, partialPostLoadDiagnostic.HasAnyBagCassettes.Readable, "independent any-bag evidence survives count failure");
Equal(true, partialPostLoadDiagnostic.BagSongs.Readable, "independent bag list evidence survives count failure");
Equal(false, partialPostLoadDiagnostic.Songs.Single(song => song.Song == "HEAVY_METAL").EffectiveStatus.Readable, "one throwing effective status does not erase other evidence");
Equal("HAVE_NOT_EARNED", partialPostLoadDiagnostic.Songs.Single(song => song.Song == "HEAVY_METAL").CanonicalStatus.Value, "canonical status survives same-song effective failure");
Equal(false, partialPostLoadDiagnostic.Songs.Single(song => song.Song == "KEEP_ON_HUSTLIN").EnquiryStatus.Readable, "one throwing static enquiry is isolated");
Equal("HAVE_IN_BAG", partialPostLoadDiagnostic.Songs.Single(song => song.Song == "BADASS").EffectiveStatus.Value, "another song remains readable after partial failures");
Equal(0, RequestSystem.SubmitCount, "partial post-load diagnostic failures never submit a request");

partialPostLoadState.ThrowOnPointerRead = true;
partialPostLoadState.ThrowOnHasUnstagedRead = true;
PlayerSaveManagementEnquiries.ThrowOnValidityRead = false;
CassettePostLoadDiagnosticState throwingSelectedMembersDiagnostic = CassetteSaveTransactionAdapter.ReadCassettePostLoadDiagnostic(
    new PostLoadCassetteProcessor(processorPostLoadState), postLoadSongs);
Equal(false, throwingSelectedMembersDiagnostic.SelectedStatePointer.Readable, "throwing selected pointer is isolated as unavailable");
Equal(false, throwingSelectedMembersDiagnostic.HasUnstagedChanges.Readable, "throwing HasUnstagedChanges is isolated as unavailable");
Equal(true, throwingSelectedMembersDiagnostic.SelectionValid.Value, "validity evidence survives throwing selected-state properties");
Equal("HAVE_IN_BAG", throwingSelectedMembersDiagnostic.Songs.Single(song => song.Song == "BADASS").EffectiveStatus.Value, "status evidence survives throwing selected-state properties");
Equal(true, throwingSelectedMembersDiagnostic.BagSongs.Readable, "UI bag evidence survives throwing selected-state properties");
Console.WriteLine("PASS: post_load_cassette_snapshot_preserves_partial_readable_evidence");

string scanHub6Source = ExtractMethods(pluginSource, "private static void ScanHub6(").Single();
string scanGarageSource = ExtractMethods(pluginSource, "private static void ScanGarage(").Single();
var liveHubGate = new CassettePostLoadSnapshotLiveGate();
int gatedSnapshotReads = 0;
int gatedDeferredLogs = 0;
foreach (bool phoneBankLive in new[] { false, false, true })
{
    CassettePostLoadSnapshotGateDecision decision = liveHubGate.Observe(phoneBankLive);
    if (decision.ReadSnapshot) gatedSnapshotReads++;
    if (decision.LogDeferred) gatedDeferredLogs++;
}
Equal(1, gatedSnapshotReads, "two pre-live F5 observations perform zero reads and one live F5 performs exactly one snapshot read");
Equal(1, gatedDeferredLogs, "repeated pre-live F5 observations emit one bounded deferred record");
CassettePostLoadSnapshotGateDecision laterAbsentDecision = liveHubGate.Observe(phoneBankLive: false);
Equal(false, laterAbsentDecision.ReadSnapshot, "later absent phone bank cannot read a cassette snapshot");
Equal(true, laterAbsentDecision.LogDeferred, "a completed live wave resets bounded deferred reporting for a later absence wave");
Equal(true, scanHub6Source.Contains("CassetteReceiptRandomization.LogPostLoadSnapshot", StringComparison.Ordinal), "Hub6 plain-F5 path invokes one cassette post-load snapshot");
Equal(true, scanHub6Source.Contains("DeveloperHarness.CurrentRoomId", StringComparison.Ordinal), "Hub6 snapshot records the observed current room instead of a hard-coded label");
int liveMarkerIndex = scanHub6Source.IndexOf("GameObject.Find(Hub6PhoneBankRootPath)", StringComparison.Ordinal);
int snapshotReadIndex = scanHub6Source.IndexOf("CassetteReceiptRandomization.LogPostLoadSnapshot", StringComparison.Ordinal);
Equal(true, liveMarkerIndex >= 0, "Hub6 F5 checks the exact live phone-bank scene marker");
Equal(true, liveMarkerIndex < snapshotReadIndex, "Hub6 live marker is checked before any cassette snapshot read");
Equal(true, scanHub6Source.Contains("CASSETTE POST-LOAD SNAPSHOT DEFERRED", StringComparison.Ordinal), "pre-live Hub6 F5 emits the bounded deferred diagnostic");
Equal(true, pluginSource.Contains("Root/GameRoom_Hub6_Logic/Objects/Phones", StringComparison.Ordinal), "post-load snapshot uses the proven Hub6 phone-bank marker");
Equal(false, scanGarageSource.Contains("LogPostLoadSnapshot", StringComparison.Ordinal), "Garage F5 path does not invoke the Hub6 cassette snapshot");
Equal(false, scanGarageSource.Contains("Hub6PhoneBankRootPath", StringComparison.Ordinal), "Garage F5 remains independent of the Hub6 live marker");
Equal(1, pluginSource.Split("CassetteReceiptRandomization.LogPostLoadSnapshot", StringSplitOptions.None).Length - 1, "cassette snapshot has exactly one production call site");

var gameplayGate = new CassetteGameplayReadyGate();
var gameplayIdentity = new CassetteGameplayReadyIdentity(Generation: 8, Epoch: 3, Slot: 4, Pointer: 0x700);
Equal(false, gameplayGate.TryCapture(out _), "phone-bank evidence before activation cannot open a cassette epoch");
gameplayGate.BeginBoundary();
gameplayGate.Activate(gameplayIdentity);
Equal(false, gameplayGate.IsOpen(gameplayIdentity), "every save activation starts closed even when an earlier save was gameplay-ready");
gameplayGate.ObserveMarker(phoneBankLive: true, markerIdentity: 101);
Equal(true, gameplayGate.TryCapture(out CassetteGameplayReadyObservation preLiveObservation), "closed active epoch may capture one exact marker observation token");
Equal(true, gameplayGate.TryOpen(preLiveObservation), "exact live marker opens the matching save epoch once");
Equal(true, gameplayGate.IsOpen(gameplayIdentity), "matching live Hub6 evidence authorizes the current epoch");
Equal(false, gameplayGate.TryOpen(preLiveObservation), "repeated live observations cannot reopen or duplicate reconciliation");
Equal(false, gameplayGate.TryCapture(out _), "open epoch stops per-frame phone-bank probes");
gameplayGate.ObserveMarker(phoneBankLive: false, markerIdentity: 0);
Equal(true, gameplayGate.IsOpen(gameplayIdentity), "ordinary room travel leaves the gameplay-ready epoch open");

gameplayGate.BeginBoundary();
Equal(false, gameplayGate.IsOpen(gameplayIdentity), "a proven save boundary synchronously closes prior gameplay readiness");
var reloadedIdentity = gameplayIdentity with { Generation = 9, Epoch = 4 };
gameplayGate.Activate(reloadedIdentity);
Equal(false, gameplayGate.TryOpen(preLiveObservation), "stale marker evidence from the prior epoch is rejected");
Equal(false, gameplayGate.IsOpen(reloadedIdentity), "stale prior-epoch evidence cannot authorize reads or grants");
gameplayGate.ObserveMarker(phoneBankLive: true, markerIdentity: 202);
Equal(true, gameplayGate.TryCapture(out CassetteGameplayReadyObservation reloadObservation), "post-boundary marker incarnation produces fresh evidence");
Equal(true, gameplayGate.TryOpen(reloadObservation), "fresh evidence opens the reloaded epoch");

var switchedIdentity = new CassetteGameplayReadyIdentity(Generation: 10, Epoch: 5, Slot: 2, Pointer: 0x900);
gameplayGate.BeginBoundary();
gameplayGate.Activate(switchedIdentity);
Equal(false, gameplayGate.IsOpen(switchedIdentity), "save switch activation closes the prior save's readiness");
Equal(false, gameplayGate.TryOpen(reloadObservation), "prior-save evidence cannot open a different slot or pointer");

var retainedMarkerGate = new CassetteGameplayReadyGate();
var retainedMarkerEpochOne = new CassetteGameplayReadyIdentity(Generation: 20, Epoch: 1, Slot: 4, Pointer: 0xA00);
retainedMarkerGate.BeginBoundary();
retainedMarkerGate.ObserveMarker(phoneBankLive: true, markerIdentity: 303);
retainedMarkerGate.Activate(retainedMarkerEpochOne);
Equal(true, retainedMarkerGate.TryCapture(out CassetteGameplayReadyObservation retainedMarkerFirstObservation), "first boundary captures its fresh marker incarnation even when activation follows marker creation");
Equal(true, retainedMarkerGate.TryOpen(retainedMarkerFirstObservation), "first fresh incarnation opens its epoch");
retainedMarkerGate.BeginBoundary();
var retainedMarkerEpochTwo = retainedMarkerEpochOne with { Generation = 21, Epoch = 2 };
retainedMarkerGate.Activate(retainedMarkerEpochTwo);
retainedMarkerGate.ObserveMarker(phoneBankLive: true, markerIdentity: 303);
Equal(false, retainedMarkerGate.TryCapture(out _), "new epoch cannot reuse a phone-bank object retained live from the previous boundary");
retainedMarkerGate.ObserveMarker(phoneBankLive: false, markerIdentity: 0);
Equal(false, retainedMarkerGate.TryCapture(out _), "post-boundary marker absence alone cannot open the epoch");
retainedMarkerGate.ObserveMarker(phoneBankLive: true, markerIdentity: 404);
Equal(true, retainedMarkerGate.TryCapture(out CassetteGameplayReadyObservation retainedMarkerFreshObservation), "absence then new presence supplies post-boundary lifetime evidence");
Equal(true, retainedMarkerGate.TryOpen(retainedMarkerFreshObservation), "new marker incarnation opens the matching new epoch");

var newestBoundaryWinsGate = new CassetteGameplayReadyGate();
newestBoundaryWinsGate.BeginBoundary();
newestBoundaryWinsGate.ObserveMarker(phoneBankLive: true, markerIdentity: 606);
newestBoundaryWinsGate.BeginBoundary();
var newestBoundaryIdentity = new CassetteGameplayReadyIdentity(Generation: 40, Epoch: 1, Slot: 4, Pointer: 0xC00);
newestBoundaryWinsGate.Activate(newestBoundaryIdentity);
Equal(false, newestBoundaryWinsGate.TryCapture(out _), "a newer exact boundary invalidates fresh marker evidence retained by the prior pending boundary");
newestBoundaryWinsGate.ObserveMarker(phoneBankLive: true, markerIdentity: 606);
Equal(false, newestBoundaryWinsGate.TryCapture(out _), "the same retained marker cannot become fresh merely because the newer epoch activated");
newestBoundaryWinsGate.ObserveMarker(phoneBankLive: false, markerIdentity: 0);
newestBoundaryWinsGate.ObserveMarker(phoneBankLive: true, markerIdentity: 707);
Equal(true, newestBoundaryWinsGate.TryCapture(out CassetteGameplayReadyObservation newestBoundaryObservation), "post-newest-boundary marker lifetime evidence authorizes capture");
Equal(true, newestBoundaryWinsGate.TryOpen(newestBoundaryObservation), "only newest-boundary marker evidence opens the epoch");

var malformedPendingBoundaryGate = new CassetteGameplayReadyGate();
malformedPendingBoundaryGate.BeginBoundary();
malformedPendingBoundaryGate.ObserveMarker(phoneBankLive: true, markerIdentity: 808);
malformedPendingBoundaryGate.BeginBoundary(); // malformed exact callback closes first, then rejects its arguments
var postMalformedIdentity = new CassetteGameplayReadyIdentity(Generation: 50, Epoch: 1, Slot: 4, Pointer: 0xD00);
malformedPendingBoundaryGate.Activate(postMalformedIdentity);
Equal(false, malformedPendingBoundaryGate.TryCapture(out _), "malformed exact callback invalidates marker evidence from an already-pending prior boundary");

var activationBoundaryGate = new CassetteGameplayReadyGate();
activationBoundaryGate.ObserveMarker(phoneBankLive: true, markerIdentity: 909);
var activationBoundaryIdentity = new CassetteGameplayReadyIdentity(Generation: 60, Epoch: 1, Slot: 4, Pointer: 0xE00);
activationBoundaryGate.Activate(activationBoundaryIdentity);
Equal(false, activationBoundaryGate.TryCapture(out _), "activation without a pending callback begins its own boundary and rejects pre-activation marker evidence");
activationBoundaryGate.ObserveMarker(phoneBankLive: false, markerIdentity: 0);
activationBoundaryGate.ObserveMarker(phoneBankLive: true, markerIdentity: 1001);
Equal(true, activationBoundaryGate.TryCapture(out _), "activation-owned boundary accepts only later marker lifetime evidence");

var malformedBoundaryGate = new CassetteGameplayReadyGate();
var malformedBoundaryIdentity = new CassetteGameplayReadyIdentity(Generation: 30, Epoch: 1, Slot: 4, Pointer: 0xB00);
malformedBoundaryGate.BeginBoundary();
malformedBoundaryGate.ObserveMarker(phoneBankLive: true, markerIdentity: 505);
malformedBoundaryGate.Activate(malformedBoundaryIdentity);
Equal(true, malformedBoundaryGate.TryCapture(out CassetteGameplayReadyObservation malformedBoundaryObservation), "fixture opens before malformed exact callback");
Equal(true, malformedBoundaryGate.TryOpen(malformedBoundaryObservation), "fixture proves the old gate started open");
malformedBoundaryGate.BeginBoundary(); // exact postfix must do this before discovering null or malformed arguments
Equal(false, malformedBoundaryGate.IsOpen(malformedBoundaryIdentity), "malformed exact callback still closes the old gameplay-ready gate");
Console.WriteLine("PASS: cassette_gameplay_ready_gate_correlates_live_hub_to_exact_save_epoch");

string gameplayCaptureSource = ExtractMethods(receiptRandomizationSource, "internal static bool TryCaptureGameplayReadyObservation(").Single();
string gameplayObserveSource = ExtractMethods(receiptRandomizationSource, "internal static void ObserveGameplayReady(").Single();
string gameplayBoundarySuspendSource = ExtractMethods(receiptRandomizationSource, "internal static void SuspendGameplayReadinessForBoundary(").Single();
Equal(false, queueBoundary.Contains("_gameplayReady.BeginBoundary()", StringComparison.Ordinal), "validated queue records identity without double-advancing the boundary already begun by its exact callback");
Equal(3, pluginSource.Split("QueueSaveBoundarySignal(", StringSplitOptions.None).Length - 1, "only the two exact validated postfixes call the save-boundary identity queue");
Equal(false, gameplayCaptureSource.Contains("TryReadCassetteStatus", StringComparison.Ordinal), "marker capture performs no cassette status read");
Equal(false, gameplayObserveSource.Contains("TrySubmitHaveInBag", StringComparison.Ordinal), "marker observation never grants directly");
Equal(false, gameplayObserveSource.Contains("TryReconcile(", StringComparison.Ordinal), "marker observation only queues managed reconciliation");
Equal(false, gameplayBoundarySuspendSource.Contains("GameObject", StringComparison.Ordinal), "non-Unity save callbacks close only managed gate state and never probe scene objects");
Equal(true, queueBoundary.Contains("prior gameplay readiness was suspended before new-load identity processing", StringComparison.Ordinal), "valid boundary diagnostic states that closure precedes new-load identity work");
Equal(true, beginMostRecentBoundary.Contains("_gameplayReady.BeginBoundary()", StringComparison.Ordinal), "unresolved most-recent boundary synchronously closes gameplay readiness");
Equal(1, beginMostRecentBoundary.Split("_gameplayReady.BeginBoundary()", StringSplitOptions.None).Length - 1, "most-recent callback begins exactly one gameplay boundary");
Equal(true, activateLoadedSave.Contains("_gameplayReady.Activate", StringComparison.Ordinal), "each activated epoch requires fresh Hub6 readiness");
int reconcileGameplayGateIndex = reconcileSource.IndexOf("_gameplayReady.IsOpen", StringComparison.Ordinal);
Equal(true, reconcileGameplayGateIndex >= 0 && reconcileGameplayGateIndex < reconcileSource.IndexOf("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal), "closed gameplay gate prevents reconciliation before any native readiness/status read");
int reconcileSongGameplayGateIndex = reconcileSongSource.IndexOf("_gameplayReady.IsOpen", StringComparison.Ordinal);
Equal(true, reconcileSongGameplayGateIndex >= 0 && reconcileSongGameplayGateIndex < reconcileSongSource.IndexOf("TryConfirmSaveSynchronizationReady", StringComparison.Ordinal), "every status read, grant, and delayed verification rechecks exact gameplay readiness");
Equal(true, unityTick.IndexOf("_gameplayReady.IsOpen", StringComparison.Ordinal) < unityTick.IndexOf("_runtime.Tick(elapsed)", StringComparison.Ordinal), "closed gameplay gate freezes bounded verification timers");
Equal(false, gameplayObserveSource.Contains("ActivateLoadedSave", StringComparison.Ordinal), "Hub6 readiness never creates a new save epoch");
string postLoadLoggerSource = ExtractMethods(pluginSource, "internal static void LogPostLoadSnapshot(").Single();
string postLoadReaderSource = ExtractMethods(transactionAdapterSource, "internal static CassettePostLoadDiagnosticState ReadCassettePostLoadDiagnostic(").Single();
foreach (string forbiddenDiagnosticCall in new[]
{
    "TrySubmitHaveInBag", "RecordSongCassetteStatusInSaveDataRequest", "ProcessRequest", "Persist",
    "TriggerUrgent", "RequestWrite", "SubmitRequest", "TryReconcile", "RequestUnityReconciliation", "RecordSubmission",
})
    Equal(false,
        postLoadLoggerSource.Contains(forbiddenDiagnosticCall, StringComparison.OrdinalIgnoreCase) ||
        postLoadReaderSource.Contains(forbiddenDiagnosticCall, StringComparison.OrdinalIgnoreCase),
        $"post-load diagnostic boundary forbids {forbiddenDiagnosticCall}");
Equal(true, postLoadLoggerSource.Contains("ReadCassettePostLoadDiagnostic", StringComparison.Ordinal), "post-load logger delegates only to the public-state diagnostic reader");
Equal(0, RequestSystem.SubmitCount, "read-only Hub6 diagnostic wiring submits no request");
PlayerSaveManagementEnquiries.ResetDiagnosticState();
SongCassetteEnquiries.ResetDiagnosticState();
Console.WriteLine("PASS: post_load_cassette_snapshot_is_wired_once_only_to_hub6_plain_f5_and_read_only");

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
Equal(false, adapterSource.Contains("TrySubmitDefaultUrgentPersist", StringComparison.Ordinal), "transaction adapter exposes no synthetic Persist submission path");
Equal(false, factorySource.Contains("TryCreateDefaultUrgentPersistRequest", StringComparison.Ordinal), "request factory exposes no synthetic Persist construction path");
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
string pointerBoundPlanAdapter = ExtractMethods(adapterSource, "internal static bool TryPreparePointerBoundPersistenceInvocation(").Single();
string pointerBoundInvokeAdapter = ExtractMethods(adapterSource, "internal static CassettePointerBoundPersistenceInvocationResult InvokePointerBoundPersistence(").Single();
Equal(true, pointerBoundPlanAdapter.Contains("PublicInstance", StringComparison.Ordinal), "acceptance plan resolves only public instance methods");
Equal(false, pointerBoundPlanAdapter.Contains("AllInstance", StringComparison.Ordinal) || pointerBoundPlanAdapter.Contains("NonPublic", StringComparison.Ordinal), "acceptance plan cannot bind private methods");
Equal(true, pointerBoundInvokeAdapter.Contains("PersistDefaultMethod.Invoke", StringComparison.Ordinal) && pointerBoundInvokeAdapter.Contains("RequestUrgentWriteMethod.Invoke", StringComparison.Ordinal), "acceptance invocation is limited to its prevalidated exact public methods");
foreach (string prohibitedAcceptancePath in new[] { "SubmitRequest", "ProcessRequest", "PersistSaveChangeBundleRequest", "PersistAllSaveChangeBundlesRequest", "RequestWriteForPlayerSave", "SaveDataManager", "WritePlayerSaveFile", "TriggerUrgentSaveWriteIfAnyChangesRequest" })
    Equal(false, pointerBoundPlanAdapter.Contains(prohibitedAcceptancePath, StringComparison.Ordinal) || pointerBoundInvokeAdapter.Contains(prohibitedAcceptancePath, StringComparison.Ordinal), $"pointer-bound acceptance prohibits {prohibitedAcceptancePath}");
foreach (string prohibited in new[] { "RequestWriteForPlayerSave", "SaveDataManager", "WritePlayerSaveFile", "SelectedPlayerSaveSlotChangedEvent", "TriggerUrgentSaveWriteIfAnyChangesRequest" })
    Equal(false, adapterSource.Contains(prohibited, StringComparison.Ordinal), $"transaction adapter prohibits {prohibited}");
Console.WriteLine("PASS: native_save_selection_and_semantic_grant_adapters");
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
enum ePlayableSong { INVALID, QUIERES_BAILAR, I_GOT_MONEY, BADASS, HEAVY_METAL, KEEP_ON_HUSTLIN, ON_THE_WAY }
enum eSongCassetteStatus { INVALID, HAVE_NOT_EARNED, HAVE_IN_BAG }

sealed class PostLoadCassetteProcessor
{
    private readonly object _state;
    public PostLoadCassetteProcessor(object state) => _state = state;
    public object ObtainState() => _state;
}

sealed class ThrowingPostLoadCassetteProcessor
{
    public object ObtainState() => throw new InvalidOperationException("post-load-processor-state");
}

sealed class PostLoadCassetteState
{
    private readonly IntPtr _pointer;
    private readonly bool _hasUnstagedChanges;

    public PostLoadCassetteState(
        long pointer,
        bool hasUnstagedChanges,
        Dictionary<ePlayableSong, eSongCassetteStatus> effectiveStatuses,
        Dictionary<ePlayableSong, eSongCassetteStatus> canonicalStatuses)
    {
        _pointer = new IntPtr(pointer);
        _hasUnstagedChanges = hasUnstagedChanges;
        EffectiveStatuses = effectiveStatuses;
        CanonicalStatuses = canonicalStatuses;
        GameProgression = new PostLoadGameProgression(canonicalStatuses);
    }

    public bool ThrowOnPointerRead { get; set; }
    public bool ThrowOnHasUnstagedRead { get; set; }
    public IntPtr Pointer => ThrowOnPointerRead
        ? throw new InvalidOperationException("selected-pointer")
        : _pointer;
    public bool HasUnstagedChanges => ThrowOnHasUnstagedRead
        ? throw new InvalidOperationException("selected-has-unstaged")
        : _hasUnstagedChanges;
    public Dictionary<ePlayableSong, eSongCassetteStatus> EffectiveStatuses { get; }
    public Dictionary<ePlayableSong, eSongCassetteStatus> CanonicalStatuses { get; }
    public PostLoadGameProgression GameProgression { get; }
    public ePlayableSong? ThrowEffectiveSong { get; init; }

    public eSongCassetteStatus GetCassetteStatusForSong(ePlayableSong song)
    {
        if (ThrowEffectiveSong == song) throw new InvalidOperationException($"effective-{song}");
        return EffectiveStatuses.TryGetValue(song, out eSongCassetteStatus status)
            ? status
            : eSongCassetteStatus.INVALID;
    }
}

sealed class PostLoadGameProgression
{
    private readonly Dictionary<ePlayableSong, eSongCassetteStatus> _statuses;
    public PostLoadGameProgression(Dictionary<ePlayableSong, eSongCassetteStatus> statuses) => _statuses = statuses;
    public FakeIl2CppNullable<eSongCassetteStatus> GetSongCassetteStatus(ePlayableSong song) =>
        _statuses.TryGetValue(song, out eSongCassetteStatus status)
            ? new(true, status)
            : new(false, eSongCassetteStatus.INVALID);
}

static class SongCassetteEnquiries
{
    public static Dictionary<ePlayableSong, eSongCassetteStatus> Statuses { get; set; } = new();
    public static List<ePlayableSong> BagSongs { get; set; } = new();
    public static ePlayableSong? ThrowStatusSong { get; set; }
    public static bool ThrowOnCount { get; set; }

    public static FakeIl2CppNullable<eSongCassetteStatus> GetSongCassetteStatus(ePlayableSong song)
    {
        if (ThrowStatusSong == song) throw new InvalidOperationException($"enquiry-{song}");
        return Statuses.TryGetValue(song, out eSongCassetteStatus status)
            ? new(true, status)
            : new(false, eSongCassetteStatus.INVALID);
    }

    public static bool HasAnyCassettesInBag() => BagSongs.Count > 0;
    public static int GetNumSongCassettesInBag() => ThrowOnCount
        ? throw new InvalidOperationException("bag-count")
        : BagSongs.Count;
    public static void FetchAllSongCassettesInBag(List<ePlayableSong> songs) => songs.AddRange(BagSongs);

    public static void ResetDiagnosticState()
    {
        Statuses = new();
        BagSongs = new();
        ThrowStatusSong = null;
        ThrowOnCount = false;
    }
}

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

sealed class ThrowingSelectedSlotDiskCommitTargetSaveDataStateFixture
{
    public ThrowingSelectedSlotDiskCommitTargetSaveDataStateFixture(
        IntPtr pointer, Dictionary<int, DiskCommitTargetPlayerStateFixture> saves)
    { Pointer = pointer; RegularPlayerSaves = saves; }
    public IntPtr Pointer { get; }
    public FakeIl2CppNullable<int> SelectedPlayerSaveSlot =>
        throw new InvalidOperationException("selected-slot-get");
    public Dictionary<int, DiskCommitTargetPlayerStateFixture> RegularPlayerSaves { get; }
}

sealed class MalformedSelectedNullable<T> { }

sealed class MalformedSelectedSlotDiskCommitTargetSaveDataStateFixture
{
    public MalformedSelectedSlotDiskCommitTargetSaveDataStateFixture(
        IntPtr pointer, Dictionary<int, DiskCommitTargetPlayerStateFixture> saves)
    { Pointer = pointer; RegularPlayerSaves = saves; }
    public IntPtr Pointer { get; }
    public MalformedSelectedNullable<int> SelectedPlayerSaveSlot { get; } = new();
    public Dictionary<int, DiskCommitTargetPlayerStateFixture> RegularPlayerSaves { get; }
}

sealed class UnconvertibleSelectedSlotDiskCommitTargetSaveDataStateFixture
{
    public UnconvertibleSelectedSlotDiskCommitTargetSaveDataStateFixture(
        IntPtr pointer, Dictionary<int, DiskCommitTargetPlayerStateFixture> saves)
    { Pointer = pointer; RegularPlayerSaves = saves; }
    public IntPtr Pointer { get; }
    public FakeIl2CppNullable<string> SelectedPlayerSaveSlot { get; } = new(true, "not-an-integer");
    public Dictionary<int, DiskCommitTargetPlayerStateFixture> RegularPlayerSaves { get; }
}

class BaseSaveFileState
{
    private readonly List<string> _calls;
    protected BaseSaveFileState(IntPtr pointer, List<string> calls) { Pointer = pointer; _calls = calls; }
    public IntPtr Pointer { get; }
    public bool ThrowUrgency { get; set; }
    public Action? OnUrgentWrite { get; set; }
    public void RequestUrgentWriteToDisk()
    {
        _calls.Add("RequestUrgentWriteToDisk");
        OnUrgentWrite?.Invoke();
        if (ThrowUrgency) throw new InvalidOperationException("urgent-write");
    }
}

class PlayerSaveFileState : BaseSaveFileState
{
    private readonly List<string> _calls;
    public PlayerSaveFileState(IntPtr pointer, List<string> calls) : base(pointer, calls) => _calls = calls;
    public bool ThrowPromotion { get; set; }
    public void PersistAllChangesInBundle(ePlayerSaveChangeBundleKey bundle)
    {
        _calls.Add($"PersistAllChangesInBundle:{bundle}:{Convert.ToInt32(bundle)}");
        if (ThrowPromotion) throw new InvalidOperationException("promote-default");
    }
}

sealed class WrongOwnerPointerBoundState
{
    public WrongOwnerPointerBoundState(IntPtr pointer) => Pointer = pointer;
    public IntPtr Pointer { get; }
    public void PersistAllChangesInBundle(ePlayerSaveChangeBundleKey bundle) { }
    public void RequestUrgentWriteToDisk() { }
}

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
