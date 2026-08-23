using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

Equal(StarHudDecision.Vanilla,
    StarHudPolicy.Decide(false, true, false, "GameRoom_Hub2", false),
    "disabled AP preserves vanilla");
Equal(StarHudDecision.Vanilla,
    StarHudPolicy.Decide(true, false, false, "GameRoom_Hub2", false),
    "incompatible slot preserves vanilla");
Equal(StarHudDecision.PreserveMusicLabPoints,
    StarHudPolicy.Decide(true, true, false, "GameRoom_Hub6", false),
    "Music Lab retains points HUD");
Equal(StarHudDecision.ReconcileCampaignBootstrap,
    StarHudPolicy.Decide(true, true, false, "GameRoom_Hub2", false),
    "Roots missing bootstrap flags is repaired");
Equal(StarHudDecision.NativeReady,
    StarHudPolicy.Decide(true, true, false, "GameRoom_Hub2", true),
    "Roots with bootstrap flags uses native HUD");
Equal(StarHudDecision.DeferToApStars,
    StarHudPolicy.Decide(true, true, true, "GameRoom_Hub2", false),
    "future live AP Stars own the HUD");

Console.WriteLine("Star HUD policy tests passed.");
