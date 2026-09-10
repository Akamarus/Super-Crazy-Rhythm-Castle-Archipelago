using RhythmCastleAP;
using Newtonsoft.Json.Linq;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}
static void True(bool value, string scenario) => Equal(true, value, scenario);
static void False(bool value, string scenario) => Equal(false, value, scenario);
static string ReadNormalizedSource(string path) =>
    File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
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

static (string Song, string Kind)? ClassifyProgressionFlag(string flag)
{
    GarageCartridgeProgressionFlag? classification =
        GarageCartridgeNativePolicy.ClassifyProgressionFlag(flag);
    if (classification is not { } classified)
        return null;
    return (classified.Cartridge.Song, classified.Kind.ToString());
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
False(GarageCartridgeNativePolicy.RandomizedCartridges.Any(x => x.Song == "Vampire Killer"),
    "the focused persistence catalog excludes physical Vampire Killer from randomized cartridges");

string[] exactSourceAliases =
{
    "Bloody Tears|LEVEL_27_CARTRIDGE_BLOODYTEARS_BAG_ITEM|LEVEL_27_CARTRIDGE_BLOODYTEARS_COLLECTED",
    "Gradius Remix|LEVEL_27_CARTRIDGE_GRADIUS_BAG_ITEM|LEVEL_27_CARTRIDGE_GRADIUS_COLLECTED",
    "Smooch|LEVEL_27_CARTRIDGE_SMOOCH_BAG_ITEM|LEVEL_27_CARTRIDGE_SMOOCH_COLLECTED",
    "Superstar|LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM|LEVEL_27_CARTRIDGE_STAR_EATER_COLLECTED",
    "Wag the Dog|LEVEL_27_CARTRIDGE_SUPER_CRAZY_RHYTHM_CASTLE_BAG_ITEM|LEVEL_27_CARTRIDGE_SUPER_CRAZY_RHYTHM_CASTLE_COLLECTED",
};
foreach (string expectedAlias in exactSourceAliases)
{
    string[] fields = expectedAlias.Split('|');
    Equal((fields[0], "BagItem"), ClassifyProgressionFlag(fields[1]),
        $"{fields[0]} exact bag alias is classified for native grant suppression");
    Equal((fields[0], "Collected"), ClassifyProgressionFlag(fields[2]),
        $"{fields[0]} exact collected alias is classified for its AP source check");
    True(GarageCartridgeNativePolicy.ShouldSuppressVanillaSourceGrant(fields[1], true),
        $"{fields[0]} exact bag alias is behaviorally suppressed for a Vampire-vanilla seed");
    False(GarageCartridgeNativePolicy.ShouldSuppressVanillaSourceGrant(fields[2], true),
        $"{fields[0]} collected source marker is retained while only its bag grant is suppressed");
}

Equal(("Vampire Killer", "BagItem"),
    ClassifyProgressionFlag("LEVEL_27_CARTRIDGE_VAMPIREKILLER_BAG_ITEM"),
    "the exact physical Vampire Killer bag alias remains cataloged");
Equal(("Vampire Killer", "Collected"),
    ClassifyProgressionFlag("LEVEL_27_CARTRIDGE_VAMPIREKILLER_COLLECTED"),
    "the exact physical Vampire Killer collected alias remains cataloged");
False(GarageCartridgeNativePolicy.ShouldSuppressVanillaSourceGrant(
        "LEVEL_27_CARTRIDGE_VAMPIREKILLER_BAG_ITEM",
        true),
    "physical Vampire Killer remains vanilla and is never suppressed for a Vampire-vanilla seed");
Equal<(string Song, string Kind)?>(null,
    ClassifyProgressionFlag("LEVEL_27_CARTRIDGE_SUPERSTAR_BAG_ITEM"),
    "a heuristic Superstar lookalike is not accepted as a native source alias");
Equal(GarageNativeGrantDecision.WaitForServer, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, GarageInsertionServerValue.Unknown, true, false), "unknown server state fails closed");
Equal(GarageNativeGrantDecision.ApplyBagItem, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, GarageInsertionServerValue.NotInserted, true, false), "known not-inserted missing bag grants once");
Equal(GarageNativeGrantDecision.AlreadyInserted, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, GarageInsertionServerValue.Inserted, true, false), "inserted state is terminal");
Equal(GarageNativeGrantDecision.None, GarageCartridgeInsertionPolicy.DecideGrant(false, true, false, GarageInsertionServerValue.NotInserted, true, false), "incompatible routing");
Equal(GarageNativeGrantDecision.None, GarageCartridgeInsertionPolicy.DecideGrant(true, false, false, GarageInsertionServerValue.NotInserted, true, false), "no AP ownership");
Equal(GarageNativeGrantDecision.WaitForNativeRead, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, GarageInsertionServerValue.NotInserted, false, false), "unreadable native bag");
Equal(GarageNativeGrantDecision.AlreadyHeld, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, GarageInsertionServerValue.NotInserted, true, true), "already-held bag");
Equal(GarageNativeGrantDecision.None, GarageCartridgeInsertionPolicy.DecideGrant(true, true, true, GarageInsertionServerValue.NotInserted, true, false), "physical vanilla cartridge");
Equal(GarageNativeGrantDecision.WaitForServer, GarageCartridgeInsertionPolicy.DecideGrant(true, true, false, (GarageInsertionServerValue)99, true, false), "unrecognized server state fails closed");

string pluginSource = ReadNormalizedSource(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));
string storageSource = ReadNormalizedSource(Path.Combine(Directory.GetCurrentDirectory(), "client", "GarageCartridgeInsertionStorage.cs"));
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
string noteGarageObjectReleasedMethod = MethodBody(garageSource, "public static void NoteGarageObjectReleased(", "private static bool RecordGarageObjectReleasedWithinLease(");
string captureGarageProcessorMethod = MethodBody(garageSource, "public static void CapturePlayerSaveRequestProcessor(object? instance)", "public static void TryFlushPendingNativeGrants()");
string nativeGrantMethod = MethodBody(garageSource, "public static void TryFlushPendingNativeGrants()", "public static void ApplySlotData(");
string releaseGarageObjectMethod = MethodBody(garageSource, "public static bool TryReleaseCartridgeObject(", "public static void NoteGarageObjectReleased(string song)");
string garageEntryTransitionRequestMethod = MethodBody(garageSource, "public static void RecordGarageEntryTransitionRequest(", "public static void RecordNativeBagProgressionRequest(");
string nativeProgressionRequestMethod = MethodBody(garageSource, "public static void RecordNativeBagProgressionRequest(", "public static void RecordNativeBagProgressionFlagUpdated(");
string nativeProgressionEventMethod = MethodBody(garageSource, "public static void RecordNativeBagProgressionFlagUpdated(", "private static void TryArmPendingNativeConsumption(");
string nativeProgressionArmMethod = MethodBody(garageSource, "private static void TryArmPendingNativeConsumption(", "public static bool TryReleaseCartridgeObject(");
string progressionRequestPrefixMethod = MethodBody(pluginSource, "public static bool ProgressionRequestPrefix(", "private static readonly string[] GateKeywords");
string progressionFlagEventMethod = MethodBody(pluginSource, "public static void ProgressionFlagEventPostfix(", "public static void BagItemRequestPostfix(");
int introRedirectClassStart = pluginSource.IndexOf("internal static class IntroRoomToHubRedirectPatches", StringComparison.Ordinal);
if (introRedirectClassStart < 0)
    throw new InvalidOperationException("Could not locate IntroRoomToHubRedirectPatches.");
string introRedirectSource = pluginSource[introRedirectClassStart..];
string introRedirectPrefixMethod = MethodBody(introRedirectSource, "public static bool Prefix(", "private static object? BuildIdentifier(");

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

True(observeNativeBagMethod.Contains("InsertionTracker.Reconcile(", StringComparison.Ordinal) &&
     nativeGrantMethod.Contains("ObserveNativeBagWithinLease(", StringComparison.Ordinal),
    "native grants flow through the production reconciliation adapter");
True(nativeProgressionArmMethod.Contains("InsertionTracker.RecordProgressionRequest(", StringComparison.Ordinal) &&
     nativeProgressionArmMethod.Contains("InsertionTracker.RecordProgressionFlagUpdated(", StringComparison.Ordinal),
    "native progression signals flow through the production reconciliation adapter");
True(nativeProgressionArmMethod.Contains("ServerSyncLifecycle.TryRunProgressionRequest(", StringComparison.Ordinal) &&
     nativeProgressionArmMethod.Contains("ServerSyncLifecycle.TryRunProgressionFlagUpdated(", StringComparison.Ordinal),
    "native progression signals use the production pre-lease admission seam");
True(nativeProgressionArmMethod.Contains(
         "GarageCartridgeRoomPolicy.IsGarageApproachRoom(room)",
         StringComparison.Ordinal) &&
     nativeProgressionArmMethod.Contains("inGarageApproachRoom", StringComparison.Ordinal),
    "production signal wiring identifies only the exact Garage approach room");
True(garageEntryTransitionRequestMethod.Contains("ServerSyncLifecycle.TryRunCurrent", StringComparison.Ordinal) &&
     garageEntryTransitionRequestMethod.Contains("current.Observe(() =>", StringComparison.Ordinal) &&
     garageEntryTransitionRequestMethod.Contains("InsertionTracker.RecordGarageEntryTransitionRequest(", StringComparison.Ordinal),
    "the exact Garage transition request binds evidence through the production generation lifecycle and adapter");
int garageEntryAttemptBinding = introRedirectPrefixMethod.IndexOf(
    "GarageCartridgeAccess.RecordGarageEntryTransitionRequest(originRoom, roomId);",
    StringComparison.Ordinal);
int roomPublication = introRedirectPrefixMethod.IndexOf(
    "DeveloperHarness.CaptureGameFlow(__instance, request);",
    StringComparison.Ordinal);
True(garageEntryAttemptBinding >= 0 &&
     roomPublication > garageEntryAttemptBinding,
    "the accepted transition request binds pre-entry evidence before production publishes the Garage room");
False(string.Join("\n", nativeProgressionRequestMethod, nativeProgressionEventMethod, nativeProgressionArmMethod)
        .Contains("NoteInserted", StringComparison.Ordinal),
    "native progression signals cannot directly mark durable insertion");
True(nativeGrantMethod.Contains("InsertionCoordinator.GetServerValue", StringComparison.Ordinal),
    "native grants read terminal insertion state from the coordinator");
True(garageSource.Contains("private static readonly GarageCartridgeReconciliationAccess ServerSyncLifecycle = new();", StringComparison.Ordinal),
    "Garage server synchronization uses the non-recursive reconciliation access seam");
True(garageSource.Contains("ServerSyncLifecycle.TryBegin(generation", StringComparison.Ordinal),
    "Garage begin is atomic with respect to an earlier close tombstone");
True(garageSource.Contains("ServerSyncLifecycle.End(generation", StringComparison.Ordinal),
    "Garage end atomically invalidates its active generation");
True(nativeGrantMethod.Contains("ServerSyncLifecycle.TryRunCurrent", StringComparison.Ordinal) &&
     nativeGrantMethod.Contains("current.Observe(() =>", StringComparison.Ordinal) &&
     nativeGrantMethod.Contains("ObserveNativeBagWithinLease(", StringComparison.Ordinal),
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
     releaseGarageObjectMethod.Contains("InsertionTracker.ShouldReleaseObject", StringComparison.Ordinal) &&
     releaseGarageObjectMethod.Contains("current.Release(() =>", StringComparison.Ordinal) &&
     releaseGarageObjectMethod.Contains("RecordGarageObjectReleasedWithinLease", StringComparison.Ordinal),
    "keeper release eligibility, Unity action, and release recording share the current-generation lease");
True(garageSource.Contains("_releaseVisit.Poll(", StringComparison.Ordinal) &&
     garageSource.Contains("GarageCartridgeAccess.TryReleaseCartridgeObject(", StringComparison.Ordinal) &&
     garageSource.Contains("_releaseVisit.WasReleased(cartridge.Song)", StringComparison.Ordinal),
    "keeper revalidates every active poll while retaining one-shot visit release state");
True(releaseGarageObjectMethod.Contains("InsertionTracker.ShouldReleaseObject", StringComparison.Ordinal) &&
     noteGarageObjectReleasedMethod.Contains("InsertionTracker.ShouldReleaseObject", StringComparison.Ordinal),
    "both production release entry points use the behaviorally tested release-eligibility adapter");

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
    True(access.TryBegin(410, () => { }),
        "reentrant native-grant regression begins a real production generation");
    var nativeGrantCallbacks = 0;
    var nestedSignalCallbacks = 0;
    bool requestAdmitted = true;
    bool updateAdmitted = true;

    True(access.TryRunCurrent(current => current.Observe(() =>
    {
        nativeGrantCallbacks++;
        requestAdmitted = access.TryRunProgressionRequest(
            value: true,
            nested => nested.Observe(() => nestedSignalCallbacks++));
        updateAdmitted = access.TryRunProgressionFlagUpdated(
            flagIsSet: true,
            flagWasSet: false,
            nested => nested.Observe(() => nestedSignalCallbacks++));
    })), "AP-authored native grant completes inside its original current-generation lease");

    False(requestAdmitted,
        "mutation admitting a nested true request reacquires the non-recursive generation lease");
    False(updateAdmitted,
        "mutation admitting a nested true update reacquires the non-recursive generation lease");
    Equal(1, nativeGrantCallbacks,
        "the outer native grant callback completes exactly once");
    Equal(0, nestedSignalCallbacks,
        "true request/update callbacks reject before any nested leased work executes");
}

{
    var access = new GarageCartridgeReconciliationAccess();
    True(access.TryBegin(411, () => { }),
        "standalone signal-admission regression begins a real production generation");
    var admittedCallbacks = 0;
    var rejectedCallbacks = 0;

    True(access.TryRunProgressionRequest(
            value: false,
            current => current.Observe(() => admittedCallbacks++)),
        "a genuine false request enters the current-generation seam");
    True(access.TryRunProgressionFlagUpdated(
            flagIsSet: false,
            flagWasSet: true,
            current => current.Observe(() => admittedCallbacks++)),
        "a genuine false-from-true update enters the current-generation seam");
    False(access.TryRunProgressionRequest(
            value: true,
            current => current.Observe(() => rejectedCallbacks++)),
        "a standalone true request rejects before lifecycle admission");
    False(access.TryRunProgressionRequest(
            value: null,
            current => current.Observe(() => rejectedCallbacks++)),
        "a null request rejects before lifecycle admission");
    False(access.TryRunProgressionFlagUpdated(
            flagIsSet: false,
            flagWasSet: false,
            current => current.Observe(() => rejectedCallbacks++)),
        "a false-to-false update rejects before lifecycle admission");
    False(access.TryRunProgressionFlagUpdated(
            flagIsSet: true,
            flagWasSet: false,
            current => current.Observe(() => rejectedCallbacks++)),
        "a true update rejects before lifecycle admission");
    False(access.TryRunProgressionFlagUpdated(
            flagIsSet: null,
            flagWasSet: true,
            current => current.Observe(() => rejectedCallbacks++)),
        "a null update rejects before lifecycle admission");
    Equal(2, admittedCallbacks,
        "both genuine consumption signal shapes retain leased execution");
    Equal(0, rejectedCallbacks,
        "non-consumption signals execute no leased callback");
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
    bool currentVisitNativeBagHeldObserved,
    bool releasedThisVisit,
    Action releaseAction)
{
    bool accepted = false;
    access.TryRunCurrent(current =>
    {
        accepted = GarageCartridgeInsertionPolicy.ShouldReleaseObject(
            usesPhysicalVanillaEntrance: false,
            serverValue,
            currentVisitNativeBagHeldObserved);
        if (accepted && !releasedThisVisit)
            current.Release(releaseAction);
    });
    return accepted;
}

{
    var access = new GarageCartridgeReconciliationAccess();
    var keeperVisit = new GarageCartridgeReleaseVisitCoordinator();
    var nativeObservations = new GarageCartridgeInsertionTracker();
    var releaseActionCount = 0;
    True(access.TryBegin(439, () => { }), "Garage-entry ordering generation begins");
    keeperVisit.SynchronizeResetEpoch(1);

    False(keeperVisit.Poll(
            "Bloody Tears",
            (releasedThisVisit, releaseAction) => TryManagedReleasePoll(
                access,
                GarageInsertionServerValue.NotInserted,
                nativeObservations.HasAuthoritativeHeldObservation("Bloody Tears"),
                releasedThisVisit,
                releaseAction),
            () => releaseActionCount++,
            () => { }),
        "Garage entry cannot release while the periodic native timer is not due and no current-visit sample exists");
    Equal(0, releaseActionCount,
        "the object remains closed before the first authoritative current-visit bag sample");

    False(nativeObservations.Observe(
            "Bloody Tears",
            compatible: true,
            apOwned: true,
            usesPhysicalVanillaEntrance: false,
            GarageInsertionServerValue.NotInserted,
            readable: true,
            held: true,
            inGarage: true,
            releasedThisVisit: false),
        "the first current-visit held sample records observation without insertion");
    True(keeperVisit.Poll(
            "Bloody Tears",
            (releasedThisVisit, releaseAction) => TryManagedReleasePoll(
                access,
                GarageInsertionServerValue.NotInserted,
                nativeObservations.HasAuthoritativeHeldObservation("Bloody Tears"),
                releasedThisVisit,
                releaseAction),
            () => releaseActionCount++,
            () => { }),
        "release becomes eligible only after the authoritative current-visit bag sample");
    Equal(1, releaseActionCount, "post-sample release performs one real Unity action");
    True(access.End(439, () => { }), "Garage-entry ordering generation ends");
}

{
    var access = new GarageCartridgeReconciliationAccess();
    var keeperVisit = new GarageCartridgeReleaseVisitCoordinator();
    var nativeObservations = new GarageCartridgeInsertionTracker();
    var releaseActionCount = 0;
    True(access.TryBegin(4391, () => { }), "absent-to-held release generation begins");
    keeperVisit.SynchronizeResetEpoch(1);

    False(nativeObservations.Observe(
            "Bloody Tears",
            compatible: true,
            apOwned: true,
            usesPhysicalVanillaEntrance: false,
            GarageInsertionServerValue.NotInserted,
            readable: true,
            held: false,
            inGarage: true,
            releasedThisVisit: false),
        "an authoritative readable-absent sample records observation without insertion");
    Equal(GarageNativeGrantDecision.ApplyBagItem,
        GarageCartridgeInsertionPolicy.DecideGrant(
            compatible: true,
            apOwned: true,
            usesPhysicalVanillaEntrance: false,
            GarageInsertionServerValue.NotInserted,
            nativeBagReadable: true,
            nativeBagHeld: false),
        "AP native grant reconciliation may remain active after the authoritative absent sample");
    False(keeperVisit.Poll(
            "Bloody Tears",
            (releasedThisVisit, releaseAction) => TryManagedReleasePoll(
                access,
                GarageInsertionServerValue.NotInserted,
                nativeObservations.HasAuthoritativeHeldObservation("Bloody Tears"),
                releasedThisVisit,
                releaseAction),
            () => releaseActionCount++,
            () => { }),
        "an authoritative readable-absent sample cannot release a randomized Garage cartridge");
    Equal(0, releaseActionCount,
        "the randomized Garage object stays closed until held evidence exists");

    False(nativeObservations.Observe(
            "Bloody Tears",
            compatible: true,
            apOwned: true,
            usesPhysicalVanillaEntrance: false,
            GarageInsertionServerValue.NotInserted,
            readable: true,
            held: true,
            inGarage: true,
            releasedThisVisit: false),
        "a subsequent authoritative readable-held sample records history without insertion");
    True(keeperVisit.Poll(
            "Bloody Tears",
            (releasedThisVisit, releaseAction) => TryManagedReleasePoll(
                access,
                GarageInsertionServerValue.NotInserted,
                nativeObservations.HasAuthoritativeHeldObservation("Bloody Tears"),
                releasedThisVisit,
                releaseAction),
            () => releaseActionCount++,
            () => { }),
        "a subsequent authoritative held sample permits randomized Garage release");
    Equal(1, releaseActionCount,
        "the held-gated release performs one real Unity action");
    True(access.End(4391, () => { }), "absent-to-held release generation ends");
}

{
    var access = new GarageCartridgeReconciliationAccess();
    var keeperVisit = new GarageCartridgeReleaseVisitCoordinator();
    var nativeObservations = new GarageCartridgeInsertionTracker();
    var releaseActionCount = 0;
    True(access.TryBegin(440, () => { }), "save-reset release generation begins");
    keeperVisit.SynchronizeResetEpoch(1);
    False(nativeObservations.Observe(
            "Bloody Tears",
            compatible: true,
            apOwned: true,
            usesPhysicalVanillaEntrance: false,
            GarageInsertionServerValue.NotInserted,
            readable: true,
            held: true,
            inGarage: true,
            releasedThisVisit: false),
        "the pre-release native sample records held state without insertion");
    True(keeperVisit.Poll(
            "Bloody Tears",
            (releasedThisVisit, releaseAction) => TryManagedReleasePoll(
                access,
                GarageInsertionServerValue.NotInserted,
                nativeObservations.HasAuthoritativeHeldObservation("Bloody Tears"),
                releasedThisVisit,
                releaseAction),
            () => releaseActionCount++,
            () => { }),
        "a current-visit authoritative sample permits the first real release");
    Equal(1, releaseActionCount, "the first release performs its real Unity action once");

    // A save/processor reset clears access-side evidence. The keeper-local marker
    // must consume the same reset epoch before another poll can claim release.
    nativeObservations.Reset();
    keeperVisit.SynchronizeResetEpoch(2);
    False(nativeObservations.Observe(
            "Bloody Tears",
            compatible: true,
            apOwned: true,
            usesPhysicalVanillaEntrance: false,
            GarageInsertionServerValue.NotInserted,
            readable: true,
            held: true,
            inGarage: true,
            releasedThisVisit: false),
        "the post-reset native sample is authoritative but is not release evidence");
    True(keeperVisit.Poll(
            "Bloody Tears",
            (releasedThisVisit, releaseAction) => TryManagedReleasePoll(
                access,
                GarageInsertionServerValue.NotInserted,
                nativeObservations.HasAuthoritativeHeldObservation("Bloody Tears"),
                releasedThisVisit,
                releaseAction),
            () => releaseActionCount++,
            () => { }),
        "save or processor reset requires and permits a new real release after a fresh sample");
    True(keeperVisit.WasReleased("Bloody Tears"),
        "only the new real release rearms current-epoch release evidence");
    Equal(2, releaseActionCount,
        "post-reset polling cannot rearm release evidence without another real Unity action");
    True(access.End(440, () => { }), "save-reset release generation ends");
}

{
    const string song = "Superstar";
    const string garageRoom = "GameRoom_27";
    var keys = GarageCartridgeNativePolicy.RandomizedCartridges.ToDictionary(
        cartridge => cartridge.Song,
        cartridge => cartridge.ServerInsertionKey);
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(keys);
    var access = new GarageCartridgeReconciliationAccess();
    var reconciliation = new GarageCartridgeReconciliationAdapter();
    var visit = new GarageCartridgeReleaseVisitCoordinator();
    var objectActive = false;
    var releaseActions = 0;
    var deactivations = 0;
    var nativeBagGrantCallbacks = 0;
    var durableInsertionConfirmations = 0;
    coordinator.DurableInsertionConfirmed += _ => durableInsertionConfirmations++;
    True(access.TryBegin(44, () => coordinator.BeginConnection(44, store)),
        "inserted-availability production generation begins");
    foreach (string key in store.ReadKeys.ToArray())
    {
        store.CompleteNextRead(
            key,
            string.Equals(key, keys[song], StringComparison.Ordinal)
                ? GarageInsertionReadResult.KnownTrue
                : GarageInsertionReadResult.KnownFalse);
    }
    await Eventually(() => coordinator.InitialSyncReady,
        "inserted-availability production server synchronization completes");
    Equal(GarageInsertionServerValue.Inserted, coordinator.GetServerValue(song),
        "server-confirmed Superstar begins terminally inserted");
    True(GarageCartridgeRoomPolicy.IsGarage(garageRoom),
        "the availability regression runs in authoritative GameRoom_27 context");
    visit.SynchronizeResetEpoch(reconciliation.ResetEpoch);

    GarageCartridgeReconciliationResult ReconcileInserted(bool releasedThisVisit)
    {
        GarageCartridgeReconciliationResult result = default;
        True(access.TryRunCurrent(current =>
            current.Observe(() =>
                result = reconciliation.Reconcile(
                    song,
                    compatible: true,
                    apOwned: true,
                    usesPhysicalVanillaEntrance: false,
                    coordinator.GetServerValue(song),
                    readable: true,
                    held: false,
                    inGarage: GarageCartridgeRoomPolicy.IsGarage(garageRoom),
                    releasedThisVisit,
                    current.Generation,
                    coordinator.NoteInserted,
                    value =>
                    {
                        nativeBagGrantCallbacks++;
                        return new GarageNativeGrantSubmission(true, $"unexpected grant value={value}");
                    }))),
            "inserted Superstar reconciles through the active production generation");
        return result;
    }

    bool PollGarageVisit()
    {
        if (!GarageCartridgeRoomPolicy.IsGarage(garageRoom))
            return false;
        return visit.Poll(
            song,
            (releasedThisVisit, releaseAction) =>
            {
                bool accepted = false;
                access.TryRunCurrent(current =>
                {
                    accepted = reconciliation.ShouldReleaseObject(
                        song,
                        usesPhysicalVanillaEntrance: false,
                        coordinator.GetServerValue(song),
                        reconciliation.ResetEpoch);
                    if (accepted && !releasedThisVisit)
                        current.Release(releaseAction);
                });
                return accepted;
            },
            () =>
            {
                objectActive = true;
                releaseActions++;
            },
            () =>
            {
                objectActive = false;
                deactivations++;
            });
    }

    GarageCartridgeReconciliationResult beforeRelease = ReconcileInserted(releasedThisVisit: false);
    Equal(GarageNativeGrantDecision.AlreadyInserted, beforeRelease.GrantDecision,
        "terminal inserted state suppresses native bag regrant while the bag is absent");
    False(beforeRelease.RecordedInsertion,
        "server-confirmed inserted state cannot record duplicate consumption");
    Equal(0, nativeBagGrantCallbacks,
        "inserted availability never invokes the native bag-grant callback");
    Equal(0, store.WriteKeys.Count,
        "loading inserted state and reconciling native absence queues no insertion write");

    True(PollGarageVisit(),
        "server-inserted AP-owned Superstar releases through the real Garage visit path");
    True(objectActive,
        "the inserted Superstar song object becomes playable in GameRoom_27");
    True(visit.WasReleased(song),
        "the inserted Superstar release records the current Garage visit");
    True(PollGarageVisit(),
        "later polling revalidates inserted Superstar availability in the same visit");
    Equal(1, releaseActions,
        "inserted Superstar performs exactly one real release action per Garage visit");
    Equal(0, deactivations,
        "valid inserted Superstar availability never deactivates its song object");

    GarageCartridgeReconciliationResult afterRelease = ReconcileInserted(releasedThisVisit: true);
    Equal(GarageNativeGrantDecision.AlreadyInserted, afterRelease.GrantDecision,
        "release does not weaken terminal bag-regrant suppression");
    False(afterRelease.RecordedInsertion,
        "release cannot duplicate the already-terminal insertion event");
    Equal(0, nativeBagGrantCallbacks,
        "no reconciliation around inserted availability invokes a native bag grant");
    Equal(0, store.WriteKeys.Count,
        "inserted availability performs no duplicate storage write");
    Equal(0, durableInsertionConfirmations,
        "inserted availability emits no duplicate durable-insertion confirmation");
    Equal(GarageInsertionServerValue.Inserted, coordinator.GetServerValue(song),
        "availability leaves inserted state terminal and true");

    reconciliation.ResetForRoomTransition(44);
    visit.SynchronizeResetEpoch(reconciliation.ResetEpoch);
    objectActive = false;
    True(PollGarageVisit(),
        "a later Garage visit releases server-inserted Superstar again for availability");
    True(PollGarageVisit(),
        "later-visit polling revalidates without repeating the release action");
    Equal(2, releaseActions,
        "each of two Garage visits performs one and only one release action");
    Equal(0, nativeBagGrantCallbacks,
        "later-visit availability still never invokes the native bag-grant callback");
    Equal(0, store.WriteKeys.Count,
        "later-visit availability still performs no insertion storage write");
    Equal(GarageInsertionServerValue.Inserted, coordinator.GetServerValue(song),
        "later-visit availability preserves terminal inserted state");
    True(access.End(44, () => coordinator.EndConnection(44)),
        "inserted-availability production generation ends cleanly");
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
                true,
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
                true,
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
False(GarageCartridgeInsertionPolicy.ShouldRecordInsertion(inserted with
    {
        CurrentBagReadable = false,
        CurrentBagHeld = true,
    }),
    "an unreadable current bag sample that carries a stale held value never records insertion");
var cases = new[] { inserted with { InGarage = false }, inserted with { PreviousBagReadable = false }, inserted with { PreviousBagHeld = false }, inserted with { ReleasedThisVisit = false }, inserted with { ApOwned = false }, inserted with { ServerValue = GarageInsertionServerValue.Unknown }, inserted with { ServerValue = GarageInsertionServerValue.Inserted }, inserted with { Compatible = false }, inserted with { UsesPhysicalVanillaEntrance = true } };
foreach (var observation in cases) False(GarageCartridgeInsertionPolicy.ShouldRecordInsertion(observation), "insertion evidence gate");
False(GarageCartridgeInsertionPolicy.ShouldReleaseObject(false, GarageInsertionServerValue.Unknown, true),
    "randomized Garage cartridge stays unavailable while server insertion state is unknown");
False(GarageCartridgeInsertionPolicy.ShouldReleaseObject(false, GarageInsertionServerValue.NotInserted, false),
    "randomized Garage cartridge stays unavailable before a current-visit native bag sample");
True(GarageCartridgeInsertionPolicy.ShouldReleaseObject(false, GarageInsertionServerValue.NotInserted, true),
    "randomized Garage cartridge releases only from authoritative not-inserted state");
True(GarageCartridgeInsertionPolicy.ShouldReleaseObject(false, GarageInsertionServerValue.Inserted, false),
    "inserted randomized Garage cartridge releases for song availability without a native bag item");
True(GarageCartridgeInsertionPolicy.ShouldReleaseObject(true, GarageInsertionServerValue.Unknown, false),
    "physical vanilla Garage entrance remains independent of AP server insertion state");

var cartridgeKeys = GarageCartridgeNativePolicy.RandomizedCartridges.ToDictionary(x => x.Song, x => x.ServerInsertionKey);

{
    var transport = new FakeArchipelagoGarageInsertionStorageTransport
    {
        ReadBehavior = (_, _, _) => Task.FromResult<JToken>(JToken.FromObject(false)),
    };
    var adapter = new ArchipelagoGarageInsertionDataStore(transport);
    Equal(GarageInsertionReadResult.KnownFalse,
        await adapter.ReadAsync("scrc:garage_inserted:v1:bloody_tears", CancellationToken.None),
        "an absent slot key initializes and reads back as known false");
    Equal(1, transport.ReadInitialValues.Count,
        "the production adapter initializes an absent key exactly once");
    Equal(JTokenType.Boolean, transport.ReadInitialValues[0].Type,
        "the absent-key initializer is a boolean token");
    False(transport.ReadInitialValues[0].Value<bool>(),
        "the absent-key initializer is the monotonic-safe false default");
}

{
    var transport = new FakeArchipelagoGarageInsertionStorageTransport
    {
        ReadBehavior = (_, _, _) => Task.FromResult<JToken>(JToken.FromObject("not-a-boolean")),
    };
    var adapter = new ArchipelagoGarageInsertionDataStore(transport);
    GarageInsertionReadResult result = await adapter.ReadAsync(
        "scrc:garage_inserted:v1:superstar",
        CancellationToken.None);
    Equal(GarageInsertionReadStatus.Malformed, result.Status,
        "a non-boolean server token remains malformed and fails closed");
    Equal("key='scrc:garage_inserted:v1:superstar' valueType='String'", result.Detail,
        "malformed diagnostics name only the safe slot key and token type");
    False(result.Detail.Contains("not-a-boolean", StringComparison.Ordinal),
        "malformed diagnostics never expose the server value");
}

{
    var exceptionTransport = new FakeArchipelagoGarageInsertionStorageTransport
    {
        ReadBehavior = (_, _, _) => Task.FromException<JToken>(new InvalidOperationException("read failed")),
    };
    var canceledTransport = new FakeArchipelagoGarageInsertionStorageTransport
    {
        ReadBehavior = (_, _, _) => Task.FromCanceled<JToken>(new CancellationToken(canceled: true)),
    };
    GarageInsertionReadResult exceptionResult = await new ArchipelagoGarageInsertionDataStore(exceptionTransport)
        .ReadAsync("scrc:garage_inserted:v1:smooch", CancellationToken.None);
    Equal(GarageInsertionReadStatus.Failed, exceptionResult.Status,
        "a production adapter read exception fails closed");
    Equal("key='scrc:garage_inserted:v1:smooch' failureType='InvalidOperationException'", exceptionResult.Detail,
        "read exception diagnostics expose only the safe exception type");
    GarageInsertionReadResult canceledResult = await new ArchipelagoGarageInsertionDataStore(canceledTransport)
        .ReadAsync("scrc:garage_inserted:v1:smooch", CancellationToken.None);
    Equal(GarageInsertionReadStatus.Failed, canceledResult.Status,
        "a production adapter read cancellation fails closed");
    Equal("key='scrc:garage_inserted:v1:smooch' failureType='canceled'", canceledResult.Detail,
        "read cancellation diagnostics remain explicit and safe");
}

{
    static ArchipelagoGarageInsertionDataStore AdapterFor(bool callbackConfirmed, bool rereadValue) =>
        new(new FakeArchipelagoGarageInsertionStorageTransport
        {
            WriteBehavior = (_, _) => Task.FromResult(new GarageInsertionWriteConfirmation(
                callbackConfirmed,
                JToken.FromObject(rereadValue))),
        });

    Equal(GarageInsertionWriteResult.Failed,
        await AdapterFor(callbackConfirmed: false, rereadValue: true)
            .WriteTrueAsync("scrc:garage_inserted:v1:bloody_tears", CancellationToken.None),
        "a false server callback cannot claim a durable write");
    Equal(GarageInsertionWriteResult.Failed,
        await AdapterFor(callbackConfirmed: true, rereadValue: false)
            .WriteTrueAsync("scrc:garage_inserted:v1:bloody_tears", CancellationToken.None),
        "a false authoritative reread cannot claim a durable write");
    Equal(GarageInsertionWriteResult.Succeeded,
        await AdapterFor(callbackConfirmed: true, rereadValue: true)
            .WriteTrueAsync("scrc:garage_inserted:v1:bloody_tears", CancellationToken.None),
        "only callback true plus reread true confirms a durable write");
}

{
    var exceptionTransport = new FakeArchipelagoGarageInsertionStorageTransport
    {
        WriteBehavior = (_, _) => Task.FromException<GarageInsertionWriteConfirmation>(
            new InvalidOperationException("write failed")),
    };
    var canceledTransport = new FakeArchipelagoGarageInsertionStorageTransport
    {
        WriteBehavior = (_, _) => Task.FromCanceled<GarageInsertionWriteConfirmation>(
            new CancellationToken(canceled: true)),
    };
    Equal(GarageInsertionWriteResult.Failed,
        await new ArchipelagoGarageInsertionDataStore(exceptionTransport)
            .WriteTrueAsync("scrc:garage_inserted:v1:wag_the_dog", CancellationToken.None),
        "a production adapter write exception remains pending rather than durable");
    Equal(GarageInsertionWriteResult.Failed,
        await new ArchipelagoGarageInsertionDataStore(canceledTransport)
            .WriteTrueAsync("scrc:garage_inserted:v1:wag_the_dog", CancellationToken.None),
        "a production adapter write cancellation remains pending rather than durable");
}

Equal(
    string.Join("\n", new[]
    {
        "GAME GARAGE INSERTION SYNC pending",
        "GAME GARAGE INSERTION SYNC ready",
        "GAME GARAGE INSERTION SYNC failed",
        "GAME GARAGE CARTRIDGE NATIVE GRANT APPLIED",
        "GAME GARAGE INSERTION CANDIDATE ARMED",
        "GAME GARAGE NATIVE CONSUMPTION OBSERVED",
        "GAME GARAGE SERVER INSERTION WRITE PENDING",
        "GAME GARAGE SERVER INSERTION CONFIRMED DURABLE",
        "GAME GARAGE ALREADY INSERTED NO REGRANT",
    }),
    string.Join("\n", new[]
    {
        GarageCartridgeDiagnostics.SyncPendingMarker,
        GarageCartridgeDiagnostics.SyncReadyMarker,
        GarageCartridgeDiagnostics.SyncFailedMarker,
        GarageCartridgeDiagnostics.NativeGrantAppliedMarker,
        GarageCartridgeDiagnostics.CandidateArmedMarker,
        GarageCartridgeDiagnostics.NativeConsumptionObservedMarker,
        GarageCartridgeDiagnostics.ServerWritePendingMarker,
        GarageCartridgeDiagnostics.ServerConfirmedDurableMarker,
        GarageCartridgeDiagnostics.AlreadyInsertedNoRegrantMarker,
    }),
    "the production diagnostic contract retains every documented exact marker");

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    var diagnostics = new List<GarageCartridgeDiagnostic>();
    coordinator.DiagnosticEmitted += diagnostics.Add;
    coordinator.BeginConnection(70, store);
    Equal("[SCRC-AP] GAME GARAGE INSERTION SYNC pending generation=70.", diagnostics.Single().Message,
        "beginning synchronization emits the exact pending transition");
    foreach (string key in store.ReadKeys)
        store.CompleteNextRead(key, GarageInsertionReadResult.KnownFalse);
    await Eventually(() => diagnostics.Count == 2, "ready synchronization emits its completion transition");
    Equal("[SCRC-AP] GAME GARAGE INSERTION SYNC ready generation=70.", diagnostics[1].Message,
        "five authoritative boolean reads emit the exact ready transition");
    Equal(GarageCartridgeDiagnosticLevel.Info, diagnostics[1].Level,
        "ready synchronization is informational");
}

{
    var store = new FakeGarageInsertionDataStore();
    var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
    var diagnostics = new List<GarageCartridgeDiagnostic>();
    coordinator.DiagnosticEmitted += diagnostics.Add;
    coordinator.BeginConnection(71, store);
    foreach (string key in store.ReadKeys)
    {
        store.CompleteNextRead(
            key,
            key == cartridgeKeys["Superstar"]
                ? GarageInsertionReadResult.MalformedValue(key, JTokenType.String)
                : GarageInsertionReadResult.KnownFalse);
    }
    await Eventually(() => diagnostics.Count == 2, "failed synchronization emits its completion transition");
    True(diagnostics[1].Message.StartsWith(
            "[SCRC-AP] GAME GARAGE INSERTION SYNC failed generation=71.",
            StringComparison.Ordinal),
        "a malformed value emits the exact failed transition");
    True(diagnostics[1].Message.Contains(
            "key='scrc:garage_inserted:v1:superstar' valueType='String'",
            StringComparison.Ordinal),
        "failed synchronization includes safe malformed key and value-type detail");
    False(diagnostics[1].Message.Contains("not-a-boolean", StringComparison.Ordinal),
        "failed synchronization cannot include a malformed raw value");
    Equal(GarageCartridgeDiagnosticLevel.Warning, diagnostics[1].Level,
        "failed synchronization is a warning");
}

Equal("[SCRC-AP] GAME GARAGE CARTRIDGE NATIVE GRANT APPLIED generation=72 song='Superstar' flag='LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM'. The normal Garage insertion path remains player-controlled. submitted",
    GarageCartridgeDiagnostics.NativeGrantApplied(
        72,
        "Superstar",
        "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
        "submitted").Message,
    "native grant submission emits the exact documented marker");
Equal("[SCRC-AP] GAME GARAGE INSERTION CANDIDATE ARMED song='Superstar'.",
    GarageCartridgeDiagnostics.CandidateArmed("Superstar").Message,
    "a released held cartridge emits the exact candidate marker");
Equal("[SCRC-AP] GAME GARAGE NATIVE CONSUMPTION OBSERVED song='Superstar'.",
    GarageCartridgeDiagnostics.NativeConsumptionObserved("Superstar").Message,
    "held-to-absent insertion emits the exact consumption marker");
Equal("[SCRC-AP] GAME GARAGE SERVER INSERTION WRITE PENDING song='Superstar'.",
    GarageCartridgeDiagnostics.ServerWritePending("Superstar").Message,
    "in-memory insertion emits the exact write-pending marker");
Equal("[SCRC-AP] GAME GARAGE SERVER INSERTION CONFIRMED DURABLE song='Superstar'.",
    GarageCartridgeDiagnostics.ServerConfirmedDurable("Superstar").Message,
    "confirmed callback plus reread emits the exact durable marker");
Equal("[SCRC-AP] GAME GARAGE ALREADY INSERTED NO REGRANT generation=72 song='Superstar'.",
    GarageCartridgeDiagnostics.AlreadyInsertedNoRegrant(72, "Superstar").Message,
    "terminal insertion reconciliation emits the exact no-regrant marker");

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
    var failures = new List<string>();
    void CheckEqual<T>(T expected, T actual, string mutation)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            failures.Add($"{mutation}: expected {expected}, got {actual}");
    }
    void CheckTrue(bool actual, string mutation) => CheckEqual(true, actual, mutation);
    void CheckFalse(bool actual, string mutation) => CheckEqual(false, actual, mutation);

    {
        var sequence = await CreateSequenceHarness(cartridgeKeys, "Superstar");
        False(sequence.Observe(readable: true, held: true, inGarage: true),
            "live-order sequence begins with authoritative held history");
        sequence.NoteGarageObjectReleased();
        CheckTrue(
            sequence.SignalProgressionRequest(
                "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
                value: false,
                inGarage: true),
            "mutation ignoring the exact in-Garage false request loses pending confirmation");
        CheckEqual(GarageInsertionServerValue.NotInserted, sequence.Coordinator.GetServerValue(sequence.Song),
            "mutation treating the progression signal alone as insertion violates authoritative readback");
        CheckEqual(0, sequence.Store.WriteKeys.Count,
            "mutation writing from the progression signal alone violates authoritative readback");

        long signalResetEpoch = sequence.ResetEpoch;
        sequence.RoomTransitionReset();
        CheckEqual(signalResetEpoch + 1, sequence.ResetEpoch,
            "mutation skipping the production room-transition epoch change retains stale identity");
        var afterExit = sequence.Reconcile(readable: true, held: false, inGarage: false);
        CheckTrue(afterExit.RecordedInsertion,
            "mutation clearing pending confirmation during the room-transition reset loses native consumption");
        CheckEqual(GarageNativeGrantDecision.AlreadyInserted, afterExit.GrantDecision,
            "mutation allowing regrant after transition resurrects a consumed cartridge");
        CheckEqual(1, sequence.Store.WriteKeys.Count,
            "mutation omitting authoritative post-transition confirmation loses the slot write");
        CheckFalse(sequence.HasPendingConsumption,
            "mutation retaining pending confirmation after authoritative absence can duplicate insertion");
    }

    {
        var sequence = await CreateSequenceHarness(cartridgeKeys, "Superstar");
        False(sequence.Observe(readable: true, held: true, inGarage: true),
            "event-only sequence begins with authoritative held history");
        sequence.NoteGarageObjectReleased();
        CheckTrue(
            sequence.SignalProgressionEvent(
                "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
                flagIsSet: false,
                flagWasSet: true,
                inGarage: true),
            "mutation ignoring the exact false update event loses pending confirmation");
        CheckEqual(GarageInsertionServerValue.NotInserted, sequence.Coordinator.GetServerValue(sequence.Song),
            "mutation treating the update event alone as durable insertion bypasses native readback");
        CheckEqual(0, sequence.Store.WriteKeys.Count,
            "mutation writing from the update event alone bypasses native readback");

        var unreadable = sequence.Reconcile(readable: false, held: false, inGarage: true);
        CheckFalse(unreadable.RecordedInsertion,
            "mutation treating unreadable confirmation as absence invents insertion");
        CheckEqual(GarageNativeGrantDecision.WaitForNativeRead, unreadable.GrantDecision,
            "mutation regranting while pending confirmation is unreadable is unsafe");
        CheckTrue(sequence.HasPendingConsumption,
            "mutation dropping pending confirmation on an unreadable sample permits later regrant");
        CheckEqual(0, sequence.Store.WriteKeys.Count,
            "mutation writing on unreadable confirmation invents durable evidence");

        sequence.RoomTransitionReset();
        CheckTrue(sequence.HasPendingConsumption,
            "mutation dropping unreadable pending confirmation at the first room boundary permits regrant");
        var stillUnreadable = sequence.Reconcile(readable: false, held: false, inGarage: false);
        CheckFalse(stillUnreadable.RecordedInsertion,
            "mutation treating post-transition unreadable state as absence invents insertion");
        sequence.RoomTransitionReset();
        CheckTrue(sequence.HasPendingConsumption,
            "mutation expiring safe unreadable pending confirmation at a later room boundary permits regrant");
        var eventuallyAbsent = sequence.Reconcile(readable: true, held: false, inGarage: false);
        CheckTrue(eventuallyAbsent.RecordedInsertion,
            "mutation failing to retain pending confirmation until authoritative readback loses consumption");
        CheckEqual(GarageNativeGrantDecision.AlreadyInserted, eventuallyAbsent.GrantDecision,
            "mutation regranting after delayed authoritative confirmation resurrects a consumed cartridge");
        CheckEqual(1, sequence.Store.WriteKeys.Count,
            "mutation omitting the delayed authoritative confirmation write loses durable insertion");
    }

    {
        var sequence = await CreateSequenceHarness(cartridgeKeys, "Superstar");
        False(sequence.Observe(readable: true, held: true, inGarage: true),
            "held-cancellation sequence begins with authoritative held history");
        sequence.NoteGarageObjectReleased();
        CheckTrue(
            sequence.SignalProgressionRequest(
                "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
                value: false,
                inGarage: true),
            "mutation refusing a valid pending signal prevents held cancellation from being tested");
        var heldAgain = sequence.Reconcile(readable: true, held: true, inGarage: true);
        CheckFalse(heldAgain.RecordedInsertion,
            "mutation confirming pending consumption while the bag is authoritatively held invents insertion");
        CheckEqual(GarageNativeGrantDecision.AlreadyHeld, heldAgain.GrantDecision,
            "mutation regranting an authoritatively held cartridge duplicates the native item");
        CheckFalse(sequence.HasPendingConsumption,
            "mutation retaining pending consumption after held readback can confirm a later unrelated absence");
        CheckEqual(0, sequence.Store.WriteKeys.Count,
            "mutation writing after held cancellation invents insertion");
    }

    {
        var outsideGarage = await CreateSequenceHarness(cartridgeKeys, "Superstar");
        False(outsideGarage.Observe(readable: true, held: true, inGarage: true),
            "ignored-signal sequence establishes held history");
        outsideGarage.NoteGarageObjectReleased();
        CheckFalse(outsideGarage.SignalProgressionRequest(
                "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
                value: false,
                inGarage: false),
            "mutation accepting false requests outside Game Garage captures unrelated state changes");
        CheckFalse(outsideGarage.SignalProgressionEvent(
                "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
                flagIsSet: false,
                flagWasSet: false,
                inGarage: true),
            "mutation accepting a false-to-false event invents native consumption");
        CheckFalse(outsideGarage.SignalProgressionEvent(
                "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
                flagIsSet: true,
                flagWasSet: false,
                inGarage: true),
            "mutation accepting a true update event mistakes a grant for consumption");
        CheckFalse(outsideGarage.SignalProgressionRequest(
                "LEVEL_27_CARTRIDGE_SMOOCH_BAG_ITEM",
                value: false,
                inGarage: true),
            "mutation accepting a different song flag cross-arms pending consumption");
        CheckFalse(outsideGarage.SignalProgressionRequest(
                "LEVEL_27_CARTRIDGE_STAR_EATER_COLLECTED",
                value: false,
                inGarage: true),
            "mutation accepting a collected marker as bag consumption corrupts insertion state");

        var withoutHeld = await CreateSequenceHarness(cartridgeKeys, "Superstar");
        withoutHeld.NoteGarageObjectReleased();
        CheckFalse(withoutHeld.SignalProgressionRequest(
                "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
                value: false,
                inGarage: true),
            "mutation arming without authoritative held history invents consumption");

        var withoutRelease = await CreateSequenceHarness(cartridgeKeys, "Superstar");
        False(withoutRelease.Observe(readable: true, held: true, inGarage: true),
            "no-release sequence establishes held history");
        CheckFalse(withoutRelease.SignalProgressionRequest(
                "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
                value: false,
                inGarage: true),
            "mutation arming without real object release evidence captures unrelated bag loss");

        var unowned = await CreateSequenceHarness(cartridgeKeys, "Superstar", apOwned: false);
        unowned.NoteGarageObjectReleased();
        CheckFalse(unowned.SignalProgressionRequest(
                "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
                value: false,
                inGarage: true),
            "mutation arming an unowned cartridge bypasses AP ownership");
    }

    {
        var tracker = new GarageCartridgeInsertionTracker();
        False(tracker.Observe(
                "Superstar",
                compatible: true,
                apOwned: true,
                usesPhysicalVanillaEntrance: false,
                GarageInsertionServerValue.NotInserted,
                readable: true,
                held: true,
                inGarage: true,
                releasedThisVisit: false),
            "server-state guard establishes held history");
        CheckFalse(tracker.TryArmPendingConsumption(
                "Superstar", false, null, false, true, true, false,
                GarageInsertionServerValue.Unknown, true, true, 1, 1),
            "mutation arming while server state is unknown bypasses synchronization");
        CheckFalse(tracker.TryArmPendingConsumption(
                "Superstar", false, null, false, true, true, false,
                GarageInsertionServerValue.Inserted, true, true, 1, 1),
            "mutation arming after terminal insertion can duplicate the transition");
        CheckFalse(tracker.TryArmPendingConsumption(
                "Superstar", false, null, false, true, false, false,
                GarageInsertionServerValue.NotInserted, true, true, 1, 1),
            "mutation arming an unowned cartridge bypasses AP ownership");
    }

    {
        var sequence = await CreateSequenceHarness(cartridgeKeys, "Superstar");
        False(sequence.Observe(readable: true, held: true, inGarage: true),
            "stale-evidence sequence establishes held history");
        sequence.NoteGarageObjectReleased();
        CheckTrue(sequence.SignalProgressionRequest(
                "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM",
                value: false,
                inGarage: true),
            "mutation dropping the pending signal prevents stale-generation validation");
        sequence.RoomTransitionReset(sequence.Generation + 1);
        CheckFalse(sequence.HasPendingConsumption,
            "mutation carrying pending evidence into another server generation is unsafe");
        var staleAbsence = sequence.Reconcile(readable: true, held: false, inGarage: false);
        CheckFalse(staleAbsence.RecordedInsertion,
            "mismatched-generation transition state cannot confirm another session's signal");
        CheckEqual(0, sequence.Store.WriteKeys.Count,
            "mutation converting mismatched-generation pending evidence into a write violates reset safety");
    }

    {
        var tracker = new GarageCartridgeInsertionTracker();
        False(tracker.Observe(
                "Vampire Killer",
                compatible: true,
                apOwned: true,
                usesPhysicalVanillaEntrance: false,
                GarageInsertionServerValue.NotInserted,
                readable: true,
                held: true,
                inGarage: true,
                releasedThisVisit: true),
            "Vampire Killer fixture establishes held evidence so physical-path rejection is independent");
        CheckFalse(tracker.TryArmPendingConsumption(
                "Vampire Killer", false, null, false, true, true, true,
                GarageInsertionServerValue.NotInserted, true, true, 1, 1),
            "mutation arming physical Vampire Killer violates its vanilla path");
        CheckFalse(tracker.TryArmPendingConsumption(
                "Vampire Killer", false, null, false, true, true, true,
                GarageInsertionServerValue.NotInserted,
                inGarage: false,
                releasedThisVisit: false,
                generation: 1,
                resetEpoch: 1,
                inGarageApproachRoom: true),
            "mutation accepting pre-Garage Vampire Killer evidence violates its physical vanilla path");
        CheckTrue(GarageCartridgeInsertionPolicy.ShouldReleaseObject(
                usesPhysicalVanillaEntrance: true,
                GarageInsertionServerValue.Unknown,
                currentVisitNativeBagHeldObserved: false),
            "mutation gating physical Vampire Killer on AP pending evidence breaks vanilla entry");
    }

    CheckTrue(progressionRequestPrefixMethod.Contains(
            "GarageCartridgeAccess.RecordNativeBagProgressionRequest(req, flag);",
            StringComparison.Ordinal),
        "mutation omitting the progression-request integration loses the synchronous false signal");
    CheckTrue(progressionFlagEventMethod.Contains(
            "GarageCartridgeAccess.RecordNativeBagProgressionFlagUpdated(evt, flag);",
            StringComparison.Ordinal),
        "mutation omitting the progression-update integration loses the synchronous false event");
    CheckTrue(garageSource.Contains("preservePendingConsumption: true", StringComparison.Ordinal),
        "mutation using a destructive room-transition reset loses valid pending confirmation");
    CheckTrue(garageSource.Contains("enteringGarageFromApproachRoom", StringComparison.Ordinal) &&
              garageSource.Contains(
                  "GarageCartridgeRoomPolicy.IsGarageEntryFromApproach(_lastRoom, room)",
                  StringComparison.Ordinal),
        "production room wiring corroborates pre-entry evidence only on exact Hub6-to-Garage entry");

    if (failures.Count != 0)
        throw new InvalidOperationException(
            "Garage transition-order regression failures:\n" + string.Join("\n", failures));
}

{
    const string superstar = "Superstar";
    const string superstarBagFlag = "LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM";
    const string garageApproachRoom = "GameRoom_Hub6";
    const string garageRoom = "GameRoom_27";
    const string unrelatedRoom = "GameRoom_Hub5";

    async Task<(
        GarageCartridgeReconciliationAdapter Adapter,
        GarageCartridgeReconciliationAccess Lifecycle,
        GarageCartridgeInsertionCoordinator Coordinator,
        FakeGarageInsertionDataStore Store)> CreateProductionFixture(
        Action<string>? durableInsertionConfirmed = null)
    {
        var adapter = new GarageCartridgeReconciliationAdapter();
        var lifecycle = new GarageCartridgeReconciliationAccess();
        var store = new FakeGarageInsertionDataStore();
        var coordinator = new GarageCartridgeInsertionCoordinator(cartridgeKeys);
        if (durableInsertionConfirmed != null)
            coordinator.DurableInsertionConfirmed += durableInsertionConfirmed;
        True(lifecycle.TryBegin(1, () => coordinator.BeginConnection(1, store)),
            "production reconciliation fixture begins the real generation lifecycle");
        foreach (string key in store.ReadKeys.ToArray())
            store.CompleteNextRead(key, GarageInsertionReadResult.KnownFalse);
        await Eventually(() => coordinator.InitialSyncReady,
            "production reconciliation fixture completes real server synchronization");
        return (adapter, lifecycle, coordinator, store);
    }

    GarageCartridgeReconciliationResult RunProductionReconciliation(
        (GarageCartridgeReconciliationAdapter Adapter,
         GarageCartridgeReconciliationAccess Lifecycle,
         GarageCartridgeInsertionCoordinator Coordinator,
         FakeGarageInsertionDataStore Store) fixture,
        List<bool> submittedGrantValues,
        string song,
        bool usesPhysicalVanillaEntrance,
        bool readable,
        bool held,
        bool inGarage,
        bool releasedThisVisit)
    {
        GarageCartridgeReconciliationResult result = default;
        True(fixture.Lifecycle.TryRunCurrent(current =>
            current.Observe(() =>
                result = fixture.Adapter.Reconcile(
                    song,
                    compatible: true,
                    apOwned: true,
                    usesPhysicalVanillaEntrance,
                    fixture.Coordinator.GetServerValue(song),
                    readable,
                    held,
                    inGarage,
                    releasedThisVisit,
                    current.Generation,
                    fixture.Coordinator.NoteInserted,
                    value =>
                    {
                        submittedGrantValues.Add(value);
                        return new GarageNativeGrantSubmission(true, "test processor accepted");
                    }))),
            "production reconciliation executes inside the current generation lease");
        return result;
    }

    bool RecordRequest(
        (GarageCartridgeReconciliationAdapter Adapter,
         GarageCartridgeReconciliationAccess Lifecycle,
         GarageCartridgeInsertionCoordinator Coordinator,
         FakeGarageInsertionDataStore Store) fixture,
        string flag,
        bool? value,
        bool compatible = true,
        bool apOwned = true,
        bool inGarage = true,
        bool releasedThisVisit = true,
        GarageInsertionServerValue? serverValue = null,
        bool inGarageApproachRoom = false)
    {
        bool armed = false;
        fixture.Lifecycle.TryRunProgressionRequest(value, current =>
            current.Observe(() =>
                armed = fixture.Adapter.RecordProgressionRequest(
                    flag,
                    value,
                    compatible,
                    apOwned,
                    serverValue ?? fixture.Coordinator.GetServerValue(superstar),
                    inGarage,
                    releasedThisVisit,
                    current.Generation,
                    inGarageApproachRoom)));
        return armed;
    }

    bool RecordEvent(
        (GarageCartridgeReconciliationAdapter Adapter,
         GarageCartridgeReconciliationAccess Lifecycle,
         GarageCartridgeInsertionCoordinator Coordinator,
         FakeGarageInsertionDataStore Store) fixture,
        string flag,
        bool? flagIsSet,
        bool? flagWasSet,
        bool inGarage = true,
        bool releasedThisVisit = true,
        bool inGarageApproachRoom = false)
    {
        bool armed = false;
        fixture.Lifecycle.TryRunProgressionFlagUpdated(
            flagIsSet,
            flagWasSet,
            current =>
            current.Observe(() =>
                armed = fixture.Adapter.RecordProgressionFlagUpdated(
                    flag,
                    flagIsSet,
                    flagWasSet,
                    compatible: true,
                    apOwned: true,
                    fixture.Coordinator.GetServerValue(superstar),
                    inGarage,
                    releasedThisVisit,
                    current.Generation,
                    inGarageApproachRoom)));
        return armed;
    }

    bool RecordGarageEntryTransitionRequest(
        (GarageCartridgeReconciliationAdapter Adapter,
         GarageCartridgeReconciliationAccess Lifecycle,
         GarageCartridgeInsertionCoordinator Coordinator,
         FakeGarageInsertionDataStore Store) fixture,
        string originRoom,
        string destinationRoom)
    {
        bool bound = false;
        bool admitted = fixture.Lifecycle.TryRunCurrent(current =>
            current.Observe(() =>
                bound = fixture.Adapter.RecordGarageEntryTransitionRequest(
                    originRoom,
                    destinationRoom,
                    current.Generation)));
        return admitted && bound;
    }

    False(GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
        "Hub6 is pre-entry evidence context, not premature Garage confirmation");
    True(GarageCartridgeRoomPolicy.IsGarageApproachRoom(garageApproachRoom),
        "production room policy identifies the exact Game Garage approach room");
    True(GarageCartridgeRoomPolicy.IsGarage(garageRoom),
        "production room policy identifies GameRoom_27 as authoritative Garage context");
    True(GarageCartridgeRoomPolicy.IsGarageEntryFromApproach(garageApproachRoom, garageRoom),
        "production room policy corroborates the exact Hub6-to-GameRoom_27 transition");
    False(GarageCartridgeRoomPolicy.IsGarageEntryFromApproach(garageApproachRoom, unrelatedRoom),
        "an unrelated departure from Hub6 cannot corroborate Garage consumption");

    {
        int durableConfirmations = 0;
        var fixture = await CreateProductionFixture(_ =>
            Interlocked.Increment(ref durableConfirmations));
        var submittedGrantValues = new List<bool>();
        _ = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true,
            held: true,
            inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
            releasedThisVisit: false);
        Equal(0, submittedGrantValues.Count,
            "Hub6 setup observes the authoritatively held AP cartridge without granting");

        True(RecordEvent(
                fixture,
                superstarBagFlag,
                flagIsSet: false,
                flagWasSet: true,
                inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
                releasedThisVisit: false,
                inGarageApproachRoom: GarageCartridgeRoomPolicy.IsGarageApproachRoom(garageApproachRoom)),
            "mutation rejecting the authentic pre-transition false update loses automatic Garage consumption");
        True(RecordRequest(
                fixture,
                superstarBagFlag,
                value: false,
                inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
                releasedThisVisit: false,
                inGarageApproachRoom: GarageCartridgeRoomPolicy.IsGarageApproachRoom(garageApproachRoom)),
            "mutation rejecting the authentic pre-transition false request loses automatic Garage consumption");
        True(RecordGarageEntryTransitionRequest(fixture, garageApproachRoom, garageRoom),
            "the exact allowed Hub6-to-GameRoom_27 request binds the pending entry attempt");
        Equal(GarageInsertionServerValue.NotInserted, fixture.Coordinator.GetServerValue(superstar),
            "pre-transition signals alone cannot mark durable insertion");
        Equal(0, fixture.Store.WriteKeys.Count,
            "pre-transition signals alone cannot write storage");

        GarageCartridgeReconciliationResult beforeRoomPublication = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true,
            held: false,
            inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
            releasedThisVisit: false);
        False(beforeRoomPublication.RecordedInsertion,
            "mutation confirming before GameRoom_27 publication accepts uncorroborated evidence");
        False(beforeRoomPublication.GrantAttempted,
            "mutation regranting while authentic pre-entry evidence waits loses native consumption");
        Equal(GarageNativeGrantDecision.WaitForGarageEntry, beforeRoomPublication.GrantDecision,
            "pre-entry absence remains explicitly pending until GameRoom_27 corroboration");
        Equal(0, submittedGrantValues.Count,
            "pre-entry evidence suppresses the native true callback until room corroboration");
        True(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "authentic pre-entry evidence remains generation-bound while awaiting GameRoom_27");

        long preTransitionEpoch = fixture.Adapter.ResetEpoch;
        True(fixture.Lifecycle.TryRunCurrent(current =>
            current.Observe(() => fixture.Adapter.ResetForRoomTransition(
                current.Generation,
                enteringGarageFromApproachRoom:
                    GarageCartridgeRoomPolicy.IsGarageEntryFromApproach(garageApproachRoom, garageRoom)))),
            "the exact Hub6-to-Garage reset runs inside the current generation lease");
        Equal(preTransitionEpoch + 1, fixture.Adapter.ResetEpoch,
            "the matching room transition advances the native-observation epoch");
        True(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "matching room transition carries and corroborates pre-entry consumption evidence");

        GarageCartridgeReconciliationResult garageConfirmation = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true,
            held: false,
            inGarage: GarageCartridgeRoomPolicy.IsGarage(garageRoom),
            releasedThisVisit: false);
        True(garageConfirmation.RecordedInsertion,
            "mutation failing to confirm after matching GameRoom_27 entry loses automatic consumption");
        Equal(GarageNativeGrantDecision.AlreadyInserted, garageConfirmation.GrantDecision,
            "Garage confirmation makes insertion terminal before any regrant decision");
        False(garageConfirmation.GrantAttempted,
            "confirmed automatic consumption never invokes the native true callback");
        Equal(0, submittedGrantValues.Count,
            "the exact pre-transition sequence performs no regrant");
        Equal(GarageInsertionServerValue.Inserted, fixture.Coordinator.GetServerValue(superstar),
            "Garage-corroborated absence marks the real coordinator terminal");
        Equal(1, fixture.Store.WriteKeys.Count,
            "Garage-corroborated absence queues exactly one monotonic insertion write");
        fixture.Store.CompleteNextWrite(
            cartridgeKeys[superstar],
            GarageInsertionWriteResult.Succeeded);
        await Eventually(
            () => Volatile.Read(ref durableConfirmations) == 1,
            "the exact pre-transition sequence reaches one durable insertion confirmation");
        Equal(1, fixture.Store.WriteKeys.Count,
            "durable confirmation does not submit a duplicate insertion write");
    }

    {
        var fixture = await CreateProductionFixture();
        var submittedGrantValues = new List<bool>();
        _ = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true,
            held: true,
            inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
            releasedThisVisit: false);
        True(RecordRequest(
                fixture,
                superstarBagFlag,
                value: false,
                inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
                releasedThisVisit: false,
                inGarageApproachRoom: GarageCartridgeRoomPolicy.IsGarageApproachRoom(garageApproachRoom)),
            "post-entry-unreadable fixture records authentic pre-entry evidence");
        True(RecordGarageEntryTransitionRequest(fixture, garageApproachRoom, garageRoom),
            "post-entry-unreadable fixture binds the exact Garage transition request");
        fixture.Adapter.ResetForRoomTransition(
            generation: 1,
            enteringGarageFromApproachRoom:
                GarageCartridgeRoomPolicy.IsGarageEntryFromApproach(garageApproachRoom, garageRoom));

        GarageCartridgeReconciliationResult unreadableInGarage = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: false,
            held: false,
            inGarage: GarageCartridgeRoomPolicy.IsGarage(garageRoom),
            releasedThisVisit: false);
        Equal(GarageNativeGrantDecision.WaitForNativeRead, unreadableInGarage.GrantDecision,
            "promoted pre-entry evidence waits for authoritative native readback in Garage");
        True(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "unreadable Garage observation retains matching pre-entry evidence for this room only");

        fixture.Adapter.ResetForRoomTransition(
            generation: 1,
            enteringGarageFromApproachRoom:
                GarageCartridgeRoomPolicy.IsGarageEntryFromApproach(garageRoom, unrelatedRoom));
        False(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "mutation carrying promoted pre-entry evidence across a later unrelated transition is unsafe");
        GarageCartridgeReconciliationResult absentOutsideGarage = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true,
            held: false,
            inGarage: GarageCartridgeRoomPolicy.IsGarage(unrelatedRoom),
            releasedThisVisit: false);
        False(absentOutsideGarage.RecordedInsertion,
            "later readable absence outside Garage cannot confirm pre-entry-derived evidence");
        Equal("True", string.Join(",", submittedGrantValues),
            "destroyed pre-entry evidence returns to exactly one ordinary native regrant");
        Equal(0, fixture.Store.WriteKeys.Count,
            "leaving Garage before readback cannot queue an insertion write");
    }

    {
        var fixture = await CreateProductionFixture();
        var submittedGrantValues = new List<bool>();
        _ = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true,
            held: true,
            inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
            releasedThisVisit: false);
        True(RecordRequest(
                fixture,
                superstarBagFlag,
                value: false,
                inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
                releasedThisVisit: false,
                inGarageApproachRoom: GarageCartridgeRoomPolicy.IsGarageApproachRoom(garageApproachRoom)),
            "stable-Hub6 fixture records an unbound false bag signal");
        False(RecordGarageEntryTransitionRequest(fixture, garageApproachRoom, unrelatedRoom),
            "an unrelated transition request cannot bind the Garage entry attempt");

        GarageCartridgeReconciliationResult stableHub6 = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true,
            held: false,
            inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
            releasedThisVisit: false);
        False(stableHub6.RecordedInsertion,
            "an unbound signal in stable Hub6 cannot record insertion");
        True(stableHub6.GrantAttempted && stableHub6.GrantSubmitted,
            "mutation retaining an unbound Hub6 signal suppresses regrant indefinitely");
        False(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "the first production Unity reconciliation aborts an unbound entry attempt");
        Equal("True", string.Join(",", submittedGrantValues),
            "stable Hub6 abort invokes exactly one ordinary native true grant");
        Equal(0, fixture.Store.WriteKeys.Count,
            "stable Hub6 abort never writes insertion storage");
    }

    {
        var fixture = await CreateProductionFixture();
        var submittedGrantValues = new List<bool>();
        _ = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true,
            held: true,
            inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
            releasedThisVisit: false);
        True(RecordEvent(
                fixture,
                superstarBagFlag,
                flagIsSet: false,
                flagWasSet: true,
                inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
                releasedThisVisit: false,
                inGarageApproachRoom: GarageCartridgeRoomPolicy.IsGarageApproachRoom(garageApproachRoom)),
            "held-cancellation fixture records pre-entry evidence");

        GarageCartridgeReconciliationResult contradicted = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true,
            held: true,
            inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
            releasedThisVisit: false);
        Equal(GarageNativeGrantDecision.AlreadyHeld, contradicted.GrantDecision,
            "authoritative held readback contradicts and cancels pre-entry consumption evidence");
        False(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "held readback cannot leave stale pre-entry evidence for a later absence");
        fixture.Adapter.ResetForRoomTransition(
            generation: 1,
            enteringGarageFromApproachRoom:
                GarageCartridgeRoomPolicy.IsGarageEntryFromApproach(garageApproachRoom, garageRoom));
        GarageCartridgeReconciliationResult laterAbsence = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true,
            held: false,
            inGarage: GarageCartridgeRoomPolicy.IsGarage(garageRoom),
            releasedThisVisit: false);
        False(laterAbsence.RecordedInsertion,
            "a later Garage absence cannot revive evidence canceled by held readback");
        Equal("True", string.Join(",", submittedGrantValues),
            "held-canceled evidence returns to exactly the ordinary native grant path");
        Equal(0, fixture.Store.WriteKeys.Count,
            "held-canceled pre-entry evidence never writes insertion storage");
    }

    {
        var fixture = await CreateProductionFixture();
        var submittedGrantValues = new List<bool>();
        _ = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true, held: true, inGarage: false, releasedThisVisit: false);
        True(RecordRequest(
                fixture,
                superstarBagFlag,
                value: false,
                inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
                releasedThisVisit: false,
                inGarageApproachRoom: GarageCartridgeRoomPolicy.IsGarageApproachRoom(garageApproachRoom)),
            "save-reset fixture records authentic pre-entry evidence");
        fixture.Adapter.Reset();
        False(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "save-style destructive reset clears pre-entry evidence");
        GarageCartridgeReconciliationResult afterSaveReset = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true, held: false, inGarage: false, releasedThisVisit: false);
        False(afterSaveReset.RecordedInsertion,
            "save reset cannot convert prior evidence into insertion");
        Equal(0, fixture.Store.WriteKeys.Count,
            "save reset cannot write insertion storage");
    }

    {
        var fixture = await CreateProductionFixture();
        var submittedGrantValues = new List<bool>();
        _ = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true, held: true, inGarage: false, releasedThisVisit: false);
        True(RecordEvent(
                fixture,
                superstarBagFlag,
                flagIsSet: false,
                flagWasSet: true,
                inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
                releasedThisVisit: false,
                inGarageApproachRoom: GarageCartridgeRoomPolicy.IsGarageApproachRoom(garageApproachRoom)),
            "disconnect fixture records authentic pre-entry evidence");
        True(fixture.Lifecycle.End(1, () => fixture.Coordinator.EndConnection(1)),
            "disconnect closes the matching production generation");
        fixture.Adapter.Reset();
        False(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "disconnect destructive reset clears pre-entry evidence");
        False(fixture.Lifecycle.TryRunCurrent(_ => throw new InvalidOperationException(
                "stale disconnected generation admitted reconciliation")),
            "disconnect rejects stale generation reconciliation");
        Equal(0, fixture.Store.WriteKeys.Count,
            "disconnect cannot convert pre-entry evidence into a storage write");
    }

    {
        var fixture = await CreateProductionFixture();
        var submittedGrantValues = new List<bool>();
        _ = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true, held: true, inGarage: false, releasedThisVisit: false);
        True(RecordRequest(
                fixture,
                superstarBagFlag,
                value: false,
                inGarage: GarageCartridgeRoomPolicy.IsGarage(garageApproachRoom),
                releasedThisVisit: false,
                inGarageApproachRoom: GarageCartridgeRoomPolicy.IsGarageApproachRoom(garageApproachRoom)),
            "generation-mismatch fixture records authentic pre-entry evidence");
        fixture.Adapter.ResetForRoomTransition(
            generation: 2,
            enteringGarageFromApproachRoom:
                GarageCartridgeRoomPolicy.IsGarageEntryFromApproach(garageApproachRoom, garageRoom));
        False(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "a different generation cannot carry pre-entry evidence into Garage");
        Equal(0, fixture.Store.WriteKeys.Count,
            "generation mismatch cannot write insertion storage");
    }

    {
        var fixture = await CreateProductionFixture();
        var submittedGrantValues = new List<bool>();
        GarageCartridgeReconciliationResult held = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true, held: true, inGarage: true, releasedThisVisit: false);
        Equal(GarageNativeGrantDecision.AlreadyHeld, held.GrantDecision,
            "production adapter records authoritative held state before release");
        Equal(0, submittedGrantValues.Count,
            "mutation granting an already-held cartridge bypasses native inventory");

        True(RecordRequest(fixture, superstarBagFlag, value: false),
            "mutation omitting the real request-to-adapter path loses pending confirmation");
        Equal(GarageInsertionServerValue.NotInserted, fixture.Coordinator.GetServerValue(superstar),
            "mutation calling NoteInserted from the request alone bypasses authoritative readback");
        Equal(0, fixture.Store.WriteKeys.Count,
            "mutation writing insertion from the request alone bypasses authoritative readback");
        Equal(0, submittedGrantValues.Count,
            "mutation submitting a native true grant from the request races vanilla consumption");

        long previousResetEpoch = fixture.Adapter.ResetEpoch;
        True(fixture.Lifecycle.TryRunCurrent(current =>
            current.Observe(() => fixture.Adapter.ResetForRoomTransition(current.Generation))),
            "production room reset executes inside the current generation lease");
        Equal(previousResetEpoch + 1, fixture.Adapter.ResetEpoch,
            "mutation skipping the production room reset leaves stale epoch identity");
        True(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "mutation destructively resetting pending state on the safe room transition loses consumption");

        GarageCartridgeReconciliationResult unreadable = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: false, held: false, inGarage: false, releasedThisVisit: false);
        False(unreadable.RecordedInsertion,
            "mutation treating unreadable native state as absence invents insertion");
        Equal(GarageNativeGrantDecision.WaitForNativeRead, unreadable.GrantDecision,
            "mutation bypassing production reconciliation can regrant while confirmation is unreadable");
        False(unreadable.GrantAttempted,
            "mutation submitting a native true grant while pending and unreadable resurrects the cartridge");
        Equal(0, submittedGrantValues.Count,
            "production grant callback must remain untouched while pending is unreadable");
        Equal(GarageInsertionServerValue.NotInserted, fixture.Coordinator.GetServerValue(superstar),
            "unreadable native state cannot invoke NoteInserted");
        Equal(0, fixture.Store.WriteKeys.Count,
            "unreadable native state cannot queue the durable write");

        long unreadableResetEpoch = fixture.Adapter.ResetEpoch;
        True(fixture.Lifecycle.TryRunCurrent(current =>
            current.Observe(() => fixture.Adapter.ResetForRoomTransition(current.Generation))),
            "a repeated safe room reset executes inside the same production generation lease");
        Equal(unreadableResetEpoch + 1, fixture.Adapter.ResetEpoch,
            "a repeated safe room reset advances the production adapter epoch");
        True(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "matching generation and epoch carry pending confirmation across repeated unreadable transitions");
        GarageCartridgeReconciliationResult stillUnreadable = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: false, held: false, inGarage: false, releasedThisVisit: false);
        False(stillUnreadable.RecordedInsertion || stillUnreadable.GrantAttempted,
            "repeated unreadable transitions neither invent insertion nor regrant");

        GarageCartridgeReconciliationResult absent = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true, held: false, inGarage: false, releasedThisVisit: false);
        True(absent.RecordedInsertion,
            "mutation omitting pending resolution from the real production adapter loses authoritative absence");
        Equal(GarageNativeGrantDecision.AlreadyInserted, absent.GrantDecision,
            "mutation calculating grant before NoteInserted can regrant after authoritative absence");
        False(absent.GrantAttempted,
            "mutation submitting true after authoritative absence resurrects the consumed cartridge");
        Equal(0, submittedGrantValues.Count,
            "authoritative pending confirmation reaches NoteInserted before the native grant boundary");
        Equal(GarageInsertionServerValue.Inserted, fixture.Coordinator.GetServerValue(superstar),
            "only authoritative readable absence invokes the real insertion coordinator");
        Equal(1, fixture.Store.WriteKeys.Count,
            "authoritative readable absence queues exactly one real slot-scoped write");
    }

    {
        var fixture = await CreateProductionFixture();
        var submittedGrantValues = new List<bool>();
        GarageCartridgeReconciliationResult missing = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true, held: false, inGarage: false, releasedThisVisit: false);
        Equal(GarageNativeGrantDecision.ApplyBagItem, missing.GrantDecision,
            "the production adapter retains the ordinary missing-bag grant path");
        True(missing.GrantAttempted && missing.GrantSubmitted,
            "mutation moving native submission outside the production adapter leaves the real path untested");
        Equal("True", string.Join(",", submittedGrantValues),
            "the production adapter submits only the native true bag grant");
        Equal(GarageInsertionServerValue.NotInserted, fixture.Coordinator.GetServerValue(superstar),
            "ordinary grant submission never marks insertion");
        Equal(0, fixture.Store.WriteKeys.Count,
            "ordinary grant submission never queues insertion storage");
    }

    {
        var fixture = await CreateProductionFixture();
        var submittedGrantValues = new List<bool>();
        _ = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true, held: true, inGarage: true, releasedThisVisit: false);
        True(RecordEvent(fixture, superstarBagFlag, flagIsSet: false, flagWasSet: true),
            "mutation omitting the real event-to-adapter path loses pending confirmation");
        Equal(GarageInsertionServerValue.NotInserted, fixture.Coordinator.GetServerValue(superstar),
            "mutation calling NoteInserted from the event alone bypasses authoritative readback");
        Equal(0, fixture.Store.WriteKeys.Count,
            "mutation writing insertion from the event alone bypasses authoritative readback");

        GarageCartridgeReconciliationResult heldAgain = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true, held: true, inGarage: true, releasedThisVisit: true);
        False(heldAgain.RecordedInsertion,
            "mutation confirming pending consumption from authoritative held state invents insertion");
        Equal(GarageNativeGrantDecision.AlreadyHeld, heldAgain.GrantDecision,
            "authoritative held state cancels pending without regrant");
        False(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "mutation retaining pending after held readback can confirm unrelated later absence");
        Equal(0, submittedGrantValues.Count,
            "held cancellation does not touch the native grant callback");
        Equal(0, fixture.Store.WriteKeys.Count,
            "held cancellation does not invoke NoteInserted");
    }

    {
        var fixture = await CreateProductionFixture();
        var submittedGrantValues = new List<bool>();
        _ = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true, held: true, inGarage: true, releasedThisVisit: false);
        True(RecordRequest(fixture, superstarBagFlag, value: false),
            "destructive-reset fixture arms pending through the real request adapter");
        long previousResetEpoch = fixture.Adapter.ResetEpoch;
        fixture.Adapter.Reset();
        Equal(previousResetEpoch + 1, fixture.Adapter.ResetEpoch,
            "destructive reset advances the production adapter epoch");
        False(fixture.Adapter.HasPendingConsumption(superstar, 1),
            "mutation retaining pending through a destructive reset can invent insertion");
        Equal(GarageInsertionServerValue.NotInserted, fixture.Coordinator.GetServerValue(superstar),
            "destructive reset never converts stale pending evidence into insertion");
        Equal(0, fixture.Store.WriteKeys.Count,
            "destructive reset never queues an insertion write");
    }

    {
        var fixture = await CreateProductionFixture();
        var submittedGrantValues = new List<bool>();
        _ = RunProductionReconciliation(
            fixture, submittedGrantValues, superstar, false,
            readable: true, held: true, inGarage: true, releasedThisVisit: false);
        False(RecordRequest(fixture, superstarBagFlag, value: false, inGarage: false),
            "mutation accepting a false request outside Garage captures unrelated state");
        False(RecordRequest(fixture, superstarBagFlag, value: false, releasedThisVisit: false),
            "mutation arming without release evidence captures unrelated bag loss");
        False(RecordRequest(fixture, superstarBagFlag, value: false, apOwned: false),
            "mutation arming an unowned cartridge bypasses AP ownership");
        False(RecordRequest(
                fixture,
                superstarBagFlag,
                value: false,
                serverValue: GarageInsertionServerValue.Unknown),
            "mutation arming under unknown server state bypasses synchronization");
        False(RecordEvent(fixture, superstarBagFlag, flagIsSet: false, flagWasSet: false),
            "mutation accepting a false-to-false update invents consumption");
        False(RecordRequest(
                fixture,
                "LEVEL_27_CARTRIDGE_STAR_EATER_COLLECTED",
                value: false),
            "mutation accepting a collected marker as bag consumption corrupts state");

        False(RecordRequest(
                fixture,
                "LEVEL_27_CARTRIDGE_VAMPIREKILLER_BAG_ITEM",
                value: false),
            "mutation allowing physical Vampire Killer into pending AP insertion breaks vanilla entry");
    }

    True(progressionRequestPrefixMethod.Contains(
            "GarageCartridgeAccess.RecordNativeBagProgressionRequest(req, flag);",
            StringComparison.Ordinal),
        "supplemental wiring guard keeps the request hook attached to GarageCartridgeAccess");
    True(progressionFlagEventMethod.Contains(
            "GarageCartridgeAccess.RecordNativeBagProgressionFlagUpdated(evt, flag);",
            StringComparison.Ordinal),
        "supplemental wiring guard keeps the event hook attached to GarageCartridgeAccess");
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
            coordinator.GetServerValue("Bloody Tears"),
            true),
        "keeper release policy rejects the coordinator's initial unknown state");
    store.CompleteNextRead(cartridgeKeys["Bloody Tears"], GarageInsertionReadResult.KnownTrue);
    await Eventually(
        () => coordinator.GetServerValue("Bloody Tears") == GarageInsertionServerValue.Inserted,
        "coordinator transitions unknown insertion state to inserted");
    True(GarageCartridgeInsertionPolicy.ShouldReleaseObject(
            false,
            coordinator.GetServerValue("Bloody Tears"),
            true),
        "keeper release policy opens for song availability after inserted state becomes authoritative");
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

sealed class FakeArchipelagoGarageInsertionStorageTransport : IArchipelagoGarageInsertionStorageTransport
{
    public Func<string, JToken, CancellationToken, Task<JToken>> ReadBehavior { get; set; } =
        (_, _, _) => Task.FromResult<JToken>(JToken.FromObject(false));
    public Func<string, CancellationToken, Task<GarageInsertionWriteConfirmation>> WriteBehavior { get; set; } =
        (_, _) => Task.FromResult(new GarageInsertionWriteConfirmation(true, JToken.FromObject(true)));
    public List<JToken> ReadInitialValues { get; } = new();

    public Task<JToken> ReadAsync(string key, JToken initialValue, CancellationToken cancellationToken)
    {
        ReadInitialValues.Add(initialValue.DeepClone());
        return ReadBehavior(key, initialValue, cancellationToken);
    }

    public Task<GarageInsertionWriteConfirmation> WriteTrueAndReadBackAsync(
        string key,
        CancellationToken cancellationToken) =>
        WriteBehavior(key, cancellationToken);
}

sealed class GarageInsertionSequenceHarness
{
    private readonly GarageCartridgeReconciliationAdapter _adapter = new();

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
    public long Generation { get; } = 1;
    public long ResetEpoch => _adapter.ResetEpoch;
    public bool HasPendingConsumption =>
        _adapter.HasPendingConsumption(Song, Generation);

    public void NoteGarageObjectReleased() => ReleasedThisVisit = true;

    public bool Observe(bool readable, bool held, bool inGarage)
    {
        GarageCartridgeReconciliationResult result = _adapter.Reconcile(
            Song,
            compatible: true,
            ApOwned,
            UsesPhysicalVanillaEntrance,
            Coordinator.GetServerValue(Song),
            readable,
            held,
            inGarage,
            ReleasedThisVisit,
            Generation,
            Coordinator.NoteInserted,
            submitGrant: null);
        return result.RecordedInsertion;
    }

    public bool SignalProgressionRequest(string flag, bool? value, bool inGarage) =>
        SignalProgression(flag, value, signalWasSet: null, requirePreviouslySet: false, inGarage);

    public bool SignalProgressionEvent(
        string flag,
        bool? flagIsSet,
        bool? flagWasSet,
        bool inGarage) =>
        SignalProgression(flag, flagIsSet, flagWasSet, requirePreviouslySet: true, inGarage);

    public (bool RecordedInsertion, GarageNativeGrantDecision GrantDecision) Reconcile(
        bool readable,
        bool held,
        bool inGarage)
    {
        GarageCartridgeReconciliationResult result = _adapter.Reconcile(
            Song,
            compatible: true,
            ApOwned,
            UsesPhysicalVanillaEntrance,
            Coordinator.GetServerValue(Song),
            readable,
            held,
            inGarage,
            ReleasedThisVisit,
            Generation,
            Coordinator.NoteInserted,
            submitGrant: null);
        return (result.RecordedInsertion, result.GrantDecision);
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
        _adapter.Reset();
        ReleasedThisVisit = false;
    }

    public void RoomTransitionReset(long? generation = null)
    {
        _adapter.ResetForRoomTransition(generation ?? Generation);
        ReleasedThisVisit = false;
    }

    private bool SignalProgression(
        string flag,
        bool? signalIsSet,
        bool? signalWasSet,
        bool requirePreviouslySet,
        bool inGarage)
    {
        return requirePreviouslySet
            ? _adapter.RecordProgressionFlagUpdated(
                flag,
                signalIsSet,
                signalWasSet,
                compatible: true,
                ApOwned,
                Coordinator.GetServerValue(Song),
                inGarage,
                ReleasedThisVisit,
                Generation)
            : _adapter.RecordProgressionRequest(
                flag,
                signalIsSet,
                compatible: true,
                ApOwned,
                Coordinator.GetServerValue(Song),
                inGarage,
                ReleasedThisVisit,
                Generation);
    }
}
