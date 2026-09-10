using System.Reflection;

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
    private const int MaxEntrancePreviewResults = 8;
    private static readonly string[] EntrancePreviewViewParameterTypes =
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
        bool effectiveOwned = enabled && compatible
            ? apOwned
            : nativeOwned;
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

    internal static bool TryApplyEntrancePreviewOwnership(
        object? previewData,
        bool enabled,
        bool compatible,
        IEnumerable<string>? apOwnedSongs,
        out int changedCount)
    {
        changedCount = 0;
        if (previewData == null)
            return false;

        try
        {
            PropertyInfo? resultsProperty = previewData.GetType().GetProperty(
                "PreviousGarageResults",
                BindingFlags.Public | BindingFlags.Instance);
            object? results = resultsProperty?.CanRead == true
                ? resultsProperty.GetValue(previewData)
                : null;
            if (results == null)
                return false;

            Type resultsType = results.GetType();
            PropertyInfo? countProperty = resultsType.GetProperty(
                "Count",
                BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo? indexer = resultsType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .SingleOrDefault(property =>
                    string.Equals(property.Name, "Item", StringComparison.Ordinal) &&
                    property.CanRead &&
                    property.GetIndexParameters() is ParameterInfo[] parameters &&
                    parameters.Length == 1 &&
                    parameters[0].ParameterType == typeof(int));
            if (countProperty?.CanRead != true ||
                countProperty.GetValue(results) is not int count ||
                count < 0 ||
                count > MaxEntrancePreviewResults ||
                indexer == null)
            {
                return false;
            }

            var pending = new List<(object Result, PropertyInfo OwnedProperty, bool NativeOwned, bool EffectiveOwned)>();
            for (int index = 0; index < count; index++)
            {
                object? result = indexer.GetValue(results, new object[] { index });
                if (result == null)
                    return false;

                Type resultType = result.GetType();
                PropertyInfo? cartridgeTypeProperty = resultType.GetProperty(
                    "cartridgeType",
                    BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo? cartridgeOwnedProperty = resultType.GetProperty(
                    "cartridgeOwned",
                    BindingFlags.Public | BindingFlags.Instance);
                object? cartridgeTypeValue = cartridgeTypeProperty?.CanRead == true
                    ? cartridgeTypeProperty.GetValue(result)
                    : null;
                object? cartridgeOwnedValue = cartridgeOwnedProperty?.CanRead == true
                    ? cartridgeOwnedProperty.GetValue(result)
                    : null;
                string nativeCartridgeType = cartridgeTypeValue?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(nativeCartridgeType) ||
                    cartridgeOwnedProperty?.CanWrite != true ||
                    cartridgeOwnedProperty.PropertyType != typeof(bool) ||
                    cartridgeOwnedValue is not bool nativeOwned ||
                    !GarageCartridgeNativePolicy.AllCartridges.Any(cartridge =>
                        string.Equals(
                            cartridge.NativeCartridgeType,
                            nativeCartridgeType,
                            StringComparison.Ordinal)))
                {
                    return false;
                }

                if (TryResolveEntrancePreviewOwned(
                        enabled,
                        compatible,
                        nativeCartridgeType,
                        nativeOwned,
                        apOwnedSongs,
                        out GarageEntrancePreviewOwnership ownership) &&
                    ownership.EffectiveOwned != nativeOwned)
                {
                    pending.Add((result, cartridgeOwnedProperty, nativeOwned, ownership.EffectiveOwned));
                }
            }

            var applied = new List<(object Result, PropertyInfo OwnedProperty, bool NativeOwned)>();
            try
            {
                foreach (var mutation in pending)
                {
                    mutation.OwnedProperty.SetValue(mutation.Result, mutation.EffectiveOwned);
                    applied.Add((mutation.Result, mutation.OwnedProperty, mutation.NativeOwned));
                }
            }
            catch
            {
                foreach (var mutation in applied.AsEnumerable().Reverse())
                {
                    try { mutation.OwnedProperty.SetValue(mutation.Result, mutation.NativeOwned); }
                    catch { }
                }
                return false;
            }

            changedCount = pending.Count;
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static bool IsExactEntrancePreviewViewSignature(
        string declaringTypeName,
        string methodName,
        string returnTypeName,
        IReadOnlyList<string>? parameterTypeNames)
    {
        return string.Equals(declaringTypeName, "LevelPreviewUIView", StringComparison.Ordinal) &&
            string.Equals(methodName, "ReflectGarageVisuals", StringComparison.Ordinal) &&
            IsExactVoidSignature(parameterTypeNames, returnTypeName, EntrancePreviewViewParameterTypes);
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
