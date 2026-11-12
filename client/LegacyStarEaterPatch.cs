using System.Runtime.InteropServices;

namespace RhythmCastleAP;

internal sealed record LegacyStarEaterPatch(IntPtr Address, byte[] Original, byte[] Applied)
{
    internal bool TryRestore(Func<IntPtr, byte[], bool> write)
    {
        var current = new byte[Applied.Length];
        Marshal.Copy(Address, current, 0, current.Length);
        // A later change at the same address belongs to its writer.
        if (!current.AsSpan().SequenceEqual(Applied) || current.AsSpan().SequenceEqual(Original)) return true;
        if (!write(Address, Original)) return false;
        Marshal.Copy(Address, current, 0, current.Length);
        return current.AsSpan().SequenceEqual(Original);
    }
}
