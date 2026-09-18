using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine;

namespace RhythmCastleAP;

internal static class QuestNativeVirtualHooks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool CheckCondition(IntPtr instance, IntPtr methodInfo);
    private static readonly List<Hook> Hooks = new();
    private static readonly List<VoidHook> VoidHooks = new();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void InvokeStep(IntPtr instance, IntPtr methodInfo);
    private sealed class VoidHook
    {
        internal readonly InvokeStep Replacement;
        internal InvokeStep Original = null!;
        internal INativeDetour Detour = null!;
        private readonly Func<object, bool> _admit;
        internal VoidHook(Func<object, bool> admit) { _admit = admit; Replacement = Invoke; }
        private void Invoke(IntPtr instance, IntPtr methodInfo)
        {
            if (!QuestChecks.Enabled) { Original(instance, methodInfo); return; }
            string path = "";
            if (_admit == QuestCheckHooks.CharacterUnlockStepPrefix && ExpandedChecks.State.Handles(KingUnlockSource.LocationId)) {
                try { path = QuestCheckHooks.PathFor(new Component(instance)); }
                catch (Exception ex) { Plugin.LoggerInstance?.LogError($"[SCRC-AP] King source path unavailable: {ex.GetBaseException().Message}"); }
            }
            NativeSequenceInvocation.Run(() => {
                var component = new Component(instance);
                return _admit(component);
            }, () => KingUnlockSource.RunNative(path, () => Original(instance, methodInfo)),
            ex => Plugin.LoggerInstance?.LogError($"[SCRC-AP] Quest native sequence admission failed: {ex.GetBaseException().Message}"));
        }
    }
    private sealed class Hook
    {
        internal readonly CheckCondition Replacement;
        internal CheckCondition Original = null!;
        internal INativeDetour Detour = null!;
        internal Hook() { Replacement = Invoke; }
        private bool Invoke(IntPtr instance, IntPtr methodInfo)
        {
            if (!QuestChecks.Enabled || !QuestChecksPolicy.IsConditionRoom(DeveloperHarness.CurrentRoomId))
                return Original(instance, methodInfo);
            return NativeConditionInvocation.Run(() => {
                bool result = false;
                return QuestCheckHooks.ConditionPrefix(new Component(instance), ref result) ? null : result;
            }, () => Original(instance, methodInfo),
            ex => Plugin.LoggerInstance?.LogError($"[SCRC-AP] Quest native condition observation failed: {ex.GetBaseException().Message}"));
        }
    }

    internal static unsafe int Install()
    {
        if (Hooks.Count != 0 || VoidHooks.Count != 0) return Hooks.Count + VoidHooks.Count;
        foreach (string name in QuestConditionHookPlan.DedicatedTypes)
        {
            try
            {
                IntPtr assemblyName = Marshal.StringToHGlobalAnsi("Assembly-CSharp");
                IntPtr assembly;
                try { assembly = IL2CPP.il2cpp_domain_assembly_open(IL2CPP.il2cpp_domain_get(), assemblyName); }
                finally { Marshal.FreeHGlobal(assemblyName); }
                if (assembly == IntPtr.Zero) throw new MissingMemberException("Native Assembly-CSharp");
                IntPtr owner = IL2CPP.il2cpp_class_from_name(IL2CPP.il2cpp_assembly_get_image(assembly), "", name);
                if (owner == IntPtr.Zero) throw new MissingMemberException(name);
                IntPtr method = IL2CPP.il2cpp_class_get_method_from_name(owner, "CheckIfMet", 0);
                if (method == IntPtr.Zero) throw new MissingMethodException(name, "CheckIfMet");
                var metadata = UnityVersionHandler.Wrap((Il2CppMethodInfo*)method);
                if (metadata.IsGeneric || metadata.IsInflated || (IntPtr)metadata.Class != owner ||
                    metadata.ParametersCount != 0 || (((int)metadata.Flags & 0x10) != 0) ||
                    IL2CPP.il2cpp_type_get_name_((IntPtr)metadata.ReturnType) != "System.Boolean" || metadata.MethodPointer == IntPtr.Zero)
                    throw new InvalidOperationException("Native condition signature mismatch");
                var hook = new Hook();
                hook.Detour = INativeDetour.CreateAndApply(metadata.MethodPointer, hook.Replacement, out hook.Original);
                Hooks.Add(hook);
                Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] Hooked native {name}.CheckIfMet with direct original trampoline.");
            }
            catch (Exception ex) { Plugin.LoggerInstance?.LogError($"[SCRC-AP] Quest native hook {name} unavailable: {ex.GetBaseException().Message}"); }
        }
        foreach (var spec in new (string Namespace, string Owner, string Method, Func<object, bool> Admit)[] {
            ("", "PopupNewCharacterDetailsSequenceStep", "Trigger", QuestCheckHooks.PopupPrefix),
            ("SIUtil.Scripting", "Sequence", "Begin", QuestCheckHooks.HandInSequencePrefix),
            ("", "UnlockPlayableCharacterSequenceStep", "Trigger", QuestCheckHooks.CharacterUnlockStepPrefix),
        })
        {
            try
            {
                IntPtr assemblyName = Marshal.StringToHGlobalAnsi("Assembly-CSharp");
                IntPtr assembly;
                try { assembly = IL2CPP.il2cpp_domain_assembly_open(IL2CPP.il2cpp_domain_get(), assemblyName); }
                finally { Marshal.FreeHGlobal(assemblyName); }
                if (assembly == IntPtr.Zero) throw new MissingMemberException("Native Assembly-CSharp");
                IntPtr owner = IL2CPP.il2cpp_class_from_name(IL2CPP.il2cpp_assembly_get_image(assembly), spec.Item1, spec.Item2);
                if (owner == IntPtr.Zero) throw new MissingMemberException(spec.Item2);
                IntPtr method = IL2CPP.il2cpp_class_get_method_from_name(owner, spec.Item3, 0);
                if (method == IntPtr.Zero) throw new MissingMethodException(spec.Item2, spec.Item3);
                var metadata = UnityVersionHandler.Wrap((Il2CppMethodInfo*)method);
                if (metadata.IsGeneric || metadata.IsInflated || (IntPtr)metadata.Class != owner ||
                    metadata.ParametersCount != 0 || (((int)metadata.Flags & 0x10) != 0) ||
                    IL2CPP.il2cpp_type_get_name_((IntPtr)metadata.ReturnType) != "System.Void" || metadata.MethodPointer == IntPtr.Zero)
                    throw new InvalidOperationException("Native sequence signature mismatch");
                var hook = new VoidHook(spec.Item4);
                hook.Detour = INativeDetour.CreateAndApply(metadata.MethodPointer, hook.Replacement, out hook.Original);
                VoidHooks.Add(hook);
                Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] Hooked native {spec.Item2}.{spec.Item3} with direct original trampoline.");
            }
            catch (Exception ex) { Plugin.LoggerInstance?.LogError($"[SCRC-AP] Quest native hook {spec.Item2}.{spec.Item3} unavailable: {ex.GetBaseException().Message}"); }
        }
        return Hooks.Count + VoidHooks.Count;
    }
}
