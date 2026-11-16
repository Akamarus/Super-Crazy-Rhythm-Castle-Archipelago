namespace RhythmCastleAP;

// Current seeds share the quest journal's freshness and slot checks. Historical seeds
// without quest checks acquire their own durable slot on their first verified selection.
internal static class NativeGrantSaveBinding
{
    private static readonly object Sync = new();
    private static readonly QuestChecksJournal Legacy = new(Path.Combine(BepInEx.Paths.ConfigPath, "RhythmCastleAP", "native-grant-slots"));
    private static string? _identity;
    private static bool _questChecks, _connected;
    private static int? _slot;
    private static string? _lastError;
    [ThreadStatic] internal static object? CurrentProcessor;

    internal static Action Prepare(string identity, bool questChecks)
    {
        int? restored = questChecks ? null : Legacy.Load(identity).Slot;
        return () => {
            lock (Sync) {
                _identity = identity;
                _connected = true;
                _questChecks = questChecks;
                _slot = restored;
                _lastError = null;
            }
        };
    }

    internal static void Disconnect() { lock (Sync) _connected = false; }

    internal static void WithBoundSave(Action<object, string> action) =>
        WithSave(true, (processor, save, _) => action(processor, save));

    internal static void ObserveBoundSave(Action<object, string> action) =>
        WithSave(false, (processor, save, _) => action(processor, save));

    internal static bool IsCurrentSaveBound()
    {
        bool admitted = false;
        WithCheckSave(_ => admitted = true);
        return admitted;
    }

    internal static void WithCheckSave(Action<string> action) =>
        WithSave(false, (_, _, roomIdentity) => action(roomIdentity));

    internal static void WithCheckSave(Action<string, string> action) =>
        WithSave(false, (_, save, roomIdentity) => action(roomIdentity, save));

    private static void WithSave(bool requireConnection, Action<object, string, string> action)
    {
        CassetteReceiptRandomization.WithStableSelectedQuestSave((processor, save, slot) => {
            lock (Sync) {
                if (_identity == null || (requireConnection && !_connected) || slot is < 1 or > 4) return;
                try {
                    if (!_connected) {
                        if (slot != (_questChecks ? QuestChecks.State.NativeSlot : _slot)) return;
                    }
                    else if (_questChecks) {
                        if (!QuestChecks.TryBindSlot(slot)) return;
                    }
                    else {
                        if (!_slot.HasValue) {
                            Legacy.Save(_identity, new(slot, Array.Empty<long>(), Array.Empty<long>()));
                            _slot = slot;
                        }
                        if (_slot != slot) {
                            Report("Select the native save slot bound to this room.");
                            return;
                        }
                    }
                }
                catch (IOException ex) { Report(ex.Message); return; }
                catch (UnauthorizedAccessException ex) { Report(ex.Message); return; }
                _lastError = null;
                object? previous = CurrentProcessor;
                CurrentProcessor = processor;
                try { action(processor, save, _identity); }
                finally { CurrentProcessor = previous; }
            }
        });
    }

    private static void Report(string message)
    {
        if (_lastError == message) return;
        _lastError = message;
        Plugin.LoggerInstance?.LogError("[SCRC-AP] Native item grant deferred. " + message);
    }
}
