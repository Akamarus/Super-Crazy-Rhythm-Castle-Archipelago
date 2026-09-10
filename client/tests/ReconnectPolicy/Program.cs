using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}
static void True(bool value, string scenario) => Equal(true, value, scenario);
static void False(bool value, string scenario) => Equal(false, value, scenario);
static void WaitUntilBlocked(Thread thread, string scenario)
{
    var timeout = System.Diagnostics.Stopwatch.StartNew();
    var spinner = new SpinWait();
    while (timeout.Elapsed < TimeSpan.FromSeconds(5))
    {
        ThreadState state = thread.ThreadState;
        if ((state & ThreadState.WaitSleepJoin) != 0)
            return;
        if ((state & ThreadState.Stopped) != 0)
            throw new InvalidOperationException($"{scenario}: thread completed instead of blocking");
        spinner.SpinOnce();
    }
    throw new InvalidOperationException($"{scenario}: thread did not block within the timeout");
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

{
    var serializedPolicy = new ReconnectPolicy();
    var connectionLock = new object();
    serializedPolicy.OnUnexpectedDisconnect();
    ReconnectAttempt attempt = serializedPolicy.NextAttempt()!.Value;
    var connectionDelegateCalls = 0;
    var admitted = true;
    var connected = true;
    using var workerReady = new ManualResetEventSlim();
    using var wakeWorker = new ManualResetEventSlim();
    using var admissionStarted = new ManualResetEventSlim();
    using var workerFinished = new ManualResetEventSlim();
    var worker = new Thread(() =>
    {
        workerReady.Set();
        wakeWorker.Wait();
        admissionStarted.Set();
        admitted = ReconnectAttemptAdmission.TryExecute(
            connectionLock,
            serializedPolicy,
            attempt,
            () =>
            {
                Interlocked.Increment(ref connectionDelegateCalls);
                return true;
            },
            out connected);
        workerFinished.Set();
    });

    worker.Start();
    True(workerReady.Wait(TimeSpan.FromSeconds(5)), "retry worker is waiting to be woken");
    Monitor.Enter(connectionLock);
    try
    {
        True(Monitor.IsEntered(connectionLock), "replacement owns the connection lock before waking the retry worker");
        wakeWorker.Set();
        True(admissionStarted.Wait(TimeSpan.FromSeconds(5)), "retry worker attempts admission while replacement holds the connection lock");
        WaitUntilBlocked(worker, "retry admission waits behind the replacement connection lock");
        False(workerFinished.IsSet, "retry admission cannot complete while replacement holds the connection lock");
        serializedPolicy.OnConnected();
        False(workerFinished.IsSet, "replacement invalidates retry intent before releasing the connection lock");
    }
    finally
    {
        Monitor.Exit(connectionLock);
    }

    True(worker.Join(TimeSpan.FromSeconds(5)), "retry admission completes after replacement releases the connection lock");
    False(admitted, "retry intent is validated only after admission acquires the connection lock");
    False(connected, "retry denied after replacement reports no connection result");
    Equal(0, connectionDelegateCalls, "replacement invalidation prevents stale connection execution");
}

{
    var serializedPolicy = new ReconnectPolicy();
    var connectionLock = new object();
    serializedPolicy.OnUnexpectedDisconnect();
    ReconnectAttempt attempt = serializedPolicy.NextAttempt()!.Value;
    using var connectionDelegateEntered = new ManualResetEventSlim();
    using var releaseConnectionDelegate = new ManualResetEventSlim();
    using var replacementAcquiredLock = new ManualResetEventSlim();
    using var workerFinished = new ManualResetEventSlim();
    var sequence = 0;
    var delegateExitSequence = 0;
    var replacementAcquireSequence = 0;
    var admitted = false;
    var connected = true;
    var worker = new Thread(() =>
    {
        admitted = ReconnectAttemptAdmission.TryExecute(
            connectionLock,
            serializedPolicy,
            attempt,
            () =>
            {
                connectionDelegateEntered.Set();
                releaseConnectionDelegate.Wait();
                delegateExitSequence = Interlocked.Increment(ref sequence);
                return false;
            },
            out connected);
        workerFinished.Set();
    });
    worker.Start();
    True(connectionDelegateEntered.Wait(TimeSpan.FromSeconds(5)), "valid retry enters its connection delegate");

    var replacement = new Thread(() =>
    {
        lock (connectionLock)
        {
            replacementAcquireSequence = Interlocked.Increment(ref sequence);
            serializedPolicy.OnConnected();
            replacementAcquiredLock.Set();
        }
    });
    replacement.Start();
    try
    {
        WaitUntilBlocked(replacement, "replacement waits behind the active retry connection delegate");
        False(replacementAcquiredLock.IsSet, "replacement cannot acquire the connection lock while retry connection executes");
        False(workerFinished.IsSet, "retry admission remains active until its connection delegate exits");
    }
    finally
    {
        releaseConnectionDelegate.Set();
    }

    True(worker.Join(TimeSpan.FromSeconds(5)), "retry admission completes after its connection delegate exits");
    True(replacement.Join(TimeSpan.FromSeconds(5)), "replacement acquires the connection lock after retry connection exits");
    True(admitted, "current retry is admitted");
    False(connected, "admitted retry propagates the connection delegate result");
    True(replacementAcquiredLock.IsSet, "replacement eventually acquires the connection lock");
    Equal(1, delegateExitSequence, "retry connection delegate exits before replacement lock acquisition");
    Equal(2, replacementAcquireSequence, "replacement lock acquisition is serialized after retry connection execution");
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

string loginWiring = pluginSource[pluginSource.IndexOf("if (result is LoginSuccessful loginSuccess)", StringComparison.Ordinal)..];
int pointConfig = loginWiring.IndexOf("pointHistory.ApplySlotData(", StringComparison.Ordinal);
int pointGate = loginWiring.IndexOf("if (pointState.Mode == MusicLabPointRuntimeMode.Incompatible)", StringComparison.Ordinal);
int connectedWiring = loginWiring.IndexOf("_connected = true;", StringComparison.Ordinal);
True(pointConfig >= 0 && pointGate > pointConfig && connectedWiring > pointGate,
    "login configures packet-complete history and rejects incompatibility before advertising connected");
False(loginWiring[..connectedWiring].Contains("AllItemsReceived", StringComparison.Ordinal),
    "login cannot publish the unready received-item cache");
string itemWiring = pluginSource[pluginSource.IndexOf("session.Items.ItemReceived += helper =>", StringComparison.Ordinal)..pluginSource.IndexOf("if (!ConnectionLifecycle.TryPublish(session, generation", StringComparison.Ordinal)];
False(itemWiring.Contains("MusicLabPointRandomization.SynchronizeHistory(", StringComparison.Ordinal),
    "individual replay callbacks cannot publish partial point history");
True(itemWiring.Contains("session.Socket.PacketReceived += packet =>", StringComparison.Ordinal) &&
    itemWiring.Contains("using (packetLease)", StringComparison.Ordinal) &&
    itemWiring.Contains("pointHistory.HandlePacket(packet)", StringComparison.Ordinal),
    "complete packets are processed under the current session generation lease");
True(itemWiring.Contains("MusicLabPointRandomization.TryHandleItemName,", StringComparison.Ordinal), "point names are consumed by item dispatch");
string ending = pluginSource[pluginSource.IndexOf("private bool ClearCurrentSession(", StringComparison.Ordinal)..pluginSource.IndexOf("private void RequestReconnect(", StringComparison.Ordinal)];
True(ending.Contains("MusicLabPointRandomization.OnDisconnected(generation)", StringComparison.Ordinal), "accepted session ending retains synchronized points");
string shutdown = pluginSource[pluginSource.IndexOf("public void Shutdown()", StringComparison.Ordinal)..pluginSource.IndexOf("private bool ClearCurrentSession(", StringComparison.Ordinal)];
True(shutdown.Contains("MusicLabPointRandomization.Reset()", StringComparison.Ordinal), "deliberate shutdown clears point state");
True(pluginSource.Contains("MusicLabPointRandomization.OnDisconnected(previousSession.Generation)", StringComparison.Ordinal), "replacement revokes previous point generation");
Console.WriteLine("Music Lab Point lifecycle wiring tests passed.");
