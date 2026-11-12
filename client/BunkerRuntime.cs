namespace RhythmCastleAP;
internal static class BunkerRuntime
{
    internal static bool Ready { get; private set; }
    internal static void ReportHookAvailability(bool ready) => Ready = ready;
    internal static readonly BunkerState State = new();
    internal static readonly BunkerJournal Journal = new(Path.Combine(BepInEx.Paths.ConfigPath, "RhythmCastleAP", "bunker"));
    [ThreadStatic] private static bool _applying;
    private static TimeSpan _remaining;
    private static string? _lastError;
    internal static bool Handles(string name)
    {
        foreach (var item in BunkerCatalog.Items)
            if (item.Name == name) return true;
        return false;
    }
    private static bool? Read(string flag) => RootsBucketRandomization.TryReadProgressionFlag(flag, out bool value) ? value : null;

    // Every randomized award is intercepted at the native progression request boundary.
    // Receipt writes use the same verified native API but carry this thread-local guard.
    internal static bool BeforeProgressionRequest(object request, string flag)
    {
        if (_applying || !State.Enabled) return true;
        bool bag = BunkerCatalog.ByBagFlag.ContainsKey(flag);
        if (!bag && !BunkerCatalog.ByCheckFlag.ContainsKey(flag)) return true;
        bool? value = ReflectionUtil.ReadBool(request, "Value");
        bool allowed = !bag || value != true;
        bool prepared = false;
        WithSave((_, _) => {
            if (bag && value == true && flag == "BUNKER_HUB_DEMON_KEY" && DeveloperHarness.CurrentRoomId == "GameRoom_Hub8")
                State.Complete(flag, true, Journal.Save);
            // Persist before allowing consumption; a failed journal write must leave the native item intact.
            if (bag && value == false && Read(flag) == true) State.Consume(flag, Journal.Save);
            prepared = true;
        });
        return allowed && prepared;
    }
    internal static void Observe(object evt, string flag)
    {
        if (_applying || !State.Enabled) return;
        bool bag = BunkerCatalog.ByBagFlag.ContainsKey(flag);
        bool source = flag != "BUNKER_HUB_DEMON_KEY" && BunkerCatalog.ByCheckFlag.ContainsKey(flag);
        if (!source && !bag) return;
        var value = ReflectionUtil.ReadBool(evt, "FlagIsSet");
        if (source && value == true)
            WithSave((_, _) => State.Complete(flag, false, Journal.Save));
        if (bag && value == false && ReflectionUtil.ReadBool(evt, "FlagWasSet") == true)
            WithSave((_, _) => State.Consume(flag, Journal.Save));
    }
    internal static void Tick(TimeSpan elapsed)
    {
        if (!State.Enabled) return;
        _remaining -= elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        if (_remaining > TimeSpan.Zero) return;
        _remaining = TimeSpan.FromSeconds(1);
        WithSave((processor, _) => {
            // Recover durable source flags, never interpreting Demon ownership as pickup.
            foreach (var check in BunkerCatalog.Checks)
                if (check.Flag != "BUNKER_HUB_DEMON_KEY" && !State.IsCompleted(check.Id) && Read(check.Flag) == true)
                    State.Complete(check.Flag, false, Journal.Save);
            foreach (var item in BunkerCatalog.Items) {
                if (State.IsConsumed(item.Id)) continue;
                string? consumedFlag = item.BagFlag switch {
                    "LIFT_KEY_BAG_ITEM" => "LOBBY_HUB_LIFT_KEY_USED",
                    "KING_VIRUS_DATA_STICK_BAG_ITEM" or "KING_VIRUS_CHEAT_CODES_BAG_ITEM" => "OVERALL_PROGRESS_CREATED_GAME_ENDER",
                    "KING_VIRUS_GAME_ENDER_BAG_ITEM" => "OVERALL_PROGRESS_USED_GAME_ENDER",
                    _ => null
                };
                if (consumedFlag != null) {
                    bool? consumed = Read(consumedFlag);
                    if (consumed == null) continue;
                    if (consumed == true) State.Consume(item.BagFlag, Journal.Save);
                }
                if (!State.HasUnconsumedReceipt(item.Id) || Read(item.BagFlag) != false) continue;
                _applying = true;
                try { WeedKillerRandomization.TrySubmitProgressionFlag(processor, item.BagFlag, true, out _); }
                finally { _applying = false; }
            }
            State.Replay((name, identity) => Plugin.AP?.QueueExpandedLocation(name, identity) == true);
        });
    }
    private static void WithSave(Action<object, string> action)
    {
        try {
            CassetteReceiptRandomization.WithStableSelectedQuestSave((processor, nativeIdentity, slot) =>
                State.WithBound(slot, Read, Journal.Save, () => action(processor, nativeIdentity)));
        } catch (Exception ex) {
            string message = ex.GetBaseException().Message;
            if (_lastError == message) return;
            _lastError = message; Plugin.LoggerInstance?.LogError("[SCRC-AP] Bunker action deferred: " + message);
        }
    }
}



