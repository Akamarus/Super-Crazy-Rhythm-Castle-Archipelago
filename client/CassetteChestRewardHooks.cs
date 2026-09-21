using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine;

namespace RhythmCastleAP;

internal static class CassetteChestRewardHooks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void InvokeReward(IntPtr instance, IntPtr methodInfo);
    private static readonly List<Hook> Hooks = new();

    private sealed class Hook
    {
        internal readonly InvokeReward Replacement;
        internal InvokeReward Original = null!;
        internal INativeDetour Detour = null!;
        private readonly bool _popup;
        internal Hook(bool popup) { _popup = popup; Replacement = Invoke; }

        private void Invoke(IntPtr instance, IntPtr methodInfo)
        {
            CassetteChestRewardAdmission.Run(CassetteReceiptRandomization.Enabled, _popup,
                () => QuestCheckHooks.PathFor(new Component(instance)),
                () =>
                {
                    bool bound = false;
                    // Selected slot, epoch, processor pointer and gameplay readiness.
                    CassetteReceiptRandomization.WithStableQuestItemSave((_, _) => bound = true);
                    return bound;
                },
                () => Original(instance, methodInfo),
                path => Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] CASSETTE CHEST {(_popup ? "POPUP" : "REWARD")} SUPPRESSED path='{path}'. Source flag and persistence remain native."),
                ex => Plugin.LoggerInstance?.LogError($"[SCRC-AP] Cassette chest source admission unavailable: {ex.GetBaseException().Message}"));
        }
    }

    internal static unsafe int Install()
    {
        if (Hooks.Count != 0) return Hooks.Count;
        CassetteChestHookInstallation.Run(TryInstall);
        return Hooks.Count;
    }

    private static unsafe bool TryInstall(bool popup)
    {
        var spec = CassetteChestHookInstallation.Target(popup);
        try
        {
            IntPtr assemblyName = Marshal.StringToHGlobalAnsi("Assembly-CSharp");
            IntPtr assembly;
            try { assembly = IL2CPP.il2cpp_domain_assembly_open(IL2CPP.il2cpp_domain_get(), assemblyName); }
            finally { Marshal.FreeHGlobal(assemblyName); }
            if (assembly == IntPtr.Zero) throw new MissingMemberException("Native Assembly-CSharp");
            IntPtr owner = IL2CPP.il2cpp_class_from_name(IL2CPP.il2cpp_assembly_get_image(assembly), "", spec.Owner);
            if (owner == IntPtr.Zero) throw new MissingMemberException(spec.Owner);
            IntPtr method = IL2CPP.il2cpp_class_get_method_from_name(owner, spec.Method, 0);
            if (method == IntPtr.Zero) throw new MissingMethodException(spec.Owner, spec.Method);
            var metadata = UnityVersionHandler.Wrap((Il2CppMethodInfo*)method);
            if (metadata.IsGeneric || metadata.IsInflated || (IntPtr)metadata.Class != owner ||
                metadata.ParametersCount != 0 || (((int)metadata.Flags & 0x10) != 0) ||
                IL2CPP.il2cpp_type_get_name_((IntPtr)metadata.ReturnType) != "System.Void" || metadata.MethodPointer == IntPtr.Zero)
                throw new InvalidOperationException("Native cassette reward signature mismatch");
            var hook = new Hook(spec.Popup);
            hook.Detour = INativeDetour.CreateAndApply(metadata.MethodPointer, hook.Replacement, out hook.Original);
            Hooks.Add(hook);
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] Hooked native {spec.Owner}.{spec.Method} for cassette chest sources.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError($"[SCRC-AP] Cassette chest hook {spec.Owner}.{spec.Method} unavailable: {ex.GetBaseException().Message}");
            return false;
        }
    }
}
