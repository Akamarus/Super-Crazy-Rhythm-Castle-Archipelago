namespace RhythmCastleAP;

internal enum StarHudDecision
{
    Vanilla,
    PreserveMusicLabPoints,
    ShowCampaignStars,
    WaitForSynchronization,
}

internal static class StarHudPolicy
{
    internal static StarHudDecision Decide(
        bool enabled,
        bool compatible,
        bool synchronized,
        string? roomId)
    {
        if (!enabled || !compatible)
            return StarHudDecision.Vanilla;
        if (string.Equals(roomId, "GameRoom_Hub6", StringComparison.OrdinalIgnoreCase))
            return StarHudDecision.PreserveMusicLabPoints;

        bool campaignHub = roomId is not null && CampaignHubs.Contains(roomId);
        if (!campaignHub)
            return StarHudDecision.Vanilla;
        return synchronized
            ? StarHudDecision.ShowCampaignStars
            : StarHudDecision.WaitForSynchronization;
    }

    private static readonly HashSet<string> CampaignHubs = new(StringComparer.OrdinalIgnoreCase)
    {
        "GameRoom_Hub1",
        "GameRoom_Hub1A",
        "GameRoom_Hub2",
        "GameRoom_Hub3",
        "GameRoom_Hub4",
        "GameRoom_Hub5",
        "GameRoom_Hub7",
    };
}
