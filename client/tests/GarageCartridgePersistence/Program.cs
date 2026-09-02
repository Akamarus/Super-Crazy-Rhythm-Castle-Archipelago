using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}
static void True(bool value, string scenario) => Equal(true, value, scenario);
static void False(bool value, string scenario) => Equal(false, value, scenario);
static string MethodBody(string source, string signature, string nextSignature)
{
    int start = source.IndexOf(signature, StringComparison.Ordinal);
    int end = source.IndexOf(nextSignature, start + signature.Length, StringComparison.Ordinal);
    if (start < 0 || end < 0)
        throw new InvalidOperationException($"Could not locate source boundaries for {signature}.");
    return source[start..end];
}
static async Task Eventually(Func<bool> condition, string scenario)
{
    for (var attempt = 0; attempt < 100; attempt++)
    {
        if (condition())
            return;
        await Task.Delay(10);
    }

    throw new InvalidOperationException($"{scenario}: condition was not met");
}

string[] expectedKeys =
{
    "Bloody Tears|scrc:garage_inserted:v1:bloody_tears",
    "Gradius Remix|scrc:garage_inserted:v1:gradius_remix",
    "Smooch|scrc:garage_inserted:v1:smooch",
    "Superstar|scrc:garage_inserted:v1:superstar",
    "Wag the Dog|scrc:garage_inserted:v1:wag_the_dog",
};
Equal(string.Join("\n", expectedKeys), string.Join("\n", GarageCartridgeNativePolicy.RandomizedCartridges.Select(x => $"{x.Song}|{x.ServerInsertionKey}")), "the five versioned slot keys are exact and deterministic");
False(GarageCartridgeNativePolicy.AllCartridges.Any(x => x.Song == "Vampire Killer" && x.ServerInsertionKey != ""), "Vampire Killer has no server key");
Equal(GarageNativeGrantDecision.WaitForServer, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, GarageInsertionServerValue.Unknown, true, false), "unknown server state fails closed");
Equal(GarageNativeGrantDecision.ApplyBagItem, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, GarageInsertionServerValue.NotInserted, true, false), "known not-inserted missing bag grants once");
Equal(GarageNativeGrantDecision.AlreadyInserted, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, GarageInsertionServerValue.Inserted, true, false), "inserted state is terminal");
Equal(GarageNativeGrantDecision.None, GarageCartridgeInsertionPolicy.DecideGrant(false, true, false, GarageInsertionServerValue.NotInserted, true, false), "incompatible routing");
Equal(GarageNativeGrantDecision.None, GarageCartridgeInsertionPolicy.DecideGrant(true, false, false, GarageInsertionServerValue.NotInserted, true, false), "no AP ownership");
Equal(GarageNativeGrantDecision.WaitForNativeRead, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, GarageInsertionServerValue.NotInserted, false, false), "unreadable native bag");
Equal(GarageNativeGrantDecision.AlreadyHeld, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, GarageInsertionServerValue.NotInserted, true, true), "already-held bag");
Equal(GarageNativeGrantDecision.None, GarageCartridgeInsertionPolicy.DecideGrant(true, true, true, GarageInsertionServerValue.NotInserted, true, false), "physical vanilla cartridge");
Equal(GarageNativeGrantDecision.WaitForServer, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, (GarageInsertionServerValue)99, true, false), "unrecognized server state fails closed");

string pluginSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));
string storageSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "GarageCartridgeInsertionStorage.cs"));
string connectMethod = MethodBody(pluginSource, "private bool TryConnectOnce()", "public void Shutdown()");
string shutdownMethod = MethodBody(pluginSource, "public void Shutdown()", "private bool ClearCurrentSession(");
string clearCurrentSessionMethod = MethodBody(pluginSource, "private bool ClearCurrentSession(", "private void RequestReconnect(");
int garageClassStart = pluginSource.IndexOf("internal static class GarageCartridgeAccess", StringComparison.Ordinal);
if (garageClassStart < 0)
    throw new InvalidOperationException("Could not locate GarageCartridgeAccess.");
string garageSource = pluginSource[garageClassStart..];
string configureMethod = MethodBody(garageSource, "public static void Configure()", "public static bool TryApplyItem(string itemName)");
string itemCallback = MethodBody(garageSource, "public static bool TryApplyItem(string itemName)", "public static void CapturePlayerSaveRequestProcessor(object? instance)");
string observeNativeBagMethod = MethodBody(garageSource, "public static void ObserveNativeBag(", "public static void NoteGarageObjectReleased(");
string noteGarageObjectReleasedMethod = MethodBody(garageSource, "public static void NoteGarageObjectReleased(", "public static void CapturePlayerSaveRequestProcessor(object? instance)");
string captureGarageProcessorMethod = MethodBody(garageSource, "public static void CapturePlayerSaveRequestProcessor(object? instance)", "public static void TryFlushPendingNativeGrants()");
string nativeGrantMethod = MethodBody(garageSource, "public static void TryFlushPendingNativeGrants()", "public static void ApplySlotData(");
string releaseGarageObjectMethod = MethodBody(garageSource, "public static bool TryReleaseCartridgeObject(", "public static void NoteGarageObjectReleased(string song)");

int generationIncrement = connectMethod.IndexOf("long generation = Interlocked.Increment(ref _connectionGeneration);", StringComparison.Ordinal);
int attemptAdmission = connectMethod.IndexOf("ConnectionLifecycle.TryAdmit(generation)", StringComparison.Ordinal);
int sessionCreation = connectMethod.IndexOf("ArchipelagoSessionFactory.CreateSession(_server)", StringComparison.Ordinal);
True(pluginSource.Contains("private long _connectionGeneration;", StringComparison.Ordinal),
    "ArchipelagoClient owns the connection generation");
True(pluginSource.Contains(
        "private readonly SessionGenerationLeaseGate<ArchipelagoSession> ConnectionLifecycle = new();",
        StringComparison.Ordinal),
    "ArchipelagoClient uses an atomic session-generation lifecycle gate");
True(generationIncrement >= 0 && generationIncrement < sessionCreation,
    "every new session attempt captures a unique generation before session creation");
True(attemptAdmission > generationIncrement && attemptAdmission < sessionCreation,
    "every session attempt is admitted before session creation so shutdown can reject publication");
True(connectMethod.Contains("ConnectionLifecycle.TryAcquire(session, generation", StringComparison.Ordinal) &&
     connectMethod.Contains("ClearCurrentSession(session, generation", StringComparison.Ordinal),
    "session callbacks atomically lease or end the matching session and generation");
False(pluginSource.Contains("private bool IsCurrentSession(", StringComparison.Ordinal),
    "the former check-then-act session helper is not available for callback dispatch");

int garageSlotData = connectMethod.IndexOf("GarageCartridgeAccess.ApplySlotData(loginSuccess.SlotData);", StringComparison.Ordinal);
int beginServerSync = connectMethod.IndexOf("GarageCartridgeAccess.BeginServerSync(session, generation)", StringComparison.Ordinal);
int connected = connectMethod.IndexOf("_connected = true;", StringComparison.Ordinal);
True(garageSlotData >= 0 && beginServerSync > garageSlotData && connected > beginServerSync,
    "compatible Garage server sync begins after slot data and before the client becomes connected");

string socketCloseHandler = MethodBody(connectMethod, "session.Socket.SocketClosed += reason =>", "session.Socket.ErrorReceived += (exception, message) =>");
True(socketCloseHandler.Contains("ClearCurrentSession(session, generation, () =>", StringComparison.Ordinal) &&
     socketCloseHandler.Contains("RequestReconnect($\"socket closed: {reason}\")", StringComparison.Ordinal),
    "current-session socket close performs its terminal work through the exclusive lifecycle callback");
string socketErrorHandler = MethodBody(connectMethod, "session.Socket.ErrorReceived += (exception, message) =>", "session.Items.ItemReceived += helper =>");
True(socketErrorHandler.Contains("ClearCurrentSession(session, generation, () =>", StringComparison.Ordinal) &&
     socketErrorHandler.Contains("RequestReconnect($\"terminal socket error: {message}\")", StringComparison.Ordinal),
    "terminal socket error performs its teardown and reconnect decision through the exclusive lifecycle callback");
string failedLogin = MethodBody(connectMethod, "if (!result.Successful)", "if (result is LoginSuccessful loginSuccess)");
True(failedLogin.Contains("ClearCurrentSession(session, generation);", StringComparison.Ordinal),
    "failed login ends its server synchronization generation");
True(connectMethod.Contains("GarageCartridgeAccess.EndServerSync(previousSession.Generation);", StringComparison.Ordinal),
    "replacing a session ends the replaced synchronization generation");
True(connectMethod.Contains("ConnectionLifecycle.TryPublish(session, generation", StringComparison.Ordinal),
    "session publication is an admitted atomic lifecycle claim");
True(connectMethod.Contains("ConnectionLifecycle.TryAcquire(session, generation, out LifecycleLease? loginLease)", StringComparison.Ordinal),
    "successful login holds a current-session lease through server sync begin and connected publication");
True(connectMethod.Contains("ConnectionLifecycle.TryAcquire(session, generation, out LifecycleLease? callbackLease)", StringComparison.Ordinal),
    "received-item dispatch holds a current-session lease so replacement cannot overtake it");
True(connectMethod.Contains("ConnectionLifecycle.Abandon(generation)", StringComparison.Ordinal),
    "connection exception cleanup ends its synchronization generation");
True(shutdownMethod.Contains("ConnectionLifecycle.Shutdown(current =>", StringComparison.Ordinal) &&
     shutdownMethod.Contains("GarageCartridgeAccess.EndServerSync(current.Generation);", StringComparison.Ordinal),
    "deliberate shutdown atomically closes admission and ends the current synchronization generation");
True(clearCurrentSessionMethod.Contains("ConnectionLifecycle.TryEnd(session, generation, () =>", StringComparison.Ordinal) &&
     clearCurrentSessionMethod.Contains("GarageCartridgeAccess.EndServerSync(generation);", StringComparison.Ordinal) &&
     clearCurrentSessionMethod.Contains("terminal?.Invoke();", StringComparison.Ordinal),
    "terminal teardown and reconnect decisions run inside the exclusive lifecycle claim");

True(configureMethod.Contains("ServerSyncLifecycle.EndActive", StringComparison.Ordinal) &&
     configureMethod.Contains("ResetNativeBagObservations", StringComparison.Ordinal),
    "Garage configuration resets server synchronization and native bag observations");
False(configureMethod.Contains("new GarageCartridgeInsertionCoordinator", StringComparison.Ordinal),
    "Garage configuration must retain monotonic pending writes in the long-lived coordinator");
True(itemCallback.Contains("RequestUnityReconciliation", StringComparison.Ordinal),
    "received-item callbacks queue Garage reconciliation on the Unity thread");
False(storageSource.Contains("TryFlushPendingNativeGrants", StringComparison.Ordinal),
    "data-storage continuations never invoke native reconciliation directly");

True(nativeGrantMethod.Contains("GarageCartridgeInsertionPolicy.DecideGrant", StringComparison.Ordinal),
    "native grants are gated by server insertion state");
True(nativeGrantMethod.Contains("InsertionCoordinator.GetServerValue", StringComparison.Ordinal),
    "native grants read terminal insertion state from the coordinator");
True(garageSource.Contains("private static readonly GarageCartridgeReconciliationAccess ServerSyncLifecycle = new();", StringComparison.Ordinal),
    "Garage server synchronization uses the non-recursive reconciliation access seam");
True(garageSource.Contains("ServerSyncLifecycle.TryBegin(generation", StringComparison.Ordinal),
    "Garage begin is atomic with respect to an earlier close tombstone");
True(garageSource.Contains("ServerSyncLifecycle.End(generation", StringComparison.Ordinal),
    "Garage end atomically invalidates its active generation");
True(nativeGrantMethod.Contains("ServerSyncLifecycle.TryRunCurrent", StringComparison.Ordinal) &&
     nativeGrantMethod.Contains("current.Observe(() => ObserveNativeBagWithinLease", StringComparison.Ordinal),
    "native observation, eligibility, and mutation share one active-generation lease through the grant");
False(nativeGrantMethod.Contains("ObserveNativeBag(\n", StringComparison.Ordinal),
    "native reconciliation does not recursively enter the public lease-acquiring observer");
False(nativeGrantMethod.Contains("TryReadCartridgeCollected", StringComparison.Ordinal),
    "native cartridge-collected enquiries are not insertion state");
False(nativeGrantMethod.Contains("HasGarageCartridgeBeenCollected", StringComparison.Ordinal),
    "the failed native registration experiment is removed");
False(nativeGrantMethod.Contains("NativeCartridgeType", StringComparison.Ordinal),
    "diagnostic cartridge enum metadata is not terminal insertion state");
string garageApInsertionAndGrantMethods = string.Join("\n", observeNativeBagMethod, noteGarageObjectReleasedMethod, nativeGrantMethod);
False(garageApInsertionAndGrantMethods.Contains("_COLLECTED", StringComparison.OrdinalIgnoreCase),
    "Garage AP insertion and grant methods never submit a native collected-source flag");
True(captureGarageProcessorMethod.Contains("ResetNativeBagObservations", StringComparison.Ordinal),
    "PlayerSaveRequestProcessor replacement resets native bag evidence");
True(pluginSource.Contains("GarageCartridgeAccess.ResetNativeBagObservations(\"selected save slot mutation\")", StringComparison.Ordinal) &&
     pluginSource.Contains("GarageCartridgeAccess.ResetNativeBagObservations(\"player save state rebuilt\")", StringComparison.Ordinal) &&
     pluginSource.Contains("GarageCartridgeAccess.ResetNativeBagObservations(\"most recent save selection\")", StringComparison.Ordinal),
    "save selection and rebuild boundaries reset native bag evidence");
True(garageSource.Contains("GarageCartridgeAccess.ResetNativeBagObservations(\n                $\"room transition", StringComparison.Ordinal),
    "room transition resets visit-local native bag evidence before reconciliation");
True(releaseGarageObjectMethod.Contains("ServerSyncLifecycle.TryRunCurrent", StringComparison.Ordinal) &&
     releaseGarageObjectMethod.Contains("InsertionCoordinator.InitialSyncReady", StringComparison.Ordinal) &&
     releaseGarageObjectMethod.Contains("GarageCartridgeInsertionPolicy.ShouldReleaseObject", StringComparison.Ordinal) &&
     releaseGarageObjectMethod.Contains("current.Release(() =>", StringComparison.Ordinal) &&
     releaseGarageObjectMethod.Contains("RecordGarageObjectReleasedWithinLease", StringComparison.Ordinal),
    "keeper release eligibility, Unity action, and release recording share the current-generation lease");
True(garageSource.Contains("_releaseVisit.Poll(", StringComparison.Ordinal) &&
     garageSource.Contains("GarageCartridgeAccess.TryReleaseCartridgeObject(", StringComparison.Ordinal) &&
     garageSource.Contains("_releaseVisit.WasReleased(cartridge.Song)", StringComparison.Ordinal),
    "keeper revalidates every active poll while retaining one-shot visit release state");

{
    var access = new GarageCartridgeReconciliationAccess();
    True(access.TryBegin(41, () => { }), "production Garage reconciliation access begins a generation");
    var observations = 0;
    var decisions = 0;
    True(access.TryRunCurrent(current =>
    {
        Equal(41L, current.Generation, "reconciliation access exposes the leased generation");
        current.Observe(() => observations++);
        decisions++;
    }), "one production reconciliation access call owns observation and decision without recursive gate acquisition");
    Equal(1, observations, "the leased observation executes exactly once");
    Equal(1, decisions, "decision work executes under the same lease after observation");
    True(access.End(41, () => { }), "production Garage reconciliation access ends the generation");
}

{
    var access = new GarageCartridgeReconciliationAccess();
    True(access.TryBegin(42, () => { }), "overlap regression generation begins");
    using var releaseActionEntered = new ManualResetEventSlim();
    using var allowReleaseActionExit = new ManualResetEventSlim();
    using var endStarted = new ManualResetEventSlim();
    using var endCompleted = new ManualResetEventSlim();
    var releaseActionCount = 0;
    Task releaseTask = Task.Run(() =>
    {
        True(access.TryRunCurrent(current => current.Release(() =>
        {
            Interlocked.Increment(ref releaseActionCount);
            releaseActionEntered.Set();
            True(allowReleaseActionExit.Wait(TimeSpan.FromSeconds(1)),
                "leased release action is allowed to exit");
        })), "release action enters through the current-generation seam");
    });
    True(releaseActionEntered.Wait(TimeSpan.FromSeconds(1)),
        "release action enters before concurrent teardown");
    Task endTask = Task.Run(() =>
    {
        endStarted.Set();
        True(access.End(42, () => { }), "concurrent teardown ends the release generation");
        endCompleted.Set();
    });
    True(endStarted.Wait(TimeSpan.FromSeconds(1)), "concurrent teardown attempt starts");
    False(endCompleted.Wait(TimeSpan.FromMilliseconds(100)),
        "teardown cannot complete while object activation and release recording are in flight");
    allowReleaseActionExit.Set();
    await Task.WhenAll(releaseTask, endTask);
    True(endCompleted.IsSet, "teardown completes after the release action exits");
    Equal(1, releaseActionCount, "overlapped release action executes exactly once");
}

{
    var access = new GarageCartridgeReconciliationAccess();
    True(access.TryBegin(43, () => { }), "teardown-first generation begins");
    True(access.End(43, () => { }), "teardown wins before the release action");
    var releaseActionCount = 0;
    False(access.TryRunCurrent(current => current.Release(() => releaseActionCount++)),
        "teardown winning first rejects the stale release action");
    Equal(0, releaseActionCount, "rejected stale release action cannot activate an object");
}

static bool TryManagedReleasePoll(
    GarageCartridgeReconciliationAccess access,
    GarageInsertionServerValue serverValue,
    bool releasedThisVisit,
    Action releaseAction)
{
    bool accepted = false;
    access.TryRunCurrent(current =>
    {
        accepted = GarageCartridgeInsertionPolicy.ShouldReleaseObject(
            usesPhysicalVanillaEntrance: false,
            serverValue);
        if (accepted && !releasedThisVisit)
            current.Release(releaseAction);
    });
    return accepted;
}

{
    var access = new GarageCartridgeReconciliationAccess();
    var visit = new GarageCartridgeReleaseVisitCoordinator();
    var serverValue = GarageInsertionServerValue.NotInserted;
    var objectActive = false;
    var releaseNotifications = 0;
    var deactivations = 0;
    True(access.TryBegin(44, () => { }), "inserted-transition visit generation begins");

    True(visit.Poll(
            "Bloody Tears",
            (releasedThisVisit, releaseAction) => TryManagedReleasePoll(
                access,
                serverValue,
                releasedThisVisit,
                releaseAction),
            () =>
            {
                objectActive = true;
                releaseNotifications++;
            },
            () =>
            {
                objectActive = false;
                deactivations++;
            }),
        "authoritative not-inserted poll releases the object");
    True(objectActive, "successful release activates the object");
    True(visit.WasReleased("Bloody Tears"), "successful release records the visit marker");

    True(visit.Poll(
            "Bloody Tears",
            (releasedThisVisit, releaseAction) => TryManagedReleasePoll(
                access,
                serverValue,
                releasedThisVisit,
                releaseAction),
            () => releaseNotifications++,
            () => deactivations++),
        "later active poll revalidates authoritative not-inserted state");
    Equal(1, releaseNotifications, "valid visit release notification remains one-shot");

    serverValue = GarageInsertionServerValue.Inserted;
    False(visit.Poll(
            "Bloody Tears",
            (releasedThisVisit, releaseAction) => TryManagedReleasePoll(
                access,
                serverValue,
                releasedThisVisit,
                releaseAction),
            () => releaseNotifications++,
            () =>
            {
                objectActive = false;
                deactivations++;
            }),
        "next active poll rejects an object after insertion becomes terminal");
    False(objectActive, "inserted transition deactivates the formerly released object");
    False(visit.WasReleased("Bloody Tears"), "inserted transition clears the keeper visit marker");
    Equal(1, releaseNotifications, "inserted transition does not repeat release notification");
    Equal(1, deactivations, "inserted transition deactivates exactly once");
    True(access.End(44, () => { }), "inserted-transition visit generation ends");
}

{
    var access = new GarageCartridgeReconciliationAccess();
    var visit = new GarageCartridgeReleaseVisitCoordinator();
    var objectActive = false;
    var releaseNotifications = 0;
    var deactivations = 0;
    True(access.TryBegin(45, () => { }), "teardown revalidation visit generation begins");
    True(visit.Poll(
            "Bloody Tears",
            (releasedThisVisit, releaseAction) => TryManagedReleasePoll(
                access,
                GarageInsertionServerValue.NotInserted,
                releasedThisVisit,
                releaseAction),
            () =>
            {
                objectActive = true;
                releaseNotifications++;
            },
            () => deactivations++),
        "pre-teardown authoritative poll releases the object");
    True(access.End(45, () => { }), "teardown wins before the next active poll");

    False(visit.Poll(
            "Bloody Tears",
            (releasedThisVisit, releaseAction) => TryManagedReleasePoll(
                access,
                GarageInsertionServerValue.NotInserted,
                releasedThisVisit,
                releaseAction),
            () => releaseNotifications++,
            () =>
            {
                objectActive = false;
                deactivations++;
            }),
        "next active poll rejects release after teardown");
    False(objectActive, "teardown revalidation deactivates the formerly released object");
    False(visit.WasReleased("Bloody Tears"), "teardown revalidation clears the keeper visit marker");
    Equal(1, releaseNotifications, "teardown does not repeat release notification");
    Equal(1, deactivations, "teardown revalidation deactivates exactly once");
}

{
    var lifecycle = new GenerationLeaseGate();
    var beginCount = 0;
    var endCount = 0;
    False(lifecycle.End(1, () => endCount++),
        "closing before server synchronization begins records a tombstone without ending inactive work");
    False(lifecycle.TryBegin(1, () => beginCount++),
        "a close that wins before begin prevents the closed generation from starting");
    Equal(0, beginCount, "close-before-begin never invokes coordinator begin");
    Equal(0, endCount, "close-before-begin never invokes coordinator end without active work");
}

{
    var lifecycle = new GenerationLeaseGate();
    True(lifecycle.TryBegin(2, () => { }), "grant-race generation begins");
    bool eligibilitySnapshot = true;
    True(lifecycle.End(2, () => { }), "disconnect ends the grant-race generation");
    var grantCount = 0;
    if (eligibilitySnapshot && lifecycle.TryAcquire(2, out LifecycleLease? grantLease))
    {
        using (grantLease)
            grantCount++;
    }
    Equal(0, grantCount,
        "a generation lease revalidation prevents a native grant after disconnect ends eligibility");
}

{
    var lifecycle = new GenerationLeaseGate();
    True(lifecycle.TryBegin(3, () => { }), "leased generation begins");
    True(lifecycle.TryAcquire(3, out LifecycleLease? grantLease), "active generation grants a lease");
    using var endStarted = new ManualResetEventSlim();
    using var endCompleted = new ManualResetEventSlim();
    Task endTask = Task.Run(() =>
    {
        endStarted.Set();
        lifecycle.End(3, () => { });
        endCompleted.Set();
    });
    True(endStarted.Wait(TimeSpan.FromSeconds(1)), "disconnect attempt starts");
    False(endCompleted.Wait(TimeSpan.FromMilliseconds(100)),
        "disconnect cannot complete while a native grant lease is active");
    grantLease!.Dispose();
    True(endCompleted.Wait(TimeSpan.FromSeconds(1)), "disconnect completes after the native lease ends");
    await endTask;
}

{
    object oldSession = new();
    object newSession = new();
    var sessions = new SessionGenerationLeaseGate<object>();
    True(sessions.TryAdmit(1), "old callback generation is admitted");
    True(sessions.TryPublish(oldSession, 1, _ => { }, out _), "old callback session is published");
    True(sessions.TryAdmit(2), "replacement callback generation is admitted");
    True(sessions.TryPublish(newSession, 2, _ => { }, out _), "replacement callback session is published");
    var dispatchCount = 0;
    if (sessions.TryAcquire(oldSession, 1, out LifecycleLease? callbackLease))
    {
        using (callbackLease)
            dispatchCount++;
    }
    Equal(0, dispatchCount,
        "a callback whose session was replaced before dispatch cannot mutate managed item state");
}

{
    object oldSession = new();
    object newSession = new();
    var sessions = new SessionGenerationLeaseGate<object>();
    True(sessions.TryAdmit(1), "leased callback generation is admitted");
    True(sessions.TryPublish(oldSession, 1, _ => { }, out _), "leased callback session is published");
    True(sessions.TryAcquire(oldSession, 1, out LifecycleLease? callbackLease),
        "current callback acquires an atomic dispatch lease");
    using var replaceStarted = new ManualResetEventSlim();
    using var replaceCompleted = new ManualResetEventSlim();
    Task replaceTask = Task.Run(() =>
    {
        replaceStarted.Set();
        True(sessions.TryAdmit(2), "replacement generation is admitted after callback lease");
        True(sessions.TryPublish(newSession, 2, _ => { }, out _), "replacement publishes after callback lease");
        replaceCompleted.Set();
    });
    True(replaceStarted.Wait(TimeSpan.FromSeconds(1)), "replacement attempt starts");
    False(replaceCompleted.Wait(TimeSpan.FromMilliseconds(100)),
        "replacement cannot complete while current callback dispatch holds its lease");
    callbackLease!.Dispose();
    True(replaceCompleted.Wait(TimeSpan.FromSeconds(1)), "replacement completes after callback dispatch ends");
    await replaceTask;
}

{
    object session = new();
    var sessions = new SessionGenerationLeaseGate<object>();
    True(sessions.TryAdmit(1), "pre-publication attempt is admitted");
    var shutdownCount = 0;
    GenerationSession<object> shutdownCurrent = sessions.Shutdown(_ => shutdownCount++);
    Equal(null, shutdownCurrent.Session, "shutdown before publication has no published session");
    Equal(1, shutdownCount, "shutdown transition executes exactly once");
    var publishCount = 0;
    False(sessions.TryPublish(session, 1, _ => publishCount++, out _),
        "an attempt admitted before shutdown cannot publish after shutdown wins");
    False(sessions.TryAdmit(2), "shutdown permanently rejects later connection attempts");
    Equal(0, publishCount, "shutdown-before-publication never invokes publication work");
}

{
    object oldSession = new();
    object newSession = new();
    var sessions = new SessionGenerationLeaseGate<object>();
    True(sessions.TryAdmit(1), "terminal-race generation is admitted");
    True(sessions.TryPublish(oldSession, 1, _ => { }, out _), "terminal-race session is published");
    True(sessions.TryAdmit(2), "replacement attempt is admitted before the old terminal callback");
    using var terminalStarted = new ManualResetEventSlim();
    using var releaseTerminal = new ManualResetEventSlim();
    using var replacementStarted = new ManualResetEventSlim();
    using var replacementCompleted = new ManualResetEventSlim();
    var order = 0;
    var terminalOrder = 0;
    var replacementOrder = 0;
    Task terminalTask = Task.Run(() =>
    {
        True(sessions.TryEnd(oldSession, 1, () =>
        {
            terminalStarted.Set();
            True(releaseTerminal.Wait(TimeSpan.FromSeconds(1)), "terminal callback is released");
            terminalOrder = Interlocked.Increment(ref order);
        }), "old terminal callback ends its current generation");
    });
    True(terminalStarted.Wait(TimeSpan.FromSeconds(1)), "terminal callback owns the lifecycle claim");
    Task replacementTask = Task.Run(() =>
    {
        replacementStarted.Set();
        True(sessions.TryPublish(newSession, 2, _ =>
        {
            replacementOrder = Interlocked.Increment(ref order);
        }, out _), "replacement publishes after terminal work");
        replacementCompleted.Set();
    });
    True(replacementStarted.Wait(TimeSpan.FromSeconds(1)), "replacement attempt starts while terminal work is active");
    False(replacementCompleted.Wait(TimeSpan.FromMilliseconds(100)),
        "replacement cannot publish while old terminal teardown and reconnect decisions are active");
    releaseTerminal.Set();
    True(replacementCompleted.Wait(TimeSpan.FromSeconds(1)), "replacement completes after terminal work");
    await Task.WhenAll(terminalTask, replacementTask);
    True(terminalOrder > 0 && replacementOrder > terminalOrder,
        "old terminal side effects finish before the replacement becomes current");
}

var inserted = new GarageInsertionObservation(true, true, false, GarageInsertionServerValue.NotInserted, true, true, true, true, true, false);
True(GarageCartridgeInsertionPolicy.ShouldRecordInsertion(inserted), "released AP cartridge held-to-absent transition records insertion");
var cases = new[] { inserted with { InGarage = false }, inserted with { PreviousBagReadable = false }, inserted with { PreviousBagHeld = false }, inserted with { ReleasedThisVisit = false }, inserted with { ApOwned = false }, inserted with { ServerValue = GarageInsertionServerValue.Unknown }, inserted with { ServerValue = GarageInsertionServerValue.Inserted }, inserted with { Compatible = false }, inserted with { UsesPhysicalVanillaEntrance = true } };
foreach (var observation in cases) False(GarageCartridgeInsertionPolicy.ShouldRecordInsertion(observation), "insertion evidence gate");
False(GarageCartridgeInsertionPolicy.ShouldReleaseObject(false, GarageInsertionServerValue.Unknown),
    "randomized Garage cartridge stays unavailable while server insertion state is unknown");
True(GarageCartridgeInsertionPolicy.ShouldReleaseObject(false, GarageInsertionServerValue.NotInserted),
    "randomized Garage cartridge releases only from authoritative not-inserted state");
False(GarageCartridgeInsertionPolicy.ShouldReleaseObject(false, GarageInsertionServerValue.Inserted),
    "inserted randomized Garage cartridge is never released");
True(GarageCartridgeInsertionPolicy.ShouldReleaseObject(true, GarageInsertionServerValue.Unknown),
    "physical vanilla Garage entrance remains independent of AP server insertion state");

var cartridgeKeys = GarageCartridgeNativePolicy.RandomizedCartridges.ToDictionary(x => x.Song, x => x.ServerInsertionKey);

static async Task<GarageInsertionSequenceHarness> CreateSequenceHarness(
    IReadOnlyDictionary<string, string> keys,
    string song = "Bloody Tears",
    bool apOwned = true,
    bool usesPhysicalVanillaEntrance = false)
{
    var harness = new GarageInsertionSequenceHarness(keys, song)
    {
        ApOwned = apOwned,
        UsesPhysicalVanillaEntrance = usesPhysicalVanillaEntrance,
    };
    foreach (string key in harness.Store.ReadKeys)
        harness.Store.CompleteNextRead(key, GarageInsertionReadResult.KnownFalse);
    await Eventually(() => harness.Coordinator.InitialSyncReady, "sequence harness completes authoritative server synchronization");
    return harness;
}

{
    var sequence = await CreateSequenceHarness(cartridgeKeys);
    False(sequence.Observe(readable: true, held: false, inGarage: false),
        "AP ownership plus known-false server state and a missing bag outside Garage does not record insertion");
    Equal(GarageNativeGrantDecision.ApplyBagItem, sequence.GrantDecision(readable: true, held: false),
        "a missing bag outside Garage requests the native AP grant");
    Equal(0, sequence.Store.WriteKeys.Count,
        "a missing bag outside Garage never submits an insertion write");
}

{
    var sequence = await CreateSequenceHarness(cartridgeKeys);
    False(sequence.Observe(readable: true, held: true, inGarage: true),
        "authoritative held sample arms the real Garage sequence");
    sequence.NoteGarageObjectReleased();
    False(sequence.Observe(readable: false, held: false, inGarage: true),
        "an unreadable poll never implies cartridge absence");
    True(sequence.Observe(readable: true, held: false, inGarage: true),
        "held and readable through release, unreadable, then absent and readable records insertion");
    Equal(1, sequence.Store.WriteKeys.Count,
        "unreadable interstitial poll preserves the last authoritative held sample for one insertion write");
}

{
    var tracker = new GarageCartridgeInsertionTracker();
    False(tracker.Observe(
            "Bloody Tears",
            compatible: true,
            apOwned: true,
            usesPhysicalVanillaEntrance: false,
            GarageInsertionServerValue.NotInserted,
            readable: false,
            held: false,
            inGarage: true,
            releasedThisVisit: true),
        "an unreadable sample with no authoritative history never records absence");
    False(tracker.Observe(
            "Bloody Tears",
            compatible: true,
            apOwned: true,
            usesPhysicalVanillaEntrance: false,
            GarageInsertionServerValue.NotInserted,
            readable: true,
            held: false,
            inGarage: true,
            releasedThisVisit: true),
        "a later first readable absence still has no invented held evidence");
}

{
    var sequence = await CreateSequenceHarness(cartridgeKeys);
    False(sequence.Observe(readable: true, held: false, inGarage: true),
        "a first missing observation in Garage has no held-to-absent evidence");
    Equal(0, sequence.Store.WriteKeys.Count,
        "a first missing Garage observation never submits an insertion write");
}

{
    var sequence = await CreateSequenceHarness(cartridgeKeys);
    False(sequence.Observe(readable: true, held: true, inGarage: false),
        "holding a cartridge in Music Lab does not record insertion");
    False(sequence.Observe(readable: true, held: false, inGarage: true),
        "a bag that disappears in Garage before object release does not record insertion");
    sequence.NoteGarageObjectReleased();
    False(sequence.Observe(readable: true, held: false, inGarage: true),
        "release after an already-observed absence cannot reuse stale held evidence");
    Equal(0, sequence.Store.WriteKeys.Count,
        "pre-release disappearance never submits an insertion write");
}

{
    var sequence = await CreateSequenceHarness(cartridgeKeys);
    False(sequence.Observe(readable: true, held: true, inGarage: true),
        "held Garage bag observation arms history without recording insertion");
    sequence.NoteGarageObjectReleased();
    True(sequence.Observe(readable: true, held: false, inGarage: true),
        "released Garage cartridge held-to-absent transition records insertion");
    Equal(1, sequence.Store.WriteKeys.Count,
        "real Garage insertion submits exactly one server write");
    Equal(GarageNativeGrantDecision.AlreadyInserted, sequence.GrantDecision(readable: true, held: false),
        "in-process insertion suppresses native resurrection before write completion");

    False(sequence.Observe(readable: true, held: false, inGarage: true),
        "repeated absent polls do not record insertion again");
    False(sequence.Observe(readable: true, held: false, inGarage: true),
        "object deactivation after release does not record insertion again");
    False(sequence.Observe(readable: true, held: false, inGarage: false),
        "room exit does not record insertion again");
    Equal(1, sequence.Store.WriteKeys.Count,
        "repeated absence, object deactivation, and room exit do not duplicate writes");

    sequence.Store.CompleteNextWrite(cartridgeKeys[sequence.Song], GarageInsertionWriteResult.Succeeded);
    await Eventually(() => sequence.DurableConfirmations == 1,
        "server confirmation raises the durable transition once");
    sequence.Store.DuplicateLastWriteCompletion(GarageInsertionWriteResult.Succeeded);
    sequence.Coordinator.NoteInserted(sequence.Song);
    await Task.Delay(20);
    Equal(1, sequence.DurableConfirmations,
        "duplicate server callbacks do not duplicate the durable transition");
    Equal(1, sequence.Store.WriteKeys.Count,
        "duplicate server callbacks do not submit another write");

    sequence.ResetNativeBagObservations();
    True(sequence.ApOwned,
        "save observation reset retains AP ownership after insertion");
    Equal(GarageInsertionServerValue.Inserted, sequence.Coordinator.GetServerValue(sequence.Song),
        "save observation reset retains terminal server insertion state");
}

{
    var sequence = await CreateSequenceHarness(cartridgeKeys);
    False(sequence.Observe(readable: true, held: true, inGarage: true),
        "pre-reset held observation records history only");
    sequence.NoteGarageObjectReleased();
    sequence.ResetNativeBagObservations();
    True(sequence.ApOwned,
        "save observation reset retains AP ownership before insertion");
    Equal(GarageInsertionServerValue.NotInserted, sequence.Coordinator.GetServerValue(sequence.Song),
        "save observation reset retains known-false server insertion state");
    False(sequence.Observe(readable: true, held: false, inGarage: true),
        "save observation reset removes held and released evidence");
    Equal(0, sequence.Store.WriteKeys.Count,
        "absence after save observation reset cannot submit an insertion write");
}

{
    var tracker = new GarageCartridgeInsertionTracker();
    False(tracker.Observe(
            "Vampire Killer",
            compatible: true,
            apOwned: true,
            usesPhysicalVanillaEntrance: true,
            GarageInsertionServerValue.NotInserted,
            readable: true,
            held: true,
            inGarage: true,
            releasedThisVisit: false),
        "physical Vampire Killer held observation never arms an insertion");
    False(tracker.HasAuthoritativeHeldObservation("Vampire Killer"),
        "physical Vampire Killer never retains armed held evidence");
    False(tracker.Observe(
            "Vampire Killer",
            compatible: true,
            apOwned: true,
            usesPhysicalVanillaEntrance: true,
            GarageInsertionServerValue.NotInserted,
            readable: true,
            held: false,
            inGarage: true,
            releasedThisVisit: true),
        "physical Vampire Killer held-to-absent transition never records insertion");
}

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    coordinator.BeginConnection(1, store);
    Equal(string.Join("\n", expectedKeys.Select(x => x[(x.IndexOf('|') + 1)..])), string.Join("\n", store.ReadKeys), "beginning a connection reads every exact slot key once");
}

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    coordinator.BeginConnection(1, store);
    False(GarageCartridgeInsertionPolicy.ShouldReleaseObject(
            false,
            coordinator.GetServerValue("Bloody Tears")),
        "keeper release policy rejects the coordinator's initial unknown state");
    store.CompleteNextRead(cartridgeKeys["Bloody Tears"], GarageInsertionReadResult.KnownTrue);
    await Eventually(
        () => coordinator.GetServerValue("Bloody Tears") == GarageInsertionServerValue.Inserted,
        "coordinator transitions unknown insertion state to inserted");
    False(GarageCartridgeInsertionPolicy.ShouldReleaseObject(
            false,
            coordinator.GetServerValue("Bloody Tears")),
        "keeper release policy remains closed across unknown-to-inserted transition");
}

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    coordinator.BeginConnection(1, store);
    foreach (var key in store.ReadKeys.Take(4))
        store.CompleteNextRead(key, GarageInsertionReadResult.KnownFalse);
    await Task.Delay(20);
    False(coordinator.InitialSyncReady, "four completed reads do not open the initial sync gate");
    store.CompleteNextRead(store.ReadKeys[4], GarageInsertionReadResult.KnownTrue);
    await Eventually(() => coordinator.InitialSyncReady, "the fifth valid boolean opens the initial sync gate");
}

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    coordinator.BeginConnection(1, store);
    var song = "Bloody Tears";
    store.CompleteNextRead(cartridgeKeys[song], GarageInsertionReadResult.KnownFalse);
    await Eventually(() => coordinator.GetServerValue(song) == GarageInsertionServerValue.NotInserted, "an absent slot value becomes not inserted");
}

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    coordinator.BeginConnection(1, store);
    foreach (var key in store.ReadKeys)
        store.CompleteNextRead(key, key == cartridgeKeys["Bloody Tears"] ? GarageInsertionReadResult.Malformed : GarageInsertionReadResult.Failed);
    await Task.Delay(20);
    Equal(GarageInsertionServerValue.Unknown, coordinator.GetServerValue("Bloody Tears"), "malformed reads remain unknown");
    False(coordinator.InitialSyncReady, "malformed or failed reads keep the initial sync gate closed");
    coordinator.BeginConnection(2, store);
    Equal(10, store.ReadKeys.Count, "a new generation retries every read after a failed initial sync");
}

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    coordinator.BeginConnection(1, store);
    coordinator.BeginConnection(2, store);
    var key = cartridgeKeys["Bloody Tears"];
    store.CompleteNextRead(key, GarageInsertionReadResult.KnownTrue);
    await Task.Delay(20);
    Equal(GarageInsertionServerValue.Unknown, coordinator.GetServerValue("Bloody Tears"), "a stale read completion cannot affect the next generation");
    store.CompleteNextRead(key, GarageInsertionReadResult.KnownFalse);
    await Eventually(() => coordinator.GetServerValue("Bloody Tears") == GarageInsertionServerValue.NotInserted, "the current generation read determines the server value");
}

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    coordinator.BeginConnection(1, store);
    coordinator.NoteInserted("Bloody Tears");
    Equal(GarageInsertionServerValue.Inserted, coordinator.GetServerValue("Bloody Tears"), "noting an insertion updates memory before its write completes");
    Equal(1, store.WriteKeys.Count, "noting an insertion starts its write");
    coordinator.NoteInserted("Bloody Tears");
    Equal(1, store.WriteKeys.Count, "duplicate insertion notes submit at most one active write");
}

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    coordinator.BeginConnection(1, store);
    coordinator.NoteInserted("Bloody Tears");
    store.CompleteNextWrite(cartridgeKeys["Bloody Tears"], GarageInsertionWriteResult.Failed);
    await Task.Delay(20);
    coordinator.EndConnection(1);
    coordinator.BeginConnection(2, store);
    await Eventually(() => store.WriteKeys.Count == 2, "a failed write remains pending and retries after reconnect");
}

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    var durableSongs = new List<string>();
    coordinator.DurableInsertionConfirmed += durableSongs.Add;
    coordinator.BeginConnection(1, store);
    coordinator.NoteInserted("Bloody Tears");
    store.CompleteNextWrite(cartridgeKeys["Bloody Tears"], GarageInsertionWriteResult.Succeeded);
    await Eventually(() => durableSongs.Count == 1, "a confirmed write raises one durable transition");
    coordinator.RetryPendingWrites();
    await Task.Delay(20);
    Equal(1, store.WriteKeys.Count, "a confirmed write clears its pending state");
    Equal("Bloody Tears", string.Join(",", durableSongs), "a confirmed write logs only one durable transition");
}

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    coordinator.BeginConnection(1, store);
    foreach (var key in store.ReadKeys)
        store.CompleteNextRead(key, GarageInsertionReadResult.KnownTrue);
    await Eventually(() => coordinator.InitialSyncReady, "a restarted client completes its initial sync");
    Equal(GarageInsertionServerValue.Inserted, coordinator.GetServerValue("Bloody Tears"), "server true after restart prevents returning to not inserted");
}

Console.WriteLine("Game Garage cartridge persistence tests passed.");

sealed class FakeGarageInsertionDataStore : IGarageInsertionDataStore
{
    private readonly Dictionary<string, Queue<TaskCompletionSource<GarageInsertionReadResult>>> _reads = new();
    private readonly Dictionary<string, Queue<TaskCompletionSource<GarageInsertionWriteResult>>> _writes = new();

    public List<string> ReadKeys { get; } = new();
    public List<string> WriteKeys { get; } = new();
    private TaskCompletionSource<GarageInsertionWriteResult>? _lastWriteCompletion;

    public Task<GarageInsertionReadResult> ReadAsync(string key, CancellationToken cancellationToken)
    {
        ReadKeys.Add(key);
        return Enqueue(_reads, key).Task;
    }

    public Task<GarageInsertionWriteResult> WriteTrueAsync(string key, CancellationToken cancellationToken)
    {
        WriteKeys.Add(key);
        _lastWriteCompletion = Enqueue(_writes, key);
        return _lastWriteCompletion.Task;
    }

    public void CompleteNextRead(string key, GarageInsertionReadResult result) => Dequeue(_reads, key).SetResult(result);
    public void CompleteNextWrite(string key, GarageInsertionWriteResult result) => Dequeue(_writes, key).SetResult(result);
    public void DuplicateLastWriteCompletion(GarageInsertionWriteResult result) => _lastWriteCompletion?.TrySetResult(result);

    private static TaskCompletionSource<T> Enqueue<T>(Dictionary<string, Queue<TaskCompletionSource<T>>> pending, string key)
    {
        if (!pending.TryGetValue(key, out var queue))
            pending[key] = queue = new Queue<TaskCompletionSource<T>>();
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        queue.Enqueue(completion);
        return completion;
    }

    private static TaskCompletionSource<T> Dequeue<T>(Dictionary<string, Queue<TaskCompletionSource<T>>> pending, string key)
    {
        if (!pending.TryGetValue(key, out var queue) || queue.Count == 0)
            throw new InvalidOperationException($"No pending operation for {key}.");
        return queue.Dequeue();
    }
}

sealed class GarageInsertionSequenceHarness
{
    private readonly GarageCartridgeInsertionTracker _tracker = new();

    public GarageInsertionSequenceHarness(IReadOnlyDictionary<string, string> keys, string song)
    {
        Song = song;
        Store = new FakeGarageInsertionDataStore();
        Coordinator = new GarageCartridgeInsertionCoordinator(keys);
        Coordinator.DurableInsertionConfirmed += _ => DurableConfirmations++;
        Coordinator.BeginConnection(1, Store);
    }

    public string Song { get; }
    public FakeGarageInsertionDataStore Store { get; }
    public GarageCartridgeInsertionCoordinator Coordinator { get; }
    public bool ApOwned { get; set; }
    public bool UsesPhysicalVanillaEntrance { get; set; }
    public bool ReleasedThisVisit { get; private set; }
    public int DurableConfirmations { get; private set; }

    public void NoteGarageObjectReleased() => ReleasedThisVisit = true;

    public bool Observe(bool readable, bool held, bool inGarage)
    {
        bool recorded = _tracker.Observe(
            Song,
            compatible: true,
            ApOwned,
            UsesPhysicalVanillaEntrance,
            Coordinator.GetServerValue(Song),
            readable,
            held,
            inGarage,
            ReleasedThisVisit);
        if (recorded)
            Coordinator.NoteInserted(Song);
        return recorded;
    }

    public GarageNativeGrantDecision GrantDecision(bool readable, bool held) =>
        GarageCartridgeInsertionPolicy.DecideGrant(
            compatible: true,
            ApOwned,
            UsesPhysicalVanillaEntrance,
            Coordinator.GetServerValue(Song),
            readable,
            held);

    public void ResetNativeBagObservations()
    {
        _tracker.Reset();
        ReleasedThisVisit = false;
    }
}
