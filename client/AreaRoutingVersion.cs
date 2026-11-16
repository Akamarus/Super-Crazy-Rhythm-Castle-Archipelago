namespace RhythmCastleAP;

internal static class AreaRoutingVersion
{
    private const string Routing = "area-routing-plant-pipes-0.15";
    private static readonly string[] RouteOptions = { "-cell-tower-path-1", "-lobby-doors-1", "-bunker-1" };

    // The generator inserts optional route markers before the original progression contract.
    // Consume them in generator order; unknown, duplicated and reordered markers cannot match.
    internal static bool Supports(string? version, string requiredPrefix)
    {
        if (version == null || !version.StartsWith(Routing, StringComparison.Ordinal)) return false;
        var remainder = version.AsSpan(Routing.Length);
        foreach (string option in RouteOptions)
        {
            if (remainder.StartsWith(option.AsSpan(), StringComparison.Ordinal))
                remainder = remainder.Slice(option.Length);
        }
        return remainder.StartsWith(requiredPrefix.AsSpan(Routing.Length), StringComparison.Ordinal);
    }
}
