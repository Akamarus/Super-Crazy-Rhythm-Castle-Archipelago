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
    bool NativeBootstrapOwned,
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
        if (snapshot.NativeBootstrapOwned)
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

internal enum AreaArrivalKind
{
    None,
    RootsPhone,
    LobbyPhone,
}

internal static class AreaArrivalPresentationPolicy
{
    internal static AreaArrivalKind DecideTransition(
        bool enabled,
        bool compatible,
        string? originRoom,
        string? destinationRoom,
        bool ownsRoots,
        bool ownsLobby)
    {
        if (!enabled || !compatible ||
            !string.Equals(originRoom, "GameRoom_Hub6", StringComparison.OrdinalIgnoreCase))
            return AreaArrivalKind.None;
        if (ownsRoots && string.Equals(destinationRoom, "GameRoom_Hub2", StringComparison.OrdinalIgnoreCase))
            return AreaArrivalKind.RootsPhone;
        if (ownsLobby &&
            (string.Equals(destinationRoom, "GameRoom_Hub1A", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(destinationRoom, "GameRoom_Hub1B", StringComparison.OrdinalIgnoreCase)))
            return AreaArrivalKind.LobbyPhone;
        return AreaArrivalKind.None;
    }

    internal static bool MatchesRoom(AreaArrivalKind arrival, string? room) => arrival switch
    {
        AreaArrivalKind.RootsPhone => string.Equals(room, "GameRoom_Hub2", StringComparison.OrdinalIgnoreCase),
        AreaArrivalKind.LobbyPhone => string.Equals(room, "GameRoom_Hub1A", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(room, "GameRoom_Hub1B", StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    internal static bool ShouldBypassCondition(AreaArrivalKind arrival, string? room, string? flag)
    {
        if (flag == null || !MatchesRoom(arrival, room)) return false;
        return arrival == AreaArrivalKind.RootsPhone ? RootsFlags.Contains(flag) :
            string.Equals(flag, "OVERALL_PROGRESS_REACHED_LOBBY_HUB", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly HashSet<string> RootsFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "ROOTS_HUB_INTRO_WITNESSED",
        "ROOTS_HUB_GATE_OPENED",
        "ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE",
    };
}
