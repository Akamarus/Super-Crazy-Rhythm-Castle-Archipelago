using System.Reflection;

namespace RhythmCastleAP;

internal static class QuestChecks
{
    internal static readonly QuestChecksState State = new();
    [ThreadStatic] private static bool _applying;
    private static TimeSpan _remaining;
    private static long _revision = -1;
    private static readonly Dictionary<string, string> Last = new();
    private static readonly Dictionary<long, string> Admitted = new();
    internal static readonly QuestChecksJournal Journal = new(Path.Combine(BepInEx.Paths.ConfigPath, "RhythmCastleAP", "quest-checks"));
    internal static bool Handles(string name) => QuestChecksPolicy.Items.ContainsKey(name);
    internal static bool Enabled => State.Enabled;
    internal static bool? HandInConsumed(string name)
    {
        var state = State.Snapshot;
        if (!state.Enabled || !state.Ready || !IsCurrentSaveBound()) return null;
        long source = name == "Old Game Data" ? QuestChecksPolicy.MemoryHandIn : QuestChecksPolicy.BatteryHandIn;
        return state.Checked.Contains(source) || state.Pending.Contains(source);
    }
    internal static bool SuppressPlunger(object request, string flag)
    {
        if (_applying || !Enabled || DeveloperHarness.CurrentRoomId != "GameRoom_Hub1B" ||
            flag != "PLUNGER_BAG_ITEM" || ReflectionUtil.ReadBool(request, "Value") != true) return false;
        WithBoundSave((_, _) => { });
        return IsCurrentSaveBound();
    }
    internal static bool SuppressCharacter(object request)
    {
        if (_applying || !Enabled || !IsCurrentSaveBound() || ReflectionUtil.ReadBool(request, "Unlocked") != true) return false;
        string? character = ReflectionUtil.ReadMember(request, "Character")?.ToString();
        return character is "GHOST_CAT" or "GUITAR_MANIAC";
    }
    internal static void Observe(object evt, string flag)
    {
        if (_applying || !Enabled) return;
        long? source = QuestChecksPolicy.SourceForEvent(DeveloperHarness.CurrentRoomId, flag,
            ReflectionUtil.ReadBool(evt, "FlagWasSet"), ReflectionUtil.ReadBool(evt, "FlagIsSet"));
        if (source.HasValue)
            WithBoundSave((_, _) => Complete(source.Value));
    }
    internal static bool IsCurrentSaveBound()
    {
        bool bound = false;
        CassetteReceiptRandomization.WithStableSelectedQuestSave((_, _, slot) => bound = State.NativeSlot == slot);
        return bound;
    }
    private static void WithBoundSave(Action<object, string> action)
    {
        CassetteReceiptRandomization.WithStableSelectedQuestSave((processor, identity, slot) => {
            try {
                if (State.Snapshot.NativeSlot == null &&
                    (ReadFlag("OUTSIDE_HUB_PLUNGER_COLLECTED") == true || ReadFlag("ROOTS_HUB_STAR_EATER_FED") == true)) {
                    Log("binding", "New quest seed needs a fresh native save; refusing existing completed source flags."); return;
                }
                if (!State.BindSlot(slot, Journal.Save)) { Log("binding", "Selected save differs from this quest seed's bound slot."); return; }
                action(processor, identity);
            } catch (Exception ex) { Log("binding", "Quest save binding deferred: " + ex.GetBaseException().Message); }
        });
    }
    internal static bool BeforeProgressionRequest(object request, string flag)
    {
        if (_applying || !Enabled) return true;
        if (ReflectionUtil.ReadBool(request, "Value") == true &&
            flag is "OUTSIDE_HUB_PLUNGER_COLLECTED" or "ROOTS_HUB_STAR_EATER_FED")
            WithBoundSave((_, _) => { });
        if (ReflectionUtil.ReadBool(request, "Value") != false) return true;
        long? source = QuestChecksPolicy.SourceForEvent(DeveloperHarness.CurrentRoomId, flag, true, false);
        if (!source.HasValue || ReadFlag(flag) != true) return true;
        bool prepared = false;
        WithBoundSave((_, _) => prepared = State.PrepareHandIn(source.Value, Journal.Save));
        return prepared;
    }
    internal static bool AdmitHandIn(long source)
    {
        if (!Enabled) return true;
        bool admitted = false;
        string flag = source == QuestChecksPolicy.BatteryHandIn ? "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM" : "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM";
        WithBoundSave((_, identity) => {
            if (ReadFlag(flag) != true) return;
            admitted = State.PrepareHandIn(source, Journal.Save);
            if (admitted) Admitted[source] = identity;
        });
        return admitted;
    }
    private static void Complete(long source)
    {
        try { if (!State.RecordSource(source, Journal.Save)) return; }
        catch (Exception ex) { Plugin.LoggerInstance?.LogError("[SCRC-AP] Cannot persist quest source: " + ex.Message); return; }
        string location = QuestChecksPolicy.Locations.Single(entry => entry.Value == source).Key;
        Admitted.Remove(source);
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] QUEST SOURCE '{location}'.");
        Plugin.AP?.QueueLocation(location);
    }
    internal static void ReplayJournal(IEnumerable<long> sources)
    {
        foreach (long id in sources)
            Plugin.AP?.QueueLocation(QuestChecksPolicy.Locations.Single(entry => entry.Value == id).Key);
    }
    internal static void Tick(TimeSpan elapsed)
    {
        if (!State.Enabled || !State.Ready) return;
        _remaining -= elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        long revision = State.Revision;
        if (revision == _revision && _remaining > TimeSpan.Zero) return;
        _remaining = TimeSpan.FromSeconds(1); _revision = revision;
        WithBoundSave((processor, identity) => {
            var current = State.Snapshot;
            State.WithCurrent(current.Revision, () => Reconcile(processor, identity, current));
        });
    }
    private static void Reconcile(object processor, string identity, QuestChecksSnapshot state)
    {
        foreach (long pending in state.Pending) {
            string flag = pending == QuestChecksPolicy.MemoryHandIn ? "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM" : "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM";
            bool? held = ReadFlag(flag);
            if (held == false) Complete(pending);
            else if (held == true && (!Admitted.TryGetValue(pending, out string? admittedIdentity) || admittedIdentity != identity))
                State.CancelPending(pending, Journal.Save);
        }
        // Durable native collection markers recover pickup/feed checks after reconnect.
        if (ReadFlag("OUTSIDE_HUB_PLUNGER_COLLECTED") == true) Complete(QuestChecksPolicy.PlungerPickup);
        if (ReadFlag("ROOTS_HUB_STAR_EATER_FED") == true) Complete(QuestChecksPolicy.RootsStarEater);
        if (state.Owned.Contains(QuestChecksPolicy.Plunger) &&
            ReadFlag("PLUNGER_BAG_ITEM") == false && ReadFlag("LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED") == false)
        {
            _applying = true;
            try { WeedKillerRandomization.TrySubmitProgressionFlag(processor, "PLUNGER_BAG_ITEM", true, out _); }
            finally { _applying = false; }
        }
        foreach (var entry in new[] { (QuestChecksPolicy.Meoo, "GHOST_CAT"), (QuestChecksPolicy.Maniac, "GUITAR_MANIAC") })
        {
            if (!state.Owned.Contains(entry.Item1)) continue;
            bool? owned = ReadCharacter(entry.Item2);
            if (owned == false) {
                _applying = true;
                try { GrantCharacter(processor, entry.Item2); }
                catch (Exception ex) { Log(entry.Item2, "grant retry: " + ex.GetBaseException().Message); }
                finally { _applying = false; }
            }
            Log(entry.Item2, $"save='{identity}' unlocked={ReadCharacter(entry.Item2)?.ToString() ?? "unknown"}");
        }
    }
    private static void Log(string key, string text) {
        if (Last.TryGetValue(key, out string? previous) && previous == text) return;
        Last[key] = text; Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] QUEST CHARACTER '{key}' {text}");
    }
    internal static bool? ReadFlag(string flag) => RootsBucketRandomization.TryReadProgressionFlag(flag, out bool value) ? value : null;
    private static bool? ReadCharacter(string character)
    {
        try {
            Type? type = ReflectionUtil.GameAssembly?.GetType("CurrentPlayerSaveEnquiries");
            MethodInfo? method = type?.GetMethods(BindingFlags.Public | BindingFlags.Static).SingleOrDefault(m =>
                m.Name == "IsCharacterUnlocked" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.Name == "ePlayableCharacter");
            return method?.Invoke(null, new[] { Enum.Parse(method.GetParameters()[0].ParameterType, character) }) as bool?;
        } catch { return null; }
    }
    private static void GrantCharacter(object processor, string character)
    {
        Type type = ReflectionUtil.GameAssembly!.GetType("RecordCharacterUnlockedValueInSaveDataRequest")!;
        object request = Activator.CreateInstance(type)!;
        PropertyInfo property = type.GetProperty("Character")!;
        property.SetValue(request, Enum.Parse(property.PropertyType, character));
        type.GetProperty("Unlocked")!.SetValue(request, true);
        MethodInfo method = processor.GetType().GetMethods().Single(m => m.Name == "ProcessRequest" &&
            m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == type);
        method.Invoke(processor, new[] { request });
    }
}
