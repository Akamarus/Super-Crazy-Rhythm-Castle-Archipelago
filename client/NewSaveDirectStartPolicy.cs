namespace RhythmCastleAP;

internal enum NewSaveDirectStartDecision
{
    Ignore,
    Redirect,
}

internal static class NewSaveDirectStartPolicy
{
    internal const string IntroRoomId = "GameRoom_04A";

    internal static NewSaveDirectStartDecision Decide(
        bool enabled,
        bool compatible,
        bool freshSavePending,
        bool alreadyRedirected,
        string? roomId) =>
        enabled && compatible && freshSavePending && !alreadyRedirected &&
        string.Equals(roomId, IntroRoomId, StringComparison.OrdinalIgnoreCase)
            ? NewSaveDirectStartDecision.Redirect
            : NewSaveDirectStartDecision.Ignore;

    internal static bool ShouldUseLegacyFallback(
        bool enabled,
        bool compatible,
        string? roomId) =>
        enabled && compatible &&
        string.Equals(roomId, IntroRoomId, StringComparison.OrdinalIgnoreCase);
}
