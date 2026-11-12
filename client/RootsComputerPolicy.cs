namespace RhythmCastleAP;

internal static class RootsComputerPolicy
{
    internal const string RoomId = "GameRoom_Hub2";
    internal const string CoverPath =
        "Root/GameRoom_Hub2_Logic/Objects/AmProContainer/Cover-tofb";

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
