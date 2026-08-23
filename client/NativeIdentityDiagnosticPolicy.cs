namespace RhythmCastleAP;

internal static class NativeIdentityDiagnosticPolicy
{
    private const string Hub6Prefix = "Root/GameRoom_Hub6_Logic/";

    private static readonly string[] BarrierTokens =
    {
        "barrier", "barricade", "construction", "fence", "roadblock",
    };

    private static readonly string[] StarHudTokens =
    {
        "starcounter", "star_counter", "star counter", "starhud", "star_hud",
    };

    internal static bool IsHub6Candidate(
        string path,
        IEnumerable<string> componentTypeNames)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string combined = path + " " + string.Join(" ", componentTypeNames ?? Array.Empty<string>());
        if (combined.Contains("RewardChest", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("MedalScoreRewardChest", StringComparison.OrdinalIgnoreCase))
            return false;

        bool starHud = StarHudTokens.Any(token =>
            combined.Contains(token, StringComparison.OrdinalIgnoreCase));
        bool hub6Barrier = path.StartsWith(Hub6Prefix, StringComparison.Ordinal) &&
                           BarrierTokens.Any(token =>
                               combined.Contains(token, StringComparison.OrdinalIgnoreCase));
        return starHud || hub6Barrier;
    }

    internal static bool IsDifficultyType(string typeName) =>
        !string.IsNullOrWhiteSpace(typeName) &&
        typeName.Contains("Difficulty", StringComparison.OrdinalIgnoreCase);

    internal static bool IsDifficultyMember(string memberName) =>
        !string.IsNullOrWhiteSpace(memberName) &&
        (memberName.Contains("Difficulty", StringComparison.OrdinalIgnoreCase) ||
         memberName.Contains("Normal", StringComparison.OrdinalIgnoreCase) ||
         memberName.Contains("Pro", StringComparison.OrdinalIgnoreCase) ||
         memberName.Contains("Available", StringComparison.OrdinalIgnoreCase) ||
         memberName.Contains("Unlocked", StringComparison.OrdinalIgnoreCase));
}
