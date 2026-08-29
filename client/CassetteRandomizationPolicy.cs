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

internal enum CassetteReceiptDecision
{
    Disabled, NoOwnership, SaveUnavailable, ProcessorUnavailable, VerifiedBag,
    VerifiedDeposited, RequestHaveInBag, UnknownNativeStatus
}

internal sealed class CassetteReceiptRuntime
{
    internal const int MaxRetryAttempts = 8;
    internal static bool UseSelectedSaveChangedEventHook => false;
    private bool _enabled;
    private readonly HashSet<string> _ownedSongs = new(StringComparer.Ordinal);
    private readonly HashSet<string> _terminalSongs = new(StringComparer.Ordinal);
    private readonly HashSet<string> _satisfiedForCurrentSave = new(StringComparer.Ordinal);
    private readonly HashSet<string> _inFlight = new(StringComparer.Ordinal);
    private readonly Dictionary<string,int> _attempts = new(StringComparer.Ordinal);
    private readonly Queue<string> _pending = new();
    internal string? RequestedNativeStatus { get; private set; }
    internal int OwnedCount => _ownedSongs.Count;

    internal void Configure(bool enabled)
    {
        _enabled = enabled;
        if (!enabled) { _pending.Clear(); _inFlight.Clear(); _attempts.Clear(); }
    }

    internal bool NoteReceived(string itemName)
    {
        if (!CassetteCatalog.ByItemName.TryGetValue(itemName, out CassetteDefinition? entry)) return false;
        bool added = _ownedSongs.Add(entry.NativeSong);
        if (_enabled && added && !_terminalSongs.Contains(entry.NativeSong)) _pending.Enqueue(entry.NativeSong);
        return true;
    }

    internal bool OwnsNativeSong(string nativeSong) => _ownedSongs.Contains(nativeSong);

    internal void OnLifecyclePoint()
    {
        _pending.Clear(); _inFlight.Clear(); _attempts.Clear(); _satisfiedForCurrentSave.Clear();
        if (!_enabled) return;
        foreach (string song in _ownedSongs.Where(x => !_terminalSongs.Contains(x)).OrderBy(x => x, StringComparer.Ordinal))
            _pending.Enqueue(song);
    }

    internal bool TryBeginReconcileAttempt(out string? nativeSong)
    {
        nativeSong = null;
        while (_pending.Count > 0)
        {
            string candidate = _pending.Dequeue();
            if (_terminalSongs.Contains(candidate) || _satisfiedForCurrentSave.Contains(candidate) || _inFlight.Contains(candidate)) continue;
            int count = _attempts.TryGetValue(candidate, out int prior) ? prior : 0;
            if (count >= MaxRetryAttempts) continue;
            _attempts[candidate] = count + 1;
            _inFlight.Add(candidate); nativeSong = candidate; return true;
        }
        return false;
    }

    internal CassetteReceiptDecision ObserveNativeStatus(string nativeSong, string? nativeStatus, bool saveAvailable, bool processorAvailable)
    {
        RequestedNativeStatus = null;
        _inFlight.Remove(nativeSong);
        if (!_enabled) return CassetteReceiptDecision.Disabled;
        if (!_ownedSongs.Contains(nativeSong)) return CassetteReceiptDecision.NoOwnership;
        if (!saveAvailable) return Retry(nativeSong, CassetteReceiptDecision.SaveUnavailable);
        if (string.Equals(nativeStatus, CassetteRandomizationPolicy.HaveDeposited, StringComparison.OrdinalIgnoreCase))
        { _terminalSongs.Add(nativeSong); return CassetteReceiptDecision.VerifiedDeposited; }
        if (string.Equals(nativeStatus, CassetteRandomizationPolicy.HaveInBag, StringComparison.OrdinalIgnoreCase))
        { _satisfiedForCurrentSave.Add(nativeSong); return CassetteReceiptDecision.VerifiedBag; }
        if (!CassetteRandomizationPolicy.IsUnearned(nativeStatus)) return Retry(nativeSong, CassetteReceiptDecision.UnknownNativeStatus);
        if (!processorAvailable) return Retry(nativeSong, CassetteReceiptDecision.ProcessorUnavailable);
        RequestedNativeStatus = CassetteRandomizationPolicy.HaveInBag;
        return Retry(nativeSong, CassetteReceiptDecision.RequestHaveInBag);
    }

    private CassetteReceiptDecision Retry(string song, CassetteReceiptDecision decision)
    {
        if (_attempts.TryGetValue(song, out int count) && count < MaxRetryAttempts) _pending.Enqueue(song);
        return decision;
    }
}

internal sealed record CassetteNativeObservation(bool SaveAvailable, bool ProcessorAvailable, string? NativeStatus);
internal sealed record CassetteSchedulerTickResult(string? NativeSong, CassetteReceiptDecision? Decision, bool WriteSubmitted);

internal sealed class CassetteReceiptScheduler
{
    private readonly CassetteReceiptRuntime _runtime = new();
    internal void Configure(bool enabled) => _runtime.Configure(enabled);
    internal bool NoteReceived(string itemName) => _runtime.NoteReceived(itemName);
    internal void OnLifecyclePoint() => _runtime.OnLifecyclePoint();
    internal CassetteSchedulerTickResult Tick(Func<string,CassetteNativeObservation> observe, Func<string,bool> requestHaveInBag)
    {
        if (!_runtime.TryBeginReconcileAttempt(out string? song) || song == null)
            return new(null, null, false);
        CassetteNativeObservation native = observe(song);
        CassetteReceiptDecision decision = _runtime.ObserveNativeStatus(song, native.NativeStatus, native.SaveAvailable, native.ProcessorAvailable);
        bool submitted = decision == CassetteReceiptDecision.RequestHaveInBag && requestHaveInBag(song);
        return new(song, decision, submitted);
    }
}

// Temporary adapter retained so the former Money-only Unity integration keeps
// compiling while Plugin.cs is migrated to CassetteReceiptRuntime below.
internal enum Level2MoneyCassetteReconcileDecision { Disabled, NoOwnership, SaveUnavailable, ProcessorUnavailable, AlreadyOwned, RequestHaveInBag, UnknownNativeStatus }
internal sealed class Level2MoneyCassetteRuntime
{
    internal const int MaxRetryAttempts = CassetteReceiptRuntime.MaxRetryAttempts;
    private readonly CassetteReceiptRuntime _inner = new();
    internal string? RequestedNativeStatus => _inner.RequestedNativeStatus;
    internal bool RetryWindowExhausted { get; private set; }
    internal int RemainingRetryAttempts { get; private set; }
    internal void Configure(bool enabled) => _inner.Configure(enabled);
    internal void NoteReceivedCount(int count) { if (count > 0) _inner.NoteReceived("Money Cassette"); }
    internal void OnSaveLifecyclePoint() { _inner.OnLifecyclePoint(); RemainingRetryAttempts = MaxRetryAttempts; RetryWindowExhausted = false; }
    internal bool TryBeginReconcileAttempt()
    {
        bool result = _inner.TryBeginReconcileAttempt(out _);
        if (result) RemainingRetryAttempts = Math.Max(0, RemainingRetryAttempts - 1);
        else if (RemainingRetryAttempts == 0) RetryWindowExhausted = true;
        return result;
    }
    internal Level2MoneyCassetteReconcileDecision ObserveNativeStatus(bool saveAvailable, bool processorAvailable, string? nativeStatus) =>
        _inner.ObserveNativeStatus("I_GOT_MONEY", nativeStatus, saveAvailable, processorAvailable) switch
        {
            CassetteReceiptDecision.Disabled => Level2MoneyCassetteReconcileDecision.Disabled,
            CassetteReceiptDecision.NoOwnership => Level2MoneyCassetteReconcileDecision.NoOwnership,
            CassetteReceiptDecision.SaveUnavailable => Level2MoneyCassetteReconcileDecision.SaveUnavailable,
            CassetteReceiptDecision.ProcessorUnavailable => Level2MoneyCassetteReconcileDecision.ProcessorUnavailable,
            CassetteReceiptDecision.VerifiedBag or CassetteReceiptDecision.VerifiedDeposited => Level2MoneyCassetteReconcileDecision.AlreadyOwned,
            CassetteReceiptDecision.RequestHaveInBag => Level2MoneyCassetteReconcileDecision.RequestHaveInBag,
            _ => Level2MoneyCassetteReconcileDecision.UnknownNativeStatus
        };
}
