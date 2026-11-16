namespace RhythmCastleAP;

internal static class PointTestInputs
{
    private static MusicLabPointTotals Tally(IEnumerable<long> ids)
    {
        var totals = new MusicLabPointTotals();
        foreach (long id in ids) totals.Add(id);
        return totals;
    }

    internal static MusicLabPointSnapshot Publish(long generation, IEnumerable<long> ids) =>
        MusicLabPointRandomization.SynchronizeTotals(generation, Tally(ids));

    internal static void Synchronize(this MusicLabPointRuntime runtime, MusicLabPointSessionIdentity identity, IEnumerable<long> ids) =>
        runtime.Synchronize(identity, Tally(ids));
}
