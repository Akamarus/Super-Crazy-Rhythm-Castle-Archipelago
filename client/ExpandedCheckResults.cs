using System.Reflection;

namespace RhythmCastleAP;

// Cache reflection metadata/identifier objects, never save-state values.
internal static class ExpandedCheckResults
{
    private static readonly object Sync = new();
    private static Assembly? _assembly;
    private static MethodInfo? _flagGetter, _resultGetter;
    private static ConstructorInfo? _levelConstructor, _variantConstructor;
    private static readonly Dictionary<string, object> Flags = new(StringComparer.Ordinal);
    private static readonly Dictionary<(string, string), object[]> Results = new();
    private static readonly HashSet<string> KnownFlags = ExpandedCheckCatalog.StarEntries
        .Where(e => !string.IsNullOrEmpty(e.Flag)).Select(e => e.Flag).ToHashSet(StringComparer.Ordinal);

    private static bool Bind()
    {
        Assembly? assembly = ReflectionUtil.GameAssembly;
        if (assembly == null) return false;
        if (ReferenceEquals(assembly, _assembly)) return true;
        _assembly = assembly; Flags.Clear(); Results.Clear();
        Type? enquiries = assembly.GetType("CurrentPlayerSaveEnquiries");
        Type? flagType = assembly.GetType("eGameProgressionFlag");
        Type? levelType = assembly.GetType("LevelIdentifier");
        Type? variantType = assembly.GetType("LevelVariantIdentifier");
        _flagGetter = enquiries != null && flagType?.IsEnum == true ? enquiries.GetMethod("GetProgressionFlagValue",
            BindingFlags.Public | BindingFlags.Static, null, new[] { flagType }, null) : null;
        if (_flagGetter?.ReturnType != typeof(bool)) _flagGetter = null;
        if (_flagGetter != null) foreach (string flag in KnownFlags)
            if (Enum.TryParse(flagType!, flag, false, out object? value) && value != null) Flags.Add(flag, value);
        _resultGetter = enquiries != null && levelType != null && variantType != null ?
            enquiries.GetMethod("HasBinaryResultLevelVariantBeenCompleted", BindingFlags.Public | BindingFlags.Static,
                null, new[] { levelType, variantType }, null) : null;
        if (_resultGetter?.ReturnType != typeof(bool)) _resultGetter = null;
        _levelConstructor = levelType?.GetConstructor(new[] { typeof(string) });
        _variantConstructor = variantType?.GetConstructor(new[] { typeof(string) });
        return true;
    }

    internal static bool? ReadFlag(string flag)
    {
        if (!KnownFlags.Contains(flag)) return null;
        lock (Sync) {
            try {
                if (!Bind() || _flagGetter == null || !Flags.TryGetValue(flag, out object? value)) return null;
                return _flagGetter.Invoke(null, new[] { value }) as bool?;
            } catch { return null; }
        }
    }

    // Binary success is native saved state, never inferred from score or stars.
    internal static bool? ReadCompleted(string level, string variant)
    {
        if (!ExpandedCheckCatalog.Entries.Any(e => e.Level == level && e.Variant == variant)) return null;
        lock (Sync) {
            try {
                if (!Bind() || _resultGetter == null || _levelConstructor == null || _variantConstructor == null) return null;
                var key = (level, variant);
                if (!Results.TryGetValue(key, out object[]? identifiers)) {
                    identifiers = new[] { _levelConstructor.Invoke(new object[] { level }), _variantConstructor.Invoke(new object[] { variant }) };
                    Results.Add(key, identifiers);
                }
                return _resultGetter.Invoke(null, identifiers) as bool?;
            } catch { return null; }
        }
    }
}
