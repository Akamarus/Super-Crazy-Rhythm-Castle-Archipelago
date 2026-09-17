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
        Check(feed.History.Length == 0 && feed.Visible.Length == 0, "pre-login send must stay buffered");
        adapter.Configure("seed", 0, 1);
        feed.Advance(0);
        Check(feed.History.Length == 1 && feed.Visible.Length == 1, "configure must publish buffered confirmed send");
        Check(feed.History[0].Text == "Sent Weed Killer to Sam" && feed.History[0].Location == "Gecko", "real send must resolve item, recipient alias and location");
        adapter.HandleMessage(sent);
        Check(feed.History.Length == 1, "repeated actual send must deduplicate");
        adapter.HandleMessage(Send(1, 2, 11, hint: true));
        Check(feed.History.Length == 1, "hint subclass must not become a sent popup");
        adapter.HandleMessage(Send(1, 1, 12));
        Check(feed.History.Length == 1, "self send must not duplicate receipt notification");
        adapter.HandleMessage(Send(2, 3, 13));
        Check(feed.History.Length == 1, "unrelated player send must be ignored");
        adapter.HandlePacket(new ReceivedItemsPacket { Index = 0, Items = Array.Empty<NetworkItem>() });
        adapter.HandlePacket(new ReceivedItemsPacket { Index = 0, Items = new[] { new NetworkItem { Item = 123, Location = -1, Player = 1 } } });
        Check(feed.History.Length == 2 && feed.History[0].Text.StartsWith("Found "), "self receipt must produce exactly one found entry");
        Console.WriteLine("PASS: real MultiClient.Net sends, hint/self/unrelated filtering, buffered login and send replay");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
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
        adapter.Configure("seed",0,1);
        adapter.HandlePacket(new ReceivedItemsPacket { Index = 0, Items = new[]{new NetworkItem { Item = 123, Location = -1, Player = 0 }} });
        feed.Advance(0);
        if (feed.History.Length != 1 || feed.Visible.Length != 1) throw new Exception("real packet adapter loses new arrival after empty initial sync");
        adapter.HandlePacket(new ReceivedItemsPacket { Index = 0, Items = new[]{new NetworkItem { Item = 123, Location = -1, Player = 0 }} });
        if (feed.History.Length != 1) throw new Exception("real packet adapter duplicates full-history replay");
        Console.WriteLine("PASS: real MultiClient.Net received-packet adapter");
        CheckSentMessages();
    }
}
namespace RhythmCastleAP
{
 internal static class Plugin { internal const string GameName = "Super Crazy Rhythm Castle"; internal static NotificationTestLogger? LoggerInstance = new(); }
 internal sealed class NotificationTestLogger { internal void LogWarning(string message) { Console.WriteLine(message); } }
}

