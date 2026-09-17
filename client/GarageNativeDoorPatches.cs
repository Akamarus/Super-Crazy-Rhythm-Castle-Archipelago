using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;

namespace RhythmCastleAP;

internal sealed record GarageDoorConsumptionSnapshot(
    GarageCartridgeNativeDefinition Cartridge, long Generation, long ResetEpoch, long SaveBoundaryEpoch);

internal static class GarageNativeDoorPatches
{
    // Keep both the detour and delegates alive for the entire game process.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void OpenDoor(IntPtr instance, IntPtr methodInfo);
    private static INativeDetour? _detour;
    private static OpenDoor? _original;
    private static readonly OpenDoor Replacement = Invoke;

    internal static unsafe int Install()
    {
        if (_detour != null) return 1;
        try
        {
            // These native classes have unloadable generated generic base wrappers.
            // Resolve native metadata directly, without constructing either wrapper.
            IntPtr name = Marshal.StringToHGlobalAnsi("Assembly-CSharp");
            IntPtr assembly;
            try { assembly = IL2CPP.il2cpp_domain_assembly_open(IL2CPP.il2cpp_domain_get(), name); }
            finally { Marshal.FreeHGlobal(name); }
            if (assembly == IntPtr.Zero) throw new MissingMemberException("Native Assembly-CSharp");
            IntPtr owner = IL2CPP.il2cpp_class_from_name(IL2CPP.il2cpp_assembly_get_image(assembly), "", "Room27LevelEntranceDoor");
            if (owner == IntPtr.Zero) throw new MissingMemberException("Native Room27LevelEntranceDoor");
            IntPtr method = IL2CPP.il2cpp_class_get_method_from_name(owner, "OpenDoor", 0);
            if (method == IntPtr.Zero) throw new MissingMethodException("Native OpenDoor");
            var metadata = UnityVersionHandler.Wrap((Il2CppMethodInfo*)method);
            string? returns = IL2CPP.il2cpp_type_get_name_((IntPtr)metadata.ReturnType);
            if (metadata.IsGeneric || metadata.IsInflated || (IntPtr)metadata.Class != owner ||
                metadata.ParametersCount != 0 || returns != "System.Void" ||
                (((int)metadata.Flags & 0x10) != 0) || metadata.MethodPointer == IntPtr.Zero)
                throw new InvalidOperationException("Native Garage consumption signature mismatch");
            _detour = INativeDetour.CreateAndApply(metadata.MethodPointer, Replacement, out _original);
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] Hooked native Room27LevelEntranceDoor.OpenDoor() for consumption readback.");
            return 1;
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError($"[SCRC-AP] GAME GARAGE native door hook failed: {ex.GetBaseException().Message}");
            return 0;
        }
    }

    private static void Invoke(IntPtr instance, IntPtr methodInfo) =>
        NativeDoorInvocation.Run(
            () => GarageCartridgeNativePolicy.RandomizedCartridges
                .Select(GarageCartridgeAccess.CaptureNativeDoorRemoval).Where(s => s != null).ToArray(),
            () => _original!(instance, methodInfo),
            snapshots => { if (snapshots != null) foreach (var snapshot in snapshots) GarageCartridgeAccess.CompleteNativeDoorRemoval(snapshot); },
            ex => Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] GAME GARAGE door observation failed: {ex.GetBaseException().Message}"));

}
