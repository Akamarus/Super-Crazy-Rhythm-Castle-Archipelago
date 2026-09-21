namespace RhythmCastleAP;

internal static class CassetteChestRewardPolicy
{
    private const string Root = "Root/GameRoom_Hub6_Logic/GameRoom_Hub6_Script/MiscSequences/ChestRewards/SongRewardSequence_";
    // Audited Hub6 reward reactors only. Chest collection, persistence, AP receipt,
    // and Cassette Lord insertion are separate native paths and remain untouched.
    private static readonly HashSet<string> Grants = new(StringComparer.Ordinal)
    {
        Root + "Quicksand/AwardCassette", Root + "Flamenco/AwardCassette",
        Root + "TenFourGoodBuddy/AwardCassette", Root + "Zen/AwardCassette",
        Root + "Wiggle/AwardCassette",
    };
    private static readonly HashSet<string> Popups = new(StringComparer.Ordinal)
    {
        Root + "Quicksand/CassettePopup", Root + "Flamenco/CassettePopup",
        Root + "TenFourGoodBuddy/CassettePopup", Root + "Zen/CassettePopup",
        Root + "Wiggle/CassettePopup",
    };

    internal static bool ShouldSuppress(bool enabled, bool currentSaveBound, string? path, bool popup) =>
        enabled && currentSaveBound && path != null && (popup ? Popups : Grants).Contains(path);
}

internal static class CassetteChestRewardAdmission
{
    internal static void Run(bool enabled, bool popup, Func<string> readPath,
        Func<bool> isCurrentSaveBound, Action original, Action<string> suppressed,
        Action<Exception> report)
    {
        bool suppress = false;
        try
        {
            if (enabled)
            {
                string path = readPath();
                if (CassetteChestRewardPolicy.ShouldSuppress(true, true, path, popup))
                {
                    suppress = isCurrentSaveBound();
                    if (suppress) suppressed(path);
                }
            }
        }
        catch (Exception ex) { try { report(ex); } catch { } }
        // A logging failure cannot undo an already proven suppression. An original
        // exception must propagate without a second native invocation.
        if (!suppress) original();
    }
}

internal static class CassetteChestHookInstallation
{
    // IL2CPP preserves explicit-interface dots; generated interop wrappers rename
    // them to underscores. Resolve the native name (token 0x06007B56 in v29 metadata).
    internal static (string Owner, string Method, bool Popup) Target(bool popup) => popup
        ? ("PopupNewSongCassetteDetailsSequenceStep", "Trigger", true)
        : ("ObtainSongCassetteOnTrigger", "TriggerReactor.OnTrigger", false);

    internal static void Run(Func<bool, bool> install)
    {
        // Cosmetic suppression must never hide an unprotected vanilla reward.
        if (install(false)) install(true);
    }
}
