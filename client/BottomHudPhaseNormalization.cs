using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RhythmCastleAP;

internal sealed class BottomHudPhaseKeeper : MonoBehaviour
{
    private const string RootsControlPath =
        "Root/GameRoom_Hub2_Logic/Objects/AmProContainer/AmProRobot";

    private string _room = string.Empty;
    private int _cooldown;
    private bool _complete;
    private bool _waitingLogged;

    public BottomHudPhaseKeeper(IntPtr pointer) : base(pointer) { }

    private void Update()
    {
        string room = DeveloperHarness.CurrentRoomId;
        if (!string.Equals(room, _room, StringComparison.OrdinalIgnoreCase))
        {
            _room = room;
            _cooldown = 30;
            _complete = false;
            _waitingLogged = false;
        }

        if (_complete || _cooldown-- > 0 ||
            !AreaAccessPrototype.Enabled || !IntroHubSkip.Compatible)
            return;

        GameObject? root = string.Equals(room, "GameRoom_Hub2", StringComparison.OrdinalIgnoreCase)
            ? GameObject.Find(RootsControlPath)
            : GameObject.Find("AmProRobot");
        if (root == null)
            return;

        if (!NativeBottomHudPhase.TryRead(root, out int phase, out string detail))
        {
            if (!_waitingLogged)
            {
                _waitingLogged = true;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] BOTTOM HUD PHASE waiting room='{room}' detail='{detail}'.");
            }
            _cooldown = 30;
            return;
        }

        BottomHudPhaseDecision decision = BottomHudPhasePolicy.Decide(
            enabled: true, compatible: true, room, phase);
        if (decision == BottomHudPhaseDecision.Preserve)
        {
            _complete = true;
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] BOTTOM HUD PHASE preserved room='{room}' nativePhase={phase}; only INVALID(0) is normalized.");
            return;
        }

        if (!NativeBottomHudPhase.TrySetIdle(root, out int verifiedPhase, out detail))
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] BOTTOM HUD PHASE normalization failed room='{room}' nativePhase={phase} detail='{detail}'.");
            _cooldown = 60;
            return;
        }

        _complete = true;
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] BOTTOM HUD PHASE normalized room='{room}' from=INVALID(0) to=IDLE({verifiedPhase}); no difficulty, character, or progression flag was selected or changed.");
    }
}

internal static class NativeBottomHudPhase
{
    private const string NativeLibrary = "GameAssembly.dll";

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_object_get_class(IntPtr obj);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_get_parent(IntPtr klass);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_get_method_from_name(
        IntPtr klass,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        int argsCount);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_runtime_invoke(
        IntPtr method,
        IntPtr obj,
        IntPtr parameters,
        ref IntPtr exception);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_object_unbox(IntPtr obj);

    internal static bool TryRead(GameObject root, out int phase, out string detail)
    {
        phase = -1;
        if (!TryResolve(root, out IntPtr toggler, out IntPtr togglerClass, out detail))
            return false;
        return TryRead(toggler, togglerClass, out phase, out detail);
    }

    internal static bool TrySetIdle(GameObject root, out int verifiedPhase, out string detail)
    {
        verifiedPhase = -1;
        if (!TryResolve(root, out IntPtr toggler, out IntPtr togglerClass, out detail))
            return false;

        IntPtr setPhase = FindMethodInHierarchy(togglerClass, "SetPhase", 1);
        if (setPhase == IntPtr.Zero)
        {
            detail = "DifficultyToggler.SetPhase(phase) was not found";
            return false;
        }

        IntPtr value = IntPtr.Zero;
        IntPtr args = IntPtr.Zero;
        try
        {
            value = Marshal.AllocHGlobal(sizeof(int));
            Marshal.WriteInt32(value, 1); // eDifficultyTogglerPhase.IDLE
            args = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(args, value);
            IntPtr exception = IntPtr.Zero;
            il2cpp_runtime_invoke(setPhase, toggler, args, ref exception);
            if (exception != IntPtr.Zero)
            {
                detail = $"SetPhase(IDLE) returned IL2CPP exception 0x{exception.ToInt64():X}";
                return false;
            }
        }
        catch (Exception ex)
        {
            detail = ex.GetBaseException().Message;
            return false;
        }
        finally
        {
            if (args != IntPtr.Zero) Marshal.FreeHGlobal(args);
            if (value != IntPtr.Zero) Marshal.FreeHGlobal(value);
        }

        if (!TryRead(toggler, togglerClass, out verifiedPhase, out detail))
            return false;
        if (verifiedPhase != 1)
        {
            detail = $"verification returned phase {verifiedPhase}, expected IDLE(1)";
            return false;
        }
        detail = "verified through DifficultyToggler.BuildState().CurrentPhase";
        return true;
    }

    private static bool TryRead(
        IntPtr toggler,
        IntPtr togglerClass,
        out int phase,
        out string detail)
    {
        phase = -1;
        try
        {
            IntPtr buildState = FindMethodInHierarchy(togglerClass, "BuildState", 0);
            if (buildState == IntPtr.Zero)
            {
                detail = "DifficultyToggler.BuildState() was not found";
                return false;
            }

            IntPtr exception = IntPtr.Zero;
            IntPtr state = il2cpp_runtime_invoke(
                buildState, toggler, IntPtr.Zero, ref exception);
            if (exception != IntPtr.Zero || state == IntPtr.Zero)
            {
                detail = exception != IntPtr.Zero
                    ? $"BuildState() returned IL2CPP exception 0x{exception.ToInt64():X}"
                    : "BuildState() returned null";
                return false;
            }

            IntPtr stateClass = il2cpp_object_get_class(state);
            IntPtr getter = FindMethodInHierarchy(stateClass, "get_CurrentPhase", 0);
            if (getter == IntPtr.Zero)
            {
                detail = "DifficultyTogglerState.get_CurrentPhase() was not found";
                return false;
            }

            exception = IntPtr.Zero;
            IntPtr boxed = il2cpp_runtime_invoke(
                getter, state, IntPtr.Zero, ref exception);
            IntPtr unboxed = boxed == IntPtr.Zero ? IntPtr.Zero : il2cpp_object_unbox(boxed);
            if (exception != IntPtr.Zero || unboxed == IntPtr.Zero)
            {
                detail = exception != IntPtr.Zero
                    ? $"get_CurrentPhase() returned IL2CPP exception 0x{exception.ToInt64():X}"
                    : "get_CurrentPhase() returned null";
                return false;
            }

            phase = Marshal.ReadInt32(unboxed);
            detail = "read through DifficultyToggler.BuildState().CurrentPhase";
            return true;
        }
        catch (Exception ex)
        {
            detail = ex.GetBaseException().Message;
            return false;
        }
    }

    private static bool TryResolve(
        GameObject root,
        out IntPtr toggler,
        out IntPtr togglerClass,
        out string detail)
    {
        toggler = IntPtr.Zero;
        togglerClass = IntPtr.Zero;
        try
        {
            MethodInfo? getByName = typeof(GameObject).GetMethod(
                "GetComponent",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null);
            object? component = getByName?.Invoke(root, new object[] { "DifficultyToggler" });
            if (component == null)
            {
                detail = "native DifficultyToggler component was not found";
                return false;
            }

            PropertyInfo? pointerProperty = component.GetType().GetProperty(
                "Pointer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            toggler = pointerProperty?.GetValue(component) is IntPtr pointer
                ? pointer
                : IntPtr.Zero;
            if (toggler == IntPtr.Zero)
            {
                detail = "native DifficultyToggler pointer was null";
                return false;
            }

            togglerClass = il2cpp_object_get_class(toggler);
            if (togglerClass == IntPtr.Zero)
            {
                detail = "native DifficultyToggler class was null";
                return false;
            }
            detail = "resolved";
            return true;
        }
        catch (Exception ex)
        {
            detail = ex.GetBaseException().Message;
            return false;
        }
    }

    private static IntPtr FindMethodInHierarchy(IntPtr klass, string name, int argCount)
    {
        for (int depth = 0; klass != IntPtr.Zero && depth < 12; depth++)
        {
            IntPtr method = il2cpp_class_get_method_from_name(klass, name, argCount);
            if (method != IntPtr.Zero)
                return method;
            klass = il2cpp_class_get_parent(klass);
        }
        return IntPtr.Zero;
    }
}
