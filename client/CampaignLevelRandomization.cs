using System.Collections;
using Newtonsoft.Json.Linq;

namespace RhythmCastleAP;

internal sealed record CampaignLevelRandomizationSnapshot(
    CampaignLocationCompatibilityMode Mode,
    IReadOnlySet<string> ActiveLocations,
    string Detail);

internal static class CampaignLevelRandomization
{
    private static readonly object Sync = new();
    private static readonly IReadOnlySet<string> LegacyLocations = new HashSet<string>(StringComparer.Ordinal)
    {
        "Level 1 - Completion",
        "Level 2 - Completion",
        "Level 3 - Completion",
        "Level 22 - Completion",
        "Level 22 - 1 Star",
        "Level 22 - 2 Stars",
        "Level 22 - 3 Stars",
    };

    private static CampaignLocationCompatibilityMode _mode = CampaignLocationCompatibilityMode.IncompatibleClaim;
    private static IReadOnlySet<string> _activeLocations = EmptyLocations();
    private static string _detail = "not configured";

    internal static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        CampaignLocationCompatibilityResult compatibility = CampaignLocationContract.Validate(slotData);
        lock (Sync)
        {
            _mode = compatibility.Mode;
            _activeLocations = compatibility.Mode switch
            {
                CampaignLocationCompatibilityMode.Compatible =>
                    new HashSet<string>(compatibility.ActiveLocations, StringComparer.Ordinal),
                CampaignLocationCompatibilityMode.Legacy =>
                    LegacyActiveLocations(slotData),
                _ => EmptyLocations(),
            };
            _detail = compatibility.Detail;
        }
    }

    internal static IReadOnlyList<string> EvaluatePersistedResult(
        string? internalLevel,
        string? variant,
        int? starsEarned)
    {
        CampaignLocationCompatibilityMode mode;
        IReadOnlySet<string> activeLocations;
        lock (Sync)
        {
            mode = _mode;
            activeLocations = _activeLocations;
        }

        if (mode == CampaignLocationCompatibilityMode.Compatible)
        {
            return LevelCompletionPolicy.LocationsForPersistedResult(
                internalLevel,
                variant,
                starsEarned,
                activeLocations);
        }

        if (mode != CampaignLocationCompatibilityMode.Legacy || !IsLegacyDefaultVariant(variant))
        {
            return Array.Empty<string>();
        }

        if (string.Equals(internalLevel, "Level_28", StringComparison.OrdinalIgnoreCase))
        {
            return LevelCompletionPolicy.LocationsForPersistedResult(
                internalLevel,
                "LevelVariant_Default",
                starsEarned,
                activeLocations);
        }

        return internalLevel?.ToUpperInvariant() switch
        {
            "LEVEL_05" => new[] { "Level 1 - Completion" },
            "LEVEL_06" => new[] { "Level 2 - Completion" },
            "LEVEL_07" => new[] { "Level 3 - Completion" },
            _ => Array.Empty<string>(),
        };
    }

    internal static void OnDisconnected()
    {
        lock (Sync)
        {
            // Results can persist after a temporary socket loss. Keep the last
            // validated contract until a replacement identity or shutdown.
        }
    }

    internal static void OnIdentityReplaced() => Clear("identity replaced");

    internal static void Shutdown() => Clear("shutdown");

    internal static CampaignLevelRandomizationSnapshot Snapshot
    {
        get
        {
            lock (Sync)
            {
                return new(
                    _mode,
                    new HashSet<string>(_activeLocations, StringComparer.Ordinal),
                    _detail);
            }
        }
    }

    private static void Clear(string detail)
    {
        lock (Sync)
        {
            _mode = CampaignLocationCompatibilityMode.IncompatibleClaim;
            _activeLocations = EmptyLocations();
            _detail = detail;
        }
    }

    private static IReadOnlySet<string> EmptyLocations() =>
        new HashSet<string>(StringComparer.Ordinal);

    private static IReadOnlySet<string> LegacyActiveLocations(Dictionary<string, object>? slotData)
    {
        var locations = LegacyLocations.Where(name => name.EndsWith("Completion", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
        if (slotData?.TryGetValue("active_campaign_star_tiers", out object? raw) != true ||
            raw is not IEnumerable entries || raw is string)
            return locations;

        var tiers = new HashSet<int>();
        foreach (object? entry in entries)
        {
            object? value = entry is JValue token && token.Type == JTokenType.Integer ? token.Value : entry;
            long tier = value is int number ? number : value is long wide ? wide : -1;
            if (tier is < 1 or > 3 || !tiers.Add((int)tier))
                return locations;
        }
        foreach (int tier in tiers)
            locations.Add(tier == 1 ? "Level 22 - 1 Star" : $"Level 22 - {tier} Stars");
        return locations;
    }

    private static bool IsLegacyDefaultVariant(string? variant) =>
        string.IsNullOrWhiteSpace(variant) ||
        string.Equals(variant, "<none>", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(variant, "LevelVariant_Default", StringComparison.OrdinalIgnoreCase);
}
