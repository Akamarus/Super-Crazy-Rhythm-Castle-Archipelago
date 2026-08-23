namespace RhythmCastleAP;

internal static class LevelCompletionPolicy
{
    internal static string? CompletionLocation(string? internalLevel) =>
        string.Equals(internalLevel, "Level_28", StringComparison.OrdinalIgnoreCase)
            ? "Level 22 - Completion"
            : null;

    internal static IReadOnlyList<string> EarnedTierLocations(
        string? internalLevel,
        int starsEarned)
    {
        if (!string.Equals(internalLevel, "Level_28", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<string>();

        int count = Math.Clamp(starsEarned, 0, 3);
        return Enumerable.Range(1, count)
            .Select(stars => $"Level 22 - {stars} Star{(stars == 1 ? "" : "s")}")
            .ToArray();
    }
}
