namespace RhythmCastleAP;

internal enum DifficultyAvailabilityDecision
{
    Vanilla,
    ExposeBoth,
    AlreadyAvailable,
}

internal static class DifficultyAvailabilityPolicy
{
    internal static DifficultyAvailabilityDecision Decide(
        bool enabled,
        bool compatible,
        bool normalAvailable,
        bool proAvailable)
    {
        if (!enabled || !compatible)
            return DifficultyAvailabilityDecision.Vanilla;
        return normalAvailable && proAvailable
            ? DifficultyAvailabilityDecision.AlreadyAvailable
            : DifficultyAvailabilityDecision.ExposeBoth;
    }
}
