namespace RhythmCastleAP;

internal static class RootsComputerPolicy
{
    internal const string RoomId = "GameRoom_Hub2";
    internal const string ControlPath =
        "Root/GameRoom_Hub2_Logic/Objects/AmProContainer/AmProRobot";
    internal const string CoverPath =
        "Root/GameRoom_Hub2_Logic/Objects/AmProContainer/Cover-tofb";

    internal static bool ShouldNormalize(
        bool enabled,
        bool compatible,
        bool ownsRoots,
        string? roomId,
        string? objectPath) =>
        enabled && compatible && ownsRoots &&
        string.Equals(roomId, RoomId, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(objectPath, ControlPath, StringComparison.Ordinal);

    internal static bool ShouldHideCover(
        bool enabled,
        bool compatible,
        bool ownsRoots,
        string? roomId) =>
        enabled && compatible && ownsRoots &&
        string.Equals(roomId, RoomId, StringComparison.OrdinalIgnoreCase);

    internal static bool ShouldNormalizeState(
        bool enabled,
        bool compatible,
        bool ownsRoots,
        string? roomId) =>
        ShouldHideCover(enabled, compatible, ownsRoots, roomId);
}
