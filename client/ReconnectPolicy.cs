namespace RhythmCastleAP;

internal enum ConnectionErrorDisposition
{
    IgnoreUntilLoginReturns,
    ObserveOnly,
    EndSessionAndReconnect,
}

internal static class ConnectionErrorDispositionPolicy
{
    internal static ConnectionErrorDisposition Decide(bool loginEstablished, Func<bool> socketConnected)
    {
        ArgumentNullException.ThrowIfNull(socketConnected);
        if (!loginEstablished)
            return ConnectionErrorDisposition.IgnoreUntilLoginReturns;
        return socketConnected()
            ? ConnectionErrorDisposition.ObserveOnly
            : ConnectionErrorDisposition.EndSessionAndReconnect;
    }
}

internal readonly record struct ReconnectAttempt(long IntentGeneration, TimeSpan Delay);

internal sealed class ReconnectPolicy
{
    private enum RetryState { Idle, Requested, ActionRequired, Shutdown }
    private static readonly int[] DelaySeconds = { 1, 2, 5, 10, 20, 40, 60 };

    private readonly object _sync = new();
    private int _attempt;
    private RetryState _state;
    private long _intentGeneration;

    internal bool ReconnectRequested
    {
        get
        {
            lock (_sync)
                return _state == RetryState.Requested;
        }
    }

    internal bool OnUnexpectedDisconnect()
    {
        lock (_sync)
        {
            if (_state == RetryState.Idle)
                SetState(RetryState.Requested);
            return _state == RetryState.Requested;
        }
    }

    internal void OnDeliberateShutdown() { lock (_sync) SetState(RetryState.Shutdown); }
    internal void OnActionRequired() { lock (_sync) SetState(RetryState.ActionRequired); }
    internal void ResetRetries() { lock (_sync) SetState(RetryState.Idle); }

    private void SetState(RetryState next)
    {
        if (_state == RetryState.Shutdown) return;
        _state = next;
        _attempt = 0;
        _intentGeneration++;
    }

    internal ReconnectAttempt? NextAttempt()
    {
        lock (_sync)
        {
            if (_state != RetryState.Requested)
                return null;
            int seconds = DelaySeconds[_attempt];
            _attempt = Math.Min(_attempt + 1, DelaySeconds.Length - 1);
            // Spread clients out when a shared room/server is unavailable for a while.
            int milliseconds = seconds < 5 ? seconds * 1000 : Random.Shared.Next(seconds * 800, seconds * 1000 + 1);
            return new ReconnectAttempt(_intentGeneration, TimeSpan.FromMilliseconds(milliseconds));
        }
    }

    internal bool IsCurrent(ReconnectAttempt attempt)
    {
        lock (_sync)
        {
            return _state == RetryState.Requested &&
                   attempt.IntentGeneration == _intentGeneration;
        }
    }
}

internal static class ReconnectAttemptAdmission
{
    internal static bool TryExecute(
        object connectionLock,
        ReconnectPolicy policy,
        ReconnectAttempt attempt,
        Func<bool> connect,
        out bool connected)
    {
        ArgumentNullException.ThrowIfNull(connectionLock);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(connect);
        lock (connectionLock)
        {
            if (!policy.IsCurrent(attempt))
            {
                connected = false;
                return false;
            }
            connected = connect();
            return true;
        }
    }
}
