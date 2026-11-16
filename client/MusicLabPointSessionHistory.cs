using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RhythmCastleAP;

// One instance per network session. Call both entry points under that session's
// generation lease. This lock orders login configuration against packet delivery.
internal sealed class MusicLabPointSessionHistory
{
    private readonly object _sync = new();
    private readonly long _generation;
    private MusicLabPointTotals _totals;
    private long _nextIndex;
    private MusicLabPointRoom? _candidate;
    private bool _configured;
    private bool _historyReady;

    internal MusicLabPointSessionHistory(long generation) => _generation = generation;

    internal MusicLabPointCompatibilityResult PrepareSlotData(Dictionary<string, object>? slotData, string game, string seed, string slot)
    {
        lock (_sync)
        {
            var room = MusicLabPointRoom.Read(slotData, game, seed, slot);
            var compatibility = MusicLabPointRandomization.CheckCompatibility(room, _generation);
            _candidate = compatibility.Mode == MusicLabPointCompatibilityMode.IncompatibleClaim ? null : room;
            return compatibility;
        }
    }

    internal void Accept()
    {
        lock (_sync)
        {
            if (!_candidate.HasValue) throw new InvalidOperationException("No compatible Music Lab Point room was prepared.");
            MusicLabPointRandomization.Apply(_candidate.Value, _generation);
            _configured = true;
            PublishIfReady();
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
                _totals = default;
                _nextIndex = 0;
                _historyReady = true;
            }
            else if (!_historyReady || received.Index != _nextIndex)
            {
                // The library requests Sync on a gap; retain the last published
                // total and wait for the resulting index-zero authoritative packet.
                _historyReady = false;
                return;
            }
            foreach (var item in received.Items)
                _totals.Add(item.Item);
            _nextIndex += received.Items.Length;
            PublishIfReady();
        }
    }

    private void PublishIfReady()
    {
        if (_configured && _historyReady)
            MusicLabPointRandomization.SynchronizeTotals(_generation, _totals);
    }
}
