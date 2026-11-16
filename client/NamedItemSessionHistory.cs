using Archipelago.MultiClient.Net.Packets;

namespace RhythmCastleAP;

// The wire packet carries this player's item IDs. Copy the packet before login
// returns so a correction never depends on a live or stale SDK ledger.
internal sealed class NamedItemSessionHistory(Action<int, IReadOnlyList<long>> publish, Action<string> requestSync)
{
    private readonly object _sync = new();
    private readonly List<long> _pending = new();
    private int _end;
    private bool _accepted, _ready, _syncRequested;

    internal void Observe(ReceivedItemsPacket packet)
    {
        lock (_sync)
        {
            if (packet.Items == null || packet.Index < 0 || packet.Items.Length > int.MaxValue - packet.Index ||
                (packet.Index != 0 && (!_ready || packet.Index != _end)))
            {
                Invalidate("Item history is discontinuous.");
                return;
            }
            long[] ids = packet.Items.Select(item => item.Item).ToArray();
            _end = packet.Index + ids.Length;
            _ready = true;
            if (_accepted) Publish(packet.Index, ids);
            else
            {
                if (packet.Index == 0) _pending.Clear();
                _pending.AddRange(ids);
            }
        }
    }

    internal void Accept()
    {
        lock (_sync)
        {
            _accepted = true;
            if (_ready) Publish(0, _pending);
            else Invalidate("Waiting for the room's initial item history.");
            _pending.Clear();
        }
    }

    private void Publish(int index, IReadOnlyList<long> ids)
    {
        try
        {
            publish(index, ids);
            if (index == 0) _syncRequested = false;
        }
        catch (Exception ex)
        {
            Invalidate($"Could not apply the room's item history ({ex.GetType().Name}).");
        }
    }

    private void Invalidate(string reason)
    {
        _ready = false;
        _pending.Clear();
        if (!_accepted) return;
        publish(0, Array.Empty<long>());
        if (_syncRequested) return;
        _syncRequested = true;
        requestSync(reason);
    }
}
