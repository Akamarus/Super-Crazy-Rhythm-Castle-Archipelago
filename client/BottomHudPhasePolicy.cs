namespace RhythmCastleAP;

internal enum BottomHudPhaseDecision
{
    Preserve,
    NormalizeToIdle,
}

internal static class BottomHudPhasePolicy
{
    internal static BottomHudPhaseDecision Decide(
        bool enabled,
        bool compatible,
        string? roomId,
        int currentPhase)
    {
        if (!enabled || !compatible || currentPhase != 0 || roomId is null || !HubRooms.Contains(roomId))
            return BottomHudPhaseDecision.Preserve;
        return BottomHudPhaseDecision.NormalizeToIdle;
    }

    private static readonly HashSet<string> HubRooms = new(StringComparer.OrdinalIgnoreCase)
    {
        "GameRoom_Hub1",
        "GameRoom_Hub1A",
        "GameRoom_Hub2",
        "GameRoom_Hub3",
        "GameRoom_Hub4",
        "GameRoom_Hub5",
        "GameRoom_Hub6",
        "GameRoom_Hub7",
    };
}
