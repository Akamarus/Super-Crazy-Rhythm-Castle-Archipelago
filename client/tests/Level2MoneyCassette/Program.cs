using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

Equal(false,
    Level2MoneyCassettePolicy.UseSelectedSaveChangedEventHook,
    "selected-save HandleEvent hook stays disabled because it crashes IL2CPP startup");

Equal(true,
    Level2MoneyCassettePolicy.ShouldSuppressEvaluation(
        "Level_06", "LevelVariant_Default", succeeded: true, Level2MoneyCassettePolicy.Invalid),
    "successful default Level 2 with a fresh-save INVALID Money cassette becomes the AP source");
Equal(true,
    Level2MoneyCassettePolicy.ShouldSuppressEvaluation(
        "Level_06", "LevelVariant_Default", succeeded: true, Level2MoneyCassettePolicy.HaveNotEarned),
    "successful default Level 2 with an explicit unearned Money cassette becomes the AP source");
Equal(false,
    Level2MoneyCassettePolicy.ShouldSuppressEvaluation(
        "Level_06", "LevelVariant_Default", succeeded: true, Level2MoneyCassettePolicy.HaveInBag),
    "Level 2 replay with an owned Money cassette remains native");
Equal(false,
    Level2MoneyCassettePolicy.ShouldSuppressEvaluation(
        "Level_06", "LevelVariant_Default", succeeded: false, Level2MoneyCassettePolicy.HaveNotEarned),
    "failed Level 2 attempt never becomes the cassette source");
Equal(false,
    Level2MoneyCassettePolicy.ShouldSuppressEvaluation(
        "Level_05", "LevelVariant_Default", succeeded: true, Level2MoneyCassettePolicy.HaveNotEarned),
    "other level cassette evaluations remain native");

var runtime = new Level2MoneyCassetteRuntime();
runtime.Configure(enabled: true);
Equal(Level2MoneyCassetteReconcileDecision.NoOwnership,
    runtime.ObserveNativeStatus(saveAvailable: true, processorAvailable: true, Level2MoneyCassettePolicy.HaveNotEarned),
    "zero received Money Cassettes never grant native ownership");

runtime.NoteReceivedCount(1);
Equal(Level2MoneyCassetteReconcileDecision.RequestHaveInBag,
    runtime.ObserveNativeStatus(saveAvailable: true, processorAvailable: true, Level2MoneyCassettePolicy.Invalid),
    "received Money Cassette grants bag ownership from a fresh-save INVALID state");
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
