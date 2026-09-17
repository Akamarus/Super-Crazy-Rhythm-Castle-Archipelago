using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;

namespace RhythmCastleAP;

// The two generated condition types share one native code address. The existing
// area-condition Harmony patch owns that address; quest code must not patch it again.
internal static class QuestSharedConditionHook
{
    private static bool _aliasVerified;
    internal static bool Ready { get; private set; }
    internal static unsafe void VerifyAlias()
    {
        try
        {
            IntPtr name = Marshal.StringToHGlobalAnsi("Assembly-CSharp");
            IntPtr assembly;
            try { assembly = IL2CPP.il2cpp_domain_assembly_open(IL2CPP.il2cpp_domain_get(), name); }
            finally { Marshal.FreeHGlobal(name); }
            if (assembly == IntPtr.Zero) throw new MissingMemberException("Native Assembly-CSharp");
            IntPtr image = IL2CPP.il2cpp_assembly_get_image(assembly);
            IntPtr Pointer(string type)
            {
                IntPtr owner = IL2CPP.il2cpp_class_from_name(image, "", type);
                if (owner == IntPtr.Zero) return IntPtr.Zero;
                IntPtr method = IL2CPP.il2cpp_class_get_method_from_name(owner, "CheckIfMet", 0);
                if (method == IntPtr.Zero) return IntPtr.Zero;
                var metadata = UnityVersionHandler.Wrap((Il2CppMethodInfo*)method);
                if ((IntPtr)metadata.Class != owner || metadata.ParametersCount != 0 || metadata.IsGeneric || metadata.IsInflated ||
                    (((int)metadata.Flags & 0x10) != 0) || IL2CPP.il2cpp_type_get_name_((IntPtr)metadata.ReturnType) != "System.Boolean")
                    return IntPtr.Zero;
                return metadata.MethodPointer;
            }
            _aliasVerified = QuestConditionHookPlan.IsVerifiedAlias(Pointer(QuestConditionHookPlan.SharedOwner).ToInt64(),
                Pointer(QuestConditionHookPlan.SharedAlias).ToInt64());
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] QUEST shared flag-condition native alias verified={_aliasVerified}; single owner is existing area-condition hook.");
        }
        catch (Exception ex) { Plugin.LoggerInstance?.LogError($"[SCRC-AP] Quest shared condition verification failed: {ex.GetBaseException().Message}"); }
    }
    internal static void RecordInstalled(MethodInfo method)
    {
        if (_aliasVerified && QuestConditionHookPlan.IsSharedEntry(method.DeclaringType?.Name, method.Name)) Ready = true;
    }
}
