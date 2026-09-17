using System.Reflection;

namespace RhythmCastleAP;

internal static class CharacterQuestItems
{
    internal static readonly CharacterQuestItemState State = new();
    [ThreadStatic] private static bool _applying;
    private static TimeSpan _untilPoll;
    private static long _revision = -1;
    private static readonly Dictionary<string, string> Last = new();
    private static readonly Dictionary<string, string> ConsumedInSave = new();
    private static MethodInfo? _maniacEnquiry;
    private static object? _maniac;

    internal static bool TryHandleItemName(string name) => CharacterQuestItemPolicy.All.Any(item => item.Name == name);

    internal static bool ShouldSuppress(object request, string flag) => !_applying &&
        CharacterQuestItemPolicy.ShouldSuppress(State.Snapshot.Enabled, DeveloperHarness.CurrentRoomId, flag,
            ReflectionUtil.ReadBool(request, "Value") == true);

    internal static void RecordConsumption(object evt, string flag)
    {
        if (_applying || !State.Snapshot.Enabled ||
            !CharacterQuestItemPolicy.All.Any(item => item.BagFlag == flag) ||
            ReflectionUtil.ReadBool(evt, "FlagWasSet") != true ||
            ReflectionUtil.ReadBool(evt, "FlagIsSet") != false) return;
        // Two bounded entries, scoped to the exact native save epoch. This closes
        // the interval between bag consumption and a later native unlock event.
        CassetteReceiptRandomization.WithStableQuestItemSave((_, identity) => ConsumedInSave[flag] = identity);
    }
    internal static void TickUnity(TimeSpan elapsed)
    {
        var snapshot = State.Snapshot;
        if (!snapshot.Enabled || !snapshot.Ready || snapshot.Owned.Count == 0) return;
        if (snapshot.Revision == _revision)
        {
            _untilPoll -= elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
            if (_untilPoll > TimeSpan.Zero) return;
        }
        _revision = snapshot.Revision;
        _untilPoll = TimeSpan.FromSeconds(1);
        // The existing cassette gate proves selected slot/pointer, gameplay readiness,
        // and processor ownership. Hold its gate and the receipt revision across reads/write.
        CassetteReceiptRandomization.WithStableQuestItemSave((processor, identity) =>
            State.WithCurrent(snapshot.Revision, () => Reconcile(processor, identity, snapshot)));
    }

    private static void Reconcile(object processor, string identity, CharacterQuestItemSnapshot snapshot)
    {
        foreach (var item in CharacterQuestItemPolicy.All)
        {
            if (!snapshot.Owned.Contains(item.Id)) continue;
            bool? held = ReadFlag(item.BagFlag);
            bool? consumed = QuestChecks.Enabled ? QuestChecks.HandInConsumed(item.Name) :
                item.Name == "Old Game Data" ? ReadManiacUnlocked() : ReadFlag("PLAYABLE_CHARACTER_UNLOCKED_MEOO");
            if (ConsumedInSave.TryGetValue(item.BagFlag, out string? consumedIdentity) && consumedIdentity == identity) consumed = true;
            string result = consumed == true ? "consumed" : held == true ? "held" : "pending";
            if (CharacterQuestItemPolicy.ShouldGrant(true, held, consumed))
            {
                bool submitted;
                string detail;
                _applying = true;
                try { submitted = WeedKillerRandomization.TrySubmitProgressionFlag(processor, item.BagFlag, true, out detail); }
                finally { _applying = false; }
                bool? verified = ReadFlag(item.BagFlag);
                result = submitted && verified == true ? "granted" : "retry";
            }
            string summary = $"save='{identity}' item='{item.Name}' result={result} held={held?.ToString() ?? "unknown"} consumed={consumed?.ToString() ?? "unknown"}";
            if (Last.TryGetValue(item.Name, out string? previous) && previous == summary) continue;
            Last[item.Name] = summary;
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] CHARACTER QUEST ITEM " + summary);
        }
    }

    private static bool? ReadFlag(string name) => RootsBucketRandomization.TryReadProgressionFlag(name, out bool value) ? value : null;

    internal static bool? ReadManiacUnlocked()
    {
        try
        {
            if (_maniacEnquiry == null)
            {
                Assembly? assembly = ReflectionUtil.GameAssembly;
                if (assembly == null) return null;
                Type? owner = ReflectionUtil.SafeGetTypes(assembly).FirstOrDefault(t => t.Name == "CurrentPlayerSaveEnquiries");
                MethodInfo? method = owner?.GetMethods(BindingFlags.Public | BindingFlags.Static).SingleOrDefault(m =>
                    m.Name == "IsCharacterUnlocked" && m.ReturnType == typeof(bool) && m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType.Name == "ePlayableCharacter");
                if (method == null) return null;
                object character = Enum.Parse(method.GetParameters()[0].ParameterType, "GUITAR_MANIAC", false);
                _maniac = character;
                _maniacEnquiry = method;
            }
            return _maniacEnquiry.Invoke(null, new[] { _maniac }) as bool?;
        }
        catch { return null; }
    }
}
