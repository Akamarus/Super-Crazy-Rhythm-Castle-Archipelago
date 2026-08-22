namespace RhythmCastleAP;

internal static class RepairCompatibilityPolicy
{
    internal const string RequiredVersion =
        "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18";

    internal static bool IsCompatible(string? implementationVersion, bool repairEnabled) =>
        repairEnabled &&
        implementationVersion != null &&
        implementationVersion.StartsWith(RequiredVersion, StringComparison.Ordinal);
}
