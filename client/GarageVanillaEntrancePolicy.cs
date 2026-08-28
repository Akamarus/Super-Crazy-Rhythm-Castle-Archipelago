namespace RhythmCastleAP;

internal static class GarageVanillaEntrancePolicy
{
    internal const string Song = "Vampire Killer";
    internal const string VersionMarker = "vanilla-vampire-garage-0.21";

    internal static bool IsEnabled(string? implementationVersion, string? vanillaSong)
    {
        return !string.IsNullOrWhiteSpace(implementationVersion)
            && implementationVersion.Contains(VersionMarker, StringComparison.OrdinalIgnoreCase)
            && string.Equals(vanillaSong, Song, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ShouldRandomizeSong(bool enabled, string song) =>
        !enabled || !string.Equals(song, Song, StringComparison.OrdinalIgnoreCase);

    internal static bool ShouldSuppressSourceGrant(bool enabled, string song) =>
        ShouldRandomizeSong(enabled, song);

    internal static bool ShouldWriteNativeSaveFlags(bool enabled) => false;
}
