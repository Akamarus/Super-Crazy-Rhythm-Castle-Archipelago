namespace RhythmCastleAP;

internal static class LevelCompletionPolicy
{
    internal static IReadOnlyList<string> LocationsForPersistedResult(
        string? internalLevel,
        int? starsEarned)
    {
        if (!string.Equals(internalLevel, "Level_28", StringComparison.OrdinalIgnoreCase) ||
            starsEarned is null or < 1)
            return Array.Empty<string>();

        int count = Math.Clamp(starsEarned.Value, 1, 3);
        return new[] { "Level 22 - Completion" }
            .Concat(Enumerable.Range(1, count)
            .Select(stars => $"Level 22 - {stars} Star{(stars == 1 ? "" : "s")}")
            .ToArray())
            .ToArray();
    }
}
