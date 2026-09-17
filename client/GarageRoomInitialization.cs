using System.Reflection;
using HarmonyLib;

namespace RhythmCastleAP;

internal static class GarageRoomInitialization
{
    internal static readonly GarageRoomInitializationState State = new();
    internal static bool CanMutate => State.CanMutate(DeveloperHarness.CurrentRoomId);
    private static string? _lastFailure;
    private static MethodInfo? _findCurrentRoot;
    internal static int Install(Harmony harmony)
    {
        try {
            Type? owner = ReflectionUtil.GameAssembly?.GetType("GameRoomManager", true);
            MethodInfo? target = owner?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SingleOrDefault(m => m.Name == "UpdateRoomChangeTasks" && m.ReturnType == typeof(void) && !m.IsVirtual &&
                    !m.ContainsGenericParameters && m.GetParameters().Length == 0);
            if (target == null) throw new MissingMethodException("GameRoomManager", "UpdateRoomChangeTasks");
            _findCurrentRoot = owner!.GetMethod("FindRootObjectForCurrentGameRoom", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (_findCurrentRoot == null || _findCurrentRoot.IsVirtual || _findCurrentRoot.ReturnType != typeof(UnityEngine.GameObject))
                throw new MissingMethodException("GameRoomManager", "FindRootObjectForCurrentGameRoom");
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(GarageRoomInitialization), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(GarageRoomInitialization), nameof(Postfix)));
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] Garage mutation gate hooked room-manager task observation.");
            return 1;
        } catch (Exception ex) {
            Plugin.LoggerInstance?.LogError($"[SCRC-AP] Garage initialization hook unavailable; in-room AP mutations remain blocked: {ex.GetBaseException().Message}");
            return 0;
        }
    }
    private static void Prefix(out long __state) =>
        __state = DeveloperHarness.CurrentRoomId == "GameRoom_27" && !CanMutate ? State.Epoch : -1;
    private static void Postfix(object __instance, long __state)
    {
        if (__state < 0) return;
        try {

            string? task = ReflectionUtil.ReadMember(__instance, "currentTask")?.ToString();
            // Ask the native manager for its current root; its generated nullable field wrapper
            // is unreadable in the live game. Never invoke the lookup during room construction.
            string? room = GarageRoomInitializationState.ReadIdleRoom(task, () => {
                var root = _findCurrentRoot!.Invoke(__instance, null) as UnityEngine.GameObject;
                if (root == null) return null;
                return root.transform.Find("GameRoom_27_Logic/Objects/Cartridges") != null
                    ? "GameRoom_27" : null;
            });
            State.Observe(__state, room, task);
        } catch (Exception ex) {
            string failure = $"epoch={__state} failed={ex.GetBaseException().Message}";
            if (_lastFailure == failure) return;
            _lastFailure = failure;
            Plugin.LoggerInstance?.LogWarning("[SCRC-AP] Garage native readiness " + failure);
        }
    }
}

