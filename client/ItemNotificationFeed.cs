namespace RhythmCastleAP;

internal sealed record NotificationReceipt(string Item, string Player, string Location, bool Own);
internal sealed record ItemNotification(string Text, string Location, bool Sent);

// Network callbacks update managed state. History is bounded; unseen popups are retained until displayed.
internal sealed class ItemNotificationFeed
{
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
    internal ItemNotification[] History { get { lock (_sync) return _history.AsEnumerable().Reverse().ToArray(); } }
    internal ItemNotification[] Visible { get { lock (_sync) return _visible.Count == 0 ? Array.Empty<ItemNotification>() : _visible.Select(v => v.Entry).ToArray(); } }
    internal void Connect(long generation, string identity)
    {
        lock (_sync)
        {
            if (generation < _generation) return;
            if (_identity != identity) { Clear(); _identity = identity; }
            _generation = generation;
        }
    }
    internal void Receive(long generation, int index, IReadOnlyList<NotificationReceipt> items)
    {
        lock (_sync)
        {
            if (_identity == null || generation != _generation || index < 0 ||
                (!_baseline && index != 0) || (_baseline && index > _nextReceipt)) return;
            bool popup = _baseline;
            int skip = Math.Max(0, _nextReceipt - index);
            for (int i = skip; i < items.Count; i++)
            {
                var receipt = items[i];
                string text = receipt.Own ? $"Found {Clean(receipt.Item)}" :
                    $"Received {Clean(receipt.Item)} from {Clean(receipt.Player)}";
                Add(new(text, Clean(receipt.Location), false), popup);
            }
            _nextReceipt = Math.Max(_nextReceipt, index + items.Count);
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
                if (remaining <= 0) _visible.RemoveAt(i); else _visible[i] = (v.Entry, remaining);
            }
            while (_visible.Count < 3 && _pending.Count > 0) _visible.Add((_pending.Dequeue(), 6));
        }
    }
    internal void Reset() { lock (_sync) { Clear(); _identity = null; _generation = 0; } }
    private void Add(ItemNotification entry, bool popup)
    {
        _history.Add(entry); if (_history.Count > 100) _history.RemoveAt(0);
        if (!popup) return;
        _pending.Enqueue(entry); // Never silently discard a reward before its popup has been displayed.
    }
    private void Clear()
    {
        _history.Clear(); _pending.Clear(); _visible.Clear(); _sent.Clear();
        _nextReceipt = 0; _baseline = false;
    }
    private static string Clean(string value)
    {
        string clean = new((value ?? "Unknown").Where(c => !char.IsControl(c)).Take(160).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "Unknown" : clean;
    }
}
