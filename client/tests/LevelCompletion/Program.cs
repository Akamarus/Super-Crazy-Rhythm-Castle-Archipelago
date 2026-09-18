using RhythmCastleAP;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LoginSuccessful = Archipelago.MultiClient.Net.LoginSuccessful;

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
    new(1, "Light Humor", "Level_05", "Roots"),
    new(2, "Pop Party", "Level_06", "Roots"),
    new(3, "The Megafying Ritual", "Level_07", "Roots"),
    new(4, "DJ Eggplant", "Level_08", "Roots"),
    new(5, "Lift Quest", "Level_09", "Roots"),
    new(6, "Boring Room", "Level_02", "Lobby"),
    new(7, "Demolition Training", "Level_19", "Lobby"),
    new(8, "Minim Tower", "Level_11", "Lobby"),
    new(9, "School Trip", "Level_20", "Lobby"),
    new(10, "The Vault", "Level_01", "Lobby"),
    new(11, "Act 1: Flavor", "Level_12", "Meat Dimension"),
    new(12, "Act 2: Sauce and Spice", "Level_15", "Meat Dimension"),
    new(13, "Act 3: Montage", "Level_22", "Meat Dimension"),
    new(14, "Act 4: Habanero", "Level_23", "Meat Dimension"),
    new(15, "Central Mainframe", "Level_16", "Cell Tower"),
    new(16, "The Thief Prince", "Level_24", "Cell Tower"),
    new(17, "Cold Storage", "Level_21", "Lobby"),
    new(18, "The Darkness", "Level_03", "Tower of Fear"),
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
    ["implementation_version"] = (string)LegacySlotData()["implementation_version"] + "-full-level-mapping-0.24",
    ["schema_version"] = 15L,
    ["campaign_level_mapping_schema"] = 1L,
    ["active_campaign_locations"] = AllCampaignLocations().OrderBy(name => name, StringComparer.Ordinal).ToArray(),
    ["special_variant_locations_active"] = false,
};

static Dictionary<string, object> LegacySlotData() => new()
{
    ["implementation_version"] = "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23",
};

static CampaignLocationCompatibilityResult Validate(Action<Dictionary<string, object>> mutate)
{
    Dictionary<string, object> data = CompatibleSlotData();
    mutate(data);
    return CampaignLocationContract.Validate(data);
}

static Dictionary<string, object> RoundTripSlotData(Dictionary<string, object> data)
{
    string json = JsonConvert.SerializeObject(new { cmd = "Connected", team = 0, slot = 1, slot_data = data });
    var packet = (ConnectedPacket)JsonConvert.DeserializeObject<ArchipelagoPacketBase>(json, new ArchipelagoPacketConverter())!;
    return new LoginSuccessful(packet).SlotData;
}

SequenceEqual(ExpectedLevels().Select(level => level.ToString()), CampaignLevelCatalog.All.Select(level => level.ToString()),
    "catalog has the exact 22 client descriptors in campaign order");
Equal(22, CampaignLevelCatalog.All.Count, "catalog has 22 descriptors");
Equal(true, CampaignLevelCatalog.TryGet("level_02", out CampaignLevelDescriptor levelSix), "catalog lookup is case-insensitive");
Equal(6, levelSix.Number, "catalog lookup returns the canonical descriptor");
Equal(false, CampaignLevelCatalog.TryGet("Level_99", out _), "unknown internal level is rejected");

var batchCampaign = CompatibleSlotData();
batchCampaign["implementation_version"] = "full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27";
batchCampaign["schema_version"] = 18;
Equal(CampaignLocationCompatibilityMode.Compatible, CampaignLocationContract.Validate(batchCampaign).Mode, "schema18 campaign contract");
batchCampaign["schema_version"] = 17;
Equal(CampaignLocationCompatibilityMode.IncompatibleClaim, CampaignLocationContract.Validate(batchCampaign).Mode, "schema18 rejects mismatched schema");
var starContract = new Dictionary<string,object>(batchCampaign);
starContract["implementation_version"] += "-ap-stars-0.28";
starContract["schema_version"] = 19;
Equal(CampaignLocationCompatibilityMode.Compatible, CampaignLocationContract.Validate(starContract).Mode, "schema19 preserves existing subsystem contract");
starContract["schema_version"] = 18;
Equal(CampaignLocationCompatibilityMode.IncompatibleClaim, CampaignLocationContract.Validate(starContract).Mode, "AP Stars suffix requires schema19");
starContract["schema_version"] = 19;
starContract["implementation_version"] = batchCampaign["implementation_version"];
Equal(CampaignLocationCompatibilityMode.IncompatibleClaim, CampaignLocationContract.Validate(starContract).Mode, "schema19 requires AP Stars suffix");
starContract["implementation_version"] += "-ap-stars-0.28-extra";
Equal(CampaignLocationCompatibilityMode.IncompatibleClaim, CampaignLocationContract.Validate(starContract).Mode, "unknown AP Stars suffix extension rejected");

var characterCampaign = CompatibleSlotData();
characterCampaign["implementation_version"] = "full-level-mapping-0.24-character-quest-items-0.25";
characterCampaign["schema_version"] = 16L;
Equal(CampaignLocationCompatibilityMode.Compatible, CampaignLocationContract.Validate(characterCampaign).Mode, "v0.25 retains full campaign mapping");
characterCampaign["schema_version"] = 15L;
Equal(CampaignLocationCompatibilityMode.IncompatibleClaim, CampaignLocationContract.Validate(characterCampaign).Mode, "v0.25 cannot downgrade schema");
CampaignLocationCompatibilityResult compatible = CampaignLocationContract.Validate(CompatibleSlotData());
Equal(CampaignLocationCompatibilityMode.Compatible, compatible.Mode, "exact v0.24 slot-data schema is compatible");
SequenceEqual(AllCampaignLocations().OrderBy(name => name, StringComparer.Ordinal), compatible.ActiveLocations.OrderBy(name => name, StringComparer.Ordinal),
    "compatible slot data retains canonical active locations");

Dictionary<string, object> wireData = RoundTripSlotData(CompatibleSlotData());
Equal(true, wireData["active_campaign_locations"] is JArray, "real login retains the campaign location list as a JSON array");
Equal(true, ((JArray)wireData["active_campaign_locations"])[0] is JValue, "real login wraps each campaign location as a JSON scalar");
Equal(CampaignLocationCompatibilityMode.Compatible, CampaignLocationContract.Validate(wireData).Mode,
    "complete v0.24 contract through ConnectedPacket and LoginSuccessful is compatible");

Equal(CampaignLocationCompatibilityMode.Legacy,
    CampaignLocationContract.Validate(new()
    {
        ["implementation_version"] = "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23",
    }).Mode,
    "v0.23 implementation remains Legacy");
foreach (Dictionary<string, object> malformedImplementation in new[]
{
    new Dictionary<string, object> { ["implementation_version"] = "unrelated-client-0.24" },
    new Dictionary<string, object>(),
    new Dictionary<string, object> { ["implementation_version"] = "full-level-mapping-0.25" },
    new Dictionary<string, object> { ["implementation_version"] = 24L },
})
{
    CampaignLocationCompatibilityResult rejected = CampaignLocationContract.Validate(malformedImplementation);
    Equal(CampaignLocationCompatibilityMode.IncompatibleClaim, rejected.Mode, "unknown, missing, future, and non-string implementation claims fail closed");
    Equal(true, rejected.Detail.Contains("implementation_version", StringComparison.Ordinal), "implementation claim rejection identifies its field");
    Equal(0, rejected.ActiveLocations.Count, "implementation claim rejection exposes no active locations");
}

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

CampaignLevelRandomization.Shutdown();
CampaignLevelRandomization.ApplySlotData(CompatibleSlotData());
Equal(CampaignLocationCompatibilityMode.Compatible, CampaignLevelRandomization.Snapshot.Mode,
    "adapter stores compatible campaign mode");
SequenceEqual(
    AllCampaignLocations().OrderBy(name => name, StringComparer.Ordinal),
    CampaignLevelRandomization.Snapshot.ActiveLocations.OrderBy(name => name, StringComparer.Ordinal),
    "adapter stores the exact compatible active set");
SequenceEqual(
    new[] { "Level 6 - Completion", "Level 6 - 1 Star", "Level 6 - 2 Stars" },
    CampaignLevelRandomization.EvaluatePersistedResult("Level_02", "LevelVariant_Default", 2),
    "compatible adapter routes default campaign results through the active-set policy");

CampaignLevelRandomization.ApplySlotData(RoundTripSlotData(CompatibleSlotData()));
Equal(CampaignLocationCompatibilityMode.Compatible, CampaignLevelRandomization.Snapshot.Mode,
    "adapter accepts real ConnectedPacket slot-data shapes");

foreach ((int[] tiers, string[] expected) in new[]
{
    (new[] { 1 }, new[] { "Level 22 - Completion", "Level 22 - 1 Star" }),
    (new[] { 1, 2 }, new[] { "Level 22 - Completion", "Level 22 - 1 Star", "Level 22 - 2 Stars" }),
    (new[] { 1, 2, 3 }, new[] { "Level 22 - Completion", "Level 22 - 1 Star", "Level 22 - 2 Stars", "Level 22 - 3 Stars" }),
})
{
    var legacy = LegacySlotData();
    legacy["active_campaign_star_tiers"] = tiers;
    CampaignLevelRandomization.ApplySlotData(RoundTripSlotData(legacy));
    SequenceEqual(expected, CampaignLevelRandomization.EvaluatePersistedResult("Level_28", "LevelVariant_Default", 3),
        $"legacy {tiers.Length}-tier difficulty preserves cumulative Level 22 checks");
    SequenceEqual(Array.Empty<string>(), CampaignLevelRandomization.EvaluatePersistedResult("Level_02", "LevelVariant_Default", 3),
        "legacy Star tiers do not activate new campaign levels");
}

CampaignLevelRandomization.ApplySlotData(LegacySlotData());
Equal(CampaignLocationCompatibilityMode.Legacy, CampaignLevelRandomization.Snapshot.Mode,
    "adapter stores legacy campaign mode");
foreach ((string level, string location) in new[]
{
    ("Level_05", "Level 1 - Completion"),
    ("Level_06", "Level 2 - Completion"),
    ("Level_07", "Level 3 - Completion"),
})
{
    SequenceEqual(
        new[] { location },
        CampaignLevelRandomization.EvaluatePersistedResult(level, "LevelVariant_Default", null),
        $"legacy adapter preserves {location} without requiring newly available Star data");
}
SequenceEqual(
    new[] { "Level 22 - Completion" },
    CampaignLevelRandomization.EvaluatePersistedResult("Level_28", "LevelVariant_Default", 1),
    "legacy adapter preserves verified Level 22 completion");
SequenceEqual(
    new[] { "Level 1 - Completion" },
    CampaignLevelRandomization.EvaluatePersistedResult("Level_05", "<none>", null),
    "legacy adapter preserves a Level 1 persisted event whose variant was unavailable");
SequenceEqual(
    new[] { "Level 22 - Completion" },
    CampaignLevelRandomization.EvaluatePersistedResult("Level_28", "<none>", 1),
    "legacy adapter preserves a verified Level 22 result whose variant was unavailable");
SequenceEqual(Array.Empty<string>(),
    CampaignLevelRandomization.EvaluatePersistedResult("Level_08", "LevelVariant_Default", 3),
    "legacy adapter does not activate Level 4 or other new campaign mappings");
SequenceEqual(Array.Empty<string>(),
    CampaignLevelRandomization.EvaluatePersistedResult("Level_05", "LevelVariant_BeeMode", 3),
    "legacy adapter still rejects non-default variants");

Dictionary<string, object> malformedCampaign = CompatibleSlotData();
malformedCampaign["campaign_level_mapping_schema"] = 2L;
CampaignLevelRandomization.ApplySlotData(malformedCampaign);
Equal(CampaignLocationCompatibilityMode.IncompatibleClaim, CampaignLevelRandomization.Snapshot.Mode,
    "adapter stores malformed v0.24 claims as incompatible");
SequenceEqual(Array.Empty<string>(),
    CampaignLevelRandomization.EvaluatePersistedResult("Level_05", "LevelVariant_Default", 3),
    "malformed v0.24 claim exposes no new campaign locations");

CampaignLevelRandomization.ApplySlotData(CompatibleSlotData());
CampaignLevelRandomization.OnDisconnected();
Equal(CampaignLocationCompatibilityMode.Compatible, CampaignLevelRandomization.Snapshot.Mode,
    "temporary disconnect retains the compatible campaign state");
SequenceEqual(
    new[] { "Level 1 - Completion", "Level 1 - 1 Star" },
    CampaignLevelRandomization.EvaluatePersistedResult("Level_05", "LevelVariant_Default", 1),
    "offline persisted results use the retained active set");

CampaignLevelRandomization.OnIdentityReplaced();
Equal(0, CampaignLevelRandomization.Snapshot.ActiveLocations.Count,
    "AP identity replacement clears the active campaign set");
SequenceEqual(Array.Empty<string>(),
    CampaignLevelRandomization.EvaluatePersistedResult("Level_05", "LevelVariant_Default", 3),
    "replacement cannot leak prior-identity campaign locations");

CampaignLevelRandomization.ApplySlotData(CompatibleSlotData());
IReadOnlyList<string> firstResult = CampaignLevelRandomization.EvaluatePersistedResult(
    "Level_05", "LevelVariant_Default", 1);
IReadOnlyList<string> improvedResult = CampaignLevelRandomization.EvaluatePersistedResult(
    "Level_05", "LevelVariant_Default", 3);
SequenceEqual(
    new[] { "Level 1 - Completion", "Level 1 - 1 Star" },
    firstResult,
    "one-Star result returns the first cumulative campaign checks");
SequenceEqual(
    new[] { "Level 1 - Completion", "Level 1 - 1 Star", "Level 1 - 2 Stars", "Level 1 - 3 Stars" },
    improvedResult,
    "repeated improved result still returns every earned tier to the location queue");
var queuedOrSent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
SequenceEqual(firstResult, firstResult.Where(queuedOrSent.Add),
    "first result queues both newly checked names");
SequenceEqual(
    new[] { "Level 1 - 2 Stars", "Level 1 - 3 Stars" },
    improvedResult.Where(queuedOrSent.Add),
    "location-level queue idempotency suppresses prior tiers but admits improvements");

SequenceEqual(
    new[] { "Level 22 - Completion", "Level 22 - 1 Star" },
    CampaignLevelRandomization.EvaluatePersistedResult("Level_28", "LevelVariant_Default", 1),
    "ordinary Level 22 result remains a campaign check");
SequenceEqual(Array.Empty<string>(),
    CampaignLevelRandomization.EvaluatePersistedResult("Level_28", "LevelVariant_Default", null),
    "a later event without result Star data cannot reuse a prior Level 22 clear");
Equal(false,
    CampaignLevelRandomization.EvaluatePersistedResult("Level_28", "LevelVariant_Default", 3).Contains("Victory"),
    "campaign adapter never returns Victory");

CampaignLevelRandomization.Shutdown();
Equal(0, CampaignLevelRandomization.Snapshot.ActiveLocations.Count,
    "shutdown clears the active campaign set");
SequenceEqual(Array.Empty<string>(),
    CampaignLevelRandomization.EvaluatePersistedResult("Level_05", "LevelVariant_Default", 3),
    "shutdown rejects late persisted results");

string pluginSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));
string persistedWiring = pluginSource[
    pluginSource.IndexOf("public static void ResultPersistedEventPostfix", StringComparison.Ordinal)..
    pluginSource.IndexOf("private sealed record PendingResult", StringComparison.Ordinal)];
Equal(true, persistedWiring.Contains("Plugin.AP?.QueueCampaignResult(", StringComparison.Ordinal),
    "production persisted-result path uses the identity-bound campaign queue exercised by lifecycle tests");
Equal(false, persistedWiring.Contains("LocationMap.InternalToLocationName", StringComparison.Ordinal),
    "production campaign results no longer use the partial location dictionary");
Equal(false, persistedWiring.Contains("bool level22", StringComparison.Ordinal),
    "Level 22 no longer has a separate campaign result branch");
Equal(false, persistedWiring.Contains("Victory", StringComparison.Ordinal),
    "ordinary persisted-result wiring cannot queue Victory");
string repeatedEventBranch = persistedWiring[
    persistedWiring.IndexOf("if (repeatedPersistedKey", StringComparison.Ordinal)..
    persistedWiring.IndexOf("MusicLabDiscovery.RecordPersistedEvent", StringComparison.Ordinal)];
Equal(false, repeatedEventBranch.Contains("return;", StringComparison.Ordinal),
    "repeated persisted events are logged without blocking improved Star tiers");
string queueWiring = pluginSource[
    pluginSource.IndexOf("public void QueueLocation", StringComparison.Ordinal)..
    pluginSource.IndexOf("private void FlushPendingChecks", StringComparison.Ordinal)];
Equal(true, queueWiring.Contains("_queuedOrSent.Add(locationName)", StringComparison.Ordinal),
    "production queue retains location-level idempotency");

ConnectionTests.Run(CompatibleSlotData);
Console.WriteLine("Level completion policy tests passed.");
