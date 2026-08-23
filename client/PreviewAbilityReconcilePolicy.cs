namespace RhythmCastleAP;

internal enum PreviewAbilityReconcileDecision
{
    NoOwnership,
    Incompatible,
    SaveUnavailable,
    AlreadyGranted,
    SubmitGrant,
}

internal static class PreviewAbilityReconcilePolicy
{
    internal static PreviewAbilityReconcileDecision Decide(
        int receivedCount,
        bool compatible,
        bool saveAvailable,
        bool? nativeFlag)
    {
        if (receivedCount < 1)
            return PreviewAbilityReconcileDecision.NoOwnership;
        if (!compatible)
            return PreviewAbilityReconcileDecision.Incompatible;
        if (!saveAvailable || nativeFlag == null)
            return PreviewAbilityReconcileDecision.SaveUnavailable;
        return nativeFlag.Value
            ? PreviewAbilityReconcileDecision.AlreadyGranted
            : PreviewAbilityReconcileDecision.SubmitGrant;
    }
}

internal interface IPreviewAbilityNativeAdapter
{
    bool SaveAvailable { get; }
    bool TryRead(string nativeFlag, out bool owned);
    bool TrySubmit(string nativeFlag, out string detail);
}

internal interface IPreviewAbilityReconciler
{
    string NativeFlag { get; }
    string LastOutcome { get; }
    IPreviewAbilityNativeAdapter NativeAdapterForTests { get; }
    void Configure(bool compatible);
    void NoteReceivedCount(int count);
    void OnLifecyclePoint(string reason);
    void TickPending(TimeSpan elapsed);
}

internal abstract class PreviewAbilityReconcilerBase : IPreviewAbilityReconciler
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);
    private readonly IPreviewAbilityNativeAdapter _native;
    private bool _compatible;
    private int _receivedCount;
    private bool _verificationPending;
    private TimeSpan _retryElapsed;

    protected PreviewAbilityReconcilerBase(IPreviewAbilityNativeAdapter native)
    {
        _native = native;
    }

    public abstract string NativeFlag { get; }
    public string LastOutcome { get; private set; } = "not-evaluated";
    public IPreviewAbilityNativeAdapter NativeAdapterForTests => _native;

    public void Configure(bool compatible) => _compatible = compatible;

    public void NoteReceivedCount(int count)
    {
        if (count > _receivedCount)
            _receivedCount = count;
    }

    public void OnLifecyclePoint(string reason)
    {
        bool owned = false;
        bool readable = _native.SaveAvailable && _native.TryRead(NativeFlag, out owned);
        bool? nativeFlag = readable ? owned : null;
        PreviewAbilityReconcileDecision decision = PreviewAbilityReconcilePolicy.Decide(
            _receivedCount,
            _compatible,
            readable,
            nativeFlag);

        switch (decision)
        {
            case PreviewAbilityReconcileDecision.NoOwnership:
                LastOutcome = "not-owned";
                return;
            case PreviewAbilityReconcileDecision.Incompatible:
                LastOutcome = "incompatible";
                return;
            case PreviewAbilityReconcileDecision.SaveUnavailable:
                LastOutcome = "save-unavailable";
                return;
            case PreviewAbilityReconcileDecision.AlreadyGranted:
                _verificationPending = false;
                _retryElapsed = TimeSpan.Zero;
                LastOutcome = "verified-owned";
                return;
            case PreviewAbilityReconcileDecision.SubmitGrant:
                if (_native.TrySubmit(NativeFlag, out _))
                {
                    _verificationPending = true;
                    _retryElapsed = TimeSpan.Zero;
                    LastOutcome = "grant-submitted";
                }
                else
                {
                    LastOutcome = "grant-failed";
                }
                return;
            default:
                throw new InvalidOperationException($"Unhandled preview ability decision at {reason}.");
        }
    }

    public void TickPending(TimeSpan elapsed)
    {
        if (!_verificationPending || elapsed <= TimeSpan.Zero)
            return;
        _retryElapsed += elapsed;
        if (_retryElapsed < RetryDelay)
            return;
        _retryElapsed = TimeSpan.Zero;
        _verificationPending = false;
        OnLifecyclePoint("bounded verification retry");
    }
}
