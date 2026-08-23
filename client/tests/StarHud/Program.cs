using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

Equal(StarHudDecision.Vanilla,
    StarHudPolicy.Decide(false, true, true, "GameRoom_Hub2"),
    "disabled AP preserves vanilla");
Equal(StarHudDecision.Vanilla,
    StarHudPolicy.Decide(true, false, true, "GameRoom_Hub2"),
    "incompatible slot preserves vanilla");
Equal(StarHudDecision.PreserveMusicLabPoints,
    StarHudPolicy.Decide(true, true, false, "GameRoom_Hub6"),
    "Music Lab retains points HUD even before synchronization");
Equal(StarHudDecision.PreserveMusicLabPoints,
    StarHudPolicy.Decide(true, true, true, "GameRoom_Hub6"),
    "Music Lab never activates campaign Stars");
Equal(StarHudDecision.WaitForSynchronization,
    StarHudPolicy.Decide(true, true, false, "GameRoom_Hub2"),
    "campaign hub waits for AP synchronization");

foreach (string hub in new[] { "GameRoom_Hub1", "GameRoom_Hub1A", "GameRoom_Hub2", "GameRoom_Hub3", "GameRoom_Hub4", "GameRoom_Hub5", "GameRoom_Hub7" })
{
    Equal(StarHudDecision.ShowCampaignStars,
        StarHudPolicy.Decide(true, true, true, hub),
        $"{hub} shows synchronized campaign Stars");
}

Equal(StarHudDecision.Vanilla,
    StarHudPolicy.Decide(true, true, true, "GameRoom_27"),
    "Game Garage preserves native HUD context");
Equal(StarHudDecision.Vanilla,
    StarHudPolicy.Decide(true, true, true, "UnknownRoom"),
    "unknown room preserves native HUD context");

Console.WriteLine("Star HUD policy tests passed.");
