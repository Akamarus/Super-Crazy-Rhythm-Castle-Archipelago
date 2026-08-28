using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

static PlantPipesDecision Decide(
    bool compatible = true,
    bool synchronized = true,
    int received = 0,
    bool save = true,
    bool processor = true,
    bool nativeOwned = false,
    bool attemptOutstanding = false,
    int retryCount = 0) =>
    PlantPipesReconciler.Decide(new PlantPipesSnapshot(
        compatible,
        synchronized,
        received,
        save,
        processor,
        nativeOwned,
        attemptOutstanding,
        retryCount));

static string MethodBody(string source, string signature, string nextSignature)
{
    int start = source.IndexOf(signature, StringComparison.Ordinal);
    int end = source.IndexOf(nextSignature, start + signature.Length, StringComparison.Ordinal);
    if (start < 0 || end < 0)
        throw new InvalidOperationException($"Could not locate source boundaries for {signature}.");
    return source[start..end];
}

Equal(PlantPipesDecision.Ignore, Decide(received: 0), "not owned");
Equal(PlantPipesDecision.Ignore, Decide(compatible: false, received: 1), "incompatible seed");
Equal(PlantPipesDecision.Ignore, Decide(synchronized: false, received: 1), "history not synchronized");
Equal(PlantPipesDecision.WaitForSave, Decide(received: 1, save: false), "history before save");
Equal(PlantPipesDecision.WaitForProcessor, Decide(received: 1, processor: false), "save before processor");
Equal(PlantPipesDecision.Apply, Decide(received: 1), "grant missing ability");
Equal(PlantPipesDecision.Verify, Decide(received: 1, attemptOutstanding: true), "verify submitted grant");
Equal(PlantPipesDecision.Satisfied, Decide(received: 2, nativeOwned: true), "duplicate history idempotent");

Equal<TimeSpan?>(TimeSpan.FromMilliseconds(250), PlantPipesReconciler.NextRetryDelay(0), "first retry");
Equal<TimeSpan?>(TimeSpan.FromMilliseconds(500), PlantPipesReconciler.NextRetryDelay(1), "second retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(1), PlantPipesReconciler.NextRetryDelay(2), "third retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(2), PlantPipesReconciler.NextRetryDelay(3), "fourth retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(4), PlantPipesReconciler.NextRetryDelay(4), "last retry");
Equal<TimeSpan?>(null, PlantPipesReconciler.NextRetryDelay(5), "retry is bounded");
Equal<TimeSpan?>(null, PlantPipesReconciler.NextRetryDelay(-1), "negative retry rejected");

string fullPluginSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));
Equal(false, fullPluginSource.Contains("patched += PatchPlayerSaveProcessorCapture();", StringComparison.Ordinal),
    "startup must not install the broad temporary save-processor diagnostic hook");
string pluginSource = fullPluginSource;
int plantPipesClass = pluginSource.IndexOf("internal static class PlantPipesRandomization", StringComparison.Ordinal);
if (plantPipesClass < 0)
    throw new InvalidOperationException("Could not locate PlantPipesRandomization.");
pluginSource = pluginSource[plantPipesClass..];
string slotCallback = MethodBody(
    pluginSource,
    "public static void ApplySlotData(Dictionary<string, object>? slotData)",
    "public static bool TryApplyItem(string itemName)");
string itemCallback = MethodBody(
    pluginSource,
    "public static bool TryApplyItem(string itemName)",
    "public static void CapturePlayerSaveRequestProcessor(object? instance)");
string flushMethod = MethodBody(
    pluginSource,
    "public static void TryFlushPendingNativeGrant()",
    "internal static void TickPending(TimeSpan elapsed)");
Equal(false, slotCallback.Contains("TryFlushPendingNativeGrant", StringComparison.Ordinal),
    "slot-data callback must not invoke native save APIs off the Unity thread");
Equal(false, itemCallback.Contains("TryFlushPendingNativeGrant", StringComparison.Ordinal),
    "item callback must not invoke native save APIs off the Unity thread");
Equal(true, flushMethod.Contains("if (_applyingNativeGrant)", StringComparison.Ordinal),
    "native grant reconciliation must reject recursive progression-hook entry");

var adapter = new FakePlantPipesNativeAdapter();
var runtime = new PlantPipesRuntime(adapter);
runtime.Configure(synchronized: true, compatible: true);
runtime.NoteReceivedCount(1);

runtime.OnLifecyclePoint("history replay");
Equal("save-unavailable", runtime.LastOutcome, "history waits for selected save");

adapter.SaveAvailable = true;
runtime.OnLifecyclePoint("save available");
Equal("processor-unavailable", runtime.LastOutcome, "selected save waits for processor");

adapter.ProcessorAvailable = true;
runtime.OnLifecyclePoint("processor captured");
Equal("grant-submitted", runtime.LastOutcome, "missing ability submits grant");
Equal(1, adapter.ApplyCount, "grant submitted once");

runtime.OnLifecyclePoint("scene entry");
Equal("verified-owned", runtime.LastOutcome, "native state verifies ownership");
Equal(1, adapter.ApplyCount, "verified ownership is idempotent");

runtime.NoteReceivedCount(2);
runtime.OnLifecyclePoint("duplicate history");
Equal("verified-owned", runtime.LastOutcome, "duplicate history remains idempotent");
Equal(1, adapter.ApplyCount, "duplicate item does not grant twice");

var delayedAdapter = new FakePlantPipesNativeAdapter { SaveAvailable = true, ProcessorAvailable = true, PersistOnApply = false };
var delayedRuntime = new PlantPipesRuntime(delayedAdapter);
delayedRuntime.Configure(synchronized: true, compatible: true);
delayedRuntime.NoteReceivedCount(1);
delayedRuntime.OnLifecyclePoint("first attempt");
Equal("grant-submitted", delayedRuntime.LastOutcome, "first delayed grant submitted");
delayedRuntime.OnLifecyclePoint("verification");
Equal("verification-pending", delayedRuntime.LastOutcome, "failed verification remains pending");
Equal(true, delayedRuntime.HasPendingRetry, "failed verification schedules bounded retry");
delayedRuntime.TickPending(TimeSpan.FromMilliseconds(249), "bounded retry");
Equal(1, delayedAdapter.ApplyCount, "retry does not fire early");
delayedRuntime.TickPending(TimeSpan.FromMilliseconds(1), "bounded retry");
Equal(2, delayedAdapter.ApplyCount, "retry resubmits after first delay");
Equal("grant-submitted", delayedRuntime.LastOutcome, "retry reports resubmission");

var completedLevel4Adapter = new FakePlantPipesNativeAdapter
{
    SaveAvailable = true,
    ProcessorAvailable = true,
    NativeOwned = true,
};
var completedLevel4Runtime = new PlantPipesRuntime(completedLevel4Adapter);
completedLevel4Runtime.Configure(synchronized: true, compatible: true);
completedLevel4Runtime.NoteReceivedCount(1);
completedLevel4Runtime.OnLifecyclePoint("pre-result verified");
Equal("verified-owned", completedLevel4Runtime.LastOutcome, "pre-result ownership verified");
completedLevel4Adapter.NativeOwned = false;
completedLevel4Runtime.OnLifecyclePoint("level-result-applied:Level_08");
Equal("grant-submitted", completedLevel4Runtime.LastOutcome, "completed Level 4 clear is repaired");
completedLevel4Runtime.OnLifecyclePoint("level-result-persisted:Level_08");
Equal("verified-owned", completedLevel4Runtime.LastOutcome, "persisted Level 4 verifies repair");
Equal(1, completedLevel4Adapter.ApplyCount, "result lifecycle submits one restorative grant");

Equal(true, fullPluginSource.Contains("PlantPipesRandomization.OnLevelResultApplied(level)", StringComparison.Ordinal),
    "result application must trigger Plant Pipes reconciliation");
Equal(true, fullPluginSource.Contains("PlantPipesRandomization.OnLevelResultPersisted(level)", StringComparison.Ordinal),
    "result persistence must trigger Plant Pipes verification");

Console.WriteLine("Plant Pipes reconciler policy tests passed.");

internal sealed class FakePlantPipesNativeAdapter : IPlantPipesNativeAdapter
{
    public bool SaveAvailable { get; set; }
    public bool ProcessorAvailable { get; set; }
    public bool NativeOwned { get; set; }
    public bool PersistOnApply { get; set; } = true;
    public int ApplyCount { get; private set; }

    public bool TryReadOwned(out bool owned)
    {
        owned = NativeOwned;
        return SaveAvailable;
    }

    public bool TryApply(out string detail)
    {
        if (!ProcessorAvailable)
        {
            detail = "processor unavailable";
            return false;
        }

        ApplyCount++;
        if (PersistOnApply)
            NativeOwned = true;
        detail = "fake grant submitted";
        return true;
    }
}
