using System.Net.WebSockets;
using System.Reflection;
using Archipelago.MultiClient.Net.Helpers;
using HarmonyLib;

namespace RhythmCastleAP;

// The pinned dependency does not expose a receive limit or custom transport
// factory. Replace only its message reader; retain its packet parsing, events,
// sends and login behavior. Refuse connections if this guard cannot be installed.
internal static class NetworkReceiveGuard
{
    private const string Owner = "jack.rhythmcastle.archipelago.receive-security";
    private static readonly object Gate = new();
    private static readonly Type HelperType = typeof(BaseArchipelagoSocketHelper<ClientWebSocket>);
    private static readonly FieldInfo SocketField = HelperType.GetField("Socket", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(HelperType.FullName, "Socket");
    private static readonly MethodInfo ClosedMethod = HelperType.GetMethod("OnSocketClosed", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(HelperType.FullName, "OnSocketClosed");

    internal static void EnsureInstalled()
    {
        lock (Gate)
        {
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

    private static bool ReadPrefix(BaseArchipelagoSocketHelper<ClientWebSocket> __instance, byte[] __0, ref Task<string> __result)
    {
        var socket = SocketField.GetValue(__instance) as ClientWebSocket
            ?? throw new InvalidOperationException("Archipelago receive socket unavailable.");
        __result = BoundedWebSocketReceive.ReadAsync(socket, __0, () => ClosedMethod.Invoke(__instance, null));
        return false;
    }
}
