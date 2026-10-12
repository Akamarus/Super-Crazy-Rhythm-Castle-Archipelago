namespace RhythmCastleAP;

// Limits scene discovery only; the existing save/marker gate still decides readiness.
internal sealed class HubReadinessProbePolicy
{
    private string _room = "";
    private double _next;
    internal bool ShouldSearch(string room, double now, bool liveObject)
    {
        if (_room != room) { _room = room; _next = 0; }
        if (!room.StartsWith("GameRoom_", System.StringComparison.Ordinal) || liveObject || now < _next) return false;
        _next = now + 0.25;
        return true;
    }
}

// Bounded retry after a one-shot reconciliation observes temporary unavailability.
internal sealed class CassetteReconciliationRetry
{
    private System.TimeSpan _elapsed;
    internal bool Tick(System.TimeSpan elapsed, bool ready, bool hasWork)
    {
        if (!ready || !hasWork) { _elapsed = System.TimeSpan.Zero; return false; }
        if (elapsed <= System.TimeSpan.Zero) return false;
        _elapsed += elapsed;
        if (_elapsed < System.TimeSpan.FromSeconds(2)) return false;
        _elapsed = System.TimeSpan.Zero;
        return true;
    }
}
