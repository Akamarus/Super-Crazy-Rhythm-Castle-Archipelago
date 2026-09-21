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
    private readonly List<long> _history = new();
    private bool _configured, _historyReady, _historyInvalid;
    internal ApStarSessionHistory(long generation, ApStarState state) { _generation=generation; _state=state; }
    internal ApStarSettings ApplySlotData(Dictionary<string,object>? data, string identity)
    {
        lock (_sync)
        {
            var settings=ApStarContract.Validate(data);
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
            if(received.Items==null||received.Index<0){Invalidate();return;}
            if(received.Index==0){_history.Clear();_historyReady=true;_historyInvalid=false;}
            else if(!_historyReady||received.Index!=_history.Count){Invalidate();return;}
            foreach(var item in received.Items)_history.Add(item.Item);
            Publish();
        }
    }
    private void Invalidate()
    {
        _historyReady = false;
        _historyInvalid = true;
        if (_configured) _state.SuspendHistory(_generation);
    }
    private void Publish(){if(_configured&&_historyReady)_state.Publish(_generation,_history);}
}
