using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine;

namespace RhythmCastleAP;

// Diagnostic only. Observe the primitive player update boundary; never alter its arguments.
internal static class MovementTimingProbe
{
    private static readonly AnimalUpdateNativeCall Replacement = Invoke;
    private static AnimalUpdateNativeCall _original = null!;
    private static INativeDetour? _detour;
    private static readonly MovementTimingBuffer Buffer = new(16384);
    private static bool _enabled, _failed;
    internal static string Status => $"playerUpdateTimingHook={_enabled && !_failed}";

    internal static unsafe bool Install()
    {
        if (_detour != null) return _enabled && !_failed;
        try
        {
            if (!OperatingSystem.IsWindows() || IntPtr.Size != 8)
                throw new PlatformNotSupportedException("Player timing supports Windows x64 only");
            IntPtr name = Marshal.StringToHGlobalAnsi("Assembly-CSharp");
            IntPtr assembly;
            try { assembly = IL2CPP.il2cpp_domain_assembly_open(IL2CPP.il2cpp_domain_get(), name); }
            finally { Marshal.FreeHGlobal(name); }
            if (assembly == IntPtr.Zero) throw new MissingMemberException("Assembly-CSharp");
            IntPtr owner = IL2CPP.il2cpp_class_from_name(IL2CPP.il2cpp_assembly_get_image(assembly), "", "PlayerCharacter");
            if (owner == IntPtr.Zero) throw new MissingMemberException("PlayerCharacter");
            IntPtr method = IL2CPP.il2cpp_class_get_method_from_name(owner, "RunUpdateWithDeltaTime", 1);
            if (method == IntPtr.Zero) throw new MissingMethodException("PlayerCharacter.RunUpdateWithDeltaTime");
            var info = UnityVersionHandler.Wrap((Il2CppMethodInfo*)method);
            if (info.IsGeneric || info.IsInflated || (IntPtr)info.Class != owner ||
                info.ParametersCount != 1 || (((int)info.Flags & 0x10) != 0) ||
                IL2CPP.il2cpp_type_get_name_((IntPtr)info.ReturnType) != "System.Void" ||
                IL2CPP.il2cpp_type_get_name_(IL2CPP.il2cpp_method_get_param(method, 0)) != "System.Single" ||
                info.MethodPointer == IntPtr.Zero)
                throw new InvalidOperationException("Native player update signature mismatch");
            _failed = false;

            _detour = INativeDetour.CreateAndApply(info.MethodPointer, Replacement, out _original);
            _enabled = true;
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] PLAYER TIMING diagnostic bound; observation only while F8 is active.");
            return true;
        }
        catch (Exception ex) { Fail(ex); return false; }
    }

    private static void Invoke(IntPtr instance, float elapsed, IntPtr methodInfo)
    {
        // Only our observer is caught. The original always executes exactly once, unchanged.
        if (Buffer.Active)
        {
            try { Buffer.Record(instance.ToInt64(), Time.frameCount, elapsed); }
            catch { Buffer.Stop(); _failed = true; }
        }
        _original(instance, elapsed, methodInfo);
    }

    internal static void Begin()
    {
        Buffer.Stop();
        if (Install()) Buffer.Begin();
    }
    internal static void Stop() => Buffer.Stop();
    internal static void AppendTo(System.Text.StringBuilder output)
    {
        Buffer.Stop();
        output.AppendLine(Status);
        output.AppendLine($"playerUpdateTiming count={Buffer.Count} dropped={Buffer.Dropped} capacity=16384; elapsed is PlayerCharacter.RunUpdateWithDeltaTime argument, not sampled Unity delta; native path forwards it to player input movement. Actor pointers are capture-local.");
        for (int i = 0; i < Buffer.Count; i++)
        {
            var e = Buffer.Get(i);
            output.AppendLine(FormattableString.Invariant($"PLAYER_TICK actor=0x{e.Actor:x} frame={e.Frame} elapsed={e.Elapsed:R}"));
        }
    }
    internal static void Shutdown()
    {
        Buffer.Stop(); _enabled = false;
        try { _detour?.Dispose(); _detour = null; }
        catch (Exception ex) { Fail(ex); }
    }
    private static void Fail(Exception ex)
    {
        Buffer.Stop(); _failed = true;
        Plugin.LoggerInstance?.LogWarning("[SCRC-AP] PLAYER TIMING diagnostic unavailable: " + ex.GetBaseException().Message);
    }
}
