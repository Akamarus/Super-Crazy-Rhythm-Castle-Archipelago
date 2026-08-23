namespace RhythmCastleAP;

internal enum StarHudDecision
{
    Vanilla,
    PreserveMusicLabPoints,
    ReconcileCampaignBootstrap,
    NativeReady,
    DeferToApStars,
}

internal static class StarHudPolicy
{
    internal static StarHudDecision Decide(
        bool enabled,
        bool compatible,
        bool liveApStarsActive,
        string? roomId,
        bool bootstrapFlagsOwned)
    {
        if (!enabled || !compatible)
            return StarHudDecision.Vanilla;
        if (liveApStarsActive)
            return StarHudDecision.DeferToApStars;
        if (string.Equals(roomId, "GameRoom_Hub6", StringComparison.OrdinalIgnoreCase))
            return StarHudDecision.PreserveMusicLabPoints;
        return bootstrapFlagsOwned
            ? StarHudDecision.NativeReady
            : StarHudDecision.ReconcileCampaignBootstrap;
    }
}
