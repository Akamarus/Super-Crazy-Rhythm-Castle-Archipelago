using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RhythmCastleAP;

internal static class NativeSignatureChecks
{
    private static nint _instance, _method;
    private static float _elapsed;
    private static int _calls;
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void NativeOriginal(nint instance, float elapsed, nint method)
    { _instance = instance; _method = method; _elapsed = elapsed; _calls++; }

    internal static unsafe void Run()
    {
        // Exercise both directions of the exact primitive delegate signature.
        // This checks CLR/native ABI transfer, NOT IL2CPP method discovery or game semantics.
        delegate* unmanaged[Cdecl]<nint, float, nint, void> native = &NativeOriginal;
        var original = Marshal.GetDelegateForFunctionPointer<AnimalUpdateNativeCall>((nint)native);
        AnimalUpdateNativeCall replacement = (instance, elapsed, method) => original(instance, elapsed, method);
        var entry = (delegate* unmanaged[Cdecl]<nint, float, nint, void>)Marshal.GetFunctionPointerForDelegate(replacement);
        foreach (float delta in new[] { .00625f, .02f, .0333333f })
        {
            int before = _calls;
            entry(unchecked((nint)0x123456789), delta, unchecked((nint)0x23456789A));
            if (_calls != before + 1 || _instance != unchecked((nint)0x123456789) || _method != unchecked((nint)0x23456789A) || _elapsed != delta)
                throw new Exception("Primitive native signature did not preserve arguments/call count");
        }
        GC.KeepAlive(replacement); GC.KeepAlive(original);
        Console.WriteLine("3 unmanaged primitive signature round-trips passed (not native gameplay validation)");
    }
}
