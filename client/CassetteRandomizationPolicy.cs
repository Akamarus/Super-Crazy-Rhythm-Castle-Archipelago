namespace RhythmCastleAP;

internal sealed record CassetteSourceDecision(bool AllowNative, IReadOnlyList<string> SourceLocationsToQueue, IReadOnlyList<string> NativeSongsToSuppress, string Detail);

internal static class CassetteRandomizationPolicy
{
    internal const string HaveInBag="HAVE_IN_BAG", HaveDeposited="HAVE_DEPOSITED", HaveNotEarned="HAVE_NOT_EARNED", Invalid="INVALID";
    // This native event is deliberately never patched: boxing its IL2CPP payload crashed startup.
    internal static bool UseSelectedSaveChangedEventHook => false;

    internal static CassetteSourceDecision DecideLevelEvaluation(string? level,string? variant,bool succeeded,IReadOnlyDictionary<string,string> nativeStatuses)
    {
        if (!succeeded) return Allow("result failed");
        var mapped=CassetteCatalog.ForLevelSource(level,variant);
        if(mapped.Count==0) return Allow("source is unrelated");
        var newlyEarned=new List<CassetteDefinition>();
        foreach(var entry in mapped)
        {
            if(!nativeStatuses.TryGetValue(entry.NativeSong,out var status) || (!IsUnearned(status)&&!IsOwned(status))) return Allow($"native status unreadable for {entry.NativeSong}");
            if(IsUnearned(status)) newlyEarned.Add(entry);
        }
        if(newlyEarned.Count==0) return Allow("all mapped cassettes already owned");
        return new(false,newlyEarned.Select(x=>x.SourceName).ToArray(),newlyEarned.Select(x=>x.NativeSong).ToArray(),$"intercepted {newlyEarned.Count} newly earned cassette(s)");
    }
    internal static bool IsUnearned(string? s)=>string.Equals(s,Invalid,StringComparison.OrdinalIgnoreCase)||string.Equals(s,HaveNotEarned,StringComparison.OrdinalIgnoreCase);
    internal static bool IsOwned(string? s)=>string.Equals(s,HaveInBag,StringComparison.OrdinalIgnoreCase)||string.Equals(s,HaveDeposited,StringComparison.OrdinalIgnoreCase);
    internal static bool ShouldSuppressNativePointChestGrant(string? nativeSong,string? status,bool fromArchipelago)
    {
        if(fromArchipelago||!string.Equals(status,HaveInBag,StringComparison.OrdinalIgnoreCase)||nativeSong==null)return false;
        return CassetteCatalog.ByNativeSong.TryGetValue(nativeSong,out CassetteDefinition? entry)&&entry.SourceType==CassetteSourceType.MusicLabPointChest;
    }
    private static CassetteSourceDecision Allow(string detail)=>new(true,Array.Empty<string>(),Array.Empty<string>(),detail);
}

// Retained until the following receipt-reconciliation task generalizes the tested
// Money receipt path to all catalog entries.
internal enum Level2MoneyCassetteReconcileDecision { Disabled, NoOwnership, SaveUnavailable, ProcessorUnavailable, AlreadyOwned, RequestHaveInBag, UnknownNativeStatus }
internal sealed class Level2MoneyCassetteRuntime
{
    internal const int MaxRetryAttempts=8;
    private bool _enabled; private int _receivedCount; private int _remainingRetryAttempts; private bool _retryWindowOpen; private bool _retryWindowExhausted;
    internal string? RequestedNativeStatus { get; private set; }
    internal bool RetryWindowExhausted=>_retryWindowExhausted;
    internal int RemainingRetryAttempts=>_remainingRetryAttempts;
    internal void Configure(bool enabled){_enabled=enabled;if(!enabled)CloseRetryWindow();}
    internal void NoteReceivedCount(int count){if(count>_receivedCount)_receivedCount=count;}
    internal void OnSaveLifecyclePoint(){if(!_enabled||_receivedCount<1)return;_remainingRetryAttempts=MaxRetryAttempts;_retryWindowOpen=true;_retryWindowExhausted=false;}
    internal bool TryBeginReconcileAttempt(){if(!_retryWindowOpen)return false;if(_remainingRetryAttempts<1){CloseRetryWindow(true);return false;}_remainingRetryAttempts--;return true;}
    internal Level2MoneyCassetteReconcileDecision ObserveNativeStatus(bool saveAvailable,bool processorAvailable,string? nativeStatus)
    {
        RequestedNativeStatus=null;
        if(!_enabled){CloseRetryWindow();return Level2MoneyCassetteReconcileDecision.Disabled;}
        if(_receivedCount<1){CloseRetryWindow();return Level2MoneyCassetteReconcileDecision.NoOwnership;}
        if(!saveAvailable)return Level2MoneyCassetteReconcileDecision.SaveUnavailable;
        if(CassetteRandomizationPolicy.IsOwned(nativeStatus)){CloseRetryWindow();return Level2MoneyCassetteReconcileDecision.AlreadyOwned;}
        if(!CassetteRandomizationPolicy.IsUnearned(nativeStatus))return Level2MoneyCassetteReconcileDecision.UnknownNativeStatus;
        if(!processorAvailable)return Level2MoneyCassetteReconcileDecision.ProcessorUnavailable;
        RequestedNativeStatus=CassetteRandomizationPolicy.HaveInBag;return Level2MoneyCassetteReconcileDecision.RequestHaveInBag;
    }
    private void CloseRetryWindow(bool exhausted=false){_remainingRetryAttempts=0;_retryWindowOpen=false;_retryWindowExhausted=exhausted;}
}
