namespace RhythmCastleAP;

internal static class GetterAvailabilityTests
{
    internal static void Run(Func<Dictionary<string, object>> contract)
    {
        MusicLabPointRandomization.Reset();
        MusicLabPointRandomization.ReportGetterAvailability(false);
        Apply(200);
        Check(MusicLabPointRuntimeMode.Incompatible, MusicLabPointRandomization.Snapshot.Mode, "missing getter must reject a valid claimed contract");
        Check(false, new ConnectionStatusProbe(true).Connected, "production connection status cannot advertise unusable points");
        if (!MusicLabPointRandomization.Snapshot.Detail.Contains("getter unavailable", StringComparison.Ordinal))
            throw new InvalidOperationException("getter incompatibility must explain the unavailable getter");
        MusicLabPointRandomization.SynchronizeHistory(200, new[] { new MusicLabPointReceipt(0, 187256155) });
        Check(0, MusicLabPointRandomization.ResolveEffectiveScore(99, 140), "missing getter cannot accept AP receipts or native fallback");
        MusicLabPointRandomization.ReportGetterAvailability(true);
        Apply(200);
        Check(MusicLabPointRuntimeMode.Incompatible, MusicLabPointRandomization.Snapshot.Mode, "availability cannot revive the rejected generation");
        Apply(201);
        Check(MusicLabPointRuntimeMode.AwaitingInitialSynchronization, MusicLabPointRandomization.Snapshot.Mode, "new session may use a verified getter");
        MusicLabPointRandomization.SynchronizeHistory(201, new[] { new MusicLabPointReceipt(0, 187256155) });
        Check(20, MusicLabPointRandomization.Snapshot.Total, "verified getter enables synchronized AP points");
        Check(true, new ConnectionStatusProbe(true).Connected, "valid connected session is usable");
        Check(false, new ConnectionStatusProbe(false).Connected, "valid contract cannot invent a connection");
        MusicLabPointRandomization.ReportGetterAvailability(false);
        Check(MusicLabPointRuntimeMode.Incompatible, MusicLabPointRandomization.Snapshot.Mode, "getter failure after configuration revokes compatibility");
        Check(false, new ConnectionStatusProbe(true).Connected, "getter failure withdraws usable connection status");
        Check(0, MusicLabPointRandomization.ResolveEffectiveScore(99, 140), "getter failure clears the synchronized score");
        MusicLabPointRandomization.ReportGetterAvailability(true);
        MusicLabPointRandomization.SynchronizeHistory(201, new[] { new MusicLabPointReceipt(0, 187256155) });
        Check(0, MusicLabPointRandomization.Snapshot.Total, "late availability and receipt callbacks cannot revive the failed generation");
        Apply(201, "different-identity");
        Check(MusicLabPointRuntimeMode.AwaitingInitialSynchronization, MusicLabPointRandomization.Snapshot.Mode, "new identity may validate recovered getter");
        MusicLabPointRandomization.ReportGetterAvailability(false);
        MusicLabPointRandomization.ApplySlotData(null, "game", "legacy", "slot", 202);
        Check(99, MusicLabPointRandomization.ResolveEffectiveScore(99, null), "legacy sessions preserve native score without getter availability");
        MusicLabPointRandomization.Reset();
        Check(99, MusicLabPointRandomization.ResolveEffectiveScore(99, null), "non-AP preserves native score without getter availability");
        MusicLabPointRandomization.ReportGetterAvailability(true);
        Console.WriteLine("PASS: getter_availability_enforces_compatibility_and_safe_recovery");

        void Apply(long generation, string seed = "getter-seed") =>
            MusicLabPointRandomization.ApplySlotData(contract(), "game", seed, "slot", generation);
    }

    private static void Check<T>(T want, T got, string scenario)
    {
        if (!EqualityComparer<T>.Default.Equals(want, got))
            throw new InvalidOperationException($"{scenario}: expected {want}, got {got}");
    }
}
