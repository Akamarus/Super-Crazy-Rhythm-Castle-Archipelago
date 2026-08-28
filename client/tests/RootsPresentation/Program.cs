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
Console.WriteLine("Roots presentation policy tests passed.");
