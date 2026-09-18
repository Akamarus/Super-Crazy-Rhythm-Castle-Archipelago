using System.Collections;
using Newtonsoft.Json.Linq;

namespace RhythmCastleAP;

internal enum CampaignLocationCompatibilityMode
{
    Legacy,
    Compatible,
    IncompatibleClaim,
}

internal sealed record CampaignLocationCompatibilityResult(
    CampaignLocationCompatibilityMode Mode,
    IReadOnlySet<string> ActiveLocations,
    string Detail);

internal static class CampaignLocationContract
{
    private const string ClaimSuffix = "full-level-mapping-0.24";
    private const string LegacyV023Implementation =
        "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23";
    private const int SchemaVersion = 15;
    private const int MappingSchema = 1;

    private static readonly IReadOnlySet<string> EmptyLocations = new HashSet<string>(StringComparer.Ordinal);
    private static readonly IReadOnlySet<string> KnownLocations = CampaignLevelCatalog.All
        .SelectMany(level => new[]
        {
            level.Location("Completion"),
            level.Location("1 Star"),
            level.Location("2 Stars"),
            level.Location("3 Stars"),
        })
        .ToHashSet(StringComparer.Ordinal);

    internal static CampaignLocationCompatibilityResult Validate(Dictionary<string, object>? slotData)
    {
        if (slotData?.TryGetValue("implementation_version", out object? versionRaw) != true ||
            UnwrapScalar(versionRaw) is not string version)
            return Fail("implementation_version");

        if (string.Equals(version, LegacyV023Implementation, StringComparison.Ordinal))
            return new(CampaignLocationCompatibilityMode.Legacy, EmptyLocations, "pre-v0.24 implementation");

        bool apStars = version.EndsWith(ClaimSuffix + "-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28", StringComparison.Ordinal);
        bool batchChecks = apStars || version.EndsWith(ClaimSuffix + "-character-quest-items-0.25-quest-checks-0.26-check-expansion-0.27", StringComparison.Ordinal);
        bool expandedQuests = batchChecks || version.EndsWith(ClaimSuffix + "-character-quest-items-0.25-quest-checks-0.26", StringComparison.Ordinal);
        bool characterItems = expandedQuests || version.EndsWith(ClaimSuffix + "-character-quest-items-0.25", StringComparison.Ordinal);
        if (!characterItems && !version.EndsWith(ClaimSuffix, StringComparison.Ordinal))
            return Fail("implementation_version");

        if (!TryReadNumber(slotData, "schema_version", out int schemaVersion) || schemaVersion != (apStars ? 19 : batchChecks ? 18 : expandedQuests ? 17 : characterItems ? 16 : SchemaVersion))
            return Fail("schema_version");
        if (!TryReadNumber(slotData, "campaign_level_mapping_schema", out int mappingSchema) || mappingSchema != MappingSchema)
            return Fail("campaign_level_mapping_schema");
        if (!TryReadLocations(slotData, out HashSet<string> activeLocations))
            return Fail("active_campaign_locations");
        if (!slotData.TryGetValue("special_variant_locations_active", out object? specialVariantsRaw) ||
            UnwrapScalar(specialVariantsRaw) is not bool specialVariantsActive ||
            specialVariantsActive)
        {
            return Fail("special_variant_locations_active");
        }

        return new(CampaignLocationCompatibilityMode.Compatible, activeLocations, "compatible");
    }

    private static bool TryReadNumber(Dictionary<string, object> slotData, string key, out int value)
    {
        value = 0;
        if (!slotData.TryGetValue(key, out object? raw))
            return false;

        switch (UnwrapScalar(raw))
        {
            case int integer:
                value = integer;
                return true;
            case long longValue when longValue is >= int.MinValue and <= int.MaxValue:
                value = (int)longValue;
                return true;
            default:
                return false;
        }
    }

    private static bool TryReadLocations(Dictionary<string, object> slotData, out HashSet<string> activeLocations)
    {
        activeLocations = new(StringComparer.Ordinal);
        if (!slotData.TryGetValue("active_campaign_locations", out object? raw) ||
            raw is not IEnumerable entries ||
            raw is string)
        {
            return false;
        }

        foreach (object? entry in entries)
        {
            if (UnwrapScalar(entry) is not string location || !KnownLocations.Contains(location) || !activeLocations.Add(location))
                return false;
        }

        return true;
    }

    private static CampaignLocationCompatibilityResult Fail(string field) =>
        new(CampaignLocationCompatibilityMode.IncompatibleClaim, EmptyLocations, $"invalid {field}");

    // ConnectedPacket.SlotData retains nested JSON scalar wrappers in 6.7.1.
    // Unwrap only accepted scalar kinds; arrays and objects remain invalid input.
    private static object? UnwrapScalar(object? raw) => raw is JValue value &&
        value.Type is JTokenType.Integer or JTokenType.String or JTokenType.Boolean or JTokenType.Null
            ? value.Value
            : raw;
}
