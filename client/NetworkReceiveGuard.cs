using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using Archipelago.MultiClient.Net.Helpers;
using HarmonyLib;

namespace RhythmCastleAP;

// The pinned dependency does not expose a receive limit or custom transport
// factory. Replace only its message reader; retain its packet parsing, events,
// sends and login behavior. The shared SDK can outlive this plugin's session
// during asynchronous disconnect, so keep its process-wide reader protected.
internal static class NetworkReceiveGuard
{
    private static readonly ConditionalWeakTable<object, ReceiveTrafficBudget> TrafficBudgets = new();
    internal static void RegisterRoomSocket(object helper) => TrafficBudgets.GetOrCreateValue(helper);
    private const string Owner = "jack.rhythmcastle.archipelago.receive-security";
    private static readonly object Gate = new();
    private static readonly Type HelperType = typeof(BaseArchipelagoSocketHelper<ClientWebSocket>);
    // Module IDs from both reviewed net6.0 DLLs in the 6.7.1 NuGet package.
    // This checks loader compatibility. Release validation checks the file hashes.
    private static readonly Guid SdkModule = new("eb248f74-6bb2-4fbf-a23d-204aea8a4180");
    private static readonly Guid JsonModule = new("64a411ed-0e88-47b6-8a4f-42ad088958b9");
    private static readonly FieldInfo SocketField = HelperType.GetField("Socket", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(HelperType.FullName, "Socket");
    private static readonly MethodInfo ClosedMethod = HelperType.GetMethod("OnSocketClosed", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(HelperType.FullName, "OnSocketClosed");

    internal static void EnsureInstalled()
    {
        lock (Gate)
        {
            VerifyDependencies(HelperType.Assembly, typeof(Newtonsoft.Json.JsonReader).Assembly);
            var target = HelperType.GetMethod("ReadMessageAsync", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(byte[]) }, null);
            if (target == null || target.ReturnType != typeof(Task<string>) || SocketField.FieldType != typeof(ClientWebSocket)
                || ClosedMethod.ReturnType != typeof(void) || ClosedMethod.GetParameters().Length != 0)
                throw new InvalidOperationException("Cannot install the Archipelago receive protection; connection refused.");
            if (Harmony.GetPatchInfo(target)?.Prefixes.Any(p => p.owner == Owner) == true) return;
            new Harmony(Owner).Patch(target, prefix: new HarmonyMethod(typeof(NetworkReceiveGuard), nameof(ReadPrefix)));
            if (Harmony.GetPatchInfo(target)?.Prefixes.Any(p => p.owner == Owner) != true)
                throw new InvalidOperationException("Archipelago receive protection was not installed; connection refused.");
        }
    }

    internal static void VerifyDependencies(Assembly sdk, Assembly json)
    {
        if (sdk.ManifestModule.ModuleVersionId != SdkModule || json.ManifestModule.ModuleVersionId != JsonModule)
            throw new InvalidOperationException("Archipelago networking requires the reviewed MultiClient.Net 6.7.1 and bundled Newtonsoft.Json DLLs. Restore both from the client download.");
    }

    private static bool ReadPrefix(BaseArchipelagoSocketHelper<ClientWebSocket> __instance, byte[] __0, ref Task<string> __result)
    {
        var socket = SocketField.GetValue(__instance) as ClientWebSocket
            ?? throw new InvalidOperationException("Archipelago receive socket unavailable.");
        TrafficBudgets.TryGetValue(__instance, out var budget);
        __result = BoundedWebSocketReceive.ReadAsync(socket, __0, () => ClosedMethod.Invoke(__instance, null), budget: budget);
        return false;
    }
}
