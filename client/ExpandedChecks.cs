namespace RhythmCastleAP;
internal static class ExpandedChecks
{
    internal static readonly ExpandedChecksState State = new();
    internal static readonly ExpandedChecksJournal Journal = new(Path.Combine(BepInEx.Paths.ConfigPath, "RhythmCastleAP", "expanded-checks"));
    private static TimeSpan _remaining;
    private static string? _lastError;
    internal static void Configure(long generation, string identity, bool enabled, IEnumerable<long> serverChecked, bool includeStarChecks = false)
    { State.Configure(generation, identity, enabled, serverChecked, Journal.Load, includeStarChecks); _remaining = TimeSpan.Zero; }
    internal static void Reset() { State.Reset(); _remaining = TimeSpan.Zero; _lastError = null; }
    internal static void BeforeProgressionRequest(object request, string flag)
    {
        if (!State.Enabled || !ExpandedChecksPolicy.ByFlag.TryGetValue(flag, out var e) ||
            e.Room != DeveloperHarness.CurrentRoomId || ReflectionUtil.ReadBool(request, "Value") != true) return;
        WithSave(slot => State.Prebind(slot, ExpandedCheckResults.ReadFlag, Journal.Save, ExpandedCheckResults.ReadCompleted, KingUnlockSource.ReadCharacter));
    }
    internal static void Observe(object evt, string flag)
    {
        if (!State.Enabled || !ExpandedChecksPolicy.ByFlag.TryGetValue(flag, out var e) ||
            e.Room != DeveloperHarness.CurrentRoomId) return;
        bool? value = ReflectionUtil.ReadBool(evt, "FlagIsSet");
        if (value != true) return;
        WithSave(slot => State.Observe(slot, DeveloperHarness.CurrentRoomId, flag, value, Journal.Save, Queue));
    }
    internal static string? BeforeKingUnlock()
    {
        if (!State.Handles(KingUnlockSource.LocationId) || DeveloperHarness.CurrentRoomId != "GameRoom_28B") return null;
        string? context = null;
        WithNativeSave((slot, nativeIdentity) => {
            if (State.Prebind(slot, ExpandedCheckResults.ReadFlag, Journal.Save, ExpandedCheckResults.ReadCompleted, KingUnlockSource.ReadCharacter)
                && KingUnlockSource.ReadCharacter("KING") == false)
                context = State.CaptureResultContext(slot, nativeIdentity);
        });
        return context;
    }
    internal static void AfterKingUnlock(string? context)
    {
        if (context == null || !State.Handles(KingUnlockSource.LocationId)) return;
        WithNativeSave((slot, nativeIdentity) => State.WithResultContext(slot, nativeIdentity, context, () => {
            State.ObserveCharacterSource(slot, nativeIdentity, "KING", context, Journal.Save);
            State.Visit(slot, ExpandedCheckResults.ReadFlag, Journal.Save, Queue, ExpandedCheckResults.ReadCompleted, KingUnlockSource.ReadCharacter);
        }));
    }
    internal static string? CaptureResultContext()
    {
        string? context = null;
        WithNativeSave((slot, nativeIdentity) => context = State.CaptureResultContext(slot, nativeIdentity));
        return context;
    }
    internal static void ObserveResult(string level, string variant, bool? successful, string? context)
    {
        if (!State.Enabled || successful != true || context == null) return;
        WithNativeSave((slot, nativeIdentity) => State.ObserveResult(slot, nativeIdentity, level, variant, successful, context, Journal.Save, Queue));
    }
    internal static void ObservePersistedResult(string level, string variant, string? context)
    {
        if (!State.Enabled || context == null) return;
        WithNativeSave((slot, nativeIdentity) => State.WithResultContext(slot, nativeIdentity, context, () => {
            bool? successful = ExpandedCheckResults.ReadCompleted(level, variant);
            State.ObserveResult(slot, nativeIdentity, level, variant, successful, context, Journal.Save, Queue);
        }));
    }
    internal static void Tick(TimeSpan elapsed)
    {
        if (!State.Enabled) return;
        _remaining -= elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        if (_remaining > TimeSpan.Zero) return;
        _remaining = TimeSpan.FromSeconds(1);
        WithNativeSave((slot, nativeIdentity) => {
            State.RetryResults(slot, nativeIdentity, Journal.Save, Queue);
            State.Visit(slot, ExpandedCheckResults.ReadFlag, Journal.Save, Queue, ExpandedCheckResults.ReadCompleted, KingUnlockSource.ReadCharacter);
        });
    }
    private static void Queue(string name)
    {
        if (Plugin.AP == null) throw new InvalidOperationException("AP check queue is unavailable.");
        string? identity = State.Identity;
        if (identity == null || !Plugin.AP.QueueExpandedLocation(name, identity))
            throw new InvalidOperationException("Expanded check queue deferred until the matching AP identity is active.");
    }
    private static void WithSave(Action<int> action) => WithNativeSave((slot, _) => action(slot));
    private static void WithNativeSave(Action<int, string> action)
    {
        try {
            CassetteReceiptRandomization.WithStableSelectedQuestSave((_, nativeIdentity, slot) => {
                action(slot, nativeIdentity);
                string? diagnostic = State.TakeBindingDiagnostic();
                if (diagnostic != null) Plugin.LoggerInstance?.LogError("[SCRC-AP] " + diagnostic);
            });
        } catch (Exception ex) {
            string message = ex.GetBaseException().Message;
            if (_lastError == message) return;
            _lastError = message;
            Plugin.LoggerInstance?.LogError("[SCRC-AP] Expanded checks deferred: " + message);
        }
    }
}
