using RhythmCastleAP;
void Check(bool value, string name) { if (!value) throw new Exception(name); }
Check(StarEaterTestOverridePolicy.Create(false, 25, 25) == null, "local overrides require explicit consent");
Check(StarEaterTestOverridePolicy.Create(true, 40, 66) == null, "vanilla settings preserve all native targets");
var bunker = StarEaterTestOverridePolicy.Create(true, 40, 25)!;
Check(bunker.Count == 1 && bunker["Secret Bunker"] == 25, "only the selected scene receives an override");
var both = StarEaterTestOverridePolicy.Create(true, -4, 12)!;
Check(both.Count == 2 && both["Royal Corridor"] == 0 && both["Secret Bunker"] == 12, "bounds and mapping");
PluginComponentOwnershipTests.Run();
