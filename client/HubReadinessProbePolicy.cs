namespace RhythmCastleAP;

// Limits scene discovery only; the existing save/marker gate still decides readiness.
internal sealed class HubReadinessProbePolicy
{
    private string _room = "";
    private double _next;
    internal bool ShouldSearch(string room, double now, bool liveObject)
    {
        if (_room != room) { _room = room; _next = 0; }
        if (room != "GameRoom_Hub6" || liveObject || now < _next) return false;
        _next = now + 0.25;
        return true;
    }
}
