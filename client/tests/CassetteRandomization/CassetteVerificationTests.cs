using RhythmCastleAP;

internal static class CassetteVerificationTests
{
    internal static void Run(Dictionary<string, object> slotData)
    {
        var state = new RecordingSave();
        CassetteReceiptRandomization.Configure();
        try
        {
            PlayerSaveManagementEnquiries.SelectedState = state;
            PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
            CassetteReceiptRandomization.ApplySlotData(CassetteSlotDataCompatibility.Validate(slotData), bundled: false);
            CassetteReceiptRandomization.CapturePlayerSaveRequestProcessor(new PlayerSaveRequestProcessor(state));
            var owner = new SaveDataRequestProcessor(new IntPtr(0x444), new object());
            CassetteReceiptRandomization.QueueSaveBoundarySignal(4, CassetteSaveBoundarySignalKind.Selection, owner);
            Tick(0);
            Tick(0);

            Receive("I_GOT_MONEY");
            Receive("HEAVY_METAL");
            Receive("ON_THE_WAY");
            Tick(500);
            state.Expect(); // No native reads or grants before gameplay is ready.
            CassetteReceiptRandomization.ObserveGameplayReadyMarker(true, 1);
            if (!CassetteReceiptRandomization.TryCaptureGameplayReadyObservation(out var observation))
                throw new InvalidOperationException("Test save did not reach gameplay readiness");
            CassetteReceiptRandomization.ObserveGameplayReady(observation);
            Tick(0);
            state.Expect("read HEAVY_METAL", "grant HEAVY_METAL", "read I_GOT_MONEY", "grant I_GOT_MONEY",
                "read ON_THE_WAY", "grant ON_THE_WAY");

            Tick(100);
            state.Expect();
            Receive("BADASS"); // Alphabetically first, but submitted 100 ms after the others.
            Tick(0);
            state.Expect("read BADASS", "grant BADASS", "read HEAVY_METAL", "read I_GOT_MONEY", "read ON_THE_WAY");

            PlayerSaveManagementEnquiries.ValidExistingSaveSelected = false;
            CassetteReceiptRandomization.OnLifecyclePoint("test save temporarily unavailable");
            Tick(300);
            state.Expect();
            PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
            CassetteReceiptRandomization.OnLifecyclePoint("test save available again");
            Tick(149);
            state.Expect("read BADASS", "read HEAVY_METAL", "read I_GOT_MONEY", "read ON_THE_WAY");
            state.Unreadable = ePlayableSong.ON_THE_WAY;
            Tick(1);
            state.Expect("read HEAVY_METAL", "read I_GOT_MONEY", "read ON_THE_WAY", "persistence");
            ExpectQueuedBatch(observation.Identity, "HEAVY_METAL", "I_GOT_MONEY");
            Tick(99);
            state.Expect();
            Tick(1);
            state.Expect("read BADASS");
            Tick(1);
            state.Expect();
            CassetteReceiptRandomization.OnLifecyclePoint("retry pending receipts");
            Tick(0);
            state.Expect("read ON_THE_WAY");

            Receive("QUIERES_BAILAR");
            Tick(0);
            state.Expect("read ON_THE_WAY", "read QUIERES_BAILAR", "grant QUIERES_BAILAR");
            CassetteReceiptRandomization.SuspendGameplayReadinessForBoundary();
            Tick(500);
            state.Expect(); // Even a previously submitted grant must wait when the player starts switching saves.
        }
        finally
        {
            CassetteReceiptRandomization.Configure();
            PlayerSaveManagementEnquiries.ResetDiagnosticState();
        }
        Console.WriteLine("PASS: cassette verification deadlines, song order and persistence handoff");
    }

    private static void Tick(int milliseconds) =>
        CassetteReceiptRandomization.TickUnity(TimeSpan.FromMilliseconds(milliseconds));

    private static void Receive(string song)
    {
        if (!CassetteReceiptRandomization.TryApplyItem(CassetteCatalog.ByNativeSong[song].ItemName))
            throw new InvalidOperationException($"Cassette receipt rejected for {song}");
    }

    private static void ExpectQueuedBatch(CassetteGameplayReadyIdentity save, params string[] songs)
    {
        // The simulated save stops at persistence eligibility. Inspect the pending batch through the
        // real coordinator's read-only API, without adding a test accessor to the shipped plugin.
        var field = typeof(CassetteReceiptRandomization)
            .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Single(field => field.FieldType == typeof(CassetteProductionPersistenceCoordinator));
        var coordinator = (CassetteProductionPersistenceCoordinator)field.GetValue(null)!;
        if (!coordinator.TryPeek(new(save.Generation, save.Epoch, save.Slot, save.Pointer), out var batch))
            throw new InvalidOperationException("No verified cassette batch queued for the active save");
        if (!batch.Songs.SequenceEqual(songs))
            throw new InvalidOperationException($"Expected queued [{string.Join(", ", songs)}], observed [{string.Join(", ", batch.Songs)}]");
    }

    private sealed class PlayerSaveRequestProcessor(RecordingSave state)
    {
        public RecordingSave ObtainState() => state;
        public void ProcessRequest(RecordSongCassetteStatusInSaveDataRequest request) =>
            state.Grant(request.Song, request.CassetteStatus);
    }

    private sealed class RecordingSave
    {
        private readonly Dictionary<ePlayableSong, eSongCassetteStatus> _inventory = new();
        private readonly List<string> _events = new();
        public IntPtr Pointer => new(0x700);
        internal ePlayableSong? Unreadable { get; set; }

        public eSongCassetteStatus GetCassetteStatusForSong(ePlayableSong song)
        {
            _events.Add($"read {song}");
            if (song == Unreadable) throw new InvalidOperationException("simulated unreadable cassette");
            return _inventory.GetValueOrDefault(song, eSongCassetteStatus.HAVE_NOT_EARNED);
        }

        public void Grant(ePlayableSong song, eSongCassetteStatus status)
        {
            _events.Add($"grant {song}");
            _inventory[song] = status;
        }

        // The native adapter reads this property when it checks eligibility to persist a verified batch.
        public bool HasUnstagedChanges
        {
            get { _events.Add("persistence"); return false; }
        }

        internal void Expect(params string[] expected)
        {
            if (!_events.SequenceEqual(expected))
                throw new InvalidOperationException($"Expected [{string.Join(", ", expected)}], observed [{string.Join(", ", _events)}]");
            _events.Clear();
        }
    }
}

namespace RhythmCastleAP
{
    internal static class Plugin { internal static TestLogger? LoggerInstance => null; }
    internal sealed class TestLogger
    {
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogDebug(string message) { }
    }
}

namespace UnityEngine
{
    internal static class Time { internal static int frameCount => 0; }
}
