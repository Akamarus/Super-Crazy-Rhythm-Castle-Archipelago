namespace RhythmCastleAP;

internal enum MeatMouseEscortRecoveryDecision
{
    Preserve,
    ReevaluateNativeSpawner,
}

internal readonly record struct MeatMouseEscortRecoverySnapshot(
    bool AuthenticatedCompatible,
    string RoomId,
    bool RequirementReadable,
    bool RequirementStated,
    bool RevolutionReadable,
    bool RevolutionTriggered,
    bool NativeSpawnerIdentityKnown,
    bool LeaderReadable,
    bool LeaderPresent,
    bool AttemptedThisRoom);

internal static class MeatMouseEscortRecoveryPolicy
{
    internal const string RoomId = "GameRoom_Hub4";
    internal const string NativeSpawnerPath =
        "Root/GameRoom_Hub4_Logic/NPCs/SpecialAnimals/SpawnMouseLeader";
    internal const string RequirementStatedFlag =
        "MEAT_HUB_ACT_FOUR_BOUNCER_REQUIREMENT_STATED";
    internal const string RevolutionTriggeredFlag =
        "MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED";

    internal static MeatMouseEscortRecoveryDecision Decide(
        MeatMouseEscortRecoverySnapshot snapshot)
    {
        if (!snapshot.AuthenticatedCompatible ||
            !string.Equals(snapshot.RoomId, RoomId, StringComparison.Ordinal) ||
            !snapshot.RequirementReadable ||
            !snapshot.RequirementStated ||
            !snapshot.RevolutionReadable ||
            snapshot.RevolutionTriggered ||
            !snapshot.NativeSpawnerIdentityKnown ||
            !snapshot.LeaderReadable ||
            snapshot.LeaderPresent ||
            snapshot.AttemptedThisRoom)
        {
            return MeatMouseEscortRecoveryDecision.Preserve;
        }

        return MeatMouseEscortRecoveryDecision.ReevaluateNativeSpawner;
    }
}
