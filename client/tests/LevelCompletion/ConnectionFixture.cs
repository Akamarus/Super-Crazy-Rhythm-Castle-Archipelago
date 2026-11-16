global using ConnectionRefusedError = Archipelago.MultiClient.Net.Enums.ConnectionRefusedError;
using System.Reflection;
using Archipelago.MultiClient.Net.DataPackage;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace RhythmCastleAP;

// Compile the unchanged production ArchipelagoClient; replace only external
// networking and unrelated native subsystems. Campaign policy, lifecycle leases,
// reconnect admission, local deduplication and queue delivery remain real.
internal static partial class ConnectionTests
{
    internal static void Run(Func<Dictionary<string, object>> contract)
    {
        var roomWithPassword = Login(contract(), seed: "credential-source");
        string? supplied = null;
        var withProvider = new ArchipelagoClient("test", "slot", "unused-config-value", FreshOutbox(), () => supplied);
        ArchipelagoSessionFactory.Next = roomWithPassword;
        withProvider.Connect();
        Check(0, roomWithPassword.LoginAttempts, "missing session-only password pauses before login");
        supplied = "session-only-fixture";
        withProvider.Connect();
        Check(supplied, roomWithPassword.LastPassword, "explicit session-only password takes precedence for this room");
        withProvider.Shutdown();
        var firstDelivery = Login(contract(), seed: "delivery-reconnect");
        string directory = FreshOutbox();
        var deliveryClient = NewClient(directory);
        deliveryClient.QueueLocation("Level 1 - 1 Star");
        Connect(deliveryClient, firstDelivery, true);
        Check(0, firstDelivery.Locations.SentCount, "startup checks cannot acquire an arbitrary room");
        Check(1, firstDelivery.Socket.SentPackets.OfType<SyncPacket>().Count(), "missing initial item history requests a full replay");
        NativeGrantSaveBinding.SelectedSaveMatches = false;
        deliveryClient.QueueLocation("Level 1 - 1 Star");
        deliveryClient.QueueCampaignResult("Level_05", "LevelVariant_Default", 1, deliveryClient.CaptureCampaignResultContext());
        Flush(deliveryClient);
        Check(0, firstDelivery.Locations.SentCount, "neither a source nor a campaign result from another save can enter the outbox");
        NativeGrantSaveBinding.SelectedSaveMatches = true;
        var originalResult = deliveryClient.CaptureCampaignResultContext();
        NativeGrantSaveBinding.NativeSave = "other-epoch";
        deliveryClient.QueueCampaignResult("Level_05", "LevelVariant_Default", 1, originalResult);
        Flush(deliveryClient);
        Check(0, firstDelivery.Locations.SentCount, "a delayed result from the previous save cannot supply Star tiers");
        NativeGrantSaveBinding.NativeSave = "initial-epoch";
        deliveryClient.QueueLocation("Level 1 - Completion");
        Flush(deliveryClient);
        Check(true, SpinWait.SpinUntil(() => firstDelivery.Locations.SentCount == 1, 5000), "first send attempted");
        firstDelivery.Socket.Close();
        var retryDelivery = Login(contract(), seed: "delivery-reconnect");
        Connect(deliveryClient, retryDelivery, true);
        Check(1, retryDelivery.Locations.SentCount, "a buffered send lost before server acknowledgement retries");
        retryDelivery.Socket.Close();
        var acknowledged = Login(contract(), seed: "delivery-reconnect");
        acknowledged.Locations.AllLocationsChecked = [187256001];
        Connect(deliveryClient, acknowledged, true);
        Check(0, acknowledged.Locations.SentCount, "acknowledged check is not replayed");
        acknowledged.Socket.Close();
        deliveryClient.QueueLocation("Level 1 - 1 Star");
        deliveryClient.Shutdown();
        var afterRestart = NewClient(directory);
        var otherRoom = Login(contract(), seed: "other-delivery-room");
        Connect(afterRestart, otherRoom, true);
        Check(0, otherRoom.Locations.SentCount, "restart cannot transfer offline checks into a different room");
        var restored = Login(contract(), seed: "delivery-reconnect");
        restored.Locations.AllLocationsChecked = [187256001];
        Connect(afterRestart, restored, true);
        Check(false, originalResult == afterRestart.CaptureCampaignResultContext(), "reloaded client cannot reuse a pending result from a retired client");
        Check("187256211", string.Join(',', restored.Locations.SentIds), "offline check survives shutdown and return to its room");
        restored.Socket.Receive(new RoomUpdatePacket { CheckedLocations = [187256295] });
        Check(true, afterRestart.QueueExpandedLocation("Lobby - Car Battery Hand-In", Plugin.GameName + "|delivery-reconnect|0|1"),
            "matching room admits journal replay");
        Flush(afterRestart);
        Check("187256211", string.Join(',', restored.Locations.SentIds), "live server confirmation suppresses a duplicate replay");
        afterRestart.Shutdown();
        ReloadInSameProcess(contract);
        SchoolTripMailFollowsServerChecks(contract);
        NamedItemsFollowAcceptedRoom(contract);
        NamedItemsUseLatestPreLoginReplay(contract);
        foreach (var rejection in Enum.GetValues<RoomRejection>())
            PendingChecksSurviveRejectedRoom(contract, rejection);
        VerifyLoginRecovery(contract);
        Console.WriteLine("PASS: room-scoped named item delivery, corrected SDK replays, rejected room check retention and restored quest checks");
        var protectedClient = NewClient();
        var protectedSession = Login(contract());
        bool loginReached = false;
        protectedSession.BeforeLoginReturns = () => loginReached = true;
        NetworkReceiveGuard.Fail = true;
        try
        {
            Connect(protectedClient, protectedSession, false);
            Check(false, loginReached, "missing security guard must prevent network login");
            Check(false, RetryPolicyOf(protectedClient).OnUnexpectedDisconnect(), "permanent guard failure pauses retries");
        }
        finally { NetworkReceiveGuard.Fail = false; }
        Connect(protectedClient, protectedSession, true);
        Check(true, loginReached, "guard failure must abandon generation so a protected retry can work");
        protectedClient.Shutdown();
        // Previous item ownership must stop before a replacement authenticates,
        // including a replacement rejected for an incompatible item contract.
        var questClient = NewClient();
        var oldQuestRoom = Login(StarContract(contract));
        Connect(questClient, oldQuestRoom, true);
        oldQuestRoom.Socket.Receive(new ReceivedItemsPacket { Index = 0, Items = [new NetworkItem { Item = 187256159 }] });
        Check(true, CharacterQuestItems.State.Snapshot.Ready, "previous ownership setup");
        var badQuestContract = contract();
        badQuestContract["randomize_character_quest_items"] = true;
        var replacementQuest = Login(badQuestContract);
        bool readyDuringLogin = true;
        replacementQuest.BeforeLoginReturns = () => readyDuringLogin = CharacterQuestItems.State.Snapshot.Ready;
        Connect(questClient, replacementQuest, false);
        Check(false, ApStars.State.Mode == ApStarMode.Ready, "replacement cannot leave retired Star authority ready");
        Check(false, readyDuringLogin, "replacement suspends grants before authentication");
        Check(false, CharacterQuestItems.State.Snapshot.Ready, "invalid replacement cannot retain old ownership");
        Check(false, questClient.QueueExpandedLocation("Level 1 - Completion", "wrong-identity"),
            "old expanded source cannot enter a replacement identity queue");
        questClient.Shutdown();
        UnauthenticatedTransportCannotFlush(contract);
        foreach (string replacement in new[] { "same", "seed", "team", "slot" })
        {
            CampaignLevelRandomization.Shutdown();
            var client = NewClient();
            var first = Login(contract());
            Connect(client, first, true);
            Check(false, client.QueueExpandedLocation("Level 1 - Completion", Plugin.GameName + "|old-seed|0|1"),
                "expanded source identity mismatch rejected before queue");
            first.Socket.Close();
            Connect(client, Login(contract(), success: false), false);
            var offline = CampaignLevelRandomization.EvaluatePersistedResult("Level_05", "LevelVariant_Default", 1);
            Check(2, offline.Count, "failed retry retains the validated campaign contract for offline results");
            client.QueueCampaignResult("Level_05", "LevelVariant_Default", 1, client.CaptureCampaignResultContext());
            client.QueueCampaignResult("Level_05", "LevelVariant_Default", 1, client.CaptureCampaignResultContext());
            var next = Login(contract(), seed: replacement == "seed" ? "other-seed" : "seed",
                team: replacement == "team" ? 1 : 0, slot: replacement == "slot" ? 2 : 1);
            Connect(client, next, true);
            Check(replacement == "same" ? 2 : 0, next.Locations.SentCount,
                "pending checks flush once only to the same authenticated identity: " + replacement);
            Flush(client);
            Check(replacement == "same" ? 2 : 0, next.Locations.SentCount, "second flush cannot duplicate checks");
            if (replacement != "same")
            {
                client.QueueCampaignResult("Level_05", "LevelVariant_Default", 1, client.CaptureCampaignResultContext());
                Flush(client);
                // QueueCampaignResult also launches background flushes. A worker
                // may own a dequeued check after our synchronous flush returns.
                Check(true, SpinWait.SpinUntil(() => next.Locations.SentCount >= 2, TimeSpan.FromSeconds(5)),
                    "new identity check delivery completes");
                Check(2, next.Locations.SentCount, "new identity may earn its own checks without stale deduplication");
            }
            client.Shutdown();
            Check(0, CampaignLevelRandomization.Snapshot.ActiveLocations.Count, "shutdown clears retained campaign state");
            client.QueueCampaignResult("Level_05", "LevelVariant_Default", 3, client.CaptureCampaignResultContext());
            Flush(client);
            Check(2, next.Locations.SentCount, "late result after shutdown cannot deliver checks");
        }

        CampaignLevelRandomization.Shutdown();
        var rejectedClient = NewClient();
        var malformed = contract();
        malformed["campaign_level_mapping_schema"] = 99L;
        Plugin.LoggerInstance.Errors.Clear();
        Connect(rejectedClient, Login(malformed), true);
        Check(true, Plugin.LoggerInstance.Errors.Any(message => message.Contains("CAMPAIGN", StringComparison.Ordinal) &&
            message.Contains("invalid campaign_level_mapping_schema", StringComparison.Ordinal)),
            "login immediately reports campaign compatibility Detail as an explicit error");
        Check(0, CampaignLevelRandomization.Snapshot.ActiveLocations.Count, "rejected login contract remains empty");
        Check(true, rejectedClient.Connected, "campaign-only rejection preserves other connection policy");
        rejectedClient.Shutdown();
        Console.WriteLine("PASS: production connection retry, identity isolation, queue delivery, and login errors");
        RunStars(contract);
    }

    private static void NamedItemsFollowAcceptedRoom(Func<Dictionary<string, object>> contract)
    {
        var client = NewClient();
        var grants = AreaAccessPrototype.Grants;
        grants.Clear();
        try
        {
            var first = Login(contract(), seed: "A");
            Connect(client, first, true);
            first.ReceiveNamedItems(0, 187256104);
            Check("Roots Access", string.Join(',', grants), "seed A receives its first live item");
            first.Socket.Close();
            grants.Clear();

            var invalid = StarContract(contract);
            invalid["star_item_count"] = 65;
            var rejected = Login(invalid, seed: "B");
            rejected.BeforeLoginReturns = () => rejected.ReceiveNamedItems(0, 187256105, 187256106);
            Connect(client, rejected, false);
            Check(0, grants.Count, "a rejected seed cannot dispatch pre-login named items");

            var originalAgain = Login(contract(), seed: "A");
            originalAgain.BeforeLoginReturns = () => originalAgain.ReceiveNamedItems(0, 187256104, 187256107);
            Connect(client, originalAgain, true);
            Check("Cell Tower Access", string.Join(',', grants), "rejected B cannot consume A's next delivery index");
            grants.Clear();

            var next = Login(contract(), seed: "B");
            int appliedDuringLogin = -1;
            next.BeforeLoginReturns = () =>
            {
                next.ReceiveNamedItems(0, 187256105, 187256106);
                appliedDuringLogin = grants.Count;
            };
            Connect(client, next, true);
            Check(0, appliedDuringLogin, "the replacement's callbacks wait for the room to pass validation");
            Check("Lobby Access,Meat Dimension Access", string.Join(',', grants),
                "the accepted seed receives both pre-login items in order, including its own item at index one");
            next.Socket.Close();
            grants.Clear();

            var replay = Login(contract(), seed: "B");
            replay.BeforeLoginReturns = () => replay.ReceiveNamedItems(0, 187256105, 187256106, 187256107);
            Connect(client, replay, true);
            next.ReceiveNamedItems(2, 187256104);
            replay.ReceiveNamedItems(0, 187256105, 187256106, 187256107);
            replay.ReceiveNamedItems(3, 187256107);
            Check("Cell Tower Access,Cell Tower Access",
                string.Join(',', grants), "same-seed replay skips old indexes and delivers both new copies exactly once");
            replay.ReceiveNamedItems(0, 187256104);
            Check("Roots Access", string.Join(',', grants), "a live correction replaces previous named ownership");
            replay.ReceiveNamedItems(0);
            Check(0, grants.Count, "an empty live replay clears named ownership");
            replay.Socket.SentPackets.Clear();
            var gap = new ReceivedItemsPacket { Index = 8, Items = [new NetworkItem { Item = 187256107 }] };
            replay.Socket.Receive(gap);
            replay.Socket.Receive(gap);
            Check(1, replay.Socket.SentPackets.OfType<SyncPacket>().Count(), "a gap requests one authoritative replay");
            replay.ReceiveNamedItems(0, 187256106);
            Check("Meat Dimension Access", string.Join(',', grants), "full replay resumes delivery after a gap");
        }
        finally
        {
            client.Shutdown();
            grants.Clear();
        }
    }

    private static void NamedItemsUseLatestPreLoginReplay(Func<Dictionary<string, object>> contract)
    {
        foreach (long[] corrected in new long[][] { [], [187256107] })
        {
            var client = NewClient();
            var grants = AreaAccessPrototype.Grants;
            try
            {
                var room = Login(contract());
                room.BeforeLoginReturns = () =>
                {
                    room.ReceiveNamedItems(0, 187256104, 187256105);
                    room.ReceiveNamedItems(0, corrected);
                };
                Connect(client, room, true);
                Check(corrected.Length == 0 ? "" : "Cell Tower Access", string.Join(',', grants),
                    "only the corrected pre-login replay can grant items, including when the replacement is empty");
                room.ReceiveNamedItems(corrected.Length, 187256106);
                Check(corrected.Length == 0 ? "Meat Dimension Access" : "Cell Tower Access,Meat Dimension Access",
                    string.Join(',', grants), "live receipts continue at the corrected history index");
            }
            finally
            {
                client.Shutdown();
                grants.Clear();
            }
        }
    }

    private static string FreshOutbox() => Path.Combine(Path.GetTempPath(), "scrc-check-outbox-" + Guid.NewGuid());
    private static ArchipelagoClient NewClient(string? directory = null) => new("test", "slot", "", directory ?? FreshOutbox());

    private enum RoomRejection { StarContract, GarageSync, BunkerJournal, ExpandedJournal, CheckOutbox, MissingSeed }

    private static void PendingChecksSurviveRejectedRoom(Func<Dictionary<string, object>> contract, RoomRejection rejection)
    {
        string directory = FreshOutbox();
        var client = NewClient(directory);
        string? damagedJournal = null;
        try
        {
            var first = Login(contract(), seed: "original");
            Connect(client, first, true);
            first.Socket.Close();
            client.QueueCampaignResult("Level_05", "LevelVariant_Default", 1, client.CaptureCampaignResultContext());

            var candidate = rejection == RoomRejection.BunkerJournal ? BunkerSlotData(contract) : StarContract(contract);
            switch (rejection)
            {
                case RoomRejection.StarContract:
                    candidate["star_item_count"] = 65;
                    break;
                case RoomRejection.GarageSync:
                    candidate["randomize_game_garage_cartridges"] = true;
                    GarageCartridgeAccess.SyncAvailable = false;
                    break;
                case RoomRejection.BunkerJournal:
                case RoomRejection.ExpandedJournal:
                case RoomRejection.CheckOutbox:
                    string journalDirectory = rejection switch {
                        RoomRejection.BunkerJournal => BunkerRuntime.JournalDirectory,
                        RoomRejection.ExpandedJournal => ExpandedChecks.JournalDirectory,
                        _ => directory
                    };
                    damagedJournal = JournalFile(journalDirectory, Plugin.GameName + "|replacement|0|1");
                    Directory.CreateDirectory(journalDirectory);
                    File.WriteAllText(damagedJournal, """{"Slot":5,"Consumed":[],"Completed":[]}""");
                    break;
            }
            var rejected = Login(candidate, seed: rejection == RoomRejection.MissingSeed ? "" : "replacement");
            rejected.BeforeLoginReturns = () => rejected.ReceiveNamedItems(0, 187256105);
            Plugin.LoggerInstance.Errors.Clear();
            Connect(client, rejected, false, $"reject invalid room: {rejection}");
            Check(0, rejected.Locations.SentCount, "rejected room never receives the original campaign checks");
            Check(0, AreaAccessPrototype.Grants.Count, $"failure at {rejection} cannot dispatch grants");
            Check(rejection == RoomRejection.GarageSync, RetryPolicyOf(client).OnUnexpectedDisconnect(),
                "transient synchronization can retry while invalid configuration pauses reconnect");
            if (damagedJournal != null)
                Check(true, Plugin.LoggerInstance.Errors.Any(message => message.Contains(Path.GetFileName(damagedJournal))),
                    $"{rejection} identifies the damaged file");

            var originalAgain = Login(contract(), seed: "original");
            Connect(client, originalAgain, true);
            Check("187256001,187256211", string.Join(',', originalAgain.Locations.SentIds.Order()),
                $"both offline checks survive another seed's failure at {rejection}");
        }
        finally
        {
            client.Shutdown();
            GarageCartridgeAccess.SyncAvailable = true;
            AreaAccessPrototype.Grants.Clear();
            if (damagedJournal != null) File.Delete(damagedJournal);
        }
    }

    private static void UnauthenticatedTransportCannotFlush(Func<Dictionary<string, object>> contract)
    {
        var client = NewClient();
        var first = Login(contract());
        Connect(client, first, true);
        var queue = (System.Collections.Concurrent.ConcurrentQueue<string>)typeof(ArchipelagoClient)
            .GetField("_pendingChecks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(client)!;
        queue.Enqueue("Level 1 - Completion");
        using var flushPaused = new ManualResetEventSlim();
        using var resumeFlush = new ManualResetEventSlim();
        using var loginPaused = new ManualResetEventSlim();
        using var resumeLogin = new ManualResetEventSlim();
        MusicLabPointRandomization.OnRead = () =>
        {
            flushPaused.Set();
            if (!resumeFlush.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("flush not resumed");
        };
        var flushing = Task.Run(() => Flush(client));
        Check(true, flushPaused.Wait(TimeSpan.FromSeconds(5)), "flush pauses after observing connected");
        first.Socket.Close();
        var retry = Login(contract(), success: false);
        retry.BeforeLoginReturns = () =>
        {
            loginPaused.Set();
            if (!resumeLogin.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("login not resumed");
        };
        var reconnecting = Task.Run(() => Connect(client, retry, false));
        int sent;
        try
        {
            Check(true, loginPaused.Wait(TimeSpan.FromSeconds(5)), "new transport is published before authentication");
            resumeFlush.Set();
            flushing.GetAwaiter().GetResult();
            sent = retry.Locations.SentCount;
        }
        finally
        {
            resumeFlush.Set();
            resumeLogin.Set();
            reconnecting.GetAwaiter().GetResult();
            client.Shutdown();
        }
        Check(0, sent, "a flush admitted before disconnect must not deliver to the unauthenticated replacement transport");
    }

    private static void VerifyLoginRecovery(Func<Dictionary<string, object>> contract)
    {
        foreach (var (codes, needsCorrection) in new (ConnectionRefusedError[], bool)[]
        {
            ([ConnectionRefusedError.InvalidSlot], true), ([ConnectionRefusedError.InvalidPassword], true),
            ([ConnectionRefusedError.InvalidGame], true), ([ConnectionRefusedError.IncompatibleVersion], true),
            ([ConnectionRefusedError.SlotAlreadyTaken, ConnectionRefusedError.InvalidItemsHandling], true),
            ([ConnectionRefusedError.SlotAlreadyTaken], false), ([], false)
        })
        {
            var client = NewClient();
            var session = Login(contract());
            session.Result = new LoginFailure { ErrorCodes = codes };
            session.BeforeLoginReturns = session.Socket.Close;
            ArchipelagoSessionFactory.Next = session;
            client.Connect();
            var retry = RetryPolicyOf(client);
            Check(!needsCorrection, retry.ReconnectRequested, "retry follows the typed login result after a racing socket close");
            Check(!needsCorrection, retry.OnUnexpectedDisconnect(), "late callbacks respect the refusal");
            client.Shutdown();
        }

        const string seed = "damaged-journal";
        string identity = string.Join("|", Plugin.GameName, seed, 0, 1);
        QuestChecks.Journal.Save(identity, new(1, [QuestChecksPolicy.BatteryHandIn], []));
        string file = JournalFile(QuestChecks.JournalDirectory, identity);
        string backup = File.ReadAllText(file);
        var journalClient = NewClient();
        Plugin.AP = journalClient;
        try
        {
            var originalRoom = Login(contract(), seed: "working-seed");
            Connect(journalClient, originalRoom, true);
            originalRoom.Socket.Close();
            journalClient.QueueCampaignResult("Level_05", "LevelVariant_Default", 1, journalClient.CaptureCampaignResultContext());
            File.WriteAllText(file, "{");
            var damagedRoom = Login(StarContract(contract), seed: seed);
            damagedRoom.BeforeLoginReturns = () => damagedRoom.Socket.Receive(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket
            {
                Index = 0, Items = [new Archipelago.MultiClient.Net.Models.NetworkItem { Item = 187256159 }]
            });
            ArchipelagoSessionFactory.Next = damagedRoom;
            journalClient.Connect();
            Check(false, journalClient.Connected, "damaged journal prevents session from becoming ready");
            Check(false, CharacterQuestItems.State.Snapshot.Ready, "failed journal validation cannot publish pre-login grants");
            Check(false, RetryPolicyOf(journalClient).OnUnexpectedDisconnect(), "damaged journal pauses automatic retries");
            Check(true, Plugin.LoggerInstance.Errors.Any(x => x.Contains(Path.GetFileName(file)) && x.Contains("reconnect paused")),
                "player gets the filename and retry status");
            Check("{", File.ReadAllText(file), "failed login preserves damaged file for recovery");
            var recoveredRoom = Login(contract(), seed: "working-seed");
            Connect(journalClient, recoveredRoom, true);
            Check(2, recoveredRoom.Locations.SentCount, "returning to the working seed delivers its two pending checks");
            Check(0, damagedRoom.Locations.SentCount, "the rejected room received no checks");
            File.WriteAllText(file, backup);
            var restoredRoom = Login(StarContract(contract), seed: seed);
            ArchipelagoSessionFactory.Next = restoredRoom;
            journalClient.Connect();
            Check(true, journalClient.Connected, "explicit connect succeeds with restored journal");
            Check("187256295", string.Join(',', restoredRoom.Locations.SentIds),
                "switching seeds delivers the restored quest journal's completed check to its own room");
        }
        finally
        {
            journalClient.Shutdown();
            Plugin.AP = null;
            File.Delete(file);
        }
    }

    private static void SchoolTripMailFollowsServerChecks(Func<Dictionary<string, object>> contract)
    {
        var data = StarContract(contract);
        data["implementation_version"] = ApStarContract.Implementation.Replace("-generation-foundation", "-lobby-doors-1-generation-foundation");
        data["lobby_open_doors"] = true;
        var client = NewClient();
        var room = Login(data, team: 1, slot: 7);
        Connect(client, room, true);
        room.Socket.Receive(new RoomUpdatePacket { CheckedLocations = [187256299] });
        Check(0, room.Socket.SentPackets.OfType<SetPacket>().Count(), "mail alone cannot open the tracked route");
        room.Socket.Receive(new RoomUpdatePacket { CheckedLocations = [187256228] });
        room.Socket.Receive(new RoomUpdatePacket { CheckedLocations = [187256228, 187256299] });
        var write = room.Socket.SentPackets.OfType<SetPacket>().Single();
        Check("SCRC_1_7_school_trip_mail", write.Key, "write the route to the actual team and slot");
        Check(Archipelago.MultiClient.Net.Enums.OperationType.Replace, write.Operations.Single().OperationType, "idempotent update");
        Check(true, (bool)write.Operations.Single().Value, "open the route without a Training clear");
        room.Socket.Close();
        var restored = Login(data, team: 1, slot: 7);
        restored.Locations.AllLocationsChecked = [187256228, 187256299];
        Connect(client, restored, true);
        Check(write.Key, restored.Socket.SentPackets.OfType<SetPacket>().Single().Key, "publish existing progress on login");
        restored.Socket.Close();
        var other = Login(data, seed: "new", slot: 2);
        other.Locations.AllLocationsChecked = [187256228];
        Connect(client, other, true);
        Check(0, other.Socket.SentPackets.OfType<SetPacket>().Count(), "Boring Room alone cannot open the new slot's route");
        client.Shutdown();
    }

    private static string JournalFile(string directory, string identity) => Path.Combine(directory,
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity))) + ".json");

    private static ReconnectPolicy RetryPolicyOf(ArchipelagoClient client) =>
        (ReconnectPolicy)typeof(ArchipelagoClient).GetField("_reconnectPolicy", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(client)!;

    private static ArchipelagoSession Login(Dictionary<string, object> data, bool success = true,
        string seed = "seed", int team = 0, int slot = 1) => new()
        {
            RoomState = new() { Seed = seed },
            Result = success ? new LoginSuccessful { SlotData = data, Team = team, Slot = slot } : new LoginFailure(),
        };
    private static void Connect(ArchipelagoClient client, ArchipelagoSession session, bool expected,
        string reason = "production login outcome")
    {
        ArchipelagoSessionFactory.Next = session;
        Check(expected, (bool)typeof(ArchipelagoClient).GetMethod("TryConnectOnce", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, null)!,
            reason);
    }
    private static void Flush(ArchipelagoClient client) =>
        typeof(ArchipelagoClient).GetMethod("FlushPendingChecks", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, null);
    private static void Check<T>(T want, T got, string why)
    {
        if (!EqualityComparer<T>.Default.Equals(want, got)) throw new InvalidOperationException($"{why}: expected {want}, got {got}");
    }
}

internal static class Plugin
{
    internal const string GameName = "Super Crazy Rhythm Castle";
    internal static TestLogger LoggerInstance { get; } = new();
    internal static ArchipelagoClient? AP;
    internal static void ResetRoomOwnership() { }
}
internal sealed class TestLogger
{
    internal List<string> Errors { get; } = new();
    internal void LogInfo(string message) { }
    internal void LogWarning(string message) { }
    internal void LogError(string message) { lock (Errors) Errors.Add(message); }
}
internal enum ItemsHandlingFlags { AllItems }
internal class LoginResult { internal bool Successful => this is LoginSuccessful; }
internal sealed class LoginSuccessful : LoginResult
{
    internal Dictionary<string, object> SlotData = new();
    internal int Team;
    internal int Slot;
}
internal sealed class LoginFailure : LoginResult
{
    internal string[] Errors = ["controlled authentication failure"];
    internal ConnectionRefusedError[] ErrorCodes = [];
}
internal static class ArchipelagoSessionFactory
{
    internal static ArchipelagoSession Next = new();
    internal static ArchipelagoSession CreateSession(string server) => Next;
}
internal sealed class ArchipelagoSession
{
    internal TestSocket Socket = new();
    internal readonly ReceivedItemsHelper Items;
    internal TestLocations Locations = new();
    internal TestRoomState RoomState = new();
    internal LoginResult Result = new LoginFailure();
    internal Action? BeforeLoginReturns;
    internal int GarageWrites;
    internal ArchipelagoSession()
    {
        var sdk = Archipelago.MultiClient.Net.ArchipelagoSessionFactory.CreateSession("localhost", 38281);
        Items = (ReceivedItemsHelper)Activator.CreateInstance(typeof(ReceivedItemsHelper),
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            [sdk.Socket, sdk.Locations, new TestItemNames(), sdk.ConnectionInfo, sdk.Players], null)!;
    }

    internal void ReceiveNamedItems(int index, params long[] ids)
    {
        var packet = new ReceivedItemsPacket { Index = index, Items = ids.Select(id => new NetworkItem { Item = id }).ToArray() };
        // The SDK updates its history and raises one named callback per arrival before the plugin sees the packet.
        typeof(ReceivedItemsHelper).GetMethod("Socket_PacketReceived", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(Items, [packet]);
        Socket.Receive(packet);
    }

    internal int LoginAttempts;
    internal string? LastPassword;
    internal LoginResult TryConnectAndLogin(string game, string slot, ItemsHandlingFlags flags, Version version,
        string? password, bool requestSlotData)
    {
        LoginAttempts++; LastPassword = password;
        BeforeLoginReturns?.Invoke();
        return Result;
    }
}
internal sealed class TestSocket
{
#pragma warning disable CS0067
    internal event Action? SocketOpened;
    internal event Action<string>? SocketClosed;
    internal event Action<Exception, string>? ErrorReceived;
    internal event Action<Archipelago.MultiClient.Net.ArchipelagoPacketBase>? PacketReceived;
#pragma warning restore CS0067
    internal readonly List<Archipelago.MultiClient.Net.ArchipelagoPacketBase> SentPackets = new();
    internal void SendPacket(Archipelago.MultiClient.Net.ArchipelagoPacketBase packet) => SentPackets.Add(packet);
    internal void Receive(Archipelago.MultiClient.Net.ArchipelagoPacketBase packet) => PacketReceived?.Invoke(packet);
    internal bool Connected => true;
    internal Task DisconnectAsync() => Task.CompletedTask;
    internal void Close() => SocketClosed?.Invoke("controlled transport loss");
}
internal sealed class TestRoomState { internal string Seed = "seed"; }
internal sealed class TestItemNames : IItemInfoResolver
{
    public string GetItemName(long id, string game) => id switch
    {
        187256104 => "Roots Access",
        187256105 => "Lobby Access",
        187256106 => "Meat Dimension Access",
        187256107 => "Cell Tower Access",
        _ => $"Item {id}"
    };
    public string GetLocationName(long id, string game) => throw new NotSupportedException();
    public long GetLocationId(string name, string game) => throw new NotSupportedException();
}
internal sealed class TestLocations
{
    internal long[] AllLocationsChecked = [];
    internal long[] AllLocations => [187256001, 187256211, 187256295];
    private readonly List<long> Sent = new();
    internal int SentCount { get { lock (Sent) return Sent.Count; } }
    internal long[] SentIds { get { lock (Sent) return Sent.ToArray(); } }
    internal long GetLocationIdFromName(string game, string name) => name switch
    { "Level 1 - Completion" => 187256001, "Level 1 - 1 Star" => 187256211, "Lobby - Car Battery Hand-In" => 187256295,
      "Level 6 - Completion" => 187256228, "Lobby - Important Letters Delivery" => 187256299, _ => -1 };
    internal void CompleteLocationChecks(long id) { lock (Sent) Sent.Add(id); }
}
internal enum MusicLabPointRuntimeMode { Synchronized, Incompatible }
internal sealed record MusicLabPointSnapshot(MusicLabPointRuntimeMode Mode, string Detail = "");
internal enum MusicLabPointCompatibilityMode { Compatible, IncompatibleClaim }
internal readonly record struct MusicLabPointCompatibilityResult(MusicLabPointCompatibilityMode Mode, string Detail);
internal static class MusicLabPointRandomization
{
    internal static Action? OnRead;
    internal static MusicLabPointSnapshot Snapshot
    {
        get { Interlocked.Exchange(ref OnRead, null)?.Invoke(); return new(MusicLabPointRuntimeMode.Synchronized); }
    }
    internal static void Reset() { }
    internal static void OnDisconnected(long generation) { }
    internal static bool TryHandleItemName(string name) => false;
}
internal sealed class MusicLabPointSessionHistory
{
    internal MusicLabPointSessionHistory(long generation) { }
    internal void HandlePacket(object packet) { }
    internal MusicLabPointCompatibilityResult PrepareSlotData(Dictionary<string, object>? data, string game, string seed, string slot) =>
        new(MusicLabPointCompatibilityMode.Compatible, "fixture");
    internal void Accept() { }
}
internal class NativeSubsystem
{
    internal static bool Enabled => false;
    internal static void ResetNamedReceipts() { }
    internal static void ReplaceIndividualReceipts(IReadOnlyList<long> ids, Action reset) => reset();
    internal static void ApplySlotData(Dictionary<string, object> data) { }
    internal static void ApplySlotDataStarter(Dictionary<string, object>? data) { }
    internal static bool TryApplyItem(string name) => false;
    internal static void RequestUnityReconciliation(string reason) { }
}
internal sealed class AreaAccessPrototype : NativeSubsystem
{
    internal static readonly List<string> Grants = new();
    internal static void ResetRoomOwnership() => Grants.Clear();
    internal new static bool TryApplyItem(string name)
    {
        if (!name.EndsWith(" Access", StringComparison.Ordinal)) return false;
        Grants.Add(name);
        return true;
    }
    internal static void EndAuthenticatedSession() { }
}
internal sealed class IntroHubSkip : NativeSubsystem { }
internal sealed partial class GarageCartridgeAccess : NativeSubsystem
{
    internal static bool SyncAvailable = true;
    private static readonly GarageCartridgeReconciliationAccess ServerSyncLifecycle = new();
    internal static readonly GarageCartridgeInsertionCoordinator InsertionCoordinator = new(
        new Dictionary<string, string> { ["Superstar"] = "scrc:garage_inserted:v1:superstar" });
    private static readonly TestDispatcher UnityDispatcher = new();
    private static void ResetNativeBagObservations(string reason) { }
    internal static void ResetRoomOwnership() { }
    private sealed class TestDispatcher { internal void Clear() { } }
}
internal sealed class ArchipelagoGarageInsertionDataStore : IGarageInsertionDataStore
{
    private readonly ArchipelagoSession _session;
    internal ArchipelagoGarageInsertionDataStore(ArchipelagoSession session)
    {
        if (!GarageCartridgeAccess.SyncAvailable) throw new InvalidOperationException("controlled Garage sync failure");
        _session = session;
    }
    public Task<GarageInsertionReadResult> ReadAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(GarageInsertionReadResult.KnownFalse);
    public Task<GarageInsertionWriteResult> WriteTrueAsync(string key, CancellationToken cancellationToken)
    {
        _session.GarageWrites++;
        return Task.FromResult(GarageInsertionWriteResult.Failed);
    }
}
internal sealed class WeedKillerRandomization : NativeSubsystem
{
    internal static readonly WeedKillerReceiptState History = new();
}
internal sealed class PlantPipesRandomization : NativeSubsystem { }
internal sealed record CassetteSlotCompatibilityResult(bool Compatible, string Detail);
internal static class CassetteSlotDataCompatibility
{
    internal static CassetteSlotCompatibilityResult Validate(Dictionary<string, object>? data) => new(true, "fixture");
}
internal sealed class CassetteReceiptRandomization : NativeSubsystem
{
    internal static void ApplySlotData(CassetteSlotCompatibilityResult compatibility, bool bundled) { }
}
internal sealed class CassetteSourceRandomization : NativeSubsystem
{
    internal static void ApplySlotData(CassetteSlotCompatibilityResult compatibility) { }
}
internal sealed class PreviewAbilityRandomization : NativeSubsystem { }
internal sealed class RootsBucketRandomization : NativeSubsystem { }
internal static class CharacterQuestItems
{
    internal static readonly CharacterQuestItemState State = new();
    internal static bool TryHandleItemName(string name) => CharacterQuestItemPolicy.All.Any(item => item.Name == name);
}

internal static class ItemNotifications
{
    internal static readonly TestNotificationFeed Feed = new();
    internal static TestNotificationSession CreateSession(object session, long generation, Action<Action> dispatch) => new();
}
internal sealed class TestNotificationFeed { internal void Reset() { } }
internal sealed class TestNotificationSession
{
    internal void HandlePacket(object packet) { }
    internal void Configure(string seed, int team, int slot, CassetteBundleContract cassettes) { }
}

internal static class NetworkReceiveGuard
{
    internal static void RegisterRoomSocket(object socket) { }
    internal static bool Fail;
    internal static void EnsureInstalled()
    { if (Fail) throw new InvalidOperationException("controlled security guard installation failure"); }
}

internal static class NativeGrantSaveBinding
{
    private static string? _identity;
    internal static bool SelectedSaveMatches = true;
    internal static string NativeSave = "initial-epoch";
    internal static Action Prepare(string identity, bool questChecks) => () => _identity = identity;
    internal static void WithCheckSave(Action<string> action)
    {
        if (_identity != null && SelectedSaveMatches) action(_identity);
    }
    internal static void WithCheckSave(Action<string, string> action) =>
        WithCheckSave(room => action(room, NativeSave));
    internal static void Disconnect() { }
}
