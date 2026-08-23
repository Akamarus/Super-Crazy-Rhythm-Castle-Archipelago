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
