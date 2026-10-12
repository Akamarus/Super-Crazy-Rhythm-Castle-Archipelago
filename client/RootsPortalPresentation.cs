using UnityEngine;

namespace RhythmCastleAP;

internal static class RootsPortalPresentation
{
    private static readonly RootsPortalPolicy Policy = new();

    internal static void Tick(TimeSpan elapsed)
    {
        try
        {
            bool active = AreaAccessPrototype.Enabled && AreaAccessPrototype.HasArea("Roots") &&
                WeedKillerRandomization.Enabled && DeveloperHarness.CurrentRoomId == "GameRoom_Hub2";
            Policy.Tick(elapsed, active,
                () => RootsBucketRandomization.TryReadProgressionFlag(WeedKillerRandomization.NativeCollectedFlag, out bool collected)
                    ? collected : null,
                () =>
                {
                    GameObject? doorway = GameObject.Find(RootsPortalPolicy.Path);
                    MeshRenderer? renderer = doorway == null ? null : doorway.GetComponent<MeshRenderer>();
                    return renderer == null ? null : new PortalRenderer(renderer);
                });
        }
        catch { Reset(); }
    }

    internal static void Reset()
    {
        try { Policy.Reset(); } catch { }
    }

    private sealed class PortalRenderer : IRootsPortalRenderer
    {
        private readonly MeshRenderer _renderer;
        internal PortalRenderer(MeshRenderer renderer) => _renderer = renderer;
        public bool Alive => _renderer != null;
        public bool Enabled { get => _renderer.enabled; set => _renderer.enabled = value; }
    }
}
