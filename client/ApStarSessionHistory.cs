using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Packets;
namespace RhythmCastleAP;
// Session-local packet history: index zero replaces even an empty replay. Never
// count ItemReceived callbacks, whose library cache can still be partial.
internal sealed class ApStarSessionHistory
{
    private readonly object _sync = new();
    private readonly long _generation;
    private readonly ApStarState _state;
    private int _receivedCount, _stars;
    private bool _configured, _historyReady, _historyInvalid;
    internal ApStarSessionHistory(long generation, ApStarState state) { _generation=generation; _state=state; }
    internal ApStarSettings ApplySlotData(Dictionary<string,object>? data, string identity, bool cellTowerPathReady, bool lobbyDoorsReady = true, bool bunkerReady = true)
    {
        lock (_sync)
        {
            var settings=ApStarContract.Validate(data, cellTowerPathReady, lobbyDoorsReady, bunkerReady);
            _state.Configure(_generation,identity,settings);_configured=true;
            if (_historyInvalid) _state.SuspendHistory(_generation);
            Publish();return settings;
        }
    }
    internal void HandlePacket(ArchipelagoPacketBase packet)
    {
        if(packet is not ReceivedItemsPacket received)return;
        lock(_sync)
        {
            if (received.Items == null || received.Index < 0 ||
                received.Items.Length > int.MaxValue - received.Index ||
                (received.Index != 0 && (!_historyReady || received.Index != _receivedCount)))
            {
                Invalidate();
                return;
            }
            if (received.Index == 0)
            {
                _stars = 0;
                _historyReady = true;
                _historyInvalid = false;
            }
            foreach (var item in received.Items)
                if (item.Item == ApStarContract.StarId) _stars++;
            _receivedCount = received.Index + received.Items.Length;
            Publish();
        }
    }
    private void Invalidate()
    {
        _historyReady = false;
        _historyInvalid = true;
        if (_configured) _state.SuspendHistory(_generation);
    }
    private void Publish(){if(_configured&&_historyReady)_state.Publish(_generation,_stars);}
}
