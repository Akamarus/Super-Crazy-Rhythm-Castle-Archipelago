using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

foreach (bool owns in new[] { false, true })
{
    Equal(GarageObjectDecision.Active,
        GarageAvailabilityPolicy.Decide(true, true, owns, GarageObjectRole.Initialization),
        "holder and play initialization always remain active");
    Equal(owns ? GarageObjectDecision.Active : GarageObjectDecision.Inactive,
        GarageAvailabilityPolicy.Decide(true, true, owns, GarageObjectRole.SongCartridge),
        "only matching song cartridge follows AP ownership");
}

foreach (GarageObjectRole role in Enum.GetValues<GarageObjectRole>())
{
    Equal(GarageObjectDecision.Vanilla,
        GarageAvailabilityPolicy.Decide(false, true, false, role),
        "disabled AP preserves vanilla");
    Equal(GarageObjectDecision.Vanilla,
        GarageAvailabilityPolicy.Decide(true, false, false, role),
        "incompatible slot preserves vanilla");
}

Console.WriteLine("Game Garage availability policy tests passed.");
