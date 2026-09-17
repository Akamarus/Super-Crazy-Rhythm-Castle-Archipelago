namespace RhythmCastleAP;

internal enum LobbyItemGrantDecision
{
    Ignore,
    Apply,
    AlreadyHeld,
    Consumed,
}

internal static class LobbyItemRandomizationPolicy
{
    private static readonly IReadOnlyList<string> PickupSources = new[]
    {
        "Lobby - Important Letters Pickup",
        "Lobby - Bean Trumpet Award",
    };

    internal static IReadOnlyList<string> SourceLocations(
        bool enabled, string? roomId, string? flag, bool value) =>
        enabled && value &&
        string.Equals(roomId, "GameRoom_Hub1A", StringComparison.Ordinal) &&
        string.Equals(flag, "LOBBY_HUB_MENIAL_TASK_ITEM_COLLECTED", StringComparison.Ordinal)
            ? PickupSources
            : Array.Empty<string>();

    internal static bool SuppressNativeGrant(
        bool enabled, string? roomId, string? flag, bool value, bool applyingApGrant) =>
        enabled && value && !applyingApGrant &&
        string.Equals(roomId, "GameRoom_Hub1A", StringComparison.Ordinal) &&
        (string.Equals(flag, "LOBBY_HUB_MENIAL_TASK_ITEM", StringComparison.Ordinal) ||
         string.Equals(flag, "BEAN_TRUMPET_ABILITY", StringComparison.Ordinal));

    internal static LobbyItemGrantDecision LettersGrant(
        bool received, bool compatible, bool held, bool deposited)
    {
        if (!received || !compatible)
            return LobbyItemGrantDecision.Ignore;
        if (deposited)
            return LobbyItemGrantDecision.Consumed;
        return held ? LobbyItemGrantDecision.AlreadyHeld : LobbyItemGrantDecision.Apply;
    }

    internal static LobbyItemGrantDecision TrumpetGrant(
        bool received, bool compatible, bool abilityHeld)
    {
        if (!received || !compatible)
            return LobbyItemGrantDecision.Ignore;
        return abilityHeld ? LobbyItemGrantDecision.AlreadyHeld : LobbyItemGrantDecision.Apply;
    }
}
