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

SequenceEqual(Array.Empty<string>(), LevelCompletionPolicy.LocationsForPersistedResult("Level_28", null), "failed Level 22 persisted without result data");
SequenceEqual(Array.Empty<string>(), LevelCompletionPolicy.LocationsForPersistedResult("Level_28", 0), "failed zero-Star Level 22 result");
SequenceEqual(Array.Empty<string>(), LevelCompletionPolicy.LocationsForPersistedResult("Level_27", 3), "unrelated level has no locations");
SequenceEqual(
    new[] { "Level 22 - Completion", "Level 22 - 1 Star" },
    LevelCompletionPolicy.LocationsForPersistedResult("Level_28", 1),
    "one-Star success sends completion and one-Star tier");
SequenceEqual(
    new[] { "Level 22 - Completion", "Level 22 - 1 Star", "Level 22 - 2 Stars" },
    LevelCompletionPolicy.LocationsForPersistedResult("Level_28", 2),
    "two-Star success is cumulative");
SequenceEqual(
    new[] { "Level 22 - Completion", "Level 22 - 1 Star", "Level 22 - 2 Stars", "Level 22 - 3 Stars" },
    LevelCompletionPolicy.LocationsForPersistedResult("Level_28", 3),
    "three-Star success is cumulative");
Equal(false, LevelCompletionPolicy.LocationsForPersistedResult("Level_28", 3).Contains("Victory"), "ordinary result never contains Victory");

Console.WriteLine("Level completion policy tests passed.");
