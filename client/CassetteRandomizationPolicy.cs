using System.Reflection;

namespace RhythmCastleAP;

internal sealed record CassetteSourceDecision(bool AllowNative, IReadOnlyList<string> SourceLocationsToQueue, IReadOnlyList<string> NativeSongsToSuppress, string Detail);
internal sealed record CassetteSlotCompatibilityResult(bool Compatible, string Detail);

internal static class CassetteSlotCompatibility
{
    internal const int Schema = 1;
    internal const int Count = 30;

    internal static CassetteSlotCompatibilityResult Validate(
        int schema,
        bool enabled,
        int count,
        IReadOnlyDictionary<string,string> items,
        IReadOnlyDictionary<string,string> sources,
        IReadOnlyDictionary<string,string> reusedLocations)
    {
        if (schema != Schema) return Fail("cassette_schema", Schema.ToString(), schema.ToString());
        if (!enabled) return Fail("full_cassette_randomization", "True", "False");
        if (count != Count) return Fail("cassette_count", Count.ToString(), count.ToString());

        foreach (CassetteDefinition entry in CassetteCatalog.All)
        {
            if (!items.TryGetValue(entry.DisplaySong, out string? item) || !string.Equals(item, entry.ItemName, StringComparison.Ordinal))
                return Fail($"cassette_items.{entry.DisplaySong}", entry.ItemName, item ?? "<missing>");
            if (!sources.TryGetValue(entry.DisplaySong, out string? source) || !string.Equals(source, entry.SourceName, StringComparison.Ordinal))
                return Fail($"cassette_sources.{entry.DisplaySong}", entry.SourceName, source ?? "<missing>");
            if (entry.ReusesExistingLocation &&
                (!reusedLocations.TryGetValue(entry.DisplaySong, out string? reused) || !string.Equals(reused, entry.SourceName, StringComparison.Ordinal)))
                return Fail($"cassette_reused_locations.{entry.DisplaySong}", entry.SourceName, reused ?? "<missing>");
        }

        if (items.Count != Count) return Fail("cassette_items.count", Count.ToString(), items.Count.ToString());
        if (sources.Count != Count) return Fail("cassette_sources.count", Count.ToString(), sources.Count.ToString());
        int expectedReused = CassetteCatalog.All.Count(x => x.ReusesExistingLocation);
        if (reusedLocations.Count != expectedReused) return Fail("cassette_reused_locations.count", expectedReused.ToString(), reusedLocations.Count.ToString());
        return new(true, "compatible");
    }

    private static CassetteSlotCompatibilityResult Fail(string key, string expected, string actual) =>
        new(false, $"{key} expected='{expected}' actual='{actual}'");
}

internal static class CassetteSlotMapReader
{
    internal static IReadOnlyDictionary<string, string> Read(object? raw)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (raw is null)
            return result;

        if (raw is System.Collections.IDictionary dictionary)
        {
            foreach (System.Collections.DictionaryEntry entry in dictionary)
                if (entry.Key?.ToString() is string mapKey && entry.Value?.ToString() is string value)
                    result[mapKey] = value;
            return result;
        }

        if (raw is System.Collections.IEnumerable entries)
        {
            foreach (object? entry in entries)
            {
                if (entry is null)
                    continue;
                Type type = entry.GetType();
                object? mapKey =
                    type.GetProperty("Key")?.GetValue(entry) ??
                    type.GetProperty("Name")?.GetValue(entry);
                object? value = type.GetProperty("Value")?.GetValue(entry);
                if (mapKey?.ToString() is string keyText && value?.ToString() is string valueText)
                    result[keyText] = valueText;
            }
        }

        return result;
    }
}

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

internal sealed record CassettePersistToken
{
    internal CassettePersistToken(long epoch, int slot, string bundle, IReadOnlyList<string> stagedSongs)
    {
        Epoch = epoch;
        Slot = slot;
        Bundle = bundle;
        StagedSongs = Array.AsReadOnly(stagedSongs.ToArray());
    }

    internal long Epoch { get; }
    internal int Slot { get; }
    internal string Bundle { get; }
    internal IReadOnlyList<string> StagedSongs { get; }
}

internal sealed class CassetteSaveEpochRuntime
{
    private readonly HashSet<string> _owned = new(StringComparer.Ordinal);
    private readonly HashSet<string> _satisfiedThisEpoch = new(StringComparer.Ordinal);
    private readonly HashSet<string> _inFlightThisEpoch = new(StringComparer.Ordinal);

    internal long Epoch { get; private set; }
    internal int? ActiveSlot { get; private set; }
    internal bool HasActiveSave => ActiveSlot.HasValue;

    internal void ActivateSave(int slot)
    {
        Epoch++;
        ActiveSlot = slot;
        _satisfiedThisEpoch.Clear();
        _inFlightThisEpoch.Clear();
    }

    internal void DeactivateSave()
    {
        ActiveSlot = null;
        _satisfiedThisEpoch.Clear();
        _inFlightThisEpoch.Clear();
    }

    internal void Receive(string nativeSong)
    {
        _owned.Add(nativeSong);
    }

    internal bool IsPending(string nativeSong) =>
        HasActiveSave && _owned.Contains(nativeSong) && !_satisfiedThisEpoch.Contains(nativeSong);

    internal void Observe(string nativeSong, string? nativeStatus)
    {
        if (!HasActiveSave || !_owned.Contains(nativeSong) || !IsSatisfiedNativeStatus(nativeStatus))
            return;

        _satisfiedThisEpoch.Add(nativeSong);
        _inFlightThisEpoch.Remove(nativeSong);
    }

    internal CassettePersistToken BeginPersist(string bundle)
    {
        if (!HasActiveSave)
            return new(Epoch, -1, bundle, Array.Empty<string>());

        string[] stagedSongs = _owned
            .Where(song => !_satisfiedThisEpoch.Contains(song) && !_inFlightThisEpoch.Contains(song))
            .OrderBy(song => song, StringComparer.Ordinal)
            .ToArray();
        foreach (string song in stagedSongs)
            _inFlightThisEpoch.Add(song);
        return new(Epoch, ActiveSlot!.Value, bundle, stagedSongs);
    }

    internal void CompletePersist(CassettePersistToken token, Func<string, string?> readStatus)
    {
        if (!HasActiveSave || token.Epoch != Epoch || token.Slot != ActiveSlot)
            return;

        foreach (string song in token.StagedSongs.Distinct(StringComparer.Ordinal))
        {
            string? status;
            try
            {
                status = readStatus(song);
            }
            catch
            {
                status = null;
            }

            _inFlightThisEpoch.Remove(song);
            if (_owned.Contains(song) && IsSatisfiedNativeStatus(status))
                _satisfiedThisEpoch.Add(song);
        }
    }

    private static bool IsSatisfiedNativeStatus(string? status) =>
        string.Equals(status, CassetteRandomizationPolicy.HaveInBag, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, CassetteRandomizationPolicy.HaveDeposited, StringComparison.OrdinalIgnoreCase);
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
        _pending.Clear(); _inFlight.Clear(); _attempts.Clear();
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
            if (_terminalSongs.Contains(candidate) || _inFlight.Contains(candidate)) continue;
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
        { _terminalSongs.Add(nativeSong); return CassetteReceiptDecision.VerifiedBag; }
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

internal sealed record CassetteNativeObservation(
    bool SaveAvailable,
    bool ProcessorAvailable,
    string? NativeStatus,
    bool AuthoritativeStateAvailable = false,
    string? AuthoritativeNativeStatus = null);

internal static class CassetteAuthoritativeStateReader
{
    internal static bool TryRead(
        object? processor,
        Type? songType,
        string nativeSong,
        out string? status,
        out string detail)
    {
        status = null;
        detail = string.Empty;
        try
        {
            if (processor == null || songType == null || !songType.IsEnum)
            {
                detail = "processor or song enum unavailable";
                return false;
            }

            object song = Enum.Parse(songType, nativeSong, ignoreCase: false);
            MethodInfo? obtainState = processor.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(method =>
                    string.Equals(method.Name, "ObtainState", StringComparison.Ordinal) &&
                    method.GetParameters().Length == 0);
            object? state = obtainState?.Invoke(processor, null);
            if (state == null)
            {
                detail = "PlayerSaveRequestProcessor.ObtainState unavailable";
                return false;
            }

            MethodInfo? readStatus = state.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(method =>
                    string.Equals(method.Name, "GetCassetteStatusForSong", StringComparison.Ordinal) &&
                    method.GetParameters().Length == 1 &&
                    method.GetParameters()[0].ParameterType == songType);
            if (readStatus == null)
            {
                detail = "PlayerSaveFileState.GetCassetteStatusForSong unavailable";
                return false;
            }

            status = readStatus.Invoke(state, new[] { song })?.ToString();
            detail = $"processor-selected save returned {status ?? "<null>"}";
            return !string.IsNullOrWhiteSpace(status);
        }
        catch (Exception ex)
        {
            detail = ex.GetBaseException().Message;
            return false;
        }
    }
}
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
        bool useAuthoritativeState = native.ProcessorAvailable;
        CassetteReceiptDecision decision = _runtime.ObserveNativeStatus(
            song,
            useAuthoritativeState ? native.AuthoritativeNativeStatus : native.NativeStatus,
            useAuthoritativeState ? native.AuthoritativeStateAvailable : native.SaveAvailable,
            native.ProcessorAvailable);
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
