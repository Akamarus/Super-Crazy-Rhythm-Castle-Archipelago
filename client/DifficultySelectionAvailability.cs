namespace RhythmCastleAP;

// Unity-thread reconciliation for skipped startup bookkeeping. This persists only
// assignment completion; native input/room guards and the player's selected difficulty remain native.
internal static class DifficultySelectionAvailability
{
    private const string AssignmentComplete = "ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE";
    private static TimeSpan _untilPoll;
    private static string _saveIdentity = "", _lastStatus = "";
    private static int _attempts;

    internal static void TickUnity(TimeSpan elapsed)
    {
        _untilPoll -= elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        if (_untilPoll > TimeSpan.Zero) return;
        _untilPoll = TimeSpan.FromSeconds(1);
        if (!Eligible) return;
        CassetteReceiptRandomization.WithStableQuestItemSave((processor, identity) =>
        {
            if (!Eligible) return;
            if (_saveIdentity != identity)
            {
                _saveIdentity = identity;
                _attempts = 0;
                _lastStatus = "";
            }
            if (!RootsBucketRandomization.TryReadProgressionFlag(AssignmentComplete, out bool complete)) return;
            if (complete) { Report("verified"); return; }
            if (_attempts >= 4) { Report("retry limit reached; native state preserved"); return; }
            _attempts++;
            bool submitted = WeedKillerRandomization.TrySubmitProgressionFlag(
                processor, AssignmentComplete, true, out _);
            bool verified = RootsBucketRandomization.TryReadProgressionFlag(AssignmentComplete, out bool after) && after;
            Report(verified ? "verified" : submitted ? "submitted; readback pending" : "submission failed; retry pending");
        });
    }

    private static bool Eligible => AreaAccessPrototype.Enabled && IntroHubSkip.Compatible &&
        AreaAccessPrototype.AuthenticatedCompatibleSession;

    private static void Report(string status)
    {
        if (_lastStatus == status) return;
        _lastStatus = status;
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] DIFFICULTY SELECTION AVAILABILITY save='{_saveIdentity}' status='{status}' attempts={_attempts}; assignment bookkeeping only; selected difficulty unchanged.");
    }
}
