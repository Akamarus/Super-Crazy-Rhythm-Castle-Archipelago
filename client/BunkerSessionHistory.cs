using Archipelago.MultiClient.Net.Packets;

namespace RhythmCastleAP;

internal sealed class BunkerSessionHistory(long generation, BunkerState state)
{
    private readonly object _sync = new();
    private readonly List<long> _history = new();
    private bool _configured, _ready;

    internal void Configure(string identity, bool enabled, Func<string, BunkerRecord> load)
    {
        lock (_sync)
        {
            state.Configure(generation, identity, enabled, load);
            _configured = true;
            Publish();
        }
    }

    internal void HandlePacket(object packet)
    {
        if (packet is not ReceivedItemsPacket received)
            return;

        lock (_sync)
        {
            // The server can replay the entire inventory on reconnect. Index zero
            // replaces the old list, even when the replacement inventory is empty.
            bool replay = received.Index == 0;
            if (received.Items == null || received.Index < 0 ||
                (!replay && (!_ready || received.Index != _history.Count)))
            {
                _ready = false;
                if (_configured)
                    state.Suspend(generation);
                return;
            }

            if (replay)
            {
                _history.Clear();
                _ready = true;
            }
            _history.AddRange(received.Items.Select(item => item.Item));
            Publish();
        }
    }

    private void Publish()
    {
        // Login and the first inventory packet can arrive in either order.
        if (_configured && _ready)
            state.Publish(generation, _history);
    }
}
