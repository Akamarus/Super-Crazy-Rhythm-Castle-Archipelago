using System.Runtime.InteropServices;
namespace RhythmCastleAP;

// Windows x64 IL2CPP instance method: this pointer, float in XMM1, MethodInfo*.
// No structs, nullable values, strings, arrays, or generated object wrappers.
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void AnimalUpdateNativeCall(IntPtr instance, float elapsed, IntPtr methodInfo);
