using RhythmCastleAP;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json.Linq;

static void Check(bool ok, string name) { if (!ok) throw new Exception(name); }
static Dictionary<string, object> Contract() => new() {
 ["implementation_version"] = "area-routing-full-level-mapping-0.24-character-quest-items-0.25", ["schema_version"] = 16,
 ["character_quest_item_schema"] = 1, ["randomize_character_quest_items"] = true,
 ["character_quest_items"] = new Dictionary<string,string> { ["Old Game Data"] = "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM", ["Car Battery"] = "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM" },
 ["character_quest_item_locations"] = new Dictionary<string,string> { ["Old Game Data"] = "Music Lab - 5 Point Chest", ["Car Battery"] = "Music Lab - 20 Point Chest" }
};
var batchContract = Contract();
batchContract["implementation_version"] += "-quest-checks-0.26-check-expansion-0.27";
batchContract["schema_version"] = 18;
Check(CharacterQuestItemPolicy.Validate(batchContract) == CharacterQuestItemMode.Enabled, "schema18 retains existing quest behavior");
batchContract["schema_version"] = 17;
Check(CharacterQuestItemPolicy.Validate(batchContract) == CharacterQuestItemMode.Invalid, "schema18 suffix rejects old schema number");
var starContract = new Dictionary<string,object>(batchContract);
starContract["implementation_version"] += "-ap-stars-0.28";
starContract["schema_version"] = 19;
Check(CharacterQuestItemPolicy.Validate(starContract) == CharacterQuestItemMode.Enabled, "schema19 preserves existing subsystem contract");
starContract["schema_version"] = 18;
Check(CharacterQuestItemPolicy.Validate(starContract) == CharacterQuestItemMode.Invalid, "AP Stars suffix requires schema19");
starContract["schema_version"] = 19;
starContract["implementation_version"] = batchContract["implementation_version"];
Check(CharacterQuestItemPolicy.Validate(starContract) == CharacterQuestItemMode.Invalid, "schema19 requires AP Stars suffix");
starContract["implementation_version"] += "-ap-stars-0.28-extra";
Check(CharacterQuestItemPolicy.Validate(starContract) == CharacterQuestItemMode.Invalid, "unknown AP Stars suffix extension rejected");

Check(CharacterQuestItemPolicy.Validate(Contract()) == CharacterQuestItemMode.Enabled, "exact contract");
Check(CharacterQuestItemPolicy.Validate(null) == CharacterQuestItemMode.Legacy, "native unchanged");
Check(CharacterQuestItemPolicy.Validate(new() { ["implementation_version"] = "full-level-mapping-0.24", ["schema_version"] = 15 }) == CharacterQuestItemMode.Legacy, "old seed unchanged");
foreach (string key in Contract().Keys) {
 var data = Contract(); data.Remove(key);
 Check(CharacterQuestItemPolicy.Validate(data) == CharacterQuestItemMode.Invalid, "missing " + key);
}
var malformed = Contract(); malformed["schema_version"] = "16";
Check(CharacterQuestItemPolicy.Validate(malformed) == CharacterQuestItemMode.Invalid, "string schema");
malformed = Contract(); malformed["character_quest_items"] = new Dictionary<string,string> { ["Car Battery"] = "CLEAN_HUB_GHOST_CAT_BATTERY_REWARDED", ["Old Game Data"] = "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM" };
Check(CharacterQuestItemPolicy.Validate(malformed) == CharacterQuestItemMode.Invalid, "collection cannot replace inventory");
foreach (bool owned in new[] {false,true}) foreach (bool? held in new bool?[] { null,false,true }) foreach (bool? spent in new bool?[] {null,false,true})
 Check(CharacterQuestItemPolicy.ShouldGrant(owned,held,spent) == (owned && held == false && spent == false), "grant/unknown/consumed matrix");
foreach (var item in CharacterQuestItemPolicy.All) {
 Check(CharacterQuestItemPolicy.ShouldSuppress(true,"GameRoom_Hub6",item.BagFlag,true), "source grant");
 Check(!CharacterQuestItemPolicy.ShouldSuppress(true,"GameRoom_Hub1A",item.BagFlag,false), "consumption preserved");
 Check(!CharacterQuestItemPolicy.ShouldSuppress(false,"GameRoom_Hub6",item.BagFlag,true), "legacy grant preserved");
 Check(!CharacterQuestItemPolicy.ShouldSuppress(true,"GameRoom_27",item.BagFlag,true), "wrong room");
}
Check(!CharacterQuestItemPolicy.ShouldSuppress(true,"GameRoom_Hub6","CLEAN_HUB_MEMORY_CARD_TAKEN",true), "chest check preserved");
static ReceivedItemsPacket Packet(int index, params long[] ids) => new() { Index=index, Items=ids.Select(id => new NetworkItem { Item=id }).ToArray() };
var state = new CharacterQuestItemState();
var session = new CharacterQuestItemSessionHistory(1,state);
session.HandlePacket(Packet(0,187256159));
Check(!state.Snapshot.Ready, "prelogin packet cannot mutate save");
session.Configure(true);
Check(state.Snapshot.Owned.SetEquals(new long[]{187256159}), "prelogin complete replay published at authentication");
session.HandlePacket(Packet(1,187256160));
Check(state.Snapshot.Owned.Count == 2, "incremental packet");
session.HandlePacket(Packet(0,187256159));
Check(state.Snapshot.Owned.Count == 1, "replay replaces not accumulates");
session.HandlePacket(Packet(8,187256160));
Check(state.Snapshot.Owned.Count == 1, "gap does not grant");
session.HandlePacket(Packet(0));
Check(state.Snapshot.Ready && state.Snapshot.Owned.Count == 0, "empty replay clears history");
var old = state.Snapshot;
var next = new CharacterQuestItemSessionHistory(2,state); next.Configure(true);
session.HandlePacket(Packet(0,187256159,187256160));
Check(!state.Snapshot.Ready && state.Snapshot.Owned.Count == 0, "old session cannot contaminate new identity");
bool applied=false; state.WithCurrent(old.Revision,()=>applied=true); Check(!applied,"stale Unity grant blocked");
next.HandlePacket(Packet(0,187256160));
state.WithCurrent(state.Snapshot.Revision,()=>applied=true); Check(applied,"current grant admitted");
state.Reset(); Check(!state.Snapshot.Enabled && !state.Snapshot.Ready, "shutdown clears ownership");
Console.WriteLine("Character quest item contract, receipt history, isolation and grant tests passed.");

// Execute production Unity reconciliation against isolated native boundaries.
CharacterQuestItems.State.Configure(50,true);
CharacterQuestItems.State.Publish(50,new long[]{187256159,187256160});
// Unrelated global progression traffic must not copy inventory or inspect requests.
var unrelatedRequest = new Request { Value = true };
var unrelatedEvent = new ConsumptionEvent();
CharacterQuestItems.ShouldSuppress(unrelatedRequest, "UNRELATED_FLAG");
CharacterQuestItems.RecordConsumption(unrelatedEvent, "UNRELATED_FLAG");
int readsBefore = ReflectionUtil.BoolReads;
long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
for (int i = 0; i < 100; i++) {
 Check(!CharacterQuestItems.ShouldSuppress(unrelatedRequest, "UNRELATED_FLAG"), "unrelated grant preserved");
 CharacterQuestItems.RecordConsumption(unrelatedEvent, "UNRELATED_FLAG");
}
Check(ReflectionUtil.BoolReads == readsBefore, "unrelated events skip request reflection");
Check(GC.GetAllocatedBytesForCurrentThread() - bytesBefore == 0, "unrelated events allocate no ownership snapshots");
DeveloperHarness.CurrentRoomId = "GameRoom_27";
Check(!CharacterQuestItems.ShouldSuppress(unrelatedRequest, "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM"), "off-source native grant preserved");
Check(ReflectionUtil.BoolReads == readsBefore, "off-source grant skips request reflection");
DeveloperHarness.CurrentRoomId = "GameRoom_Hub6";
Check(CharacterQuestItems.ShouldSuppress(unrelatedRequest, "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM"), "matching source grant remains suppressed");
RootsBucketRandomization.Flags["LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM"]=false;
RootsBucketRandomization.Flags["CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM"]=false;
RootsBucketRandomization.Flags["PLAYABLE_CHARACTER_UNLOCKED_MEOO"]=false;
CassetteReceiptRandomization.Stable=false;
CharacterQuestItems.TickUnity(TimeSpan.FromSeconds(1));
Check(RootsBucketRandomization.Reads==0 && WeedKillerRandomization.Grants==0,"no reflection/write before stable selected save");
CassetteReceiptRandomization.Stable=true;
CurrentPlayerSaveEnquiries.Fail=true;
CharacterQuestItems.TickUnity(TimeSpan.FromSeconds(1));
Check(WeedKillerRandomization.Grants==1,"unknown Maniac state blocks memory but not proven battery");
CurrentPlayerSaveEnquiries.Fail=false;
CharacterQuestItems.TickUnity(TimeSpan.FromSeconds(1));
Check(WeedKillerRandomization.Grants==2,"both AP items granted as inventory");
int reads=RootsBucketRandomization.Reads;
for(int i=0;i<999;i++) CharacterQuestItems.TickUnity(TimeSpan.FromMilliseconds(1));
Check(RootsBucketRandomization.Reads==reads,"no per-frame native polling");
CharacterQuestItems.TickUnity(TimeSpan.FromSeconds(1));
Check(WeedKillerRandomization.Grants==2,"held grants idempotent");
RootsBucketRandomization.Flags["LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM"]=false;
RootsBucketRandomization.Flags["CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM"]=false;
// Simulate native consumption before the delayed character reward arrives.
typeof(CharacterQuestItems).GetMethod("RecordConsumption", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.Invoke(null,
    new object[] { new ConsumptionEvent(), "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM" });
typeof(CharacterQuestItems).GetMethod("RecordConsumption", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.Invoke(null,
    new object[] { new ConsumptionEvent(), "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM" });
CharacterQuestItems.TickUnity(TimeSpan.FromSeconds(2));
Check(WeedKillerRandomization.Grants==2,"consumed inventory must not refill before the character unlock event");
RootsBucketRandomization.Flags["PLAYABLE_CHARACTER_UNLOCKED_MEOO"]=true;
CurrentPlayerSaveEnquiries.Maniac=true;
CharacterQuestItems.State.Publish(50,new long[]{187256159,187256160});
CharacterQuestItems.TickUnity(TimeSpan.FromSeconds(1));
Check(WeedKillerRandomization.Grants==2,"reconnect history does not restore consumed items");
CassetteReceiptRandomization.Identity="slot4:epoch2";
RootsBucketRandomization.Flags["PLAYABLE_CHARACTER_UNLOCKED_MEOO"]=false;
CurrentPlayerSaveEnquiries.Maniac=false;
CharacterQuestItems.TickUnity(TimeSpan.FromSeconds(1));
Check(WeedKillerRandomization.Grants==4,"new stable native save replays current seed inventory");
CharacterQuestItems.State.Configure(51,true);
RootsBucketRandomization.Flags["CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM"]=false;
CharacterQuestItems.TickUnity(TimeSpan.FromSeconds(1));
Check(WeedKillerRandomization.Grants==4,"new identity waits for complete history");
Console.WriteLine("Production character-item Unity reconciliation tests passed.");
