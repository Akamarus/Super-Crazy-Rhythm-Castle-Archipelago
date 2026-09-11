namespace RhythmCastleAP;

internal enum MeatMouseEscortRecoveryDecision
{
    Preserve,
    RetryPending,
    ReevaluateNativeSpawner,
}

internal enum MeatMouseEscortRecoveryObservation
{
    None,
    Pending,
    Skipped,
    Failed,
    Ready,
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
        if (!string.Equals(snapshot.RoomId, RoomId, StringComparison.Ordinal) ||
            snapshot.AttemptedThisRoom)
        {
            return MeatMouseEscortRecoveryDecision.Preserve;
        }

        if (!snapshot.AuthenticatedCompatible ||
            !snapshot.RequirementReadable ||
            !snapshot.RevolutionReadable ||
            !snapshot.NativeSpawnerIdentityKnown ||
            !snapshot.LeaderReadable)
        {
            return MeatMouseEscortRecoveryDecision.RetryPending;
        }

        if (!snapshot.RequirementStated ||
            snapshot.RevolutionTriggered ||
            snapshot.LeaderPresent)
        {
            return MeatMouseEscortRecoveryDecision.Preserve;
        }

        return MeatMouseEscortRecoveryDecision.ReevaluateNativeSpawner;
    }

    internal static int? RetryDelayFrames(int pendingEvaluationCount) =>
        pendingEvaluationCount switch
        {
            0 => 15,
            1 => 30,
            2 => 60,
            3 => 120,
            4 => 240,
            5 => 480,
            _ => null,
        };
}

internal sealed class MeatMouseEscortRecoveryRuntime
{
    private int _framesUntilNextEvaluation;
    private bool _ready;

    internal bool AttemptConsumed { get; private set; }
    internal bool Finished { get; private set; }
    internal int PendingEvaluationCount { get; private set; }

    internal bool ShouldEvaluateFrame() =>
        !AttemptConsumed && !Finished && _framesUntilNextEvaluation == 0;

    internal void AdvanceFrame()
    {
        if (_framesUntilNextEvaluation > 0)
            _framesUntilNextEvaluation--;
    }

    internal MeatMouseEscortRecoveryObservation Observe(
        MeatMouseEscortRecoverySnapshot snapshot)
    {
        if (AttemptConsumed || Finished)
            return MeatMouseEscortRecoveryObservation.None;

        MeatMouseEscortRecoveryDecision decision =
            MeatMouseEscortRecoveryPolicy.Decide(snapshot);
        if (decision == MeatMouseEscortRecoveryDecision.ReevaluateNativeSpawner)
        {
            _ready = true;
            return MeatMouseEscortRecoveryObservation.Ready;
        }

        _ready = false;
        if (decision == MeatMouseEscortRecoveryDecision.Preserve)
        {
            Finished = true;
            return MeatMouseEscortRecoveryObservation.Skipped;
        }

        int? delay = MeatMouseEscortRecoveryPolicy.RetryDelayFrames(PendingEvaluationCount);
        PendingEvaluationCount++;
        if (!delay.HasValue)
        {
            Finished = true;
            return MeatMouseEscortRecoveryObservation.Failed;
        }

        _framesUntilNextEvaluation = delay.Value;
        return MeatMouseEscortRecoveryObservation.Pending;
    }

    internal bool TryConsumeAttempt()
    {
        if (!_ready || AttemptConsumed || Finished)
            return false;

        _ready = false;
        AttemptConsumed = true;
        Finished = true;
        return true;
    }

    internal void Reset()
    {
        _framesUntilNextEvaluation = 0;
        _ready = false;
        AttemptConsumed = false;
        Finished = false;
        PendingEvaluationCount = 0;
    }
}
