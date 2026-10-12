namespace RhythmCastleAP;

internal interface IRootsPortalRenderer
{
    bool Alive { get; }
    bool Enabled { get; set; }
}

// Owns only a renderer override, never the doorway object, collider or interaction.
internal sealed class RootsPortalPolicy
{
    internal const string Path = "Root/GameRoom_Hub2_Logic/Objects/PhoneBox/CleanRoomsDoorway-tofb/Doorway";
    private IRootsPortalRenderer? _renderer;
    private bool _changed;
    private int _attempts;
    private TimeSpan _remaining;

    internal void Tick(TimeSpan elapsed, bool active, Func<bool?> sourceCollected, Func<IRootsPortalRenderer?> bind)
    {
        if (!active) { Reset(); return; }
        _remaining -= elapsed;
        if (_remaining > TimeSpan.Zero) return;
        _remaining = TimeSpan.FromSeconds(1);
        if (sourceCollected() != false) { Restore(); return; }
        if (_renderer?.Alive != true)
        {
            _renderer = null;
            _changed = false;
            if (_attempts >= 30) return;
            _attempts++;
            _renderer = bind();
        }
        if (_renderer?.Alive == true && _renderer.Enabled)
        {
            _renderer.Enabled = false;
            _changed = true;
        }
    }

    private void Restore()
    {
        if (_changed && _renderer?.Alive == true) _renderer.Enabled = true;
        _changed = false;
    }

    internal void Reset()
    {
        Restore();
        _renderer = null;
        _attempts = 0;
        _remaining = TimeSpan.Zero;
    }
}
