using System.Globalization;

namespace RhythmCastleAP;

internal static class MusicLabPointRandomization
{
    private static readonly object Sync = new();
    private static readonly MusicLabPointRuntime Runtime = new();
    private static MusicLabPointSessionIdentity? _identity;
    private static long? _activeGeneration;
    private static readonly HashSet<string> LoggedEvents = new(StringComparer.Ordinal);
    private static bool _getterAvailable;

    internal static void Configure() => Reset();

    internal static MusicLabPointSnapshot ApplySlotData(
        Dictionary<string, object>? slotData,
        string game,
        string seed,
        string slot,
        long generation)
    {
        lock (Sync)
        {
            MusicLabPointCompatibilityResult compatibility = MusicLabPointContract.ValidateSlotData(slotData);
            object? rawSchema = null;
            slotData?.TryGetValue("schema_version", out rawSchema);
            int.TryParse(Convert.ToString(rawSchema, CultureInfo.InvariantCulture), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int schema);
            MusicLabPointSessionIdentity identity = new(game, seed, slot, schema);
            if (_identity.HasValue && _identity.Value != identity)
                Reset();
            MusicLabPointSnapshot previous = Runtime.Snapshot;
            _identity = identity;
            _activeGeneration = generation;
            Runtime.Configure(compatibility, identity);
            if (compatibility.Mode == MusicLabPointCompatibilityMode.Compatible)
            {
                LogOnce("contract-accepted", "contract accepted");
                LogGetterAvailability();
            }
            else if (compatibility.Mode == MusicLabPointCompatibilityMode.IncompatibleClaim)
                LogOnce("contract-rejected", $"contract rejected: {compatibility.Detail}");
            LogChange(previous);
            return Runtime.Snapshot;
        }
    }

    internal static MusicLabPointSnapshot SynchronizeHistory(
        long generation,
        Func<IEnumerable<MusicLabPointReceipt>> readReceipts)
    {
        lock (Sync)
        {
            if (_activeGeneration != generation || !_identity.HasValue)
                return Runtime.Snapshot;
            // AllItemsReceived is a replaceable cached collection. Acquire it
            // under the publication lock so a paused older read cannot follow
            // a newer callback's publication within the same generation.
            return SynchronizeHistory(generation, readReceipts());
        }
    }

    internal static MusicLabPointSnapshot SynchronizeHistory(
        long generation,
        IEnumerable<MusicLabPointReceipt> receipts)
    {
        lock (Sync)
        {
            if (_activeGeneration != generation || !_identity.HasValue)
                return Runtime.Snapshot;
            // Enumerate under the same lock as configuration and other rebuilds;
            // no deferred history enumeration escapes into the runtime.
            MusicLabPointReceipt[] history = receipts.ToArray();
            MusicLabPointSnapshot previous = Runtime.Snapshot;
            Runtime.Synchronize(_identity.Value, history);
            if (Runtime.Snapshot.Mode == MusicLabPointRuntimeMode.Synchronized)
            {
                LogHistoryAnomalies(history);
                if (previous.Mode == MusicLabPointRuntimeMode.RetainedDisconnected)
                    LogOnce("reconnect-rebuild", $"reconnect rebuild previous={previous.Total} total={Runtime.Snapshot.Total} corrected={previous.Total != Runtime.Snapshot.Total}");
            }
            LogChange(previous);
            return Runtime.Snapshot;
        }
    }

    internal static bool TryHandleItemName(string itemName) => itemName is
        "Music Lab Point" or "Music Lab Point Bundle" or "Music Lab Point Large Bundle";

    internal static void OnDisconnected(long generation)
    {
        lock (Sync)
        {
            if (_activeGeneration != generation || !_identity.HasValue)
                return;
            MusicLabPointSnapshot previous = Runtime.Snapshot;
            _activeGeneration = null;
            Runtime.MarkDisconnected(_identity.Value);
            LogChange(previous);
        }
    }

    internal static void Reset()
    {
        lock (Sync)
        {
            _activeGeneration = null;
            _identity = null;
            Runtime.Reset();
            LoggedEvents.Clear();
        }
    }

    internal static MusicLabPointSnapshot Snapshot
    {
        get { lock (Sync) return Runtime.Snapshot; }
    }

    internal static int ResolveEffectiveScore(int nativeScore, int? developerOverride)
    {
        lock (Sync)
            return Runtime.ResolveEffectiveScore(nativeScore, developerOverride);
    }

    private static void LogChange(MusicLabPointSnapshot previous)
    {
        MusicLabPointSnapshot current = Runtime.Snapshot;
        if (previous.Mode != current.Mode || previous.Total != current.Total || previous.AcceptedInstances != current.AcceptedInstances)
            LogOnce($"state:{current.Mode}:{current.Total}:{current.AcceptedInstances}",
                $"mode={current.Mode} total={current.Total} instances={current.AcceptedInstances} detail='{current.Detail}'");
    }

    internal static void ReportGetterAvailability(bool available)
    {
        lock (Sync)
        {
            _getterAvailable = available;
            LogGetterAvailability();
        }
    }

    private static void LogGetterAvailability() => LogOnce(
        _getterAvailable ? "getter-ready" : "getter-unavailable",
        _getterAvailable
            ? "managed getter ready; effective-score replacement scoped to GameRoom_Hub6 for AP point sessions"
            : "getter unavailable; AP Music Lab Point economy cannot be active; release blocked");

    private static void LogHistoryAnomalies(MusicLabPointReceipt[] history)
    {
        HashSet<int> seen = new();
        long weightedTotal = 0;
        foreach (MusicLabPointReceipt receipt in history)
        {
            if (!seen.Add(receipt.Index))
                continue;
            // The permanent SCRC item registry spans +101 through +155.
            // Other registered items are normal receipts, not point anomalies.
            if (receipt.ItemId < 187256101 || receipt.ItemId > 187256155)
                LogOnce("unknown-item", "unknown received item ID ignored for Music Lab Points");
            weightedTotal += receipt.ItemId switch
            {
                187256153 => 1,
                187256154 => 10,
                187256155 => 20,
                _ => 0,
            };
        }
        if (weightedTotal > 180)
            LogOnce("cap-anomaly", "cap anomaly: received point value exceeds 180; effective total capped at 180");
    }

    private static void LogOnce(string key, string message)
    {
        if (LoggedEvents.Add(key))
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] MUSIC LAB POINTS {message}.");
    }
}
