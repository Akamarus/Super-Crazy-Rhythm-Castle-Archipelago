using Archipelago.MultiClient.Net.Packets;

namespace RhythmCastleAP;

// Only a contiguous index-zero history from the live connection authorizes delivery.
internal sealed class WeedKillerReceiptState
{
    private readonly object _sync = new();
    private long _generation = -1;
    private bool _ended = true;
    private bool _owned;
    internal bool Owned { get { lock (_sync) return !_ended && _owned; } }
    internal void Begin(long generation)
    {
        lock (_sync)
        {
            if (generation <= _generation) return;
            _generation = generation;
            _ended = false;
            _owned = false;
        }
    }
    internal void End(long generation)
    {
        lock (_sync)
        {
            if (generation < _generation) return;
            _generation = generation;
            _ended = true;
            _owned = false;
        }
    }
    internal void Publish(long generation, bool owned)
    {
        lock (_sync)
            if (generation == _generation && !_ended) _owned = owned;
    }
    internal void WithOwned(Action action)
    {
        lock (_sync)
            if (!_ended && _owned) action();
    }
}

internal sealed class WeedKillerSessionHistory
{
    private const long WeedKillerItemId = 187256116; // docs/IDS.md
    private readonly object _sync = new();
    private readonly long _generation;
    private bool _configured, _ready, _owned;
    private int _count;
    internal WeedKillerSessionHistory(long generation)
    {
        _generation = generation;
        WeedKillerRandomization.History.Begin(generation);
    }
    internal void Configure()
    {
        lock (_sync) { _configured = true; Publish(); }
    }
    internal void HandlePacket(object packet)
    {
        if (packet is not ReceivedItemsPacket received) return;
        lock (_sync)
        {
            if (received.Items == null || received.Index < 0 ||
                (received.Index != 0 && (!_ready || received.Index != _count)))
            {
                _ready = false;
                Publish();
                return;
            }
            if (received.Index == 0) { _count = 0; _owned = false; _ready = true; }
            if (received.Items.Length > int.MaxValue - _count)
            {
                _ready = false;
                Publish();
                return;
            }
            _count += received.Items.Length;
            _owned |= received.Items.Any(item => item.Item == WeedKillerItemId);
            Publish();
        }
    }
    private void Publish() => WeedKillerRandomization.History.Publish(_generation, _configured && _ready && _owned);
}
