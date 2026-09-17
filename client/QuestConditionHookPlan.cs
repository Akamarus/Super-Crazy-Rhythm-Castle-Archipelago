namespace RhythmCastleAP;

internal static class QuestConditionHookPlan
{
    internal const string SharedOwner = "IsGameProgressionFlagTrueCondition";
    internal const string SharedAlias = "IsGameProgressionFlagSetCondition";
    internal static readonly string[] DedicatedTypes = {
        "IsPlayableCharacterUnlockedCondition", "CompoundCondition"
    };
    internal static bool IsVerifiedAlias(long owner, long alias) => owner != 0 && owner == alias;
    internal static bool IsSharedEntry(string? type, string method) => type == SharedOwner && method == "CheckIfMet";
}
