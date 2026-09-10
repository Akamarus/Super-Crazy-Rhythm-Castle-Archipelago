using System.Reflection;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json;

namespace RhythmCastleAP;

internal static class PacketHistoryTests
{
    internal static void Run(Func<Dictionary<string, object>> contract)
    {
        MusicLabPointRandomization.Reset();
        using var first = new SessionFixture(100, contract);
        first.Login();
        Check(MusicLabPointRuntimeMode.AwaitingInitialSynchronization, MusicLabPointRandomization.Snapshot.Mode,
            "Connected login with delayed history must remain awaiting, not publish the new empty cache");
        first.Deliver(0, 187256155);
        Check(20, MusicLabPointRandomization.Snapshot.Total, "first complete history publishes 20");
        first.Disconnect();

        using var reconnect = new SessionFixture(101, contract);
        reconnect.Login();
        Check(20, MusicLabPointRandomization.Snapshot.Total, "reconnect retains 20 until complete history");
        Check(MusicLabPointRuntimeMode.RetainedDisconnected, MusicLabPointRandomization.Snapshot.Mode,
            "delayed reconnect preserves retained mode");
        var replayTotals = new List<int>();
        reconnect.Session.Items.ItemReceived += _ => replayTotals.Add(MusicLabPointRandomization.Snapshot.Total);
        reconnect.Deliver(0, 187256153, 187256154);
        Check("20,20", string.Join(',', replayTotals), "library replay callbacks cannot publish partial prefixes");
        Check(11, MusicLabPointRandomization.Snapshot.Total, "complete reconnect corrects to 11");
        int logCount = Plugin.LoggerInstance.Messages.Count;
        reconnect.Deliver(0, 187256153, 187256154);
        Check(11, MusicLabPointRandomization.Snapshot.Total, "same authoritative replay never double counts");
        Check(logCount, Plugin.LoggerInstance.Messages.Count, "same authoritative replay publishes no change");
        reconnect.Deliver(2, 187256155);
        Check(31, MusicLabPointRandomization.Snapshot.Total, "contiguous live receipt updates once");
        Check(3, MusicLabPointRandomization.Snapshot.AcceptedInstances, "live receipt adds one index");
        Check(logCount + 1, Plugin.LoggerInstance.Messages.Count, "one live receipt produces exactly one state transition");
        reconnect.DeliverCompletedPacket(99, 187256155);
        Check(31, MusicLabPointRandomization.Snapshot.Total, "history gap retains last complete total");
        reconnect.Deliver(3, 187256155);
        Check(31, MusicLabPointRandomization.Snapshot.Total, "after a gap only an authoritative replay can restore readiness");

        // 6.7.1 leaves cachedReceivedItems unchanged for an empty resynchronization.
        reconnect.Deliver(0);
        Check(0, MusicLabPointRandomization.Snapshot.Total, "authoritative empty replay corrects a nonempty total");
        Check(MusicLabPointRuntimeMode.Synchronized, MusicLabPointRandomization.Snapshot.Mode, "empty history is ready");
        reconnect.Disconnect();

        using var early = new SessionFixture(102, contract);
        early.Deliver(0, 187256155);
        early.Deliver(1, 187256154);
        early.Login();
        Check(30, MusicLabPointRandomization.Snapshot.Total, "history and live packet before login publish complete buffered history");
        early.Disconnect();

        using var empty = new SessionFixture(103, contract);
        empty.Login();
        empty.Deliver(0);
        Check(0, MusicLabPointRandomization.Snapshot.Total, "empty first history corrects retained reconnect total");
        empty.Disconnect();

        // Pause the library halfway through replay, configure login concurrently,
        // then resume. A login cache read would expose the first 20-point prefix.
        using (var racingLogin = new SessionFixture(107, contract))
        using (var replayPaused = new ManualResetEventSlim())
        using (var continueReplay = new ManualResetEventSlim())
        {
            int callbacks = 0;
            racingLogin.Session.Items.ItemReceived += _ =>
            {
                if (++callbacks != 1) return;
                replayPaused.Set();
                if (!continueReplay.Wait(TimeSpan.FromSeconds(5))) throw new InvalidOperationException("replay was not released");
            };
            Exception? workerError = null;
            var worker = new Thread(() =>
            {
                try { racingLogin.Deliver(0, 187256155, 187256154); }
                catch (Exception error) { workerError = error; }
            }) { IsBackground = true };
            worker.Start();
            try
            {
                Check(true, replayPaused.Wait(TimeSpan.FromSeconds(5)), "real replay reaches partial cache boundary");
                racingLogin.Login();
                Check(0, MusicLabPointRandomization.Snapshot.Total, "login during replay cannot publish partial history");
                Check(MusicLabPointRuntimeMode.RetainedDisconnected, MusicLabPointRandomization.Snapshot.Mode,
                    "login during replay preserves retained readiness");
            }
            finally
            {
                continueReplay.Set();
                Check(true, worker.Join(TimeSpan.FromSeconds(5)), "packet replay finishes");
            }
            if (workerError != null) throw new InvalidOperationException("packet worker failed", workerError);
            Check(30, MusicLabPointRandomization.Snapshot.Total, "completed racing replay publishes 30 exactly once");
            racingLogin.Deliver(0);
        }

        using var interrupted = new SessionFixture(104, contract);
        interrupted.Login();
        interrupted.Session.Items.ItemReceived += _ => interrupted.Disconnect();
        interrupted.Deliver(0, 187256155, 187256154);
        Check(0, MusicLabPointRandomization.Snapshot.Total, "disconnect during replay rejects the packet publication");
        Check(MusicLabPointRuntimeMode.RetainedDisconnected, MusicLabPointRandomization.Snapshot.Mode,
            "disconnect during replay preserves previous authoritative state");

        using var stale = new SessionFixture(105, contract);
        stale.Login();
        SessionFixture? replacement = null;
        stale.Session.Items.ItemReceived += _ =>
        {
            if (replacement != null) return;
            stale.Disconnect();
            replacement = new SessionFixture(106, contract, "different-seed");
            replacement.Login();
        };
        stale.Deliver(0, 187256155, 187256154);
        using (replacement)
        {
            Check(0, MusicLabPointRandomization.Snapshot.Total, "replacement identity rejects the old replay");
            Check(MusicLabPointRuntimeMode.AwaitingInitialSynchronization, MusicLabPointRandomization.Snapshot.Mode,
                "new identity has not inherited old readiness");
            replacement!.Deliver(0, 187256153);
            Check(1, MusicLabPointRandomization.Snapshot.Total, "replacement publishes only its own complete history");
        }
        MusicLabPointRandomization.Reset();
        Console.WriteLine("PASS: real_library_packet_history_readiness_empty_replay_reconnect_and_generation_races");
    }

    private static void Check<T>(T want, T got, string scenario)
    {
        if (!EqualityComparer<T>.Default.Equals(want, got))
            throw new InvalidOperationException($"{scenario}: expected {want}, got {got}");
    }

    // No socket is opened. Inject JSON through the actual installed parser and
    // synchronous socket event dispatcher, retaining the real ReceivedItemsHelper.
    private sealed class SessionFixture : IDisposable
    {
        internal ArchipelagoSession Session { get; } = ArchipelagoSessionFactory.CreateSession("localhost:38281");
        private readonly SessionGenerationLeaseGate<ArchipelagoSession> _gate = new();
        private readonly long _generation;
        private readonly Func<Dictionary<string, object>> _contract;
        private readonly string _seed;
        private readonly MusicLabPointSessionHistory _history;

        internal SessionFixture(long generation, Func<Dictionary<string, object>> contract, string seed = "history-seed")
        {
            _generation = generation;
            _contract = contract;
            _seed = seed;
            _history = new MusicLabPointSessionHistory(generation);
            _gate.TryAdmit(generation);
            _gate.TryPublish(Session, generation, _ => { }, out _);
            Session.Items.ItemReceived += helper =>
            {
                if (!_gate.TryAcquire(Session, generation, out LifecycleLease? lease)) return;
                using (lease)
                {
                    while (helper.Any()) helper.DequeueItem();
                }
            };
            Session.Socket.PacketReceived += packet =>
            {
                if (!_gate.TryAcquire(Session, generation, out LifecycleLease? lease)) return;
                using (lease) _history.HandlePacket(packet);
            };
            Session.Socket.ErrorReceived += (error, message) => throw new InvalidOperationException(message, error);
        }

        internal void Login()
        {
            if (!_gate.TryAcquire(Session, _generation, out LifecycleLease? lease)) return;
            using (lease)
            {
                var json = JsonConvert.SerializeObject(new { cmd = "Connected", team = 0, slot = 1, slot_data = _contract() });
                var packet = (ConnectedPacket)JsonConvert.DeserializeObject<ArchipelagoPacketBase>(json, new ArchipelagoPacketConverter())!;
                _history.ApplySlotData(new LoginSuccessful(packet).SlotData, "game", _seed, "slot");
            }
        }

        internal void Deliver(int index, params long[] items)
        {
            string json = JsonConvert.SerializeObject(new[] { new { cmd = "ReceivedItems", index,
                items = items.Select(item => new { item, location = -1, player = 1, flags = 1 }).ToArray() } });
            typeof(Archipelago.MultiClient.Net.Helpers.ArchipelagoSocketHelper).BaseType!
                .GetMethod("OnMessageReceived", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Session.Socket, new object[] { json });
        }

        internal void DeliverCompletedPacket(int index, params long[] items)
        {
            // A real gap asks the server for Sync synchronously. With no network
            // open, exercise our completed-packet boundary directly for this case.
            var packet = new ReceivedItemsPacket { Index = index,
                Items = items.Select(item => new Archipelago.MultiClient.Net.Models.NetworkItem { Item = item }).ToArray() };
            if (!_gate.TryAcquire(Session, _generation, out LifecycleLease? lease)) return;
            using (lease) _history.HandlePacket(packet);
        }

        internal void Disconnect() => _gate.TryEnd(Session, _generation, () => MusicLabPointRandomization.OnDisconnected(_generation));
        public void Dispose() => Disconnect();
    }
}
