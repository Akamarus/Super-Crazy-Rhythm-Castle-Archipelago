using RhythmCastleAP;
using Newtonsoft.Json.Linq;
using System.Text.Json;

static void Equal<T>(T expected, T actual, string scenario) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}"); }
static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual, string scenario) { var e=expected.ToArray(); var a=actual.ToArray(); if (!e.SequenceEqual(a, StringComparer.Ordinal)) throw new InvalidOperationException($"{scenario}: expected [{string.Join(", ",e)}], got [{string.Join(", ",a)}]"); }
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

var constructedRequest = CassetteNativeRequestFactory.TryCreateHaveInBagRequest(
    typeof(TestCassetteRequest),
    typeof(TestSong),
    typeof(TestCassetteStatus),
    typeof(TestBundle),
    nameof(TestSong.QUIERES_BAILAR),
    out object? request,
    out string requestDetail);
Equal(true, constructedRequest, "cassette request uses semantic constructor");
var typedRequest = (TestCassetteRequest)request!;
Equal(TestSong.QUIERES_BAILAR, typedRequest.Song, "semantic constructor receives song");
Equal(TestCassetteStatus.HAVE_IN_BAG, typedRequest.CassetteStatus, "semantic constructor receives bag status");
Equal(TestBundle.DEFAULT, typedRequest.Bundle, "semantic constructor receives default bundle");
Equal(true, typedRequest.SemanticConstructorUsed, "parameterless member-write construction is prohibited");
Equal("song='QUIERES_BAILAR' status='HAVE_IN_BAG' bundle='DEFAULT'", requestDetail, "constructor detail includes semantic values");
string pluginSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));

Equal(true, pluginSource.Contains("PatchExactMethod(\"SaveDataRequestProcessor\", \"ChangeSelectedPlayerSaveSlot\", \"Int32\", nameof(CassetteSaveTransactionPatches.SelectedSlotMutationPostfix))", StringComparison.Ordinal), "exact selected-slot mutation hook installed");
Equal(true, pluginSource.Contains("PatchExactMethod(\"SaveDataRequestProcessor\", \"CreateNewPlayerSaveFileInEmptySlot\", \"Int32\", nameof(CassetteSaveTransactionPatches.SelectedSlotMutationPostfix))", StringComparison.Ordinal), "exact empty-slot creation hook installed");
Equal(true, pluginSource.Contains("PatchExactMethod(\"SaveDataRequestProcessor\", \"ProcessRequest\", \"BuildPlayerSaveStateFromFileRequest\", nameof(CassetteSaveTransactionPatches.BuiltPlayerSaveStatePostfix))", StringComparison.Ordinal), "exact save-state build hook installed");
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
string extractionDiagnostic = ExtractMethods(pluginSource, "private static void LogExtractionFailureOnce(").Single();
Equal(true, extractionDiagnostic.Contains("ExtractionFailures.Add", StringComparison.Ordinal), "extraction diagnostics are bounded by a one-time key set");
Equal(false, extractionDiagnostic.Contains("ReflectionUtil.ReadMember", StringComparison.Ordinal), "diagnostic logger performs no unsafe object traversal");
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
Equal(false, queueBoundary.Contains("if (kind == CassetteSaveBoundarySignalKind.Selection)", StringComparison.Ordinal), "Creation and Build cannot bypass stabilizer signaling");
Equal(true, queueBoundary.Contains("_unityReconciliationRequested = false", StringComparison.Ordinal), "exact boundary callback suspends prior reconciliation immediately");
Equal(true, queueBoundary.Contains("IsCompatiblePlayerSaveRequestProcessor(_playerSaveRequestProcessor)", StringComparison.Ordinal), "selection binds only a compatible preexisting processor");
string captureProcessor = ExtractMethods(receiptRandomizationSource, "internal static void CapturePlayerSaveRequestProcessor(").Single();
Equal(true, captureProcessor.Contains("_saveIdentity.CaptureProcessor", StringComparison.Ordinal), "compatible post-selection processor can bind pending selection");
Equal(false, queueBoundary.Contains("TryGetLoadedSave", StringComparison.Ordinal), "boundary callback performs no native selected-save read");
string diskCommitSource = ExtractMethods(receiptRandomizationSource, "private static void TickDiskCommit(").Single();
Equal(true, diskCommitSource.Contains("TrySubmitDefaultUrgentPersist", StringComparison.Ordinal), "verified cassette wave submits the narrow public persist transaction");
Equal(true, diskCommitSource.Contains("TryReadPublicWriteState", StringComparison.Ordinal), "disk transaction polls public write completion state");
Equal(true, diskCommitSource.Contains("ObserveUnavailable", StringComparison.Ordinal), "unreadable public write state still advances bounded fail-closed completion");
foreach (string prohibited in new[] { "PersistAllSaveChangeBundlesRequest", "TriggerUrgentSaveWriteIfAnyChangesRequest", "RequestWriteForPlayerSave", "SaveDataManager", "WritePlayerSaveFile" })
    Equal(false, diskCommitSource.Contains(prohibited, StringComparison.Ordinal), $"disk transaction avoids prohibited broad/private path {prohibited}");
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
diskCommit.Stage("BADASS"); diskCommit.Stage("THE_HEIST"); diskCommit.Stage("BADASS");
Equal(2, diskCommit.Songs.Count, "one reconciliation wave coalesces cassette grants");
var dirtyWrite = new CassettePublicWriteState(true, false, 10, null, null);
Equal(true, diskCommit.TryBegin(1, 4, 400, dirtyWrite), "dirty verified wave begins one disk transaction");
Equal(false, diskCommit.TryBegin(1, 4, 400, dirtyWrite), "active wave cannot submit a duplicate persist");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.Observe(1, 4, 400, new(true, true, 10, null, null), true, TimeSpan.FromSeconds(1)), "required disk write remains pending");
Equal(CassetteDiskCommitOutcome.Success, diskCommit.Observe(1, 4, 400, new(false, false, 11, null, null), true, TimeSpan.FromSeconds(1)), "advanced successful write completes transaction");
Equal(false, diskCommit.HasWork, "successful disk commit clears batched songs");

diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryBegin(2, 4, 401, dirtyWrite), "first verified wave begins");
diskCommit.Stage("THE_HEIST");
Equal(CassetteDiskCommitOutcome.Success, diskCommit.Observe(2, 4, 401, new(false, false, 11, null, null), true, TimeSpan.Zero), "first wave can complete after a later grant verifies");
Equal(true, diskCommit.HasWork, "grant verified after submission remains queued for its own disk transaction");
Equal(1, diskCommit.Songs.Count, "successful write clears only the submitted wave");
Equal("THE_HEIST", diskCommit.Songs[0], "later verified grant is never attributed to the earlier write");

diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryBegin(2, 4, 401, dirtyWrite), "failure case begins");
Equal(CassetteDiskCommitOutcome.Failure, diskCommit.Observe(2, 4, 401, new(true, false, 10, 12, "IO_ERROR"), true, TimeSpan.Zero), "advanced failure fails transaction");
Equal(false, diskCommit.HasWork, "failed wave is bounded until a new epoch or newly verified song");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryBegin(2, 4, 401, dirtyWrite), "timeout case begins");
Equal(CassetteDiskCommitOutcome.Timeout, diskCommit.Observe(2, 4, 401, dirtyWrite, true, TimeSpan.FromSeconds(10)), "bounded disk transaction times out");
Equal(false, diskCommit.HasWork, "timed out wave cannot resubmit every frame");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryBegin(2, 4, 401, dirtyWrite), "epoch-switch case begins");
Equal(CassetteDiskCommitOutcome.Cancelled, diskCommit.Observe(3, 4, 401, dirtyWrite, true, TimeSpan.Zero), "epoch switch cancels transaction");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryBegin(3, 4, 401, dirtyWrite), "pointer-switch case begins");
Equal(CassetteDiskCommitOutcome.Cancelled, diskCommit.Observe(3, 4, 402, dirtyWrite, true, TimeSpan.Zero), "pointer switch cancels transaction");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryBegin(3, 4, 402, dirtyWrite), "status-regression case begins");
Equal(CassetteDiskCommitOutcome.Failure, diskCommit.Observe(3, 4, 402, dirtyWrite, false, TimeSpan.Zero), "cassette status regression fails closed");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryBegin(3, 4, 402, dirtyWrite), "unreadable-state timeout case begins");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.ObserveUnavailable(3, 4, 402, true, TimeSpan.FromSeconds(9)), "temporarily unreadable write state remains pending within the bound");
Equal(CassetteDiskCommitOutcome.Timeout, diskCommit.ObserveUnavailable(3, 4, 402, true, TimeSpan.FromSeconds(1)), "unreadable write state cannot postpone timeout indefinitely");
diskCommit.Reset(); diskCommit.Stage("BADASS"); Equal(true, diskCommit.TryBegin(3, 4, 402, dirtyWrite), "unreadable-state identity case begins");
Equal(CassetteDiskCommitOutcome.Cancelled, diskCommit.ObserveUnavailable(3, 4, 403, true, TimeSpan.Zero), "identity replacement fails closed even when write state is unreadable");
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
Equal(true, diskCommit.TryBegin(7, 4, 407, dirtyWrite), "readable state begins transaction after pre-submit acquisition");
Equal(CassetteDiskCommitOutcome.Pending, diskCommit.Observe(7, 4, 407, dirtyWrite, true, TimeSpan.FromSeconds(9)), "pre-submit acquisition time is not charged to submitted write confirmation");
diskCommit.Reset();
Equal(false, diskCommit.TryBegin(3, 4, 402, dirtyWrite), "no verified songs means no persist");
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
Equal(ePlayerSaveChangeBundleKey.DEFAULT, semanticProcessor.LastRequest.Bundle, "request uses default native bundle");
Equal(true, semanticProcessor.LastRequest.SemanticConstructorUsed, "submission uses semantic cassette constructor");
Equal(true, submitDetail.Contains("DEFAULT", StringComparison.Ordinal), "submission detail identifies default bundle");
var publicWriteProcessor = new PublicWriteProcessorFixture(new PublicWriteStateFixture());
Equal(true, CassetteSaveTransactionAdapter.TryReadPublicWriteState(publicWriteProcessor, out CassettePublicWriteState publicWriteState, out string publicWriteStage), "adapter reads the public write-completion contract");
Equal(new CassettePublicWriteState(true, false, 10, 8, "IO_ERROR"), publicWriteState, "adapter returns exact public write-completion values");
Equal("success", publicWriteStage, "public write-completion read reports success");
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteState(new PublicWriteProcessorFixture(new PrivateSuccessNullableWriteStateFixture()), out _, out publicWriteStage), "private success-time nullable contract fails closed");
Equal("write-success-time-nullable-contract-missing", publicWriteStage, "private success-time wrapper reports the exact public boundary failure");
Equal(false, CassetteSaveTransactionAdapter.TryReadPublicWriteState(new PublicWriteProcessorFixture(new PrivateReasonNullableWriteStateFixture()), out _, out publicWriteStage), "private failure-reason nullable contract fails closed");
Equal("write-failure-reason-nullable-contract-missing", publicWriteStage, "private failure-reason wrapper reports the exact public boundary failure");
RequestSystem.Reset();
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
Equal(0, RequestSystem.SubmitCount, "write-state diagnostics never submit a persist request");
Equal(true, CassetteSaveTransactionAdapter.TrySubmitDefaultUrgentPersist(out string persistDetail), "adapter submits narrow public persist request");
Equal(1, RequestSystem.SubmitCount, "batched transaction submits one persist request");
Equal(ePlayerSaveChangeBundleKey.DEFAULT, RequestSystem.LastRequest!.Bundle.Value, "persist request uses exact DEFAULT bundle");
Equal(eSaveFileWriteType.URGENT, RequestSystem.LastRequest.WriteTypeToRequest, "persist request uses exact URGENT write type");
Equal(true, persistDetail.Contains("DEFAULT", StringComparison.Ordinal) && persistDetail.Contains("URGENT", StringComparison.Ordinal), "persist detail records exact public semantics");

string adapterSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "CassetteSaveTransactionAdapter.cs"));
foreach (string obsoleteDiagnostic in new[] { "CassetteSaveFingerprint", "TryGetLoadedSaveFingerprint", "TryGetProcessorSaveFingerprint", "TryBuildFingerprint", "UnwrapNullablePublic" })
    Equal(false, adapterSource.Contains(obsoleteDiagnostic, StringComparison.Ordinal), $"temporary adapter diagnostic removed: {obsoleteDiagnostic}");
string policySource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "CassetteRandomizationPolicy.cs"));
foreach (string obsoleteDiagnostic in new[] { "CassetteSaveIdentityStabilizer", "CassetteMostRecentIdentityProbe", "CassetteProcessorIdentityProbe", "CassetteSaveFingerprint" })
    Equal(false, policySource.Contains(obsoleteDiagnostic, StringComparison.Ordinal), $"temporary policy diagnostic removed: {obsoleteDiagnostic}");
string processorIdentityAdapter = ExtractMethods(adapterSource, "internal static bool TryGetProcessorSaveIdentity(").Single();
Equal(true, processorIdentityAdapter.Contains("PublicInstance", StringComparison.Ordinal), "authoritative processor identity uses public-only state lookup");
Equal(false, processorIdentityAdapter.Contains("AllInstance", StringComparison.Ordinal), "authoritative processor identity cannot invoke private state access");
string pointerJoinAdapter = ExtractMethods(adapterSource, "internal static bool TryMatchRegularSaveSlot(").Single();
Equal(true, pointerJoinAdapter.Contains("PublicInstance", StringComparison.Ordinal), "regular-save pointer join uses public-only APIs");
Equal(false, pointerJoinAdapter.Contains("AllInstance", StringComparison.Ordinal) || pointerJoinAdapter.Contains("AllStatic", StringComparison.Ordinal), "regular-save pointer join cannot bind non-public members");
string persistAdapter = ExtractMethods(adapterSource, "internal static bool TrySubmitDefaultUrgentPersist(").Single();
Equal(true, persistAdapter.Contains("PublicStatic", StringComparison.Ordinal) && persistAdapter.Contains("PublicInstance", StringComparison.Ordinal), "disk persist uses public-only request construction and routing");
Equal(false, persistAdapter.Contains("AllStatic", StringComparison.Ordinal) || persistAdapter.Contains("AllInstance", StringComparison.Ordinal), "disk persist never resolves private APIs");
Equal(true, persistAdapter.Contains("Convert.ToInt32(bundle) != 1", StringComparison.Ordinal) && persistAdapter.Contains("Convert.ToInt32(urgent) != 0", StringComparison.Ordinal), "disk persist validates exact DEFAULT=1/URGENT=0 semantics");
foreach (string prohibited in new[] { "PersistAllChangesInBundle", "RequestWriteForPlayerSave", "SaveDataManager", "WritePlayerSaveFile", "SelectedPlayerSaveSlotChangedEvent", "PersistAllSaveChangeBundlesRequest", "TriggerUrgentSaveWriteIfAnyChangesRequest" })
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
    public TestSong Song { get; set; }
    public TestCassetteStatus CassetteStatus { get; set; }
    public TestBundle Bundle { get; set; }
    public bool SemanticConstructorUsed { get; }

    private TestCassetteRequest() { }

    public TestCassetteRequest(TestSong song, TestCassetteStatus cassetteStatus, TestBundle bundle)
    {
        Song = song;
        CassetteStatus = cassetteStatus;
        Bundle = bundle;
        SemanticConstructorUsed = true;
    }
}

static class PlayerSaveManagementEnquiries
{
    public static FakeIl2CppNullable<int>? SelectedSlot { get; set; }
    public static object? SelectedState { get; set; }
    public static FakeIl2CppNullable<int>? GetSelectedSaveFileSlotNumber() => SelectedSlot;
    public static object? TryGetSelectedSlotSaveFileState() => SelectedState;
}

sealed class FakeIl2CppNullable<T>
{
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
    public bool HasChanges => true;
    public bool RequiresWriteToDisk => false;
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
enum ePlayableSong { INVALID, QUIERES_BAILAR, I_GOT_MONEY, BADASS }
enum eSongCassetteStatus { INVALID, HAVE_IN_BAG }

sealed class PersistSaveChangeBundleRequest
{
    public PersistSaveChangeBundleRequest(FakeIl2CppNullable<ePlayerSaveChangeBundleKey> bundle, eSaveFileWriteType writeType)
    { Bundle = bundle; WriteTypeToRequest = writeType; }
    public FakeIl2CppNullable<ePlayerSaveChangeBundleKey> Bundle { get; }
    public eSaveFileWriteType WriteTypeToRequest { get; }
}

sealed class PersistAllSaveChangeBundlesRequest { }

static class RequestSystem
{
    public static int SubmitCount { get; private set; }
    public static PersistSaveChangeBundleRequest? LastRequest { get; private set; }
    public static void SubmitRequest<T>(T request)
    {
        SubmitCount++;
        LastRequest = request as PersistSaveChangeBundleRequest;
    }
    public static void Reset() { SubmitCount = 0; LastRequest = null; }
}

sealed class PlayerSaveRequestProcessor
{
    private readonly TransactionSaveState _state;
    public int ObtainStateCalls { get; private set; }
    public int ProcessRequestCalls { get; private set; }
    public RecordSongCassetteStatusInSaveDataRequest? LastRequest { get; private set; }

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
    public ePlayableSong Song { get; }
    public eSongCassetteStatus CassetteStatus { get; }
    public ePlayerSaveChangeBundleKey Bundle { get; }
    public bool SemanticConstructorUsed { get; }

    public RecordSongCassetteStatusInSaveDataRequest(ePlayableSong song, eSongCassetteStatus cassetteStatus, ePlayerSaveChangeBundleKey bundle)
    {
        Song = song;
        CassetteStatus = cassetteStatus;
        Bundle = bundle;
        SemanticConstructorUsed = true;
    }
}
