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
    Validate(data => ((Dictionary<object, object>)data["music_lab_point_thresholds"])["5"] = "Music Lab - Changed Chest").Mode,
    "changed threshold location fails");
Equal(MusicLabPointCompatibilityMode.IncompatibleClaim,
    Validate(data =>
    {
        Dictionary<object, object> thresholds = (Dictionary<object, object>)data["music_lab_point_thresholds"];
        object location = thresholds[10];
        thresholds.Remove(10);
        thresholds[11] = location;
    }).Mode,
    "changed threshold key fails");
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

MusicLabPointRandomization.Configure();
Equal(42, MusicLabPointRandomization.ResolveEffectiveScore(42, null), "adapter starts native");
MusicLabPointRandomization.ApplySlotData(CompatibleSlotData(), "Super Crazy Rhythm Castle", "seed-a", "slot-a", 1L);
Equal(MusicLabPointRuntimeMode.AwaitingInitialSynchronization, MusicLabPointRandomization.Snapshot.Mode, "adapter validates slot data before history");
foreach (string name in new[] { "Music Lab Point", "Music Lab Point Bundle", "Music Lab Point Large Bundle" })
{
    Equal(true, MusicLabPointRandomization.TryHandleItemName(name), "exact point name is consumed");
    Equal(0, MusicLabPointRandomization.Snapshot.Total, "item names cannot increment total");
}
Equal(false, MusicLabPointRandomization.TryHandleItemName("music lab point"), "item name matching is exact");
Equal(false, MusicLabPointRandomization.TryHandleItemName("Stardust"), "unknown item is not consumed");

MusicLabPointReceipt[] history = { new(0, 187256153), new(1, 187256155), new(2, 187256155), new(3, 999999999) };
MusicLabPointRandomization.SynchronizeHistory(1L, history);
Equal(41, MusicLabPointRandomization.Snapshot.Total, "authoritative history includes each permanent-ID instance");
Equal(3, MusicLabPointRandomization.Snapshot.AcceptedInstances, "unknown IDs do not count");
int logCount = Plugin.LoggerInstance.Messages.Count;
MusicLabPointRandomization.SynchronizeHistory(1L, history);
Equal(41, MusicLabPointRandomization.Snapshot.Total, "replayed history replaces rather than increments");
Equal(logCount, Plugin.LoggerInstance.Messages.Count, "unchanged history does not log again");
MusicLabPointRandomization.OnDisconnected(99L);
Equal(MusicLabPointRuntimeMode.Synchronized, MusicLabPointRandomization.Snapshot.Mode, "stale disconnect is ignored");
MusicLabPointRandomization.OnDisconnected(1L);
Equal(MusicLabPointRuntimeMode.RetainedDisconnected, MusicLabPointRandomization.Snapshot.Mode, "disconnect retains synchronized matching identity");
MusicLabPointRandomization.SynchronizeHistory(1L, Array.Empty<MusicLabPointReceipt>());
Equal(41, MusicLabPointRandomization.Snapshot.Total, "ended generation cannot overwrite retained points");
MusicLabPointRandomization.ApplySlotData(CompatibleSlotData(), "Super Crazy Rhythm Castle", "seed-a", "slot-a", 2L);
Equal(41, MusicLabPointRandomization.Snapshot.Total, "same identity reconnect retains points before history");
MusicLabPointRandomization.SynchronizeHistory(1L, Array.Empty<MusicLabPointReceipt>());
Equal(41, MusicLabPointRandomization.Snapshot.Total, "old callback cannot overwrite reconnect");
MusicLabPointRandomization.SynchronizeHistory(2L, new[] { new MusicLabPointReceipt(0, 187256154) });
Equal(10, MusicLabPointRandomization.ResolveEffectiveScore(99, 180), "adapter score uses refreshed history");
MusicLabPointRandomization.ApplySlotData(CompatibleSlotData(), "Super Crazy Rhythm Castle", "seed-b", "slot-a", 3L);
Equal(0, MusicLabPointRandomization.Snapshot.Total, "replacement identity cannot inherit an old total");
MusicLabPointRandomization.SynchronizeHistory(2L, history);
Equal(0, MusicLabPointRandomization.Snapshot.Total, "replacement rejects previous identity callback");
MusicLabPointRandomization.SynchronizeHistory(3L, history);
MusicLabPointRandomization.Reset();
MusicLabPointRandomization.SynchronizeHistory(3L, history);
Equal(MusicLabPointRuntimeMode.Native, MusicLabPointRandomization.Snapshot.Mode, "shutdown reset rejects late history");
Equal(0, MusicLabPointRandomization.Snapshot.Total, "shutdown clears authoritative total");
MusicLabPointRandomization.ApplySlotData(new Dictionary<string, object> { ["implementation_version"] = V023 }, "Super Crazy Rhythm Castle", "seed-a", "slot-a", 4L);
MusicLabPointRandomization.SynchronizeHistory(4L, history);
Equal(0, MusicLabPointRandomization.ResolveEffectiveScore(99, 180), "invalid contract fails closed in adapter");
MusicLabPointRandomization.ApplySlotData(null, "Super Crazy Rhythm Castle", "legacy", "slot-a", 5L);
Equal(99, MusicLabPointRandomization.ResolveEffectiveScore(99, null), "legacy adapter keeps native score");
MusicLabPointRandomization.Reset();

foreach (bool experimental in new[] { false, true })
{
    bool fallbackCalled = false;
    Func<string, bool> miss = _ => false;
    Func<string, bool> point = name => MusicLabPointRandomization.TryHandleItemName(name);
    Action<string> fallback = _ => fallbackCalled = true;
    Equal(true, ReceivedItemDispatch.TryApply("Music Lab Point Bundle", experimental, miss, miss, miss, miss, miss, miss, miss, point, fallback), "point dispatch always consumes point item");
    Equal(false, fallbackCalled, "point dispatch suppresses experimental native fallback");
    Equal(0, MusicLabPointRandomization.Snapshot.Total, "point dispatch never changes authoritative state");
}

// Reproduce the two concurrent read leases used by login and ItemReceived.
// A library receipt update replaces its cached history while login holds the old
// collection. The real adapter must order acquisition and publication together.
MusicLabPointRandomization.ApplySlotData(CompatibleSlotData(), "Super Crazy Rhythm Castle", "race-seed", "slot-a", 10L);
var sessionGate = new SessionGenerationLeaseGate<object>();
var raceSession = new object();
Equal(true, sessionGate.TryAdmit(10L), "race generation admitted");
Equal(true, sessionGate.TryPublish(raceSession, 10L, _ => { }, out _), "race session published");
MusicLabPointReceipt[] cachedHistory = { new(0, 187256153) };
using var loginCapturedHistory = new ManualResetEventSlim();
using var releaseLoginHistory = new ManualResetEventSlim();
using var callbackStarted = new ManualResetEventSlim();
using var callbackAcquiredHistory = new ManualResetEventSlim();
Exception? loginError = null;
Exception? callbackError = null;
var loginThread = new Thread(() =>
{
    try
    {
        Equal(true, sessionGate.TryAcquire(raceSession, 10L, out LifecycleLease? lease), "login holds current read lease");
        using (lease)
            MusicLabPointRandomization.SynchronizeHistory(10L, () =>
            {
                MusicLabPointReceipt[] captured = Volatile.Read(ref cachedHistory);
                loginCapturedHistory.Set();
                if (!releaseLoginHistory.Wait(TimeSpan.FromSeconds(5)))
                    throw new InvalidOperationException("login history was not released");
                return captured;
            });
    }
    catch (Exception error) { loginError = error; }
}) { IsBackground = true };
var callbackThread = new Thread(() =>
{
    try
    {
        Equal(true, sessionGate.TryAcquire(raceSession, 10L, out LifecycleLease? lease), "callback holds concurrent current read lease");
        using (lease)
        {
            callbackStarted.Set();
            MusicLabPointRandomization.SynchronizeHistory(10L, () =>
            {
                callbackAcquiredHistory.Set();
                return Volatile.Read(ref cachedHistory);
            });
        }
    }
    catch (Exception error) { callbackError = error; }
}) { IsBackground = true };
bool callbackReadWhileLoginPaused;
loginThread.Start();
try
{
    Equal(true, loginCapturedHistory.Wait(TimeSpan.FromSeconds(5)), "login captures old cached history");
    Volatile.Write(ref cachedHistory, new[] { new MusicLabPointReceipt(0, 187256153), new MusicLabPointReceipt(1, 187256155) });
    callbackThread.Start();
    Equal(true, callbackStarted.Wait(TimeSpan.FromSeconds(5)), "new callback enters under concurrent read lease");
    var deadline = System.Diagnostics.Stopwatch.StartNew();
    var spinner = new SpinWait();
    while ((callbackThread.ThreadState & (ThreadState.WaitSleepJoin | ThreadState.Stopped)) == 0)
    {
        if (deadline.Elapsed > TimeSpan.FromSeconds(5))
            throw new InvalidOperationException("callback did not reach synchronization boundary");
        spinner.SpinOnce();
    }
    callbackReadWhileLoginPaused = callbackAcquiredHistory.IsSet;
}
finally
{
    releaseLoginHistory.Set();
    Equal(true, loginThread.Join(TimeSpan.FromSeconds(5)), "login history publication completes");
    if ((callbackThread.ThreadState & ThreadState.Unstarted) == 0)
        Equal(true, callbackThread.Join(TimeSpan.FromSeconds(5)), "callback history publication completes");
}
if (loginError != null) throw new InvalidOperationException("login worker failed", loginError);
if (callbackError != null) throw new InvalidOperationException("callback worker failed", callbackError);
Equal(21, MusicLabPointRandomization.Snapshot.Total, "older same-generation login history cannot overwrite newer callback history");
Equal(false, callbackReadWhileLoginPaused, "history acquisition waits for prior acquisition and publication");
Equal(true, callbackAcquiredHistory.IsSet, "new callback acquires history after login publishes");
MusicLabPointRandomization.Reset();
Console.WriteLine("PASS: same_generation_login_callback_history_is_serialized");
Console.WriteLine("Music Lab Point policy and adapter tests passed.");
