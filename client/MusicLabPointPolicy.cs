using System.Collections;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace RhythmCastleAP;

internal enum MusicLabPointCompatibilityMode
{
    LegacyNative,
    Compatible,
    IncompatibleClaim,
}

internal readonly record struct MusicLabPointCompatibilityResult(
    MusicLabPointCompatibilityMode Mode,
    string Detail);

internal readonly record struct MusicLabPointSessionIdentity(
    string Game, string Seed, string Slot, int Schema);

internal readonly record struct MusicLabPointReceipt(int Index, long ItemId);

internal enum MusicLabPointRuntimeMode
{
    Native,
    AwaitingInitialSynchronization,
    Synchronized,
    RetainedDisconnected,
    Incompatible,
}

internal readonly record struct MusicLabPointSnapshot(
    MusicLabPointRuntimeMode Mode, int Total, int AcceptedInstances, string Detail);

internal static class MusicLabPointContract
{
    private const string ClaimSuffix = "music-lab-points-0.23";
    private const int SchemaVersion = 14;
    private const int PointSchema = 1;
    private const int TotalInstances = 20;
    private const int TotalValue = 180;

    private static readonly IReadOnlyDictionary<string, int> ItemIds =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Music Lab Point"] = 187256153,
            ["Music Lab Point Bundle"] = 187256154,
            ["Music Lab Point Large Bundle"] = 187256155,
        };

    private static readonly IReadOnlyDictionary<string, int> Values =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Music Lab Point"] = 1,
            ["Music Lab Point Bundle"] = 10,
            ["Music Lab Point Large Bundle"] = 20,
        };

    private static readonly IReadOnlyDictionary<string, int> Counts =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Music Lab Point"] = 10,
            ["Music Lab Point Bundle"] = 3,
            ["Music Lab Point Large Bundle"] = 7,
        };

    private static readonly IReadOnlyDictionary<int, string> Thresholds =
        new Dictionary<int, string>
        {
            [5] = "Music Lab - 5 Point Chest",
            [10] = "Music Lab - 10 Point Chest",
            [20] = "Music Lab - 20 Point Chest",
            [32] = "Music Lab - 32 Point Chest",
            [46] = "Music Lab - 46 Point Chest",
            [64] = "Music Lab - 64 Point Chest",
            [89] = "Music Lab - 89 Point Chest",
            [111] = "Music Lab - 111 Point Chest",
            [140] = "Music Lab - 140 Point Chest",
        };

    internal static MusicLabPointCompatibilityResult ValidateSlotData(Dictionary<string, object>? slotData)
    {
        if (slotData?.TryGetValue("implementation_version", out object? versionRaw) != true ||
            UnwrapScalar(versionRaw) is not string version ||
            !version.EndsWith(ClaimSuffix, StringComparison.Ordinal))
        {
            return new(MusicLabPointCompatibilityMode.LegacyNative, "pre-v0.23 implementation");
        }

        if (!TryReadNumber(slotData, "schema_version", out int schemaVersion))
            return Fail("schema_version", SchemaVersion, Actual(slotData, "schema_version"));
        if (schemaVersion != SchemaVersion)
            return Fail("schema_version", SchemaVersion, schemaVersion);

        if (!slotData.TryGetValue("music_lab_points_enabled", out object? enabledRaw) || UnwrapScalar(enabledRaw) is not bool enabled)
            return Fail("music_lab_points_enabled", true, Actual(slotData, "music_lab_points_enabled"));
        if (!enabled)
            return Fail("music_lab_points_enabled", true, false);

        if (!TryReadNumber(slotData, "music_lab_points_schema", out int pointSchema))
            return Fail("music_lab_points_schema", PointSchema, Actual(slotData, "music_lab_points_schema"));
        if (pointSchema != PointSchema)
            return Fail("music_lab_points_schema", PointSchema, pointSchema);

        if (!TryReadStringNumberMap(slotData, "music_lab_point_items", out Dictionary<string, int> itemIds))
            return Fail("music_lab_point_items", "exact map", Actual(slotData, "music_lab_point_items"));
        MusicLabPointCompatibilityResult? itemFailure = ValidateMap("music_lab_point_items", ItemIds, itemIds);
        if (itemFailure.HasValue)
            return itemFailure.Value;
        if (itemIds.Values.Distinct().Count() != itemIds.Count)
            return Fail("music_lab_point_items", "unique permanent IDs", "duplicate ID");

        if (!TryReadStringNumberMap(slotData, "music_lab_point_values", out Dictionary<string, int> values))
            return Fail("music_lab_point_values", "exact map", Actual(slotData, "music_lab_point_values"));
        MusicLabPointCompatibilityResult? valueFailure = ValidateMap("music_lab_point_values", Values, values);
        if (valueFailure.HasValue)
            return valueFailure.Value;

        if (!TryReadStringNumberMap(slotData, "music_lab_point_counts", out Dictionary<string, int> counts))
            return Fail("music_lab_point_counts", "exact map", Actual(slotData, "music_lab_point_counts"));
        MusicLabPointCompatibilityResult? countFailure = ValidateMap("music_lab_point_counts", Counts, counts);
        if (countFailure.HasValue)
            return countFailure.Value;

        if (!TryReadNumber(slotData, "music_lab_point_total_instances", out int totalInstances))
            return Fail("music_lab_point_total_instances", TotalInstances, Actual(slotData, "music_lab_point_total_instances"));
        if (totalInstances != TotalInstances)
            return Fail("music_lab_point_total_instances", TotalInstances, totalInstances);

        if (!TryReadNumber(slotData, "music_lab_point_total_value", out int totalValue))
            return Fail("music_lab_point_total_value", TotalValue, Actual(slotData, "music_lab_point_total_value"));
        if (totalValue != TotalValue)
            return Fail("music_lab_point_total_value", TotalValue, totalValue);

        if (!TryReadNumber(slotData, "music_lab_point_max_effective", out int maxEffective))
            return Fail("music_lab_point_max_effective", TotalValue, Actual(slotData, "music_lab_point_max_effective"));
        if (maxEffective != TotalValue)
            return Fail("music_lab_point_max_effective", TotalValue, maxEffective);

        if (!TryReadNumberStringMap(slotData, "music_lab_point_thresholds", out Dictionary<int, string> thresholds))
            return Fail("music_lab_point_thresholds", "exact map", Actual(slotData, "music_lab_point_thresholds"));
        MusicLabPointCompatibilityResult? thresholdFailure = ValidateMap("music_lab_point_thresholds", Thresholds, thresholds);
        if (thresholdFailure.HasValue)
            return thresholdFailure.Value;

        return new(MusicLabPointCompatibilityMode.Compatible, "compatible");
    }

    private static MusicLabPointCompatibilityResult? ValidateMap<TKey, TValue>(
        string field,
        IReadOnlyDictionary<TKey, TValue> expected,
        IReadOnlyDictionary<TKey, TValue> actual)
        where TKey : notnull
    {
        if (actual.Count != expected.Count)
            return Fail(field, expected.Count, actual.Count);

        foreach ((TKey key, TValue expectedValue) in expected)
        {
            if (!actual.TryGetValue(key, out TValue? actualValue))
                return Fail($"{field}.{key}", expectedValue, "<missing>");
            if (!EqualityComparer<TValue>.Default.Equals(expectedValue, actualValue))
                return Fail($"{field}.{key}", expectedValue, actualValue);
        }

        return null;
    }

    private static bool TryReadNumber(Dictionary<string, object> data, string key, out int value)
    {
        value = 0;
        return data.TryGetValue(key, out object? raw) && TryConvertInt(raw, out value);
    }

    private static bool TryReadStringNumberMap(
        Dictionary<string, object> data,
        string key,
        out Dictionary<string, int> result)
    {
        result = new(StringComparer.Ordinal);
        if (!data.TryGetValue(key, out object? raw) || !TryEntries(raw, out IEnumerable<(object? Key, object? Value)> entries))
            return false;

        foreach ((object? mapKey, object? mapValue) in entries)
        {
            if (UnwrapScalar(mapKey) is not string textKey || !TryConvertInt(mapValue, out int value) || !result.TryAdd(textKey, value))
                return false;
        }
        return true;
    }

    private static bool TryReadNumberStringMap(
        Dictionary<string, object> data,
        string key,
        out Dictionary<int, string> result)
    {
        result = new();
        if (!data.TryGetValue(key, out object? raw) || !TryEntries(raw, out IEnumerable<(object? Key, object? Value)> entries))
            return false;

        foreach ((object? mapKey, object? mapValue) in entries)
        {
            if (!TryConvertInt(mapKey, out int numericKey) || UnwrapScalar(mapValue) is not string textValue || !result.TryAdd(numericKey, textValue))
                return false;
        }
        return true;
    }

    private static bool TryEntries(object? raw, out IEnumerable<(object? Key, object? Value)> entries)
    {
        if (raw is IDictionary dictionary)
        {
            entries = DictionaryEntries(dictionary);
            return true;
        }
        if (raw is IEnumerable enumerable && raw is not string)
        {
            entries = EnumerableEntries(enumerable);
            return true;
        }
        entries = Array.Empty<(object? Key, object? Value)>();
        return false;
    }

    private static IEnumerable<(object? Key, object? Value)> DictionaryEntries(IDictionary dictionary)
    {
        foreach (DictionaryEntry entry in dictionary)
            yield return (entry.Key, entry.Value);
    }

    private static IEnumerable<(object? Key, object? Value)> EnumerableEntries(IEnumerable entries)
    {
        foreach (object? entry in entries)
        {
            if (entry is null)
            {
                yield return (null, null);
                continue;
            }
            Type type = entry.GetType();
            yield return (
                type.GetProperty("Key")?.GetValue(entry) ?? type.GetProperty("Name")?.GetValue(entry),
                type.GetProperty("Value")?.GetValue(entry));
        }
    }

    private static bool TryConvertInt(object? raw, out int value)
    {
        switch (UnwrapScalar(raw))
        {
            case int integer:
                value = integer;
                return true;
            case long longValue when longValue is >= int.MinValue and <= int.MaxValue:
                value = (int)longValue;
                return true;
            case string text:
                return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
            default:
                value = 0;
                return false;
        }
    }

    // ConnectedPacket.SlotData retains nested JObject/JValue nodes in 6.7.1.
    // Unwrap only JSON scalar kinds supported by the strict CLR validation;
    // never stringify containers or coerce floating numbers and booleans.
    private static object? UnwrapScalar(object? raw) => raw is JValue value &&
        value.Type is JTokenType.Integer or JTokenType.String or JTokenType.Boolean or JTokenType.Null
            ? value.Value
            : raw;

    private static string Actual(Dictionary<string, object> data, string key) =>
        data.TryGetValue(key, out object? value) ? Display(value) : "<missing>";

    private static MusicLabPointCompatibilityResult Fail(string key, object? expected, object? actual) =>
        new(MusicLabPointCompatibilityMode.IncompatibleClaim,
            $"{Bound(key)} expected='{Bound(Display(expected))}' actual='{Bound(Display(actual))}'");

    private static string Display(object? value) => value?.ToString() ?? "<null>";

    private static string Bound(string value) => value.Length <= 160 ? value : value[..160];
}

internal sealed class MusicLabPointRuntime
{
    private static readonly IReadOnlyDictionary<long, int> ValuesById = new Dictionary<long, int>
    {
        [187256153] = 1,
        [187256154] = 10,
        [187256155] = 20,
    };

    private MusicLabPointSessionIdentity? _identity;

    internal MusicLabPointSnapshot Snapshot { get; private set; } =
        new(MusicLabPointRuntimeMode.Native, 0, 0, "native");

    internal void Configure(MusicLabPointCompatibilityResult result, MusicLabPointSessionIdentity identity)
    {
        if (result.Mode == MusicLabPointCompatibilityMode.LegacyNative)
        {
            Clear(identity, MusicLabPointRuntimeMode.Native, result.Detail);
            return;
        }
        if (result.Mode == MusicLabPointCompatibilityMode.IncompatibleClaim)
        {
            Clear(identity, MusicLabPointRuntimeMode.Incompatible, result.Detail);
            return;
        }
        if (Snapshot.Mode == MusicLabPointRuntimeMode.RetainedDisconnected && _identity == identity)
            return;

        Clear(identity, MusicLabPointRuntimeMode.AwaitingInitialSynchronization, "awaiting initial synchronization");
    }

    internal void Synchronize(MusicLabPointSessionIdentity identity, IEnumerable<MusicLabPointReceipt> receipts)
    {
        ArgumentNullException.ThrowIfNull(receipts);
        bool synchronizationAllowed = Snapshot.Mode is
            MusicLabPointRuntimeMode.AwaitingInitialSynchronization or
            MusicLabPointRuntimeMode.Synchronized or
            MusicLabPointRuntimeMode.RetainedDisconnected;
        if (!synchronizationAllowed || _identity != identity)
            return;

        HashSet<int> seenIndexes = new();
        HashSet<int> acceptedIndexes = new();
        int total = 0;
        foreach (MusicLabPointReceipt receipt in receipts)
        {
            if (!seenIndexes.Add(receipt.Index) || !ValuesById.TryGetValue(receipt.ItemId, out int value))
                continue;
            acceptedIndexes.Add(receipt.Index);
            total = Math.Min(180, total + value);
        }

        Snapshot = new(MusicLabPointRuntimeMode.Synchronized, total, acceptedIndexes.Count, "synchronized received-item history");
    }

    internal void MarkDisconnected(MusicLabPointSessionIdentity identity)
    {
        if (Snapshot.Mode != MusicLabPointRuntimeMode.Synchronized || _identity != identity)
            return;
        Snapshot = Snapshot with { Mode = MusicLabPointRuntimeMode.RetainedDisconnected, Detail = "retained while disconnected" };
    }

    internal void Reset() => Clear(null, MusicLabPointRuntimeMode.Native, "native");

    internal int ResolveEffectiveScore(int nativeScore, int? developerOverride) =>
        Snapshot.Mode switch
        {
            MusicLabPointRuntimeMode.AwaitingInitialSynchronization => 0,
            MusicLabPointRuntimeMode.Incompatible => 0,
            MusicLabPointRuntimeMode.Synchronized => Snapshot.Total,
            MusicLabPointRuntimeMode.RetainedDisconnected => Snapshot.Total,
            _ => developerOverride ?? nativeScore,
        };

    private void Clear(MusicLabPointSessionIdentity? identity, MusicLabPointRuntimeMode mode, string detail)
    {
        _identity = identity;
        Snapshot = new(mode, 0, 0, detail);
    }
}
