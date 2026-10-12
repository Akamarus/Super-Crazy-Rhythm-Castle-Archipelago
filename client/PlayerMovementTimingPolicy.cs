namespace RhythmCastleAP;

// Candidate: batch only undersized movement-input updates, never the whole character update.
public sealed class PlayerMovementTimingPolicy
{
    private sealed class Clock { public int Frame; public float Pending; }
    private readonly Dictionary<long, Clock> _actors = new();
    public void Reset() => _actors.Clear();
    public bool Admit(long actor, int frame, float supplied, bool active, out float elapsed)
    {
        elapsed = supplied;
        if (!active || actor == 0 || !float.IsFinite(supplied) || supplied <= 0 || supplied > .25f)
        { _actors.Remove(actor); return true; }
        if (!_actors.TryGetValue(actor, out var clock))
        {
            if (_actors.Count >= 4) { _actors.Clear(); }
            _actors[actor] = clock = new Clock { Frame = frame };
        }
        if (frame <= clock.Frame || frame - (long)clock.Frame > 1) clock.Pending = 0;
        clock.Frame = frame;
        clock.Pending += supplied;
        if (clock.Pending > .25f) { _actors.Remove(actor); return true; }
        if (clock.Pending < .002f) return false;
        elapsed = clock.Pending; clock.Pending = 0;
        return true;
    }
}
