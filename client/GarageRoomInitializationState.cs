namespace RhythmCastleAP;

internal sealed class GarageRoomInitializationState
{
    internal static string? ReadIdleRoom(string? task, System.Func<string?> readRoom) => task == "NONE" ? readRoom() : null;
    private readonly object _sync = new();
    private long _epoch;
    private string _room = "";
    private bool _ready;
    internal long Epoch { get { lock (_sync) return _epoch; } }
    internal void Transition(string room) { lock (_sync) { _epoch++; _room = room; _ready = false; } }
    internal bool Observe(long epoch, string? nativeRoom, string? nativeTask)
    {
        lock (_sync) {
            if (epoch != _epoch || _room != "GameRoom_27") return false;
            bool wasReady = _ready;
            _ready = nativeRoom == "GameRoom_27" && nativeTask == "NONE";
            return !wasReady && _ready;
        }
    }
    internal bool CanMutate(string room) { lock (_sync) return room != "GameRoom_27" || (_room == room && _ready); }
}

