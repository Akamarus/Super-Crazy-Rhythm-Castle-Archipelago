namespace RhythmCastleAP;

internal static class MusicLabBarrierPolicy
{
    internal static IReadOnlyList<string> KnownBarrierPaths { get; } = new[]
    {
        "Root/GameRoom_Hub6_Logic/Objects/Barriers-tofb/Barriers_Lobby",
        "Root/GameRoom_Hub6_Logic/Objects/Barriers-tofb/Barriers_Final",
    };

    internal static bool ShouldDisable(
        bool enabled,
        bool compatible,
        string? roomId,
        string? exactPath) =>
        enabled && compatible &&
        string.Equals(roomId, "GameRoom_Hub6", StringComparison.OrdinalIgnoreCase) &&
        KnownBarrierPaths.Contains(exactPath, StringComparer.Ordinal);
}
