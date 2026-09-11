namespace RhythmCastleAP;

internal static class MeatMouseEscortBindingDiagnosticPolicy
{
    internal const bool RequestsMutation = false;

    internal static bool ShouldInspectPath(string? path)
    {
        if (string.Equals(
                path,
                MeatMouseEscortRecoveryPolicy.NativeSpawnerPath,
                StringComparison.Ordinal))
            return true;

        string childPrefix = MeatMouseEscortRecoveryPolicy.NativeSpawnerPath + "/";
        if (path == null || !path.StartsWith(childPrefix, StringComparison.Ordinal))
            return false;

        string relative = path[childPrefix.Length..];
        return relative.Length > 0 && !relative.Contains('/', StringComparison.Ordinal);
    }

    internal static bool ShouldEmit(string? roomId, bool alreadyEmitted) =>
        !alreadyEmitted &&
        string.Equals(roomId, MeatMouseEscortRecoveryPolicy.RoomId, StringComparison.Ordinal);
}
