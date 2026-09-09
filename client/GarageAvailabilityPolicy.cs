namespace RhythmCastleAP;

internal enum GarageObjectRole
{
    Initialization,
    SongCartridge,
}

internal enum GarageObjectDecision
{
    Vanilla,
    Active,
    Inactive,
}

internal readonly record struct GarageEntrancePreviewOwnership(
    string Song,
    string NativeCartridgeType,
    bool RoutingEnabled,
    bool Compatible,
    bool NativeOwned,
    bool ApOwned,
    bool EffectiveOwned);

internal static class GarageAvailabilityPolicy
{
    private static readonly string[] EntrancePreviewParameterTypes =
    {
        "eRoom27GameCartridgeType",
        "Boolean",
        "eCleanMedal",
        "eCleanMedal",
    };

    internal static GarageObjectDecision Decide(
        bool enabled,
        bool compatible,
        bool ownsCartridge,
        GarageObjectRole role)
    {
        if (!enabled || !compatible)
            return GarageObjectDecision.Vanilla;
        if (role == GarageObjectRole.Initialization)
            return GarageObjectDecision.Active;
        return ownsCartridge
            ? GarageObjectDecision.Active
            : GarageObjectDecision.Inactive;
    }

    internal static bool TryResolveEntrancePreviewOwned(
        bool enabled,
        bool compatible,
        string nativeCartridgeType,
        bool nativeOwned,
        IEnumerable<string>? apOwnedSongs,
        out GarageEntrancePreviewOwnership ownership)
    {
        GarageCartridgeNativeDefinition cartridge =
            GarageCartridgeNativePolicy.RandomizedCartridges.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.NativeCartridgeType,
                    nativeCartridgeType,
                    StringComparison.Ordinal));
        if (string.IsNullOrEmpty(cartridge.Song))
        {
            ownership = default;
            return false;
        }

        bool apOwned = apOwnedSongs?.Contains(cartridge.Song, StringComparer.OrdinalIgnoreCase) == true;
        bool effectiveOwned = nativeOwned || (enabled && compatible && apOwned);
        ownership = new GarageEntrancePreviewOwnership(
            cartridge.Song,
            cartridge.NativeCartridgeType,
            enabled,
            compatible,
            nativeOwned,
            apOwned,
            effectiveOwned);
        return true;
    }

    internal static bool IsExactEntrancePreviewRefreshSignature(
        string declaringTypeName,
        string methodName,
        string returnTypeName,
        IReadOnlyList<string>? parameterTypeNames)
    {
        return string.Equals(declaringTypeName, "LevelPreviewUIData", StringComparison.Ordinal) &&
            string.Equals(methodName, "RefreshDataForGarageCartridge", StringComparison.Ordinal) &&
            string.Equals(returnTypeName, "Void", StringComparison.Ordinal) &&
            parameterTypeNames is { Count: 4 } &&
            EntrancePreviewParameterTypes.SequenceEqual(parameterTypeNames, StringComparer.Ordinal);
    }
}
