using System.Globalization;

namespace RhythmCastleAP;

internal static class MusicLabPointRandomization
{
    private static readonly object Sync = new();
    private static readonly MusicLabPointRuntime Runtime = new();
    private static MusicLabPointSessionIdentity? _identity;
    private static long? _activeGeneration;

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
            MusicLabPointSnapshot previous = Runtime.Snapshot;
            _activeGeneration = null;
            _identity = null;
            Runtime.Reset();
            LogChange(previous);
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
        if (previous.Mode != current.Mode || previous.Total != current.Total)
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] MUSIC LAB POINTS mode={current.Mode} total={current.Total} instances={current.AcceptedInstances} detail='{current.Detail}'.");
    }
}
