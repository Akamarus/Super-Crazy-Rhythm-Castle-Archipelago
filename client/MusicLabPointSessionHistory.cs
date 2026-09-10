using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RhythmCastleAP;

// One instance per network session. Call both entry points under that session's
// generation lease. This lock orders login configuration against packet delivery.
internal sealed class MusicLabPointSessionHistory
{
    private readonly object _sync = new();
    private readonly long _generation;
    private readonly List<MusicLabPointReceipt> _history = new();
    private bool _configured;
    private bool _historyReady;

    internal MusicLabPointSessionHistory(long generation) => _generation = generation;

    internal MusicLabPointSnapshot ApplySlotData(Dictionary<string, object>? slotData, string game, string seed, string slot)
    {
        lock (_sync)
        {
            MusicLabPointRandomization.ApplySlotData(slotData, game, seed, slot, _generation);
            _configured = true;
            PublishIfReady();
            return MusicLabPointRandomization.Snapshot;
        }
    }

    internal void HandlePacket(ArchipelagoPacketBase packet)
    {
        if (packet is not ReceivedItemsPacket received)
            return;
        lock (_sync)
        {
            // 6.7.1 processes ReceivedItems synchronously before our later socket
            // subscriber. Index zero is a complete authoritative replay, including
            // an empty array. Its helper cache is partial during replay callbacks
            // and can remain stale after an empty replay, so use packet IDs here.
            if (received.Items == null || received.Index < 0)
            {
                _historyReady = false;
                return;
            }
            if (received.Index == 0)
            {
                _history.Clear();
                _historyReady = true;
            }
            else if (!_historyReady || received.Index != _history.Count)
            {
                // The library requests Sync on a gap; retain the last published
                // total and wait for the resulting index-zero authoritative packet.
                _historyReady = false;
                return;
            }
            foreach (var item in received.Items)
                _history.Add(new MusicLabPointReceipt(_history.Count, item.Item));
            PublishIfReady();
        }
    }

    private void PublishIfReady()
    {
        if (_configured && _historyReady)
            MusicLabPointRandomization.SynchronizeHistory(_generation, _history);
    }
}
