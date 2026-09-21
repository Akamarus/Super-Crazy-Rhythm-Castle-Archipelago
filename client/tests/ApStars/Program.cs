using RhythmCastleAP;
using Newtonsoft.Json.Linq;
static class Program {
 static int passed;
 static void Check(bool value,string label){if(!value)throw new Exception(label);passed++;}
 static Dictionary<string,object> Contract(int goal=50)=>new(){
 ["schema_version"]=19,["implementation_version"]=ApStarContract.Implementation,["star_victory_schema"]=1,
 ["star_item_name"]="Star",["star_item_id"]=187256118,["star_item_count"]=66,["star_item_maximum"]=66,
 ["star_items_active"]=true,["client_star_gate_enforcement_active"]=true,["post_threshold_victory_active"]=true,
 ["required_stars"]=goal,["campaign_level_mapping_schema"]=1,["victory_level_internal_id"]="Level_28",
 ["generated_star_requirements"]=Enumerable.Range(1,22).ToDictionary(n=>"Level "+n,n=>Math.Min(n-1,goal-1)),
 ["star_eater_requirements"]=new Dictionary<string,int>{{"Roots",3},{"Lobby",15},{"Cell Tower",30},{"Royal Corridor",40},{"Secret Bunker",50}}};
 static void Main(){
 var contract=ApStarContract.Validate(Contract());Check(contract.Mode==ApStarMode.Awaiting,"valid exact contract");
 foreach(string key in Contract().Keys){var bad=Contract();bad.Remove(key);Check(ApStarContract.Validate(bad).Mode==ApStarMode.Incompatible,"missing "+key);}
 var badType=Contract();badType["required_stars"]="50";Check(ApStarContract.Validate(badType).Mode==ApStarMode.Incompatible,"numeric strings rejected");
 var badMap=Contract();badMap["generated_star_requirements"]=new Dictionary<string,int>{{"Level 1",0}};Check(ApStarContract.Validate(badMap).Mode==ApStarMode.Incompatible,"partial gate map rejected");
 var badLevel22=Contract();((Dictionary<string,int>)badLevel22["generated_star_requirements"])["Level 22"]=50;Check(ApStarContract.Validate(badLevel22).Mode==ApStarMode.Incompatible,"Level22 below goal required");
 Check(ApStarContract.Validate(Contract(1)).Mode==ApStarMode.Awaiting,"goal1 all gates zero");
 var j=JObject.FromObject(Contract()).ToObject<Dictionary<string,object>>()!;Check(ApStarContract.Validate(j).Mode==ApStarMode.Awaiting,"actual JSON shapes");
 var legacy=Contract();legacy["schema_version"]=18;legacy["implementation_version"]=ApStarContract.Implementation[..^14];legacy["star_items_active"]=false;legacy["client_star_gate_enforcement_active"]=false;legacy.Remove("star_victory_schema");legacy.Remove("post_threshold_victory_active");Check(ApStarContract.Validate(legacy).Mode==ApStarMode.Native,"legacy inactive retains native");
 var state=new ApStarState();state.Configure(1,"seed|team0|slot1",contract);Check(!state.CanEnter("Level_05","LevelVariant_Default"),"awaiting sync fails closed even zero gate");
 state.Publish(1,Array.Empty<long>());Check(state.CanEnter("Level_05","LevelVariant_Default"),"synced zero gate opens");Check(!state.CanEnter("Level_06","LevelVariant_Default"),"below gate blocked");
 state.Publish(1,new[]{187256118L});Check(state.CanEnter("Level_06","LevelVariant_Default"),"at gate opens");Check(state.CanEnter("Level_06","LevelVariant_BeeMode"),"special variant not campaign gated");
 state.Publish(1,Enumerable.Repeat(187256118L,49));var early=state.Capture("native4","Level_28","LevelVariant_Default",1);state.Publish(1,Enumerable.Repeat(187256118L,50));Check(!state.Commit(early,"native4"),"late Star cannot qualify earlier clear");Check(!state.GoalPending,"receipt alone never wins");
 var failed=state.Capture("native4","Level_28","LevelVariant_Default",0);Check(!state.Commit(failed,"native4"),"failed clear never wins");
 var ready=state.Capture("native4","Level_28","LevelVariant_Default",1);Check(!state.Commit(ready,"different-save"),"wrong native save rejected");
 state.Disconnect(1);Check(state.Total==50 && state.CanEnter("Level_28","LevelVariant_Default"),"disconnect retains synchronized AP stars");Check(state.Commit(ready,"native4"),"qualified offline result can queue goal");Check(!state.Commit(ready,"native4"),"persisted duplicate consumed once");
 state.Configure(2,"seed|team0|slot1",contract);Check(state.Total==50 && state.GoalPending,"same identity reconnect preserves total and pending goal");state.Publish(1,Array.Empty<long>());Check(state.Total==50,"stale history fenced");state.Publish(2,new[]{187256118L});Check(state.Total==1,"authoritative replay replaces count");
 state.Configure(3,"other|team0|slot1",contract);Check(state.Total==0&&!state.GoalPending,"identity switch clears stars and goal");Check(!state.Commit(ready,"native4"),"old identity result fenced");
 state.Publish(3,Enumerable.Repeat(187256118L,100));Check(state.Total==66,"admin excess capped66");state.Publish(3,Enumerable.Repeat(187256118L,100));Check(state.Total==66,"history replay idempotent");
 Check(state.ResolveHud("GameRoom_Hub6",7)==7,"MusicLab retains point HUD");Check(state.ResolveHud("GameRoom_Hub5B",7)==66,"CellTower displays AP stars");Check(state.ResolveHud("GameRoom_Hub8",7)==66,"Bunker displays AP stars");
 state.Configure(4,"third",ApStarContract.Validate(badType));Check(!state.CanEnter("Level_05","LevelVariant_Default")&&state.ResolveHud("GameRoom_Hub2",99)==0,"malformed claim fails closed");
 var packetState=new ApStarState();var h=new ApStarSessionHistory(1,packetState);
 h.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=0, Items=new[]{new Archipelago.MultiClient.Net.Models.NetworkItem {Item=ApStarContract.StarId}} });
 h.ApplySlotData(Contract(),"packet-slot");Check(packetState.Total==1,"prelogin packet retained until config");
 h.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=1, Items=new[]{new Archipelago.MultiClient.Net.Models.NetworkItem {Item=ApStarContract.StarId}} });Check(packetState.Total==2,"incremental packet appended");
 h.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=4, Items=Array.Empty<Archipelago.MultiClient.Net.Models.NetworkItem>() });Check(packetState.Total==2,"gap retains prior authoritative total");
 Check(packetState.Mode==ApStarMode.Awaiting&&!packetState.CanEnter("Level_05","LevelVariant_Default")&&packetState.ResolveTotal(99)==0,"gap suspends gate and HUD authority until full replay");
 h.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=2, Items=Array.Empty<Archipelago.MultiClient.Net.Models.NetworkItem>() });
 Check(packetState.Mode==ApStarMode.Awaiting,"incremental packet cannot repair a missing history segment");
 h.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=0, Items=Array.Empty<Archipelago.MultiClient.Net.Models.NetworkItem>() });Check(packetState.Total==0,"empty replay clears old count");
 Check(packetState.Mode==ApStarMode.Ready&&packetState.CanEnter("Level_05","LevelVariant_Default"),"complete empty replay restores authority");
 h.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=0, Items=Enumerable.Repeat(new Archipelago.MultiClient.Net.Models.NetworkItem {Item=ApStarContract.StarId},50).ToArray() });
 h.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=-1, Items=Array.Empty<Archipelago.MultiClient.Net.Models.NetworkItem>() });
 var invalidClear=packetState.Capture("native","Level_28","LevelVariant_Default",1);
 Check(packetState.Mode==ApStarMode.Awaiting&&!packetState.Commit(invalidClear,"native"),"negative packet index cannot qualify victory using prior Stars");
 packetState.Disconnect(1);Check(!packetState.CanEnter("Level_05","LevelVariant_Default"),"disconnect after invalid history must not restore gate authority");
 h.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=0, Items=Enumerable.Repeat(new Archipelago.MultiClient.Net.Models.NetworkItem {Item=ApStarContract.StarId},50).ToArray() });
 Check(packetState.Mode==ApStarMode.Ready&&!packetState.Commit(invalidClear,"native"),"valid replay cannot retroactively qualify an invalid-history clear");
 var replay=new ApStarSessionHistory(2,packetState);
 replay.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=0, Items=null });
 replay.ApplySlotData(Contract(),"packet-slot");
 Check(packetState.Mode==ApStarMode.Awaiting,"invalid prelogin history remains suspended after configuration");
 replay.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=0, Items=Array.Empty<Archipelago.MultiClient.Net.Models.NetworkItem>() });
 h.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=-1, Items=null });
 Check(packetState.Mode==ApStarMode.Ready,"retired generation cannot suspend current authoritative history");
 replay.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket { Index=0, Items=null });
 Check(packetState.Mode==ApStarMode.Awaiting,"null item history suspends an already configured session");
 Check(!state.CanEnter("",""),"unknown entry blocked under claimed contract");
 Console.WriteLine($"{passed} AP Star assertions passed.");
 }
}
