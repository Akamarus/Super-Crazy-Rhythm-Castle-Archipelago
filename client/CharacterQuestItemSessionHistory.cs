using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;

namespace RhythmCastleAP;

internal sealed class CharacterQuestItemSessionHistory
{
    private readonly object _sync = new();
    private readonly long _generation;
    private readonly CharacterQuestItemState _state;
    private readonly List<long> _history = new();
    private bool _configured, _ready;
    internal CharacterQuestItemSessionHistory(long generation, CharacterQuestItemState state)
    { _generation = generation; _state = state; }
    internal void Configure(bool enabled)
    {
        lock (_sync) { _state.Configure(_generation, enabled); _configured = true; Publish(); }
    }
    internal void HandlePacket(object packet)
    {
        if (packet is not ReceivedItemsPacket received) return;
        lock (_sync)
        {
            if (received.Items == null || received.Index < 0) { _ready = false; return; }
            if (received.Index == 0) { _history.Clear(); _ready = true; }
            else if (!_ready || received.Index != _history.Count) { _ready = false; return; }
            _history.AddRange(received.Items.Select(item => item.Item));
            Publish();
        }
    }
    private void Publish() { if (_configured && _ready) _state.Publish(_generation, _history); }
}
