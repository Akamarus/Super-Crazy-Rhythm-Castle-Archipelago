using System.Globalization;

namespace RhythmCastleAP;

internal readonly record struct MusicLabPointRoom(
    MusicLabPointSessionIdentity Identity, MusicLabPointCompatibilityResult Contract)
{
    internal static MusicLabPointRoom Read(Dictionary<string, object>? slotData, string game, string seed, string slot)
    {
        object? rawSchema = null;
        slotData?.TryGetValue("schema_version", out rawSchema);
        int.TryParse(Convert.ToString(rawSchema, CultureInfo.InvariantCulture), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int schema);
        return new(new(game, seed, slot, schema), MusicLabPointContract.ValidateSlotData(slotData));
    }
}

internal static class MusicLabPointRandomization
{
    private static readonly object Sync = new();
    private static readonly MusicLabPointRuntime Runtime = new();
    private static MusicLabPointSessionIdentity? _identity;
    private static long? _activeGeneration;
    private static readonly HashSet<string> LoggedEvents = new(StringComparer.Ordinal);
    private static bool _getterAvailable;
    private static bool _pointContractCompatible;
    private static long? _getterRejectedGeneration;
    private const string GetterUnavailableReason = "getter unavailable; AP Music Lab Points incompatible for this session";

    internal static void Configure() => Reset();

    internal static MusicLabPointCompatibilityResult CheckCompatibility(MusicLabPointRoom room, long generation)
    {
        lock (Sync)
            return room.Contract.Mode == MusicLabPointCompatibilityMode.Compatible &&
                (!_getterAvailable || (_identity == room.Identity && _getterRejectedGeneration == generation))
                ? new(MusicLabPointCompatibilityMode.IncompatibleClaim, GetterUnavailableReason)
                : room.Contract;
    }

    internal static void Apply(MusicLabPointRoom room, long generation)
    {
        lock (Sync)
        {
            var identity = room.Identity;
            if (_identity.HasValue && _identity.Value != identity)
                Reset();
            MusicLabPointSnapshot previous = Runtime.Snapshot;
            _identity = identity;
            _activeGeneration = generation;
            _pointContractCompatible = room.Contract.Mode == MusicLabPointCompatibilityMode.Compatible;
            var compatibility = CheckCompatibility(room, generation);
            if (_pointContractCompatible && compatibility.Mode == MusicLabPointCompatibilityMode.IncompatibleClaim)
                _getterRejectedGeneration = generation;
            Runtime.Configure(compatibility, identity);
            if (compatibility.Mode == MusicLabPointCompatibilityMode.Compatible)
            {
                LogOnce("contract-accepted", "contract accepted");
                LogGetterAvailability();
            }
            else if (compatibility.Mode == MusicLabPointCompatibilityMode.IncompatibleClaim)
                LogOnce("contract-rejected", $"contract rejected: {compatibility.Detail}");
            LogChange(previous);
        }
    }

    internal static MusicLabPointSnapshot SynchronizeTotals(long generation, MusicLabPointTotals totals)
    {
        lock (Sync)
        {
            if (_activeGeneration != generation || !_identity.HasValue)
                return Runtime.Snapshot;
            MusicLabPointSnapshot previous = Runtime.Snapshot;
            var anomalies = Runtime.Synchronize(_identity.Value, totals);
            if (Runtime.Snapshot.Mode == MusicLabPointRuntimeMode.Synchronized)
            {
                if (anomalies.UnknownItem)
                    LogOnce("unknown-item", "unknown received item ID ignored for Music Lab Points");
                if (anomalies.OverCap)
                    LogOnce("cap-anomaly", "cap anomaly: received point value exceeds 180; effective total capped at 180");
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
            _pointContractCompatible = false;
            _getterRejectedGeneration = null;
            Runtime.Reset();
            LoggedEvents.Clear();
        }
    }

    internal static MusicLabPointSnapshot Snapshot
    {
        get { lock (Sync) return Runtime.Snapshot; }
    }

    internal static int ResolveEffectiveScore(int nativeScore)
    {
        lock (Sync)
            return Runtime.ResolveEffectiveScore(nativeScore);
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
            if (!available && _pointContractCompatible && _identity.HasValue)
            {
                MusicLabPointSnapshot previous = Runtime.Snapshot;
                _getterRejectedGeneration = _activeGeneration;
                Runtime.Configure(new(MusicLabPointCompatibilityMode.IncompatibleClaim, GetterUnavailableReason), _identity.Value);
                LogOnce("getter-incompatible", GetterUnavailableReason);
                LogChange(previous);
            }
            LogGetterAvailability();
        }
    }

    private static void LogGetterAvailability() => LogOnce(
        _getterAvailable ? "getter-ready" : "getter-unavailable",
        _getterAvailable
            ? "managed getter ready; effective-score replacement scoped to GameRoom_Hub6 for AP point sessions"
            : "getter unavailable; AP Music Lab Point economy cannot be active; release blocked");

    private static void LogOnce(string key, string message)
    {
        if (LoggedEvents.Add(key))
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] MUSIC LAB POINTS {message}.");
    }
}
