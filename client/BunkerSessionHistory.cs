using Archipelago.MultiClient.Net.Packets;
namespace RhythmCastleAP;
internal sealed class BunkerSessionHistory(long generation, BunkerState state)
{
    private readonly object _sync = new();
    private readonly List<long> _history = new();
    private bool _configured, _ready;
    internal void Configure(string identity, bool enabled, Func<string, BunkerRecord> load)
    { lock (_sync) { state.Configure(generation, identity, enabled, load); _configured = true; Publish(); } }
    internal void HandlePacket(object packet)
    {
        if (packet is not ReceivedItemsPacket received) return;
        lock (_sync) {
            if (received.Items == null || received.Index < 0 || (received.Index != 0 && (!_ready || received.Index != _history.Count))) {
                _ready = false; if (_configured) state.Suspend(generation); return;
            }
            if (received.Index == 0) { _history.Clear(); _ready = true; }
            _history.AddRange(received.Items.Select(i => i.Item)); Publish();
        }
    }
    private void Publish() { if (_configured && _ready) state.Publish(generation, _history); }
}
