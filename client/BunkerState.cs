namespace RhythmCastleAP;
// Native access enters through the selected-save gate, then this lock, always in that order.
internal sealed class BunkerState
{
    private readonly object _sync = new();
    private long _generation = -1;
    private bool _enabled, _ready;
    private string? _identity;
    private int? _slot;
    private readonly HashSet<long> _sent = new();
    private HashSet<long> _owned = new(), _consumed = new(), _completed = new();
    internal bool Enabled { get { lock (_sync) return _enabled; } }
    internal bool Ready { get { lock (_sync) return _enabled && _ready; } }
    internal string? Identity { get { lock (_sync) return _identity; } }
    internal IReadOnlySet<long> Completed { get { lock (_sync) return new HashSet<long>(_completed); } }
    internal void Configure(long generation, string identity, bool enabled, Func<string, BunkerRecord> load)
    {
        lock (_sync) {
            if (generation < _generation) return;
            _generation = generation; _enabled = _ready = false; _owned.Clear(); _sent.Clear();
            var record = enabled ? load(identity) : new(null, Array.Empty<long>(), Array.Empty<long>());
            BunkerJournal.Validate(record);
            _identity = identity; _slot = record.Slot; _consumed = record.Consumed.ToHashSet(); _completed = record.Completed.ToHashSet(); _enabled = enabled;
        }
    }
    internal void Publish(long generation, IEnumerable<long> history)
    {
        lock (_sync) {
            if (generation != _generation || !_enabled) return;
            _owned = history.Where(id => BunkerCatalog.Items.Any(i => i.Id == id)).ToHashSet(); _ready = true;
        }
    }
    internal void Suspend(long generation)
    {
        lock (_sync) { if (generation < _generation) return; _generation = generation; _ready = false; _owned.Clear(); }
    }
    internal void Reset() { lock (_sync) { _generation = -1; _enabled = _ready = false; _identity = null; _slot = null; _owned.Clear(); _consumed.Clear(); _completed.Clear(); _sent.Clear(); } }
    internal bool Bind(int slot, Func<string, bool?> read, Action<string, BunkerRecord> save)
    {
        lock (_sync) {
            if (!_enabled || _identity == null || slot is < 1 or > 4) return false;
            if (_slot.HasValue) return _slot == slot;
            // A new seed cannot inherit old source or randomized inventory flags.
            if (BunkerCatalog.Checks.Select(c => c.Flag).Concat(BunkerCatalog.Items.Select(i => i.BagFlag)).Distinct().Any(f => read(f) != false)) return false;
            save(_identity, new(slot, _consumed.ToArray(), _completed.ToArray())); _slot = slot; return true;
        }
    }
    internal void WithBound(int slot, Func<string, bool?> read, Action<string, BunkerRecord> save, Action action)
    { lock (_sync) if (Bind(slot, read, save)) action(); }
    internal bool ShouldGrant(long item, bool? held)
    { lock (_sync) return _enabled && _ready && _slot.HasValue && held == false && _owned.Contains(item) && !_consumed.Contains(item); }
    internal void Consume(string flag, Action<string, BunkerRecord> save)
    {
        lock (_sync) {
            var item = BunkerCatalog.Items.FirstOrDefault(i => i.BagFlag == flag);
            if (!_enabled || !_slot.HasValue || _identity == null || item == null || _consumed.Contains(item.Id)) return;
            var next = _consumed.Append(item.Id).ToHashSet(); save(_identity, new(_slot, next.ToArray(), _completed.ToArray())); _consumed = next;
        }
    }
    internal void Replay(Func<string, string, bool> queue)
    {
        lock (_sync) {
            if (!_enabled || _identity == null || !_slot.HasValue) return;
            foreach (var check in BunkerCatalog.Checks)
                if (_completed.Contains(check.Id) && !_sent.Contains(check.Id) && queue(check.Name, _identity)) _sent.Add(check.Id);
        }
    }
    internal void Complete(string flag, bool nativeAward, Action<string, BunkerRecord> save)
    {
        lock (_sync) {
            if (flag == "BUNKER_HUB_DEMON_KEY" && !nativeAward) return;
            var check = BunkerCatalog.Checks.FirstOrDefault(c => c.Flag == flag);
            if (!_enabled || !_slot.HasValue || _identity == null || check == null || _completed.Contains(check.Id)) return;
            var next = _completed.Append(check.Id).ToHashSet(); save(_identity, new(_slot, _consumed.ToArray(), next.ToArray())); _completed = next;
        }
    }
}


