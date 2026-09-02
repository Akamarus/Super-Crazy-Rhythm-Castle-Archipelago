using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}
static void True(bool value, string scenario) => Equal(true, value, scenario);
static void False(bool value, string scenario) => Equal(false, value, scenario);

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

{
    var deferredPolicy = new ReconnectPolicy();
    var connectionLock = new object();
    using var delayObtained = new ManualResetEventSlim();
    using var wakeWorker = new ManualResetEventSlim();
    ReconnectAttempt? delayedAttempt = null;
    var sessionCreationCount = 0;
    var admitted = true;
    Task oldWorker = Task.Run(() =>
    {
        deferredPolicy.OnUnexpectedDisconnect();
        delayedAttempt = deferredPolicy.NextAttempt();
        delayObtained.Set();
        True(wakeWorker.Wait(TimeSpan.FromSeconds(1)), "old retry worker wakes after replacement connects");
        admitted = ReconnectAttemptAdmission.TryExecute(
            connectionLock,
            deferredPolicy,
            delayedAttempt!.Value,
            () =>
            {
                sessionCreationCount++;
                return true;
            },
            out _);
    });
    True(delayObtained.Wait(TimeSpan.FromSeconds(1)), "old callback schedules retry and worker obtains its delay token");
    Equal(TimeSpan.FromSeconds(1), delayedAttempt!.Value.Delay, "old worker captured the first retry delay");
    lock (connectionLock)
        deferredPolicy.OnConnected();
    wakeWorker.Set();
    await oldWorker;
    False(admitted, "old worker is denied after replacement clears and advances retry intent");
    Equal(0, sessionCreationCount, "stale retry is denied before session creation or publication");
}

{
    var currentPolicy = new ReconnectPolicy();
    var connectionLock = new object();
    currentPolicy.OnUnexpectedDisconnect();
    ReconnectAttempt oldAttempt = currentPolicy.NextAttempt()!.Value;
    currentPolicy.OnConnected();
    currentPolicy.OnUnexpectedDisconnect();
    ReconnectAttempt currentAttempt = currentPolicy.NextAttempt()!.Value;
    var sessionCreationCount = 0;
    False(ReconnectAttemptAdmission.TryExecute(
            connectionLock,
            currentPolicy,
            oldAttempt,
            () =>
            {
                sessionCreationCount++;
                return true;
            },
            out _),
        "an old worker cannot consume a newer active reconnect intent");
    True(ReconnectAttemptAdmission.TryExecute(
            connectionLock,
            currentPolicy,
            currentAttempt,
            () =>
            {
                sessionCreationCount++;
                return false;
            },
            out bool connected),
        "retry worker is admitted while its retry intent remains current");
    False(connected, "admitted retry reports the connection attempt result");
    Equal(1, sessionCreationCount, "current retry reaches session creation exactly once");
}

string pluginSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));
Equal(true, pluginSource.Contains("Interlocked.CompareExchange(ref _reconnectWorkerActive, 1, 0)", StringComparison.Ordinal),
    "only one reconnect worker is scheduled");
Equal(true, pluginSource.Contains("previous.Socket.DisconnectAsync()", StringComparison.Ordinal),
    "replaced session is disconnected");
Equal(true, pluginSource.Contains("_processedReceivedItemIndexes.Add(itemIndex)", StringComparison.Ordinal),
    "replayed received items are deduplicated");
Equal(true, pluginSource.Contains("_reconnectPolicy.OnDeliberateShutdown()", StringComparison.Ordinal),
    "explicit shutdown disables retry");
Equal(true, pluginSource.Contains("Task.Delay(attempt.Value.Delay, _shutdownToken.Token)", StringComparison.Ordinal),
    "retry delay does not block the Unity thread");
Equal(true, pluginSource.Contains("ReconnectAttemptAdmission.TryExecute(", StringComparison.Ordinal),
    "deferred retry revalidates intent under connection-attempt admission");

Console.WriteLine("Reconnect policy tests passed.");
