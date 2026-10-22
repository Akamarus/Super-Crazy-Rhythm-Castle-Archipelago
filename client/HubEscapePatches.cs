using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RhythmCastleAP;

internal static class HubEscapePatches
{
    internal static int Install(Harmony harmony, Assembly? assembly)
    {
        if (assembly == null) return 0;
        Type? type = ReflectionUtil.SafeGetTypes(assembly).FirstOrDefault(t => t.Name == "SetActiveOnRoomInitialise");
        MethodInfo? target = type?.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SingleOrDefault(m => m.Name == "HandleEvent" && m.ReturnType == typeof(void) &&
                m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.Name == "GameRoomInitialisedEvent");
        if (target == null) {
            Plugin.LoggerInstance?.LogWarning("[SCRC-AP] CELL RETURN GATE hook unavailable; native behavior retained.");
            return 0;
        }
        try {
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(HubEscapePatches), nameof(AfterRoomInitialised)));
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] CELL RETURN GATE initialization hook installed.");
            return 1;
        } catch (Exception ex) {
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CELL RETURN GATE hook unavailable: {ex.GetType().Name}.");
            return 0;
        }
    }

    public static void AfterRoomInitialised(object __instance)
    {
        // Run only at native initialization, never scan scenes or modify movement per frame.
        if (!AreaAccessPrototype.Enabled || !AreaAccessPrototype.AuthenticatedCompatibleSession ||
            __instance is not Component component || component.gameObject == null) return;
        // Cheap name filter before building the exact hierarchy path.
        if (component.name is not ("DoorToHR5B_Open" or "DoorToHR5B_Closed")) return;
        try {
            var names = new List<string>();
            for (Transform? t = component.transform; t != null; t = t.parent) names.Add(t.name);
            names.Reverse();
            string path = string.Join("/", names);
            bool? active = HubEscapePolicy.GateActive(true, true, AreaAccessPrototype.HasArea("Cell Tower"), path);
            if (!active.HasValue) return;
            // Either half may have disabled itself in the original callback.
            // Reconcile the exact sibling pair so the open half need not receive an event.
            Transform? parent = component.transform.parent;
            Transform? open = parent?.Find("DoorToHR5B_Open");
            Transform? closed = parent?.Find("DoorToHR5B_Closed");
            if (open == null || closed == null) return;
            string entrancePath = path.Substring(0, path.LastIndexOf('/'));
            if (!HubEscapePolicy.ClearExitRequirement(true, true, AreaAccessPrototype.HasArea("Cell Tower"), entrancePath)) return;
            // Native Switch 1820 separately requires condition 1752 (level attempt).
            // Clear only this scene-local reference, not the shared condition/flag.
            Component? exitSwitch = parent!.gameObject.GetComponents<Component>().FirstOrDefault(c =>
                c != null && System.Runtime.InteropServices.Marshal.PtrToStringAnsi(
                    Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_name(
                        Il2CppInterop.Runtime.IL2CPP.il2cpp_object_get_class(c.Pointer))) == "Switch");
            bool repaired = HubEscapeNative.TryClear(exitSwitch?.Pointer ?? IntPtr.Zero, out string detail);
            if (!repaired) {
                Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CELL RETURN GATE path='{entrancePath}' failed: {detail}.");
                return;
            }
            open.gameObject.SetActive(true);
            closed.gameObject.SetActive(false);
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] CELL RETURN GATE path='{path}' return route open, exit requirement cleared; native progression unchanged.");
        } catch (Exception ex) {
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CELL RETURN GATE failed safely: {ex.GetType().Name}.");
        }
    }
}
