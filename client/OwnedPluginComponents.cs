using UnityEngine;

namespace RhythmCastleAP;

internal sealed class OwnedPluginComponents
{
    private readonly List<MonoBehaviour> _components = new();
    internal void Add(MonoBehaviour component) => _components.Add(component);

    internal bool Shutdown(Action<Exception> report)
    {
        for (int i = _components.Count - 1; i >= 0; i--)
        {
            var component = _components[i];
            try
            {
                if (component != null)
                {
                    if (component is BunkerStarRequirementKeeper keeper) keeper.Retire();
                    component.enabled = false;
                    // BepInEx's host object may contain components from other plugins.
                    UnityEngine.Object.Destroy(component);
                }
            }
            catch (Exception ex) { report(ex); continue; }
            _components.RemoveAt(i);
        }
        return _components.Count == 0;
    }
}
