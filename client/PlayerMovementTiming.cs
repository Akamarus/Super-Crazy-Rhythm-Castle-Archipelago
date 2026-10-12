using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine;

namespace RhythmCastleAP;

// Candidate: preserve elapsed input movement across very short rendered frames.
internal static class PlayerMovementTiming
{
    private static readonly PlayerMovementNativeCall Replacement = Invoke;
    private static PlayerMovementNativeCall _original = null!;
    private static INativeDetour? _detour;
    private static readonly PlayerMovementTimingPolicy Policy = new();
    private static Func<bool>? _running;
    private static string _room = "";
    private static long _admitted, _deferred;
    private static bool _enabled, _failed;
    internal static string Summary => $"playerMovementTiming={_enabled && !_failed} admitted={_admitted} deferred={_deferred}";

    internal static unsafe bool Install()
    {
        if (_detour != null) return _enabled && !_failed;
        try
        {
            if (!OperatingSystem.IsWindows() || IntPtr.Size != 8)
                throw new PlatformNotSupportedException("Player timing supports Windows x64 only");
            var running = ReflectionUtil.GameAssembly?.GetType("GameRoomEnquiries")?.GetMethod("InARunningRoom", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
                ?? throw new MissingMethodException("GameRoomEnquiries.InARunningRoom");
            _running = (Func<bool>)running.CreateDelegate(typeof(Func<bool>));
            IntPtr name = Marshal.StringToHGlobalAnsi("Assembly-CSharp");
            IntPtr assembly;
            try { assembly = IL2CPP.il2cpp_domain_assembly_open(IL2CPP.il2cpp_domain_get(), name); }
            finally { Marshal.FreeHGlobal(name); }
            if (assembly == IntPtr.Zero) throw new MissingMemberException("Assembly-CSharp");
            IntPtr owner = IL2CPP.il2cpp_class_from_name(IL2CPP.il2cpp_assembly_get_image(assembly), "", "PlayerCharacter");
            if (owner == IntPtr.Zero) throw new MissingMemberException("PlayerCharacter");
            IntPtr method = IL2CPP.il2cpp_class_get_method_from_name(owner, "UpdateMovementFromPlayerInput", 2);
            if (method == IntPtr.Zero) throw new MissingMethodException("PlayerCharacter.UpdateMovementFromPlayerInput");
            var info = UnityVersionHandler.Wrap((Il2CppMethodInfo*)method);
            if (info.IsGeneric || info.IsInflated || (IntPtr)info.Class != owner ||
                info.ParametersCount != 2 || (((int)info.Flags & 0x10) != 0) ||
                IL2CPP.il2cpp_type_get_name_((IntPtr)info.ReturnType) != "System.Void" ||
                IL2CPP.il2cpp_type_get_name_(IL2CPP.il2cpp_method_get_param(method, 0)) != "System.Single" ||
                info.MethodPointer == IntPtr.Zero)
                throw new InvalidOperationException("Native player update signature mismatch");
            IntPtr idClass = IL2CPP.il2cpp_class_from_name(IL2CPP.il2cpp_assembly_get_image(assembly), "", "PlayerIdentifier");
            uint alignment = 0;
            if (idClass == IntPtr.Zero || !IL2CPP.il2cpp_class_is_valuetype(idClass) ||
                IL2CPP.il2cpp_class_value_size(idClass, ref alignment) != 1 ||
                IL2CPP.il2cpp_type_get_name_(IL2CPP.il2cpp_method_get_param(method, 1)) != "PlayerIdentifier")
                throw new InvalidOperationException("PlayerIdentifier must use the validated one-byte value ABI");
            _failed = false;
            Policy.Reset();

            _detour = INativeDetour.CreateAndApply(info.MethodPointer, Replacement, out _original);
            _enabled = true;
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] PLAYER MOVEMENT TIMING candidate active; movement-only minimum 2ms, elapsed carried; rendering unchanged.");
            return true;
        }
        catch (Exception ex) { Fail(ex); return false; }
    }

    private static void Invoke(IntPtr instance, float supplied, byte player, IntPtr methodInfo)
    {
        float elapsed = supplied; bool admit = true;
        try
        {
            if (_enabled && !_failed)
            {
                string room = DeveloperHarness.CurrentRoomId;
                if (room != _room) { Policy.Reset(); _room = room; }
                admit = Policy.Admit(instance.ToInt64(), Time.frameCount, supplied,
                    room.StartsWith("GameRoom_", StringComparison.Ordinal) && Time.timeScale > 0 && _running!(), out elapsed);
                if (admit) _admitted++; else _deferred++;
            }
        }
        catch (Exception ex) { Fail(ex); admit = true; elapsed = supplied; }
        // Never catch/retry the native update; arguments other than elapsed remain unchanged.
        if (admit) _original(instance, elapsed, player, methodInfo);
    }
    internal static void Reset() { Policy.Reset(); _room = ""; _admitted = _deferred = 0; }
    internal static void Shutdown()
    {
        _enabled = false; Reset();
        try { _detour?.Dispose(); _detour = null; }
        catch (Exception ex) { Fail(ex); }
    }
    private static void Fail(Exception ex)
    {
        _failed = true; Reset();
        Plugin.LoggerInstance?.LogWarning("[SCRC-AP] PLAYER MOVEMENT TIMING disabled: " + ex.GetBaseException().Message);
    }
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void PlayerMovementNativeCall(IntPtr instance, float elapsed, byte player, IntPtr methodInfo);
