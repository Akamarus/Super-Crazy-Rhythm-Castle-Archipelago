using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine;

namespace RhythmCastleAP;

// Gameplay accepted in Montage at uncapped rendering rates. Never attach to UpdateMovement's nullable
// value-type signature: its generated Harmony bridge crashed before admission.
internal static class MontageAnimalTick
{
    private static readonly AnimalUpdateNativeCall Replacement = Invoke;
    private static AnimalUpdateNativeCall _original = null!;
    private static INativeDetour? _detour;
    private static Func<bool>? _roomRunning;
    private static readonly MontageAnimalTickPolicy Policy = new();
    private static string _room = "";
    private static bool _failed;
    private static bool _enabled;
    private static long _seen, _executed, _deferred;
    private static double _suppliedSeconds, _executedSeconds;
    internal static string Summary => $"montageAnimalTick={_enabled && !_failed} actors={Policy.ActorCount} seen={_seen} executed={_executed} deferred={_deferred} suppliedSeconds={_suppliedSeconds:F4} executedSeconds={_executedSeconds:F4}";

    internal static unsafe bool Install()
    {
        if (_detour != null) return _enabled && !_failed;
        try
        {
            if (!OperatingSystem.IsWindows() || IntPtr.Size != 8)
                throw new PlatformNotSupportedException("Animal timing supports Windows x64 only");
            var running = ReflectionUtil.GameAssembly?.GetType("GameRoomEnquiries")?.GetMethod(
                "InARunningRoom", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
                ?? throw new MissingMethodException("GameRoomEnquiries.InARunningRoom");
            _roomRunning = (Func<bool>)running.CreateDelegate(typeof(Func<bool>));
            IntPtr name = Marshal.StringToHGlobalAnsi("Assembly-CSharp");
            IntPtr assembly;
            try { assembly = IL2CPP.il2cpp_domain_assembly_open(IL2CPP.il2cpp_domain_get(), name); }
            finally { Marshal.FreeHGlobal(name); }
            if (assembly == IntPtr.Zero) throw new MissingMemberException("Assembly-CSharp");
            IntPtr owner = IL2CPP.il2cpp_class_from_name(IL2CPP.il2cpp_assembly_get_image(assembly), "", "MeatAnimalCharacter");
            if (owner == IntPtr.Zero) throw new MissingMemberException("MeatAnimalCharacter");
            IntPtr method = IL2CPP.il2cpp_class_get_method_from_name(owner, "RunUpdateWithDeltaTime", 1);
            if (method == IntPtr.Zero) throw new MissingMethodException("MeatAnimalCharacter.RunUpdateWithDeltaTime");
            var info = UnityVersionHandler.Wrap((Il2CppMethodInfo*)method);
            if (info.IsGeneric || info.IsInflated || (IntPtr)info.Class != owner ||
                info.ParametersCount != 1 || (((int)info.Flags & 0x10) != 0) ||
                IL2CPP.il2cpp_type_get_name_((IntPtr)info.ReturnType) != "System.Void" ||
                IL2CPP.il2cpp_type_get_name_(IL2CPP.il2cpp_method_get_param(method, 0)) != "System.Single" ||
                info.MethodPointer == IntPtr.Zero)
                throw new InvalidOperationException("Native animal update signature mismatch");
            _failed = false;
            Reset();
            _detour = INativeDetour.CreateAndApply(info.MethodPointer, Replacement, out _original);
            _enabled = true;
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] MONTAGE ANIMAL TICK active; rendering unchanged.");
            return true;
        }
        catch (Exception ex) { Fail(ex); return false; }
    }

    private static void Invoke(IntPtr instance, float elapsed, IntPtr methodInfo)
    {
        float applied = elapsed;
        bool admitted = true;
        // Catch only our decision code. Never catch and retry the original update.
        try
        {
            if (_enabled && !_failed)
            {
                string room = DeveloperHarness.CurrentRoomId;
                if (room != _room) { Reset(); _room = room; }
                if (room == "GameRoom_22")
                {
                    bool active = Time.timeScale > 0 && _roomRunning!();
                    admitted = Policy.Admit(instance.ToInt64(), Time.fixedTimeAsDouble, Time.frameCount, elapsed, active, out applied);
                    _seen++;
                    if (float.IsFinite(elapsed)) _suppliedSeconds += elapsed;
                    if (admitted) { _executed++; if (float.IsFinite(applied)) _executedSeconds += applied; }
                    else _deferred++;
                }
            }
        }
        catch (Exception ex) { Fail(ex); admitted = true; applied = elapsed; }
        if (admitted) _original(instance, applied, methodInfo);
    }

    internal static void Shutdown()
    {
        // Stop admission before detachment; keep both delegates rooted even if disposal fails.
        _enabled = false;
        Reset();
        try { _detour?.Dispose(); _detour = null; }
        catch (Exception ex) { Fail(ex); }
    }

    internal static void Reset()
    {
        Policy.Reset();
        _seen = _executed = _deferred = 0;
        _suppliedSeconds = _executedSeconds = 0;
    }
    private static void Fail(Exception ex)
    {
        _failed = true;
        Reset();
        Plugin.LoggerInstance?.LogWarning("[SCRC-AP] MONTAGE ANIMAL TICK disabled: " + ex.GetBaseException().Message);
    }
}
