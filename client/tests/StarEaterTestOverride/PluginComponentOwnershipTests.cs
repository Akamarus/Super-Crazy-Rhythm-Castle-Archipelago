using UnityEngine;

namespace RhythmCastleAP
{
    internal static class PluginComponentOwnershipTests
    {
        internal static void Run()
        {
            var owner = new OwnedPluginComponents();
            var other = new MonoBehaviour();
            var retry = new MonoBehaviour { Busy = true };
            var keeper = new BunkerStarRequirementKeeper();
            owner.Add(retry);
            owner.Add(keeper);
            var errors = new List<Exception>();
            if (owner.Shutdown(errors.Add) || errors.Count != 1 || retry.enabled ||
                keeper.enabled || !keeper.Retired || keeper.Destroyed != 1 || !other.enabled || other.Destroyed != 0)
                throw new Exception("unload must disable owned components and preserve other users of their host");
            retry.Busy = false;
            if (!owner.Shutdown(errors.Add) || !owner.Shutdown(errors.Add) || retry.Destroyed != 1 || keeper.Destroyed != 1)
                throw new Exception("unload must retry only the component still awaiting destruction");
            Console.WriteLine("Plugin component ownership and unload retry passed.");
        }
    }

    internal sealed class BunkerStarRequirementKeeper : MonoBehaviour
    {
        internal bool Retired;
        internal void Retire() => Retired = true;
    }
}

namespace UnityEngine
{
    internal class Object
    {
        internal int Destroyed;
        internal bool Busy;
        internal static void Destroy(Object value)
        {
            if (value.Busy) throw new InvalidOperationException("component busy");
            value.Destroyed++;
        }
    }
    internal class MonoBehaviour : Object { internal bool enabled = true; }
}
