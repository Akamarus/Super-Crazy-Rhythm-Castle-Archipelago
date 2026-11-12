using Archipelago.MultiClient.Net.Packets;

namespace RhythmCastleAP;

internal sealed class QuestChecksSession
{
    private readonly object _sync = new();
    private readonly long _generation;
    private readonly List<long> _items = new();
    private readonly HashSet<long> _checks = new();
    private bool _configured, _ready;
    internal QuestChecksSession(long generation) { _generation = generation; }
    internal void Configure(string identity, QuestJournalRecord? restored, IEnumerable<long> checks)
    {
        lock (_sync) {
            QuestChecks.State.Configure(_generation, identity, restored != null, restored);
            _checks.UnionWith(checks); _configured = true; Publish();
            if (restored != null) QuestChecks.ReplayJournal(restored.Completed);
        }
    }
    internal void HandlePacket(object packet)
    {
        lock (_sync) {
            if (packet is ReceivedItemsPacket received) {
                if (received.Items == null || received.Index < 0) { _ready = false; QuestChecks.State.Suspend(_generation); return; }
                if (received.Index == 0) { _items.Clear(); _ready = true; }
                else if (!_ready || received.Index != _items.Count) { _ready = false; QuestChecks.State.Suspend(_generation); return; }
                _items.AddRange(received.Items.Select(item => item.Item));
            }
            else if (packet is RoomUpdatePacket update && update.CheckedLocations != null)
                _checks.UnionWith(update.CheckedLocations);
            else
                return;
            Publish();
        }
    }
    private void Publish() { if (_configured && _ready) QuestChecks.State.Publish(_generation, _items, _checks); }
}
