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
foreach (string diagnosticRequest in new[]
{
    "SelectPlayerSaveSlotRequest",
    "SelectMostRecentlyUsedRegularPlayerSaveSlotRequest",
    "EnsureAPlayerSaveSlotIsSelectedRequest",
    "EnsurePlayerSaveFileExistsInSelectedSlotRequest",
})
    Equal(true, pluginSource.Contains($"PatchExactMethod(\"SaveDataRequestProcessor\", \"ProcessRequest\", \"{diagnosticRequest}\", nameof(CassetteSaveTransactionPatches.PublicSelectionDiagnosticPostfix))", StringComparison.Ordinal), $"diagnostic-only exact hook installed for {diagnosticRequest}");
Equal(true, pluginSource.Contains("PatchExactMethod(\"SaveDataState\", \"set_SelectedPlayerSaveSlot\", \"Nullable`1\", nameof(CassetteSaveTransactionPatches.SelectedSlotSetterDiagnosticPostfix))", StringComparison.Ordinal), "diagnostic-only exact selected-slot setter hook installed");

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
string publicSelectionDiagnostic = ExtractMethods(pluginSource, "public static void PublicSelectionDiagnosticPostfix(").Single();
Equal(true, publicSelectionDiagnostic.Contains("SelectPlayerSaveSlotRequest", StringComparison.Ordinal), "public selection diagnostic recognizes explicit slot request");
Equal(true, publicSelectionDiagnostic.Contains("EnsureAPlayerSaveSlotIsSelectedRequest", StringComparison.Ordinal), "public selection diagnostic recognizes documented default slot request");
Equal(true, publicSelectionDiagnostic.Contains("LogPublicSelectionDiagnosticOnce", StringComparison.Ordinal), "public selection diagnostics are bounded by identity/value");
foreach (string prohibitedCall in new[] { "QueueSaveBoundarySignal", "ActivateLoadedSave", "DeactivateLoadedSave", "TryGetLoadedSave", "TryReconcile", "TrySubmitHaveInBag" })
    Equal(false, publicSelectionDiagnostic.Contains(prohibitedCall, StringComparison.Ordinal), $"public selection diagnostic forbids semantic call {prohibitedCall}");
Equal(true, publicSelectionDiagnostic.Contains("QueueMostRecentSelectionIdentityDiagnostic", StringComparison.Ordinal), "most-recent postfix queues managed identity probe");
string queueMostRecentProbe = ExtractMethods(pluginSource, "internal static void QueueMostRecentSelectionIdentityDiagnostic(").Single();
Equal(true, queueMostRecentProbe.Contains("_mostRecentIdentityProbe.Queue", StringComparison.Ordinal), "most-recent probe queue mutates pure managed state");
foreach (string prohibitedCall in new[] { "_saveIdentity.Signal", "ActivateLoadedSave", "DeactivateLoadedSave", "TryGetLoadedSave", "TryReconcile", "TrySubmitHaveInBag" })
    Equal(false, queueMostRecentProbe.Contains(prohibitedCall, StringComparison.Ordinal), $"most-recent probe queue forbids semantic call {prohibitedCall}");
string selectedSlotSetterDiagnostic = ExtractMethods(pluginSource, "public static void SelectedSlotSetterDiagnosticPostfix(").Single();
Equal(true, selectedSlotSetterDiagnostic.Contains("ReflectionUtil.UnwrapNullable", StringComparison.Ordinal), "selected-slot setter safely unwraps generated nullable value");
Equal(true, selectedSlotSetterDiagnostic.Contains("LogPublicSelectionDiagnosticOnce", StringComparison.Ordinal), "selected-slot setter logging is bounded");
foreach (string prohibitedCall in new[] { "QueueSaveBoundarySignal", "ActivateLoadedSave", "DeactivateLoadedSave", "TryGetLoadedSave", "TryReconcile", "TrySubmitHaveInBag" })
    Equal(false, selectedSlotSetterDiagnostic.Contains(prohibitedCall, StringComparison.Ordinal), $"selected-slot setter diagnostic forbids semantic call {prohibitedCall}");
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
Equal(true, unityTick.Contains("TryReconcile(reason)", StringComparison.Ordinal), "Unity keeper drains queued reconciliation intent");
Equal(true, unityTick.Contains("TryGetLoadedSaveIdentity", StringComparison.Ordinal), "Unity keeper observes selected slot and native public-state pointer");
Equal(true, unityTick.Contains("ObserveDetailed", StringComparison.Ordinal), "Unity keeper feeds identity into diagnostic stabilizer observation");
Equal(true, unityTick.Contains("LogIdentityDiagnosticOnChange", StringComparison.Ordinal), "Unity keeper logs identity stabilization only on change");
Equal(true, unityTick.Contains("expectedSlot", StringComparison.Ordinal) && unityTick.Contains("selectedSlot", StringComparison.Ordinal) && unityTick.Contains("selectedStatePointer", StringComparison.Ordinal), "Unity diagnostic includes expected slot, actual slot, and pointer");
Equal(true, unityTick.Contains("_mostRecentIdentityProbe.Pending", StringComparison.Ordinal) && unityTick.Contains("_mostRecentIdentityProbe.Observe", StringComparison.Ordinal), "Unity keeper consumes bounded most-recent probe");
Equal(true, unityTick.Contains("TryGetLoadedSaveFingerprint", StringComparison.Ordinal), "bounded Unity probe reads fixed public-state fingerprint");
Equal(true, unityTick.Contains("TryGetProcessorSaveFingerprint", StringComparison.Ordinal), "bounded Unity probe reads processor-local public-state fingerprint");
foreach (string prohibitedCall in new[] { "_saveIdentity.Signal", "TrySubmitHaveInBag" })
{
    string probeBlock = unityTick[..unityTick.IndexOf("CassetteSaveActivation? activation", StringComparison.Ordinal)];
    Equal(false, probeBlock.Contains(prohibitedCall, StringComparison.Ordinal), $"diagnostic probe block forbids semantic call {prohibitedCall}");
}
string queueBoundary = ExtractMethods(receiptRandomizationSource, "internal static void QueueSaveBoundarySignal(").Single();
Equal(true, queueBoundary.Contains("_saveIdentity.Signal", StringComparison.Ordinal), "exact boundary callback records managed signal");
Equal(true, queueBoundary.Contains("_unityReconciliationRequested = false", StringComparison.Ordinal), "exact boundary callback suspends prior reconciliation immediately");
Equal(true, queueBoundary.Contains("_processorIdentityProbe.SignalSelection", StringComparison.Ordinal), "exact selection invalidates old processor probe generation");
string captureProcessor = ExtractMethods(receiptRandomizationSource, "internal static void CapturePlayerSaveRequestProcessor(").Single();
Equal(true, captureProcessor.Contains("_processorIdentityProbe.CaptureAfterSelection", StringComparison.Ordinal), "only a post-selection processor callback arms processor probe");
Equal(false, queueBoundary.Contains("TryGetLoadedSave", StringComparison.Ordinal), "boundary callback performs no native selected-save read");
string keeperSource = ExtractClass(pluginSource, "CassetteReceiptReconciliationKeeper");
Equal(true, keeperSource.Contains("Stopwatch.GetTimestamp()", StringComparison.Ordinal), "keeper uses a monotonic production clock");
Equal(true, keeperSource.Contains("CassetteReceiptRandomization.TickUnity(elapsed)", StringComparison.Ordinal), "keeper passes actual elapsed time every Unity update");
Equal(false, keeperSource.Contains("TimeSpan.FromSeconds(1)", StringComparison.Ordinal), "keeper does not substitute a frame-count interval for elapsed time");
foreach (string obsolete in new[] { "CassetteReceiptRuntime", "CassetteReceiptScheduler", "Level2MoneyCassetteRuntime", "_terminalSongs" })
    Equal(false, pluginSource.Contains(obsolete, StringComparison.Ordinal) || File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "CassetteRandomizationPolicy.cs")).Contains(obsolete, StringComparison.Ordinal), $"obsolete process-wide cassette state removed: {obsolete}");
Equal(false, pluginSource.Contains("PatchMethodsByParameter(\n                \"HandleEvent\",\n                \"SelectedPlayerSaveSlotChangedEvent\"", StringComparison.Ordinal), "unsafe selected-save event hook remains absent");
Console.WriteLine("PASS: safe_save_lifecycle_production_wiring");

var stabilizer = new CassetteSaveIdentityStabilizer();
Equal(false, stabilizer.BoundaryPending, "no signal starts inactive");
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 400), "room observation without exact signal cannot activate");
stabilizer.Signal(4, CassetteSaveBoundarySignalKind.Creation);
Equal(true, stabilizer.BoundaryPending, "exact creation signal suspends old epoch immediately");
Equal(4, stabilizer.ExpectedSlot, "pending diagnostic exposes expected slot");
Equal(CassetteSaveIdentityObservationKind.Mismatch, stabilizer.ObserveDetailed(0, 100).Kind, "diagnostic classifies slot mismatch");
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(0, 100), "startup slot cannot satisfy slot-4 signal");
Equal(CassetteSaveIdentityObservationKind.FirstStable, stabilizer.ObserveDetailed(4, 400).Kind, "diagnostic classifies first stable observation");
var detailedActivation = stabilizer.ObserveDetailed(4, 400);
Equal(CassetteSaveIdentityObservationKind.Activated, detailedActivation.Kind, "diagnostic classifies second observation activation");
var stableActivation = detailedActivation.Activation;
Equal(4, stableActivation!.Value.Slot, "slot 4 activates after two matching observations");
Equal(400L, stableActivation.Value.Pointer, "activation records native pointer");
Equal(false, stabilizer.BoundaryPending, "activation resumes epoch work");

stabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection);
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 400), "first observation precedes newer signal");
stabilizer.Signal(4, CassetteSaveBoundarySignalKind.Build);
Equal(true, stabilizer.BoundaryPending, "same-slot build keeps boundary suspended");
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 400), "new same-slot signal resets stability");
stableActivation = stabilizer.Observe(4, 400);
Equal(true, stableActivation!.Value.IncludesBuild, "coalesced generation preserves build");
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 400), "coalesced generation activates at most once");
stabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection);
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 400), "duplicate selection first observation");
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 400), "duplicate selection does not create another epoch");
Equal(false, stabilizer.BoundaryPending, "stable duplicate selection resumes existing epoch without duplicating it");

stabilizer.Signal(4, CassetteSaveBoundarySignalKind.Selection);
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 401), "new pointer first observation");
stableActivation = stabilizer.Observe(4, 401);
Equal(401L, stableActivation!.Value.Pointer, "pointer replacement activates once");
stabilizer.Signal(5, CassetteSaveBoundarySignalKind.Selection);
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(4, 401), "mismatched slot cannot activate");
Equal<CassetteSaveActivation?>(null, stabilizer.Observe(5, 500), "slot switch first observation");
stableActivation = stabilizer.Observe(5, 500);
Equal(5, stableActivation!.Value.Slot, "slot switch activates");
stabilizer.Reset();
Equal(false, stabilizer.BoundaryPending, "reset clears pending boundary");
Console.WriteLine("PASS: final_save_identity_stabilization");

var probe = new CassetteMostRecentIdentityProbe();
Equal(false, probe.Pending, "most-recent diagnostic starts inactive");
probe.Queue();
Equal(true, probe.Pending, "most-recent diagnostic queues managed probe");
var probeResult = probe.Observe(readable: true, slot: 4, pointer: 400, stage: "success");
Equal(CassetteMostRecentIdentityProbeKind.FirstObservation, probeResult.Kind, "first identity observation is diagnostic only");
Equal(true, probe.Pending, "probe waits for one confirming observation");
probeResult = probe.Observe(readable: true, slot: 4, pointer: 400, stage: "success");
Equal(CassetteMostRecentIdentityProbeKind.Stable, probeResult.Kind, "second matching identity is reported stable");
Equal(false, probe.Pending, "stable probe clears after two observations");
Equal<CassetteSaveActivation?>(null, probeResult.Activation, "diagnostic probe cannot produce epoch activation");
probe.Queue();
probeResult = probe.Observe(readable: false, slot: 0, pointer: 0, stage: "state-null");
Equal(CassetteMostRecentIdentityProbeKind.ReadFailure, probeResult.Kind, "read failure reports bounded stage");
Equal(false, probe.Pending, "failed probe clears without retry loop");
Console.WriteLine("PASS: most_recent_identity_probe_is_bounded_and_non_authoritative");

var processorProbe = new CassetteProcessorIdentityProbe();
Equal(false, processorProbe.CaptureAfterSelection(), "pre-selection processor capture cannot arm probe");
processorProbe.SignalSelection();
Equal(true, processorProbe.WaitingForCapture, "selection waits for a later processor capture");
Equal(false, processorProbe.Pending, "selection alone does not read processor state");
Equal(true, processorProbe.CaptureAfterSelection(), "post-selection processor capture arms probe");
Equal(true, processorProbe.Pending, "post-selection capture queues bounded Unity probe");
long processorProbeGeneration = processorProbe.Generation;
var processorProbeResult = processorProbe.Observe(processorProbeGeneration, readable: true, pointer: 300, stage: "success");
Equal(CassetteProcessorIdentityProbeKind.FirstObservation, processorProbeResult.Kind, "processor probe requires confirmation");
processorProbeResult = processorProbe.Observe(processorProbeGeneration, readable: true, pointer: 300, stage: "success");
Equal(CassetteProcessorIdentityProbeKind.Stable, processorProbeResult.Kind, "processor probe reports stable second observation");
Equal(false, processorProbe.Pending, "processor probe clears after two observations");
Equal<CassetteSaveActivation?>(null, processorProbeResult.Activation, "processor diagnostic cannot activate epoch");
processorProbe.SignalSelection();
processorProbe.CaptureAfterSelection();
long staleProcessorProbeGeneration = processorProbe.Generation;
processorProbe.SignalSelection();
Equal(false, processorProbe.Pending, "new selection invalidates prior generation capture");
Equal(true, processorProbe.WaitingForCapture, "new selection requires a new processor callback");
processorProbe.CaptureAfterSelection();
processorProbeResult = processorProbe.Observe(staleProcessorProbeGeneration, readable: true, pointer: 999, stage: "success");
Equal(CassetteProcessorIdentityProbeKind.None, processorProbeResult.Kind, "stale generation observation cannot affect current probe");
Equal(true, processorProbe.Pending, "stale generation observation leaves current probe pending");
Console.WriteLine("PASS: processor_identity_probe_requires_post_selection_capture");

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
Equal(true, CassetteSaveTransactionAdapter.TryGetLoadedSaveFingerprint(out CassetteSaveFingerprint fingerprint, out string fingerprintStage), "diagnostic fingerprint reads fixed public fields");
Equal("success", fingerprintStage, "fingerprint success stage is bounded");
Equal(nameof(FakePlayerSavePublicState), fingerprint.StateType, "fingerprint records safe runtime type identity");
Equal(1234L, fingerprint.PlayTimeInSeconds, "fingerprint records play time");
Equal(new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc).Ticks, fingerprint.LastPlayDateTimeUtcTicks, "fingerprint records last-play ticks");
Equal(nameof(eSongCassetteStatus.HAVE_IN_BAG), fingerprint.IGotMoneyStatus, "fingerprint records I Got Money cassette status");
Equal(nameof(eSongCassetteStatus.INVALID), fingerprint.BadassStatus, "fingerprint records Badass cassette status");
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
Equal(false, CassetteSaveTransactionAdapter.TryGetLoadedSaveFingerprint(out _, out fingerprintStage), "missing fingerprint fields fail closed");
Equal("fingerprint-game-stats-missing", fingerprintStage, "fingerprint failure names exact missing field stage");
PlayerSaveManagementEnquiries.SelectedState = new PrivateFingerprintState();
Equal(false, CassetteSaveTransactionAdapter.TryGetLoadedSaveFingerprint(out _, out fingerprintStage), "fingerprint rejects private replacement members");
Equal("fingerprint-game-stats-missing", fingerprintStage, "private GameStats is not treated as public metadata");
PlayerSaveManagementEnquiries.SelectedState = new PrivateNullable<FakePlayerSavePublicState>(new FakePlayerSavePublicState(new IntPtr(0x400)));
Equal(false, CassetteSaveTransactionAdapter.TryGetLoadedSaveFingerprint(out _, out fingerprintStage), "fingerprint rejects nullable-shaped wrappers with private members");
Equal("fingerprint-state-null", fingerprintStage, "private nullable members fail closed before fingerprint traversal");
PlayerSaveManagementEnquiries.SelectedState = new PrivateStatusFingerprintState();
Equal(false, CassetteSaveTransactionAdapter.TryGetLoadedSaveFingerprint(out _, out fingerprintStage), "fingerprint rejects private nullable cassette status members");
Equal("fingerprint-I_GOT_MONEY-status-empty", fingerprintStage, "private cassette nullable members fail closed");
PlayerSaveManagementEnquiries.SelectedState = new FakePlayerSavePublicState(IntPtr.Zero);
Equal(false, CassetteSaveTransactionAdapter.TryGetLoadedSaveIdentity(out _, out _, out identityStage), "zero pointer fails closed");
Equal("pointer-zero", identityStage, "zero pointer has exact stage");

var transactionState = new TransactionSaveState(eSongCassetteStatus.HAVE_IN_BAG);
var transactionProcessor = new PlayerSaveRequestProcessor(transactionState);
Equal(true, CassetteSaveTransactionAdapter.TryReadCassetteStatus(transactionProcessor, nameof(ePlayableSong.QUIERES_BAILAR), out string? transactionStatus), "transaction adapter reads current processor state");
Equal(nameof(eSongCassetteStatus.HAVE_IN_BAG), transactionStatus, "transaction adapter returns authoritative cassette status");
Equal(1, transactionProcessor.ObtainStateCalls, "transaction read obtains current state for each call");
Equal(true, CassetteSaveTransactionAdapter.TryGetProcessorSaveFingerprint(transactionProcessor, out long processorPointer, out CassetteSaveFingerprint processorFingerprint, out string processorFingerprintStage), "processor-local fingerprint reads captured processor state");
Equal("success", processorFingerprintStage, "processor-local fingerprint success stage");
Equal(0x700L, processorPointer, "processor-local fingerprint reads public pointer");
Equal(777L, processorFingerprint.PlayTimeInSeconds, "processor-local fingerprint reads selected processor playtime");
Equal(false, CassetteSaveTransactionAdapter.TryGetProcessorSaveFingerprint(new PrivateOnlyFixture.PlayerSaveRequestProcessor(), out _, out _, out processorFingerprintStage), "processor fingerprint rejects private-only ObtainState");
Equal("processor-fingerprint-obtain-state-missing", processorFingerprintStage, "private ObtainState fails closed");

var semanticProcessor = new PlayerSaveRequestProcessor(new TransactionSaveState(eSongCassetteStatus.INVALID));
Equal(true, CassetteSaveTransactionAdapter.IsCompatiblePlayerSaveRequestProcessor(semanticProcessor), "exact player processor is compatible");
Equal(true, CassetteSaveTransactionAdapter.TrySubmitHaveInBag(semanticProcessor, nameof(ePlayableSong.QUIERES_BAILAR), out string submitDetail), "adapter submits semantic cassette request");
Equal(1, semanticProcessor.ProcessRequestCalls, "submission processes exactly one semantic request");
Equal(ePlayableSong.QUIERES_BAILAR, semanticProcessor.LastRequest!.Song, "request receives native song");
Equal(eSongCassetteStatus.HAVE_IN_BAG, semanticProcessor.LastRequest.CassetteStatus, "request receives bag status");
Equal(ePlayerSaveChangeBundleKey.DEFAULT, semanticProcessor.LastRequest.Bundle, "request uses default native bundle");
Equal(true, semanticProcessor.LastRequest.SemanticConstructorUsed, "submission uses semantic cassette constructor");
Equal(true, submitDetail.Contains("DEFAULT", StringComparison.Ordinal), "submission detail identifies default bundle");

string adapterSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "CassetteSaveTransactionAdapter.cs"));
string fingerprintAdapter = ExtractMethods(adapterSource, "internal static bool TryGetLoadedSaveFingerprint(").Single();
string buildFingerprintAdapter = ExtractMethods(adapterSource, "private static bool TryBuildFingerprint(").Single();
Equal(true, fingerprintAdapter.Contains("PublicStatic", StringComparison.Ordinal) && buildFingerprintAdapter.Contains("PublicInstance", StringComparison.Ordinal), "fingerprint uses explicit public-only reflection flags");
Equal(false, fingerprintAdapter.Contains("AllStatic", StringComparison.Ordinal) || fingerprintAdapter.Contains("AllInstance", StringComparison.Ordinal), "fingerprint never binds non-public members");
Equal(false, buildFingerprintAdapter.Contains("AllStatic", StringComparison.Ordinal) || buildFingerprintAdapter.Contains("AllInstance", StringComparison.Ordinal), "shared fingerprint fields never bind non-public members");
string processorFingerprintAdapter = ExtractMethods(adapterSource, "internal static bool TryGetProcessorSaveFingerprint(").Single();
Equal(true, processorFingerprintAdapter.Contains("PublicInstance", StringComparison.Ordinal), "processor fingerprint uses public-only ObtainState lookup");
Equal(false, processorFingerprintAdapter.Contains("AllInstance", StringComparison.Ordinal), "processor fingerprint cannot invoke private ObtainState");
foreach (string prohibited in new[] { "PersistAllChangesInBundle", "RequestWriteForPlayerSave", "SaveDataManager", "WritePlayerSaveFile", "SelectedPlayerSaveSlotChangedEvent", "PersistSaveChangeBundleRequest", "PersistAllSaveChangeBundlesRequest" })
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

sealed class PrivateFingerprintState
{
    private FakeGameStats GameStats { get; } = new(1, DateTime.UnixEpoch);
    private eSongCassetteStatus GetCassetteStatusForSong(ePlayableSong song) => eSongCassetteStatus.INVALID;
}

sealed class PrivateNullable<T>
{
    private bool HasValue => true;
    private T Value { get; }
    public PrivateNullable(T value) => Value = value;
}

sealed class PrivateStatusFingerprintState
{
    public FakeGameStats GameStats { get; } = new(1, DateTime.UnixEpoch);
    public PrivateNullable<eSongCassetteStatus> GetCassetteStatusForSong(ePlayableSong song) =>
        new(eSongCassetteStatus.HAVE_IN_BAG);
}

enum ePlayerSaveChangeBundleKey { INVALID, DEFAULT, CAMPAIGN }
enum ePlayableSong { INVALID, QUIERES_BAILAR, I_GOT_MONEY, BADASS }
enum eSongCassetteStatus { INVALID, HAVE_IN_BAG }

sealed class PersistSaveChangeBundleRequest
{
    public FakeIl2CppNullable<ePlayerSaveChangeBundleKey>? Bundle { get; init; }
}

sealed class PersistAllSaveChangeBundlesRequest { }

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
    public TransactionSaveState(eSongCassetteStatus status)
    {
        _status = status;
        Pointer = new IntPtr(0x700);
        GameStats = new FakeGameStats(777, new DateTime(2026, 8, 31, 13, 0, 0, DateTimeKind.Utc));
    }
    public IntPtr Pointer { get; }
    public FakeGameStats GameStats { get; }
    public eSongCassetteStatus GetCassetteStatusForSong(ePlayableSong song) => _status;
}

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
