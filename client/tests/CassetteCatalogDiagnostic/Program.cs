using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string scenario)
{
    string expectedText = string.Join("|", expected);
    string actualText = string.Join("|", actual);
    if (!string.Equals(expectedText, actualText, StringComparison.Ordinal))
        throw new InvalidOperationException($"{scenario}: expected {expectedText}, got {actualText}");
}

var sources = new[]
{
    new CassetteCatalogDiagnosticSource(
        "level", "Level_06", "LevelVariant_Default", "I_GOT_MONEY",
        "Level_06|LevelVariant_Default|I_GOT_MONEY"),
    new CassetteCatalogDiagnosticSource(
        "sequence-step", "", "", "QUICKSAND",
        "Root/GameRoom_Hub6_Logic/Objects/RewardChests/SongChest_QuickSand"),
    new CassetteCatalogDiagnosticSource(
        "level", "Level_06", "LevelVariant_Default", "I_GOT_MONEY",
        "Level_06|LevelVariant_Default|I_GOT_MONEY"),
};

var snapshot = CassetteCatalogDiagnosticPolicy.CreateSnapshot(
    sources,
    new[] { "I_GOT_MONEY", "QUICKSAND", "FLAMENCO" },
    levelCount: 2,
    variantCount: 4);

Equal(false, CassetteCatalogDiagnosticPolicy.RequestsMutation,
    "catalog diagnostic remains read-only");
Equal(2, snapshot.Sources.Count,
    "exact duplicate source observations collapse to one deterministic row");
SequenceEqual(
    new[] { "level:I_GOT_MONEY", "sequence-step:QUICKSAND" },
    snapshot.Sources.Select(source => $"{source.SourceType}:{source.Song}"),
    "source rows sort by source identity");
SequenceEqual(new[] { "FLAMENCO" }, snapshot.MissingSongs,
    "missing expected songs are reported explicitly");
Equal(2, snapshot.UniqueSongCount,
    "unique song coverage ignores duplicate observations");
Equal(false, snapshot.IsComplete,
    "missing expected songs keep the diagnostic incomplete");
Equal(2, snapshot.LevelCount, "runtime level count is preserved");
Equal(4, snapshot.VariantCount, "runtime variant count is preserved");

var ambiguous = CassetteCatalogDiagnosticPolicy.CreateSnapshot(
    new[]
    {
        new CassetteCatalogDiagnosticSource(
            "level", "Level_06", "LevelVariant_Default", "I_GOT_MONEY",
            "Level_06|LevelVariant_Default|I_GOT_MONEY"),
        new CassetteCatalogDiagnosticSource(
            "trigger", "", "", "I_GOT_MONEY", "Root/UnexpectedTrigger"),
    },
    new[] { "I_GOT_MONEY" },
    levelCount: 1,
    variantCount: 1);

SequenceEqual(new[] { "I_GOT_MONEY" }, ambiguous.AmbiguousSongs,
    "one song mapped to distinct physical sources is reported as ambiguous");
Equal(false, ambiguous.IsComplete,
    "ambiguous physical sources keep the diagnostic incomplete");

var verifiedAliases = CassetteCatalogDiagnosticPolicy.CreateSnapshot(
    new[]
    {
        new CassetteCatalogDiagnosticSource(
            "level", "Level_02", "LevelVariant_Default", "AOK",
            "LevelDataProvider[0].GetAllLevelsData()[1].Variants[0].SongCassettes[0]=103:AOK",
            "cassette:AOK"),
        new CassetteCatalogDiagnosticSource(
            "level", "Level_02", "LevelVariant_DevilMode", "AOK",
            "LevelDataProvider[0].GetAllLevelsData()[1].Variants[1].SongCassettes[0]=103:AOK",
            "cassette:AOK"),
    },
    new[] { "AOK" },
    levelCount: 1,
    variantCount: 2);

SequenceEqual(new[] { "AOK" }, verifiedAliases.AliasSongs,
    "verified native routes to one logical cassette source are reported as aliases");
SequenceEqual(Array.Empty<string>(), verifiedAliases.AmbiguousSongs,
    "verified aliases are not reported as ambiguous physical sources");
Equal(true, verifiedAliases.IsComplete,
    "verified aliases preserve complete unique-song coverage");

var unexpected = CassetteCatalogDiagnosticPolicy.CreateSnapshot(
    new[]
    {
        new CassetteCatalogDiagnosticSource(
            "level", "Level_06", "LevelVariant_Default", "I_GOT_MONEY",
            "Level_06|LevelVariant_Default|I_GOT_MONEY"),
        new CassetteCatalogDiagnosticSource(
            "trigger", "", "", "UNEXPECTED_SONG", "Root/UnexpectedSongTrigger"),
    },
    new[] { "I_GOT_MONEY" },
    levelCount: 1,
    variantCount: 1);

SequenceEqual(new[] { "UNEXPECTED_SONG" }, unexpected.UnexpectedSongs,
    "observed songs outside the approved catalog are reported explicitly");
Equal(false, unexpected.IsComplete,
    "unexpected cassette sources keep the diagnostic incomplete");

var complete = CassetteCatalogDiagnosticPolicy.CreateSnapshot(
    sources.Take(2),
    new[] { "I_GOT_MONEY", "QUICKSAND" },
    levelCount: 2,
    variantCount: 4);

Equal(true, complete.IsComplete,
    "full unique coverage with no source ambiguity is complete");

string[] approvedNativeSongs =
{
    "THE_LITTLE_THINGS", "NO_PLAN_B", "JOLT_CITY", "QUIERES_BAILAR", "QUICKSAND", "GOLD",
    "I_GOT_MONEY", "HIPPO_AND_FROG", "ON_THE_WAY", "BADASS", "HEAVY_METAL", "AOK",
    "RAINBOW_MELODIES", "SNEAKING_LOOP", "THE_HEIST", "MONEY_DUB", "LETS_GO", "BOUNCE",
    "THE_EPICAL", "HOLLYWOOD_TRAILER", "FALSE_DATA", "GOTTA_GET_UP", "FUMBLIN_AROUND",
    "PARTY_NON_STOP", "KEEP_ON_HUSTLIN", "ANOTHER_DAY_IN_PARADISE", "FLAMENCO",
    "TEN_FOUR_GOOD_BUDDY", "ZEN", "WIGGLE",
};
var approvedSnapshot = CassetteCatalogDiagnosticPolicy.CreateSnapshot(
    approvedNativeSongs.Select((song, index) => new CassetteCatalogDiagnosticSource(
        "level", $"Level_{index + 1:00}", "LevelVariant_Default", song,
        $"fixture-{index + 1:00}")),
    levelCount: 30,
    variantCount: 30);

Equal(30, approvedSnapshot.UniqueSongCount,
    "the built-in approved catalog contains all 30 native songs");
Equal(true, approvedSnapshot.IsComplete,
    "the exact approved native catalog satisfies built-in coverage");

var tutorialScope = CassetteCatalogDiagnosticPolicy.DecideScope("GameRoom_04A");
Equal(true, tutorialScope.ScanGlobalLevels,
    "tutorial INSERT scans the global level provider");
Equal(false, tutorialScope.ScanRoomLocalNonLevelSources,
    "tutorial INSERT does not mislabel unloaded non-level components as globally scanned");

string[] hub6ChestSongs =
{
    "QUICKSAND", "FLAMENCO", "TEN_FOUR_GOOD_BUDDY", "ZEN", "WIGGLE",
};
var globalLevelSources = approvedNativeSongs
    .Except(hub6ChestSongs, StringComparer.Ordinal)
    .Select((song, index) => new CassetteCatalogDiagnosticSource(
        "level", $"Level_{index + 1:00}", "LevelVariant_Default", song,
        $"global-level-{index + 1:00}", $"cassette:{song}"))
    .Concat(new[]
    {
        new CassetteCatalogDiagnosticSource("level", "Level_02", "LevelVariant_DevilMode", "AOK", "alias-AOK", "cassette:AOK"),
        new CassetteCatalogDiagnosticSource("level", "Level_13", "LevelVariant_DevilMode", "FUMBLIN_AROUND", "alias-FUMBLIN_AROUND", "cassette:FUMBLIN_AROUND"),
        new CassetteCatalogDiagnosticSource("level", "Level_11", "LevelVariant_Default", "GOLD", "alias-GOLD-default", "cassette:GOLD"),
        new CassetteCatalogDiagnosticSource("level", "Level_11", "LevelVariant_DevilMode", "GOLD", "alias-GOLD-devil", "cassette:GOLD"),
        new CassetteCatalogDiagnosticSource("level", "Level_06", "LevelVariant_BeeMode", "I_GOT_MONEY", "alias-I_GOT_MONEY", "cassette:I_GOT_MONEY"),
        new CassetteCatalogDiagnosticSource("level", "Level_14", "LevelVariant_DevilMode", "KEEP_ON_HUSTLIN", "alias-KEEP_ON_HUSTLIN", "cassette:KEEP_ON_HUSTLIN"),
        new CassetteCatalogDiagnosticSource("level", "Level_12", "LevelVariant_BeeMode", "LETS_GO", "alias-LETS_GO", "cassette:LETS_GO"),
        new CassetteCatalogDiagnosticSource("level", "Level_11", "LevelVariant_Default", "ON_THE_WAY", "alias-ON_THE_WAY-default", "cassette:ON_THE_WAY"),
        new CassetteCatalogDiagnosticSource("level", "Level_11", "LevelVariant_DevilMode", "ON_THE_WAY", "alias-ON_THE_WAY-devil", "cassette:ON_THE_WAY"),
        new CassetteCatalogDiagnosticSource("level", "Level_11", "LevelVariant_DevilMode", "RAINBOW_MELODIES", "alias-RAINBOW_MELODIES-devil", "cassette:RAINBOW_MELODIES"),
        new CassetteCatalogDiagnosticSource("level", "Level_19", "LevelVariant_Default", "RAINBOW_MELODIES", "alias-RAINBOW_MELODIES-level19", "cassette:RAINBOW_MELODIES"),
        new CassetteCatalogDiagnosticSource("level", "Level_20", "LevelVariant_Default", "SNEAKING_LOOP", "alias-SNEAKING_LOOP", "cassette:SNEAKING_LOOP"),
    })
    .ToArray();

var tutorialCoverage = CassetteCatalogDiagnosticPolicy.CreateCoverage(
    globalLevelSources,
    Array.Empty<CassetteCatalogDiagnosticSource>(),
    tutorialScope,
    levelCount: 25,
    variantCount: 25);

Equal(true, tutorialCoverage.GlobalLevels.IsComplete,
    "tutorial scan can prove all 25 level-earned cassette sources");
Equal(37, tutorialCoverage.GlobalLevels.Sources.Count,
    "the runtime trigger count is preserved alongside 25 unique logical sources");
SequenceEqual(
    new[]
    {
        "AOK", "FUMBLIN_AROUND", "GOLD", "I_GOT_MONEY", "KEEP_ON_HUSTLIN",
        "LETS_GO", "ON_THE_WAY", "RAINBOW_MELODIES", "SNEAKING_LOOP",
    },
    tutorialCoverage.GlobalLevels.AliasSongs,
    "all nine runtime duplicate-song groups are verified aliases");
SequenceEqual(Array.Empty<string>(), tutorialCoverage.GlobalLevels.AmbiguousSongs,
    "the complete runtime catalog has no ambiguous physical source groups");
Equal(false, tutorialCoverage.RoomLocalNonLevelScanned,
    "tutorial scan records that room-local non-level sources were not observed");
Equal(false, tutorialCoverage.AllPhysicalSourcesProven,
    "global level completeness alone never claims all 30 physical sources");

var hub6Scope = CassetteCatalogDiagnosticPolicy.DecideScope("GameRoom_Hub6");
Equal(true, hub6Scope.ScanGlobalLevels,
    "Hub6 INSERT still scans the global level provider");
Equal(true, hub6Scope.ScanRoomLocalNonLevelSources,
    "Hub6 INSERT also scans room-local non-level award components");

var hub6Coverage = CassetteCatalogDiagnosticPolicy.CreateCoverage(
    globalLevelSources,
    hub6ChestSongs.Select((song, index) => new CassetteCatalogDiagnosticSource(
        "sequence-step", "", "", song, $"hub6-chest-{index + 1}")),
    hub6Scope,
    levelCount: 25,
    variantCount: 25);

Equal(true, hub6Coverage.RoomLocalNonLevel.IsComplete,
    "Hub6 scan can prove all five independently established non-level chest songs");
Equal(true, hub6Coverage.AllPhysicalSourcesProven,
    "all 30 physical sources are proven only when global and room-local scopes are complete");

Console.WriteLine("Cassette catalog diagnostic policy tests passed.");
