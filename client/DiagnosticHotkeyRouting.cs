namespace RhythmCastleAP;

internal enum DiagnosticHotkeyAction
{
    ExistingDefault,
    Level4ToLiftQuest,
}

internal static class DiagnosticHotkeyRouting
{
    public static DiagnosticHotkeyAction ForF4(
        string? roomId,
        bool control,
        bool shift,
        bool alt) =>
        !control && !shift && !alt && IsLevel4ToLiftQuestRoom(roomId)
            ? DiagnosticHotkeyAction.Level4ToLiftQuest
            : DiagnosticHotkeyAction.ExistingDefault;

    public static DiagnosticHotkeyAction ForPlainF5(string? roomId) =>
        IsLevel4ToLiftQuestRoom(roomId)
            ? DiagnosticHotkeyAction.Level4ToLiftQuest
            : DiagnosticHotkeyAction.ExistingDefault;

    private static bool IsLevel4ToLiftQuestRoom(string? roomId) =>
        string.Equals(roomId, "GameRoom_08", StringComparison.Ordinal) ||
        string.Equals(roomId, "GameRoom_Hub2", StringComparison.Ordinal) ||
        string.Equals(roomId, "GameRoom_09", StringComparison.Ordinal);
}
