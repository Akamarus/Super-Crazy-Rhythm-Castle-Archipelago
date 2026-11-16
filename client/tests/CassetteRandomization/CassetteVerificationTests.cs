using RhythmCastleAP;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

internal static class CassetteVerificationTests
{
    internal static void Run(Dictionary<string, object> slotData)
    {
        var state = new RecordingSave();
        CassetteReceiptRandomization.Configure();
        try
        {
            SelectSave(slotData, state);

            Receive("I_GOT_MONEY");
            Receive("HEAVY_METAL");
            Receive("ON_THE_WAY");
            Tick(500);
            state.Expect(); // No native reads or grants before gameplay is ready.
            var identity = EnterGameplay();
            Tick(0);
            if (CassetteReceiptRandomization.TryPrepareUnload()) throw new Exception("Live unload discarded submitted cassette grants");
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
            ExpectQueuedBatch(identity, "HEAVY_METAL", "I_GOT_MONEY");
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

            VerifyReadRecovery(slotData, deposited: false);
            VerifyReadRecovery(slotData, deposited: true);
            VerifyColdResume(slotData);
            CassetteReceiptRandomization.Configure();
            var legacy = new RecordingSave();
            SelectSave(slotData, legacy);
            var legacySkip = new CassetteSlotCompatibilityResult(false, "historical room without cassette routing");
            CassetteReceiptRandomization.ApplySlotData(legacySkip, bundled: false);
            EnterGameplay();
            Tick(0);
            int boundCount = 0;
            CassetteReceiptRandomization.WithStableSelectedQuestSave((_, _, slot) => boundCount += slot == 4 ? 1 : 0);
            if (boundCount != 1) throw new Exception("Historical rooms without cassette routing must still identify their selected save");
            Receive("ON_THE_WAY");
            Tick(500);
            legacy.Expect();
            CassetteReceiptRandomization.Configure();
            var beforeSwitch = new RecordingSave();
            SelectSave(slotData, beforeSwitch);
            EnterGameplay();
            Receive("BADASS");
            Tick(0);
            var otherSave = new RecordingSave { Pointer = new IntPtr(0x701) };
            PlayerSaveManagementEnquiries.SelectedState = otherSave;
            if (!CassetteReceiptRandomization.TryPrepareUnload()) throw new Exception("A proved different selected save could not unload");
            otherSave.Expect();
            VerifyRoomReplacement(slotData);
            CassetteReceiptRandomization.Configure();
            var retrySave = new RecordingSave { DropGrant = true };
            SelectSave(slotData, retrySave);
            EnterGameplay();
            Receive("BADASS");
            Tick(0);
            retrySave.Expect("read BADASS", "grant BADASS");
            retrySave.DropGrant = false;
            Tick(250);
            retrySave.Expect("read BADASS", "grant BADASS");
            CassetteReceiptRandomization.SuspendGameplayReadinessForBoundary();
            if (!CassetteReceiptRandomization.TryPrepareUnload()) throw new Exception("An invalidated save held plugin unload indefinitely");
            VerifyBundledReconnect(slotData, 1, emptyReplay: false);
            VerifyBundledReconnect(slotData, 3, emptyReplay: true);
        }
        finally
        {
            CassetteReceiptRandomization.Configure();
            PlayerSaveManagementEnquiries.ResetDiagnosticState();
        }
        Console.WriteLine("PASS: cassette verification deadlines, song order and persistence handoff");
    }

    private static void VerifyColdResume(Dictionary<string, object> slotData)
    {
        // The Unity-free test assembly supplies the same named processor the game supplies.
        var assembly = typeof(ReflectionUtil).GetField("_gameAssembly",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var previous = assembly.GetValue(null);
        try
        {
            assembly.SetValue(null, typeof(global::PlayerSaveRequestProcessor).Assembly);
            CassetteReceiptRandomization.Configure();
            PlayerSaveManagementEnquiries.ResetDiagnosticState();
            var saves = new Dictionary<int, RegularSaveStateFixture?>();
            var owner = new SaveDataRequestProcessor(new IntPtr(0x444), new SaveDataStateFixture(saves));
            var state = new RecordingSave();
            CassetteReceiptRandomization.BeginMostRecentSelectionBoundary(owner);
            CassetteReceiptRandomization.ApplySlotData(CassetteSlotDataCompatibility.Validate(slotData), bundled: false);
            Receive("ZEN");
            Tick(2100); // The selected save is not readable yet.
            state.Expect();
            PlayerSaveManagementEnquiries.SelectedSlot = new(2);
            PlayerSaveManagementEnquiries.SelectedState = state;
            PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
            Tick(2100); // The regular-save table is still empty.
            saves[1] = saves[2] = new(state.Pointer, 777);
            Tick(2100); // The same pointer in two slots cannot establish identity.
            Tick(0);
            if (CassetteReceiptRandomization.TryCaptureGameplayReadyObservation(out _))
                throw new Exception("Ambiguous save was accepted on cold resume");
            saves[1] = new(new IntPtr(0x123), 888);
            Tick(2100);
            Tick(0);
            var identity = EnterGameplay();
            if (identity.Slot != 2) throw new Exception("Cold resume selected the wrong slot");
            NativeGrantSaveBinding.BoundSlot = 1;
            Tick(0);
            state.Expect(); // A different room's bound slot must never receive Zen.
            NativeGrantSaveBinding.BoundSlot = 2;
            Tick(2100);
            Tick(250);
            state.Expect("read ZEN", "grant ZEN", "read ZEN", "persistence");
            ExpectQueuedBatch(identity, "ZEN");

            VerifyDeliveryInRunningRoom(slotData, identifyBeforeReceipt: true);
            VerifyDeliveryInRunningRoom(slotData, identifyBeforeReceipt: false);
        }
        finally
        {
            assembly.SetValue(null, previous);
            NativeGrantSaveBinding.BoundSlot = null;
            PlayerSaveManagementEnquiries.ResetDiagnosticState();
        }
    }

    private static void VerifyDeliveryInRunningRoom(Dictionary<string, object> slotData, bool identifyBeforeReceipt)
    {
        CassetteReceiptRandomization.Configure();
        PlayerSaveManagementEnquiries.ResetDiagnosticState();
        var state = new RecordingSave();
        var saves = new Dictionary<int, RegularSaveStateFixture?>();
        var owner = new SaveDataRequestProcessor(new IntPtr(0x444), new SaveDataStateFixture(saves));
        CassetteReceiptRandomization.BeginMostRecentSelectionBoundary(owner);
        PlayerSaveManagementEnquiries.SelectedSlot = new(2);
        PlayerSaveManagementEnquiries.SelectedState = state;
        PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
        NativeGrantSaveBinding.BoundSlot = 2;
        if (identifyBeforeReceipt) saves[2] = new(state.Pointer, 777);

        CassetteGameplayReadyIdentity? identity = null;
        void StayInRoom()
        {
            Tick(250);
            // The keeper observes the same live player every time, even if AP connects later.
            CassetteReceiptRandomization.ObserveGameplayReadyMarker(true, 42);
            if (CassetteReceiptRandomization.TryCaptureGameplayReadyObservation(out var observed))
            {
                CassetteReceiptRandomization.ObserveGameplayReady(observed);
                identity = observed.Identity;
            }
        }
        for (int i = 0; i < 12; i++) StayInRoom();
        if (identity.HasValue != identifyBeforeReceipt)
            throw new InvalidOperationException("Unexpected save identity before Zen arrived");
        state.Expect();
        CassetteReceiptRandomization.ApplySlotData(CassetteSlotDataCompatibility.Validate(slotData), bundled: false);
        Receive("ZEN");
        StayInRoom();
        if (!identifyBeforeReceipt)
        {
            state.Expect(); // The game is running but the client still lacks proof of the destination.
            saves[2] = new(state.Pointer, 777);
        }
        for (int i = 0; i < 16; i++) StayInRoom();
        if (identity?.Slot != 2)
            throw new InvalidOperationException("The client did not identify slot 2 while the player stood still");
        state.ExpectSongEvents("read ZEN", "grant ZEN", "read ZEN");
        ExpectQueuedBatch(identity.Value, "ZEN");
    }

    private static void VerifyRoomReplacement(Dictionary<string, object> slotData)
    {
        CassetteReceiptRandomization.Configure();
        var state = new RecordingSave();
        SelectSave(slotData, state);
        var identity = EnterGameplay();
        Receive("I_GOT_MONEY");
        Tick(0);
        state.Expect("read I_GOT_MONEY", "grant I_GOT_MONEY");
        Tick(250);
        state.Expect("read I_GOT_MONEY", "persistence");
        ExpectQueuedBatch(identity, "I_GOT_MONEY");
        var disk = PersistenceCoordinator();
        var selected = new CassettePersistenceAcceptanceIdentity(identity.Generation, identity.Epoch, identity.Slot, identity.Pointer);
        if (!disk.TryPeek(selected, out var wave) ||
            disk.TryInvoke(selected, wave.WaveId, true, new(default, 1, 1, wave.Songs),
                "test write", () => { }, () => new(true, "invoked", "")) != CassetteProductionPersistenceStartOutcome.Invoked)
            throw new InvalidOperationException("Test could not start the selected save's write");
        var writing = disk.Attempt;
        if (CassetteReceiptRandomization.TryPrepareUnload()) throw new Exception("Live unload orphaned an invoked native write");
        disk.TryEnqueue(selected, ["ON_THE_WAY"]);
        Receive("BADASS");
        CassetteReceiptRandomization.ResetRoomOwnership(() => { });
        ExpectQueuedBatch(identity);
        if (disk.CanSubmitNativeCassetteGrant(true))
            throw new InvalidOperationException("A room change released the selected save's in-flight write");
        var written = new CassettePublicWriteDiagnosticState(default, 2, false, 1, 2, selected.Pointer,
            new Dictionary<string, string> { ["I_GOT_MONEY"] = "HAVE_IN_BAG" });
        if (disk.Observe(writing, selected, written, true, true, TimeSpan.Zero, "UPDATE", "", "", "") !=
            CassettePersistenceAcceptanceOutcome.Verified)
            throw new InvalidOperationException("The previous room's already-invoked write lost its verification");
        Receive("HEAVY_METAL");
        Tick(0);
        state.Expect("read HEAVY_METAL", "grant HEAVY_METAL");
        Tick(250);
        state.Expect("read HEAVY_METAL", "persistence");
        ExpectQueuedBatch(identity, "HEAVY_METAL");
    }

    private static void VerifyReadRecovery(Dictionary<string, object> slotData, bool deposited)
    {
        CassetteReceiptRandomization.Configure();
        var state = new RecordingSave();
        SelectSave(slotData, state);
        var identity = EnterGameplay();
        Receive("ON_THE_WAY");
        Tick(0);
        state.Expect("read ON_THE_WAY", "grant ON_THE_WAY");

        state.Unreadable = ePlayableSong.ON_THE_WAY;
        Tick(250);
        state.Expect("read ON_THE_WAY");
        Tick(249);
        state.Expect();
        Tick(1);
        state.Expect("read ON_THE_WAY");
        state.Unreadable = null;
        if (deposited) state.Deposit(ePlayableSong.ON_THE_WAY);
        CassetteReceiptRandomization.OnLifecyclePoint("ordinary reconciliation while verification is pending");
        Tick(249);
        state.Expect("read ON_THE_WAY");
        ExpectQueuedBatch(identity);
        Tick(1);
        if (deposited)
        {
            state.Expect("read ON_THE_WAY");
            ExpectQueuedBatch(identity);
        }
        else
        {
            state.Expect("read ON_THE_WAY", "persistence");
            ExpectQueuedBatch(identity, "ON_THE_WAY");
        }
        Receive("ON_THE_WAY");
        Tick(250);
        state.Expect();
    }

    private static void VerifyBundledReconnect(Dictionary<string, object> slotData, long generation, bool emptyReplay)
    {
        CassetteReceiptRandomization.Configure();
        var pairs = CassetteCatalog.All.OrderByDescending(song => song.NativeSong is "BADASS" or "HEAVY_METAL")
            .Select(song => song.ItemName).Chunk(2)
            .Select((members, index) => new KeyValuePair<string, string[]>($"Cassette Bundle {index + 1:00}", members));
        var contract = CassetteBundleContract.Parse(new() {
            ["schema_version"] = 20, ["cassette_bundle_size"] = 2, ["cassette_bundles"] = pairs.ToDictionary()
        });
        var packet = new ReceivedItemsPacket { Index = 0, Items = [new NetworkItem { Item = 187256164 }] };
        var session = new CassetteBundleSessionHistory(generation);
        var state = new RecordingSave();
        SelectSave(slotData, state, bundled: true);
        session.Configure(contract);
        session.HandlePacket(packet);
        var identity = EnterGameplay();
        Tick(0);
        state.Expect("read BADASS", "grant BADASS", "read HEAVY_METAL", "grant HEAVY_METAL");

        CassetteBundleSessionHistory.End(generation);
        Tick(500);
        state.Expect();
        var reconnect = new CassetteBundleSessionHistory(generation + 1);
        CassetteReceiptRandomization.ApplySlotData(CassetteSlotDataCompatibility.Validate(slotData), bundled: true);
        reconnect.Configure(contract);
        Tick(500);
        state.Expect();
        reconnect.HandlePacket(emptyReplay ? new ReceivedItemsPacket { Index = 0, Items = [] } : packet);
        Tick(0);
        if (emptyReplay)
        {
            state.Expect();
            reconnect.HandlePacket(packet);
            Tick(0);
        }
        state.Expect("read BADASS", "read HEAVY_METAL");
        Tick(250);
        state.Expect("read BADASS", "read HEAVY_METAL", "persistence");
        ExpectQueuedBatch(identity, "BADASS", "HEAVY_METAL");
        CassetteBundleSessionHistory.End(generation + 1);
    }

    private static void SelectSave(Dictionary<string, object> slotData, RecordingSave state, bool bundled = false)
    {
        PlayerSaveManagementEnquiries.SelectedState = state;
        PlayerSaveManagementEnquiries.ValidExistingSaveSelected = true;
        CassetteReceiptRandomization.ApplySlotData(CassetteSlotDataCompatibility.Validate(slotData), bundled);
        CassetteReceiptRandomization.CapturePlayerSaveRequestProcessor(new PlayerSaveRequestProcessor(state));
        var owner = new SaveDataRequestProcessor(new IntPtr(0x444), new object());
        CassetteReceiptRandomization.QueueSaveBoundarySignal(4, CassetteSaveBoundarySignalKind.Selection, owner);
        Tick(0);
        Tick(0);
    }

    private static CassetteGameplayReadyIdentity EnterGameplay()
    {
        CassetteReceiptRandomization.ObserveGameplayReadyMarker(true, 1);
        if (!CassetteReceiptRandomization.TryCaptureGameplayReadyObservation(out var observation))
            throw new InvalidOperationException("Test save did not reach gameplay readiness");
        CassetteReceiptRandomization.ObserveGameplayReady(observation);
        return observation.Identity;
    }

    private static void Tick(int milliseconds) =>
        CassetteReceiptRandomization.TickUnity(TimeSpan.FromMilliseconds(milliseconds));

    private static void Receive(string song)
    {
        if (!CassetteReceiptRandomization.TryApplyItem(CassetteCatalog.ByNativeSong[song].ItemName))
            throw new InvalidOperationException($"Cassette receipt rejected for {song}");
    }

    private static CassetteProductionPersistenceCoordinator PersistenceCoordinator()
    {
        // The simulated save stops at persistence eligibility. Use the real coordinator
        // to exercise that boundary without adding a test accessor to the plugin.
        var field = typeof(CassetteReceiptRandomization)
            .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Single(field => field.FieldType == typeof(CassetteProductionPersistenceCoordinator));
        return (CassetteProductionPersistenceCoordinator)field.GetValue(null)!;
    }

    private static void ExpectQueuedBatch(CassetteGameplayReadyIdentity save, params string[] songs)
    {
        var coordinator = PersistenceCoordinator();
        bool queued = coordinator.TryPeek(new(save.Generation, save.Epoch, save.Slot, save.Pointer), out var batch);
        if (songs.Length == 0 && !queued) return;
        if (!queued) throw new InvalidOperationException("No verified cassette batch queued for the active save");
        if (!batch.Songs.SequenceEqual(songs))
            throw new InvalidOperationException($"Expected queued [{string.Join(", ", songs)}], observed [{string.Join(", ", batch.Songs)}]");
    }

    private sealed class PlayerSaveRequestProcessor(RecordingSave state)
    {
        public RecordingSave ObtainState() => state;
        public void ProcessRequest(RecordSongCassetteStatusInSaveDataRequest request) =>
            state.Grant(request.Song, request.CassetteStatus);
    }

    internal sealed class RecordingSave
    {
        private readonly Dictionary<ePlayableSong, eSongCassetteStatus> _inventory = new();
        private readonly List<string> _events = new();
        public IntPtr Pointer { get; init; } = new(0x700);
        internal bool DropGrant { get; set; }
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
            if (!DropGrant) _inventory[song] = status;
        }

        internal void Deposit(ePlayableSong song) => _inventory[song] = eSongCassetteStatus.HAVE_DEPOSITED;

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

        internal void ExpectSongEvents(params string[] expected)
        {
            // Disk eligibility may be polled on subsequent frames after the bag grant.
            _events.RemoveAll(entry => entry == "persistence");
            Expect(expected);
        }
    }
}

namespace RhythmCastleAP
{
    internal static class NativeGrantSaveBinding
    {
        internal static int? BoundSlot;
        internal static bool IsCurrentSaveBound()
        {
            bool allowed = false;
            CassetteReceiptRandomization.WithStableSelectedQuestSave((_, _, slot) =>
                allowed = !BoundSlot.HasValue || BoundSlot == slot);
            return allowed;
        }
    }
    internal static class RootsIntroCutsceneBypass { internal static void CapturePlayerSaveRequestProcessor(object? processor) { } }
    internal static class GarageCartridgeAccess { internal static void CapturePlayerSaveRequestProcessor(object? processor) { } }
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
