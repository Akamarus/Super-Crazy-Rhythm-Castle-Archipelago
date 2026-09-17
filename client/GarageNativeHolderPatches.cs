using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Runtime;
namespace RhythmCastleAP;

internal static class GarageNativeHolderPatches
{
    // GameRoomReadyToRunEvent is an empty native value type (one byte).
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void HandleReady(IntPtr instance, byte evt, IntPtr methodInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool ReadFlag(int flag, IntPtr methodInfo);
    private static readonly HandleReady ReadyReplacement = Ready;
    private static readonly ReadFlag FlagReplacement = Flag;
    private static HandleReady? _readyOriginal;
    private static ReadFlag? _flagOriginal;
    private static INativeDetour? _readyDetour, _flagDetour;
    private static readonly Dictionary<int, string> FlagNames = new();
    [ThreadStatic] private static int _holderScope;
    internal static unsafe int Install()
    {
        try {
            IntPtr name = Marshal.StringToHGlobalAnsi("Assembly-CSharp");
            IntPtr assembly;
            try { assembly = IL2CPP.il2cpp_domain_assembly_open(IL2CPP.il2cpp_domain_get(), name); }
            finally { Marshal.FreeHGlobal(name); }
            if (assembly == IntPtr.Zero) throw new MissingMemberException("Native Assembly-CSharp");
            IntPtr image = IL2CPP.il2cpp_assembly_get_image(assembly);
            IntPtr holder = IL2CPP.il2cpp_class_from_name(image, "", "Room27GameCartridgeHolder");
            IntPtr enquiries = IL2CPP.il2cpp_class_from_name(image, "", "GameProgressionEnquiries");
            IntPtr eventClass = IL2CPP.il2cpp_class_from_name(image, "", "GameRoomReadyToRunEvent");
            uint eventAlignment = 0;
            if (holder == IntPtr.Zero || enquiries == IntPtr.Zero || eventClass == IntPtr.Zero ||
                IL2CPP.il2cpp_class_value_size(eventClass, ref eventAlignment) != 1)
                throw new InvalidOperationException("Unexpected Garage holder event ABI");
            IntPtr ready = IL2CPP.il2cpp_class_get_method_from_name(holder, "HandleEvent", 1);
            IntPtr flag = IL2CPP.il2cpp_class_get_method_from_name(enquiries, "IsFlagSet", 1);
            if (ready == IntPtr.Zero || flag == IntPtr.Zero) throw new MissingMethodException("Garage holder native boundary");
            var readyInfo = UnityVersionHandler.Wrap((Il2CppMethodInfo*)ready);
            var flagInfo = UnityVersionHandler.Wrap((Il2CppMethodInfo*)flag);
            if (readyInfo.IsGeneric || readyInfo.IsInflated || flagInfo.IsGeneric || flagInfo.IsInflated ||
                (IntPtr)readyInfo.Class != holder || (IntPtr)flagInfo.Class != enquiries ||
                readyInfo.ParametersCount != 1 || flagInfo.ParametersCount != 1 ||
                ((int)readyInfo.Flags & 0x10) != 0 || ((int)flagInfo.Flags & 0x10) == 0 ||
                IL2CPP.il2cpp_type_get_name_((IntPtr)readyInfo.ReturnType) != "System.Void" ||
                IL2CPP.il2cpp_type_get_name_((IntPtr)flagInfo.ReturnType) != "System.Boolean" ||
                IL2CPP.il2cpp_type_get_name_(IL2CPP.il2cpp_method_get_param(ready, 0)) != "GameRoomReadyToRunEvent" ||
                IL2CPP.il2cpp_type_get_name_(IL2CPP.il2cpp_method_get_param(flag, 0)) != "eGameProgressionFlag" ||
                readyInfo.MethodPointer == IntPtr.Zero || flagInfo.MethodPointer == IntPtr.Zero || readyInfo.MethodPointer == flagInfo.MethodPointer)
                throw new InvalidOperationException("Unexpected Garage holder native signature");
            Type enumType = ReflectionUtil.GameAssembly!.GetType("eGameProgressionFlag", true)!;
            if (Enum.GetUnderlyingType(enumType) != typeof(int)) throw new InvalidOperationException("Unexpected flag ABI");
            foreach (var cartridge in GarageCartridgeNativePolicy.RandomizedCartridges)
                FlagNames.Add(Convert.ToInt32(Enum.Parse(enumType, cartridge.NativeCollectedFlag)), cartridge.NativeCollectedFlag);
            _flagDetour = INativeDetour.CreateAndApply(flagInfo.MethodPointer, FlagReplacement, out _flagOriginal);
            _readyDetour = INativeDetour.CreateAndApply(readyInfo.MethodPointer, ReadyReplacement, out _readyOriginal);
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] Garage native holder initialization ownership ready; vanilla holder placement retained.");
            return 2;
        } catch (Exception ex) {
            Plugin.LoggerInstance?.LogError($"[SCRC-AP] Garage native holder ownership unavailable: {ex.GetBaseException().Message}");
            return 0;
        }
    }
    private static void Ready(IntPtr instance, byte evt, IntPtr methodInfo)
    {
        _holderScope++;
        try { _readyOriginal!(instance, evt, methodInfo); }
        finally { _holderScope--; }
    }
    private static bool Flag(int flag, IntPtr methodInfo)
    {
        try {
            if (_holderScope > 0 && DeveloperHarness.CurrentRoomId == "GameRoom_27" && FlagNames.TryGetValue(flag, out string? name)) {
                bool? owned = GarageHolderOwnershipPolicy.Resolve(name, GarageCartridgeAccess.Enabled, true, GarageCartridgeAccess.HasCartridge);
                if (owned.HasValue) return owned.Value;
            }
        } catch (Exception ex) {
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] Garage holder ownership read failed: {ex.GetBaseException().Message}");
        }
        return _flagOriginal!(flag, methodInfo);
    }
}
