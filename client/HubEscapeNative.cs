using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
namespace RhythmCastleAP;

// Caller must first match the exact scene path and authenticated area ownership.
internal static class HubEscapeNative
{
    internal static bool TryClear(IntPtr instance, out string detail)
    {
        detail = "missing Switch";
        if (instance == IntPtr.Zero) return false;
        IntPtr klass = IL2CPP.il2cpp_object_get_class(instance);
        if (Name(klass) != "Switch") return false;
        IntPtr field = FindField(klass, "requiredCondition");
        if (field == IntPtr.Zero) { detail = "requiredCondition field missing"; return false; }
        IntPtr condition = IL2CPP.il2cpp_field_get_value_object(field, instance);
        if (condition == IntPtr.Zero) { detail = "already clear"; return true; }
        IntPtr conditionClass = IL2CPP.il2cpp_object_get_class(condition);
        if (Name(conditionClass) != "IsGameProgressionFlagSetCondition") {
            detail = "unexpected condition type"; return false;
        }
        IntPtr flagField = FindField(conditionClass, "flag");
        if (flagField == IntPtr.Zero) { detail = "condition flag missing"; return false; }
        IntPtr boxed = IL2CPP.il2cpp_field_get_value_object(flagField, condition);
        if (boxed == IntPtr.Zero || Marshal.ReadInt32(IL2CPP.il2cpp_object_unbox(boxed)) != 116001) {
            detail = "condition is not Central Mainframe attempt"; return false;
        }
        // Dedicated object-reference setter supplies the write barrier. Never use a raw memory write.
        IL2CPP.il2cpp_field_set_value_object(instance, field, IntPtr.Zero);
        bool cleared = IL2CPP.il2cpp_field_get_value_object(field, instance) == IntPtr.Zero;
        detail = cleared ? "attempt requirement cleared" : "reference write did not persist";
        return cleared;
    }
    private static string? Name(IntPtr klass) => klass == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(klass));
    private static IntPtr FindField(IntPtr klass, string name)
    {
        for (int depth=0; klass != IntPtr.Zero && depth<12; depth++) {
            IntPtr field = IL2CPP.il2cpp_class_get_field_from_name(klass, name);
            if (field != IntPtr.Zero) return field;
            klass = IL2CPP.il2cpp_class_get_parent(klass);
        }
        return IntPtr.Zero;
    }
}
