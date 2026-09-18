using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace RhythmCastleAP;

// Replaces only five scene-instance references, never a shared definition or method.
internal static class ApStarEaterThresholds
{
    private sealed record Target(string Area, string Room, string Path, int FullFlag);
    private static readonly Target[] Targets = {
        new("Roots", "GameRoom_Hub2", "Root/GameRoom_Hub2_Logic/NPCs/StarEaterNPC/StarEaterInteraction", 302),
        new("Lobby", "GameRoom_Hub1A", "Root/GameRoom_Hub1_Logic/Hub1A_Logic_MeatHubEntrance/MeatEntrance/StarEaterInteraction", 606),
        new("Cell Tower", "GameRoom_Hub5B", "Root/GameRoom_Hub5B_Logic/Objects/StarEater/StarEater/HR05B_StarEater/Interaction", 823),
        new("Royal Corridor", "GameRoom_Hub7", "Root/GameRoom_Hub7_Logic/Objects/StarEaterAndGap/StarEater/StarEaterInteraction", 1002),
        new("Secret Bunker", "GameRoom_Hub8", "Root/GameRoom_Hub8_Logic/Objects/NPCs/StarEater/Interaction", 1261),
    };
    private sealed record Replacement(Component Component, IntPtr Field, Il2CppSystem.Object Original,
        Il2CppSystem.Object Value, int Required);
    private static Replacement? _replacement;
    private static string _room = "";
    private static string _lastFailure = "";
    private static int _retryFrame;
    internal static bool CurrentRoomReady { get; private set; }
    internal static bool IsReadyForRoom(string room) =>
        CurrentRoomReady && string.Equals(_room, room, StringComparison.Ordinal) &&
        (!Targets.Any(t => t.Room == room) || (_replacement != null && _replacement.Component != null));

    // null means restore legacy state; callers must pass only a fully validated new-contract map.
    internal static void Tick(string room, IReadOnlyDictionary<string, int>? requirements)
    {
        if (requirements == null || !string.Equals(room, _room, StringComparison.Ordinal))
        {
            Restore();
            _room = room;
            _retryFrame = 0;
        }
        if (requirements == null) return;
        Target? target = Targets.FirstOrDefault(t => t.Room == room);
        if (target == null) { CurrentRoomReady = true; return; }
        if (!requirements.TryGetValue(target.Area, out int required) || required < 0 || required > 66)
        { CurrentRoomReady = false; return; }
        if (_replacement != null && _replacement.Required == required && _replacement.Component != null)
        { CurrentRoomReady = true; return; }
        if (Time.frameCount < _retryFrame) return;
        _retryFrame = Time.frameCount + 60;
        try
        {
            Restore();
            if (_replacement != null) return; // A failed restoration must retain ownership and roots.
            GameObject? go = GameObject.Find(target.Path);
            if (go == null) { CurrentRoomReady = false; return; }
            Component? component = go.GetComponents<Component>().FirstOrDefault(c => c != null &&
                Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(IL2CPP.il2cpp_object_get_class(c.Pointer))) == "StarEaterInteraction");
            if (component == null) throw new MissingMemberException("Exact StarEaterInteraction component");
            IntPtr klass = IL2CPP.il2cpp_object_get_class(component.Pointer);
            IntPtr fullField = FindField(klass, "fullFlag");
            IntPtr fullBox = IL2CPP.il2cpp_field_get_value_object(fullField, component.Pointer);
            if (fullBox == IntPtr.Zero || Marshal.ReadInt32(IL2CPP.il2cpp_object_unbox(fullBox)) != target.FullFlag)
                throw new InvalidOperationException("Star Eater source flag mismatch");
            IntPtr field = FindField(klass, "feedingThreshold");
            IntPtr originalPointer = IL2CPP.il2cpp_field_get_value_object(field, component.Pointer);
            if (originalPointer == IntPtr.Zero) throw new InvalidOperationException("Missing native threshold reference");
            var original = new Il2CppSystem.Object(originalPointer);
            IntPtr valueClass = IL2CPP.il2cpp_object_get_class(originalPointer);
            if (Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(valueClass)) != "DefinedInt")
                throw new InvalidOperationException("Unexpected native threshold class");
            IntPtr constructor = IL2CPP.il2cpp_class_get_method_from_name(valueClass, ".ctor", 1);
            if (constructor == IntPtr.Zero) throw new MissingMethodException("DefinedInt", ".ctor(Int32)");
            IntPtr newPointer = IL2CPP.il2cpp_object_new(valueClass);
            if (newPointer == IntPtr.Zero) throw new InvalidOperationException("DefinedInt allocation failed");
            var value = new Il2CppSystem.Object(newPointer); // Roots the newly allocated native object.
            IntPtr argument = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                Marshal.WriteInt32(argument, required);
                RuntimeInvoke(constructor, newPointer, new[] { argument }, out IntPtr exception);
                if (exception != IntPtr.Zero) throw new InvalidOperationException("DefinedInt constructor rejected value");
            }
            finally { Marshal.FreeHGlobal(argument); }
            // Assign through IL2CPP's field API, preserving its reference write barrier.
            SetReference(field, component.Pointer, ref newPointer);
            _replacement = new(component, field, original, value, required);
            if (IL2CPP.il2cpp_field_get_value_object(field, component.Pointer) != newPointer)
                throw new InvalidOperationException("Threshold reference write did not persist");
            CurrentRoomReady = true;
            _lastFailure = "";
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] AP STAR EATER requirement applied area='{target.Area}' required={required} exactPath='{target.Path}'.");
        }
        catch (Exception ex)
        {
            CurrentRoomReady = false;
            string reason = ex.GetBaseException().Message;
            if (_lastFailure != reason)
            {
                _lastFailure = reason;
                Plugin.LoggerInstance?.LogError($"[SCRC-AP] AP STAR EATER threshold unavailable area='{target.Area}' reason='{reason}'.");
            }
        }
    }

    internal static void Restore()
    {
        CurrentRoomReady = false;
        Replacement? replacement = _replacement;
        if (replacement == null) return;
        try
        {
            if (replacement.Component != null)
            {
                // Never overwrite another owner's later replacement.
                IntPtr current = IL2CPP.il2cpp_field_get_value_object(replacement.Field, replacement.Component.Pointer);
                if (current == replacement.Value.Pointer)
                {
                    IntPtr original = replacement.Original.Pointer;
                    SetReference(replacement.Field, replacement.Component.Pointer, ref original);
                }
            }
            _replacement = null;
        }
        catch (Exception ex)
        {
            // Keep roots on restoration failure so a still-live native reference is not abandoned.
            Plugin.LoggerInstance?.LogError($"[SCRC-AP] AP STAR EATER restore failed: {ex.GetBaseException().Message}");
        }
    }

    private static IntPtr FindField(IntPtr klass, string name)
    {
        while (klass != IntPtr.Zero)
        {
            IntPtr field = IL2CPP.il2cpp_class_get_field_from_name(klass, name);
            if (field != IntPtr.Zero) return field;
            klass = IL2CPP.il2cpp_class_get_parent(klass);
        }
        throw new MissingFieldException(name);
    }
    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_field_set_value")]
    private static extern void SetReference(IntPtr field, IntPtr instance, ref IntPtr value);
    [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_runtime_invoke")]
    private static extern IntPtr RuntimeInvoke(IntPtr method, IntPtr instance, IntPtr[] args, out IntPtr exception);
}
