namespace RhythmCastleAP;

internal enum PlantPipesDecision
{
    Ignore,
    WaitForSave,
    WaitForProcessor,
    Apply,
    Verify,
    Satisfied,
}

internal readonly record struct PlantPipesSnapshot(
    bool Compatible,
    bool Synchronized,
    int ReceivedCount,
    bool SaveAvailable,
    bool ProcessorAvailable,
    bool NativeOwned,
    bool AttemptOutstanding,
    int RetryCount);

internal static class PlantPipesReconciler
{
    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
    };

    internal static PlantPipesDecision Decide(PlantPipesSnapshot snapshot)
    {
        if (!snapshot.Compatible || !snapshot.Synchronized || snapshot.ReceivedCount < 1)
            return PlantPipesDecision.Ignore;
        if (snapshot.NativeOwned)
            return PlantPipesDecision.Satisfied;
        if (!snapshot.SaveAvailable)
            return PlantPipesDecision.WaitForSave;
        if (!snapshot.ProcessorAvailable)
            return PlantPipesDecision.WaitForProcessor;
        return snapshot.AttemptOutstanding
            ? PlantPipesDecision.Verify
            : PlantPipesDecision.Apply;
    }

    internal static TimeSpan? NextRetryDelay(int retryCount) =>
        retryCount >= 0 && retryCount < RetryDelays.Length
            ? RetryDelays[retryCount]
            : null;
}

internal interface IPlantPipesNativeAdapter
{
    bool SaveAvailable { get; }
    bool ProcessorAvailable { get; }
    bool TryReadOwned(out bool owned);
    bool TryApply(out string detail);
}

internal sealed class PlantPipesRuntime
{
    private readonly IPlantPipesNativeAdapter _native;
    private bool _synchronized;
    private bool _compatible;
    private int _receivedCount;
    private bool _attemptOutstanding;
    private int _retryCount;
    private TimeSpan _retryElapsed;

    internal PlantPipesRuntime(IPlantPipesNativeAdapter native)
    {
        _native = native;
    }

    internal string LastOutcome { get; private set; } = "not-evaluated";

    internal bool HasPendingRetry =>
        _attemptOutstanding && PlantPipesReconciler.NextRetryDelay(_retryCount) != null;

    internal void Configure(bool synchronized, bool compatible)
    {
        _synchronized = synchronized;
        _compatible = compatible;
    }

    internal void NoteReceivedCount(int count)
    {
        if (count > _receivedCount)
            _receivedCount = count;
    }

    internal void OnLifecyclePoint(string reason)
    {
        if (!_native.SaveAvailable || !_native.TryReadOwned(out bool nativeOwned))
        {
            LastOutcome = "save-unavailable";
            return;
        }

        PlantPipesDecision decision = PlantPipesReconciler.Decide(new PlantPipesSnapshot(
            _compatible,
            _synchronized,
            _receivedCount,
            SaveAvailable: true,
            _native.ProcessorAvailable,
            nativeOwned,
            _attemptOutstanding,
            _retryCount));

        switch (decision)
        {
            case PlantPipesDecision.Ignore:
                LastOutcome = "ignored";
                return;
            case PlantPipesDecision.WaitForProcessor:
                LastOutcome = "processor-unavailable";
                return;
            case PlantPipesDecision.Satisfied:
                _attemptOutstanding = false;
                _retryCount = 0;
                _retryElapsed = TimeSpan.Zero;
                LastOutcome = "verified-owned";
                return;
            case PlantPipesDecision.Verify:
                LastOutcome = "verification-pending";
                return;
            case PlantPipesDecision.Apply:
                if (_native.TryApply(out _))
                {
                    _attemptOutstanding = true;
                    _retryElapsed = TimeSpan.Zero;
                    LastOutcome = "grant-submitted";
                }
                else
                {
                    LastOutcome = _native.ProcessorAvailable ? "grant-failed" : "processor-unavailable";
                }
                return;
            case PlantPipesDecision.WaitForSave:
                LastOutcome = "save-unavailable";
                return;
            default:
                throw new InvalidOperationException($"Unhandled Plant Pipes decision {decision} at {reason}.");
        }
    }

    internal void TickPending(TimeSpan elapsed, string reason)
    {
        if (!_attemptOutstanding || elapsed <= TimeSpan.Zero)
            return;

        TimeSpan? delay = PlantPipesReconciler.NextRetryDelay(_retryCount);
        if (delay == null)
        {
            _attemptOutstanding = false;
            LastOutcome = "retry-exhausted";
            return;
        }

        _retryElapsed += elapsed;
        if (_retryElapsed < delay.Value)
            return;

        _retryElapsed -= delay.Value;
        _attemptOutstanding = false;
        _retryCount++;
        OnLifecyclePoint(reason);
    }
}
