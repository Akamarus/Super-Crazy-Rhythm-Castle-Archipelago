namespace RhythmCastleAP;

internal static class NativeHookOwnershipTests
{
    private sealed class Handle(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }

    internal static void Run()
    {
        var hooks = new OwnedNativeHooks();
        var calls = new List<string>();
        bool reject = true;
        Action replacement = () => { };
        hooks.Add(new Handle(() => calls.Add("first")), replacement);
        hooks.Add(new Handle(() => {
            calls.Add("second");
            if (hooks.Active) throw new Exception("callbacks remained active during detach");
            if (reject) throw new IOException("busy");
        }), replacement);
        hooks.Add(new Handle(() => calls.Add("third")), replacement);
        var errors = new List<string>();
        if (!hooks.Active || hooks.Shutdown(ex => errors.Add(ex.Message)) || hooks.Active ||
            hooks.Count != 1 || !errors.SequenceEqual(new[] { "busy" }) ||
            !calls.SequenceEqual(new[] { "third", "second", "first" }))
            throw new Exception("failed detach must retain only the failed hook and stop callbacks");
        reject = false;
        if (!hooks.Shutdown(ex => errors.Add(ex.Message)) || !hooks.Shutdown(ex => errors.Add(ex.Message)) ||
            hooks.Count != 0 || calls.Count != 4 || calls[^1] != "second")
            throw new Exception("repeated detach must retry only remaining handles");
        hooks.Add(new Handle(() => calls.Add("reloaded")), replacement);
        if (!hooks.Active || !hooks.Shutdown(ex => errors.Add(ex.Message)) || calls[^1] != "reloaded")
            throw new Exception("cleanly unloaded hooks must support a fresh install");
        Console.WriteLine("Native hook cleanup, partial failure, retry and reload passed.");
    }
}
