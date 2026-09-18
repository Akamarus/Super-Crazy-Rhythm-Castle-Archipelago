using System.Reflection;

namespace RhythmCastleAP;

internal static class RootsBucketRandomization
{
    internal const string HipGlassesItem = "Hip Glasses";
    internal const string ChickenBucketItem = "Chicken Bucket";
    internal const string HipGlassesLocation = "Roots - Level 4 - Hip Glasses";
    internal const string BucketTradeLocation = "Roots - Bucket Minion Trade";
    internal const string HipGlassesNativeFlag = "HIP_GLASSES_BAG_ITEM";
    internal const string HipGlassesSourceFlag = "LEVEL_08_GLASSES_COLLECTED";
    internal const string BucketTradeFlag = "ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES";
    internal const string ChickenBucketNativeFlag = "CHICKEN_BUCKET_BAG_ITEM";
    internal const string ChickenConsumedFlag = "LEVEL_09_COMBO_ABILITY_EARNED";

    private static readonly object Sync = new();
    private static object? _playerSaveRequestProcessor;
    private static bool _slotDataSynchronized;
    private static bool _compatible;
    private static int _hipGlassesReceived;
    private static int _chickenBucketReceived;
    private static bool _hipGrantAppliedThisProcess;
    private static bool _chickenGrantAppliedThisProcess;
    private static bool _nativeProbePendingLogged;
    private static string? _selectedSaveIdentity;
    private static TimeSpan _pollRemaining;
    private static MethodInfo? _flagGetter;
    private static readonly Dictionary<string, object> FlagArguments = new(StringComparer.Ordinal);

    [ThreadStatic]
    private static int _applyingArchipelagoGrant;

    internal static void Configure()
    {
        lock (Sync)
        {
            _playerSaveRequestProcessor = null;
            _slotDataSynchronized = false;
            _compatible = false;
            _hipGlassesReceived = 0;
            _chickenBucketReceived = 0;
            _hipGrantAppliedThisProcess = false;
            _chickenGrantAppliedThisProcess = false;
            _nativeProbePendingLogged = false;
            _selectedSaveIdentity = null;
            _pollRemaining = TimeSpan.Zero;
        }
    }

    internal static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        string implementation = ReadSlotString(slotData, "implementation_version");
        bool requested = ReadSlotBool(slotData, "randomize_hip_glasses_chicken_bucket");
        bool metadataMatches =
            string.Equals(ReadSlotString(slotData, "hip_glasses_item"), HipGlassesItem, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "chicken_bucket_item"), ChickenBucketItem, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "hip_glasses_source_location"), HipGlassesLocation, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "bucket_minion_trade_location"), BucketTradeLocation, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "hip_glasses_source_flag"), HipGlassesSourceFlag, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "hip_glasses_native_flag"), HipGlassesNativeFlag, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "bucket_trade_flag"), BucketTradeFlag, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "chicken_bucket_native_flag"), ChickenBucketNativeFlag, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "combo_bucket_conversion_flag"), ChickenConsumedFlag, StringComparison.Ordinal);
        bool compatible = RootsBucketRandomizationPolicy.IsCompatible(implementation, requested) && metadataMatches;

        lock (Sync)
        {
            _slotDataSynchronized = true;
            _compatible = compatible;
        }

        Plugin.LoggerInstance?.LogWarning(
            compatible
                ? $"[SCRC-AP] ROOTS BUCKET RANDOMIZATION ENABLED implementation='{implementation}'. Level 4 and Bucket Minion sources are AP checks; received inventory reconciles against native consumption markers."
                : $"[SCRC-AP] ROOTS BUCKET RANDOMIZATION disabled implementation='{implementation}' requested={requested} metadataMatches={metadataMatches}; native behavior remains unchanged.");

    }

    internal static bool TryApplyItem(string itemName)
    {
        bool hip = string.Equals(itemName, HipGlassesItem, StringComparison.Ordinal);
        bool chicken = string.Equals(itemName, ChickenBucketItem, StringComparison.Ordinal);
        if (!hip && !chicken)
            return false;

        int count;
        lock (Sync)
        {
            if (hip)
                count = ++_hipGlassesReceived;
            else
                count = ++_chickenBucketReceived;
        }

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] ROOTS BUCKET ITEM RECEIVED item='{itemName}' receivedCount={count}.");
        return true;
    }

    internal static void CapturePlayerSaveRequestProcessor(object? instance)
    {
        if (instance == null ||
            !string.Equals(instance.GetType().Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal))
            return;

        lock (Sync)
            _playerSaveRequestProcessor = instance;
    }

    // Only the Unity update may deliver items, using the verified selected save.
    internal static void TickUnity(TimeSpan elapsed)
    {
        _pollRemaining -= elapsed;
        if (_pollRemaining > TimeSpan.Zero) return;
        _pollRemaining = TimeSpan.FromSeconds(1);
        CassetteReceiptRandomization.WithStableQuestItemSave((processor, identity) =>
        {
            lock (Sync)
            {
                if (_selectedSaveIdentity != identity)
                {
                    _selectedSaveIdentity = identity;
                    _hipGrantAppliedThisProcess = false;
                    _chickenGrantAppliedThisProcess = false;
                    _nativeProbePendingLogged = false;
                }
                _playerSaveRequestProcessor = processor;
            }
            // The legacy consumable also needs a retry without a native progression event.
            WeedKillerRandomization.CapturePlayerSaveRequestProcessor(processor);
            WeedKillerRandomization.TryFlushPendingNativeGrant();
            TryFlushPendingNativeGrants();
        });
    }

    internal static void TryFlushPendingNativeGrants()
    {
        if (_applyingArchipelagoGrant != 0)
            return;

        object? processor;
        bool synchronized;
        bool compatible;
        int hipReceived;
        int chickenReceived;
        bool hipApplied;
        bool chickenApplied;
        lock (Sync)
        {
            processor = _playerSaveRequestProcessor;
            synchronized = _slotDataSynchronized;
            compatible = _compatible;
            hipReceived = _hipGlassesReceived;
            chickenReceived = _chickenBucketReceived;
            hipApplied = _hipGrantAppliedThisProcess;
            chickenApplied = _chickenGrantAppliedThisProcess;
        }

        if (!synchronized || !compatible || processor == null || (hipReceived <= 0 && chickenReceived <= 0))
            return;

        if (!TryReadProgressionFlag(HipGlassesNativeFlag, out bool hipHeld) ||
            !TryReadProgressionFlag(BucketTradeFlag, out bool hipConsumed) ||
            !TryReadProgressionFlag(ChickenBucketNativeFlag, out bool chickenHeld) ||
            !TryReadProgressionFlag(ChickenConsumedFlag, out bool chickenConsumed))
        {
            bool log;
            lock (Sync)
            {
                log = !_nativeProbePendingLogged;
                _nativeProbePendingLogged = true;
            }
            if (log)
                Plugin.LoggerInstance?.LogInfo("[SCRC-AP] ROOTS BUCKET reconciliation deferred until native progression enquiries are available.");
            return;
        }

        lock (Sync)
            _nativeProbePendingLogged = false;

        RootsBucketReconciliation result = RootsBucketRandomizationPolicy.Reconcile(
            synchronized,
            compatible,
            new RootsBucketLifecycleState(
                hipReceived,
                chickenReceived,
                hipHeld || hipApplied,
                hipConsumed,
                chickenHeld || chickenApplied,
                chickenConsumed));

        ApplyGrantDecision(processor, HipGlassesItem, HipGlassesNativeFlag, result.HipGlasses, hip: true);
        ApplyGrantDecision(processor, ChickenBucketItem, ChickenBucketNativeFlag, result.ChickenBucket, hip: false);
    }

    private static void ApplyGrantDecision(
        object processor,
        string itemName,
        string flagName,
        RootsBucketGrantDecision decision,
        bool hip)
    {
        if (decision == RootsBucketGrantDecision.Consumed)
        {
            return;
        }
        if (decision != RootsBucketGrantDecision.Apply)
            return;

        _applyingArchipelagoGrant++;
        bool submitted;
        string detail;
        try
        {
            submitted = WeedKillerRandomization.TrySubmitProgressionFlag(processor, flagName, true, out detail);
        }
        finally
        {
            _applyingArchipelagoGrant--;
        }

        if (!submitted)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] ROOTS BUCKET native grant pending item='{itemName}' flag='{flagName}'. {detail}");
            return;
        }

        lock (Sync)
        {
            if (hip)
                _hipGrantAppliedThisProcess = true;
            else
                _chickenGrantAppliedThisProcess = true;
        }
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS BUCKET NATIVE GRANT APPLIED item='{itemName}' flag='{flagName}'. {detail}");
    }

    internal static bool ShouldSuppressVanillaGrant(object request, string flag)
    {
        bool synchronized;
        bool compatible;
        lock (Sync)
        {
            synchronized = _slotDataSynchronized;
            compatible = _compatible;
        }

        bool value = ReflectionUtil.ReadBool(request, "Value") ?? false;
        bool suppress = _applyingArchipelagoGrant == 0 &&
                        RootsBucketRandomizationPolicy.ShouldSuppressVanillaGrant(
                            synchronized,
                            compatible,
                            DeveloperHarness.CurrentRoomId,
                            flag,
                            value);
        if (suppress)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] ROOTS BUCKET VANILLA GRANT SUPPRESSED flag='{flag}' room='{DeveloperHarness.CurrentRoomId}'. Native story progression continues; inventory must come from Archipelago.");
        }
        return suppress;
    }

    internal static void RecordSourceCollected(object request, string flag)
    {
        bool active;
        lock (Sync)
            active = _slotDataSynchronized && _compatible;
        if (!active || !(ReflectionUtil.ReadBool(request, "Value") ?? false))
            return;

        string? location = null;
        if (string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_08", StringComparison.Ordinal) &&
            string.Equals(flag, HipGlassesSourceFlag, StringComparison.Ordinal))
            location = HipGlassesLocation;
        else if (string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_Hub2", StringComparison.Ordinal) &&
                 string.Equals(flag, BucketTradeFlag, StringComparison.Ordinal))
            location = BucketTradeLocation;

        if (location == null)
            return;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS BUCKET SOURCE AP CHECK flag='{flag}' location='{location}'. Native marker retained.");
        Plugin.AP?.QueueLocation(location);
    }

    internal static bool TryReadProgressionFlag(string flagName, out bool value)
    {
        value = false;
        lock (Sync)
        {
            try
            {
                Assembly? assembly = ReflectionUtil.GameAssembly;
                if (assembly == null) return false;
                Type? flagType = assembly.GetType("eGameProgressionFlag");
                if (flagType?.IsEnum != true) return false;
                _flagGetter ??= assembly.GetType("CurrentPlayerSaveEnquiries")?.GetMethod(
                    "GetProgressionFlagValue", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { flagType }, null);
                if (_flagGetter?.ReturnType != typeof(bool)) return false;
                if (!FlagArguments.TryGetValue(flagName, out object? argument))
                {
                    if (!Enum.TryParse(flagType, flagName, false, out argument) || argument == null) return false;
                    FlagArguments.Add(flagName, argument);
                }
                if (_flagGetter.Invoke(null, new[] { argument }) is not bool result) return false;
                value = result;
                return true;
            }
            catch { return false; }
        }
    }

    private static string ReadSlotString(Dictionary<string, object>? slotData, string key) =>
        slotData != null && slotData.TryGetValue(key, out object? raw)
            ? raw?.ToString() ?? string.Empty
            : string.Empty;

    private static bool ReadSlotBool(Dictionary<string, object>? slotData, string key)
    {
        if (slotData == null || !slotData.TryGetValue(key, out object? raw) || raw == null)
            return false;
        if (raw is bool value)
            return value;
        return bool.TryParse(raw.ToString(), out bool parsed) && parsed;
    }
}
