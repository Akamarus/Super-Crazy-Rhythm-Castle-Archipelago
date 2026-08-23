namespace RhythmCastleAP;

internal sealed class ReconnectPolicy
{
    private static readonly TimeSpan[] Delays =
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
    };

    private readonly object _sync = new();
    private int _attempt;
    private bool _reconnectRequested;
    private bool _shutdown;

    internal void OnUnexpectedDisconnect()
    {
        lock (_sync)
        {
            if (_shutdown || _reconnectRequested)
                return;
            _reconnectRequested = true;
            _attempt = 0;
        }
    }

    internal void OnDeliberateShutdown()
    {
        lock (_sync)
        {
            _shutdown = true;
            _reconnectRequested = false;
            _attempt = 0;
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
        }
    }

    internal TimeSpan? NextDelay()
    {
        lock (_sync)
        {
            if (_shutdown || !_reconnectRequested)
                return null;
            int index = Math.Min(_attempt, Delays.Length - 1);
            _attempt++;
            return Delays[index];
        }
    }
}
