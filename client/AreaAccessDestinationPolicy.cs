namespace RhythmCastleAP;

internal enum AreaAccessDestinationDecision
{
    Preserve,
    RedirectToMusicLab,
}

internal static class AreaAccessDestinationPolicy
{
    internal const string MusicLabRoomId = "GameRoom_Hub6";

    internal static AreaAccessDestinationDecision Decide(
        bool enabled,
        string? destinationRoomId,
        bool isMajorAreaHub,
        bool ownsDestination)
    {
        if (!enabled ||
            string.IsNullOrEmpty(destinationRoomId) ||
            string.Equals(destinationRoomId, MusicLabRoomId, StringComparison.Ordinal) ||
            !isMajorAreaHub ||
            ownsDestination)
        {
            return AreaAccessDestinationDecision.Preserve;
        }

        return AreaAccessDestinationDecision.RedirectToMusicLab;
    }
}
