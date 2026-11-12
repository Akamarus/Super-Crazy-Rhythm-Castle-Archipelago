namespace RhythmCastleAP;

internal sealed record NotificationReceipt(string Item, string Player, string Location, bool Own);
internal sealed record ItemNotification(string Text, string Location, bool Sent);

// Network callbacks update managed state. History is bounded; unseen popups are retained until displayed.
internal sealed class ItemNotificationFeed
{
    internal const int HistoryLimit = 100;
    private readonly object _sync = new();
    private readonly List<ItemNotification> _history = new();
    private readonly Queue<ItemNotification> _pending = new();
    private readonly List<(ItemNotification Entry, double Remaining)> _visible = new();
    private readonly HashSet<string> _sent = new(StringComparer.Ordinal);
    private string? _identity;
    private long _generation;
    private int _nextReceipt;
    private bool _baseline;
    internal int OutstandingCount { get { lock (_sync) return _pending.Count + _visible.Count; } }
    // Render snapshots can outlive the lock while a network callback adds more items.
    // The countdown alone never changes which entries the overlay draws.
    private IReadOnlyList<ItemNotification>? _historySnapshot = Array.Empty<ItemNotification>();
    private IReadOnlyList<ItemNotification>? _visibleSnapshot = Array.Empty<ItemNotification>();
    internal IReadOnlyList<ItemNotification> History
    {
        get
        {
            lock (_sync)
            {
                if (_historySnapshot == null)
                {
                    var entries = _history.ToArray();
                    Array.Reverse(entries);
                    _historySnapshot = Array.AsReadOnly(entries);
                }
                return _historySnapshot;
            }
        }
    }
    internal IReadOnlyList<ItemNotification> Visible
    {
        get
        {
            lock (_sync)
                return _visibleSnapshot ??= _visible.Count == 0 ? Array.Empty<ItemNotification>() :
                    Array.AsReadOnly(_visible.Select(card => card.Entry).ToArray());
        }
    }
    internal void Connect(long generation, string identity)
    {
        lock (_sync)
        {
            if (generation < _generation) return;
            if (_identity != identity) { Clear(); _identity = identity; }
            _generation = generation;
        }
    }
    internal int? FirstUnseenOffset(long generation, int index, int count)
    {
        lock (_sync)
        {
            if (_identity == null || generation != _generation || index < 0 || count < 0 || count > int.MaxValue - index ||
                (!_baseline && index != 0) || (_baseline && index > _nextReceipt)) return null;
            // The silent first sync only displays its latest 100 rows. Its full count still sets the cursor.
            if (!_baseline) return Math.Max(0, count - HistoryLimit);
            int offset = Math.Max(0, _nextReceipt - index);
            return offset >= count ? null : offset;
        }
    }
    internal void Receive(long generation, int index, IReadOnlyList<NotificationReceipt> items, int firstOffset = 0)
    {
        lock (_sync)
        {
            int count = checked(firstOffset + items.Count);
            if (FirstUnseenOffset(generation, index, count) is not int skip || skip < firstOffset) return;
            bool popup = _baseline;
            for (int i = skip - firstOffset; i < items.Count; i++)
            {
                var receipt = items[i];
                string text = receipt.Own ? $"Found {Clean(receipt.Item)}" :
                    $"Received {Clean(receipt.Item)} from {Clean(receipt.Player)}";
                Add(new(text, Clean(receipt.Location), false), popup);
            }
            _nextReceipt = Math.Max(_nextReceipt, index + count);
            _baseline = true;
        }
    }
    internal void Send(long generation, string key, string item, string player, string location)
    {
        lock (_sync)
        {
            if (_identity == null || generation != _generation || !_sent.Add(key)) return;
            // Keep source keys for this identity: a full seed exceeds 256 checks,
            // and reconnect replay must not requeue already-announced rewards.
            Add(new($"Sent {Clean(item)} to {Clean(player)}", Clean(location), true), true);
        }
    }
    internal void Advance(double seconds, int displayedCount = 3)
    {
        lock (_sync)
        {
            for (int i = Math.Min(_visible.Count, Math.Max(0, displayedCount)) - 1; i >= 0; i--)
            {
                var v = _visible[i]; double remaining = v.Remaining - Math.Max(0, seconds);
                if (remaining <= 0)
                {
                    _visible.RemoveAt(i);
                    _visibleSnapshot = null;
                }
                else _visible[i] = (v.Entry, remaining);
            }
            while (_visible.Count < 3 && _pending.Count > 0)
            {
                _visible.Add((_pending.Dequeue(), 6));
                _visibleSnapshot = null;
            }
        }
    }
    internal void Reset() { lock (_sync) { Clear(); _identity = null; _generation = 0; } }
    private void Add(ItemNotification entry, bool popup)
    {
        _historySnapshot = null;
        _history.Add(entry); if (_history.Count > HistoryLimit) _history.RemoveAt(0);
        if (!popup) return;
        _pending.Enqueue(entry); // Never silently discard a reward before its popup has been displayed.
    }
    private void Clear()
    {
        _history.Clear(); _pending.Clear(); _visible.Clear(); _sent.Clear();
        _historySnapshot = _visibleSnapshot = Array.Empty<ItemNotification>();
        _nextReceipt = 0; _baseline = false;
    }
    internal static string Clean(string? value)
    {
        string clean = new((value ?? "Unknown").Where(c => !char.IsControl(c)).Take(160).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "Unknown" : clean;
    }
}
