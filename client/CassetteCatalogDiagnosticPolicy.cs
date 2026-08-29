namespace RhythmCastleAP;

internal sealed record CassetteCatalogDiagnosticSource(
    string SourceType,
    string Level,
    string Variant,
    string Song,
    string NativeIdentity);

internal sealed record CassetteCatalogDiagnosticSnapshot(
    IReadOnlyList<CassetteCatalogDiagnosticSource> Sources,
    IReadOnlyList<string> MissingSongs,
    IReadOnlyList<string> UnexpectedSongs,
    IReadOnlyList<string> DuplicateSongs,
    int UniqueSongCount,
    int LevelCount,
    int VariantCount,
    bool IsComplete);

internal sealed record CassetteCatalogDiagnosticScope(
    bool ScanGlobalLevels,
    bool ScanRoomLocalNonLevelSources);

internal sealed record CassetteCatalogDiagnosticCoverage(
    CassetteCatalogDiagnosticSnapshot GlobalLevels,
    CassetteCatalogDiagnosticSnapshot RoomLocalNonLevel,
    bool RoomLocalNonLevelScanned,
    bool AllPhysicalSourcesProven);

internal static class CassetteCatalogDiagnosticPolicy
{
    internal static bool RequestsMutation => false;

    internal static IReadOnlyList<string> ApprovedLevelEarnedNativeSongs { get; } = new[]
    {
        "THE_LITTLE_THINGS",
        "NO_PLAN_B",
        "JOLT_CITY",
        "QUIERES_BAILAR",
        "GOLD",
        "I_GOT_MONEY",
        "HIPPO_AND_FROG",
        "ON_THE_WAY",
        "BADASS",
        "HEAVY_METAL",
        "AOK",
        "RAINBOW_MELODIES",
        "SNEAKING_LOOP",
        "THE_HEIST",
        "MONEY_DUB",
        "LETS_GO",
        "BOUNCE",
        "THE_EPICAL",
        "HOLLYWOOD_TRAILER",
        "FALSE_DATA",
        "GOTTA_GET_UP",
        "FUMBLIN_AROUND",
        "PARTY_NON_STOP",
        "KEEP_ON_HUSTLIN",
        "ANOTHER_DAY_IN_PARADISE",
    };

    internal static IReadOnlyList<string> ApprovedHub6NonLevelNativeSongs { get; } = new[]
    {
        "QUICKSAND",
        "FLAMENCO",
        "TEN_FOUR_GOOD_BUDDY",
        "ZEN",
        "WIGGLE",
    };

    internal static IReadOnlyList<string> ApprovedNativeSongs { get; } =
        ApprovedLevelEarnedNativeSongs
            .Concat(ApprovedHub6NonLevelNativeSongs)
            .ToArray();

    internal static CassetteCatalogDiagnosticScope DecideScope(string? roomId) =>
        new(
            ScanGlobalLevels: true,
            ScanRoomLocalNonLevelSources: string.Equals(
                roomId,
                "GameRoom_Hub6",
                StringComparison.OrdinalIgnoreCase));

    internal static CassetteCatalogDiagnosticCoverage CreateCoverage(
        IEnumerable<CassetteCatalogDiagnosticSource> globalLevelSources,
        IEnumerable<CassetteCatalogDiagnosticSource> roomLocalNonLevelSources,
        CassetteCatalogDiagnosticScope scope,
        int levelCount,
        int variantCount)
    {
        CassetteCatalogDiagnosticSnapshot globalLevels = CreateSnapshot(
            globalLevelSources,
            ApprovedLevelEarnedNativeSongs,
            levelCount,
            variantCount);
        CassetteCatalogDiagnosticSnapshot roomLocalNonLevel = CreateSnapshot(
            scope.ScanRoomLocalNonLevelSources
                ? roomLocalNonLevelSources
                : Array.Empty<CassetteCatalogDiagnosticSource>(),
            ApprovedHub6NonLevelNativeSongs,
            levelCount: 0,
            variantCount: 0);

        return new CassetteCatalogDiagnosticCoverage(
            globalLevels,
            roomLocalNonLevel,
            scope.ScanRoomLocalNonLevelSources,
            scope.ScanGlobalLevels &&
            globalLevels.IsComplete &&
            scope.ScanRoomLocalNonLevelSources &&
            roomLocalNonLevel.IsComplete);
    }

    internal static CassetteCatalogDiagnosticSnapshot CreateSnapshot(
        IEnumerable<CassetteCatalogDiagnosticSource> sources,
        int levelCount,
        int variantCount) =>
        CreateSnapshot(sources, ApprovedNativeSongs, levelCount, variantCount);

    internal static CassetteCatalogDiagnosticSnapshot CreateSnapshot(
        IEnumerable<CassetteCatalogDiagnosticSource> sources,
        IEnumerable<string> expectedSongs,
        int levelCount,
        int variantCount)
    {
        var orderedSources = sources
            .Distinct()
            .OrderBy(source => source.SourceType, StringComparer.Ordinal)
            .ThenBy(source => source.Level, StringComparer.Ordinal)
            .ThenBy(source => source.Variant, StringComparer.Ordinal)
            .ThenBy(source => source.Song, StringComparer.Ordinal)
            .ThenBy(source => source.NativeIdentity, StringComparer.Ordinal)
            .ToArray();

        var observedSongs = orderedSources
            .Select(source => source.Song)
            .Where(song => !string.IsNullOrWhiteSpace(song))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(song => song, StringComparer.Ordinal)
            .ToArray();

        var expectedSongArray = expectedSongs
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var missingSongs = expectedSongArray
            .Where(song => !observedSongs.Contains(song, StringComparer.OrdinalIgnoreCase))
            .OrderBy(song => song, StringComparer.Ordinal)
            .ToArray();

        var unexpectedSongs = observedSongs
            .Where(song => !expectedSongArray.Contains(song, StringComparer.OrdinalIgnoreCase))
            .OrderBy(song => song, StringComparer.Ordinal)
            .ToArray();

        var duplicateSongs = orderedSources
            .Where(source => !string.IsNullOrWhiteSpace(source.Song))
            .GroupBy(source => source.Song, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(song => song, StringComparer.Ordinal)
            .ToArray();

        return new CassetteCatalogDiagnosticSnapshot(
            orderedSources,
            missingSongs,
            unexpectedSongs,
            duplicateSongs,
            observedSongs.Length,
            levelCount,
            variantCount,
            missingSongs.Length == 0 &&
            unexpectedSongs.Length == 0 &&
            duplicateSongs.Length == 0);
    }
}
