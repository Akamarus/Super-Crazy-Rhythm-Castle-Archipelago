using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual, string scenario)
{
    string[] expectedArray = expected.ToArray();
    string[] actualArray = actual.ToArray();
    if (!expectedArray.SequenceEqual(actualArray, StringComparer.Ordinal))
        throw new InvalidOperationException($"{scenario}: expected [{string.Join(", ", expectedArray)}], got [{string.Join(", ", actualArray)}]");
}

Equal("Level 22 - Completion", LevelCompletionPolicy.CompletionLocation("Level_28"), "native Level 22");
Equal<string?>(null, LevelCompletionPolicy.CompletionLocation("Level_27"), "unrelated level");
SequenceEqual(Array.Empty<string>(), LevelCompletionPolicy.EarnedTierLocations("Level_28", 0), "zero-Star clear");
SequenceEqual(new[] { "Level 22 - 1 Star" }, LevelCompletionPolicy.EarnedTierLocations("Level_28", 1), "one-Star clear");
SequenceEqual(new[] { "Level 22 - 1 Star", "Level 22 - 2 Stars" }, LevelCompletionPolicy.EarnedTierLocations("Level_28", 2), "two-Star clear is cumulative");
SequenceEqual(new[] { "Level 22 - 1 Star", "Level 22 - 2 Stars", "Level 22 - 3 Stars" }, LevelCompletionPolicy.EarnedTierLocations("Level_28", 3), "three-Star clear is cumulative");
SequenceEqual(Array.Empty<string>(), LevelCompletionPolicy.EarnedTierLocations("Level_27", 3), "unrelated level has no tiers");
Equal(false, LevelCompletionPolicy.EarnedTierLocations("Level_28", 3).Contains("Victory"), "ordinary result never contains Victory");

Console.WriteLine("Level completion policy tests passed.");
