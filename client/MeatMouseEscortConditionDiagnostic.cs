using System.Reflection;

namespace RhythmCastleAP;

internal static class MeatMouseEscortConditionDiagnosticPolicy
{
    internal const string SpawnConditionPath =
        MeatMouseEscortRecoveryPolicy.NativeSpawnerPath + "/SpawnCondition";
    internal const string RevolutionConditionPath =
        SpawnConditionPath + "/RevolutionNotTriggeredCondition";

    internal static bool ShouldInspectPath(string? path) =>
        string.Equals(path, SpawnConditionPath, StringComparison.Ordinal) ||
        string.Equals(path, RevolutionConditionPath, StringComparison.Ordinal);

    internal static bool ShouldEmit(string? roomId, bool alreadyEmitted, bool exactSubtreeAvailable) =>
        !alreadyEmitted && exactSubtreeAvailable &&
        string.Equals(roomId, MeatMouseEscortRecoveryPolicy.RoomId, StringComparison.Ordinal);
}

internal sealed class MeatMouseEscortConditionDiagnosticRuntime
{
    private static readonly int[] RetryDelays = { 15, 30, 60, 120, 240, 480 };
    private int _framesUntilPoll;
    private int _polls;

    internal bool Emitted { get; private set; }
    internal bool Finished => Emitted || _polls > RetryDelays.Length;

    internal void Reset()
    {
        _framesUntilPoll = 0;
        _polls = 0;
        Emitted = false;
    }

    internal bool ShouldEvaluateFrame() => !Finished && _framesUntilPoll == 0;

    internal void AdvanceFrame()
    {
        if (_framesUntilPoll > 0)
            _framesUntilPoll--;
    }

    internal bool Observe(string? roomId, bool exactSubtreeAvailable)
    {
        if (!ShouldEvaluateFrame())
            return false;

        _polls++;
        if (MeatMouseEscortConditionDiagnosticPolicy.ShouldEmit(
                roomId, Emitted, exactSubtreeAvailable))
        {
            Emitted = true;
            return true;
        }

        if (_polls <= RetryDelays.Length)
            _framesUntilPoll = RetryDelays[_polls - 1];
        return false;
    }
}

internal readonly record struct MeatMouseEscortConditionState(bool Readable, bool Met, string Stage);

internal static class MeatMouseEscortConditionReader
{
    internal static MeatMouseEscortConditionState Read(object? nativeCondition)
    {
        if (nativeCondition == null)
            return new(false, false, "condition-wrapper-unavailable");

        Type nativeType = nativeCondition.GetType();
        bool generalCondition = false;
        for (Type? current = nativeType; current != null; current = current.BaseType)
        {
            if (string.Equals(current.FullName, "GeneralCondition", StringComparison.Ordinal))
            {
                generalCondition = true;
                break;
            }
        }
        if (!generalCondition)
            return new(false, false, "general-condition-contract-unavailable");

        MethodInfo[] checks;
        try
        {
            checks = nativeType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method =>
                    string.Equals(method.Name, "CheckIfMet", StringComparison.Ordinal) &&
                    method.GetParameters().Length == 0)
                .ToArray();
        }
        catch
        {
            return new(false, false, "condition-method-lookup-failed");
        }
        if (checks.Length != 1 || checks[0].ReturnType != typeof(bool))
            return new(false, false, "exact-check-if-met-unavailable");

        try
        {
            return checks[0].Invoke(nativeCondition, Array.Empty<object>()) is bool met
                ? new(true, met, "success")
                : new(false, false, "condition-result-invalid");
        }
        catch
        {
            return new(false, false, "condition-evaluation-failed");
        }
    }
}
