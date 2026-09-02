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
        GarageInsertionServerValue serverValue) =>
        usesPhysicalVanillaEntrance || serverValue == GarageInsertionServerValue.NotInserted;
}

internal sealed class GarageCartridgeInsertionTracker
{
    private readonly Dictionary<string, GarageNativeBagObservation> _previous =
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

    internal void Reset() => _previous.Clear();

    private readonly record struct GarageNativeBagObservation(bool Readable, bool Held);
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
        ArgumentNullException.ThrowIfNull(observation);
        if (!_active)
            throw new ObjectDisposedException(nameof(GarageCartridgeReconciliationLease));
        observation();
    }

    internal void Invalidate() => _active = false;
}
