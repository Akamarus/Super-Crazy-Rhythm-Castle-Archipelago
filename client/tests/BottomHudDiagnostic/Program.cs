using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

var policy = new BottomHudDiagnosticPolicy();
foreach (string lifecycle in new[] { "direct-start", "after-level-1", "after-reload", "after-restart" })
{
    Equal(BottomHudDiagnosticDecision.Emit,
        policy.Decide("GameRoom_Hub2", lifecycle, controlFound: true),
        $"{lifecycle} first snapshot");
    Equal(BottomHudDiagnosticDecision.SkipDuplicate,
        policy.Decide("GameRoom_Hub2", lifecycle, controlFound: true),
        $"{lifecycle} frame duplicate");
}

Equal(BottomHudDiagnosticDecision.WaitForControl,
    policy.Decide("GameRoom_Hub2", "new-save", controlFound: false),
    "missing exact control waits without consuming lifecycle");
Equal(BottomHudDiagnosticDecision.Emit,
    policy.Decide("GameRoom_Hub2", "new-save", controlFound: true),
    "late exact control emits snapshot");
Equal(BottomHudDiagnosticDecision.IgnoreRoom,
    policy.Decide("GameRoom_Hub6", "direct-start", controlFound: true),
    "Music Lab is not the Roots combined control");
Equal(false, BottomHudDiagnosticPolicy.RequestsMutation, "diagnostic is read-only");

string phaseSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "BottomHudPhaseNormalization.cs"));
Equal(true, phaseSource.Contains("SetPhase", StringComparison.Ordinal),
    "phase repair uses the exact native phase method");
Equal(true, phaseSource.Contains("phase != 0", StringComparison.Ordinal) ||
            File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "BottomHudPhasePolicy.cs"))
                .Contains("currentPhase != 0", StringComparison.Ordinal),
    "phase repair is limited to INVALID(0)");
Equal(false, phaseSource.Contains("SetPlayerTrackingDifficultyRequest", StringComparison.Ordinal),
    "phase repair never selects a difficulty");
Equal(false, phaseSource.Contains("TrySubmitProgressionFlag", StringComparison.Ordinal),
    "phase repair never writes progression flags");
Equal(false, phaseSource.Contains("SetActive(", StringComparison.Ordinal),
    "phase repair does not alter object activation");

foreach (string room in new[] { "GameRoom_Hub1", "GameRoom_Hub1A", "GameRoom_Hub2", "GameRoom_Hub3", "GameRoom_Hub4", "GameRoom_Hub5", "GameRoom_Hub6", "GameRoom_Hub7" })
{
    Equal(BottomHudPhaseDecision.NormalizeToIdle,
        BottomHudPhasePolicy.Decide(true, true, room, currentPhase: 0),
        $"{room} normalizes only INVALID phase");
}
Equal(BottomHudPhaseDecision.Preserve,
    BottomHudPhasePolicy.Decide(true, true, "GameRoom_Hub2", currentPhase: 1),
    "IDLE phase remains native");
Equal(BottomHudPhaseDecision.Preserve,
    BottomHudPhasePolicy.Decide(true, true, "GameRoom_Hub2", currentPhase: 2),
    "CALCULATING phase is never interrupted");
Equal(BottomHudPhaseDecision.Preserve,
    BottomHudPhasePolicy.Decide(false, true, "GameRoom_Hub2", currentPhase: 0),
    "disabled AP preserves vanilla");
Equal(BottomHudPhaseDecision.Preserve,
    BottomHudPhasePolicy.Decide(true, false, "GameRoom_Hub2", currentPhase: 0),
    "incompatible slot preserves vanilla");
Equal(BottomHudPhaseDecision.Preserve,
    BottomHudPhasePolicy.Decide(true, true, "GameRoom_05", currentPhase: 0),
    "gameplay room preserves native state");

string pluginSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));
int diagnosticStart = pluginSource.IndexOf("internal static class BottomHudDiagnostic", StringComparison.Ordinal);
int diagnosticEnd = pluginSource.IndexOf("internal static class PreviewAbilityRandomization", diagnosticStart, StringComparison.Ordinal);
if (diagnosticStart < 0 || diagnosticEnd < 0)
    throw new InvalidOperationException("Could not locate BottomHudDiagnostic source boundaries.");
string diagnosticSource = pluginSource[diagnosticStart..diagnosticEnd];
Equal(true, diagnosticSource.Contains("Root/GameRoom_Hub2_Logic/Objects/AmProContainer/AmProRobot", StringComparison.Ordinal),
    "diagnostic binds only the retained exact combined control path");
Equal(false, diagnosticSource.Contains("Resources.FindObjectsOfTypeAll", StringComparison.Ordinal),
    "diagnostic does not poll all loaded objects");
Equal(false, diagnosticSource.Contains("SetActive(", StringComparison.Ordinal),
    "diagnostic does not mutate object activation");
Equal(false, diagnosticSource.Contains("TrySubmitProgressionFlag", StringComparison.Ordinal),
    "diagnostic does not write progression flags");

Console.WriteLine("Bottom HUD diagnostic policy tests passed.");
