using RhythmCastleAP;
using Newtonsoft.Json.Linq;
int checks = 0;
void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
Dictionary<string, object> Contract() => new() {
    ["implementation_version"] = "full-level-mapping-0.24-character-quest-items-0.25-quest-checks-0.26",
    ["schema_version"] = 17, ["quest_checks_schema"] = 1,
    ["quest_items"] = QuestChecksPolicy.Items, ["quest_locations"] = QuestChecksPolicy.Locations,
};
var batchContract = Contract();
batchContract["implementation_version"] += "-check-expansion-0.27";
batchContract["schema_version"] = 18;
Check(QuestChecksPolicy.Validate(batchContract) == QuestChecksMode.Enabled, "schema18 retains existing quest behavior");
batchContract["schema_version"] = 17;
Check(QuestChecksPolicy.Validate(batchContract) == QuestChecksMode.Invalid, "schema18 suffix rejects old schema number");
var starContract = new Dictionary<string,object>(batchContract);
starContract["implementation_version"] += "-ap-stars-0.28";
starContract["schema_version"] = 19;
Check(QuestChecksPolicy.Validate(starContract) == QuestChecksMode.Enabled, "schema19 preserves existing subsystem contract");
starContract["schema_version"] = 18;
Check(QuestChecksPolicy.Validate(starContract) == QuestChecksMode.Invalid, "AP Stars suffix requires schema19");
starContract["schema_version"] = 19;
starContract["implementation_version"] = batchContract["implementation_version"];
Check(QuestChecksPolicy.Validate(starContract) == QuestChecksMode.Invalid, "schema19 requires AP Stars suffix");
starContract["implementation_version"] += "-ap-stars-0.28-extra";
Check(QuestChecksPolicy.Validate(starContract) == QuestChecksMode.Invalid, "unknown AP Stars suffix extension rejected");

Check(QuestChecksPolicy.Validate(Contract()) == QuestChecksMode.Enabled, "exact new contract");
Check(QuestChecksPolicy.IsConditionRoom("GameRoom_Hub6"), "native condition dispatch admits the Music Lab");
Check(!QuestChecksPolicy.IsConditionRoom("GameRoom_Hub2"), "unrelated room bypasses quest condition dispatch");
foreach (string file in new[] { "QuestNativeVirtualHooks.cs", "QuestCheckHooks.cs" })
    Check(File.ReadAllText(Path.Combine("client", file)).Contains("!QuestChecksPolicy.IsConditionRoom(DeveloperHarness.CurrentRoomId)"),
        "outer and inner production dispatch share the tested room predicate: " + file);
const string batteryChestCondition = "Root/GameRoom_Hub6_Logic/Objects/RewardChests/CatBatteryChest/Interaction/Condition/AlreadyHaveCatCharacter";
Check(QuestChecksPolicy.ConditionResult(batteryChestCondition, new HashSet<long>{QuestChecksPolicy.BatteryHandIn}, () => false, () => false) == false,
    "completed battery hand-in and AP Meoo cannot block the unopened randomized chest");
Check(QuestChecksPolicy.ConditionResult(batteryChestCondition.Replace("AlreadyHaveCatCharacter", "AlreadyHaveCatBattery"), new HashSet<long>(), () => false, () => false) == null,
    "chest collection flag condition remains native");
Check(QuestChecksPolicy.ConditionResult(batteryChestCondition.Replace("CatBatteryChest", "OtherChest"), new HashSet<long>(), () => false, () => false) == null,
    "unrelated chest character conditions remain native");

Check(QuestChecksPolicy.Validate(null) == QuestChecksMode.Legacy, "non AP remains native");
Check(QuestChecksPolicy.Validate(new() { ["schema_version"] = 16, ["implementation_version"] = "character-quest-items-0.25" }) == QuestChecksMode.Legacy, "existing seed stays native for new quests");
foreach (string key in Contract().Keys) {
    var invalid = Contract(); invalid.Remove(key);
    Check(QuestChecksPolicy.Validate(invalid) == QuestChecksMode.Invalid, "missing field " + key);
}
var wrong = Contract(); wrong["schema_version"] = "17";
Check(QuestChecksPolicy.Validate(wrong) == QuestChecksMode.Invalid, "string schema rejected");
wrong = Contract(); wrong["quest_items"] = new Dictionary<string,long>(QuestChecksPolicy.Items) { ["Plunger"] = 1 };
Check(QuestChecksPolicy.Validate(wrong) == QuestChecksMode.Invalid, "wrong permanent ID rejected");
Check(QuestChecksPolicy.SourceForEvent("GameRoom_Hub2", "ROOTS_HUB_STAR_EATER_FED", false, true) == QuestChecksPolicy.RootsStarEater, "actual feed source");
Check(QuestChecksPolicy.SourceForEvent("GameRoom_Hub2", "ROOTS_HUB_STAR_EATER_BLOCKADE_REMOVED", false, true) == null, "bypassed blockade is not feeding");
Check(QuestChecksPolicy.SourceForEvent("GameRoom_Hub1B", "OUTSIDE_HUB_PLUNGER_COLLECTED", false, true) == QuestChecksPolicy.PlungerPickup, "plunger collected marker");
Check(QuestChecksPolicy.SourceForEvent("GameRoom_Hub1A", "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM", true, false) == QuestChecksPolicy.BatteryHandIn, "battery handin");
Check(QuestChecksPolicy.SourceForEvent("GameRoom_Hub6", "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM", true, false) == null, "wrong room isn't handin");
Check(QuestChecksPolicy.SourceForEvent("GameRoom_27", "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM", false, false) == null, "already absent isn't handin");
var state = new QuestChecksState();
state.Configure(1, "seedA/team0/slot1", true);
state.BindSlot(4, (_, _) => { });
state.Publish(1, new[] { QuestChecksPolicy.Meoo, QuestChecksPolicy.Maniac }, Array.Empty<long>());
Check(state.Snapshot.Ready && state.Snapshot.Checked.Count == 0, "early character receipts don't complete handins");
Check(state.RecordSource(QuestChecksPolicy.BatteryHandIn), "first source");
Check(!state.RecordSource(QuestChecksPolicy.BatteryHandIn), "source deduplicates");
state.Suspend(2);
Check(!state.Snapshot.Ready, "relogin suspends grants");
state.Configure(2, "seedA/team0/slot1", true);
state.Publish(2, Array.Empty<long>(), Array.Empty<long>());
Check(state.Snapshot.Checked.Contains(QuestChecksPolicy.BatteryHandIn), "same identity retains pending handin even without character");
state.Configure(3, "seedB/team0/slot1", true);
Check(!state.Snapshot.Checked.Any(), "new seed clears prior handins");
state.Publish(2, new[] { QuestChecksPolicy.Plunger }, new[] { QuestChecksPolicy.MemoryHandIn });
Check(!state.Snapshot.Ready, "stale callback rejected");
state.Publish(3, Array.Empty<long>(), new[] { QuestChecksPolicy.MemoryHandIn });
Check(state.Snapshot.Checked.Contains(QuestChecksPolicy.MemoryHandIn), "server completion restores consumed marker after restart");
state.Reset(); Check(!state.Snapshot.Enabled, "reset");
string meooPath = "Root/GameRoom_Hub1_Logic/Hub1A_Logic_TopLeft/UnlockMeooCharacter/Conditions/";
var none = new HashSet<long>();
Check(QuestChecksPolicy.ConditionResult(meooPath+"MeooIsUnlockedCondition", none, () => true, () => false) == false, "early Meoo ownership never blocks handin");
Check(QuestChecksPolicy.ConditionResult(meooPath+"HaveMeooBatteryCondition", none, () => false, () => false) == false, "chest alone cannot admit handin");
Check(QuestChecksPolicy.ConditionResult(meooPath+"HaveMeooBatteryCondition", none, () => true, () => false) == true, "early battery needs no native chest");
Check(QuestChecksPolicy.ConditionResult("CharacterSelection/MeooIsUnlockedCondition", none, () => true, () => false) == null, "character selection unchanged");
Check(QuestChecksPolicy.ConditionResult("Root/GameRoom_Hub1B_Logic/Objects/CollectableBagObjects/Plunger/Condition", none, () => false, () => false) == true, "early held/used plunger cannot hide source");
string temp = Path.Combine(Path.GetTempPath(), "scrc-quest-tests-" + Guid.NewGuid());
var journal = new QuestChecksJournal(temp);
state.Configure(5, "offline-seed", true);
state.BindSlot(4, journal.Save);
Check(state.RecordSource(QuestChecksPolicy.MemoryHandIn, journal.Save), "offline handin journaled");
var restarted = new QuestChecksState();
restarted.Configure(1, "offline-seed", true, journal.Load("offline-seed"));
Check(restarted.Snapshot.Checked.Contains(QuestChecksPolicy.MemoryHandIn), "offline handin restored before item grants");
Check(journal.Load("different-seed").Completed.Length == 0, "journal isolated by seed identity");
Check(!state.BindSlot(3, journal.Save), "another selected save cannot inherit sources");
var failing = new QuestChecksState(); failing.Configure(1, "failure", true); failing.BindSlot(4, (_, _) => { });
try { failing.PrepareHandIn(QuestChecksPolicy.BatteryHandIn, (_, _) => throw new IOException("disk unavailable")); }
catch (IOException) { }
Check(!failing.Snapshot.Pending.Any() && !failing.Snapshot.Checked.Any(), "failed prepare cannot admit consumption or complete source");
failing.PrepareHandIn(QuestChecksPolicy.BatteryHandIn, journal.Save);
var recovered = new QuestChecksState(); recovered.Configure(1, "failure", true, journal.Load("failure"));
Check(recovered.Snapshot.Pending.Contains(QuestChecksPolicy.BatteryHandIn), "prepared consumption survives restart before completion write");
try { recovered.RecordSource(QuestChecksPolicy.BatteryHandIn, (_, _) => throw new IOException("disk unavailable")); }
catch (IOException) { }
Check(recovered.Snapshot.Pending.Contains(QuestChecksPolicy.BatteryHandIn), "failed completion retains durable recoverable pending source");
Check(recovered.RecordSource(QuestChecksPolicy.BatteryHandIn, journal.Save), "completion retries after I/O recovers");
foreach (string file in Directory.GetFiles(temp)) File.Delete(file);
Directory.Delete(temp);
Console.WriteLine($"Quest checks: {checks} assertions passed.");

QuestChecks.State.Reset();
var session = new QuestChecksSession(10);
session.Configure("session-seed", true, Array.Empty<long>());
session.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index = 0, Items = Array.Empty<Archipelago.MultiClient.Net.Models.NetworkItem>() });
Check(QuestChecks.State.Snapshot.Ready, "complete index-zero receipt history accepted");
session.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index = 8, Items = Array.Empty<Archipelago.MultiClient.Net.Models.NetworkItem>() });
Check(!QuestChecks.State.Snapshot.Ready, "gapped history suspends published readiness");
session.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index = 0, Items = Array.Empty<Archipelago.MultiClient.Net.Models.NetworkItem>() });
Check(QuestChecks.State.Snapshot.Ready, "full replay recovers readiness");
Console.WriteLine("Quest session malformed-history recovery passed.");

foreach (bool nativeValue in new[] { false, true })
{
    int calls = 0;
    bool Original() { calls++; return nativeValue; }
    Check(NativeConditionInvocation.Run(() => null, Original, _ => {}) == nativeValue && calls == 1,
        "unhandled condition invokes native original exactly once");
    foreach (bool overridden in new[] { false, true })
    {
        calls = 0;
        Check(NativeConditionInvocation.Run(() => overridden, Original, _ => {}) == overridden && calls == 0,
            "handled condition never enters original wrapper");
    }
    calls = 0;
    Check(NativeConditionInvocation.Run(() => throw new Exception("observation"), Original,
        _ => throw new Exception("logger")) == nativeValue && calls == 1,
        "observation and logger failures preserve native result without re-entry");
}
Console.WriteLine("Native condition dispatch: 8 assertions passed.");

foreach (int mode in new[] { 0, 1, 2 })
{
    int calls = 0;
    NativeSequenceInvocation.Run(() => mode == 2 ? throw new Exception("unreadable path") : mode == 1,
        () => calls++, _ => throw new Exception("logger"));
    Check(calls == (mode == 1 ? 1 : 0), "denied or unidentified native sequence cannot execute");
}
Console.WriteLine("Native sequence admission: 3 assertions passed.");

// Native compiler folds the True and Set wrappers onto the same function.
var nativeAddresses = new Dictionary<string, long> {
    ["IsGameProgressionFlagTrueCondition"] = 100,
    ["IsGameProgressionFlagSetCondition"] = 100,
    ["IsPlayableCharacterUnlockedCondition"] = 200,
    ["CompoundCondition"] = 300,
};
var patchedAddresses = new HashSet<long> { nativeAddresses[QuestConditionHookPlan.SharedOwner] };
foreach (string name in QuestConditionHookPlan.DedicatedTypes)
    Check(patchedAddresses.Add(nativeAddresses[name]), "each native condition address has exactly one hook owner");
Check(QuestConditionHookPlan.IsVerifiedAlias(100, 100), "shared nonzero native code pointer accepted");
Check(!QuestConditionHookPlan.IsVerifiedAlias(100, 101), "different method pointers cannot share ownership");
Check(!QuestConditionHookPlan.IsVerifiedAlias(0, 0), "missing native methods cannot enable quest routing");
Check(QuestConditionHookPlan.IsSharedEntry("IsGameProgressionFlagTrueCondition", "CheckIfMet"), "existing area hook dispatches quest condition");
Check(!QuestConditionHookPlan.IsSharedEntry("GeneralCondition", "CheckIfMet"), "base condition does not dispatch quest twice");
Check(!QuestConditionHookPlan.IsSharedEntry("IsGameProgressionFlagTrueCondition", "ReliesOn"), "dependency query never becomes a quest condition");
Console.WriteLine("Shared native condition ownership: 8 assertions passed.");
