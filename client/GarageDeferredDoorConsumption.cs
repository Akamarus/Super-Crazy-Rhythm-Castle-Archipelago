namespace RhythmCastleAP;
internal enum GarageDoorReadback { None, Waiting, Consumed }
internal sealed class GarageDeferredDoorConsumption
{
    private readonly Dictionary<string, (long Generation, long Epoch, bool Entered)> _pending = new();
    internal void Arm(string song, long generation, long epoch) => _pending[song] = (generation, epoch, false);
    internal void Transition(long? generation, long previousEpoch, long epoch, bool enteringGarage)
    {
        foreach (string song in _pending.Keys.ToArray()) {
            var pending = _pending[song];
            if (enteringGarage && pending.Generation == generation && pending.Epoch == previousEpoch && !pending.Entered)
                _pending[song] = (pending.Generation, epoch, true);
            else _pending.Remove(song);
        }
    }
    internal GarageDoorReadback Observe(string song, long generation, long epoch, bool inGarage, bool readable, bool held)
    {
        if (!_pending.TryGetValue(song, out var pending)) return GarageDoorReadback.None;
        if (pending.Generation != generation || pending.Epoch != epoch || (pending.Entered && !inGarage)) {
            _pending.Remove(song); return GarageDoorReadback.None;
        }
        if (!pending.Entered || !readable) return GarageDoorReadback.Waiting;
        _pending.Remove(song);
        return held ? GarageDoorReadback.None : GarageDoorReadback.Consumed;
    }
}
