namespace RhythmCastleAP;

// Unity installs and tears down these hooks on the main thread. The delegate
// keeps its callback target and original trampoline alive until disposal succeeds.
internal sealed class OwnedNativeHooks
{
    private readonly List<(IDisposable Detour, Delegate Replacement)> _installed = new();
    private volatile bool _active;
    internal bool Active => _active;
    internal int Count => _installed.Count;

    internal void Add(IDisposable detour, Delegate replacement)
    {
        _installed.Add((detour, replacement));
        _active = true;
    }

    internal bool Shutdown(Action<Exception> report)
    {
        _active = false;
        for (int i = _installed.Count - 1; i >= 0; i--)
        {
            try { _installed[i].Detour.Dispose(); }
            catch (Exception ex) { report(ex); continue; }
            _installed.RemoveAt(i);
        }
        return _installed.Count == 0;
    }
}
