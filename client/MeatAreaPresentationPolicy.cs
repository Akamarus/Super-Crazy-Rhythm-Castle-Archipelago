namespace RhythmCastleAP;

internal static class MeatAreaPresentationPolicy
{
    internal const string RoomId = "GameRoom_Hub4";
    internal const string FirstAreaGatePath =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/FirstAreaGate";

    internal static bool ShouldSuppressFirstAreaGate(
        bool enabled,
        bool compatible,
        bool ownsMeatAccess,
        string? roomId,
        string? rootPath) =>
        enabled && compatible && ownsMeatAccess &&
        string.Equals(roomId, RoomId, StringComparison.Ordinal) &&
        string.Equals(rootPath, FirstAreaGatePath, StringComparison.Ordinal);
}
