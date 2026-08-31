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

internal enum CassetteSaveBoundarySignalKind
{
    Selection,
    Creation,
    Build,
}

internal readonly record struct CassetteSaveActivation(
    int Slot,
    long Pointer,
    long Generation,
    CassetteSaveBoundarySignalKind Reason,
    bool IncludesBuild);

internal readonly record struct CassetteProcessorSaveIdentitySnapshot(
    long Generation,
    int? ExpectedSlot,
    bool Pending,
    object? Processor)
{
    internal bool IsCurrent(CassetteProcessorSaveIdentitySnapshot current) =>
        Pending && current.Pending && Generation == current.Generation &&
        ExpectedSlot == current.ExpectedSlot && ReferenceEquals(Processor, current.Processor);
}

internal sealed class CassetteProcessorSaveIdentityStabilizer
{
    private long _generation;
    private int? _expectedSlot;
    private object? _processor;
    private long _candidatePointer;
    private int _matchingObservations;
    private CassetteSaveBoundarySignalKind _reason;
    private bool _includesBuild;
    private int? _activatedSlot;
    private long _activatedPointer;

    internal long SuspendUnresolved()
    {
        _generation++;
        _expectedSlot = null;
        _processor = null;
        ResetCandidate();
        Pending = true;
        return _generation;
    }

    internal long Signal(int expectedSlot, CassetteSaveBoundarySignalKind kind, object? processor)
    {
        bool coalescesSameSlot = Pending && _expectedSlot == expectedSlot;
        bool retainedBuild = coalescesSameSlot && _includesBuild;
        _generation++;
        _expectedSlot = expectedSlot;
        _processor = processor;
        _reason = kind;
        _includesBuild = retainedBuild || kind == CassetteSaveBoundarySignalKind.Build;
        ResetCandidate();
        Pending = true;
        return _generation;
    }

    internal bool Pending { get; private set; }

    internal bool CaptureProcessor(object processor)
    {
        if (!Pending || !_expectedSlot.HasValue) return false;
        if (ReferenceEquals(_processor, processor)) return false;
        _processor = processor;
        _candidatePointer = 0;
        _matchingObservations = 0;
        return true;
    }

    internal CassetteProcessorSaveIdentitySnapshot Capture() =>
        new(_generation, _expectedSlot, Pending, _processor);

    internal CassetteSaveActivation? Observe(
        long generation,
        object? processor,
        bool readable,
        long pointer,
        string stage)
    {
        if (!Pending) return null;
        if (generation != _generation ||
            processor == null || !ReferenceEquals(processor, _processor) ||
            !readable || pointer == 0 || !_expectedSlot.HasValue)
        {
            ResetCandidate();
            return null;
        }

        if (_candidatePointer != pointer)
        {
            _candidatePointer = pointer;
            _matchingObservations = 1;
            return null;
        }

        _matchingObservations++;
        if (_matchingObservations < 2) return null;

        Pending = false;
        bool identityChanged = _activatedSlot != _expectedSlot.Value || _activatedPointer != pointer;
        bool requiresNewEpoch = identityChanged || _reason == CassetteSaveBoundarySignalKind.Creation || _includesBuild;
        if (!requiresNewEpoch) return null;
        _activatedSlot = _expectedSlot.Value;
        _activatedPointer = pointer;
        return new CassetteSaveActivation(
            _expectedSlot.Value,
            pointer,
            _generation,
            _reason,
            _includesBuild);
    }

    internal void Reset()
    {
        _expectedSlot = null;
        _processor = null;
        _reason = CassetteSaveBoundarySignalKind.Selection;
        _includesBuild = false;
        _activatedSlot = null;
        _activatedPointer = 0;
        _candidatePointer = 0;
        _matchingObservations = 0;
        Pending = false;
    }

    private void ResetCandidate()
    {
        _candidatePointer = 0;
        _matchingObservations = 0;
    }
}

internal readonly record struct CassetteRegularSavePointerJoinSnapshot(
    long Generation,
    bool Pending,
    object? SaveDataProcessor,
    object? PlayerSaveProcessor)
{
    internal bool Ready => Pending && SaveDataProcessor != null && PlayerSaveProcessor != null;
}

internal sealed class CassetteRegularSavePointerJoinProbe
{
    private long _generation;
    private object? _saveDataProcessor;
    private object? _playerSaveProcessor;

    internal bool Pending { get; private set; }

    internal long Queue(object saveDataProcessor)
    {
        _generation++;
        _saveDataProcessor = saveDataProcessor;
        _playerSaveProcessor = null;
        Pending = true;
        return _generation;
    }

    internal bool CapturePlayerProcessor(object? processor)
    {
        if (!Pending || processor == null) return false;
        _playerSaveProcessor = processor;
        return true;
    }

    internal CassetteRegularSavePointerJoinSnapshot Capture() =>
        new(_generation, Pending, _saveDataProcessor, _playerSaveProcessor);

    internal bool TryConsume(CassetteRegularSavePointerJoinSnapshot snapshot)
    {
        if (!snapshot.Ready || !Pending || snapshot.Generation != _generation ||
            !ReferenceEquals(snapshot.SaveDataProcessor, _saveDataProcessor) ||
            !ReferenceEquals(snapshot.PlayerSaveProcessor, _playerSaveProcessor))
            return false;
        Pending = false;
        _saveDataProcessor = null;
        _playerSaveProcessor = null;
        return true;
    }

    internal void Cancel()
    {
        _generation++;
        Pending = false;
        _saveDataProcessor = null;
        _playerSaveProcessor = null;
    }
}

internal sealed class CassetteDiagnosticSignatureDeduplicator
{
    private string? _lastSignature;

    internal bool ShouldLog(string signature)
    {
        if (string.Equals(_lastSignature, signature, StringComparison.Ordinal)) return false;
        _lastSignature = signature;
        return true;
    }

    internal void Reset() => _lastSignature = null;
}

internal readonly record struct CassettePublicWriteState(
    bool HasChanges,
    bool RequiresWriteToDisk,
    double? LastSuccessTime,
    double? LastFailureTime,
    string? FailureReason);

internal enum CassetteDiskCommitOutcome { None, Pending, Success, Failure, Timeout, Cancelled }

internal sealed class CassetteDiskCommitRuntime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private readonly HashSet<string> _songs = new(StringComparer.Ordinal);
    private readonly HashSet<string> _activeSongs = new(StringComparer.Ordinal);
    private bool _active;
    private bool _blocked;
    private long _revision;
    private long _activeRevision;
    private long _epoch;
    private int _slot;
    private long _pointer;
    private double? _baselineSuccess;
    private double? _baselineFailure;
    private string? _baselineFailureReason;
    private TimeSpan _elapsed;

    internal bool HasWork => _active || (_songs.Count > 0 && !_blocked);
    internal bool Active => _active;
    internal IReadOnlyList<string> Songs => (_active ? _activeSongs : _songs).OrderBy(x => x, StringComparer.Ordinal).ToArray();
    internal void Stage(string song)
    {
        if (!_songs.Add(song)) return;
        _revision++;
        _blocked = false;
    }
    internal void Reset()
    {
        _songs.Clear(); _activeSongs.Clear(); _active = false; _blocked = false;
        _revision = 0; _activeRevision = 0; _elapsed = TimeSpan.Zero;
    }

    internal bool TryBegin(long epoch, int slot, long pointer, CassettePublicWriteState state)
    {
        if (_active || _songs.Count == 0 || pointer == 0) return false;
        _active = true; _epoch = epoch; _slot = slot; _pointer = pointer;
        _activeSongs.Clear(); _activeSongs.UnionWith(_songs); _activeRevision = _revision;
        _baselineSuccess = state.LastSuccessTime; _baselineFailure = state.LastFailureTime;
        _baselineFailureReason = state.FailureReason;
        _elapsed = TimeSpan.Zero;
        return true;
    }

    internal CassetteDiskCommitOutcome Observe(
        long epoch, int slot, long pointer, CassettePublicWriteState state,
        bool statusesRetained, TimeSpan elapsed)
    {
        CassetteDiskCommitOutcome boundary = CheckBoundary(epoch, slot, pointer, statusesRetained);
        if (boundary is not CassetteDiskCommitOutcome.Pending) return boundary;
        bool failureAdvanced = state.LastFailureTime.HasValue &&
            (!_baselineFailure.HasValue || state.LastFailureTime.Value > _baselineFailure.Value);
        bool failureReasonChanged = !string.IsNullOrWhiteSpace(state.FailureReason) &&
            !string.Equals(state.FailureReason, _baselineFailureReason, StringComparison.Ordinal);
        if (failureAdvanced || failureReasonChanged)
            return FinishFailure(CassetteDiskCommitOutcome.Failure);
        bool successAdvanced = state.LastSuccessTime.HasValue &&
            (!_baselineSuccess.HasValue || state.LastSuccessTime.Value > _baselineSuccess.Value);
        if (!state.RequiresWriteToDisk && successAdvanced)
        {
            _active = false;
            _songs.ExceptWith(_activeSongs);
            _activeSongs.Clear();
            _blocked = false;
            return CassetteDiskCommitOutcome.Success;
        }
        return AdvanceTimeout(elapsed);
    }

    internal CassetteDiskCommitOutcome ObserveUnavailable(
        long epoch, int slot, long pointer, bool statusesRetained, TimeSpan elapsed)
    {
        CassetteDiskCommitOutcome boundary = CheckBoundary(epoch, slot, pointer, statusesRetained);
        return boundary is CassetteDiskCommitOutcome.Pending ? AdvanceTimeout(elapsed) : boundary;
    }

    private CassetteDiskCommitOutcome CheckBoundary(long epoch, int slot, long pointer, bool statusesRetained)
    {
        if (!_active) return CassetteDiskCommitOutcome.None;
        if (epoch != _epoch || slot != _slot || pointer != _pointer)
            return FinishFailure(CassetteDiskCommitOutcome.Cancelled);
        if (!statusesRetained) return FinishFailure(CassetteDiskCommitOutcome.Failure);
        return CassetteDiskCommitOutcome.Pending;
    }

    private CassetteDiskCommitOutcome AdvanceTimeout(TimeSpan elapsed)
    {
        if (elapsed > TimeSpan.Zero) _elapsed += elapsed;
        if (_elapsed >= Timeout) return FinishFailure(CassetteDiskCommitOutcome.Timeout);
        return CassetteDiskCommitOutcome.Pending;
    }

    private CassetteDiskCommitOutcome FinishFailure(CassetteDiskCommitOutcome outcome)
    {
        _active = false;
        _activeSongs.Clear();
        _blocked = _revision == _activeRevision;
        return outcome;
    }
}

internal sealed class CassetteSaveEpochRuntime
{
    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(3),
    };
    private readonly HashSet<string> _owned = new(StringComparer.Ordinal);
    private readonly HashSet<string> _satisfiedThisEpoch = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _attemptsThisEpoch = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TimeSpan> _verificationElapsed = new(StringComparer.Ordinal);

    internal long Epoch { get; private set; }
    internal int? ActiveSlot { get; private set; }
    internal bool HasActiveSave => ActiveSlot.HasValue;

    internal void ActivateSave(int slot)
    {
        Epoch++;
        ActiveSlot = slot;
        _satisfiedThisEpoch.Clear();
        _attemptsThisEpoch.Clear();
        _verificationElapsed.Clear();
    }

    internal void DeactivateSave()
    {
        ActiveSlot = null;
        _satisfiedThisEpoch.Clear();
        _attemptsThisEpoch.Clear();
        _verificationElapsed.Clear();
    }

    internal void Receive(string nativeSong)
    {
        _owned.Add(nativeSong);
    }

    internal bool IsPending(string nativeSong) =>
        HasActiveSave && _owned.Contains(nativeSong) && !_satisfiedThisEpoch.Contains(nativeSong);

    internal IReadOnlyList<string> PendingSongs => HasActiveSave
        ? _owned.Where(IsPending).OrderBy(song => song, StringComparer.Ordinal).ToArray()
        : Array.Empty<string>();

    internal bool CanSubmit(string nativeSong, string? nativeStatus, bool processorAvailable) =>
        IsPending(nativeSong) &&
        processorAvailable &&
        CassetteRandomizationPolicy.IsUnearned(nativeStatus) &&
        !_verificationElapsed.ContainsKey(nativeSong) &&
        (!_attemptsThisEpoch.TryGetValue(nativeSong, out int attempts) || attempts < RetryDelays.Length);

    internal void RecordSubmission(string nativeSong)
    {
        if (!IsPending(nativeSong)) return;
        _attemptsThisEpoch[nativeSong] = _attemptsThisEpoch.TryGetValue(nativeSong, out int attempts) ? attempts + 1 : 1;
        _verificationElapsed[nativeSong] = TimeSpan.Zero;
    }

    internal void RecordSubmissionFailure(string nativeSong) => _verificationElapsed.Remove(nativeSong);

    internal IReadOnlyList<string> Tick(TimeSpan elapsed)
    {
        if (!HasActiveSave || elapsed <= TimeSpan.Zero) return Array.Empty<string>();
        var ready = new List<string>();
        foreach (string song in _verificationElapsed.Keys.ToArray())
        {
            TimeSpan total = _verificationElapsed[song] + elapsed;
            _verificationElapsed[song] = total;
            int attempt = _attemptsThisEpoch.TryGetValue(song, out int count) ? count : 1;
            TimeSpan delay = RetryDelays[Math.Clamp(attempt - 1, 0, RetryDelays.Length - 1)];
            if (total >= delay) ready.Add(song);
        }
        return ready;
    }

    internal void Observe(string nativeSong, string? nativeStatus)
    {
        if (!HasActiveSave || !_owned.Contains(nativeSong) || !IsSatisfiedNativeStatus(nativeStatus))
            return;

        _satisfiedThisEpoch.Add(nativeSong);
        _verificationElapsed.Remove(nativeSong);
    }

    internal bool RecordVerification(string nativeSong, string? nativeStatus)
    {
        if (!IsPending(nativeSong)) return false;
        _verificationElapsed.Remove(nativeSong);
        Observe(nativeSong, nativeStatus);
        return string.Equals(nativeStatus, CassetteRandomizationPolicy.HaveInBag, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSatisfiedNativeStatus(string? status) =>
        string.Equals(status, CassetteRandomizationPolicy.HaveInBag, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, CassetteRandomizationPolicy.HaveDeposited, StringComparison.OrdinalIgnoreCase);
}

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
