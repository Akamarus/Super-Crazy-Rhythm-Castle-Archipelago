namespace RhythmCastleAP;

internal enum GarageObjectRole
{
    Initialization,
    SongCartridge,
}

internal enum GarageObjectDecision
{
    Vanilla,
    Active,
    Inactive,
}

internal static class GarageAvailabilityPolicy
{
    internal static GarageObjectDecision Decide(
        bool enabled,
        bool compatible,
        bool ownsCartridge,
        GarageObjectRole role)
    {
        if (!enabled || !compatible)
            return GarageObjectDecision.Vanilla;
        if (role == GarageObjectRole.Initialization)
            return GarageObjectDecision.Active;
        return ownsCartridge
            ? GarageObjectDecision.Active
            : GarageObjectDecision.Inactive;
    }
}
