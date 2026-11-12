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
        // AP ownership is independent of completing this native source. A cassette
        // received early must not erase its source check; AP queueing is idempotent.
        string[] sources = mapped.Select(x=>x.SourceName).ToArray();
        if(newlyEarned.Count==0) return new(true,sources,Array.Empty<string>(),"all mapped cassettes already owned; source checks retained");
        return new(false,sources,newlyEarned.Select(x=>x.NativeSong).ToArray(),$"intercepted {newlyEarned.Count} newly earned cassette(s)");
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

internal readonly record struct CassetteGameplayReadyIdentity(
    long Generation,
    long Epoch,
    int Slot,
    long Pointer);

internal readonly record struct CassetteGameplayReadyObservation(
    CassetteGameplayReadyIdentity Identity,
    long BoundaryRevision,
    long MarkerIncarnation,
    int MarkerIdentity);

internal sealed class CassetteGameplayReadyGate
{
    private CassetteGameplayReadyIdentity? _activeIdentity;
    private long _boundaryRevision;
    private bool _boundaryPending;
    private bool _open;
    private bool _markerLive;
    private int _markerIdentity;
    private long _markerIncarnation;
    private long _freshMarkerBoundaryRevision = -1;

    internal void BeginBoundary()
    {
        _boundaryRevision++;
        _boundaryPending = true;
        _activeIdentity = null;
        _open = false;
        _freshMarkerBoundaryRevision = -1;
    }

    internal void Activate(CassetteGameplayReadyIdentity identity)
    {
        if (!_boundaryPending)
            BeginBoundary();
        _activeIdentity = identity;
        _open = false;
    }

    internal void ObserveMarker(bool phoneBankLive, int markerIdentity)
    {
        if (!phoneBankLive)
        {
            _markerLive = false;
            _markerIdentity = 0;
            return;
        }

        bool newIncarnation = !_markerLive || _markerIdentity != markerIdentity;
        _markerLive = true;
        _markerIdentity = markerIdentity;
        if (!newIncarnation) return;

        _markerIncarnation++;
        if (_boundaryPending)
            _freshMarkerBoundaryRevision = _boundaryRevision;
    }

    internal bool IsOpen(CassetteGameplayReadyIdentity identity) =>
        _open && _activeIdentity == identity;

    internal bool TryCapture(out CassetteGameplayReadyObservation observation)
    {
        if (!_activeIdentity.HasValue || !_boundaryPending || _open || !_markerLive ||
            _freshMarkerBoundaryRevision != _boundaryRevision)
        {
            observation = default;
            return false;
        }

        observation = new(
            _activeIdentity.Value,
            _boundaryRevision,
            _markerIncarnation,
            _markerIdentity);
        return true;
    }

    internal bool TryOpen(CassetteGameplayReadyObservation observation)
    {
        if (_open || !_boundaryPending || !_markerLive || !_activeIdentity.HasValue ||
            _activeIdentity.Value != observation.Identity ||
            _boundaryRevision != observation.BoundaryRevision ||
            _freshMarkerBoundaryRevision != observation.BoundaryRevision ||
            _markerIncarnation != observation.MarkerIncarnation ||
            _markerIdentity != observation.MarkerIdentity)
            return false;

        _open = true;
        _boundaryPending = false;
        return true;
    }
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

internal static class CassetteDiskCommitDiagnosticFormatter
{
    internal static string FormatNullableDouble(double? value)
    {
        if (!value.HasValue) return "present=False value=<null> bits=<null>";
        return $"present=True value={value.Value.ToString("R", CultureInfo.InvariantCulture)} bits=0x{BitConverter.DoubleToInt64Bits(value.Value):X16}";
    }
}

internal readonly record struct CassettePersistenceTargetDiagnosticIdentity(
    long Generation,
    long Epoch,
    int Slot,
    long StatePointer);

internal readonly record struct CassettePersistenceTargetDiagnosticWave(
    CassettePersistenceTargetDiagnosticIdentity Identity,
    IReadOnlyList<string> Songs);

internal sealed class CassetteSaveEpochRuntime
{
    private static readonly TimeSpan VerificationDelay = TimeSpan.FromMilliseconds(250);
    private readonly HashSet<string> _owned = new(StringComparer.Ordinal);
    private readonly HashSet<string> _satisfiedThisEpoch = new(StringComparer.Ordinal);
    private readonly HashSet<string> _newlyVerifiedGrantDiagnosticWave = new(StringComparer.Ordinal);
    private readonly HashSet<string> _attemptedThisEpoch = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TimeSpan> _submittedAt = new(StringComparer.Ordinal);
    private TimeSpan _verificationClock;

    internal long Epoch { get; private set; }
    internal int? ActiveSlot { get; private set; }
    internal bool HasActiveSave => ActiveSlot.HasValue;

    internal void ActivateSave(int slot)
    {
        Epoch++;
        ActiveSlot = slot;
        _satisfiedThisEpoch.Clear();
        _newlyVerifiedGrantDiagnosticWave.Clear();
        _attemptedThisEpoch.Clear();
        _submittedAt.Clear();
    }

    internal void DeactivateSave()
    {
        ActiveSlot = null;
        _satisfiedThisEpoch.Clear();
        _newlyVerifiedGrantDiagnosticWave.Clear();
        _attemptedThisEpoch.Clear();
        _submittedAt.Clear();
    }

    internal void ReplaceOwnedSongs(IEnumerable<string> songs)
    {
        _owned.Clear();
        _owned.UnionWith(songs);
        _satisfiedThisEpoch.IntersectWith(_owned);
        _newlyVerifiedGrantDiagnosticWave.IntersectWith(_owned);
        _attemptedThisEpoch.IntersectWith(_owned);
        foreach (string song in _submittedAt.Keys.Where(song => !_owned.Contains(song)).ToArray()) _submittedAt.Remove(song);
    }

    internal void Receive(string nativeSong)
    {
        _owned.Add(nativeSong);
    }

    internal bool IsPending(string nativeSong) =>
        HasActiveSave && _owned.Contains(nativeSong) && !_satisfiedThisEpoch.Contains(nativeSong);

    // Only owned songs can be marked satisfied, and replacing ownership also prunes that set.
    internal bool HasPendingSongs => HasActiveSave && _owned.Count > _satisfiedThisEpoch.Count;

    internal IReadOnlyList<string> PendingSongs => HasPendingSongs
        ? _owned.Where(IsPending).OrderBy(song => song, StringComparer.Ordinal).ToArray()
        : Array.Empty<string>();

    internal IReadOnlyList<string> OwnedSongs => _owned
        .OrderBy(song => song, StringComparer.Ordinal).ToArray();

    internal bool TryConsumeNewlyVerifiedGrantDiagnosticWave(
        long generation,
        long statePointer,
        out CassettePersistenceTargetDiagnosticWave wave)
    {
        wave = default;
        if (!HasActiveSave || _newlyVerifiedGrantDiagnosticWave.Count == 0) return false;
        string[] songs = _newlyVerifiedGrantDiagnosticWave
            .OrderBy(song => song, StringComparer.Ordinal).ToArray();
        _newlyVerifiedGrantDiagnosticWave.Clear();
        wave = new(
            new(generation, Epoch, ActiveSlot!.Value, statePointer),
            songs);
        return true;
    }

    internal bool CanSubmit(string nativeSong, string? nativeStatus, bool processorAvailable) =>
        IsPending(nativeSong) &&
        processorAvailable &&
        CassetteRandomizationPolicy.IsUnearned(nativeStatus) &&
        !_attemptedThisEpoch.Contains(nativeSong);

    internal void RecordSubmission(string nativeSong)
    {
        if (!IsPending(nativeSong)) return;
        _attemptedThisEpoch.Add(nativeSong);
        if (_submittedAt.Count == 0) _verificationClock = TimeSpan.Zero;
        _submittedAt[nativeSong] = _verificationClock;
    }

    internal void RecordSubmissionFailure(string nativeSong)
    {
        if (!IsPending(nativeSong)) return;
        _attemptedThisEpoch.Add(nativeSong);
        _submittedAt.Remove(nativeSong);
    }

    internal IReadOnlyList<string> Tick(TimeSpan elapsed)
    {
        if (_submittedAt.Count == 0 || !HasActiveSave || elapsed <= TimeSpan.Zero)
            return Array.Empty<string>();
        // The caller advances this clock only while the selected save is ready.
        _verificationClock += elapsed;
        List<string>? ready = null;
        foreach (var timer in _submittedAt)
            if (_verificationClock - timer.Value >= VerificationDelay)
                (ready ??= new List<string>()).Add(timer.Key);
        return ready is null ? Array.Empty<string>() : ready;
    }

    internal void Observe(string nativeSong, string? nativeStatus)
    {
        // Ordinary observations cannot bypass delayed verification and its persistence wave.
        // RecordVerification removes the timer before accepting its authoritative observation.
        if (_submittedAt.ContainsKey(nativeSong) ||
            !HasActiveSave || !_owned.Contains(nativeSong) || !CassetteRandomizationPolicy.IsOwned(nativeStatus))
            return;

        _satisfiedThisEpoch.Add(nativeSong);
    }

    internal bool RecordVerification(string nativeSong, string? nativeStatus)
    {
        if (!IsPending(nativeSong)) return false;
        bool delayedVerificationWasDue = _submittedAt.Remove(nativeSong, out TimeSpan submittedAt) &&
            _verificationClock - submittedAt >= VerificationDelay;
        Observe(nativeSong, nativeStatus);
        bool newlyVerifiedGrant = string.Equals(
            nativeStatus, CassetteRandomizationPolicy.HaveInBag, StringComparison.OrdinalIgnoreCase);
        if (newlyVerifiedGrant && delayedVerificationWasDue)
            _newlyVerifiedGrantDiagnosticWave.Add(nativeSong);
        return newlyVerifiedGrant;
    }


}

internal static class CassettePersistenceTargetDiagnosticDecision
{
    internal static string Classify(
        bool identityCurrent,
        bool expectedEntryMatches,
        bool selectedEntryMatches) =>
        !identityCurrent
            ? "identity-changed-no-candidate"
            : selectedEntryMatches
                ? "guarded-request-candidate"
                : expectedEntryMatches
                    ? "pointer-bound-public-state-candidate"
                    : "no-write-candidate";
}

internal readonly record struct CassettePersistenceAcceptanceIdentity(
    long Generation,
    long Epoch,
    int Slot,
    long Pointer);

internal readonly record struct CassettePersistenceAcceptanceBaseline(
    CassettePublicWriteState WriteState,
    int RedundancyBundleIndex,
    int RedundancyBundleRevision,
    IReadOnlyList<string> Songs);

internal readonly record struct CassettePersistenceAcceptanceAttempt(
    long Id,
    long Generation,
    long Epoch,
    int Slot,
    long Pointer)
{
    internal CassettePersistenceAcceptanceIdentity Identity =>
        new(Generation, Epoch, Slot, Pointer);
}

internal readonly record struct CassettePersistenceQueuedWave(
    CassettePersistenceAcceptanceIdentity Identity,
    IReadOnlyList<string> Songs,
    long WaveId);

internal sealed class CassettePersistenceWaveQueue
{
    private readonly Queue<CassettePersistenceQueuedWave> _waves = new();
    private CassettePersistenceAcceptanceIdentity _identity;
    private bool _hasEpoch;
    private bool _epochTombstoned;
    private long _waveCounter;

    internal void BeginEpoch(CassettePersistenceAcceptanceIdentity identity)
    {
        if (_hasEpoch && identity == _identity) return;
        _waves.Clear();
        _identity = identity;
        _hasEpoch = identity.Pointer != 0;
        _epochTombstoned = false;
    }

    internal bool TryEnqueue(
        CassettePersistenceAcceptanceIdentity identity,
        IEnumerable<string> songs)
    {
        if (!_hasEpoch || _epochTombstoned || identity != _identity || identity.Pointer == 0)
            return false;
        string[] normalized = songs
            .Where(song => !string.IsNullOrWhiteSpace(song))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(song => song, StringComparer.Ordinal)
            .ToArray();
        if (normalized.Length == 0) return false;
        _waves.Enqueue(new(identity, normalized, ++_waveCounter));
        return true;
    }

    internal bool TryPeek(
        CassettePersistenceAcceptanceIdentity identity,
        out CassettePersistenceQueuedWave wave)
    {
        wave = default;
        if (!_hasEpoch || _epochTombstoned || identity != _identity || _waves.Count == 0)
            return false;
        wave = _waves.Peek();
        return true;
    }

    internal void Complete(CassettePersistenceQueuedWave wave)
    {
        if (!_hasEpoch || _waves.Count == 0) return;
        CassettePersistenceQueuedWave current = _waves.Peek();
        if (wave.Identity != _identity || current.Identity != wave.Identity ||
            current.WaveId != wave.WaveId)
            return;
        _waves.Dequeue();
    }

    internal void TombstoneEpoch(CassettePersistenceAcceptanceIdentity identity)
    {
        if (!_hasEpoch || identity != _identity) return;
        _waves.Clear();
        _epochTombstoned = true;
    }

    internal void CancelEpoch(CassettePersistenceAcceptanceIdentity identity)
    {
        if (!_hasEpoch || identity != _identity) return;
        _waves.Clear();
        _hasEpoch = false;
        _identity = default;
    }
}

internal enum CassettePersistenceAcceptanceOutcome
{
    None,
    Pending,
    Verified,
    Failed,
    Timeout,
    Cancelled,
}

internal readonly record struct CassettePersistenceAcceptanceEligibilityEvidence(
    bool OptInEnabled,
    bool RoutingCompatible,
    bool GameplayReady,
    bool WaveCurrent,
    bool SelectionValid,
    bool SelectedPointerMatches,
    bool RegisteredRetainedProcessorMatch,
    bool ExpectedSlotEntryMatches,
    bool PointerBoundDecision,
    bool DefaultBundlePresent,
    int DefaultBundleChangeCount,
    bool StatusesRetainedInBag,
    bool HasUnstagedChanges,
    bool HasChanges,
    bool RequiresWriteToDisk);

internal static class CassettePersistenceAcceptanceEligibility
{
    internal static bool Evaluate(
        CassettePersistenceAcceptanceEligibilityEvidence evidence,
        out string stage)
    {
        if (!evidence.OptInEnabled) { stage = "acceptance-disabled"; return false; }
        if (!evidence.RoutingCompatible) { stage = "acceptance-routing-incompatible"; return false; }
        if (!evidence.GameplayReady) { stage = "acceptance-gameplay-not-ready"; return false; }
        if (!evidence.WaveCurrent) { stage = "acceptance-wave-stale"; return false; }
        if (!evidence.SelectionValid) { stage = "acceptance-selection-invalid"; return false; }
        if (!evidence.SelectedPointerMatches) { stage = "acceptance-selected-pointer-mismatch"; return false; }
        if (!evidence.RegisteredRetainedProcessorMatch) { stage = "acceptance-processor-ownership-mismatch"; return false; }
        if (!evidence.ExpectedSlotEntryMatches) { stage = "acceptance-expected-entry-mismatch"; return false; }
        if (!evidence.PointerBoundDecision) { stage = "acceptance-not-pointer-bound-candidate"; return false; }
        if (!evidence.DefaultBundlePresent) { stage = "acceptance-default-bundle-missing"; return false; }
        if (evidence.DefaultBundleChangeCount <= 0) { stage = "acceptance-default-bundle-empty"; return false; }
        if (!evidence.StatusesRetainedInBag) { stage = "acceptance-status-not-retained"; return false; }
        if (!evidence.HasUnstagedChanges) { stage = "acceptance-no-unstaged-changes"; return false; }
        if (!evidence.HasChanges) { stage = "acceptance-no-changes"; return false; }
        // RequiresWriteToDisk is a pending-write flag, not an in-flight lock.
        // Native PersistSaveChangeBundleRequest also stages bundles while it is set;
        // deferring here strands AP receipts and blocks the Cassette Lord bundle.
        stage = "success";
        return true;
    }
}

internal static class CassettePersistenceAcceptanceOwnershipProof
{
    internal static bool IsExactPointerBoundOwner(
        bool hasRegisteredPointer,
        long registeredPointer,
        bool hasRetainedPointer,
        long retainedPointer,
        bool hasExpectedEntryPointer,
        long expectedEntryPointer,
        long expectedStatePointer) =>
        hasRegisteredPointer && registeredPointer != 0 &&
        hasRetainedPointer && retainedPointer == registeredPointer &&
        hasExpectedEntryPointer && expectedEntryPointer == expectedStatePointer &&
        expectedStatePointer != 0;

    internal static bool IsKnownExpectedSlotOnlyDiagnostic(
        bool aggregateReadable,
        string stage,
        bool hasSelectedSlot,
        bool hasSelectedEntryPointer,
        bool hasSelectedSide) =>
        aggregateReadable ||
        string.Equals(stage, "target-selected-entry-missing", StringComparison.Ordinal) &&
        hasSelectedSlot && !hasSelectedEntryPointer && !hasSelectedSide;
}

internal sealed class CassettePointerBoundPersistenceRuntime
{
    private static readonly TimeSpan HardTimeout = TimeSpan.FromSeconds(130);
    private static readonly TimeSpan MaximumUpdateElapsed = TimeSpan.FromSeconds(1);
    private CassettePersistenceAcceptanceIdentity _epochIdentity;
    private bool _hasEpoch;
    private bool _epochTombstoned;
    private bool _active;
    private bool _invoked;
    private long _attemptCounter;
    private CassettePersistenceAcceptanceAttempt _attempt;
    private CassettePersistenceAcceptanceBaseline _baseline;
    private TimeSpan _elapsed;

    internal bool Active => _active;
    internal bool Invoked => _active && _invoked;
    internal CassettePersistenceAcceptanceAttempt Attempt => _attempt;
    internal CassettePersistenceAcceptanceBaseline Baseline => _baseline;
    internal CassettePersistenceAcceptanceOutcome LastOutcome { get; private set; }
    internal string LastFailure { get; private set; } = string.Empty;

    internal void BeginEpoch(CassettePersistenceAcceptanceIdentity identity)
    {
        if (_hasEpoch && _epochIdentity == identity) return;
        if (_active) Finish(CassettePersistenceAcceptanceOutcome.Cancelled, "new-save-epoch");
        _epochIdentity = identity;
        _hasEpoch = true;
        _epochTombstoned = false;
        _active = false;
        _invoked = false;
        _attempt = default;
        _baseline = default;
        _elapsed = TimeSpan.Zero;
        LastOutcome = CassettePersistenceAcceptanceOutcome.None;
        LastFailure = string.Empty;
    }

    internal bool TryPrepare(
        CassettePersistenceAcceptanceIdentity identity,
        CassettePersistenceAcceptanceBaseline baseline,
        out CassettePersistenceAcceptanceAttempt attempt)
    {
        attempt = default;
        if (!_hasEpoch || identity != _epochIdentity || identity.Pointer == 0 ||
            baseline.Songs.Count == 0 || _active || _epochTombstoned)
            return false;
        _attempt = new(++_attemptCounter, identity.Generation, identity.Epoch, identity.Slot, identity.Pointer);
        _baseline = baseline with
        {
            Songs = baseline.Songs.Distinct(StringComparer.Ordinal)
                .OrderBy(song => song, StringComparer.Ordinal).ToArray(),
        };
        _active = true;
        _invoked = false;
        _elapsed = TimeSpan.Zero;
        LastOutcome = CassettePersistenceAcceptanceOutcome.Pending;
        LastFailure = string.Empty;
        attempt = _attempt;
        return true;
    }

    internal bool MarkInvoked(CassettePersistenceAcceptanceAttempt attempt)
    {
        if (!_active || attempt != _attempt) return false;
        _invoked = true;
        return true;
    }

    internal CassettePersistenceAcceptanceOutcome MarkIndeterminate(
        CassettePersistenceAcceptanceAttempt attempt,
        string stage)
    {
        if (!_active || attempt != _attempt) return CassettePersistenceAcceptanceOutcome.None;
        _epochTombstoned = true;
        return Finish(CassettePersistenceAcceptanceOutcome.Failed, $"indeterminate:{stage}");
    }

    internal CassettePersistenceAcceptanceOutcome Observe(
        CassettePersistenceAcceptanceAttempt attempt,
        CassettePersistenceAcceptanceIdentity currentIdentity,
        CassettePublicWriteDiagnosticState state,
        bool statusesRetained,
        bool stateReadable,
        TimeSpan elapsed)
    {
        if (!_active || !_invoked || attempt != _attempt)
            return CassettePersistenceAcceptanceOutcome.None;
        if (currentIdentity != _epochIdentity || currentIdentity != attempt.Identity ||
            stateReadable && state.StatePointer != attempt.Pointer)
            return FinishTombstoned("identity-or-pointer-changed");
        if (!statusesRetained)
            return FinishTombstoned("cassette-status-regression");
        if (!stateReadable)
            return FinishTombstoned("public-state-unreadable");

        CassettePublicWriteState baselineWrite = _baseline.WriteState;
        CassettePublicWriteState currentWrite = state.WriteState;
        bool failureAdvanced = currentWrite.LastFailureTime.HasValue &&
            (!baselineWrite.LastFailureTime.HasValue ||
             currentWrite.LastFailureTime.Value > baselineWrite.LastFailureTime.Value);
        if (failureAdvanced)
            return FinishTombstoned("failure-time-advanced");
        if (!string.IsNullOrWhiteSpace(currentWrite.FailureReason) &&
            !string.Equals(currentWrite.FailureReason, baselineWrite.FailureReason, StringComparison.Ordinal))
            return FinishTombstoned("failure-reason-changed");

        // A completion event alone cannot identify which save operation finished.
        // Require an advance since this batch started and cleared native write flags.
        bool successAdvanced = currentWrite.LastSuccessTime.HasValue &&
            (!baselineWrite.LastSuccessTime.HasValue ||
             currentWrite.LastSuccessTime.Value > baselineWrite.LastSuccessTime.Value);
        bool redundancyAdvanced = state.RedundancyBundleRevision > _baseline.RedundancyBundleRevision ||
            state.RedundancyBundleIndex != _baseline.RedundancyBundleIndex;
        if (!state.HasUnstagedChanges && !currentWrite.HasChanges && !currentWrite.RequiresWriteToDisk &&
            (successAdvanced || redundancyAdvanced))
        {
            return Finish(CassettePersistenceAcceptanceOutcome.Verified, string.Empty);
        }

        TimeSpan boundedElapsed = elapsed <= TimeSpan.Zero
            ? TimeSpan.Zero
            : elapsed > MaximumUpdateElapsed ? MaximumUpdateElapsed : elapsed;
        _elapsed += boundedElapsed;
        if (_elapsed >= HardTimeout)
        {
            _epochTombstoned = true;
            return Finish(CassettePersistenceAcceptanceOutcome.Timeout, "hard-timeout-130s");
        }
        return CassettePersistenceAcceptanceOutcome.Pending;
    }

    internal CassettePersistenceAcceptanceOutcome Cancel(string reason)
    {
        if (!_active) return CassettePersistenceAcceptanceOutcome.None;
        return Finish(CassettePersistenceAcceptanceOutcome.Cancelled, reason);
    }

    private CassettePersistenceAcceptanceOutcome FinishTombstoned(string failure)
    {
        _epochTombstoned = true;
        return Finish(CassettePersistenceAcceptanceOutcome.Failed, failure);
    }

    private CassettePersistenceAcceptanceOutcome Finish(
        CassettePersistenceAcceptanceOutcome outcome,
        string failure)
    {
        _active = false;
        _invoked = false;
        LastOutcome = outcome;
        LastFailure = failure;
        return outcome;
    }
}

internal readonly record struct CassettePersistenceAcceptanceMarkerRecord(
    long Sequence,
    CassettePersistenceAcceptanceAttempt Attempt,
    string Marker,
    string Detail,
    bool Terminal);

internal delegate bool CassettePersistenceAcceptanceMarkerTake(
    out CassettePersistenceAcceptanceMarkerRecord marker);

internal sealed class CassettePersistenceAcceptanceMarkerJournal
{
    private readonly Queue<CassettePersistenceAcceptanceMarkerRecord> _pending = new();
    private long _sequence;
    private bool _hasAttempt;
    private bool _terminal;
    private CassettePersistenceAcceptanceAttempt _attempt;

    internal bool TryBeginAttempt(
        CassettePersistenceAcceptanceAttempt attempt,
        string detail)
    {
        if (_hasAttempt || attempt.Id <= 0 || attempt.Pointer == 0) return false;
        _hasAttempt = true;
        _terminal = false;
        _attempt = attempt;
        return Enqueue(attempt, "PRE", detail, terminal: false);
    }

    internal bool TryAppend(
        CassettePersistenceAcceptanceAttempt attempt,
        string marker,
        string detail,
        bool terminal)
    {
        if (!_hasAttempt || _terminal || attempt != _attempt ||
            string.IsNullOrWhiteSpace(marker))
            return false;
        bool appended = Enqueue(attempt, marker, detail, terminal);
        if (appended && terminal) _terminal = true;
        return appended;
    }

    internal void RetireAttempt(CassettePersistenceAcceptanceAttempt attempt)
    {
        if (!_hasAttempt || attempt != _attempt) return;
        _hasAttempt = false;
        _terminal = false;
        _attempt = default;
    }

    internal bool TryDequeue(out CassettePersistenceAcceptanceMarkerRecord marker) =>
        _pending.TryDequeue(out marker);

    private bool Enqueue(
        CassettePersistenceAcceptanceAttempt attempt,
        string marker,
        string detail,
        bool terminal)
    {
        _pending.Enqueue(new(
            ++_sequence,
            attempt,
            marker,
            detail ?? string.Empty,
            terminal));
        return true;
    }
}

internal sealed class CassettePersistenceAcceptanceMarkerEmitter
{
    private readonly object _emissionSync = new();

    internal void Drain(
        CassettePersistenceAcceptanceMarkerTake take,
        Action<CassettePersistenceAcceptanceMarkerRecord> emit)
    {
        lock (_emissionSync)
        {
            while (take(out CassettePersistenceAcceptanceMarkerRecord marker))
                emit(marker);
        }
    }
}

internal enum CassetteProductionPersistenceStartOutcome
{
    Deferred,
    Invoked,
    Indeterminate,
    Stale,
}

internal readonly record struct CassetteProductionPersistenceAdapterResult(
    bool Invoked,
    string Stage,
    string Detail);

internal sealed class CassetteProductionPersistenceCoordinator
{
    private readonly object _sync = new();
    private readonly Action _requestReconciliation;
    private readonly CassettePointerBoundPersistenceRuntime _runtime = new();
    private readonly CassettePersistenceWaveQueue _waves = new();
    private readonly CassettePersistenceAcceptanceMarkerJournal _markers = new();
    private CassettePersistenceAcceptanceIdentity _identity;
    private bool _hasEpoch;
    private CassettePersistenceQueuedWave _activeWave;
    private bool _hasActiveWave;
    private long _deferredLoggedWaveId;

    internal CassetteProductionPersistenceCoordinator(Action requestReconciliation) =>
        _requestReconciliation = requestReconciliation ?? throw new ArgumentNullException(nameof(requestReconciliation));

    internal bool Active
    {
        get { lock (_sync) return _runtime.Active; }
    }

    internal bool Invoked
    {
        get { lock (_sync) return _runtime.Invoked; }
    }

    internal CassettePersistenceAcceptanceAttempt Attempt
    {
        get { lock (_sync) return _runtime.Attempt; }
    }

    internal CassettePersistenceAcceptanceBaseline Baseline
    {
        get { lock (_sync) return _runtime.Baseline; }
    }

    internal void BeginEpoch(CassettePersistenceAcceptanceIdentity identity)
    {
        lock (_sync)
        {
            if (_hasEpoch && identity == _identity) return;
            _runtime.BeginEpoch(identity);
            _waves.BeginEpoch(identity);
            _identity = identity;
            _hasEpoch = identity.Pointer != 0;
            _activeWave = default;
            _hasActiveWave = false;
            _deferredLoggedWaveId = 0;
        }
    }

    internal bool TryEnqueue(
        CassettePersistenceAcceptanceIdentity identity,
        IEnumerable<string> songs)
    {
        lock (_sync) return _waves.TryEnqueue(identity, songs);
    }

    internal bool TryPeek(
        CassettePersistenceAcceptanceIdentity identity,
        out CassettePersistenceQueuedWave wave)
    {
        lock (_sync) return _waves.TryPeek(identity, out wave);
    }

    internal bool CanSubmitNativeCassetteGrant(bool semanticEligible)
    {
        lock (_sync) return !_runtime.Active && semanticEligible;
    }

    internal bool ShouldLogDeferred(long waveId)
    {
        lock (_sync)
        {
            if (waveId <= 0 || _deferredLoggedWaveId == waveId) return false;
            _deferredLoggedWaveId = waveId;
            return true;
        }
    }

    internal CassetteProductionPersistenceStartOutcome TryInvoke(
        CassettePersistenceAcceptanceIdentity identity,
        long waveId,
        bool eligible,
        CassettePersistenceAcceptanceBaseline baseline,
        string preDetail,
        Action onPrepared,
        Func<CassetteProductionPersistenceAdapterResult> invokeAdapter)
    {
        if (!eligible) return CassetteProductionPersistenceStartOutcome.Deferred;
        if (onPrepared == null) throw new ArgumentNullException(nameof(onPrepared));
        if (invokeAdapter == null) throw new ArgumentNullException(nameof(invokeAdapter));

        CassettePersistenceAcceptanceAttempt attempt;
        lock (_sync)
        {
            if (!_hasEpoch || identity != _identity ||
                !_waves.TryPeek(identity, out CassettePersistenceQueuedWave wave) ||
                wave.WaveId != waveId || !_runtime.TryPrepare(identity, baseline, out attempt))
                return CassetteProductionPersistenceStartOutcome.Stale;
            if (!_markers.TryBeginAttempt(attempt, preDetail))
            {
                _runtime.MarkIndeterminate(attempt, "marker-pre-admission");
                _waves.TombstoneEpoch(identity);
                _activeWave = default;
                _hasActiveWave = false;
                return CassetteProductionPersistenceStartOutcome.Indeterminate;
            }
            _activeWave = wave;
            _hasActiveWave = true;
        }

        onPrepared();

        CassetteProductionPersistenceAdapterResult result;
        try
        {
            result = invokeAdapter();
        }
        catch (Exception ex)
        {
            result = new(false, "adapter-callback-throw", $"{ex.GetType().Name}:{ex.Message}");
        }

        lock (_sync)
        {
            if (!_runtime.Active || _runtime.Attempt != attempt)
                return CassetteProductionPersistenceStartOutcome.Stale;
            if (!result.Invoked)
            {
                _runtime.MarkIndeterminate(attempt, result.Stage);
                _waves.TombstoneEpoch(identity);
                _markers.TryAppend(
                    attempt,
                    "FAILED",
                    $"reason='{_runtime.LastFailure}' stage='{result.Stage}' detail='{result.Detail}'",
                    terminal: true);
                _markers.RetireAttempt(attempt);
                _activeWave = default;
                _hasActiveWave = false;
                return CassetteProductionPersistenceStartOutcome.Indeterminate;
            }
            if (!_runtime.MarkInvoked(attempt))
                return CassetteProductionPersistenceStartOutcome.Stale;
            _markers.TryAppend(attempt, "INVOKED", result.Detail, terminal: false);
            return CassetteProductionPersistenceStartOutcome.Invoked;
        }
    }

    internal CassettePersistenceAcceptanceOutcome Observe(
        CassettePersistenceAcceptanceAttempt attempt,
        CassettePersistenceAcceptanceIdentity currentIdentity,
        CassettePublicWriteDiagnosticState state,
        bool statusesRetained,
        bool stateReadable,
        TimeSpan elapsed,
        string phase,
        string targetStage,
        string writeStage,
        string stateDetail)
    {
        bool reconcile = false;
        CassettePersistenceAcceptanceOutcome outcome;
        lock (_sync)
        {
            outcome = _runtime.Observe(
                attempt, currentIdentity, state, statusesRetained, stateReadable,
                elapsed);
            if (outcome != CassettePersistenceAcceptanceOutcome.None &&
                string.Equals(phase, "IMMEDIATE POST", StringComparison.Ordinal))
                _markers.TryAppend(
                    attempt,
                    "IMMEDIATE POST",
                    $"targetStage='{targetStage}' writeStage='{writeStage}' {stateDetail}",
                    terminal: false);
            if (outcome == CassettePersistenceAcceptanceOutcome.Verified)
            {
                if (_hasActiveWave && _activeWave.Identity == attempt.Identity)
                    _waves.Complete(_activeWave);
                _markers.TryAppend(attempt, "VERIFIED", stateDetail, terminal: true);
                _markers.RetireAttempt(attempt);
                _activeWave = default;
                _hasActiveWave = false;
                _deferredLoggedWaveId = 0;
                reconcile = true;
            }
            else if (outcome is CassettePersistenceAcceptanceOutcome.Failed or
                     CassettePersistenceAcceptanceOutcome.Timeout)
            {
                _waves.TombstoneEpoch(attempt.Identity);
                string marker = outcome == CassettePersistenceAcceptanceOutcome.Timeout
                    ? "TIMEOUT"
                    : "FAILED";
                _markers.TryAppend(
                    attempt,
                    marker,
                    $"reason='{_runtime.LastFailure}' targetStage='{targetStage}' writeStage='{writeStage}'",
                    terminal: true);
                _markers.RetireAttempt(attempt);
                _activeWave = default;
                _hasActiveWave = false;
            }
        }
        if (reconcile) _requestReconciliation();
        return outcome;
    }

    internal CassettePersistenceAcceptanceOutcome CancelEpoch(
        CassettePersistenceAcceptanceIdentity identity,
        string reason)
    {
        lock (_sync)
        {
            if (!_hasEpoch || identity != _identity)
                return CassettePersistenceAcceptanceOutcome.None;
            CassettePersistenceAcceptanceAttempt attempt = _runtime.Attempt;
            CassettePersistenceAcceptanceOutcome outcome = _runtime.Cancel(reason);
            if (outcome == CassettePersistenceAcceptanceOutcome.Cancelled)
                _markers.TryAppend(
                    attempt, "CANCELLED", $"reason='{reason}'", terminal: true);
            _waves.CancelEpoch(identity);
            _markers.RetireAttempt(attempt);
            _identity = default;
            _hasEpoch = false;
            _activeWave = default;
            _hasActiveWave = false;
            _deferredLoggedWaveId = 0;
            return outcome;
        }
    }

    internal bool TryDequeueMarker(out CassettePersistenceAcceptanceMarkerRecord marker)
    {
        lock (_sync) return _markers.TryDequeue(out marker);
    }
}

// A deferred immutable wave stays queued, but its native ownership reads must
// not run at render frequency. A new wave/save gets one immediate attempt.
internal sealed class CassettePersistencePollScheduler
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
    private CassettePersistenceAcceptanceIdentity _identity;
    private long _waveId;
    private bool _hasWave;
    private TimeSpan _remaining;

    internal bool TryBegin(CassettePersistenceAcceptanceIdentity identity, long waveId, TimeSpan elapsed)
    {
        if (identity.Pointer == 0 || waveId <= 0) return false;
        if (!_hasWave || _identity != identity || _waveId != waveId)
        {
            _identity = identity;
            _waveId = waveId;
            _hasWave = true;
            _remaining = Interval;
            return true;
        }
        if (elapsed > TimeSpan.Zero)
            _remaining = elapsed >= _remaining ? TimeSpan.Zero : _remaining - elapsed;
        if (_remaining > TimeSpan.Zero) return false;
        _remaining = Interval;
        return true;
    }
}
