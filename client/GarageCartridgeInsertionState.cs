namespace RhythmCastleAP;

internal enum GarageInsertionServerValue
{
    Unknown,
    NotInserted,
    Inserted,
}

internal enum GarageNativeGrantDecision
{
    None,
    WaitForServer,
    WaitForNativeRead,
    AlreadyHeld,
    AlreadyInserted,
    ApplyBagItem,
}

internal readonly record struct GarageInsertionObservation(
    bool Compatible,
    bool ApOwned,
    bool UsesPhysicalVanillaEntrance,
    GarageInsertionServerValue ServerValue,
    bool InGarage,
    bool ReleasedThisVisit,
    bool PreviousBagReadable,
    bool PreviousBagHeld,
    bool CurrentBagReadable,
    bool CurrentBagHeld);

internal static class GarageCartridgeInsertionPolicy
{
    internal static GarageNativeGrantDecision DecideGrant(
        bool compatible,
        bool apOwned,
        bool usesPhysicalVanillaEntrance,
        GarageInsertionServerValue serverValue,
        bool nativeBagReadable,
        bool nativeBagHeld)
    {
        if (!compatible || !apOwned || usesPhysicalVanillaEntrance)
            return GarageNativeGrantDecision.None;
        if (serverValue != GarageInsertionServerValue.NotInserted && serverValue != GarageInsertionServerValue.Inserted)
            return GarageNativeGrantDecision.WaitForServer;
        if (!nativeBagReadable)
            return GarageNativeGrantDecision.WaitForNativeRead;
        if (serverValue == GarageInsertionServerValue.Inserted)
            return GarageNativeGrantDecision.AlreadyInserted;
        if (nativeBagHeld)
            return GarageNativeGrantDecision.AlreadyHeld;
        return GarageNativeGrantDecision.ApplyBagItem;
    }

    internal static bool ShouldRecordInsertion(GarageInsertionObservation observation) =>
        observation.Compatible &&
        observation.ApOwned &&
        !observation.UsesPhysicalVanillaEntrance &&
        observation.ServerValue == GarageInsertionServerValue.NotInserted &&
        observation.InGarage &&
        observation.ReleasedThisVisit &&
        observation.PreviousBagReadable &&
        observation.PreviousBagHeld &&
        observation.CurrentBagReadable &&
        !observation.CurrentBagHeld;

    internal static bool ShouldReleaseObject(
        bool usesPhysicalVanillaEntrance,
        GarageInsertionServerValue serverValue,
        bool currentVisitNativeBagHeldObserved) =>
        usesPhysicalVanillaEntrance ||
        (currentVisitNativeBagHeldObserved && serverValue == GarageInsertionServerValue.NotInserted);
}

internal sealed class GarageCartridgeInsertionTracker
{
    private readonly Dictionary<string, GarageNativeBagObservation> _previous =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GaragePendingNativeConsumption> _pendingConsumption =
        new(StringComparer.OrdinalIgnoreCase);

    internal bool Observe(
        string song,
        bool compatible,
        bool apOwned,
        bool usesPhysicalVanillaEntrance,
        GarageInsertionServerValue serverValue,
        bool readable,
        bool held,
        bool inGarage,
        bool releasedThisVisit)
    {
        if (!compatible || !apOwned || usesPhysicalVanillaEntrance)
        {
            _previous.Remove(song);
            return false;
        }

        _previous.TryGetValue(song, out GarageNativeBagObservation previous);
        var observation = new GarageInsertionObservation(
            compatible,
            apOwned,
            usesPhysicalVanillaEntrance,
            serverValue,
            inGarage,
            releasedThisVisit,
            previous.Readable,
            previous.Held,
            readable,
            held);
        if (readable)
            _previous[song] = new GarageNativeBagObservation(true, held);
        return GarageCartridgeInsertionPolicy.ShouldRecordInsertion(observation);
    }

    internal bool HasAuthoritativeHeldObservation(string song) =>
        _previous.TryGetValue(song, out GarageNativeBagObservation observation) &&
        observation.Readable &&
        observation.Held;

    internal bool TryArmPendingConsumption(
        string song,
        bool? signalIsSet,
        bool? signalWasSet,
        bool requirePreviouslySet,
        bool compatible,
        bool apOwned,
        bool usesPhysicalVanillaEntrance,
        GarageInsertionServerValue serverValue,
        bool inGarage,
        bool releasedThisVisit,
        long generation,
        long resetEpoch)
    {
        if (signalIsSet != false ||
            (requirePreviouslySet && signalWasSet != true) ||
            !compatible ||
            !apOwned ||
            usesPhysicalVanillaEntrance ||
            serverValue != GarageInsertionServerValue.NotInserted ||
            !inGarage ||
            !releasedThisVisit ||
            !HasAuthoritativeHeldObservation(song))
        {
            return false;
        }

        _pendingConsumption[song] = new GaragePendingNativeConsumption(
            generation,
            resetEpoch);
        return true;
    }

    internal bool HasPendingConsumption(string song, long generation, long resetEpoch) =>
        _pendingConsumption.TryGetValue(song, out GaragePendingNativeConsumption pending) &&
        pending.Generation == generation &&
        pending.ResetEpoch == resetEpoch;

    internal bool TryResolvePendingConsumption(
        string song,
        long generation,
        long resetEpoch,
        GarageInsertionServerValue serverValue,
        bool readable,
        bool held,
        out bool confirmed)
    {
        confirmed = false;
        if (!HasPendingConsumption(song, generation, resetEpoch))
            return false;

        if (serverValue == GarageInsertionServerValue.Inserted)
        {
            _pendingConsumption.Remove(song);
            return true;
        }

        if (serverValue != GarageInsertionServerValue.NotInserted || !readable)
            return true;

        _pendingConsumption.Remove(song);
        confirmed = !held;
        return true;
    }

    internal void Reset()
    {
        _previous.Clear();
        _pendingConsumption.Clear();
    }

    internal void ResetForRoomTransition(
        long generation,
        long previousResetEpoch,
        long resetEpoch)
    {
        _previous.Clear();

        var carry = new List<string>();
        var remove = new List<string>();
        foreach ((string song, GaragePendingNativeConsumption pending) in _pendingConsumption)
        {
            if (pending.Generation == generation &&
                pending.ResetEpoch == previousResetEpoch)
            {
                carry.Add(song);
            }
            else
            {
                remove.Add(song);
            }
        }

        foreach (string song in carry)
        {
            GaragePendingNativeConsumption pending = _pendingConsumption[song];
            _pendingConsumption[song] = pending with
            {
                ResetEpoch = resetEpoch,
            };
        }
        foreach (string song in remove)
            _pendingConsumption.Remove(song);
    }

    private readonly record struct GarageNativeBagObservation(bool Readable, bool Held);
    private readonly record struct GaragePendingNativeConsumption(
        long Generation,
        long ResetEpoch);
}

internal readonly record struct GarageNativeGrantSubmission(
    bool Submitted,
    string Detail);

internal readonly record struct GarageCartridgeReconciliationResult(
    bool RecordedInsertion,
    bool CandidateArmed,
    GarageNativeGrantDecision GrantDecision,
    bool GrantAttempted,
    bool GrantSubmitted,
    string GrantDetail);

internal sealed class GarageCartridgeReconciliationAdapter
{
    private readonly object _gate = new();
    private readonly GarageCartridgeInsertionTracker _tracker = new();
    private long _resetEpoch;

    internal long ResetEpoch
    {
        get
        {
            lock (_gate)
                return _resetEpoch;
        }
    }

    internal bool HasAuthoritativeHeldObservation(string song)
    {
        lock (_gate)
            return _tracker.HasAuthoritativeHeldObservation(song);
    }

    internal bool HasPendingConsumption(string song, long generation)
    {
        lock (_gate)
            return _tracker.HasPendingConsumption(song, generation, _resetEpoch);
    }

    internal bool RecordProgressionRequest(
        string flag,
        bool? value,
        bool compatible,
        bool apOwned,
        GarageInsertionServerValue serverValue,
        bool inGarage,
        bool releasedThisVisit,
        long generation) =>
        TryArmPendingConsumption(
            flag,
            value,
            signalWasSet: null,
            requirePreviouslySet: false,
            compatible,
            apOwned,
            serverValue,
            inGarage,
            releasedThisVisit,
            generation);

    internal bool RecordProgressionFlagUpdated(
        string flag,
        bool? flagIsSet,
        bool? flagWasSet,
        bool compatible,
        bool apOwned,
        GarageInsertionServerValue serverValue,
        bool inGarage,
        bool releasedThisVisit,
        long generation) =>
        TryArmPendingConsumption(
            flag,
            flagIsSet,
            flagWasSet,
            requirePreviouslySet: true,
            compatible,
            apOwned,
            serverValue,
            inGarage,
            releasedThisVisit,
            generation);

    internal GarageCartridgeReconciliationResult Reconcile(
        string song,
        bool compatible,
        bool apOwned,
        bool usesPhysicalVanillaEntrance,
        GarageInsertionServerValue serverValue,
        bool readable,
        bool held,
        bool inGarage,
        bool releasedThisVisit,
        long generation,
        Action<string> noteInserted,
        Func<bool, GarageNativeGrantSubmission>? submitGrant)
    {
        ArgumentNullException.ThrowIfNull(noteInserted);

        bool recordInsertion;
        bool candidateArmed;
        lock (_gate)
        {
            _tracker.TryResolvePendingConsumption(
                song,
                generation,
                _resetEpoch,
                serverValue,
                readable,
                held,
                out bool pendingConfirmed);
            bool transitionConfirmed = _tracker.Observe(
                song,
                compatible,
                apOwned,
                usesPhysicalVanillaEntrance,
                serverValue,
                readable,
                held,
                inGarage,
                releasedThisVisit);
            recordInsertion = pendingConfirmed || transitionConfirmed;
            candidateArmed = compatible &&
                apOwned &&
                !usesPhysicalVanillaEntrance &&
                serverValue == GarageInsertionServerValue.NotInserted &&
                inGarage &&
                releasedThisVisit &&
                readable &&
                held;
        }

        if (recordInsertion)
            noteInserted(song);

        GarageInsertionServerValue effectiveServerValue = recordInsertion
            ? GarageInsertionServerValue.Inserted
            : serverValue;
        GarageNativeGrantDecision decision = GarageCartridgeInsertionPolicy.DecideGrant(
            compatible,
            apOwned,
            usesPhysicalVanillaEntrance,
            effectiveServerValue,
            readable,
            held);
        if (decision != GarageNativeGrantDecision.ApplyBagItem || submitGrant == null)
        {
            return new GarageCartridgeReconciliationResult(
                recordInsertion,
                candidateArmed,
                decision,
                GrantAttempted: false,
                GrantSubmitted: false,
                GrantDetail: string.Empty);
        }

        GarageNativeGrantSubmission submission = submitGrant(true);
        return new GarageCartridgeReconciliationResult(
            recordInsertion,
            candidateArmed,
            decision,
            GrantAttempted: true,
            submission.Submitted,
            submission.Detail);
    }

    internal void Reset()
    {
        lock (_gate)
        {
            _resetEpoch++;
            _tracker.Reset();
        }
    }

    internal void ResetForRoomTransition(long generation)
    {
        lock (_gate)
        {
            long previousResetEpoch = _resetEpoch;
            _resetEpoch++;
            _tracker.ResetForRoomTransition(generation, previousResetEpoch, _resetEpoch);
        }
    }

    private bool TryArmPendingConsumption(
        string flag,
        bool? signalIsSet,
        bool? signalWasSet,
        bool requirePreviouslySet,
        bool compatible,
        bool apOwned,
        GarageInsertionServerValue serverValue,
        bool inGarage,
        bool releasedThisVisit,
        long generation)
    {
        GarageCartridgeProgressionFlag? classification =
            GarageCartridgeNativePolicy.ClassifyProgressionFlag(flag);
        if (classification is not { Kind: GarageCartridgeProgressionFlagKind.BagItem } classified)
            return false;

        GarageCartridgeNativeDefinition cartridge = classified.Cartridge;
        lock (_gate)
        {
            return _tracker.TryArmPendingConsumption(
                cartridge.Song,
                signalIsSet,
                signalWasSet,
                requirePreviouslySet,
                compatible,
                apOwned,
                cartridge.UsesPhysicalVanillaEntrance,
                serverValue,
                inGarage,
                releasedThisVisit,
                generation,
                _resetEpoch);
        }
    }
}

internal sealed class GarageCartridgeReconciliationAccess
{
    private readonly GenerationLeaseGate _lifecycle = new();

    internal bool TryBegin(long generation, Action begin) =>
        _lifecycle.TryBegin(generation, begin);

    internal bool End(long generation, Action end) =>
        _lifecycle.End(generation, end);

    internal bool EndActive(Action<long> end) =>
        _lifecycle.EndActive(end);

    internal bool TryRunCurrent(Action<GarageCartridgeReconciliationLease> reconcile)
    {
        ArgumentNullException.ThrowIfNull(reconcile);
        if (!_lifecycle.TryAcquireCurrent(out long generation, out LifecycleLease? lifecycleLease))
            return false;

        using (lifecycleLease)
        {
            var reconciliationLease = new GarageCartridgeReconciliationLease(generation);
            try
            {
                reconcile(reconciliationLease);
                return true;
            }
            finally
            {
                reconciliationLease.Invalidate();
            }
        }
    }
}

internal sealed class GarageCartridgeReconciliationLease
{
    private bool _active = true;

    internal GarageCartridgeReconciliationLease(long generation)
    {
        Generation = generation;
    }

    internal long Generation { get; }

    internal void Observe(Action observation)
    {
        Execute(observation);
    }

    internal void Release(Action release)
    {
        Execute(release);
    }

    private void Execute(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (!_active)
            throw new ObjectDisposedException(nameof(GarageCartridgeReconciliationLease));
        action();
    }

    internal void Invalidate() => _active = false;
}

internal sealed class GarageCartridgeReleaseVisitCoordinator
{
    private readonly HashSet<string> _released = new(StringComparer.OrdinalIgnoreCase);
    private long _resetEpoch = long.MinValue;

    internal void SynchronizeResetEpoch(long resetEpoch)
    {
        if (_resetEpoch == resetEpoch)
            return;

        _released.Clear();
        _resetEpoch = resetEpoch;
    }

    internal bool Poll(
        string song,
        Func<bool, Action, bool> tryRelease,
        Action releaseAction,
        Action deactivateAction)
    {
        if (string.IsNullOrWhiteSpace(song))
            throw new ArgumentException("Garage cartridge song must be non-empty.", nameof(song));
        ArgumentNullException.ThrowIfNull(tryRelease);
        ArgumentNullException.ThrowIfNull(releaseAction);
        ArgumentNullException.ThrowIfNull(deactivateAction);

        bool releasedThisVisit = _released.Contains(song);
        bool accepted = tryRelease(releasedThisVisit, () =>
        {
            releaseAction();
            _released.Add(song);
        });
        if (accepted)
            return true;

        deactivateAction();
        _released.Remove(song);
        return false;
    }

    internal bool WasReleased(string song) => _released.Contains(song);

    internal void Clear(string song) => _released.Remove(song);

    internal void Clear() => _released.Clear();
}
