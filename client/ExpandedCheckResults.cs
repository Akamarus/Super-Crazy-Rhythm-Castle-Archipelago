using System.Reflection;

namespace RhythmCastleAP;

// Cache reflection metadata/identifier objects, never save-state values.
internal static class ExpandedCheckResults
{
    private static readonly object Sync = new();
    private static Assembly? _assembly;
    private static MethodInfo? _resultGetter;
    private static ConstructorInfo? _levelConstructor, _variantConstructor;
    private static readonly Dictionary<(string, string), object[]> Results = new();
    private static readonly HashSet<string> KnownFlags = ExpandedCheckCatalog.StarEntries
        .Where(e => !string.IsNullOrEmpty(e.Flag)).Select(e => e.Flag).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<(string, string)> KnownResults = ExpandedCheckCatalog.Entries
        .Where(entry => entry.Level != null && entry.Variant != null)
        .Select(entry => (entry.Level!, entry.Variant!)).ToHashSet();

    private static bool Bind()
    {
        Assembly? assembly = ReflectionUtil.GameAssembly;
        if (assembly == null) return false;
        if (ReferenceEquals(assembly, _assembly)) return true;
        _assembly = assembly; Results.Clear();
        Type? enquiries = assembly.GetType("CurrentPlayerSaveEnquiries");
        Type? levelType = assembly.GetType("LevelIdentifier");
        Type? variantType = assembly.GetType("LevelVariantIdentifier");
        _resultGetter = enquiries != null && levelType != null && variantType != null ?
            enquiries.GetMethod("HasBinaryResultLevelVariantBeenCompleted", BindingFlags.Public | BindingFlags.Static,
                null, new[] { levelType, variantType }, null) : null;
        if (_resultGetter?.ReturnType != typeof(bool)) _resultGetter = null;
        _levelConstructor = levelType?.GetConstructor(new[] { typeof(string) });
        _variantConstructor = variantType?.GetConstructor(new[] { typeof(string) });
        return true;
    }

    internal static bool? ReadFlag(string flag) => KnownFlags.Contains(flag) ? NativeProgressionFlags.Read(flag) : null;

    // Binary success is native saved state, never inferred from score or stars.
    internal static bool? ReadCompleted(string level, string variant)
    {
        var key = (level, variant);
        if (!KnownResults.Contains(key)) return null;
        lock (Sync) {
            try {
                if (!Bind() || _resultGetter == null || _levelConstructor == null || _variantConstructor == null) return null;
                if (!Results.TryGetValue(key, out object[]? identifiers)) {
                    identifiers = new[] { _levelConstructor.Invoke(new object[] { level }), _variantConstructor.Invoke(new object[] { variant }) };
                    Results.Add(key, identifiers);
                }
                return _resultGetter.Invoke(null, identifiers) as bool?;
            } catch { return null; }
        }
    }
}
