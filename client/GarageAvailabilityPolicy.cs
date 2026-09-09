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

internal enum GarageEntrancePipelineDiagnosticStage
{
    Refresh,
    Data,
    View,
}

internal sealed class GarageEntrancePipelineDiagnosticGate
{
    private readonly object _sync = new();
    private readonly Dictionary<GarageEntrancePipelineDiagnosticStage, string> _lastSignatures = new();

    internal bool ShouldLog(GarageEntrancePipelineDiagnosticStage stage, string signature)
    {
        signature ??= string.Empty;
        lock (_sync)
        {
            if (_lastSignatures.TryGetValue(stage, out string? previous) &&
                string.Equals(previous, signature, StringComparison.Ordinal))
            {
                return false;
            }

            _lastSignatures[stage] = signature;
            return true;
        }
    }

    internal void Reset()
    {
        lock (_sync)
            _lastSignatures.Clear();
    }
}

internal static class GarageAvailabilityPolicy
{
    private static readonly string[] EntrancePreviewParameterTypes =
    {
        "eRoom27GameCartridgeType",
        "Boolean",
        "eCleanMedal",
        "eCleanMedal",
    };
    private static readonly string[] EntrancePipelineRefreshParameterTypes =
    {
        "LevelIdentifier",
        "LevelVariantIdentifier",
    };
    private static readonly string[] EntrancePipelineViewParameterTypes =
    {
        "LevelPreviewUIData",
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

    internal static bool IsExactEntrancePipelineRefreshSignature(
        string declaringTypeName,
        string methodName,
        string returnTypeName,
        IReadOnlyList<string>? parameterTypeNames)
    {
        return string.Equals(declaringTypeName, "LevelPreviewUI", StringComparison.Ordinal) &&
            string.Equals(methodName, "RefreshLevelPreviewUIData", StringComparison.Ordinal) &&
            IsExactVoidSignature(parameterTypeNames, returnTypeName, EntrancePipelineRefreshParameterTypes);
    }

    internal static bool IsExactEntrancePipelineViewSignature(
        string declaringTypeName,
        string methodName,
        string returnTypeName,
        IReadOnlyList<string>? parameterTypeNames)
    {
        return string.Equals(declaringTypeName, "LevelPreviewUIView", StringComparison.Ordinal) &&
            string.Equals(methodName, "ReflectGarageVisuals", StringComparison.Ordinal) &&
            IsExactVoidSignature(parameterTypeNames, returnTypeName, EntrancePipelineViewParameterTypes);
    }

    private static bool IsExactVoidSignature(
        IReadOnlyList<string>? actualParameterTypes,
        string returnTypeName,
        IReadOnlyList<string> expectedParameterTypes)
    {
        return string.Equals(returnTypeName, "Void", StringComparison.Ordinal) &&
            actualParameterTypes is not null &&
            expectedParameterTypes.SequenceEqual(actualParameterTypes, StringComparer.Ordinal);
    }
}
