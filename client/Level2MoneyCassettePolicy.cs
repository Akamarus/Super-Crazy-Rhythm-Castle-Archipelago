namespace RhythmCastleAP;

internal readonly record struct Level2MoneyCassetteSourceDecision(
    bool QueueLocation,
    bool ReplacementWasCollected);

internal static class Level2MoneyCassettePolicy
{
    internal const string HaveInBag = "HAVE_IN_BAG";
    internal const string HaveDeposited = "HAVE_DEPOSITED";
    internal const string HaveNotEarned = "HAVE_NOT_EARNED";

    internal static bool IsSourceAward(string? level, string? variant, bool wasCollected)
    {
        return wasCollected &&
            string.Equals(level, "Level_06", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(variant, "LevelVariant_Default", StringComparison.OrdinalIgnoreCase);
    }

    internal static Level2MoneyCassetteSourceDecision DecideSourceAward(
        string? level,
        string? variant,
        bool wasCollected)
    {
        bool queueLocation = IsSourceAward(level, variant, wasCollected);
        return new Level2MoneyCassetteSourceDecision(
            queueLocation,
            queueLocation ? false : wasCollected);
    }
}

internal enum Level2MoneyCassetteReconcileDecision
{
    Disabled,
    NoOwnership,
    SaveUnavailable,
    ProcessorUnavailable,
    AlreadyOwned,
    RequestHaveInBag,
    UnknownNativeStatus,
}

internal sealed class Level2MoneyCassetteRuntime
{
    private bool _enabled;
    private int _receivedCount;

    internal string? RequestedNativeStatus { get; private set; }

    internal void Configure(bool enabled)
    {
        _enabled = enabled;
    }

    internal void NoteReceivedCount(int count)
    {
        if (count > _receivedCount)
            _receivedCount = count;
    }

    internal Level2MoneyCassetteReconcileDecision ObserveNativeStatus(
        bool saveAvailable,
        bool processorAvailable,
        string? nativeStatus)
    {
        RequestedNativeStatus = null;

        if (!_enabled)
            return Level2MoneyCassetteReconcileDecision.Disabled;
        if (_receivedCount < 1)
            return Level2MoneyCassetteReconcileDecision.NoOwnership;
        if (!saveAvailable)
            return Level2MoneyCassetteReconcileDecision.SaveUnavailable;
        if (IsOwned(nativeStatus))
            return Level2MoneyCassetteReconcileDecision.AlreadyOwned;
        if (!string.Equals(nativeStatus, Level2MoneyCassettePolicy.HaveNotEarned, StringComparison.OrdinalIgnoreCase))
            return Level2MoneyCassetteReconcileDecision.UnknownNativeStatus;
        if (!processorAvailable)
            return Level2MoneyCassetteReconcileDecision.ProcessorUnavailable;

        RequestedNativeStatus = Level2MoneyCassettePolicy.HaveInBag;
        return Level2MoneyCassetteReconcileDecision.RequestHaveInBag;
    }

    private static bool IsOwned(string? nativeStatus)
    {
        return string.Equals(nativeStatus, Level2MoneyCassettePolicy.HaveInBag, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(nativeStatus, Level2MoneyCassettePolicy.HaveDeposited, StringComparison.OrdinalIgnoreCase);
    }
}
