using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

static void Contains(string expectedPart, string actual, string scenario)
{
    if (!actual.Contains(expectedPart, StringComparison.Ordinal))
        throw new InvalidOperationException($"{scenario}: expected detail containing '{expectedPart}', got '{actual}'");
}

const string V023 =
    "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22-music-lab-points-0.23";
const string V022 =
    "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18-consolidated-preview-0.19-difficulty-filtering-0.20-vanilla-vampire-garage-0.21-full-cassettes-0.22";

static Dictionary<string, object> CompatibleSlotData()
{
    return new(StringComparer.Ordinal)
    {
        ["implementation_version"] = V023,
        ["schema_version"] = 14L,
        ["music_lab_points_enabled"] = true,
        ["music_lab_points_schema"] = "1",
        ["music_lab_point_items"] = new Dictionary<string, object>
        {
            ["Music Lab Point"] = 187256153,
            ["Music Lab Point Bundle"] = "187256154",
            ["Music Lab Point Large Bundle"] = 187256155L,
        },
        ["music_lab_point_values"] = new Dictionary<string, object>
        {
            ["Music Lab Point"] = 1,
            ["Music Lab Point Bundle"] = "10",
            ["Music Lab Point Large Bundle"] = 20L,
        },
        ["music_lab_point_counts"] = new Dictionary<string, object>
        {
            ["Music Lab Point"] = "10",
            ["Music Lab Point Bundle"] = 3L,
            ["Music Lab Point Large Bundle"] = 7,
        },
        ["music_lab_point_total_instances"] = "20",
        ["music_lab_point_total_value"] = 180L,
        ["music_lab_point_max_effective"] = 180,
        ["music_lab_point_thresholds"] = new Dictionary<object, object>
        {
            ["5"] = "Music Lab - 5 Point Chest",
            [10] = "Music Lab - 10 Point Chest",
            [20L] = "Music Lab - 20 Point Chest",
            [32] = "Music Lab - 32 Point Chest",
            [46] = "Music Lab - 46 Point Chest",
            [64] = "Music Lab - 64 Point Chest",
            [89] = "Music Lab - 89 Point Chest",
            [111] = "Music Lab - 111 Point Chest",
            [140] = "Music Lab - 140 Point Chest",
        },
    };
}

static MusicLabPointCompatibilityResult Validate(Action<Dictionary<string, object>> mutate)
{
    Dictionary<string, object> data = CompatibleSlotData();
    mutate(data);
    return MusicLabPointContract.ValidateSlotData(data);
}

Equal(MusicLabPointCompatibilityMode.LegacyNative,
    MusicLabPointContract.ValidateSlotData(new() { ["implementation_version"] = V022 }).Mode,
    "v0.22 retains native points");
Equal(MusicLabPointCompatibilityMode.Compatible,
    MusicLabPointContract.ValidateSlotData(CompatibleSlotData()).Mode,
    "complete v0.23 contract is compatible");

MusicLabPointCompatibilityResult missing = Validate(data => data.Remove("music_lab_point_total_value"));
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim, missing.Mode, "missing field fails closed");
Contains("music_lab_point_total_value", missing.Detail, "missing field identifies key");

Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => ((Dictionary<string, object>)data["music_lab_point_items"])["Unexpected Point"] = 99).Mode,
    "extra item-map entry fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => data["schema_version"] = 13).Mode,
    "old top-level schema fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => data["music_lab_points_schema"] = 0).Mode,
    "old point schema fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => data["music_lab_points_enabled"] = false).Mode,
    "feature flag off fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data =>
    {
        Dictionary<string, object> items = (Dictionary<string, object>)data["music_lab_point_items"];
        object id = items["Music Lab Point"];
        items.Remove("Music Lab Point");
        items["Renamed Music Lab Point"] = id;
    }).Mode,
    "renamed point item fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => ((Dictionary<string, object>)data["music_lab_point_items"])["Music Lab Point"] = 187256999).Mode,
    "changed point ID fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => ((Dictionary<string, object>)data["music_lab_point_values"])["Music Lab Point Bundle"] = 11).Mode,
    "changed point value fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => ((Dictionary<string, object>)data["music_lab_point_counts"])["Music Lab Point Large Bundle"] = 8).Mode,
    "changed point count fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => data["music_lab_point_total_instances"] = 19).Mode,
    "wrong instance total fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => data["music_lab_point_total_value"] = 179).Mode,
    "wrong value total fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => data["music_lab_point_max_effective"] = 179).Mode,
    "wrong effective total fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => data["music_lab_point_total_value"] = true).Mode,
    "boolean is not accepted as a numeric value");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => ((Dictionary<object, object>)data["music_lab_point_thresholds"])[5] = "Music Lab - Changed Chest").Mode,
    "changed threshold location fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data => ((Dictionary<string, object>)data["music_lab_point_items"])["Music Lab Point Bundle"] = 187256153).Mode,
    "duplicate permanent ID fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    MusicLabPointContract.ValidateSlotData(new() { ["implementation_version"] = V023 }).Mode,
    "v0.23 claim without point sub-contract fails");

MusicLabPointSessionIdentity identity = new("Super Crazy Rhythm Castle", "seed-a", "slot-a", 14);
MusicLabPointSessionIdentity differentIdentity = new("Super Crazy Rhythm Castle", "seed-b", "slot-a", 14);
MusicLabPointCompatibilityResult compatible = MusicLabPointContract.ValidateSlotData(CompatibleSlotData());
MusicLabPointCompatibilityResult legacy = MusicLabPointContract.ValidateSlotData(new() { ["implementation_version"] = V022 });
MusicLabPointCompatibilityResult malformed = MusicLabPointContract.ValidateSlotData(new() { ["implementation_version"] = V023 });

MusicLabPointRuntime runtime = new();
runtime.Configure(legacy, identity);
Equal(MusicLabPointRuntimeMode.Native, runtime.Snapshot.Mode, "legacy config is native");
Equal(42, runtime.ResolveEffectiveScore(42, null), "native uses native score");
Equal(73, runtime.ResolveEffectiveScore(42, 73), "developer override applies only in native mode");

runtime.Configure(compatible, identity);
Equal(MusicLabPointRuntimeMode.AwaitingInitialSynchronization, runtime.Snapshot.Mode, "compatible config awaits history");
Equal(0, runtime.Snapshot.Total, "awaiting total is zero");
Equal(0, runtime.ResolveEffectiveScore(42, 73), "awaiting ignores native and override");

runtime.Synchronize(identity, new[]
{
    new MusicLabPointReceipt(1, 187256153),
    new MusicLabPointReceipt(2, 187256154),
    new MusicLabPointReceipt(3, 187256155),
});
Equal(MusicLabPointRuntimeMode.Synchronized, runtime.Snapshot.Mode, "history synchronizes");
Equal(31, runtime.Snapshot.Total, "weighted receipts sum");
Equal(3, runtime.Snapshot.AcceptedInstances, "accepted instances count");
Equal(31, runtime.ResolveEffectiveScore(42, 73), "synchronized total wins over override");

runtime.Synchronize(identity, new[]
{
    new MusicLabPointReceipt(1, 187256153),
    new MusicLabPointReceipt(1, 187256155),
    new MusicLabPointReceipt(2, 187256154),
    new MusicLabPointReceipt(3, 999999999),
});
Equal(11, runtime.Snapshot.Total, "duplicate receipt indexes cannot change sum");
Equal(2, runtime.Snapshot.AcceptedInstances, "unknown IDs are ignored");

runtime.Synchronize(identity, Enumerable.Range(0, 30).Select(index => new MusicLabPointReceipt(index, 187256155)));
Equal(180, runtime.Snapshot.Total, "point total saturates at cap");
Equal(30, runtime.Snapshot.AcceptedInstances, "all known receipt instances are counted");

runtime.MarkDisconnected(identity);
Equal(MusicLabPointRuntimeMode.RetainedDisconnected, runtime.Snapshot.Mode, "disconnect retains synchronized total");
Equal(180, runtime.ResolveEffectiveScore(42, 73), "retained total wins over override");
runtime.Configure(compatible, identity);
Equal(MusicLabPointRuntimeMode.RetainedDisconnected, runtime.Snapshot.Mode, "same identity reconnect retains total");
runtime.Synchronize(identity, new[]
{
    new MusicLabPointReceipt(1, 187256153),
    new MusicLabPointReceipt(2, 187256155),
});
Equal(MusicLabPointRuntimeMode.Synchronized, runtime.Snapshot.Mode, "same identity reconnect accepts refreshed history");
Equal(21, runtime.Snapshot.Total, "refreshed history replaces retained total");
runtime.Configure(compatible, differentIdentity);
Equal(MusicLabPointRuntimeMode.AwaitingInitialSynchronization, runtime.Snapshot.Mode, "different identity resets to awaiting");
Equal(0, runtime.Snapshot.Total, "different identity clears retained total");

runtime.Configure(malformed, identity);
Equal(MusicLabPointRuntimeMode.Incompatible, runtime.Snapshot.Mode, "malformed v0.23 enters incompatible mode");
Equal(0, runtime.Snapshot.Total, "malformed v0.23 has zero total");
Equal(0, runtime.ResolveEffectiveScore(42, 73), "incompatible ignores native and override");

runtime.Reset();
Equal(MusicLabPointRuntimeMode.Native, runtime.Snapshot.Mode, "reset returns to native");
Equal(0, runtime.Snapshot.AcceptedInstances, "reset clears receipts");

Console.WriteLine("Music Lab Point policy tests passed.");
