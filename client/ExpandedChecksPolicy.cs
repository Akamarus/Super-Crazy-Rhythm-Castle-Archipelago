using Newtonsoft.Json.Linq;
namespace RhythmCastleAP;
internal enum ExpandedChecksMode { Legacy, Enabled, Invalid }
internal static class ExpandedChecksPolicy
{
    internal const string Suffix = "-check-expansion-0.27";
    internal const string VersionSuffix = "full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26" + Suffix;
    internal static readonly IReadOnlyDictionary<long, ExpandedCheckEntry> ById = ExpandedCheckCatalog.StarEntries.ToDictionary(e => e.Id);
    internal static readonly IReadOnlyDictionary<string, ExpandedCheckEntry> ByFlag = ExpandedCheckCatalog.StarEntries.Where(e => !string.IsNullOrEmpty(e.Flag)).ToDictionary(e => e.Flag, StringComparer.Ordinal);
    internal static ExpandedChecksMode Validate(Dictionary<string, object>? data)
    {
        if (data == null) return ExpandedChecksMode.Legacy;
        try {
            var root = JObject.FromObject(data);
            string? version = root["implementation_version"]?.Type == JTokenType.String ? (string?)root["implementation_version"] : null;
            bool claim = version?.Contains("check-expansion", StringComparison.Ordinal) == true ||
                root["expanded_checks_schema"] != null || root["expanded_check_locations"] != null ||
                (root["schema_version"]?.Type == JTokenType.Integer && (long?)root["schema_version"] >= 18);
            if (!claim) return ExpandedChecksMode.Legacy;
            bool stars = root["schema_version"]?.Type == JTokenType.Integer && (long?)root["schema_version"] == 19;
            var entries = stars ? ExpandedCheckCatalog.StarEntries : ExpandedCheckCatalog.Entries;
            if (version?.EndsWith(VersionSuffix + (stars ? "-ap-stars-0.28" : ""), StringComparison.Ordinal) != true ||
                root["schema_version"]?.Type != JTokenType.Integer || (long?)root["schema_version"] != (stars ? 19 : 18) ||
                root["expanded_checks_schema"]?.Type != JTokenType.Integer || (long?)root["expanded_checks_schema"] != 1 ||
                root["expanded_check_locations"] is not JObject map || map.Count != entries.Count)
                return ExpandedChecksMode.Invalid;
            foreach (var e in entries)
                if (map[e.Name]?.Type != JTokenType.Integer || (long?)map[e.Name] != e.Id) return ExpandedChecksMode.Invalid;
            return ExpandedChecksMode.Enabled;
        } catch { return ExpandedChecksMode.Invalid; }
    }
}

// Every callback runs while the identity lock is held. Native access is separately
// protected by WithStableSelectedQuestSave before entering this state machine.
internal sealed class ExpandedChecksState
{
    private readonly object _sync = new();
    private long _generation = -1;
    private string? _identity;
    private int? _slot;
    private bool _enabled;
    private IReadOnlyList<ExpandedCheckEntry> _entries = ExpandedCheckCatalog.Entries;
    private string? _bindingReason;
    private readonly HashSet<string> _reportedBindingReasons = new(StringComparer.Ordinal);
    private readonly Dictionary<long, string> _pendingResults = new(), _pendingCharacterWrites = new();
    private readonly HashSet<long> _completed = new(), _server = new(), _sent = new(), _pendingCharacters = new();
    internal string? Identity { get { lock (_sync) return _identity; } }
    internal bool Enabled { get { lock (_sync) return _enabled; } }
    internal void Configure(long generation, string identity, bool enabled, IEnumerable<long> serverChecked,
        Func<string, ExpandedJournalRecord> load, bool includeStarChecks = false)
    {
        lock (_sync) {
            if (generation < _generation) return;
            _generation = generation;
            if (string.IsNullOrWhiteSpace(identity)) throw new ArgumentException("AP identity is required.");
            if (_identity != identity) {
                _entries = includeStarChecks ? ExpandedCheckCatalog.StarEntries : ExpandedCheckCatalog.Entries;
                _enabled = false; _identity = null; _slot = null;
                _completed.Clear(); _pendingCharacters.Clear(); _server.Clear(); _sent.Clear(); _pendingResults.Clear(); _pendingCharacterWrites.Clear();
                _bindingReason = null; _reportedBindingReasons.Clear();
                var record = enabled ? load(identity) : new ExpandedJournalRecord(null, Array.Empty<long>());
                ExpandedChecksJournal.Validate(record);
                if (record.Completed.Concat(record.PendingCharacters ?? Array.Empty<long>()).Any(id => !_entries.Any(e => e.Id == id))) throw new InvalidDataException("Journal contains sources outside the active contract.");
                _slot = record.Slot; _completed.UnionWith(record.Completed); _pendingCharacters.UnionWith(record.PendingCharacters ?? Array.Empty<long>());
            }
            _identity = identity; _generation = generation; _enabled = enabled;
            _server.UnionWith(serverChecked.Where(ExpandedChecksPolicy.ById.ContainsKey));
            _sent.Clear();
        }
    }
    internal bool Visit(int slot, Func<string, bool?> read, Action<string, ExpandedJournalRecord> persist, Action<string> queue, Func<string, string, bool?>? readResult = null, Func<string, bool?>? readCharacter = null)
    {
        lock (_sync) {
            if (!Bind(slot, read, persist, readResult, readCharacter)) return false;
            foreach (var e in _entries) {
                if (_server.Contains(e.Id)) continue;
                if ((e.Character == null || _pendingCharacters.Contains(e.Id)) && !_completed.Contains(e.Id) && Read(e, read, readResult, readCharacter) == true) Complete(e.Id, persist);
                Queue(e.Id, queue);
            }
            return true;
        }
    }
    internal bool Prebind(int slot, Func<string, bool?> read, Action<string, ExpandedJournalRecord> persist, Func<string, string, bool?>? readResult = null, Func<string, bool?>? readCharacter = null)
    { lock (_sync) return Bind(slot, read, persist, readResult, readCharacter); }
    private bool Bind(int slot, Func<string, bool?> read, Action<string, ExpandedJournalRecord> persist, Func<string, string, bool?>? readResult, Func<string, bool?>? readCharacter)
    {
        if (!_enabled || _identity == null || slot is < 1 or > 4) return false;
        if (_slot.HasValue) return _slot == slot;
        // Even server-checked markers must be clear on an unbound native save.
        foreach (var e in _entries) {
            bool? value = Read(e, read, readResult, readCharacter);
            if (value == false) continue;
            _bindingReason = value == true
                ? $"Cannot bind expanded checks: '{e.Name}' is already completed. Connect this seed before loading a fresh native save."
                : $"Cannot bind expanded checks: '{e.Name}' is unreadable. Wait for the selected save to finish loading; if this persists, report the client log.";
            return false;
        }
        persist(_identity, new(slot, _completed.OrderBy(id => id).ToArray(), _pendingCharacters.OrderBy(id => id).ToArray()));
        _slot = slot; _bindingReason = null; return true;
    }
    private static bool? Read(ExpandedCheckEntry entry, Func<string, bool?> flag, Func<string, string, bool?>? result, Func<string, bool?>? character)
        => entry.Character != null ? character?.Invoke(entry.Character) : !string.IsNullOrEmpty(entry.Flag) ? flag(entry.Flag) : entry.Level != null && entry.Variant != null ? result?.Invoke(entry.Level, entry.Variant) : null;
    internal void Observe(int slot, string room, string flag, bool? value,
        Action<string, ExpandedJournalRecord> persist, Action<string> queue)
    {
        lock (_sync) {
            if (!_enabled || _slot != slot || value != true ||
                !ExpandedChecksPolicy.ByFlag.TryGetValue(flag, out var e) || !_entries.Contains(e) || e.Room != room || _server.Contains(e.Id)) return;
            if (!_completed.Contains(e.Id)) Complete(e.Id, persist);
            Queue(e.Id, queue);
        }
    }
    internal string? CaptureResultContext(int slot, string nativeIdentity)
    {
        lock (_sync) {
            if (!_enabled || _identity == null || _slot != slot || string.IsNullOrEmpty(nativeIdentity)) return null;
            return $"{_generation}:{_identity.Length}:{_identity}:{slot}:{nativeIdentity}";
        }
    }
    internal string? TakeBindingDiagnostic()
    {
        lock (_sync) return _bindingReason != null && _reportedBindingReasons.Add(_bindingReason) ? _bindingReason : null;
    }
    internal void WithResultContext(int slot, string nativeIdentity, string context, Action action)
    {
        // Keep the generation guard and native read atomic against Configure/Reset.
        lock (_sync) if (context == CaptureResultContext(slot, nativeIdentity)) action();
    }
    internal void ObserveResult(int slot, string nativeIdentity, string level, string variant, bool? successful,
        string? context, Action<string, ExpandedJournalRecord> persist, Action<string> queue)
    {
        lock (_sync) {
            if (successful != true || context == null || context != CaptureResultContext(slot, nativeIdentity)) return;
            var entry = _entries.SingleOrDefault(e => e.Level == level && e.Variant == variant);
            if (entry == null || _server.Contains(entry.Id)) return;
            if (!_completed.Contains(entry.Id)) {
                _pendingResults[entry.Id] = nativeIdentity;
                Complete(entry.Id, persist);
                _pendingResults.Remove(entry.Id);
            }
            Queue(entry.Id, queue);
        }
    }
    internal bool Handles(long id) { lock (_sync) return _enabled && _entries.Any(e => e.Id == id); }
    internal void ObserveCharacterSource(int slot, string nativeIdentity, string character, string context,
        Action<string, ExpandedJournalRecord> persist)
    {
        lock (_sync) {
            if (context != CaptureResultContext(slot, nativeIdentity)) return;
            var entry = _entries.SingleOrDefault(e => e.Character == character);
            if (entry == null || _server.Contains(entry.Id) || _completed.Contains(entry.Id) || _pendingCharacters.Contains(entry.Id)) return;
            _pendingCharacterWrites[entry.Id] = nativeIdentity;
            PersistCharacterSource(entry.Id, persist);
        }
    }
    private void PersistCharacterSource(long id, Action<string, ExpandedJournalRecord> persist)
    {
        var pending = _pendingCharacters.Append(id).Distinct().OrderBy(value => value).ToArray();
        persist(_identity!, new(_slot, _completed.OrderBy(value => value).ToArray(), pending));
        _pendingCharacters.Add(id);
        _pendingCharacterWrites.Remove(id);
    }
    internal void RetryResults(int slot, string nativeIdentity, Action<string, ExpandedJournalRecord> persist, Action<string> queue)
    {
        lock (_sync) {
            if (!_enabled || _slot != slot) return;
            foreach (var entry in _pendingCharacterWrites.ToArray()) {
                if (entry.Value == nativeIdentity) PersistCharacterSource(entry.Key, persist);
            }
            foreach (var entry in _pendingResults.ToArray()) {
                if (entry.Value != nativeIdentity) continue;
                if (!_completed.Contains(entry.Key) && !_server.Contains(entry.Key)) Complete(entry.Key, persist);
                _pendingResults.Remove(entry.Key);
                Queue(entry.Key, queue);
            }
        }
    }
    private void Complete(long id, Action<string, ExpandedJournalRecord> persist)
    {
        var next = _completed.Append(id).OrderBy(value => value).ToArray();
        persist(_identity!, new(_slot, next, _pendingCharacters.Where(value => value != id).OrderBy(value => value).ToArray()));
        _pendingCharacters.Remove(id);
        _completed.Add(id);
    }
    private void Queue(long id, Action<string> queue)
    {
        if (!_completed.Contains(id) || _server.Contains(id) || _sent.Contains(id)) return;
        queue(ExpandedChecksPolicy.ById[id].Name);
        _sent.Add(id);
    }
    internal void Reset()
    {
        lock (_sync) {
            // Keep the generation fence so obsolete transports cannot revive state.
            _generation++; _identity = null; _slot = null; _enabled = false;
            _completed.Clear(); _pendingCharacters.Clear(); _server.Clear(); _sent.Clear(); _pendingResults.Clear(); _pendingCharacterWrites.Clear();
                _bindingReason = null; _reportedBindingReasons.Clear();
        }
    }
}
