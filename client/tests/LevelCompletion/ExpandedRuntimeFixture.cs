namespace RhythmCastleAP;
internal static class ExpandedChecks {
 internal static ExpandedChecksState State = new();
 internal static void Configure(long generation,string identity,bool enabled,IEnumerable<long> checks,bool includeStarChecks=false) =>
  State.Configure(generation,identity,enabled,checks,_=>new ExpandedJournalRecord(null,Array.Empty<long>()),includeStarChecks);
 internal static void Reset()=>State.Reset();
}


// Only the Unity-facing singleton/hook boundary is substituted. The star
// contract, receipt history, state transitions and goal qualification are real.
internal static class ApStars { internal static readonly ApStarState State = new(); }
internal static class ApStarNativeHooks { internal static bool Ready = true; }

internal static class CellTowerPathPatches { internal static bool Ready = true; }

internal static class LobbyDoorPatches { internal static bool Ready = true; }

internal static class BunkerRuntime { internal static bool Ready=true; internal static readonly BunkerState State=new(); internal static readonly BunkerJournal Journal=new(Path.Combine(Path.GetTempPath(),"bunker-connection-tests-"+Guid.NewGuid())); internal static bool Handles(string name)=>BunkerCatalog.Items.Any(i=>i.Name==name); }
