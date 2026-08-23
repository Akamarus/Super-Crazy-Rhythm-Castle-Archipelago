using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

var policy = new ReconnectPolicy();
Equal<TimeSpan?>(null, policy.NextDelay(), "connected startup has no retry");

policy.OnUnexpectedDisconnect();
Equal<TimeSpan?>(TimeSpan.FromSeconds(1), policy.NextDelay(), "first retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(2), policy.NextDelay(), "second retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(5), policy.NextDelay(), "third retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(10), policy.NextDelay(), "fourth retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(30), policy.NextDelay(), "fifth retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(30), policy.NextDelay(), "bounded repeated retry");

policy.OnConnected();
Equal<TimeSpan?>(null, policy.NextDelay(), "successful login resets retries");
policy.OnUnexpectedDisconnect();
Equal<TimeSpan?>(TimeSpan.FromSeconds(1), policy.NextDelay(), "recovery starts fresh schedule");

policy.OnDeliberateShutdown();
Equal<TimeSpan?>(null, policy.NextDelay(), "shutdown cancels pending retry");
policy.OnUnexpectedDisconnect();
Equal<TimeSpan?>(null, policy.NextDelay(), "socket close during shutdown is ignored");

Console.WriteLine("Reconnect policy tests passed.");
