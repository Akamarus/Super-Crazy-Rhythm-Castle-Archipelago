namespace RhythmCastleAP;
internal static class ExpandedChecks {
 internal static readonly ExpandedChecksState State = new();
 internal static readonly string JournalDirectory = Path.Combine(Path.GetTempPath(), "expanded-connection-tests-" + Guid.NewGuid());
 internal static readonly ExpandedChecksJournal Journal = new(JournalDirectory);
 internal static Action Prepare(long generation,string identity,bool enabled,IEnumerable<long> checks,bool includeStarChecks=false) =>
  State.Prepare(generation,identity,enabled,checks,Journal.Load,includeStarChecks);
 internal static void Reset()=>State.Reset();
}


// Only the Unity-facing singleton/hook boundary is substituted. The star
// contract, receipt history, state transitions and goal qualification are real.
internal static class ApStars { internal static readonly ApStarState State = new(); }
internal static class ApStarNativeHooks { internal static bool Ready = true; }

internal static class CellTowerPathPatches { internal static bool Ready = true; }

internal static class LobbyDoorPatches { internal static bool Ready = true; }

internal static class BunkerRuntime
{
    internal static bool Ready = true;
    internal static readonly BunkerState State = new();
    internal static readonly string JournalDirectory = Path.Combine(Path.GetTempPath(), "bunker-connection-tests-" + Guid.NewGuid());
    internal static readonly BunkerJournal Journal = new(JournalDirectory);
    internal static bool Handles(string name) => BunkerCatalog.Items.Any(item => item.Name == name);
}
