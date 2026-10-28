using System.Reflection;
using HarmonyLib;
using UnityEngine;
namespace RhythmCastleAP;

internal static class CellTowerPathPatches
{
    internal static bool Ready { get; private set; }
    // The native event contains one GameObject reference, passed as one pointer-sized value on Windows x64.
    // Harmony's generated bridge treats its value as a pointer for il2cpp_value_box.
    [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
    private delegate void HandleRegistered(IntPtr instance, IntPtr evt, IntPtr methodInfo);
    private static readonly HandleRegistered Replacement = Registered;
    private static HandleRegistered? _original;
    private static BepInEx.Unity.IL2CPP.Hook.INativeDetour? _detour;
    internal static unsafe int Install(Harmony harmony, Assembly? assembly)
    {
        Ready = false;
        if (assembly == null) return 0;
        try {
            IntPtr name = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi("Assembly-CSharp");
            IntPtr nativeAssembly;
            try { nativeAssembly = Il2CppInterop.Runtime.IL2CPP.il2cpp_domain_assembly_open(Il2CppInterop.Runtime.IL2CPP.il2cpp_domain_get(), name); }
            finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(name); }
            if (nativeAssembly == IntPtr.Zero) throw new MissingMemberException("Native Assembly-CSharp");
            IntPtr image = Il2CppInterop.Runtime.IL2CPP.il2cpp_assembly_get_image(nativeAssembly);
            IntPtr type = Il2CppInterop.Runtime.IL2CPP.il2cpp_class_from_name(image, "", "TurnOffObjectAfterRegistered");
            IntPtr eventClass = Il2CppInterop.Runtime.IL2CPP.il2cpp_class_from_name(image, "", "GameRoomRegisteredExistingEntitiesEvent");
            uint alignment = 0;
            if (type == IntPtr.Zero || eventClass == IntPtr.Zero ||
                Il2CppInterop.Runtime.IL2CPP.il2cpp_class_value_size(eventClass, ref alignment) != IntPtr.Size)
                throw new InvalidOperationException("Unexpected Cell path registration event ABI");
            IntPtr field = Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_field_from_name(eventClass, "<RootGameObject>k__BackingField");
            if (field == IntPtr.Zero ||
                Il2CppInterop.Runtime.IL2CPP.il2cpp_type_get_name_(Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_type(field)) != "UnityEngine.GameObject" ||
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(field) != 2 * IntPtr.Size)
                throw new InvalidOperationException("Unexpected Cell path registration event field layout");
            IntPtr target = Il2CppInterop.Runtime.IL2CPP.il2cpp_class_get_method_from_name(type, "HandleEvent", 1);
            if (target == IntPtr.Zero) throw new MissingMethodException("Cell path native registration");
            var info = Il2CppInterop.Runtime.Runtime.UnityVersionHandler.Wrap((Il2CppInterop.Runtime.Runtime.Il2CppMethodInfo*)target);
            if (info.IsGeneric || info.IsInflated || (IntPtr)info.Class != type || info.ParametersCount != 1 ||
                ((int)info.Flags & 0x10) != 0 || info.MethodPointer == IntPtr.Zero ||
                Il2CppInterop.Runtime.IL2CPP.il2cpp_type_get_name_((IntPtr)info.ReturnType) != "System.Void" ||
                Il2CppInterop.Runtime.IL2CPP.il2cpp_type_get_name_(Il2CppInterop.Runtime.IL2CPP.il2cpp_method_get_param(target, 0)) != "GameRoomRegisteredExistingEntitiesEvent")
                throw new InvalidOperationException("Unexpected Cell path registration signature");
            _detour = BepInEx.Unity.IL2CPP.Hook.INativeDetour.CreateAndApply(info.MethodPointer, Replacement, out _original);
            Ready = true;
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] CELL PATH native registration hook installed; event ABI validated, no value boxing.");
            return 1;
        } catch (Exception ex) {
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CELL PATH hook failed: {ex.GetBaseException().Message}.");
            return 0;
        }
    }
    private static void Registered(IntPtr instance, IntPtr evt, IntPtr methodInfo)
    {
        // Preserve native initialization for every object before applying the exact Cell Tower repair.
        _original!(instance, evt, methodInfo);
        if (instance == IntPtr.Zero || !AreaAccessPrototype.Enabled ||
            !AreaAccessPrototype.AuthenticatedCompatibleSession || !AreaAccessPrototype.HasArea("Cell Tower")) return;
        try { AfterRegistered(Il2CppInterop.Runtime.Runtime.Il2CppObjectPool.Get<Component>(instance)); }
        catch (Exception ex) { Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CELL PATH callback failed: {ex.GetType().Name}."); }
    }
    public static void AfterRegistered(object __instance)
    {
        if (!AreaAccessPrototype.Enabled || !AreaAccessPrototype.AuthenticatedCompatibleSession ||
            __instance is not Component component || component.gameObject == null ||
            component.name != "HangingCell_Raised") return;
        try {
            var names = new List<string>();
            for (Transform? t=component.transform; t!=null; t=t.parent) names.Add(t.name);
            names.Reverse();
            string path=string.Join("/",names);
            if (!CellTowerPathPolicy.ShouldOpen(true,true,AreaAccessPrototype.HasArea("Cell Tower"),path)) return;
            Transform? lowered=component.transform.parent?.Find("HangingCell_Lowered");
            if (lowered == null) { Plugin.LoggerInstance?.LogWarning("[SCRC-AP] CELL PATH exact lowered cell missing."); return; }
            // Mirror only Old Tom's two targets from native ShowUnlockedCells/HideLockedCells.
            // Do not invoke ApplyEscapeEffects: it also changes the bees and quest notes.
            if (lowered.gameObject.activeSelf) lowered.gameObject.SetActive(false);
            if (!component.gameObject.activeSelf) component.gameObject.SetActive(true);
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] CELL PATH Old Tom route raised for Cell Tower Access; nectar inventory, bees, story flags and AP checks unchanged.");
        } catch (Exception ex) {
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CELL PATH failed: {ex.GetType().Name}.");
        }
    }
}


