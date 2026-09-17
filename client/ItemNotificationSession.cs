using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;

namespace RhythmCastleAP;

internal static class ItemNotifications
{
    internal static readonly ItemNotificationFeed Feed = new();
    internal static ItemNotificationSession CreateSession(object rawSession, long generation, Action<Action> dispatch) =>
        new((ArchipelagoSession)rawSession, generation, Feed, dispatch);
}

internal sealed class ItemNotificationSession
{
    private readonly object _sync = new();
    private readonly ArchipelagoSession _session;
    private readonly long _generation;
    private readonly ItemNotificationFeed _feed;
    private readonly List<object> _beforeLogin = new();
    private bool _ready;
    private int _slot;
    internal ItemNotificationSession(ArchipelagoSession session, long generation, ItemNotificationFeed feed, Action<Action> dispatch)
    {
        _session = session; _generation = generation; _feed = feed;
        session.MessageLog.OnMessageReceived += message => dispatch(() => HandleMessage(message));
    }
    internal void HandleMessage(LogMessage message)
    {
        // Hints inherit ItemSendLogMessage but are not actual item deliveries.
        if (message.GetType() == typeof(ItemSendLogMessage)) Accept(message);
    }
    internal void HandlePacket(object packet)
    {
        if (packet is ReceivedItemsPacket) Accept(packet);
    }
    internal void Configure(string seed, int team, int slot)
    {
        lock (_sync)
        {
            _slot = slot;
            _feed.Connect(_generation, System.Text.Json.JsonSerializer.Serialize(new { seed, team, slot }));
            _ready = true;
            foreach (object pending in _beforeLogin) SafePublish(pending);
            _beforeLogin.Clear();
        }
    }
    private void Accept(object message)
    {
        try
        {
            lock (_sync)
            {
                if (!_ready) { _beforeLogin.Add(message); return; }
                SafePublish(message);
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] Item notification unavailable: {ex.Message}");
        }
    }
    private void SafePublish(object message)
    {
        try { Publish(message); }
        catch (Exception ex) { Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] Item notification unavailable: {ex.Message}"); }
    }
    private void Publish(object message)
    {
        if (message is ReceivedItemsPacket received && received.Items != null)
        {
            var entries = received.Items.Select(item =>
            {
                var player = _session.Players.GetPlayerInfo(item.Player);
                string name = item.Player == 0 ? "Server" : player?.Alias ?? player?.Name ?? "Unknown player";
                string location = item.Location < 0 ? "Archipelago" :
                    _session.Locations.GetLocationNameFromId(item.Location, player?.Game ?? Plugin.GameName);
                return new NotificationReceipt(_session.Items.GetItemName(item.Item, Plugin.GameName), name, location, item.Player == _slot);
            }).ToArray();
            _feed.Receive(_generation, received.Index, entries);
        }
        else if (message is ItemSendLogMessage sent && sent.IsSenderTheActivePlayer && !sent.IsReceiverTheActivePlayer)
        {
            string key = $"{sent.Sender.Slot}:{sent.Item.LocationId}:{sent.Receiver.Slot}:{sent.Item.ItemId}";
            _feed.Send(_generation, key, sent.Item.ItemName,
                sent.Receiver.Alias ?? sent.Receiver.Name, sent.Item.LocationName);
        }
    }
}

