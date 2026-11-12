using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.DataPackage;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using System.Collections.ObjectModel;
using System.Reflection;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using RhythmCastleAP;
internal static class AdapterTests
{
    private static void CheckSentMessages()
    {
        var session = ArchipelagoSessionFactory.CreateSession("localhost", 1);
        var feed = new ItemNotificationFeed();
        var adapter = new ItemNotificationSession(session, 1, feed, action => action());
        var players = new TestPlayers();
        var resolver = new TestItemNames();
        ItemSendLogMessage Send(int sender, int receiver, long location, bool hint = false)
        {
            // Library constructors are internal; reflection creates real message objects
            // without connecting a socket. Only player/name lookup boundaries are fakes.
            object[] args = hint
                ? new object[] { Array.Empty<MessagePart>(), players, receiver, sender,
                    new NetworkItem { Item = 123, Location = location, Player = sender }, false, resolver }
                : new object[] { Array.Empty<MessagePart>(), players, receiver, sender,
                    new NetworkItem { Item = 123, Location = location, Player = sender }, resolver };
            return (ItemSendLogMessage)Activator.CreateInstance(
                hint ? typeof(HintItemSendLogMessage) : typeof(ItemSendLogMessage),
                BindingFlags.Instance | BindingFlags.NonPublic, null, args, null)!;
        }
        var sent = Send(1, 2, 10);
        adapter.HandleMessage(sent);
        feed.Advance(0);
        Check(feed.History.Count == 0 && feed.Visible.Count == 0, "pre-login send must stay buffered");
        adapter.Configure("seed", 0, 1, CassetteBundleContract.Parse(null));
        feed.Advance(0);
        Check(feed.History.Count == 1 && feed.Visible.Count == 1, "configure must publish buffered confirmed send");
        Check(feed.History[0].Text == "Sent Weed Killer to Sam" && feed.History[0].Location == "Gecko", "real send must resolve item, recipient alias and location");
        adapter.HandleMessage(sent);
        Check(feed.History.Count == 1, "repeated actual send must deduplicate");
        adapter.HandleMessage(Send(1, 2, 11, hint: true));
        Check(feed.History.Count == 1, "hint subclass must not become a sent popup");
        adapter.HandleMessage(Send(1, 1, 12));
        Check(feed.History.Count == 1, "self send must not duplicate receipt notification");
        adapter.HandleMessage(Send(2, 3, 13));
        Check(feed.History.Count == 1, "unrelated player send must be ignored");
        adapter.HandlePacket(new ReceivedItemsPacket { Index = 0, Items = Array.Empty<NetworkItem>() });
        adapter.HandlePacket(new ReceivedItemsPacket { Index = 0, Items = new[] { new NetworkItem { Item = 123, Location = -1, Player = 1 } } });
        Check(feed.History.Count == 2 && feed.History[0].Text.StartsWith("Found "), "self receipt must produce exactly one found entry");

        var next = new ItemNotificationSession(session, 2, feed, action => action());
        var sdkReceive = session.Items.GetType().GetMethod("Socket_PacketReceived", BindingFlags.Instance | BindingFlags.NonPublic)!;
        void Deliver(int index, int count)
        {
            var packet = new ReceivedItemsPacket { Index = index, Items = Enumerable.Repeat(
                new NetworkItem { Item = 123, Location = -1, Player = 0 }, count).ToArray() };
            sdkReceive.Invoke(session.Items, new object[] { packet });
            next.HandlePacket(packet);
        }
        Deliver(0, 400);
        next.HandleMessage(Send(1, 2, 20));
        Deliver(400, 2);
        // Repeats and unrelated traffic must not consume send capacity.
        for (int i = 0; i < 600; i++)
        {
            next.HandleMessage(Send(2, 3, 1000 + i));
            next.HandleMessage(Send(1, 2, 20));
        }
        for (int i = 0; i < 600; i++) next.HandleMessage(Send(1, 2, 1000 + i));
        int existing = feed.OutstandingCount;
        next.Configure("seed", 0, 1, CassetteBundleContract.Parse(null));
        Check(session.Items.AllItemsReceived.Count == 402, "notification limits must preserve SDK receipt history");
        int deliveries = 0;
        while (session.Items.Any()) { session.Items.DequeueItem(); deliveries++; }
        Check(deliveries == 402, "notification replay must leave every SDK delivery for the gameplay consumer");
        Check(feed.OutstandingCount == existing + 401 + 512, "pre-login limits must keep every unseen receipt and bound distinct confirmed sends");
        var presented = new List<ItemNotification>();
        feed.Advance(0);
        while (feed.Visible.Count > 0) { presented.Add(feed.Visible[0]); feed.Advance(7, 1); }
        Check(presented[existing + 398].Sent == false && presented[existing + 399].Sent &&
            !presented[existing + 400].Sent && !presented[existing + 401].Sent,
            "pre-login receipt batches and confirmed sends must preserve arrival order");
        Console.WriteLine("PASS: real MultiClient.Net sends, hint/self/unrelated filtering, buffered login and send replay");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference BufferInitialPacket(ItemNotificationSession adapter)
    {
        var items = new NetworkItem[1000];
        adapter.HandlePacket(new ReceivedItemsPacket { Index = 0, Items = items });
        return new WeakReference(items);
    }
    private static void CheckDelayedReceiptHistory()
    {
        var session = ArchipelagoSessionFactory.CreateSession("localhost", 1);
        var feed = new ItemNotificationFeed();
        var adapter = new ItemNotificationSession(session, 1, feed, action => action());
        var buffer = BufferInitialPacket(adapter);
        GC.Collect();
        Check(!buffer.IsAlive, "pre-login notification staging must release the packet's item array");
        adapter.Configure("late-ledger", 0, 1, CassetteBundleContract.Parse(null));
        var replay = new ReceivedItemsPacket { Index = 0, Items = new NetworkItem[1001] };
        adapter.HandlePacket(replay);
        Check(feed.History.Count == 100 && feed.OutstandingCount == 1,
            "a late full replay must recover the silent baseline and notify only the new receipt");
        adapter.HandlePacket(replay);
        Check(feed.OutstandingCount == 1, "a repeated full replay must not duplicate the recovered arrival");
    }
    private sealed class TestItemNames : IItemInfoResolver
    {
        public string GetItemName(long id, string game) => "Weed Killer";
        public string GetLocationName(long id, string game) => "Gecko";
        public long GetLocationId(string name, string game) => 10;
    }
    private sealed class TestPlayers : IPlayerHelper
    {
        private readonly PlayerInfo[] _players = new[]
        {
            new PlayerInfo(0, 1, "Local", "Jack", Plugin.GameName, Array.Empty<NetworkSlot>(), Array.Empty<int>()),
            new PlayerInfo(0, 2, "Remote", "Sam", Plugin.GameName, Array.Empty<NetworkSlot>(), Array.Empty<int>()),
            new PlayerInfo(0, 3, "Other", "Alex", Plugin.GameName, Array.Empty<NetworkSlot>(), Array.Empty<int>())
        };
        public ReadOnlyDictionary<int, ReadOnlyCollection<PlayerInfo>> Players =>
            new(new Dictionary<int, ReadOnlyCollection<PlayerInfo>> { [0] = Array.AsReadOnly(_players) });
        public IEnumerable<PlayerInfo> AllPlayers => _players;
        public PlayerInfo ActivePlayer => _players[0];
        public PlayerInfo GetPlayerInfo(int slot) => _players.Single(p => p.Slot == slot);
        public PlayerInfo GetPlayerInfo(int team, int slot) => _players.Single(p => p.Team == team && p.Slot == slot);
        public string GetPlayerAlias(int slot) => GetPlayerInfo(slot).Alias;
        public string GetPlayerName(int slot) => GetPlayerInfo(slot).Name;
        public string GetPlayerAliasAndName(int slot) => GetPlayerAlias(slot) + " (" + GetPlayerName(slot) + ")";
    }
    internal static void Run()
    {
        var session = ArchipelagoSessionFactory.CreateSession("localhost", 1);
        var feed = new ItemNotificationFeed();
        var adapter = new ItemNotificationSession(session, 1, feed, action => action());
        adapter.HandlePacket(new ReceivedItemsPacket { Index = 0, Items = Array.Empty<NetworkItem>() });
        adapter.Configure("seed",0,1,CassetteBundleContract.Parse(null));
        adapter.HandlePacket(new ReceivedItemsPacket { Index = 0, Items = new[]{new NetworkItem { Item = 123, Location = -1, Player = 0 }} });
        feed.Advance(0);
        if (feed.History.Count != 1 || feed.Visible.Count != 1) throw new Exception("real packet adapter loses new arrival after empty initial sync");
        adapter.HandlePacket(new ReceivedItemsPacket { Index = 0, Items = new[]{new NetworkItem { Item = 123, Location = -1, Player = 0 }} });
        if (feed.History.Count != 1) throw new Exception("real packet adapter duplicates full-history replay");
        var reconnected = new ItemNotificationSession(session, 2, feed, action => action());
        reconnected.Configure("seed", 0, 1, CassetteBundleContract.Parse(null));
        Check(feed.FirstUnseenOffset(2, 0, 2) == 1 && feed.FirstUnseenOffset(2, 0, 1) == null,
            "a reconnect resolves only unseen receipt names");
        reconnected.HandlePacket(new ReceivedItemsPacket { Index = 0, Items = new[] {
            new NetworkItem { Item = 123, Location = -1, Player = 0 },
            new NetworkItem { Item = 456, Location = -1, Player = 1 }
        } });
        Check(feed.History.Count == 2 && feed.History[0].Text.StartsWith("Found "),
            "a replay with an offline arrival publishes its suffix once");
        Console.WriteLine("PASS: real MultiClient.Net received-packet adapter");
        var bundleFeed=new ItemNotificationFeed();
        var bundleAdapter=new ItemNotificationSession(session,2,bundleFeed,action=>action());
        var bundleData=new Dictionary<string,object>{["schema_version"]=20,["cassette_bundle_size"]=2,["cassette_bundles"]=Enumerable.Range(0,15).ToDictionary(n=>$"Cassette Bundle {n+1:00}",n=>CassetteCatalog.All.Skip(n*2).Take(2).Select(c=>c.ItemName).ToArray())};
        bundleAdapter.HandlePacket(new ReceivedItemsPacket{Index=0,Items=Array.Empty<NetworkItem>()});
        bundleAdapter.Configure("bundle-seed",0,1,CassetteBundleContract.Parse(bundleData));
        bundleAdapter.HandlePacket(new ReceivedItemsPacket{Index=0,Items=new[]{new NetworkItem{Item=187256164,Location=-1,Player=0}}});
        bundleFeed.Advance(0);
        Check(bundleFeed.History.Single().Text.Contains("The Little Things, No Plan B"),"bundle receipt history resolves songs by permanent ID even before data-package names are available");
        Check(bundleFeed.Visible.Single().Text.Contains("The Little Things, No Plan B"),"bundle popup names every received song");
        CheckSentMessages();
        CheckDelayedReceiptHistory();
    }
}
namespace RhythmCastleAP
{
 internal static class Plugin { internal const string GameName = "Super Crazy Rhythm Castle"; internal static NotificationTestLogger? LoggerInstance = new(); }
 internal sealed class NotificationTestLogger { internal void LogWarning(string message) { Console.WriteLine(message); } }
}

