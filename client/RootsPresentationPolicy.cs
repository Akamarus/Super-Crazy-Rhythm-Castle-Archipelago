namespace RhythmCastleAP;

internal enum RootsIntroDecision
{
    Vanilla,
    QueueFlag,
    Wait,
    AllowTransition,
    FallbackVanilla,
}

internal readonly record struct RootsIntroSnapshot(
    bool Enabled,
    bool Compatible,
    bool EnteringRoots,
    bool NativeFlagOwned,
    bool ProcessorAvailable,
    bool SubmissionOutstanding,
    int RetryCount);

internal static class RootsPresentationPolicy
{
    internal const string DifficultySequenceType =
        "AssignAndCommentOnMusicDifficultySequenceStep";

    internal static RootsIntroDecision DecideIntro(RootsIntroSnapshot snapshot)
    {
        if (!snapshot.Enabled || !snapshot.Compatible || !snapshot.EnteringRoots)
            return RootsIntroDecision.Vanilla;
        if (snapshot.NativeFlagOwned)
            return RootsIntroDecision.AllowTransition;
        if (RetryDelay(snapshot.RetryCount) == null)
            return RootsIntroDecision.FallbackVanilla;
        if (snapshot.SubmissionOutstanding)
            return RootsIntroDecision.Wait;
        return RootsIntroDecision.QueueFlag;
    }

    internal static TimeSpan? RetryDelay(int retryCount) => retryCount switch
    {
        0 => TimeSpan.Zero,
        1 => TimeSpan.FromMilliseconds(50),
        2 => TimeSpan.FromMilliseconds(100),
        3 => TimeSpan.FromMilliseconds(200),
        _ => null,
    };

    internal static bool ShouldSuppressPostLevelOne(
        bool enabled,
        bool compatible,
        string? sequenceType,
        bool levelOnePersisted) =>
        enabled && compatible && levelOnePersisted &&
        string.Equals(sequenceType, DifficultySequenceType, StringComparison.Ordinal);
}
