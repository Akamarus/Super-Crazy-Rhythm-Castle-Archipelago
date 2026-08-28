using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

Equal(DifficultyAvailabilityDecision.Vanilla,
    DifficultyAvailabilityPolicy.Decide(false, true, false, false),
    "disabled AP preserves vanilla");
Equal(DifficultyAvailabilityDecision.Vanilla,
    DifficultyAvailabilityPolicy.Decide(true, false, false, false),
    "incompatible slot preserves vanilla");
Equal(DifficultyAvailabilityDecision.ExposeBoth,
    DifficultyAvailabilityPolicy.Decide(true, true, false, true),
    "missing Normal availability is repaired");
Equal(DifficultyAvailabilityDecision.ExposeBoth,
    DifficultyAvailabilityPolicy.Decide(true, true, true, false),
    "missing Pro availability is repaired");
Equal(DifficultyAvailabilityDecision.AlreadyAvailable,
    DifficultyAvailabilityPolicy.Decide(true, true, true, true),
    "both native choices remain untouched");

Console.WriteLine("Difficulty availability policy tests passed.");
