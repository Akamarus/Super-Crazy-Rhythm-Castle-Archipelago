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
    private static readonly TimeSpan[] Delays =
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
    };

    private readonly object _sync = new();
    private int _attempt;
    private bool _reconnectRequested;
    private bool _shutdown;
    private long _intentGeneration;

    internal bool ReconnectRequested
    {
        get
        {
            lock (_sync)
                return _reconnectRequested && !_shutdown;
        }
    }

    internal void OnUnexpectedDisconnect()
    {
        lock (_sync)
        {
            if (_shutdown || _reconnectRequested)
                return;
            _reconnectRequested = true;
            _attempt = 0;
            _intentGeneration++;
        }
    }

    internal void OnDeliberateShutdown()
    {
        lock (_sync)
        {
            _shutdown = true;
            _reconnectRequested = false;
            _attempt = 0;
            _intentGeneration++;
        }
    }

    internal void OnConnected()
    {
        lock (_sync)
        {
            if (_shutdown)
                return;
            _reconnectRequested = false;
            _attempt = 0;
            _intentGeneration++;
        }
    }

    internal TimeSpan? NextDelay()
    {
        return NextAttempt()?.Delay;
    }

    internal ReconnectAttempt? NextAttempt()
    {
        lock (_sync)
        {
            if (_shutdown || !_reconnectRequested)
                return null;
            int index = Math.Min(_attempt, Delays.Length - 1);
            _attempt++;
            return new ReconnectAttempt(_intentGeneration, Delays[index]);
        }
    }

    internal bool IsCurrent(ReconnectAttempt attempt)
    {
        lock (_sync)
        {
            return !_shutdown &&
                   _reconnectRequested &&
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
