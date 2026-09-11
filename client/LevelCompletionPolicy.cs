namespace RhythmCastleAP;

internal static class LevelCompletionPolicy
{
    internal static IReadOnlyList<string> LocationsForPersistedResult(
        string? internalLevel,
        string? variant,
        int? starsEarned,
        IReadOnlySet<string> activeLocations)
    {
        if (!string.Equals(variant, "LevelVariant_Default", StringComparison.OrdinalIgnoreCase) ||
            starsEarned is null or < 1 ||
            !CampaignLevelCatalog.TryGet(internalLevel, out CampaignLevelDescriptor level))
            return Array.Empty<string>();

        List<string> candidates = [level.Location("Completion")];
        for (int stars = 1; stars <= Math.Clamp(starsEarned.Value, 1, 3); stars++)
            candidates.Add(level.Location(stars == 1 ? "1 Star" : $"{stars} Stars"));
        return candidates.Where(activeLocations.Contains).ToArray();
    }
}
