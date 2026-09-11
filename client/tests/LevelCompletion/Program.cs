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

static HashSet<string> Set(params string[] locations) => new(locations, StringComparer.Ordinal);

static CampaignLevelDescriptor[] ExpectedLevels() =>
[
    new(1, "The Little Things", "Level_05", "Roots"),
    new(2, "Pop Party", "Level_06", "Roots"),
    new(3, "Jolt City", "Level_07", "Roots"),
    new(4, "Quieres Bailar", "Level_08", "Roots"),
    new(5, "Lift Quest", "Level_09", "Roots"),
    new(6, "Boring Room", "Level_02", "Lobby"),
    new(7, "Demolition Training", "Level_19", "Lobby"),
    new(8, "Minim Tower", "Level_11", "Lobby"),
    new(9, "School Trip", "Level_20", "Lobby"),
    new(10, "The Vault", "Level_01", "Lobby"),
    new(11, "Act 1: Flavor", "Level_12", "Meat Dimension"),
    new(12, "Act 2", "Level_15", "Meat Dimension"),
    new(13, "Act 3", "Level_22", "Meat Dimension"),
    new(14, "Act 4", "Level_23", "Meat Dimension"),
    new(15, "Central Mainframe", "Level_16", "Cell Tower"),
    new(16, "Thief Prince", "Level_24", "Cell Tower"),
    new(17, "Cold Storage", "Level_21", "Lobby"),
    new(18, "Darkness", "Level_03", "Tower of Fear"),
    new(19, "Escape", "Level_13", "Tower of Fear"),
    new(20, "Loneliness", "Level_25", "Tower of Fear"),
    new(21, "Locker Room", "Level_14", "Royal Corridor"),
    new(22, "King Ferdinand I", "Level_28", "Royal Corridor"),
];

static HashSet<string> AllCampaignLocations() =>
    ExpectedLevels()
        .SelectMany(level => new[]
        {
            $"Level {level.Number} - Completion",
            $"Level {level.Number} - 1 Star",
            $"Level {level.Number} - 2 Stars",
            $"Level {level.Number} - 3 Stars",
        })
        .ToHashSet(StringComparer.Ordinal);

static Dictionary<string, object> CompatibleSlotData() => new()
{
    ["implementation_version"] = "full-level-mapping-0.24",
    ["schema_version"] = 15L,
    ["campaign_level_mapping_schema"] = 1L,
    ["active_campaign_locations"] = AllCampaignLocations().OrderBy(name => name, StringComparer.Ordinal).ToArray(),
    ["special_variant_locations_active"] = false,
};

static CampaignLocationCompatibilityResult Validate(Action<Dictionary<string, object>> mutate)
{
    Dictionary<string, object> data = CompatibleSlotData();
    mutate(data);
    return CampaignLocationContract.Validate(data);
}

SequenceEqual(ExpectedLevels().Select(level => level.ToString()), CampaignLevelCatalog.All.Select(level => level.ToString()),
    "catalog has the exact 22 client descriptors in campaign order");
Equal(22, CampaignLevelCatalog.All.Count, "catalog has 22 descriptors");
Equal(true, CampaignLevelCatalog.TryGet("level_02", out CampaignLevelDescriptor levelSix), "catalog lookup is case-insensitive");
Equal(6, levelSix.Number, "catalog lookup returns the canonical descriptor");
Equal(false, CampaignLevelCatalog.TryGet("Level_99", out _), "unknown internal level is rejected");

CampaignLocationCompatibilityResult compatible = CampaignLocationContract.Validate(CompatibleSlotData());
Equal(CampaignLocationCompatibilityMode.Compatible, compatible.Mode, "exact v0.24 slot-data schema is compatible");
SequenceEqual(AllCampaignLocations().OrderBy(name => name, StringComparer.Ordinal), compatible.ActiveLocations.OrderBy(name => name, StringComparer.Ordinal),
    "compatible slot data retains canonical active locations");
Equal(CampaignLocationCompatibilityMode.Legacy,
    CampaignLocationContract.Validate(new() { ["implementation_version"] = "music-lab-points-0.23" }).Mode,
    "v0.23 implementation remains Legacy");

foreach ((string field, Action<Dictionary<string, object>> mutate) in new[]
{
    ("schema_version", (Action<Dictionary<string, object>>)(data => data["schema_version"] = 14L)),
    ("campaign_level_mapping_schema", data => data["campaign_level_mapping_schema"] = 2L),
    ("active_campaign_locations", data => data["active_campaign_locations"] = new[] { "Level 99 - Completion" }),
    ("active_campaign_locations", data => data["active_campaign_locations"] = new[] { "Level 1 - Completion", "Level 1 - Completion" }),
    ("special_variant_locations_active", data => data["special_variant_locations_active"] = true),
})
{
    CampaignLocationCompatibilityResult rejected = Validate(mutate);
    Equal(CampaignLocationCompatibilityMode.IncompatibleClaim, rejected.Mode, $"malformed v0.24 claim rejects {field}");
    Equal(true, rejected.Detail.Contains(field, StringComparison.Ordinal), $"rejection identifies failing {field}");
    Equal(0, rejected.ActiveLocations.Count, $"malformed {field} fails closed with no active locations");
}

SequenceEqual(Array.Empty<string>(),
    LevelCompletionPolicy.LocationsForPersistedResult("Level_28", "LevelVariant_Default", null, AllCampaignLocations()),
    "failed Level 22 persisted without result data");
SequenceEqual(Array.Empty<string>(),
    LevelCompletionPolicy.LocationsForPersistedResult("Level_28", "LevelVariant_Default", 0, AllCampaignLocations()),
    "failed zero-Star Level 22 result");
SequenceEqual(Array.Empty<string>(),
    LevelCompletionPolicy.LocationsForPersistedResult("Level_27", "LevelVariant_Default", 3, AllCampaignLocations()),
    "unrelated level has no locations");
SequenceEqual(
    new[] { "Level 6 - Completion", "Level 6 - 1 Star", "Level 6 - 2 Stars" },
    LevelCompletionPolicy.LocationsForPersistedResult(
        "Level_02", "LevelVariant_Default", 3,
        Set("Level 6 - Completion", "Level 6 - 1 Star", "Level 6 - 2 Stars")),
    "policy intersects result tiers with active seed locations");
SequenceEqual(Array.Empty<string>(),
    LevelCompletionPolicy.LocationsForPersistedResult("Level_06", "LevelVariant_BeeMode", 3, AllCampaignLocations()),
    "Bee variant never sends normal checks");
SequenceEqual(Array.Empty<string>(),
    LevelCompletionPolicy.LocationsForPersistedResult("Level_02", "LevelVariant_DevilMode", 3, AllCampaignLocations()),
    "Devil variant never sends normal checks");
SequenceEqual(Array.Empty<string>(),
    LevelCompletionPolicy.LocationsForPersistedResult("Level_02", "LevelVariant_Unknown", 3, AllCampaignLocations()),
    "unknown variant fails closed");
SequenceEqual(
    new[] { "Level 22 - Completion", "Level 22 - 1 Star", "Level 22 - 2 Stars", "Level 22 - 3 Stars" },
    LevelCompletionPolicy.LocationsForPersistedResult("Level_28", "LevelVariant_Default", 3, AllCampaignLocations()),
    "three-Star success is cumulative");
Equal(false, LevelCompletionPolicy.LocationsForPersistedResult("Level_28", "LevelVariant_Default", 3, AllCampaignLocations()).Contains("Victory"),
    "ordinary result never contains Victory");

Console.WriteLine("Level completion policy tests passed.");
