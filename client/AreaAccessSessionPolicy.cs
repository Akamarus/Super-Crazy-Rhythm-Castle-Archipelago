namespace RhythmCastleAP;

internal static class AreaAccessSessionPolicy
{
    private const string ImplementationPrefix = "area-routing";

    internal static bool IsAuthenticatedCompatible(
        bool enabled,
        string? implementationVersion) =>
        enabled &&
        implementationVersion?.StartsWith(
            ImplementationPrefix,
            StringComparison.OrdinalIgnoreCase) == true;
}
