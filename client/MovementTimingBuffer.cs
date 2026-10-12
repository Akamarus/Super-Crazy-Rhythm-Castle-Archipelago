namespace RhythmCastleAP;

// Fixed storage; no allocation or formatting in the observed native call.
public sealed class MovementTimingBuffer
{
    public readonly record struct Entry(long Actor, int Frame, float Elapsed);
    private readonly Entry[] _entries;
    public int Count { get; private set; }
    public int Dropped { get; private set; }
    public bool Active { get; private set; }
    public MovementTimingBuffer(int capacity) { _entries = new Entry[capacity]; }
    public void Begin() { Count = Dropped = 0; Active = true; }
    public void Stop() { Active = false; }
    public void Record(long actor, int frame, float elapsed)
    {
        if (!Active) return;
        if (Count == _entries.Length) { if (Dropped < int.MaxValue) Dropped++; return; }
        _entries[Count++] = new Entry(actor, frame, elapsed);
    }
    public Entry Get(int index) => index >= 0 && index < Count ? _entries[index] : throw new System.ArgumentOutOfRangeException(nameof(index));
}
