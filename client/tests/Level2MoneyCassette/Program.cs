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

var sourceDecision = Level2MoneyCassettePolicy.DecideSourceAward(
    "Level_06",
    "LevelVariant_Default",
    wasCollected: true);
Equal(true,
    sourceDecision.QueueLocation,
    "first default Level 2 cassette award queues the AP source location");
Equal(false,
    sourceDecision.ReplacementWasCollected,
    "first default Level 2 cassette award suppresses the native cassette");

var excludedSourceDecisions = new[]
{
    (Decision: Level2MoneyCassettePolicy.DecideSourceAward(
        "Level_06", "LevelVariant_Default", wasCollected: false), OriginalWasCollected: false,
        Scenario: "replayed Level 2 result"),
    (Decision: Level2MoneyCassettePolicy.DecideSourceAward(
        "Level_06", "LevelVariant_BeeMode", wasCollected: true), OriginalWasCollected: true,
        Scenario: "Bee Mode cassette award"),
    (Decision: Level2MoneyCassettePolicy.DecideSourceAward(
        "Level_05", "LevelVariant_Default", wasCollected: true), OriginalWasCollected: true,
        Scenario: "unrelated level cassette award"),
};

foreach (var excluded in excludedSourceDecisions)
{
    Equal(false,
        excluded.Decision.QueueLocation,
        $"{excluded.Scenario} does not queue the AP source location");
    Equal(excluded.OriginalWasCollected,
        excluded.Decision.ReplacementWasCollected,
        $"{excluded.Scenario} preserves the original WasCollected value");
}

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

var selectedSaveRuntime = new Level2MoneyCassetteRuntime();
selectedSaveRuntime.Configure(enabled: true);
selectedSaveRuntime.NoteReceivedCount(1);
selectedSaveRuntime.OnSaveLifecyclePoint();
Equal(true,
    selectedSaveRuntime.TryBeginReconcileAttempt(),
    "first selected save opens a bounded reconciliation window");
Equal(Level2MoneyCassetteReconcileDecision.AlreadyOwned,
    selectedSaveRuntime.ObserveNativeStatus(
        saveAvailable: true,
        processorAvailable: true,
        Level2MoneyCassettePolicy.HaveInBag),
    "owned first selected save closes only its current reconciliation window");
Equal(false,
    selectedSaveRuntime.TryBeginReconcileAttempt(),
    "verified ownership pauses retries for the current selected save");

selectedSaveRuntime.OnSaveLifecyclePoint();
Equal(true,
    selectedSaveRuntime.TryBeginReconcileAttempt(),
    "selecting or creating another save reopens reconciliation for persistent AP ownership");
Equal(Level2MoneyCassetteReconcileDecision.RequestHaveInBag,
    selectedSaveRuntime.ObserveNativeStatus(
        saveAvailable: true,
        processorAvailable: true,
        Level2MoneyCassettePolicy.HaveNotEarned),
    "unowned later selected save receives the persistent AP cassette");

var exhaustedRuntime = new Level2MoneyCassetteRuntime();
exhaustedRuntime.Configure(enabled: true);
exhaustedRuntime.NoteReceivedCount(1);
exhaustedRuntime.OnSaveLifecyclePoint();
for (int attempt = 0; attempt < Level2MoneyCassetteRuntime.MaxRetryAttempts; attempt++)
{
    Equal(true,
        exhaustedRuntime.TryBeginReconcileAttempt(),
        $"bounded retry attempt {attempt + 1} is available");
}
Equal(false,
    exhaustedRuntime.TryBeginReconcileAttempt(),
    "retry window pauses after the bounded attempt limit");

exhaustedRuntime.OnSaveLifecyclePoint();
Equal(true,
    exhaustedRuntime.TryBeginReconcileAttempt(),
    "later save activity with the same processor starts a fresh bounded retry window");
Equal(Level2MoneyCassetteReconcileDecision.RequestHaveInBag,
    exhaustedRuntime.ObserveNativeStatus(
        saveAvailable: true,
        processorAvailable: true,
        Level2MoneyCassettePolicy.HaveNotEarned),
    "same-processor lifecycle recovery grants after earlier save-unavailable exhaustion");

Console.WriteLine("Level 2 Money Cassette policy tests passed.");
