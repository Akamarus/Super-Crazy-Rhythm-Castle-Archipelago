using System.Collections;

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
            versionRaw is not string version ||
            !version.EndsWith(ClaimSuffix, StringComparison.Ordinal))
        {
            return new(CampaignLocationCompatibilityMode.Legacy, EmptyLocations, "pre-v0.24 implementation");
        }

        if (!TryReadNumber(slotData, "schema_version", out int schemaVersion) || schemaVersion != SchemaVersion)
            return Fail("schema_version");
        if (!TryReadNumber(slotData, "campaign_level_mapping_schema", out int mappingSchema) || mappingSchema != MappingSchema)
            return Fail("campaign_level_mapping_schema");
        if (!TryReadLocations(slotData, out HashSet<string> activeLocations))
            return Fail("active_campaign_locations");
        if (!slotData.TryGetValue("special_variant_locations_active", out object? specialVariantsRaw) ||
            specialVariantsRaw is not bool specialVariantsActive ||
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

        switch (raw)
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
            if (entry is not string location || !KnownLocations.Contains(location) || !activeLocations.Add(location))
                return false;
        }

        return true;
    }

    private static CampaignLocationCompatibilityResult Fail(string field) =>
        new(CampaignLocationCompatibilityMode.IncompatibleClaim, EmptyLocations, $"invalid {field}");
}
