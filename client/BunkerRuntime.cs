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
    internal static bool Handles(string name) => BunkerCatalog.Items.Any(i => i.Name == name);
    private static bool IsBag(string flag) => BunkerCatalog.Items.Any(i => i.BagFlag == flag);
    private static bool? Read(string flag) => RootsBucketRandomization.TryReadProgressionFlag(flag, out bool value) ? value : null;

    // Every randomized award is intercepted at the native progression request boundary.
    // Receipt writes use the same verified native API but carry this thread-local guard.
    internal static bool BeforeProgressionRequest(object request, string flag)
    {
        if (_applying || !State.Enabled) return true;
        bool? value = ReflectionUtil.ReadBool(request, "Value");
        bool bag = IsBag(flag);
        if (!bag && !BunkerCatalog.Checks.Any(c => c.Flag == flag)) return true;
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
        var value = ReflectionUtil.ReadBool(evt, "FlagIsSet");
        if (value == true && flag != "BUNKER_HUB_DEMON_KEY" && BunkerCatalog.Checks.Any(c => c.Flag == flag))
            WithSave((_, _) => State.Complete(flag, false, Journal.Save));
        if (value == false && ReflectionUtil.ReadBool(evt, "FlagWasSet") == true && IsBag(flag))
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
                if (check.Flag != "BUNKER_HUB_DEMON_KEY" && Read(check.Flag) == true) State.Complete(check.Flag, false, Journal.Save);
            foreach (var item in BunkerCatalog.Items) {
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
                if (!State.ShouldGrant(item.Id, Read(item.BagFlag))) continue;
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



