using Newtonsoft.Json.Linq;

namespace RhythmCastleAP;

internal enum QuestChecksMode { Legacy, Enabled, Invalid }
internal sealed record QuestChecksSnapshot(long Revision, bool Enabled, bool Ready,
    IReadOnlySet<long> Owned, IReadOnlySet<long> Checked, int? NativeSlot, IReadOnlySet<long> Pending);

internal static class QuestChecksPolicy
{
    internal const string Suffix = "quest-checks-0.26";
    internal const long Plunger = 187256161, Meoo = 187256162, Maniac = 187256163;
    internal const long PlungerPickup = 187256294, BatteryHandIn = 187256295,
        MemoryHandIn = 187256296, RootsStarEater = 187256297;
    internal static readonly Dictionary<string, long> Items = new(StringComparer.Ordinal) {
        ["Plunger"] = Plunger, ["Meoo"] = Meoo, ["Maniac"] = Maniac,
    };
    internal static readonly Dictionary<string, long> Locations = new(StringComparer.Ordinal) {
        ["Lobby - Plunger Pickup"] = PlungerPickup,
        ["Lobby - Car Battery Hand-In"] = BatteryHandIn,
        ["Game Garage - Old Game Data Hand-In"] = MemoryHandIn,
        ["Roots - Star Eater Fed"] = RootsStarEater,
    };
    internal static QuestChecksMode Validate(Dictionary<string, object>? data)
    {
        if (data == null) return QuestChecksMode.Legacy;
        try {
            var root = JObject.FromObject(data);
            string? version = root["implementation_version"]?.Type == JTokenType.String ? (string?)root["implementation_version"] : null;
            bool claim = version?.Contains(Suffix, StringComparison.Ordinal) == true || root["quest_checks_schema"] != null;
            if (!claim) return QuestChecksMode.Legacy;
            if (version?.EndsWith("character-quest-items-0.25-" + Suffix, StringComparison.Ordinal) != true ||
                root["schema_version"]?.Type != JTokenType.Integer || (int?)root["schema_version"] != 17 ||
                root["quest_checks_schema"]?.Type != JTokenType.Integer || (int?)root["quest_checks_schema"] != 1)
                return QuestChecksMode.Invalid;
            foreach (var pair in new[] { ("quest_items", Items), ("quest_locations", Locations) }) {
                if (root[pair.Item1] is not JObject map || map.Count != pair.Item2.Count) return QuestChecksMode.Invalid;
                foreach (var entry in pair.Item2)
                    if (map[entry.Key]?.Type != JTokenType.Integer || (long?)map[entry.Key] != entry.Value) return QuestChecksMode.Invalid;
            }
            return QuestChecksMode.Enabled;
        } catch { return QuestChecksMode.Invalid; }
    }
    internal static long? SourceForEvent(string room, string flag, bool? before, bool? after) => (room, flag, before, after) switch {
        ("GameRoom_Hub1B", "OUTSIDE_HUB_PLUNGER_COLLECTED", _, true) => PlungerPickup,
        ("GameRoom_Hub2", "ROOTS_HUB_STAR_EATER_FED", _, true) => RootsStarEater,
        ("GameRoom_Hub1A", "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM", true, false) => BatteryHandIn,
        ("GameRoom_27", "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM", true, false) => MemoryHandIn,
        _ => null,
    };
    internal static bool? ConditionResult(string path, IReadOnlySet<long> completed, Func<bool?> batteryHeld, Func<bool?> plungerCollected) => path switch {
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_TopLeft/UnlockMeooCharacter/Conditions/MeooIsUnlockedCondition" => completed.Contains(BatteryHandIn),
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_TopLeft/UnlockMeooCharacter/Conditions/HaveMeooBatteryCondition" => batteryHeld() == true,
        "Root/GameRoom_Hub1B_Logic/Objects/CollectableBagObjects/Plunger/Condition" => !completed.Contains(PlungerPickup) && plungerCollected() == false,
        _ => null,
    };
}

internal sealed class QuestChecksState
{
    private readonly object _sync = new();
    private long _generation, _revision;
    private string? _identity;
    private int? _slot;
    private bool _enabled, _ready;
    private HashSet<long> _owned = new(), _checked = new(), _pending = new();
    internal bool Enabled { get { lock (_sync) return _enabled; } }
    internal bool Ready { get { lock (_sync) return _ready; } }
    internal long Revision { get { lock (_sync) return _revision; } }
    internal int? NativeSlot { get { lock (_sync) return _slot; } }
    internal QuestChecksSnapshot Snapshot { get { lock (_sync) return new(_revision, _enabled, _ready,
        new HashSet<long>(_owned), new HashSet<long>(_checked), _slot, new HashSet<long>(_pending)); } }
    internal void Configure(long generation, string identity, bool enabled, QuestJournalRecord? restored = null)
    {
        lock (_sync) {
            if (generation < _generation) return;
            if (_identity != identity) { _checked.Clear(); _pending.Clear(); _slot = null; }
            if (restored != null) { _checked.UnionWith(restored.Completed); _pending.UnionWith(restored.Pending); _slot = restored.Slot; }
            _identity = identity; _generation = generation; _enabled = enabled;
            _ready = false; _owned.Clear(); _revision++;
        }
    }
    internal void Suspend(long generation) { lock (_sync) { if (generation < _generation) return; _generation = generation; _ready = false; _owned.Clear(); _revision++; } }
    internal void Publish(long generation, IEnumerable<long> owned, IEnumerable<long> completed)
    {
        lock (_sync) {
            if (generation != _generation || !_enabled) return;
            _owned = owned.Where(QuestChecksPolicy.Items.ContainsValue).ToHashSet();
            _checked.UnionWith(completed.Where(QuestChecksPolicy.Locations.ContainsValue));
            _pending.ExceptWith(_checked);
            _ready = true; _revision++;
        }
    }
    internal bool BindSlot(int slot, Action<string, QuestJournalRecord> persist)
    {
        lock (_sync) {
            if (!_enabled || _identity == null) return false;
            if (_slot.HasValue) return _slot.Value == slot;
            persist(_identity, new(slot, _checked.ToArray(), _pending.ToArray()));
            _slot = slot; _revision++; return true;
        }
    }
    internal bool PrepareHandIn(long id, Action<string, QuestJournalRecord> persist)
    {
        lock (_sync) {
            if (!_enabled || _identity == null || !_slot.HasValue ||
                (id != QuestChecksPolicy.BatteryHandIn && id != QuestChecksPolicy.MemoryHandIn)) return false;
            if (_checked.Contains(id) || _pending.Contains(id)) return true;
            var next = new HashSet<long>(_pending) { id };
            persist(_identity, new(_slot, _checked.ToArray(), next.ToArray()));
            _pending = next; _revision++; return true;
        }
    }
    internal bool RecordSource(long id, Action<string, QuestJournalRecord>? persist = null)
    {
        lock (_sync) {
            if (!_enabled || _identity == null || !_slot.HasValue || !QuestChecksPolicy.Locations.ContainsValue(id) || _checked.Contains(id)) return false;
            var next = new HashSet<long>(_checked) { id };
            var pending = new HashSet<long>(_pending); pending.Remove(id);
            persist?.Invoke(_identity, new(_slot, next.ToArray(), pending.ToArray()));
            _checked = next; _pending = pending; _revision++; return true;
        }
    }
    internal void CancelPending(long id, Action<string, QuestJournalRecord> persist)
    {
        lock (_sync) {
            if (_identity == null || !_pending.Contains(id)) return;
            var next = new HashSet<long>(_pending); next.Remove(id);
            persist(_identity, new(_slot, _checked.ToArray(), next.ToArray()));
            _pending = next; _revision++;
        }
    }
    internal void WithCurrent(long revision, Action action) { lock (_sync) if (_revision == revision && _enabled && _ready) action(); }
    internal void Reset() { lock (_sync) { _generation = 0; _slot = null; _identity = null; _enabled = _ready = false; _owned.Clear(); _checked.Clear(); _pending.Clear(); _revision++; } }
}
