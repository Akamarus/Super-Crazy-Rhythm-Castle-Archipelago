using Newtonsoft.Json.Linq;

namespace RhythmCastleAP;

internal enum CharacterQuestItemMode { Legacy, Enabled, Invalid }
internal sealed record CharacterQuestItemDefinition(string Name, long Id, string BagFlag, string Location);
internal sealed record CharacterQuestItemSnapshot(long Revision, bool Enabled, bool Ready, IReadOnlySet<long> Owned);

internal static class CharacterQuestItemPolicy
{
    internal const string Suffix = "character-quest-items-0.25";
    internal static readonly CharacterQuestItemDefinition[] All = {
        new("Old Game Data", 187256159, "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM", "Music Lab - 5 Point Chest"),
        new("Car Battery", 187256160, "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM", "Music Lab - 20 Point Chest"),
    };
    internal static CharacterQuestItemMode Validate(Dictionary<string, object>? data)
    {
        if (data == null) return CharacterQuestItemMode.Legacy;
        try
        {
            var root = JObject.FromObject(data);
            string? version = root["implementation_version"]?.Type == JTokenType.String ? (string?)root["implementation_version"] : null;
            bool claim = version?.Contains(Suffix, StringComparison.Ordinal) == true ||
                root["randomize_character_quest_items"]?.Value<bool>() == true || root["character_quest_item_schema"] != null;
            if (!claim) return CharacterQuestItemMode.Legacy;
            bool apStars = version?.EndsWith("full-level-mapping-0.24-" + Suffix + "-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28", StringComparison.Ordinal) == true;
            bool batchChecks = apStars || version?.EndsWith("full-level-mapping-0.24-" + Suffix + "-quest-checks-0.26-check-expansion-0.27", StringComparison.Ordinal) == true;
            bool expanded = batchChecks || version?.EndsWith("full-level-mapping-0.24-" + Suffix + "-quest-checks-0.26", StringComparison.Ordinal) == true;
            if ((!expanded && version?.EndsWith("full-level-mapping-0.24-" + Suffix, StringComparison.Ordinal) != true) ||
                root["schema_version"]?.Type != JTokenType.Integer || (int?)root["schema_version"] != (apStars ? 19 : batchChecks ? 18 : expanded ? 17 : 16) ||
                root["character_quest_item_schema"]?.Type != JTokenType.Integer || (int?)root["character_quest_item_schema"] != 1 ||
                root["randomize_character_quest_items"]?.Type != JTokenType.Boolean || (bool?)root["randomize_character_quest_items"] != true)
                return CharacterQuestItemMode.Invalid;
            foreach (string key in new[] { "character_quest_items", "character_quest_item_locations" })
            {
                if (root[key] is not JObject map || map.Count != All.Length) return CharacterQuestItemMode.Invalid;
                foreach (var item in All)
                    if (map[item.Name]?.Type != JTokenType.String || (string?)map[item.Name] !=
                        (key == "character_quest_items" ? item.BagFlag : item.Location)) return CharacterQuestItemMode.Invalid;
            }
            return CharacterQuestItemMode.Enabled;
        }
        catch { return CharacterQuestItemMode.Invalid; }
    }
    internal static bool ShouldSuppress(bool enabled, string room, string flag, bool value) =>
        enabled && value && room == "GameRoom_Hub6" && IsBagFlag(flag);
    internal static bool IsBagFlag(string flag)
    {
        foreach (var item in All)
            if (item.BagFlag == flag) return true;
        return false;
    }
    internal static bool ShouldGrant(bool owned, bool? held, bool? consumed) => owned && held == false && consumed == false;
}

// Only complete, authenticated packet history may replace ownership. Native-save
// state stays outside this network-thread object and is read on Unity's thread.
internal sealed class CharacterQuestItemState
{
    private readonly object _sync = new();
    private long _generation, _revision;
    private bool _enabled, _ready;
    private HashSet<long> _owned = new();
    internal bool Enabled { get { lock (_sync) return _enabled; } }
    internal long Revision { get { lock (_sync) return _revision; } }
    internal CharacterQuestItemSnapshot Snapshot { get { lock (_sync) return new(_revision, _enabled, _ready, new HashSet<long>(_owned)); } }
    internal void Configure(long generation, bool enabled)
    {
        lock (_sync)
        {
            if (generation < _generation) return;
            _generation = generation; _enabled = enabled; _ready = false; _owned.Clear(); _revision++;
        }
    }
    internal void Suspend(long generation)
    {
        lock (_sync)
        {
            if (generation < _generation) return;
            _generation = generation; _ready = false; _owned.Clear(); _revision++;
        }
    }
    internal void Publish(long generation, IEnumerable<long> history)
    {
        lock (_sync)
        {
            if (generation != _generation || !_enabled) return;
            _owned = history.Where(id => CharacterQuestItemPolicy.All.Any(item => item.Id == id)).ToHashSet();
            _ready = true; _revision++;
        }
    }
    internal void WithCurrent(long revision, Action action)
    {
        lock (_sync) if (_revision == revision && _enabled && _ready) action();
    }
    internal void Reset()
    {
        lock (_sync) { _generation = 0; _enabled = _ready = false; _owned.Clear(); _revision++; }
    }
}
