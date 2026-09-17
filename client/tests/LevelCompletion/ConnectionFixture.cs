using System.Reflection;

namespace RhythmCastleAP;

// Compile the unchanged production ArchipelagoClient; replace only external
// networking and unrelated native subsystems. Campaign policy, lifecycle leases,
// reconnect admission, local deduplication and queue delivery remain real.
internal static class ConnectionTests
{
    internal static void Run(Func<Dictionary<string, object>> contract)
    {
        // Previous item ownership must stop before a replacement authenticates,
        // including a replacement rejected for an incompatible item contract.
        var questClient = new ArchipelagoClient("test", "slot", "", false);
        Connect(questClient, Login(contract()), true);
        CharacterQuestItems.State.Configure(1, true);
        CharacterQuestItems.State.Publish(1, new long[] { 187256159 });
        Check(true, CharacterQuestItems.State.Snapshot.Ready, "previous ownership setup");
        var badQuestContract = contract();
        badQuestContract["randomize_character_quest_items"] = true;
        var replacementQuest = Login(badQuestContract);
        bool readyDuringLogin = true;
        replacementQuest.BeforeLoginReturns = () => readyDuringLogin = CharacterQuestItems.State.Snapshot.Ready;
        Connect(questClient, replacementQuest, false);
        Check(false, readyDuringLogin, "replacement suspends grants before authentication");
        Check(false, CharacterQuestItems.State.Snapshot.Ready, "invalid replacement cannot retain old ownership");
        questClient.Shutdown();
        UnauthenticatedTransportCannotFlush(contract);
        foreach (string replacement in new[] { "same", "seed", "team", "slot" })
        {
            CampaignLevelRandomization.Shutdown();
            var client = new ArchipelagoClient("test", "slot", "", false);
            var first = Login(contract());
            Connect(client, first, true);
            first.Socket.Close();
            Connect(client, Login(contract(), success: false), false);
            var offline = CampaignLevelRandomization.EvaluatePersistedResult("Level_05", "LevelVariant_Default", 1);
            Check(2, offline.Count, "failed retry retains the validated campaign contract for offline results");
            client.QueueCampaignResult("Level_05", "LevelVariant_Default", 1);
            client.QueueCampaignResult("Level_05", "LevelVariant_Default", 1);
            var next = Login(contract(), seed: replacement == "seed" ? "other-seed" : "seed",
                team: replacement == "team" ? 1 : 0, slot: replacement == "slot" ? 2 : 1);
            Connect(client, next, true);
            Check(replacement == "same" ? 2 : 0, next.Locations.SentCount,
                "pending checks flush once only to the same authenticated identity: " + replacement);
            Flush(client);
            Check(replacement == "same" ? 2 : 0, next.Locations.SentCount, "second flush cannot duplicate checks");
            if (replacement != "same")
            {
                client.QueueCampaignResult("Level_05", "LevelVariant_Default", 1);
                Flush(client);
                // QueueCampaignResult also launches background flushes. A worker
                // may own a dequeued check after our synchronous flush returns.
                Check(true, SpinWait.SpinUntil(() => next.Locations.SentCount >= 2, TimeSpan.FromSeconds(5)),
                    "new identity check delivery completes");
                Check(2, next.Locations.SentCount, "new identity may earn its own checks without stale deduplication");
            }
            client.Shutdown();
            Check(0, CampaignLevelRandomization.Snapshot.ActiveLocations.Count, "shutdown clears retained campaign state");
            client.QueueCampaignResult("Level_05", "LevelVariant_Default", 3);
            Flush(client);
            Check(2, next.Locations.SentCount, "late result after shutdown cannot deliver checks");
        }

        CampaignLevelRandomization.Shutdown();
        var rejectedClient = new ArchipelagoClient("test", "slot", "", false);
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
    }

    private static void UnauthenticatedTransportCannotFlush(Func<Dictionary<string, object>> contract)
    {
        var client = new ArchipelagoClient("test", "slot", "", false);
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

    private static ArchipelagoSession Login(Dictionary<string, object> data, bool success = true,
        string seed = "seed", int team = 0, int slot = 1) => new()
        {
            RoomState = new() { Seed = seed },
            Result = success ? new LoginSuccessful { SlotData = data, Team = team, Slot = slot } : new LoginFailure(),
        };
    private static void Connect(ArchipelagoClient client, ArchipelagoSession session, bool expected)
    {
        ArchipelagoSessionFactory.Next = session;
        Check(expected, (bool)typeof(ArchipelagoClient).GetMethod("TryConnectOnce", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(client, null)!,
            "production login outcome");
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
internal sealed class LoginFailure : LoginResult { internal string[] Errors = ["controlled authentication failure"]; }
internal static class ArchipelagoSessionFactory
{
    internal static ArchipelagoSession Next = new();
    internal static ArchipelagoSession CreateSession(string server) => Next;
}
internal sealed class ArchipelagoSession
{
    internal TestSocket Socket = new();
    internal TestItems Items = new();
    internal TestLocations Locations = new();
    internal TestRoomState RoomState = new();
    internal LoginResult Result = new LoginFailure();
    internal Action? BeforeLoginReturns;
    internal LoginResult TryConnectAndLogin(string game, string slot, ItemsHandlingFlags flags, Version version,
        string? password, bool requestSlotData)
    {
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
    internal event Action<object>? PacketReceived;
#pragma warning restore CS0067
    internal bool Connected => true;
    internal Task DisconnectAsync() => Task.CompletedTask;
    internal void Close() => SocketClosed?.Invoke("controlled transport loss");
}
internal sealed class TestRoomState { internal string Seed = "seed"; }
internal sealed class TestItems
{
#pragma warning disable CS0067
    internal event Action<TestItems>? ItemReceived;
#pragma warning restore CS0067
    internal bool Any() => false;
    internal int Index => 0;
    internal TestItem DequeueItem() => new();
}
internal sealed class TestItem
{
    internal long ItemId => 0;
    internal string ItemName => "unused";
    internal TestPlayer? Player => null;
}
internal sealed class TestPlayer { internal string Name => "unused"; }
internal sealed class TestLocations
{
    internal long[] AllLocationsChecked => Array.Empty<long>();
    internal long[] AllLocations => new long[] { 187256001, 187256211 };
    private readonly List<long> Sent = new();
    internal int SentCount { get { lock (Sent) return Sent.Count; } }
    internal long GetLocationIdFromName(string game, string name) => name switch
    { "Level 1 - Completion" => 187256001, "Level 1 - 1 Star" => 187256211, _ => -1 };
    internal void CompleteLocationChecks(long id) { lock (Sent) Sent.Add(id); }
}
internal enum MusicLabPointRuntimeMode { Synchronized, Incompatible }
internal sealed record MusicLabPointSnapshot(MusicLabPointRuntimeMode Mode, string Detail = "");
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
    internal MusicLabPointSnapshot ApplySlotData(Dictionary<string, object> data, string game, string seed, string slot) => MusicLabPointRandomization.Snapshot;
}
internal static class ReceivedItemDispatch { internal static void TryApply(string name, bool apply, params Func<string, bool>[] handlers) { } }
internal class NativeSubsystem
{
    internal static bool Enabled => false;
    internal static void ApplySlotData(Dictionary<string, object> data) { }
    internal static void ApplySlotDataStarter(Dictionary<string, object> data) { }
    internal static bool TryApplyItem(string name) => false;
    internal static bool ApplyArchipelagoItem(string name) => false;
    internal static void EndServerSync(long generation) { }
    internal static bool BeginServerSync(ArchipelagoSession session, long generation) => true;
    internal static void RequestUnityReconciliation(string reason) { }
}
internal sealed class AreaAccessPrototype : NativeSubsystem
{
    internal static void EndAuthenticatedSession() { }
}
internal sealed class IntroHubSkip : NativeSubsystem { }
internal sealed class RootsStartupBootstrap : NativeSubsystem { }
internal sealed class GarageCartridgeAccess : NativeSubsystem { }
internal sealed class WeedKillerRandomization : NativeSubsystem { }
internal sealed class PlantPipesRandomization : NativeSubsystem { }
internal sealed class CassetteReceiptRandomization : NativeSubsystem { }
internal sealed class CassetteSourceRandomization : NativeSubsystem { }
internal sealed class PreviewAbilityRandomization : NativeSubsystem { }
internal sealed class BottomHudDiagnostic : NativeSubsystem { }
internal sealed class RootsBucketRandomization : NativeSubsystem { }
internal sealed class NativeProgression : NativeSubsystem { }
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
    internal void Configure(string seed, int team, int slot) { }
}
