namespace RhythmCastleAP;

internal sealed class PreviewAbilityReconcileDispatcher
{
    private readonly object _sync = new();
    private string? _pendingReason;

    internal void Request(string reason)
    {
        lock (_sync)
            _pendingReason = reason;
    }

    internal bool Drain(Action<string> reconcile)
    {
        string? reason;
        lock (_sync)
        {
            reason = _pendingReason;
            _pendingReason = null;
        }

        if (reason == null)
            return false;

        reconcile(reason);
        return true;
    }

    internal void Clear()
    {
        lock (_sync)
            _pendingReason = null;
    }
}
