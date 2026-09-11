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
    AreaAccessDestinationPolicy.Decide(true, "GameRoom_Hub4", true, false),
    "an unowned major-area destination is redirected to Music Lab");
Equal(AreaAccessDestinationDecision.RedirectToMusicLab,
    AreaAccessDestinationPolicy.Decide(true, "GameRoom_Hub2", true, false),
    "destination guarding is independent of the transition origin");
Equal(AreaAccessDestinationDecision.Preserve,
    AreaAccessDestinationPolicy.Decide(false, "GameRoom_Hub4", true, false),
    "disabled Area Access preserves native transitions");
Equal(AreaAccessDestinationDecision.Preserve,
    AreaAccessDestinationPolicy.Decide(true, "GameRoom_Hub4", true, true),
    "owned major-area destinations are preserved");
Equal(AreaAccessDestinationDecision.Preserve,
    AreaAccessDestinationPolicy.Decide(true, "GameRoom_27", false, false),
    "non-area destinations are preserved");
Equal(AreaAccessDestinationDecision.Preserve,
    AreaAccessDestinationPolicy.Decide(true, AreaAccessDestinationPolicy.MusicLabRoomId, false, false),
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
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(authenticatedCompatible: false)),
    "unauthenticated Area Access session fails closed");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(roomId: "GameRoom_Hub40")),
    "mouse recovery is exact-room scoped");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(requirementReadable: false)),
    "unreadable Act 4 requirement fails closed");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(requirementStated: false)),
    "unstated Act 4 requirement preserves native state");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(revolutionReadable: false)),
    "unreadable revolution state fails closed");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(revolutionTriggered: true)),
    "completed revolution is never repaired");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(nativeSpawnerIdentityKnown: false)),
    "unknown native spawner identity fails closed");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(leaderReadable: false)),
    "unreadable native Character state fails closed");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(leaderPresent: true)),
    "present mouse leader is never duplicated");
Equal(MeatMouseEscortRecoveryDecision.Preserve,
    MeatMouseEscortRecoveryPolicy.Decide(MouseRecovery(attemptedThisRoom: true)),
    "mouse recovery is bounded to one attempt per room lifetime");

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
    transitionPatchSource.Contains("AreaAccessPrototype.TryGetAreaForHubRoom(", StringComparison.Ordinal),
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
    mouseKeeperSource.Contains("MeatMouseEscortRecoveryPolicy.Decide(", StringComparison.Ordinal) &&
    mouseKeeperSource.Contains("\"SpawnMeatAnimalCharacterOnDemand\"", StringComparison.Ordinal) &&
    mouseKeeperSource.Contains("\"SetSpawnCount\"", StringComparison.Ordinal) &&
    mouseKeeperSource.Contains("\"OnTrigger\"", StringComparison.Ordinal),
    "mouse recovery uses the exact existing native spawner reset and trigger boundary");
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
Console.WriteLine("Roots presentation policy tests passed.");
