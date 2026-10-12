using System.Reflection;
namespace RhythmCastleAP;

// Temporary read-only diagnosis of the native Change-button entry guards.
internal static class SelectorAvailabilityDiagnostics
{
    private static TimeSpan _remaining;
    private static string _identity = "", _last = "";
    private static int _observations;
    internal static void TickUnity(TimeSpan elapsed)
    {
        if (!DeveloperHarness.Enabled) return;
        _remaining -= elapsed;
        if (_remaining > TimeSpan.Zero) return;
        _remaining = TimeSpan.FromSeconds(3);
        CassetteReceiptRandomization.WithStableQuestItemSave((_, identity) =>
        {
            string key = identity + "/" + DeveloperHarness.CurrentRoomId;
            if (key != _identity) { _identity = key; _last = ""; _observations = 0; }
            if (_observations++ >= 20) return;
            string status;
            try
            {
                Assembly? game = ReflectionUtil.GameAssembly;
                Type? players = game?.GetType("PlayerEnquiries");
                object? roomAllowed = players?.GetMethod("InARunningRoomThatIsNotALevel", Type.EmptyTypes)?.Invoke(null, null);
                object? room = game?.GetType("GameRoomEnquiries")?.GetMethod("TryGetCurrentRoomState", Type.EmptyTypes)?.Invoke(null, null);
                object? level = game?.GetType("LevelEnquiries")?.GetMethod("GetCurrentLevelStateInternal", new[] { typeof(bool) })?.Invoke(null, new object[] { true });
                status = $"room='{DeveloperHarness.CurrentRoomId}' menuRoomAllowed={roomAllowed ?? "unavailable"} roomPhase={ReflectionUtil.ReadMember(room, "CurrentPhase") ?? "unavailable"} levelPhase={ReflectionUtil.ReadMember(level, "CurrentPhase") ?? "unavailable"} activeLevel={ReflectionUtil.ReadMember(level, "ActiveLevel") ?? "unavailable"}";
                object? ids = players?.GetMethod("GetAllLocalPlayers", Type.EmptyTypes)?.Invoke(null, null);
                object? iterator = ids?.GetType().GetMethod("GetEnumerator", Type.EmptyTypes)?.Invoke(ids, null);
                MethodInfo? next = iterator?.GetType().GetMethod("MoveNext", Type.EmptyTypes);
                PropertyInfo? current = iterator?.GetType().GetProperty("Current");
                Type? action = game?.GetType("eWorldPlayerInputAction");
                for (int i = 0; i < 4 && iterator != null && Equals(next?.Invoke(iterator, null), true); i++)
                {
                    object? player = current?.GetValue(iterator);
                    if (player == null || action == null) continue;
                    // Native RunUpdateForPlayer passes world action 1 before reading the hold input.
                    MethodInfo? allowed = game?.GetType("PlayerInputEnquiries")?.GetMethod("IsPlayerAllowedToPerformAction", new[] { player.GetType(), action });
                    object? permission = allowed?.Invoke(null, new[] { player, Enum.ToObject(action, 1) });
                    status += $" player[{i}]ChangeAllowed={permission ?? "unavailable"}";
                }
            }
            catch (Exception ex) { status = "read unavailable: " + ex.GetBaseException().Message; }
            if (status == _last) return;
            _last = status;
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] SELECTOR ENTRY DIAGNOSTIC " + status);
        });
    }
}
