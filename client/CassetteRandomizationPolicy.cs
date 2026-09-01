using System.Reflection;
using System.Globalization;

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

internal sealed class CassetteBoundedDiagnosticSignatureDeduplicator
{
    private readonly int _capacity;
    private readonly HashSet<string> _signatures = new(StringComparer.Ordinal);

    internal CassetteBoundedDiagnosticSignatureDeduplicator(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    internal bool ShouldLog(string signature)
    {
        if (_signatures.Contains(signature) || _signatures.Count >= _capacity) return false;
        _signatures.Add(signature);
        return true;
    }
}

internal readonly record struct CassettePublicWriteState(
    bool HasChanges,
    bool RequiresWriteToDisk,
    double? LastSuccessTime,
    double? LastFailureTime,
    string? FailureReason);

internal readonly record struct CassettePublicWriteDiagnosticState(
    CassettePublicWriteState WriteState,
    double? CurrentGameTime,
    bool HasUnstagedChanges,
    int RedundancyBundleIndex,
    int RedundancyBundleRevision,
    long StatePointer,
    IReadOnlyDictionary<string, string> Statuses);

internal enum CassetteDiskCommitOutcome { None, Pending, Success, Failure, Timeout, HardTimeout, Cancelled }

internal enum CassetteDiskCommitEventOutcome { Ignored, SuccessWake, Failure }

internal readonly record struct CassetteDiskCommitAttempt(
    long Id,
    long Generation,
    long Epoch,
    int Slot,
    long Pointer);

internal enum CassetteDiskCommitDiagnosticPhase
{
    Pre,
    Post,
    Event,
    StillPending,
    HardTimeout,
    PreparedEventIgnored,
    Failure,
    LifecycleBefore,
    LifecycleAfter,
    PreTarget,
    PostTarget,
}

internal enum CassetteDiskCommitFailureKind
{
    None,
    IdentityUnreadable,
    PointerMismatch,
    StatusUnreadable,
    StatusRegression,
    FailureTimeAdvanced,
    FailureReasonChanged,
    EventFailure,
    HardTimeout,
    SubmissionFailed,
}

internal readonly record struct CassetteDiskCommitFailureDiagnostic(
    CassetteDiskCommitFailureKind Kind,
    string Detail)
{
    internal static CassetteDiskCommitFailureDiagnostic IdentityUnreadable(string stage) =>
        new(CassetteDiskCommitFailureKind.IdentityUnreadable, $"stage='{stage}'");

    internal static CassetteDiskCommitFailureDiagnostic PointerMismatch(long expected, long observed) =>
        new(CassetteDiskCommitFailureKind.PointerMismatch, $"expected=0x{expected:X} observed=0x{observed:X}");

    internal static CassetteDiskCommitFailureDiagnostic StatusUnreadable(string song, string stage) =>
        new(CassetteDiskCommitFailureKind.StatusUnreadable, $"song='{song}' stage='{stage}'");

    internal static CassetteDiskCommitFailureDiagnostic StatusRegression(string song, string? status) =>
        new(CassetteDiskCommitFailureKind.StatusRegression, $"song='{song}' status='{status ?? "<null>"}'");

    internal static CassetteDiskCommitFailureDiagnostic FailureTimeAdvanced(double? baseline, double current) =>
        new(
            CassetteDiskCommitFailureKind.FailureTimeAdvanced,
            $"baselineBits={FormatBits(baseline)} currentBits={FormatBits(current)}");

    internal static CassetteDiskCommitFailureDiagnostic FailureReasonChanged(string? baseline, string current) =>
        new(
            CassetteDiskCommitFailureKind.FailureReasonChanged,
            $"baseline='{baseline ?? "<null>"}' current='{current}'");

    internal static CassetteDiskCommitFailureDiagnostic EventFailure() =>
        new(CassetteDiskCommitFailureKind.EventFailure, "same-slot public completion event reported failure");

    internal static CassetteDiskCommitFailureDiagnostic HardTimeout() =>
        new(CassetteDiskCommitFailureKind.HardTimeout, "active-update watchdog reached 130 seconds");

    internal static CassetteDiskCommitFailureDiagnostic SubmissionFailed(string detail) =>
        new(CassetteDiskCommitFailureKind.SubmissionFailed, $"stage='{detail}'");

    private static string FormatBits(double? value) => value.HasValue
        ? $"0x{BitConverter.DoubleToInt64Bits(value.Value):X16}"
        : "<null>";
}

internal readonly record struct CassetteDiskCommitDiagnosticContext(
    CassetteDiskCommitAttempt Attempt,
    CassetteDiskCommitDiagnosticPhase Phase,
    int EventOrdinal,
    TimeSpan Elapsed,
    IReadOnlyList<string> ActiveSongs,
    IReadOnlyList<string> QueuedSongs,
    CassetteDiskCommitFailureKind FailureKind = CassetteDiskCommitFailureKind.None,
    string FailureDetail = "");

internal static class CassetteDiskCommitDiagnosticFormatter
{
    internal static string FormatNullableDouble(double? value)
    {
        if (!value.HasValue) return "present=False value=<null> bits=<null>";
        return $"present=True value={value.Value.ToString("R", CultureInfo.InvariantCulture)} bits=0x{BitConverter.DoubleToInt64Bits(value.Value):X16}";
    }
}

internal enum CassetteBundleRoutingBoundary
{
    RecordSong,
    PersistBundle,
    PersistAllBundles,
    DiscardAllUnstaged,
}

internal enum CassetteBundleRoutingPhase { Entry, Exit }

internal readonly record struct CassetteBundleRoutingDiagnosticToken(
    long TokenId,
    CassetteDiskCommitAttempt Attempt,
    CassetteBundleRoutingBoundary Boundary,
    CassetteBundleRoutingPhase Phase,
    string SongKey,
    long ExpectedPointer,
    IReadOnlyList<string> Songs,
    bool OriginalAllowed = true);

internal sealed class CassetteBundleRoutingDiagnosticRuntime
{
    private readonly HashSet<string> _claimedEntries = new(StringComparer.Ordinal);
    private readonly HashSet<long> _openTokens = new();
    private readonly Dictionary<CassetteDiskCommitAttempt, HashSet<string>> _relevantSongs = new();
    private CassetteDiskCommitAttempt? _candidateAttempt;
    private long _tokenCounter;

    internal void EstablishCandidate(CassetteDiskCommitAttempt attempt)
    {
        if (attempt.Id > 0 && attempt.Pointer != 0) _candidateAttempt = attempt;
    }

    internal void RegisterRelevantSong(
        CassetteDiskCommitAttempt attempt,
        string song,
        bool establishCandidate)
    {
        if (attempt.Id <= 0 || attempt.Pointer == 0 || string.IsNullOrWhiteSpace(song)) return;
        if (!_relevantSongs.TryGetValue(attempt, out HashSet<string>? songs))
        {
            songs = new HashSet<string>(StringComparer.Ordinal);
            _relevantSongs[attempt] = songs;
        }
        songs.Add(song);
        if (establishCandidate) EstablishCandidate(attempt);
    }

    internal bool TryResolveGlobalAttempt(
        CassetteDiskCommitAttempt? activeAttempt,
        CassetteDiskCommitAttempt previewAttempt,
        out CassetteDiskCommitAttempt attempt)
    {
        if (activeAttempt.HasValue)
        {
            attempt = activeAttempt.Value;
            return true;
        }
        if (_candidateAttempt.HasValue && _candidateAttempt.Value == previewAttempt)
        {
            attempt = previewAttempt;
            return true;
        }
        attempt = default;
        return false;
    }

    internal IReadOnlyList<string> GetRelevantSongs(CassetteDiskCommitAttempt attempt) =>
        _relevantSongs.TryGetValue(attempt, out HashSet<string>? songs)
            ? songs.OrderBy(song => song, StringComparer.Ordinal).ToArray()
            : Array.Empty<string>();

    internal bool TryBegin(
        CassetteDiskCommitAttempt attempt,
        CassetteBundleRoutingBoundary boundary,
        string songKey,
        IReadOnlyList<string> songs,
        out CassetteBundleRoutingDiagnosticToken token)
    {
        token = default;
        if (attempt.Id <= 0 || attempt.Pointer == 0) return false;
        string normalizedSongKey = string.IsNullOrWhiteSpace(songKey) ? "<none>" : songKey;
        string key = $"{attempt.Id}|{attempt.Generation}|{attempt.Epoch}|{attempt.Slot}|{attempt.Pointer}|{boundary}|{normalizedSongKey}";
        if (!_claimedEntries.Add(key)) return false;
        long tokenId = ++_tokenCounter;
        _openTokens.Add(tokenId);
        token = new(
            tokenId,
            attempt,
            boundary,
            CassetteBundleRoutingPhase.Entry,
            normalizedSongKey,
            attempt.Pointer,
            songs.Where(song => !string.IsNullOrWhiteSpace(song))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(song => song, StringComparer.Ordinal)
                .ToArray());
        return true;
    }

    internal bool TryComplete(
        CassetteBundleRoutingDiagnosticToken token,
        out CassetteBundleRoutingDiagnosticToken completed)
    {
        completed = default;
        if (token.TokenId <= 0 || token.Phase is not CassetteBundleRoutingPhase.Entry ||
            !_openTokens.Remove(token.TokenId))
            return false;
        completed = token with { Phase = CassetteBundleRoutingPhase.Exit };
        return true;
    }

    internal void Reset()
    {
        _claimedEntries.Clear();
        _openTokens.Clear();
        _relevantSongs.Clear();
        _candidateAttempt = null;
    }
}

internal sealed class CassetteDiskCommitRuntime
{
    private sealed class TerminalDiagnosticTombstone
    {
        internal CassetteDiskCommitAttempt Attempt { get; init; }
        internal CassetteDiskCommitOutcome Outcome { get; init; }
        internal CassetteDiskCommitFailureDiagnostic Failure { get; init; }
        internal int EventOrdinal { get; init; }
        internal TimeSpan Elapsed { get; init; }
        internal string[] ActiveSongs { get; init; } = Array.Empty<string>();
        internal string[] QueuedSongs { get; init; } = Array.Empty<string>();
        internal bool FailureClaimed { get; set; }
        internal bool HardTimeoutClaimed { get; set; }
        internal bool LateEventClaimed { get; set; }
        internal bool LateEventSuperseded { get; set; }
        internal bool LifecycleBeforeClaimed { get; set; }
        internal bool LifecycleAfterClaimed { get; set; }
        internal long LifecycleToken { get; set; }
        internal int LifecycleRequestSlot { get; set; }
    }

    private static readonly TimeSpan AcquisitionTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaximumActiveUpdateElapsed = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan StillPendingAfter = TimeSpan.FromSeconds(11);
    private static readonly TimeSpan HardTimeout = TimeSpan.FromSeconds(130);
    private readonly HashSet<string> _songs = new(StringComparer.Ordinal);
    private readonly HashSet<string> _activeSongs = new(StringComparer.Ordinal);
    private bool _active;
    private bool _submitted;
    private bool _blocked;
    private bool _indeterminate;
    private bool _successEventObserved;
    private bool _stillPendingReported;
    private bool _rejectedEventReported;
    private int _diagnosticsClaimed;
    private int _eventOrdinal;
    private string[] _diagnosticAttemptSongs = Array.Empty<string>();
    private long _attemptCounter;
    private long _lifecycleTokenCounter;
    private CassetteDiskCommitAttempt _attempt;
    private TerminalDiagnosticTombstone? _terminalTombstone;
    private long _revision;
    private long _activeRevision;
    private long _generation;
    private long _epoch;
    private int _slot;
    private long _pointer;
    private double? _baselineSuccess;
    private double? _baselineFailure;
    private string? _baselineFailureReason;
    private TimeSpan _elapsed;
    private bool _acquiring;
    private bool _acquisitionDeferredReported;
    private long _acquisitionRevision;
    private long _acquisitionEpoch;
    private int _acquisitionSlot;
    private long _acquisitionPointer;
    private TimeSpan _acquisitionElapsed;

    internal bool HasWork => _active || (_songs.Count > 0 && !_blocked);
    internal bool Active => _submitted;
    internal IReadOnlyList<string> Songs => (_active ? _activeSongs : _songs).OrderBy(x => x, StringComparer.Ordinal).ToArray();
    internal CassetteDiskCommitAttempt PreviewRoutingDiagnosticAttempt(
        long generation,
        long epoch,
        int slot,
        long pointer) =>
        _active && _generation == generation && _epoch == epoch && _slot == slot && _pointer == pointer
            ? _attempt
            : new CassetteDiskCommitAttempt(_attemptCounter + 1, generation, epoch, slot, pointer);
    internal static TimeSpan CapActiveUpdateElapsed(TimeSpan elapsed) =>
        elapsed <= TimeSpan.Zero ? TimeSpan.Zero :
        elapsed > MaximumActiveUpdateElapsed ? MaximumActiveUpdateElapsed : elapsed;
    internal void Stage(string song)
    {
        if (!_songs.Add(song)) return;
        _revision++;
        if (!_indeterminate) _blocked = false;
    }
    internal void Reset()
    {
        _songs.Clear(); _activeSongs.Clear(); _active = false; _submitted = false; _blocked = false; _indeterminate = false;
        _successEventObserved = false; _stillPendingReported = false; _rejectedEventReported = false;
        _diagnosticsClaimed = 0; _eventOrdinal = 0; _diagnosticAttemptSongs = Array.Empty<string>();
        _attempt = default;
        _terminalTombstone = null;
        _revision = 0; _activeRevision = 0; _elapsed = TimeSpan.Zero;
        ClearAcquisition();
    }

    internal bool TryPrepare(
        long generation, long epoch, int slot, long pointer, CassettePublicWriteState state,
        out CassetteDiskCommitAttempt attempt)
    {
        attempt = default;
        if (_active || _blocked || _songs.Count == 0 || pointer == 0) return false;
        ClearAcquisition();
        long attemptId = ++_attemptCounter;
        if (_terminalTombstone != null) _terminalTombstone.LateEventSuperseded = true;
        _attempt = new CassetteDiskCommitAttempt(attemptId, generation, epoch, slot, pointer);
        _active = true; _generation = generation; _epoch = epoch; _slot = slot; _pointer = pointer;
        _activeSongs.Clear(); _activeSongs.UnionWith(_songs); _activeRevision = _revision;
        _diagnosticAttemptSongs = _activeSongs.OrderBy(song => song, StringComparer.Ordinal).ToArray();
        _baselineSuccess = state.LastSuccessTime; _baselineFailure = state.LastFailureTime;
        _baselineFailureReason = state.FailureReason;
        _elapsed = TimeSpan.Zero; _submitted = false; _successEventObserved = false;
        _stillPendingReported = false; _rejectedEventReported = false;
        _diagnosticsClaimed = 0; _eventOrdinal = 0;
        attempt = _attempt;
        return true;
    }

    internal bool MarkSubmitted(CassetteDiskCommitAttempt attempt, CassettePublicWriteState postSubmitState)
    {
        if (!_active || _submitted || attempt != _attempt) return false;
        _baselineSuccess = postSubmitState.LastSuccessTime;
        _baselineFailure = postSubmitState.LastFailureTime;
        _baselineFailureReason = postSubmitState.FailureReason;
        _submitted = true;
        return true;
    }

    internal CassetteDiskCommitOutcome MarkSubmissionIndeterminate(
        CassetteDiskCommitAttempt attempt,
        CassetteDiskCommitFailureDiagnostic failure = default)
    {
        if (!_active || _submitted || attempt != _attempt) return CassetteDiskCommitOutcome.None;
        _indeterminate = true;
        if (failure.Kind is CassetteDiskCommitFailureKind.None)
            failure = CassetteDiskCommitFailureDiagnostic.SubmissionFailed("post-submit baseline indeterminate");
        return FinishFailure(
            CassetteDiskCommitOutcome.HardTimeout,
            failure);
    }

    internal bool TryGetSubmittedAttempt(out CassetteDiskCommitAttempt attempt)
    {
        attempt = _attempt;
        return _active && _submitted;
    }

    internal bool TryGetPreparedAttempt(out CassetteDiskCommitAttempt attempt)
    {
        attempt = _attempt;
        return _active && !_submitted;
    }

    internal bool TryClaimDiagnostic(
        CassetteDiskCommitAttempt attempt,
        CassetteDiskCommitDiagnosticPhase phase,
        out CassetteDiskCommitDiagnosticContext context)
    {
        context = default;
        if (phase is CassetteDiskCommitDiagnosticPhase.Failure or CassetteDiskCommitDiagnosticPhase.HardTimeout)
            return TryClaimTerminalDiagnostic(attempt, phase, out context);
        if (attempt != _attempt || !CanClaimDiagnostic(phase)) return false;
        int flag = 1 << (int)phase;
        if ((_diagnosticsClaimed & flag) != 0) return false;
        _diagnosticsClaimed |= flag;
        if (phase is CassetteDiskCommitDiagnosticPhase.PreparedEventIgnored) _eventOrdinal++;
        string[] activeSongs = _diagnosticAttemptSongs.ToArray();
        string[] queuedSongs = _songs.Except(_diagnosticAttemptSongs, StringComparer.Ordinal)
            .OrderBy(song => song, StringComparer.Ordinal).ToArray();
        context = new(attempt, phase, _eventOrdinal, _elapsed, activeSongs, queuedSongs);
        return true;
    }

    private bool TryClaimTerminalDiagnostic(
        CassetteDiskCommitAttempt attempt,
        CassetteDiskCommitDiagnosticPhase phase,
        out CassetteDiskCommitDiagnosticContext context)
    {
        context = default;
        TerminalDiagnosticTombstone? tombstone = _terminalTombstone;
        if (tombstone == null || tombstone.Attempt != attempt) return false;
        if (phase is CassetteDiskCommitDiagnosticPhase.Failure)
        {
            bool failureSnapshot = tombstone.Outcome is CassetteDiskCommitOutcome.Failure ||
                (tombstone.Outcome is CassetteDiskCommitOutcome.HardTimeout &&
                 tombstone.Failure.Kind is not CassetteDiskCommitFailureKind.HardTimeout);
            if (!failureSnapshot || tombstone.FailureClaimed) return false;
            tombstone.FailureClaimed = true;
        }
        else
        {
            if (tombstone.Outcome is not CassetteDiskCommitOutcome.HardTimeout ||
                tombstone.Failure.Kind is not CassetteDiskCommitFailureKind.HardTimeout ||
                tombstone.HardTimeoutClaimed)
                return false;
            tombstone.HardTimeoutClaimed = true;
        }
        context = CreateTombstoneContext(tombstone, phase);
        return true;
    }

    internal bool TryGetLastTerminalAttempt(out CassetteDiskCommitAttempt attempt)
    {
        attempt = _terminalTombstone?.Attempt ?? default;
        return _terminalTombstone != null;
    }

    internal bool TryClaimLateEvent(CassetteDiskCommitAttempt attempt, int eventSlot)
    {
        TerminalDiagnosticTombstone? tombstone = _terminalTombstone;
        if (_active || tombstone == null || tombstone.Attempt != attempt || tombstone.LateEventSuperseded ||
            eventSlot != attempt.Slot || tombstone.LateEventClaimed)
            return false;
        tombstone.LateEventClaimed = true;
        return true;
    }

    internal bool TryBeginSaveStateLifecycleDiagnostic(
        int requestSlot,
        out long token,
        out CassetteDiskCommitDiagnosticContext context)
    {
        token = 0;
        context = default;
        TerminalDiagnosticTombstone? tombstone = _terminalTombstone;
        if (tombstone == null || tombstone.LifecycleBeforeClaimed || requestSlot != tombstone.Attempt.Slot)
            return false;
        tombstone.LifecycleBeforeClaimed = true;
        tombstone.LifecycleToken = ++_lifecycleTokenCounter;
        tombstone.LifecycleRequestSlot = requestSlot;
        token = tombstone.LifecycleToken;
        context = CreateTombstoneContext(tombstone, CassetteDiskCommitDiagnosticPhase.LifecycleBefore);
        return true;
    }

    internal bool TryCompleteSaveStateLifecycleDiagnostic(
        long token,
        int requestSlot,
        out CassetteDiskCommitDiagnosticContext context)
    {
        context = default;
        TerminalDiagnosticTombstone? tombstone = _terminalTombstone;
        if (token == 0 || tombstone == null || tombstone.LifecycleToken != token ||
            tombstone.LifecycleRequestSlot != requestSlot ||
            !tombstone.LifecycleBeforeClaimed || tombstone.LifecycleAfterClaimed)
            return false;
        tombstone.LifecycleAfterClaimed = true;
        context = CreateTombstoneContext(tombstone, CassetteDiskCommitDiagnosticPhase.LifecycleAfter);
        return true;
    }

    private static CassetteDiskCommitDiagnosticContext CreateTombstoneContext(
        TerminalDiagnosticTombstone tombstone,
        CassetteDiskCommitDiagnosticPhase phase) =>
        new(
            tombstone.Attempt,
            phase,
            tombstone.EventOrdinal,
            tombstone.Elapsed,
            tombstone.ActiveSongs.ToArray(),
            tombstone.QueuedSongs.ToArray(),
            tombstone.Failure.Kind,
            tombstone.Failure.Detail);

    private bool CanClaimDiagnostic(CassetteDiskCommitDiagnosticPhase phase) => phase switch
    {
        CassetteDiskCommitDiagnosticPhase.Pre => _active && !_submitted,
        CassetteDiskCommitDiagnosticPhase.Post => _active,
        CassetteDiskCommitDiagnosticPhase.Event => _eventOrdinal > 0,
        CassetteDiskCommitDiagnosticPhase.StillPending => _stillPendingReported,
        CassetteDiskCommitDiagnosticPhase.PreparedEventIgnored => _active && !_submitted,
        CassetteDiskCommitDiagnosticPhase.PreTarget => _active && !_submitted,
        CassetteDiskCommitDiagnosticPhase.PostTarget => _active,
        _ => false,
    };

    internal CassetteDiskCommitOutcome FailPrepared(
        CassetteDiskCommitAttempt attempt,
        CassetteDiskCommitFailureDiagnostic failure = default)
    {
        if (!_active || _submitted || attempt != _attempt) return CassetteDiskCommitOutcome.None;
        if (failure.Kind is CassetteDiskCommitFailureKind.None)
            failure = CassetteDiskCommitFailureDiagnostic.SubmissionFailed("unknown");
        return FinishFailure(CassetteDiskCommitOutcome.Failure, failure);
    }

    internal bool TryReportRejectedEvent(CassetteDiskCommitAttempt attempt)
    {
        if (!_active || !_submitted || attempt != _attempt || _rejectedEventReported) return false;
        _rejectedEventReported = true;
        return true;
    }

    internal CassetteDiskCommitEventOutcome ObserveWriteCompletedEvent(
        CassetteDiskCommitAttempt attempt, int eventSlot, bool succeeded)
    {
        if (!_active || !_submitted || attempt != _attempt || eventSlot != _slot)
            return CassetteDiskCommitEventOutcome.Ignored;
        if (!succeeded)
        {
            _eventOrdinal++;
            FinishFailure(CassetteDiskCommitOutcome.Failure, CassetteDiskCommitFailureDiagnostic.EventFailure());
            return CassetteDiskCommitEventOutcome.Failure;
        }
        if (_successEventObserved) return CassetteDiskCommitEventOutcome.Ignored;
        _eventOrdinal++;
        _successEventObserved = true;
        return CassetteDiskCommitEventOutcome.SuccessWake;
    }

    internal CassetteDiskCommitOutcome ObservePreSubmitUnavailable(
        long epoch, int slot, long pointer, bool identityReadable, long observedPointer,
        bool statusesRetained, TimeSpan elapsed, out bool reportDeferred)
    {
        reportDeferred = false;
        if (_active || _songs.Count == 0 || _blocked) return CassetteDiskCommitOutcome.None;
        if (!_acquiring)
            StartAcquisition(epoch, slot, pointer);
        else if (_acquisitionRevision != _revision)
            StartAcquisition(epoch, slot, pointer);
        else if (_acquisitionEpoch != epoch || _acquisitionSlot != slot || _acquisitionPointer != pointer)
            return FinishPreSubmitFailure(CassetteDiskCommitOutcome.Cancelled);

        if (!_acquisitionDeferredReported)
        {
            _acquisitionDeferredReported = true;
            reportDeferred = true;
        }
        if (pointer == 0 || (identityReadable && observedPointer != pointer))
            return FinishPreSubmitFailure(CassetteDiskCommitOutcome.Cancelled);
        if (identityReadable && !statusesRetained)
            return FinishPreSubmitFailure(CassetteDiskCommitOutcome.Failure);
        if (elapsed > TimeSpan.Zero) _acquisitionElapsed += elapsed;
        return _acquisitionElapsed >= AcquisitionTimeout
            ? FinishPreSubmitFailure(CassetteDiskCommitOutcome.Timeout)
            : CassetteDiskCommitOutcome.Pending;
    }

    internal CassetteDiskCommitOutcome Observe(
        CassetteDiskCommitAttempt attempt, CassettePublicWriteState state,
        bool statusesRetained, TimeSpan elapsed, out bool reportStillPending)
        => Observe(
            attempt,
            state,
            statusesRetained,
            statusesRetained
                ? default
                : CassetteDiskCommitFailureDiagnostic.StatusRegression("<unknown>", "<unavailable>"),
            elapsed,
            out reportStillPending);

    internal CassetteDiskCommitOutcome Observe(
        CassetteDiskCommitAttempt attempt, CassettePublicWriteState state,
        bool statusesRetained, CassetteDiskCommitFailureDiagnostic boundaryFailure,
        TimeSpan elapsed, out bool reportStillPending)
    {
        reportStillPending = false;
        CassetteDiskCommitOutcome boundary = CheckBoundary(attempt, statusesRetained, boundaryFailure);
        if (boundary is not CassetteDiskCommitOutcome.Pending) return boundary;
        bool failureAdvanced = state.LastFailureTime.HasValue &&
            (!_baselineFailure.HasValue || state.LastFailureTime.Value > _baselineFailure.Value);
        bool failureReasonChanged = !string.IsNullOrWhiteSpace(state.FailureReason) &&
            !string.Equals(state.FailureReason, _baselineFailureReason, StringComparison.Ordinal);
        if (failureAdvanced)
            return FinishFailure(
                CassetteDiskCommitOutcome.Failure,
                CassetteDiskCommitFailureDiagnostic.FailureTimeAdvanced(_baselineFailure, state.LastFailureTime!.Value));
        if (failureReasonChanged)
            return FinishFailure(
                CassetteDiskCommitOutcome.Failure,
                CassetteDiskCommitFailureDiagnostic.FailureReasonChanged(_baselineFailureReason, state.FailureReason!));
        bool successAdvanced = state.LastSuccessTime.HasValue &&
            (!_baselineSuccess.HasValue || state.LastSuccessTime.Value > _baselineSuccess.Value);
        if (!state.RequiresWriteToDisk && successAdvanced)
        {
            _active = false; _submitted = false;
            _songs.ExceptWith(_activeSongs);
            _activeSongs.Clear();
            _blocked = false;
            return CassetteDiskCommitOutcome.Success;
        }
        return AdvanceTimeout(elapsed, out reportStillPending);
    }

    internal CassetteDiskCommitOutcome ObserveUnavailable(
        CassetteDiskCommitAttempt attempt, bool statusesRetained,
        TimeSpan elapsed, out bool reportStillPending)
        => ObserveUnavailable(
            attempt,
            statusesRetained,
            statusesRetained
                ? default
                : CassetteDiskCommitFailureDiagnostic.StatusRegression("<unknown>", "<unavailable>"),
            elapsed,
            out reportStillPending);

    internal CassetteDiskCommitOutcome ObserveUnavailable(
        CassetteDiskCommitAttempt attempt, bool statusesRetained,
        CassetteDiskCommitFailureDiagnostic boundaryFailure,
        TimeSpan elapsed, out bool reportStillPending)
    {
        reportStillPending = false;
        CassetteDiskCommitOutcome boundary = CheckBoundary(attempt, statusesRetained, boundaryFailure);
        return boundary is CassetteDiskCommitOutcome.Pending
            ? AdvanceTimeout(elapsed, out reportStillPending)
            : boundary;
    }

    private CassetteDiskCommitOutcome CheckBoundary(
        CassetteDiskCommitAttempt attempt,
        bool statusesRetained,
        CassetteDiskCommitFailureDiagnostic boundaryFailure)
    {
        if (!_active || !_submitted || attempt != _attempt) return CassetteDiskCommitOutcome.None;
        if (!statusesRetained)
        {
            if (boundaryFailure.Kind is CassetteDiskCommitFailureKind.None)
                boundaryFailure = CassetteDiskCommitFailureDiagnostic.StatusRegression("<unknown>", "<unavailable>");
            return FinishFailure(CassetteDiskCommitOutcome.Failure, boundaryFailure);
        }
        return CassetteDiskCommitOutcome.Pending;
    }

    private CassetteDiskCommitOutcome AdvanceTimeout(TimeSpan elapsed, out bool reportStillPending)
    {
        reportStillPending = false;
        if (elapsed > TimeSpan.Zero) _elapsed += elapsed;
        if (_elapsed >= HardTimeout)
        {
            _indeterminate = true;
            return FinishFailure(CassetteDiskCommitOutcome.HardTimeout, CassetteDiskCommitFailureDiagnostic.HardTimeout());
        }
        if (!_stillPendingReported && _elapsed >= StillPendingAfter)
        {
            _stillPendingReported = true;
            reportStillPending = true;
        }
        return CassetteDiskCommitOutcome.Pending;
    }

    private CassetteDiskCommitOutcome FinishFailure(
        CassetteDiskCommitOutcome outcome,
        CassetteDiskCommitFailureDiagnostic failure)
    {
        PreserveTerminalDiagnostic(outcome, failure);
        _active = false; _submitted = false;
        _activeSongs.Clear();
        _blocked = _indeterminate || _revision == _activeRevision;
        return outcome;
    }

    private void PreserveTerminalDiagnostic(
        CassetteDiskCommitOutcome outcome,
        CassetteDiskCommitFailureDiagnostic failure)
    {
        string[] activeSongs = _diagnosticAttemptSongs.ToArray();
        string[] queuedSongs = _songs.Except(_diagnosticAttemptSongs, StringComparer.Ordinal)
            .OrderBy(song => song, StringComparer.Ordinal).ToArray();
        _terminalTombstone = new TerminalDiagnosticTombstone
        {
            Attempt = _attempt,
            Outcome = outcome,
            Failure = failure,
            EventOrdinal = _eventOrdinal,
            Elapsed = _elapsed,
            ActiveSongs = activeSongs,
            QueuedSongs = queuedSongs,
        };
    }

    private void StartAcquisition(long epoch, int slot, long pointer)
    {
        _acquiring = true; _acquisitionDeferredReported = false;
        _acquisitionRevision = _revision; _acquisitionEpoch = epoch;
        _acquisitionSlot = slot; _acquisitionPointer = pointer;
        _acquisitionElapsed = TimeSpan.Zero;
    }

    private CassetteDiskCommitOutcome FinishPreSubmitFailure(CassetteDiskCommitOutcome outcome)
    {
        ClearAcquisition();
        _blocked = true;
        return outcome;
    }

    private void ClearAcquisition()
    {
        _acquiring = false; _acquisitionDeferredReported = false;
        _acquisitionRevision = 0; _acquisitionEpoch = 0;
        _acquisitionSlot = 0; _acquisitionPointer = 0;
        _acquisitionElapsed = TimeSpan.Zero;
    }
}

internal sealed class CassetteSaveEpochRuntime
{
    private static readonly TimeSpan[] VerificationDelays =
    {
        TimeSpan.FromMilliseconds(250),
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
        (!_attemptsThisEpoch.TryGetValue(nativeSong, out int attempts) || attempts < VerificationDelays.Length);

    internal void RecordSubmission(string nativeSong)
    {
        if (!IsPending(nativeSong)) return;
        _attemptsThisEpoch[nativeSong] = _attemptsThisEpoch.TryGetValue(nativeSong, out int attempts) ? attempts + 1 : 1;
        _verificationElapsed[nativeSong] = TimeSpan.Zero;
    }

    internal void RecordSubmissionFailure(string nativeSong)
    {
        if (!IsPending(nativeSong)) return;
        _attemptsThisEpoch[nativeSong] = _attemptsThisEpoch.TryGetValue(nativeSong, out int attempts) ? attempts + 1 : 1;
        _verificationElapsed.Remove(nativeSong);
    }

    internal IReadOnlyList<string> Tick(TimeSpan elapsed)
    {
        if (!HasActiveSave || elapsed <= TimeSpan.Zero) return Array.Empty<string>();
        var ready = new List<string>();
        foreach (string song in _verificationElapsed.Keys.ToArray())
        {
            TimeSpan total = _verificationElapsed[song] + elapsed;
            _verificationElapsed[song] = total;
            int attempt = _attemptsThisEpoch.TryGetValue(song, out int count) ? count : 1;
            TimeSpan delay = VerificationDelays[Math.Clamp(attempt - 1, 0, VerificationDelays.Length - 1)];
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
