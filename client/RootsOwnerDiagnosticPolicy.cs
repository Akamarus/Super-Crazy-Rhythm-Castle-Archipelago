namespace RhythmCastleAP;

internal static class RootsOwnerDiagnosticPolicy
{
    internal const string OwnerPath =
        "Root/GameRoom_Hub2_Logic/ProgressionLogic/PlayGateAndDifficultyScene";

    internal static bool RequestsMutation => false;

    internal static bool ShouldInspect(string? roomId, string? ownerPath) =>
        string.Equals(roomId, "GameRoom_Hub2", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(ownerPath, OwnerPath, StringComparison.Ordinal);
}
