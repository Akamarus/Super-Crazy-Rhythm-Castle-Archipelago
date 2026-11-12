using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
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
    // More than a complete Perfection slot. PrintJSON sends have no indexed history to replay.
    private const int BeforeLoginSendLimit = 512;
    private sealed record PendingSend(int ReceiptEnd, string Key, int Receiver, long ItemId, long LocationId,
        string? Item, string Player, string? Location);
    private readonly Queue<PendingSend> _sends = new();
    private readonly HashSet<string> _sendKeys = new(StringComparer.Ordinal);
    private bool _sendLimitReported;
    private bool _beforeLogin = true;
    private int _initialReceiptEnd = -1, _receiptEnd = -1;
    private int _slot;
    private CassetteBundleContract? _cassettes;
    internal ItemNotificationSession(ArchipelagoSession session, long generation, ItemNotificationFeed feed, Action<Action> dispatch)
    {
        _session = session; _generation = generation; _feed = feed;
        session.MessageLog.OnMessageReceived += message => dispatch(() => HandleMessage(message));
    }
    internal void HandleMessage(LogMessage message)
    {
        // Hints inherit ItemSendLogMessage but are not actual item deliveries.
        if (message.GetType() != typeof(ItemSendLogMessage)) return;
        var sent = (ItemSendLogMessage)message;
        try
        {
            lock (_sync)
            {
                if (!sent.IsSenderTheActivePlayer || sent.IsReceiverTheActivePlayer) return;
                string key = $"{sent.Sender.Slot}:{sent.Item.LocationId}:{sent.Receiver.Slot}:{sent.Item.ItemId}";
                if (!_beforeLogin)
                {
                    _feed.Send(_generation, key, sent.Item.ItemName, sent.Receiver.Alias ?? sent.Receiver.Name, sent.Item.LocationName);
                    return;
                }
                if (_sendKeys.Contains(key)) return;
                if (_sendKeys.Count == BeforeLoginSendLimit)
                {
                    if (!_sendLimitReported)
                        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] Extra pre-login sent-item notifications omitted after {BeforeLoginSendLimit} distinct sends.");
                    _sendLimitReported = true;
                    return;
                }
                // Keep only bounded display fields and numeric IDs. Names unavailable during
                // login can be looked up after the session loads the data package.
                _sends.Enqueue(new(_receiptEnd, key, sent.Receiver.Slot, sent.Item.ItemId, sent.Item.LocationId,
                    SavedName(sent.Item.ItemName), ItemNotificationFeed.Clean(sent.Receiver.Alias ?? sent.Receiver.Name),
                    SavedName(sent.Item.LocationName)));
                _sendKeys.Add(key);
            }
        }
        catch (Exception ex) { Warn(ex); }
    }
    internal void HandlePacket(object packet)
    {
        if (packet is not ReceivedItemsPacket received || received.Items == null || received.Index < 0 ||
            received.Items.Length > int.MaxValue - received.Index) return;
        try
        {
            lock (_sync)
            {
                if (_beforeLogin)
                {
                    // A later full replay can also supply an earlier prefix if the helper
                    // hadn't published its indexed history when Configure ran.
                    if (_cassettes != null)
                    {
                        if (received.Index == 0 && received.Items.Length >= _receiptEnd)
                            FlushBeforeLogin(i => Resolve(received.Items[i]));
                        else FlushFromSessionHistory();
                    }
                    if (_beforeLogin)
                    {
                        if (received.Index <= Math.Max(0, _receiptEnd))
                        {
                            if (_initialReceiptEnd < 0) _initialReceiptEnd = received.Items.Length;
                            _receiptEnd = Math.Max(_receiptEnd, received.Index + received.Items.Length);
                        }
                        return;
                    }
                }
                PublishReceipts(received.Index, received.Items.Length, i => Resolve(received.Items[i]));
            }
        }
        catch (Exception ex) { Warn(ex); }
    }
    internal void Configure(string seed, int team, int slot, CassetteBundleContract cassettes)
    {
        lock (_sync)
        {
            _cassettes = cassettes;
            _slot = slot;
            _feed.Connect(_generation, System.Text.Json.JsonSerializer.Serialize(new { seed, team, slot }));
            try { FlushFromSessionHistory(); }
            catch (Exception ex) { Warn(ex); }
        }
    }
    private void FlushFromSessionHistory()
    {
        // MultiClient already keeps the authenticated item ledger independently of its
        // gameplay delivery queue. Pre-login callbacks retain only two receipt positions.
        var history = _session.Items.AllItemsReceived;
        if (!_beforeLogin || (history?.Count ?? 0) < _receiptEnd) return;
        FlushBeforeLogin(i =>
        {
            var item = history![i];
            return Resolve(item.ItemId, item.LocationId, item.Player.Slot);
        });
    }
    private void FlushBeforeLogin(Func<int, NotificationReceipt> resolve)
    {
        int published = -1;
        void PublishThrough(int end)
        {
            if (end <= published) return;
            if (published < 0)
            {
                PublishReceipts(0, _initialReceiptEnd, resolve);
                published = _initialReceiptEnd;
            }
            if (end > published)
            {
                int start = published;
                PublishReceipts(start, end - start, i => resolve(start + i));
                published = end;
            }
        }
        while (_sends.TryPeek(out var sent))
        {
            PublishThrough(sent.ReceiptEnd);
            try
            {
                var item = sent.Item ?? _session.Items.GetItemName(sent.ItemId, _session.Players.GetPlayerInfo(sent.Receiver)?.Game);
                var location = sent.Location ?? _session.Locations.GetLocationNameFromId(sent.LocationId, Plugin.GameName);
                _feed.Send(_generation, sent.Key, item, sent.Player, location);
            }
            catch (Exception ex) { Warn(ex); }
            _sends.Dequeue();
        }
        PublishThrough(_receiptEnd);
        _sendKeys.Clear();
        _beforeLogin = false;
    }
    private void PublishReceipts(int index, int count, Func<int, NotificationReceipt> resolve)
    {
        if (_feed.FirstUnseenOffset(_generation, index, count) is not int skip) return;
        var entries = new NotificationReceipt[count - skip];
        for (int i = skip; i < count; i++) entries[i - skip] = resolve(i);
        _feed.Receive(_generation, index, entries, skip);
    }
    private NotificationReceipt Resolve(NetworkItem item) => Resolve(item.Item, item.Location, item.Player);
    private NotificationReceipt Resolve(long itemId, long locationId, int playerSlot)
    {
        var player = _session.Players.GetPlayerInfo(playerSlot);
        string name = playerSlot == 0 ? "Server" : player?.Alias ?? player?.Name ?? "Unknown player";
        string location = locationId < 0 ? "Archipelago" :
            _session.Locations.GetLocationNameFromId(locationId, player?.Game ?? Plugin.GameName);
        string itemName = _session.Items.GetItemName(itemId, Plugin.GameName);
        return new NotificationReceipt(_cassettes!.Display(itemId, itemName), name, location, playerSlot == _slot);
    }
    private static string? SavedName(string? value) => value == null ? null : ItemNotificationFeed.Clean(value);
    private static void Warn(Exception ex) =>
        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] Item notification unavailable: {ex.Message}");
}

