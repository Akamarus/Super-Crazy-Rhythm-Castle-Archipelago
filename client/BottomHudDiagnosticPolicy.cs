namespace RhythmCastleAP;

internal enum BottomHudDiagnosticDecision
{
    IgnoreRoom,
    WaitForControl,
    Emit,
    SkipDuplicate,
}

internal sealed class BottomHudDiagnosticPolicy
{
    private readonly HashSet<string> _emitted = new(StringComparer.OrdinalIgnoreCase);

    internal const bool RequestsMutation = false;

    internal BottomHudDiagnosticDecision Decide(
        string? roomId,
        string? lifecycle,
        bool controlFound)
    {
        if (!string.Equals(roomId, "GameRoom_Hub2", StringComparison.OrdinalIgnoreCase))
            return BottomHudDiagnosticDecision.IgnoreRoom;
        if (!controlFound)
            return BottomHudDiagnosticDecision.WaitForControl;

        string key = $"{roomId}|{lifecycle?.Trim()}";
        return _emitted.Add(key)
            ? BottomHudDiagnosticDecision.Emit
            : BottomHudDiagnosticDecision.SkipDuplicate;
    }
}
