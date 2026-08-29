using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

Equal(true,
    Level2MoneyCassettePolicy.IsSourceAward("Level_06", "LevelVariant_Default", wasCollected: true),
    "first default Level 2 cassette award is the source");
Equal(false,
    Level2MoneyCassettePolicy.IsSourceAward("Level_06", "LevelVariant_Default", wasCollected: false),
    "replayed Level 2 result is not the source");
Equal(false,
    Level2MoneyCassettePolicy.IsSourceAward("Level_06", "LevelVariant_BeeMode", wasCollected: true),
    "Bee Mode cassette award is not the source");
Equal(false,
    Level2MoneyCassettePolicy.IsSourceAward("Level_05", "LevelVariant_Default", wasCollected: true),
    "unrelated level cassette award is not the source");
Equal(true,
    Level2MoneyCassettePolicy.IsSourceAward("level_06", "levelvariant_default", wasCollected: true),
    "source identifiers compare without case sensitivity");

var runtime = new Level2MoneyCassetteRuntime();
runtime.Configure(enabled: true);
Equal(Level2MoneyCassetteReconcileDecision.NoOwnership,
    runtime.ObserveNativeStatus(saveAvailable: true, processorAvailable: true, Level2MoneyCassettePolicy.HaveNotEarned),
    "zero received Money Cassettes never grant native ownership");

runtime.NoteReceivedCount(1);
Equal(Level2MoneyCassetteReconcileDecision.RequestHaveInBag,
    runtime.ObserveNativeStatus(saveAvailable: true, processorAvailable: true, Level2MoneyCassettePolicy.HaveNotEarned),
    "received Money Cassette requests bag ownership when not earned");
Equal(Level2MoneyCassettePolicy.HaveInBag,
    runtime.RequestedNativeStatus,
    "missing native ownership requests HAVE_IN_BAG");
Equal(Level2MoneyCassetteReconcileDecision.AlreadyOwned,
    runtime.ObserveNativeStatus(saveAvailable: true, processorAvailable: true, Level2MoneyCassettePolicy.HaveInBag),
    "bag ownership is complete without another request");
Equal(Level2MoneyCassetteReconcileDecision.AlreadyOwned,
    runtime.ObserveNativeStatus(saveAvailable: true, processorAvailable: true, Level2MoneyCassettePolicy.HaveDeposited),
    "deposited ownership is complete without another request");

var retryRuntime = new Level2MoneyCassetteRuntime();
retryRuntime.Configure(enabled: true);
retryRuntime.NoteReceivedCount(1);
Equal(Level2MoneyCassetteReconcileDecision.SaveUnavailable,
    retryRuntime.ObserveNativeStatus(saveAvailable: false, processorAvailable: false, nativeStatus: null),
    "unavailable save defers receipt reconciliation");
Equal(Level2MoneyCassetteReconcileDecision.ProcessorUnavailable,
    retryRuntime.ObserveNativeStatus(saveAvailable: true, processorAvailable: false, Level2MoneyCassettePolicy.HaveNotEarned),
    "unavailable processor defers receipt reconciliation");
Equal(Level2MoneyCassetteReconcileDecision.RequestHaveInBag,
    retryRuntime.ObserveNativeStatus(saveAvailable: true, processorAvailable: true, Level2MoneyCassettePolicy.HaveNotEarned),
    "retry retains receipt ownership after unavailable save and processor");

Console.WriteLine("Level 2 Money Cassette policy tests passed.");
