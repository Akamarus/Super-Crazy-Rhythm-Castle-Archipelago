using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

static RootsIntroSnapshot Snapshot(
    bool enabled = true,
    bool compatible = true,
    bool enteringRoots = true,
    bool nativeFlagOwned = false,
    bool processorAvailable = true,
    bool submissionOutstanding = false,
    int retryCount = 0) =>
    new(enabled, compatible, enteringRoots, nativeFlagOwned,
        processorAvailable, submissionOutstanding, retryCount);

Equal(RootsIntroDecision.Vanilla, RootsPresentationPolicy.DecideIntro(Snapshot(enabled: false)), "disabled AP preserves vanilla");
Equal(RootsIntroDecision.Vanilla, RootsPresentationPolicy.DecideIntro(Snapshot(compatible: false)), "incompatible slot preserves vanilla");
Equal(RootsIntroDecision.Vanilla, RootsPresentationPolicy.DecideIntro(Snapshot(enteringRoots: false)), "unrelated transition preserves vanilla");
Equal(RootsIntroDecision.QueueFlag, RootsPresentationPolicy.DecideIntro(Snapshot(processorAvailable: false)), "missing processor queues flag work");
Equal(RootsIntroDecision.Wait, RootsPresentationPolicy.DecideIntro(Snapshot(submissionOutstanding: true)), "unverified submission defers transition");
Equal(RootsIntroDecision.AllowTransition, RootsPresentationPolicy.DecideIntro(Snapshot(nativeFlagOwned: true)), "verified native flag permits transition");
Equal(RootsIntroDecision.FallbackVanilla, RootsPresentationPolicy.DecideIntro(Snapshot(processorAvailable: false, retryCount: 4)), "retry exhaustion restores vanilla transition");

Equal(TimeSpan.Zero, RootsPresentationPolicy.RetryDelay(0), "first retry is immediate");
Equal(TimeSpan.FromMilliseconds(50), RootsPresentationPolicy.RetryDelay(1), "second retry delay");
Equal(TimeSpan.FromMilliseconds(100), RootsPresentationPolicy.RetryDelay(2), "third retry delay");
Equal(TimeSpan.FromMilliseconds(200), RootsPresentationPolicy.RetryDelay(3), "fourth retry delay");
Equal<TimeSpan?>(null, RootsPresentationPolicy.RetryDelay(4), "retry schedule is bounded");

Equal(true, RootsPresentationPolicy.ShouldSuppressPostLevelOne(true, true, "AssignAndCommentOnMusicDifficultySequenceStep", true), "exact post-Level-1 difficulty sequence is suppressed");
Equal(false, RootsPresentationPolicy.ShouldSuppressPostLevelOne(true, true, "AssignAndCommentOnMusicDifficultySequenceStep", false), "sequence before persisted Level 1 is preserved");
Equal(false, RootsPresentationPolicy.ShouldSuppressPostLevelOne(true, true, "UnrelatedSequenceStep", true), "unrelated sequence is preserved");
Equal(false, RootsPresentationPolicy.ShouldSuppressPostLevelOne(true, false, "AssignAndCommentOnMusicDifficultySequenceStep", true), "incompatible slot preserves sequence");

Equal(AreaArrivalKind.RootsPhone,
    AreaArrivalPresentationPolicy.DecideTransition(true, true, "GameRoom_Hub6", "GameRoom_Hub2", true, false),
    "AP phone arrival arms Roots presentation overrides");
Equal(AreaArrivalKind.LobbyPhone,
    AreaArrivalPresentationPolicy.DecideTransition(true, true, "GameRoom_Hub6", "GameRoom_Hub1A", false, true),
    "AP phone arrival arms Lobby presentation override");
Equal(AreaArrivalKind.None,
    AreaArrivalPresentationPolicy.DecideTransition(true, true, "GameRoom_Hub2", "GameRoom_Hub1A", true, true),
    "vanilla Lift Quest arrival is preserved");
Equal(AreaArrivalKind.None,
    AreaArrivalPresentationPolicy.DecideTransition(true, true, "GameRoom_05", "GameRoom_Hub2", true, true),
    "returning from a level does not arm first-arrival overrides");

foreach (string flag in new[]
         {
             "ROOTS_HUB_INTRO_WITNESSED",
             "ROOTS_HUB_GATE_OPENED",
             "ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE",
         })
{
    Equal(true,
        AreaArrivalPresentationPolicy.ShouldBypassCondition(AreaArrivalKind.RootsPhone, "GameRoom_Hub2", flag),
        $"Roots AP-phone arrival satisfies {flag} during scene initialization");
}
Equal(true,
    AreaArrivalPresentationPolicy.ShouldBypassCondition(AreaArrivalKind.LobbyPhone, "GameRoom_Hub1A", "OVERALL_PROGRESS_REACHED_LOBBY_HUB"),
    "Lobby AP-phone arrival suppresses only its first-arrival presentation");
Equal(false,
    AreaArrivalPresentationPolicy.ShouldBypassCondition(AreaArrivalKind.LobbyPhone, "GameRoom_Hub1A", "LOBBY_HUB_HEIST_KING_WITNESSED"),
    "unrelated Lobby story conditions remain native");
Equal(false,
    AreaArrivalPresentationPolicy.ShouldBypassCondition(AreaArrivalKind.RootsPhone, "GameRoom_Hub6", "ROOTS_HUB_INTRO_WITNESSED"),
    "override is scoped to the destination scene");

string pluginSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));
Equal(true,
    pluginSource.Contains("RootsIntroCutsceneBypass.CapturePlayerSaveRequestProcessor(__instance)", StringComparison.Ordinal),
    "pre-entry bootstrap captures the native save processor");
Equal(true,
    pluginSource.Contains("RootsIntroCutsceneBypass.CapturePlayerSaveRequestProcessor(processor)", StringComparison.Ordinal),
    "pre-entry bootstrap shares the proven stateless processor when ordinary progression has not supplied one");
Equal(true,
    pluginSource.Contains("RootsIntroCutsceneBypass.ShouldAllowTransition(", StringComparison.Ordinal),
    "transition boundary waits for pre-entry bootstrap verification");
Equal(true,
    pluginSource.Contains("RootsIntroCutsceneBypass.TickPending();", StringComparison.Ordinal),
    "Unity update loop advances a held transition");
Equal(true,
    pluginSource.Contains("NativeGateOpenedFlag = \"ROOTS_HUB_GATE_OPENED\"", StringComparison.Ordinal) &&
    pluginSource.Contains("NativeDifficultyCompleteFlag = \"ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE\"", StringComparison.Ordinal),
    "bootstrap contains only the discovered Roots presentation bookkeeping flags");
Equal(true,
    RootsOwnerDiagnosticPolicy.ShouldInspect("GameRoom_Hub2", RootsOwnerDiagnosticPolicy.OwnerPath),
    "diagnostic targets only the exact discovered owner in Roots");
Equal(false,
    RootsOwnerDiagnosticPolicy.ShouldInspect("GameRoom_Hub2", "Root/OtherOwner"),
    "diagnostic excludes unrelated progression owners");
Equal(false, RootsOwnerDiagnosticPolicy.RequestsMutation,
    "owner diagnostic is read-only");
Equal(true, RootsComputerPolicy.ShouldNormalize(true, true, true, "GameRoom_Hub2", RootsComputerPolicy.ControlPath),
    "exact AP Roots computer state is normalized");
Equal(false, RootsComputerPolicy.ShouldNormalize(true, true, true, "GameRoom_Hub2", "Root/OtherControl"),
    "unrelated controls are untouched");
Equal(false, RootsComputerPolicy.ShouldNormalize(true, true, false, "GameRoom_Hub2", RootsComputerPolicy.ControlPath),
    "locked Roots does not receive normalization");
Equal(true, RootsComputerPolicy.ShouldHideCover(true, true, true, "GameRoom_Hub2"),
    "Sophisticated Computer cover is hidden for owned AP Roots");
Equal(true, RootsComputerPolicy.ShouldNormalizeState(true, true, true, "GameRoom_Hub2"),
    "difficulty toggler state reports idle only in owned AP Roots");

const string meatFirstAreaGatePath =
    "Root/GameRoom_Hub4_Logic/Objects/Doors/FirstAreaGate";
Equal(true,
    AreaAccessSessionPolicy.IsAuthenticatedCompatible(
        true, "area-routing-plant-pipes-0.15"),
    "authenticated Area Access slot data enables area presentation");
Equal(false,
    AreaAccessSessionPolicy.IsAuthenticatedCompatible(
        false, "area-routing-plant-pipes-0.15"),
    "disabled Area Access rejects otherwise compatible slot data");
Equal(false,
    AreaAccessSessionPolicy.IsAuthenticatedCompatible(true, null),
    "missing slot implementation is not Area Access compatible");
Equal(false,
    AreaAccessSessionPolicy.IsAuthenticatedCompatible(true, "music-lab-points-0.23"),
    "generic compatible session features cannot authorize Area Access");
Equal(true,
    MeatAreaPresentationPolicy.ShouldSuppressFirstAreaGate(
        true, true, true, "GameRoom_Hub4", meatFirstAreaGatePath),
    "owned Meat Dimension AP route suppresses the exact first gate");
Equal(false,
    MeatAreaPresentationPolicy.ShouldSuppressFirstAreaGate(
        false, true, true, "GameRoom_Hub4", meatFirstAreaGatePath),
    "disabled Area Access preserves the Meat gate");
Equal(false,
    MeatAreaPresentationPolicy.ShouldSuppressFirstAreaGate(
        true, false, true, "GameRoom_Hub4", meatFirstAreaGatePath),
    "an unauthenticated slot preserves the Meat gate");
Equal(false,
    MeatAreaPresentationPolicy.ShouldSuppressFirstAreaGate(
        true, true, false, "GameRoom_Hub4", meatFirstAreaGatePath),
    "locked Meat Dimension preserves the gate");
Equal(false,
    MeatAreaPresentationPolicy.ShouldSuppressFirstAreaGate(
        true, true, true, "GameRoom_Hub40", meatFirstAreaGatePath),
    "neighboring room identifiers do not suppress the Meat gate");
Equal(false,
    MeatAreaPresentationPolicy.ShouldSuppressFirstAreaGate(
        true, true, true, "GameRoom_Hub4", meatFirstAreaGatePath + "/Collision"),
    "only the exact Meat first gate root is suppressed");

Equal(AreaAccessDestinationDecision.RedirectToMusicLab,
    AreaAccessDestinationPolicy.Decide(true, true, "GameRoom_Hub4", true, false),
    "an unowned major-area destination is redirected to Music Lab");
Equal(AreaAccessDestinationDecision.RedirectToMusicLab,
    AreaAccessDestinationPolicy.Decide(true, true, "GameRoom_Hub2", true, false),
    "destination guarding is independent of the transition origin");
Equal(AreaAccessDestinationDecision.Preserve,
    AreaAccessDestinationPolicy.Decide(false, false, "GameRoom_Hub4", true, false),
    "disabled Area Access preserves native transitions");
Equal(AreaAccessDestinationDecision.RedirectToMusicLab,
    AreaAccessDestinationPolicy.Decide(true, false, "GameRoom_Hub4", true, false),
    "pending or incompatible Area Access redirects an unowned destination");
Equal(AreaAccessDestinationDecision.RedirectToMusicLab,
    AreaAccessDestinationPolicy.Decide(true, false, "GameRoom_Hub4", true, true),
    "stale ownership cannot preserve a destination after authentication ends");
Equal(AreaAccessDestinationDecision.Preserve,
    AreaAccessDestinationPolicy.Decide(true, true, "GameRoom_Hub4", true, true),
    "owned major-area destinations are preserved");
Equal(AreaAccessDestinationDecision.Preserve,
    AreaAccessDestinationPolicy.Decide(true, false, "GameRoom_27", false, false),
    "non-area destinations are preserved");
Equal(AreaAccessDestinationDecision.Preserve,
    AreaAccessDestinationPolicy.Decide(true, false, AreaAccessDestinationPolicy.MusicLabRoomId, false, false),
    "return travel to Music Lab is never redirected");

static MeatMouseEscortRecoverySnapshot MouseRecovery(
    bool authenticatedCompatible = true,
    string roomId = "GameRoom_Hub4",
    bool requirementReadable = true,
    bool requirementStated = true,
    bool revolutionReadable = true,
    bool revolutionTriggered = false,
    bool nativeSpawnerIdentityKnown = true,
    bool leaderReadable = true,
    bool leaderPresent = false,
    bool attemptedThisRoom = false) =>
    new(authenticatedCompatible, roomId, requirementReadable, requirementStated,
        revolutionReadable, revolutionTriggered, nativeSpawnerIdentityKnown,
        leaderReadable, leaderPresent, attemptedThisRoom);

Equal(MeatMouseEscortRecoveryDecision.ReevaluateNativeSpawner,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery()),
    "interrupted Act 4 escort re-evaluates the exact native mouse spawner");
Equal(MeatMouseEscortRecoveryDecision.RetryPending,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(authenticatedCompatible: false)),
    "pending authenticated Area Access session retries later");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(roomId: "GameRoom_Hub40")),
    "mouse recovery is exact-room scoped");
Equal(MeatMouseEscortRecoveryDecision.RetryPending,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(requirementReadable: false)),
    "unreadable Act 4 requirement retries later");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(requirementStated: false)),
    "unstated Act 4 requirement preserves native state");
Equal(MeatMouseEscortRecoveryDecision.RetryPending,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(revolutionReadable: false)),
    "unreadable revolution state retries later");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(revolutionTriggered: true)),
    "completed revolution is never repaired");
Equal(MeatMouseEscortRecoveryDecision.RetryPending,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(nativeSpawnerIdentityKnown: false)),
    "pending native spawner identity retries later");
Equal(MeatMouseEscortRecoveryDecision.RetryPending,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(leaderReadable: false)),
    "unreadable native Character state retries later");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(leaderPresent: true)),
    "present mouse leader is never duplicated");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(attemptedThisRoom: true)),
    "mouse recovery is bounded to one attempt per room lifetime");

var pendingRecovery = new MeatMouseEscortRecoveryRuntime();
Equal(true, pendingRecovery.ShouldEvaluateFrame(),
    "new room recovery evaluates after its initial scene delay");
Equal(MeatMouseEscortRecoveryObservation.Pending,
    pendingRecovery.Observe(MouseRecovery(authenticatedCompatible: false)),
    "pending authentication schedules a bounded later evaluation");
Equal(false, pendingRecovery.AttemptConsumed,
    "pending authentication does not consume the native recovery attempt");
Equal(false, pendingRecovery.ShouldEvaluateFrame(),
    "pending recovery is rate-limited instead of hot-looped");
int pendingWaitFrames = 0;
while (!pendingRecovery.ShouldEvaluateFrame() && pendingWaitFrames < 1000)
{
    pendingRecovery.AdvanceFrame();
    pendingWaitFrames++;
}
Equal(15, pendingWaitFrames,
    "first pending recovery waits the bounded literal frame delay");
Equal(MeatMouseEscortRecoveryObservation.Ready,
    pendingRecovery.Observe(MouseRecovery()),
    "a later fully readable frame becomes eligible for native recovery");
Equal(false, pendingRecovery.AttemptConsumed,
    "eligibility alone does not consume the attempt before the native call boundary");
Equal(true, pendingRecovery.TryConsumeAttempt(),
    "the fully proven native call consumes the one room-lifetime attempt");
Equal(true, pendingRecovery.AttemptConsumed,
    "actual native execution records the consumed attempt");
Equal(MeatMouseEscortRecoveryObservation.None,
    pendingRecovery.Observe(MouseRecovery()),
    "an actual execution prevents repeat recovery in the same room");
Equal(false, pendingRecovery.TryConsumeAttempt(),
    "the native attempt cannot be consumed twice");

var unreadableRecovery = new MeatMouseEscortRecoveryRuntime();
Equal(MeatMouseEscortRecoveryObservation.Pending,
    unreadableRecovery.Observe(MouseRecovery(leaderReadable: false)),
    "an unreadable native Character frame schedules reevaluation");
Equal(false, unreadableRecovery.AttemptConsumed,
    "an unreadable native Character frame does not consume the attempt");
for (int frame = 0; frame < 15; frame++)
    unreadableRecovery.AdvanceFrame();
Equal(MeatMouseEscortRecoveryObservation.Ready,
    unreadableRecovery.Observe(MouseRecovery()),
    "a later readable native Character frame can execute recovery");
Equal(true, unreadableRecovery.TryConsumeAttempt(),
    "later valid state consumes the attempt only at the native call boundary");

var exhaustedRecovery = new MeatMouseEscortRecoveryRuntime();
foreach (int expectedDelay in new[] { 15, 30, 60, 120, 240, 480 })
{
    Equal(MeatMouseEscortRecoveryObservation.Pending,
        exhaustedRecovery.Observe(MouseRecovery(requirementReadable: false)),
        $"unreadable prerequisite schedules bounded retry delay {expectedDelay}");
    int elapsed = 0;
    while (!exhaustedRecovery.ShouldEvaluateFrame() && elapsed <= expectedDelay)
    {
        exhaustedRecovery.AdvanceFrame();
        elapsed++;
    }
    Equal(expectedDelay, elapsed,
        $"pending retry uses literal delay {expectedDelay}");
}
Equal(MeatMouseEscortRecoveryObservation.Failed,
    exhaustedRecovery.Observe(MouseRecovery(requirementReadable: false)),
    "unreadable prerequisites stop after the bounded retry schedule");
Equal(false, exhaustedRecovery.AttemptConsumed,
    "retry exhaustion never consumes the native recovery attempt");
Equal(true, exhaustedRecovery.Finished,
    "retry exhaustion prevents an endless hot-loop");

Equal(true,
    MeatMouseEscortBindingDiagnosticPolicy.ShouldInspectPath(
        "Root/GameRoom_Hub4_Logic/NPCs/SpecialAnimals/SpawnMouseLeader"),
    "binding diagnostic includes the exact native spawner container");
Equal(true,
    MeatMouseEscortBindingDiagnosticPolicy.ShouldInspectPath(
        "Root/GameRoom_Hub4_Logic/NPCs/SpecialAnimals/SpawnMouseLeader/Guide"),
    "binding diagnostic includes an exact direct child");
Equal(false,
    MeatMouseEscortBindingDiagnosticPolicy.ShouldInspectPath(
        "Root/GameRoom_Hub4_Logic/NPCs/SpecialAnimals/SpawnMouseLeader/Guide/Nested"),
    "binding diagnostic excludes grandchildren");
Equal(false,
    MeatMouseEscortBindingDiagnosticPolicy.ShouldInspectPath(
        "Root/GameRoom_Hub4_Logic/NPCs/SpecialAnimals/SpawnMouseLeaderOther"),
    "binding diagnostic excludes prefix-collision objects");
Equal(false,
    MeatMouseEscortBindingDiagnosticPolicy.ShouldInspectPath(
        "Root/GameRoom_Hub40_Logic/NPCs/SpecialAnimals/SpawnMouseLeader"),
    "binding diagnostic excludes other rooms");
Equal(false,
    MeatMouseEscortBindingDiagnosticPolicy.ShouldEmit(
        "GameRoom_Hub4", false, false),
    "a missing exact container does not consume the room diagnostic");
Equal(true,
    MeatMouseEscortBindingDiagnosticPolicy.ShouldEmit(
        "GameRoom_Hub4", false, true),
    "the later available exact container emits in the same room lifetime");
Equal(false,
    MeatMouseEscortBindingDiagnosticPolicy.ShouldEmit(
        "GameRoom_Hub4", true, true),
    "binding diagnostic cannot emit twice after a successful emission");
Equal(false,
    MeatMouseEscortBindingDiagnosticPolicy.ShouldEmit(
        "GameRoom_Hub40", false, true),
    "binding diagnostic cannot emit outside exact Hub4");
Equal(false, MeatMouseEscortBindingDiagnosticPolicy.RequestsMutation,
    "binding diagnostic is read-only by contract");

Type? conditionDiagnosticType = typeof(MeatMouseEscortRecoveryPolicy).Assembly.GetType(
    "RhythmCastleAP.MeatMouseEscortConditionDiagnosticPolicy",
    throwOnError: false, ignoreCase: false);
Equal(true, conditionDiagnosticType != null,
    "exact mouse condition diagnostic policy exists");
System.Reflection.MethodInfo? conditionScope = conditionDiagnosticType!.GetMethod(
    "ShouldInspectPath", System.Reflection.BindingFlags.Static |
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
Equal(true, conditionScope != null,
    "exact mouse condition diagnostic exposes a path scope gate");
bool InspectCondition(string path) => (bool)conditionScope!.Invoke(null, new object[] { path })!;
Equal(true, InspectCondition(
    "Root/GameRoom_Hub4_Logic/NPCs/SpecialAnimals/SpawnMouseLeader/SpawnCondition"),
    "condition diagnostic includes only exact SpawnCondition");
Equal(true, InspectCondition(
    "Root/GameRoom_Hub4_Logic/NPCs/SpecialAnimals/SpawnMouseLeader/SpawnCondition/RevolutionNotTriggeredCondition"),
    "condition diagnostic includes its exact known revolution child");
Equal(false, InspectCondition(
    "Root/GameRoom_Hub4_Logic/NPCs/SpecialAnimals/SpawnMouseLeader/SpawnCondition/OtherCondition"),
    "condition diagnostic rejects unapproved siblings");
Equal(false, InspectCondition(
    "Root/GameRoom_Hub4_Logic/NPCs/SpecialAnimals/SpawnMouseLeader/SpawnCondition/RevolutionNotTriggeredCondition/Nested"),
    "condition diagnostic rejects deeper descendants");
Equal(false, InspectCondition(
    "Root/GameRoom_Hub40_Logic/NPCs/SpecialAnimals/SpawnMouseLeader/SpawnCondition"),
    "condition diagnostic rejects other rooms");
System.Reflection.MethodInfo? conditionEmission = conditionDiagnosticType.GetMethod(
    "ShouldEmit", System.Reflection.BindingFlags.Static |
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
Equal(true, conditionEmission != null,
    "exact condition diagnostic exposes a late-loaded emission gate");
bool EmitCondition(string room, bool emitted, bool exactSubtreeAvailable) =>
    (bool)conditionEmission!.Invoke(null, new object[] { room, emitted, exactSubtreeAvailable })!;
Equal(false, EmitCondition("GameRoom_Hub4", false, false),
    "missing exact condition child cannot consume the once-per-room emission");
Equal(true, EmitCondition("GameRoom_Hub4", false, true),
    "later available condition subtree emits once in Hub4");
Equal(false, EmitCondition("GameRoom_Hub4", true, true),
    "condition diagnostic does not repeat after emission");
Equal(false, EmitCondition("GameRoom_Hub40", false, true),
    "condition diagnostic never emits in another room");
var lateCondition = new MeatMouseEscortConditionDiagnosticRuntime();
Equal(true, lateCondition.ShouldEvaluateFrame(),
    "condition diagnostic polls once after the room initialization delay");
Equal(false, lateCondition.Observe("GameRoom_Hub4", false),
    "missing exact condition subtree remains pending without emission");
Equal(false, lateCondition.Emitted,
    "missing subtree does not consume the diagnostic");
int conditionWait = 0;
while (!lateCondition.ShouldEvaluateFrame() && conditionWait <= 15)
{
    lateCondition.AdvanceFrame();
    conditionWait++;
}
Equal(15, conditionWait, "late condition diagnostic uses a bounded 15-frame retry");
Equal(true, lateCondition.Observe("GameRoom_Hub4", true),
    "later exact subtree emits in the same room lifetime");
Equal(true, lateCondition.Emitted, "emission is consumed only after full availability");
Equal(false, lateCondition.ShouldEvaluateFrame(),
    "successful condition diagnostic never repeats");
lateCondition.Reset();
Equal(true, lateCondition.ShouldEvaluateFrame(),
    "room exit resets the condition diagnostic lifetime");
var exhaustedCondition = new MeatMouseEscortConditionDiagnosticRuntime();
foreach (int delay in new[] { 15, 30, 60, 120, 240, 480 })
{
    Equal(false, exhaustedCondition.Observe("GameRoom_Hub4", false),
        $"missing exact subtree schedules a bounded {delay}-frame retry");
    int elapsed = 0;
    while (!exhaustedCondition.ShouldEvaluateFrame() && elapsed <= delay)
    {
        exhaustedCondition.AdvanceFrame();
        elapsed++;
    }
    Equal(delay, elapsed, $"condition retry delay is exactly {delay} frames");
}
Equal(false, exhaustedCondition.Observe("GameRoom_Hub4", false),
    "last unavailable poll does not emit");
Equal(true, exhaustedCondition.Finished,
    "condition diagnostic stops after seven bounded polls");
Equal(false, exhaustedCondition.Emitted,
    "exhaustion does not claim a diagnostic was emitted");

Type? conditionReaderType = typeof(MeatMouseEscortRecoveryPolicy).Assembly.GetType(
    "RhythmCastleAP.MeatMouseEscortConditionReader", false, false);
Equal(true, conditionReaderType != null,
    "exact native condition reader exists");
System.Reflection.MethodInfo? conditionRead = conditionReaderType!.GetMethod(
    "Read", System.Reflection.BindingFlags.Static |
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
Equal(true, conditionRead != null,
    "exact condition reader exposes its read-only entry point");
object ReadCondition(object condition) => conditionRead!.Invoke(null, new[] { condition })!;
static (bool Readable, bool Met, string Stage) ConditionState(object state)
{
    Type type = state.GetType();
    return ((bool)type.GetProperty("Readable")!.GetValue(state)!,
        (bool)type.GetProperty("Met")!.GetValue(state)!,
        (string)type.GetProperty("Stage")!.GetValue(state)!);
}
var trueCondition = new ExactMouseGeneralCondition(true);
var trueConditionState = ConditionState(ReadCondition(trueCondition));
Equal(true, trueConditionState.Readable, "native GeneralCondition bool is readable");
Equal(true, trueConditionState.Met, "native true CheckIfMet remains true");
Equal(1, trueCondition.Calls, "exact CheckIfMet is evaluated once");
var falseCondition = new ExactMouseGeneralCondition(false);
var falseConditionState = ConditionState(ReadCondition(falseCondition));
Equal(true, falseConditionState.Readable, "native false condition is readable");
Equal(false, falseConditionState.Met, "native false CheckIfMet remains false");
Equal(1, falseCondition.Calls, "native false condition is evaluated once");
Equal(false, ConditionState(ReadCondition(new WrongMouseCondition())).Readable,
    "unrelated bool method is never invoked");
Equal(false, ConditionState(ReadCondition(new WrongReturnMouseGeneralCondition())).Readable,
    "a hidden non-bool CheckIfMet cannot fall back to an inherited bool method");
Equal(false, ConditionState(ReadCondition(new ThrowingMouseGeneralCondition())).Readable,
    "native condition evaluation failure is unreadable, not false");

var interopEmptyMouse = MeatMouseEscortCharacterReader.Read(
    new SpawnMeatAnimalCharacterOnDemand(TestMouseCharacterMode.InteropEmpty));
Equal(true, interopEmptyMouse.Readable,
    "exact IL2CPP empty-nullable exception is readable native absence");
Equal(false, interopEmptyMouse.Present,
    "exact IL2CPP empty nullable proves the mouse leader is missing");
Equal("empty-il2cpp-nullable", interopEmptyMouse.Stage,
    "exact IL2CPP empty nullable reports its distinct read stage");

var explicitEmptyMouse = MeatMouseEscortCharacterReader.Read(
    new SpawnMeatAnimalCharacterOnDemand(TestMouseCharacterMode.ExplicitEmpty));
Equal(true, explicitEmptyMouse.Readable,
    "ordinary false HasValue remains readable");
Equal(false, explicitEmptyMouse.Present,
    "ordinary false HasValue proves the mouse leader is missing");

var presentMouse = MeatMouseEscortCharacterReader.Read(
    new SpawnMeatAnimalCharacterOnDemand(TestMouseCharacterMode.Present));
Equal(true, presentMouse.Readable,
    "present native Character is readable");
Equal(true, presentMouse.Present,
    "present native Character prevents recovery duplication");

var unrelatedMouseFailure = MeatMouseEscortCharacterReader.Read(
    new SpawnMeatAnimalCharacterOnDemand(TestMouseCharacterMode.UnrelatedFailure));
Equal(false, unrelatedMouseFailure.Readable,
    "unrelated Character getter null-reference fails closed");

var wrongMouseSpawner = MeatMouseEscortCharacterReader.Read(new WrongMouseSpawnerType());
Equal(false, wrongMouseSpawner.Readable,
    "Character reader rejects a non-exact native spawner type");

int meatKeeperStart = pluginSource.IndexOf(
    "internal sealed class MeatAreaBaselineKeeper", StringComparison.Ordinal);
int areaAccessStart = pluginSource.IndexOf(
    "internal static class AreaAccessPrototype", meatKeeperStart, StringComparison.Ordinal);
Equal(true, meatKeeperStart >= 0 && areaAccessStart > meatKeeperStart,
    "Meat Area baseline keeper has a bounded production source region");
string meatKeeperSource = pluginSource[meatKeeperStart..areaAccessStart];
Equal(true,
    meatKeeperSource.Contains(
        "AreaAccessPrototype.AuthenticatedCompatibleSession", StringComparison.Ordinal),
    "Meat gate suppression uses explicit authenticated Area Access compatibility");
Equal(false,
    meatKeeperSource.Contains("Plugin.AP?.Connected", StringComparison.Ordinal),
    "generic connection and Music Lab compatibility cannot authorize the Meat gate");
Equal(false,
    meatKeeperSource.Contains("IntroHubSkip", StringComparison.Ordinal),
    "Meat gate suppression is independent of direct-start compatibility");
int phoneKeeperStart = pluginSource.IndexOf(
    "internal sealed class AreaPhoneAccessKeeper", areaAccessStart, StringComparison.Ordinal);
Equal(true, phoneKeeperStart > areaAccessStart,
    "Area Access prototype has a bounded production source region");
string areaAccessSource = pluginSource[areaAccessStart..phoneKeeperStart];
Equal(true,
    areaAccessSource.Contains(
        "AreaAccessSessionPolicy.IsAuthenticatedCompatible", StringComparison.Ordinal),
    "authenticated Area Access state is derived from Area Access slot compatibility");
Equal(true,
    pluginSource.Contains("AreaAccessPrototype.EndAuthenticatedSession();", StringComparison.Ordinal),
    "session teardown clears authenticated Area Access compatibility");

int transitionPatchStart = pluginSource.IndexOf(
    "internal static class IntroRoomToHubRedirectPatches", StringComparison.Ordinal);
int transitionPatchEnd = pluginSource.IndexOf(
    "internal static class ProgressionPatches", transitionPatchStart, StringComparison.Ordinal);
Equal(true, transitionPatchStart >= 0 && transitionPatchEnd > transitionPatchStart,
    "transition patch has a bounded production source region");
string transitionPatchSource = pluginSource[transitionPatchStart..transitionPatchEnd];
Equal(true,
    transitionPatchSource.Contains("AreaAccessDestinationPolicy.Decide(", StringComparison.Ordinal) &&
    transitionPatchSource.Contains("AreaAccessDestinationPolicy.MusicLabRoomId", StringComparison.Ordinal) &&
    transitionPatchSource.Contains("AreaAccessPrototype.TryGetAreaForHubRoom(", StringComparison.Ordinal) &&
    transitionPatchSource.Contains("AreaAccessPrototype.AuthenticatedCompatibleSession", StringComparison.Ordinal),
    "every unowned major-area destination is rewritten through the pure guard policy");
Equal(true,
    transitionPatchSource.Contains(
        "string.Equals(originRoom, AreaAccessDestinationPolicy.MusicLabRoomId", StringComparison.Ordinal),
    "existing Hub6 phone-origin hard guard remains explicit");

int mouseKeeperStart = pluginSource.IndexOf(
    "internal sealed class MeatMouseEscortRecoveryKeeper", StringComparison.Ordinal);
int mouseKeeperEnd = pluginSource.IndexOf(
    "internal static class AreaAccessPrototype", mouseKeeperStart, StringComparison.Ordinal);
Equal(true, mouseKeeperStart >= 0 && mouseKeeperEnd > mouseKeeperStart,
    "mouse escort recovery has a bounded production source region");
string mouseKeeperSource = pluginSource[mouseKeeperStart..mouseKeeperEnd];
Equal(true,
    mouseKeeperSource.Contains("_runtime.Observe(snapshot)", StringComparison.Ordinal) &&
    mouseKeeperSource.Contains("\"SpawnMeatAnimalCharacterOnDemand\"", StringComparison.Ordinal) &&
    mouseKeeperSource.Contains("\"SetSpawnCount\"", StringComparison.Ordinal) &&
    mouseKeeperSource.Contains("\"OnTrigger\"", StringComparison.Ordinal),
    "mouse recovery uses the exact existing native spawner reset and trigger boundary");
int mouseRuntimeBindingStart = mouseKeeperSource.IndexOf(
    "GameObject? spawnerObject", StringComparison.Ordinal);
int mouseRuntimeBindingEnd = mouseKeeperSource.IndexOf(
    "bool requirementReadable", mouseRuntimeBindingStart, StringComparison.Ordinal);
Equal(true,
    mouseRuntimeBindingStart >= 0 && mouseRuntimeBindingEnd > mouseRuntimeBindingStart,
    "mouse recovery native binding has a bounded production source region");
string mouseRuntimeBindingSource =
    mouseKeeperSource[mouseRuntimeBindingStart..mouseRuntimeBindingEnd];
Equal(true,
    mouseRuntimeBindingSource.Contains("new[] { typeof(IntPtr) }", StringComparison.Ordinal) &&
    mouseRuntimeBindingSource.Contains("pointerConstructor.Invoke", StringComparison.Ordinal) &&
    mouseKeeperSource.Split(
        "MeatMouseEscortCharacterReader.Read(", StringSplitOptions.None).Length - 1 == 2,
    "recovery binds the exact typed pointer wrapper and uses the exact nullable Character reader");
Equal(true,
    mouseKeeperSource.Contains("MeatMouseEscortRecoveryPolicy.RequirementStatedFlag", StringComparison.Ordinal) &&
    mouseKeeperSource.Contains("MeatMouseEscortRecoveryPolicy.RevolutionTriggeredFlag", StringComparison.Ordinal) &&
    mouseKeeperSource.Contains("AreaAccessPrototype.AuthenticatedCompatibleSession", StringComparison.Ordinal),
    "mouse recovery requires readable native quest state and authenticated Area Access");
Equal(false,
    mouseKeeperSource.Contains("QueueLocation", StringComparison.Ordinal) ||
    mouseKeeperSource.Contains("TrySubmitProgressionFlag", StringComparison.Ordinal) ||
    mouseKeeperSource.Contains("Instantiate", StringComparison.Ordinal) ||
    mouseKeeperSource.Contains("Clone", StringComparison.Ordinal) ||
    mouseKeeperSource.Contains("SpawnRoom23MouseCharacterRequest", StringComparison.Ordinal),
    "mouse recovery dispatches no checks, writes no progression, and creates no synthetic NPC");
int mouseDecisionIndex = mouseKeeperSource.IndexOf(
    "_runtime.Observe(snapshot)", StringComparison.Ordinal);
int mouseAttemptIndex = mouseKeeperSource.IndexOf(
    "_runtime.TryConsumeAttempt()", StringComparison.Ordinal);
int mouseNativeCallIndex = mouseKeeperSource.IndexOf(
    "resetSpawnCount!.Invoke", StringComparison.Ordinal);
Equal(true,
    mouseDecisionIndex >= 0 && mouseAttemptIndex > mouseDecisionIndex &&
    mouseNativeCallIndex > mouseAttemptIndex,
    "production consumes the bounded attempt only after eligibility and immediately before the native call");

int mouseBindingDiagnosticStart = mouseKeeperSource.IndexOf(
    "private static void EmitNativeBindingDiagnostic", StringComparison.Ordinal);
int mouseBindingDiagnosticEnd = mouseKeeperSource.IndexOf(
    "private void ResetForRoomExit", Math.Max(0, mouseBindingDiagnosticStart), StringComparison.Ordinal);
Equal(true,
    mouseBindingDiagnosticStart >= 0 && mouseBindingDiagnosticEnd > mouseBindingDiagnosticStart,
    "mouse recovery exposes a bounded native-binding diagnostic region");
string mouseBindingDiagnosticSource =
    mouseKeeperSource[mouseBindingDiagnosticStart..mouseBindingDiagnosticEnd];
Equal(true,
    mouseBindingDiagnosticSource.Contains(
        "GameObject.Find(MeatMouseEscortRecoveryPolicy.NativeSpawnerPath)", StringComparison.Ordinal) &&
    mouseBindingDiagnosticSource.Contains("transform.childCount", StringComparison.Ordinal) &&
    mouseBindingDiagnosticSource.Contains("transform.GetChild", StringComparison.Ordinal),
    "binding diagnostic inspects only the exact native spawner container and its direct children");
Equal(false,
    mouseBindingDiagnosticSource.Contains("GetComponentsInChildren", StringComparison.Ordinal) ||
    mouseBindingDiagnosticSource.Contains("Resources.FindObjectsOfTypeAll", StringComparison.Ordinal),
    "binding diagnostic does not widen into descendant or scene-wide scans");
Equal(true,
    mouseBindingDiagnosticSource.Contains("GetComponents<Component>()", StringComparison.Ordinal) &&
    mouseBindingDiagnosticSource.Contains("il2cpp_object_get_class", StringComparison.Ordinal) &&
    mouseBindingDiagnosticSource.Contains("SpawnMeatAnimalCharacterOnDemand", StringComparison.Ordinal) &&
    mouseBindingDiagnosticSource.Contains("new[] { typeof(IntPtr) }", StringComparison.Ordinal) &&
    mouseBindingDiagnosticSource.Contains("SetSpawnCount", StringComparison.Ordinal) &&
    mouseBindingDiagnosticSource.Contains("OnTrigger", StringComparison.Ordinal) &&
    mouseBindingDiagnosticSource.Contains(
        "MeatMouseEscortCharacterReader.Read(wrapped)", StringComparison.Ordinal) &&
    mouseBindingDiagnosticSource.Contains("characterReadStage", StringComparison.Ordinal) &&
    mouseBindingDiagnosticSource.Contains("BINDING WRAPPER SUMMARY", StringComparison.Ordinal),
    "binding diagnostic reports every exact native binding and Character-read boundary");
Equal(false,
    mouseBindingDiagnosticSource.Contains("resetSpawnCount!.Invoke", StringComparison.Ordinal) ||
    mouseBindingDiagnosticSource.Contains("triggerSpawner!.Invoke", StringComparison.Ordinal) ||
    mouseBindingDiagnosticSource.Contains("SetActive", StringComparison.Ordinal) ||
    mouseBindingDiagnosticSource.Contains("QueueLocation", StringComparison.Ordinal) ||
    mouseBindingDiagnosticSource.Contains("TrySubmitProgressionFlag", StringComparison.Ordinal) ||
    mouseBindingDiagnosticSource.Contains("Instantiate", StringComparison.Ordinal) ||
    mouseBindingDiagnosticSource.Contains("Clone", StringComparison.Ordinal),
    "binding diagnostic cannot invoke recovery, mutate scene state, write progression, dispatch checks, or clone NPCs");
int mouseBindingAvailabilityGuard = mouseKeeperSource.IndexOf(
    "room, _bindingDiagnosticEmitted, spawnerObject != null", StringComparison.Ordinal);
int mouseBindingEmissionMark = mouseKeeperSource.IndexOf(
    "_bindingDiagnosticEmitted = true", Math.Max(0, mouseBindingAvailabilityGuard), StringComparison.Ordinal);
int mouseBindingObserverCall = mouseKeeperSource.IndexOf(
    "EmitNativeBindingDiagnostic();", Math.Max(0, mouseBindingEmissionMark), StringComparison.Ordinal);
Equal(true,
    mouseBindingAvailabilityGuard >= 0 &&
    mouseBindingEmissionMark > mouseBindingAvailabilityGuard &&
    mouseBindingObserverCall > mouseBindingEmissionMark &&
    mouseKeeperSource.Contains("_bindingDiagnosticEmitted = false", StringComparison.Ordinal) &&
    mouseKeeperSource.Contains("MEAT MOUSE ESCORT BINDING failed", StringComparison.Ordinal) &&
    mouseBindingDiagnosticSource.Contains(
        "MeatMouseEscortBindingDiagnosticPolicy.ShouldInspectPath", StringComparison.Ordinal),
    "production consumes the fail-closed exact-scope emission only after the container is available");
Equal(true,
    mouseKeeperSource.Contains("EmitExactConditionDiagnostic();", StringComparison.Ordinal) &&
    mouseKeeperSource.Contains("MeatMouseEscortConditionDiagnosticPolicy.ShouldEmit(", StringComparison.Ordinal) &&
    mouseKeeperSource.Contains("_conditionDiagnosticEmitted = false", StringComparison.Ordinal),
    "production emits the separate exact condition diagnostic once after its subtree loads");
int conditionPolling = mouseKeeperSource.IndexOf(
    "TryEmitExactConditionDiagnostic(room);", StringComparison.Ordinal);
int recoveryFrameGate = mouseKeeperSource.IndexOf(
    "if (!_runtime.ShouldEvaluateFrame())", StringComparison.Ordinal);
Equal(true, conditionPolling >= 0 && recoveryFrameGate > conditionPolling,
    "condition polling continues independently when recovery has skipped or exhausted");
int exactConditionStart = mouseKeeperSource.IndexOf(
    "private static void EmitExactConditionDiagnostic", StringComparison.Ordinal);
int exactConditionEnd = mouseKeeperSource.IndexOf(
    "private static void EmitNativeBindingDiagnostic", Math.Max(0, exactConditionStart), StringComparison.Ordinal);
Equal(true, exactConditionStart >= 0 && exactConditionEnd > exactConditionStart,
    "exact condition diagnostic has a bounded source region");
string exactConditionSource = mouseKeeperSource[exactConditionStart..exactConditionEnd];
Equal(true,
    exactConditionSource.Contains("GetComponents<Component>()", StringComparison.Ordinal) &&
    exactConditionSource.Contains("DiagnosticNativeClassFullName", StringComparison.Ordinal) &&
    exactConditionSource.Contains("MeatMouseEscortConditionReader.Read(", StringComparison.Ordinal) &&
    exactConditionSource.Contains("GeneralCondition", StringComparison.Ordinal),
    "exact condition diagnostic logs native identity and evaluates only proven GeneralCondition wrappers");
Equal(false,
    exactConditionSource.Contains("GetComponentsInChildren", StringComparison.Ordinal) ||
    exactConditionSource.Contains("Resources.FindObjectsOfTypeAll", StringComparison.Ordinal) ||
    exactConditionSource.Contains("SetSpawnCount", StringComparison.Ordinal) ||
    exactConditionSource.Contains("OnTrigger", StringComparison.Ordinal) ||
    exactConditionSource.Contains("SetActive", StringComparison.Ordinal) ||
    exactConditionSource.Contains("QueueLocation", StringComparison.Ordinal) ||
    exactConditionSource.Contains("TrySubmitProgressionFlag", StringComparison.Ordinal) ||
    exactConditionSource.Contains("Instantiate", StringComparison.Ordinal) ||
    exactConditionSource.Contains("Clone", StringComparison.Ordinal),
    "exact condition diagnostic cannot mutate scene, progression, AP, or spawn state");
Console.WriteLine("Roots presentation policy tests passed.");
