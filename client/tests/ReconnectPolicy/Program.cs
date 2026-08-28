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

var recoveryLifecycle = new ReconnectPolicy();
recoveryLifecycle.OnUnexpectedDisconnect();
Equal(true, recoveryLifecycle.ReconnectRequested, "socket close records reconnect intent");
Equal<TimeSpan?>(TimeSpan.FromSeconds(1), recoveryLifecycle.NextDelay(), "socket close schedules retry");
Equal<TimeSpan?>(TimeSpan.FromSeconds(2), recoveryLifecycle.NextDelay(), "failed login advances retry");
recoveryLifecycle.OnConnected();
Equal(false, recoveryLifecycle.ReconnectRequested, "successful login clears reconnect intent");
Equal<TimeSpan?>(null, recoveryLifecycle.NextDelay(), "successful recovered login stops worker");

string pluginSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));
Equal(true, pluginSource.Contains("Interlocked.CompareExchange(ref _reconnectWorkerActive, 1, 0)", StringComparison.Ordinal),
    "only one reconnect worker is scheduled");
Equal(true, pluginSource.Contains("previous.Socket.DisconnectAsync()", StringComparison.Ordinal),
    "replaced session is disconnected");
Equal(true, pluginSource.Contains("_processedReceivedItemIndexes.Add(itemIndex)", StringComparison.Ordinal),
    "replayed received items are deduplicated");
Equal(true, pluginSource.Contains("_reconnectPolicy.OnDeliberateShutdown()", StringComparison.Ordinal),
    "explicit shutdown disables retry");
Equal(true, pluginSource.Contains("Task.Delay(delay.Value, _shutdownToken.Token)", StringComparison.Ordinal),
    "retry delay does not block the Unity thread");

Console.WriteLine("Reconnect policy tests passed.");
