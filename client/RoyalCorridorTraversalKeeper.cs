using UnityEngine;

namespace RhythmCastleAP;

internal sealed class RoyalCorridorTraversalKeeper : MonoBehaviour
{
    private GameObject? _blocker;
    private GameObject? _bridge;
    private bool _blockerBaseline;
    private bool _bridgeBaseline;
    private bool _bound;
    private float _nextPoll;

    public RoyalCorridorTraversalKeeper(IntPtr pointer) : base(pointer) { }

    private void LateUpdate()
    {
        if (!RoyalCorridorTraversalPolicy.ShouldOpen(
                AreaAccessPrototype.Enabled, IntroHubSkip.Compatible,
                AreaAccessPrototype.HasArea("Royal Corridor"), DeveloperHarness.CurrentRoomId))
        {
            Restore();
            _nextPoll = Time.unscaledTime + 0.25f;
            return;
        }
        if (Time.unscaledTime < _nextPoll) return;
        _nextPoll = Time.unscaledTime + 0.25f;
        try
        {
            if (!_bound || _blocker == null || _bridge == null)
            {
                // Find the active parent, then exact children including the inactive bridge.
                // Never disable the Star Eater root, interaction, or mask collision.
                var root = GameObject.Find(RoyalCorridorTraversalPolicy.RootPath);
                if (root == null) return;
                var blocker = root.transform.Find(RoyalCorridorTraversalPolicy.BlockerChild);
                var bridge = root.transform.Find(RoyalCorridorTraversalPolicy.BridgeChild);
                if (blocker == null || bridge == null) return;
                _blocker = blocker.gameObject;
                _bridge = bridge.gameObject;
                _blockerBaseline = _blocker.activeSelf;
                _bridgeBaseline = _bridge.activeSelf;
                _bound = true;
                Plugin.LoggerInstance?.LogInfo(
                    "[SCRC-AP] ROYAL CORRIDOR TRAVERSAL READY: exact BlockingCollision disabled and BridgeAcrossGap enabled for Royal Corridor Access; feeding requirement and quest flags unchanged.");
            }
            // The native skip route uses this bridge; removing only the wall leaves a gap.
            if (!_bridge.activeSelf) _bridge.SetActive(true);
            if (_blocker.activeSelf) _blocker.SetActive(false);
        }
        catch
        {
            Restore();
        }
    }

    private void OnDestroy() => Restore();

    private void Restore()
    {
        if (!_bound) return;
        try { if (_blocker != null && _blocker.activeSelf != _blockerBaseline) _blocker.SetActive(_blockerBaseline); }
        catch { }
        try { if (_bridge != null && _bridge.activeSelf != _bridgeBaseline) _bridge.SetActive(_bridgeBaseline); }
        catch { }
        _blocker = null;
        _bridge = null;
        _bound = false;
    }
}
