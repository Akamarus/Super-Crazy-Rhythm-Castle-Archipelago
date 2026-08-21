using RhythmCastleAP;

static void AssertEqual(DiagnosticHotkeyAction expected, DiagnosticHotkeyAction actual, string scenario)
{
    if (expected != actual)
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

AssertEqual(DiagnosticHotkeyAction.Level4ToLiftQuest, DiagnosticHotkeyRouting.ForF4("GameRoom_08", control: false, shift: false, alt: false), "plain F4 inside Level 4");
AssertEqual(DiagnosticHotkeyAction.Level4ToLiftQuest, DiagnosticHotkeyRouting.ForF4("GameRoom_Hub2", control: false, shift: false, alt: false), "plain F4 beside Bucket Minion");
AssertEqual(DiagnosticHotkeyAction.Level4ToLiftQuest, DiagnosticHotkeyRouting.ForF4("GameRoom_09", control: false, shift: false, alt: false), "plain F4 inside Lift Quest");
AssertEqual(DiagnosticHotkeyAction.ExistingDefault, DiagnosticHotkeyRouting.ForF4("GameRoom_Hub8", control: false, shift: false, alt: false), "plain F4 outside the focused rooms");
AssertEqual(DiagnosticHotkeyAction.ExistingDefault, DiagnosticHotkeyRouting.ForF4("GameRoom_08", control: false, shift: false, alt: true), "Alt+F4 preserves existing behavior");
AssertEqual(DiagnosticHotkeyAction.ExistingDefault, DiagnosticHotkeyRouting.ForF4("GameRoom_08", control: true, shift: false, alt: false), "Ctrl+F4 preserves existing behavior");
AssertEqual(DiagnosticHotkeyAction.ExistingDefault, DiagnosticHotkeyRouting.ForF4("GameRoom_08", control: false, shift: true, alt: false), "Shift+F4 preserves existing behavior");
AssertEqual(DiagnosticHotkeyAction.ExistingDefault, DiagnosticHotkeyRouting.ForF4(null, control: false, shift: false, alt: false), "unknown room preserves existing behavior");
AssertEqual(DiagnosticHotkeyAction.ExistingDefault, DiagnosticHotkeyRouting.ForF4("GameRoom_080", control: false, shift: false, alt: false), "near-match room preserves existing behavior");

AssertEqual(DiagnosticHotkeyAction.Level4ToLiftQuest, DiagnosticHotkeyRouting.ForPlainF5("GameRoom_08"), "F5 inside Level 4");
AssertEqual(DiagnosticHotkeyAction.Level4ToLiftQuest, DiagnosticHotkeyRouting.ForPlainF5("GameRoom_Hub2"), "F5 beside Bucket Minion");
AssertEqual(DiagnosticHotkeyAction.Level4ToLiftQuest, DiagnosticHotkeyRouting.ForPlainF5("GameRoom_09"), "F5 inside Lift Quest");
AssertEqual(DiagnosticHotkeyAction.ExistingDefault, DiagnosticHotkeyRouting.ForPlainF5("GameRoom_Hub6"), "F5 keeps Music Lab behavior");

Console.WriteLine("Diagnostic hotkey routing tests passed.");
