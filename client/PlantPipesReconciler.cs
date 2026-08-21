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
