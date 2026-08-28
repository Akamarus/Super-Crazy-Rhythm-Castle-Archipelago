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

Equal(true,
    GarageVanillaEntrancePolicy.IsEnabled("area-routing-test-vanilla-vampire-garage-0.21", "Vampire Killer"),
    "v0.21 enables the vanilla entrance cartridge");
Equal(false,
    GarageVanillaEntrancePolicy.IsEnabled("area-routing-test-difficulty-filtering-0.20", null),
    "v0.20 seeds preserve six-cartridge randomization");
Equal(false,
    GarageVanillaEntrancePolicy.ShouldRandomizeSong(true, "Vampire Killer"),
    "v0.21 does not AP-randomize Vampire Killer");
Equal(true,
    GarageVanillaEntrancePolicy.ShouldRandomizeSong(true, "Smooch"),
    "v0.21 still randomizes the other Garage cartridges");
Equal(false,
    GarageVanillaEntrancePolicy.ShouldSuppressSourceGrant(true, "Vampire Killer"),
    "v0.21 preserves the native Vampire Killer grant");
Equal(true,
    GarageVanillaEntrancePolicy.ShouldSuppressSourceGrant(true, "Smooch"),
    "v0.21 still suppresses randomized native grants");
Equal(true,
    GarageVanillaEntrancePolicy.ShouldSuppressSourceGrant(false, "Vampire Killer"),
    "v0.20 retains its old Vampire Killer source behavior");
Equal(false,
    GarageVanillaEntrancePolicy.ShouldWriteNativeSaveFlags(true),
    "v0.21 requires the physical vanilla pickup and never writes its save flags");

Console.WriteLine("Game Garage availability policy tests passed.");
