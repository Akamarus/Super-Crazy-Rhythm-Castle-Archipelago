namespace RhythmCastleAP;

internal static class RoyalCorridorTraversalPolicy
{
    internal const string RootPath = "Root/GameRoom_Hub7_Logic/Objects/StarEaterAndGap";
    internal const string BlockerChild = "StarEater/BlockingCollision";
    internal const string BridgeChild = "BridgeAcrossGap";

    internal static bool ShouldOpen(bool enabled, bool compatible, bool accessOwned, string? room) =>
        enabled && compatible && accessOwned &&
        string.Equals(room, "GameRoom_Hub7", StringComparison.OrdinalIgnoreCase);
}
