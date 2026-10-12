namespace RhythmCastleAP;

// Native animal updates are admitted at most once per physics clock value.
// Rendering stays untouched. This class has no Unity/native dependencies.
internal sealed class MontageAnimalTickPolicy
{
    private sealed class Clock
    {
        internal double Tick = double.NaN;
        internal int Frame;
        internal float Pending;
    }
    private readonly Dictionary<long, Clock> _actors = new();
    internal int ActorCount => _actors.Count;
    internal void Reset() => _actors.Clear();
    internal bool Admit(long actor, double physicsTick, int frame, float supplied,
        bool active, out float elapsed)
    {
        elapsed = supplied;
        if (!active || actor == 0 || !double.IsFinite(physicsTick) ||
            !float.IsFinite(supplied) || supplied <= 0 || supplied > .25f)
        { _actors.Remove(actor); return true; }
        if (!_actors.TryGetValue(actor, out var clock))
        {
            if (_actors.Count >= 32)
            {
                long stale = 0;
                foreach (var entry in _actors)
                    if (frame - (long)entry.Value.Frame > 1) { stale = entry.Key; break; }
                if (stale == 0) return true;
                _actors.Remove(stale);
            }
            _actors[actor] = clock = new Clock { Frame = frame };
        }
        if (physicsTick < clock.Tick || frame < clock.Frame || frame - (long)clock.Frame > 1)
        { clock.Tick = double.NaN; clock.Pending = 0; }
        clock.Frame = frame;
        clock.Pending += supplied;
        if (clock.Pending > .25f) { _actors.Remove(actor); return true; }
        if (physicsTick == clock.Tick) return false;
        clock.Tick = physicsTick;
        elapsed = clock.Pending;
        clock.Pending = 0;
        return true;
    }
}
