namespace RhythmCastleAP;

internal static class NativeConditionInvocation
{
    internal static bool Run(Func<bool?> evaluate, Func<bool> original, Action<Exception> report)
    {
        try
        {
            bool? result = evaluate();
            if (result.HasValue) return result.Value;
        }
        catch (Exception ex) { try { report(ex); } catch { } }
        // Direct trampoline only: never redispatch a generated virtual wrapper.
        return original();
    }
}

internal static class NativeSequenceInvocation
{
    internal static void Run(Func<bool> admit, Action original, Action<Exception> report)
    {
        try { if (!admit()) return; }
        catch (Exception ex) { try { report(ex); } catch { } return; }
        original();
    }
}
