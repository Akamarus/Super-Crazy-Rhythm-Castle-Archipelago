namespace RhythmCastleAP;

internal enum GarageCartridgeDiagnosticLevel
{
    Info,
    Warning,
}

internal readonly record struct GarageCartridgeDiagnostic(
    GarageCartridgeDiagnosticLevel Level,
    string Message);

internal static class GarageCartridgeDiagnostics
{
    internal const string SyncPendingMarker = "GAME GARAGE INSERTION SYNC pending";
    internal const string SyncReadyMarker = "GAME GARAGE INSERTION SYNC ready";
    internal const string SyncFailedMarker = "GAME GARAGE INSERTION SYNC failed";
    internal const string NativeGrantAppliedMarker = "GAME GARAGE CARTRIDGE NATIVE GRANT APPLIED";
    internal const string CandidateArmedMarker = "GAME GARAGE INSERTION CANDIDATE ARMED";
    internal const string NativeConsumptionObservedMarker = "GAME GARAGE NATIVE CONSUMPTION OBSERVED";
    internal const string ServerWritePendingMarker = "GAME GARAGE SERVER INSERTION WRITE PENDING";
    internal const string ServerConfirmedDurableMarker = "GAME GARAGE SERVER INSERTION CONFIRMED DURABLE";
    internal const string AlreadyInsertedNoRegrantMarker = "GAME GARAGE ALREADY INSERTED NO REGRANT";

    internal static GarageCartridgeDiagnostic SyncPending(long generation) =>
        Info($"{SyncPendingMarker} generation={generation}.");

    internal static GarageCartridgeDiagnostic SyncReady(long generation) =>
        Info($"{SyncReadyMarker} generation={generation}.");

    internal static GarageCartridgeDiagnostic SyncFailed(long generation, string detail) =>
        Warning($"{SyncFailedMarker} generation={generation}. {detail}");

    internal static GarageCartridgeDiagnostic NativeGrantApplied(
        long generation,
        string song,
        string nativeBagFlag,
        string detail) =>
        Warning(
            $"{NativeGrantAppliedMarker} generation={generation} song='{song}' flag='{nativeBagFlag}'. " +
            $"The normal Garage insertion path remains player-controlled. {detail}");

    internal static GarageCartridgeDiagnostic CandidateArmed(string song) =>
        Info($"{CandidateArmedMarker} song='{song}'.");

    internal static GarageCartridgeDiagnostic NativeConsumptionObserved(string song) =>
        Warning($"{NativeConsumptionObservedMarker} song='{song}'.");

    internal static GarageCartridgeDiagnostic ServerWritePending(string song) =>
        Warning($"{ServerWritePendingMarker} song='{song}'.");

    internal static GarageCartridgeDiagnostic ServerConfirmedDurable(string song) =>
        Warning($"{ServerConfirmedDurableMarker} song='{song}'.");

    internal static GarageCartridgeDiagnostic AlreadyInsertedNoRegrant(long generation, string song) =>
        Info($"{AlreadyInsertedNoRegrantMarker} generation={generation} song='{song}'.");

    private static GarageCartridgeDiagnostic Info(string message) =>
        new(GarageCartridgeDiagnosticLevel.Info, $"[SCRC-AP] {message}");

    private static GarageCartridgeDiagnostic Warning(string message) =>
        new(GarageCartridgeDiagnosticLevel.Warning, $"[SCRC-AP] {message}");
}
