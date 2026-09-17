namespace RhythmCastleAP;

internal static class NativeDoorInvocation
{
    internal static void Run<T>(Func<T?> before, Action original, Action<T?> after, Action<Exception> report) where T : class
    {
        T? snapshot = null;
        try { snapshot = before(); }
        catch (Exception ex) { Report(ex); }
        original();
        try { after(snapshot); }
        catch (Exception ex) { Report(ex); }
        void Report(Exception ex) { try { report(ex); } catch { /* Never escape a native callback through diagnostics. */ } }
    }
}
