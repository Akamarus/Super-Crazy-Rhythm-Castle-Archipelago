using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
namespace RhythmCastleAP;

internal static class GarageNativeSavedMedals
{
    private static readonly Dictionary<string, (IntPtr Method, Type Song, Type Scalar)> Bindings = new();
    private static object? ReadBoxedScalar(IntPtr pointer, Type scalar)
    {
        if (pointer == IntPtr.Zero) return null;
        IntPtr klass = IL2CPP.il2cpp_object_get_class(pointer);
        string? name = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(klass));
        string? ns = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_namespace(klass));
        string fullName = string.IsNullOrEmpty(ns) ? name ?? "" : ns + "." + name;
        // IL2CPP boxes Nullable<T> as T (or null), but generated getters wrap that
        // pointer as Nullable<T>. Calling its Value getter reads the wrong layout.
        if (fullName != scalar.FullName) throw new InvalidOperationException("Native nullable scalar mismatch: " + fullName + " expected " + scalar.FullName);
        int raw = Marshal.ReadInt32(IL2CPP.il2cpp_object_unbox(pointer));
        return scalar.IsEnum ? Enum.ToObject(scalar, raw) : raw;
    }
    internal static unsafe object? ReadSaved(string getter, string nativeSong)
    {
        (IntPtr Method, Type Song, Type Scalar) binding;
        lock (Bindings) {
            if (!Bindings.TryGetValue(getter, out binding)) {
                Type owner = ReflectionUtil.GameAssembly!.GetType("CurrentPlayerSaveEnquiries", true)!;
                MethodInfo method = owner.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .Single(m => m.Name == getter && m.GetParameters().Length == 1);
                Type scalar = method.ReturnType.GetGenericArguments().Single();
                Type songType = method.GetParameters()[0].ParameterType;
                if (songType.Name != "ePlayableSong" || !songType.IsEnum || Enum.GetUnderlyingType(songType) != typeof(int) ||
                    (scalar != typeof(int) && (!scalar.IsEnum || Enum.GetUnderlyingType(scalar) != typeof(int))))
                    throw new InvalidOperationException("Unexpected native Garage enquiry signature");
                FieldInfo field = owner.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .Single(f => f.Name.StartsWith("NativeMethodInfoPtr_" + getter + "_", StringComparison.Ordinal));
                IntPtr metadata = (IntPtr)field.GetValue(null)!;
                if (metadata == IntPtr.Zero) throw new InvalidOperationException("Missing native Garage enquiry metadata");
                binding = (metadata, songType, scalar);
                Bindings.Add(getter, binding);
            }
        }
        int song = Convert.ToInt32(Enum.Parse(binding.Song, nativeSong));
        void** args = stackalloc void*[1];
        args[0] = &song;
        IntPtr exception = IntPtr.Zero;
        IntPtr boxed = IL2CPP.il2cpp_runtime_invoke(binding.Method, IntPtr.Zero, args, ref exception);
        Il2CppException.RaiseExceptionIfNecessary(exception);
        // Bypass generated Nullable<T> construction, including the empty/null case.
        return ReadBoxedScalar(boxed, binding.Scalar);
    }
    internal static (int? Medal, int? Pro) Read(string nativeSong)
    {
        object? medal = ReadSaved("GetBestMedalEarnedForGarageStickerSong", nativeSong);
        object? pro = ReadSaved("GetBestMedalEarnedOnProForGarageStickerSong", nativeSong);
        return (medal == null ? null : Convert.ToInt32(medal), pro == null ? null : Convert.ToInt32(pro));
    }
}
