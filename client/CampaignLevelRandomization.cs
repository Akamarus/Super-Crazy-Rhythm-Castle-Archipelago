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
                    new HashSet<string>(LegacyLocations, StringComparer.Ordinal),
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

    private static bool IsLegacyDefaultVariant(string? variant) =>
        string.IsNullOrWhiteSpace(variant) ||
        string.Equals(variant, "<none>", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(variant, "LevelVariant_Default", StringComparison.OrdinalIgnoreCase);
}
