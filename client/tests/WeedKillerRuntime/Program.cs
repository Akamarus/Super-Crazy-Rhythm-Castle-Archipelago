using System.Reflection;
using RhythmCastleAP;
int failed=0;long generation=0;
void Check(bool value,string name){Console.WriteLine($"{(value?"PASS":"FAIL")}: {name}");if(!value)failed++;}
void Tick(double seconds=2) => typeof(WeedKillerRandomization).GetMethod("TickUnity",BindingFlags.Static|BindingFlags.NonPublic)?.Invoke(null,new object[]{TimeSpan.FromSeconds(seconds)});
void Start(){WeedKillerRandomization.Configure();var h=new WeedKillerSessionHistory(++generation);h.Configure();h.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket{Index=0,Items=new[]{new Archipelago.MultiClient.Net.Packets.NetworkItem{Item=187256116}}});Native.Flags.Clear();Native.Reads=Native.Writes=0;Native.Drop=false;CassetteReceiptRandomization.Ready=false;Native.Flags["WEED_KILLER_BAG_ITEM"]=false;Native.Flags["ROOTS_HUB_LEVEL_07_DOOR_REVEALED"]=false;WeedKillerRandomization.ApplySlotData(new(){["implementation_version"]="area-routing-plant-pipes-0.15",["randomize_weed_killer"]=true});WeedKillerRandomization.CapturePlayerSaveRequestProcessor(new PlayerSaveRequestProcessor());}
Start();WeedKillerRandomization.TryApplyItem("Weed Killer");WeedKillerRandomization.TryFlushPendingNativeGrant();Tick();
Check(Native.Writes==0&&Native.Reads==0,"receipt and request hooks defer all native access until stable Unity tick");
CassetteReceiptRandomization.Ready=true;Native.Drop=true;Tick();int attempts=Native.Writes;Native.Drop=false;Tick();
Check(Native.Writes==attempts+1&&Native.Flags["WEED_KILLER_BAG_ITEM"],"silently dropped submission retries without restart");
int delivered=Native.Writes;Tick();Check(Native.Writes==delivered,"held item not duplicated");
int reads=Native.Reads;Tick(.01);Check(Native.Reads==reads,"polling bounded between Unity frames");
Native.Flags["WEED_KILLER_BAG_ITEM"]=false;Tick();Check(Native.Flags["WEED_KILLER_BAG_ITEM"],"native scene overwrite reconciles AP ownership");
Native.Flags["WEED_KILLER_BAG_ITEM"]=false;Native.Flags["ROOTS_HUB_LEVEL_07_DOOR_REVEALED"]=true;delivered=Native.Writes;WeedKillerRandomization.TryApplyItem("Weed Killer");Tick();Check(Native.Writes==delivered&&!Native.Flags["WEED_KILLER_BAG_ITEM"],"consumed door marker prevents refill on replay");
Start();Native.Flags["ROOTS_HUB_LEVEL_07_DOOR_REVEALED"]=true;WeedKillerRandomization.TryApplyItem("Weed Killer");CassetteReceiptRandomization.Ready=true;Tick();Check(Native.Writes==0,"consumed item stays consumed after process restart");
Start();WeedKillerRandomization.TryApplyItem("Weed Killer");CassetteReceiptRandomization.Ready=true;Native.Flags.Remove("ROOTS_HUB_LEVEL_07_DOOR_REVEALED");Tick();Check(Native.Writes==0,"unknown consumed state defers grant");Native.Flags["ROOTS_HUB_LEVEL_07_DOOR_REVEALED"]=false;Tick();Check(Native.Flags["WEED_KILLER_BAG_ITEM"],"unreadable state recovers");
CassetteReceiptRandomization.Identity="other-save";Native.Flags["WEED_KILLER_BAG_ITEM"]=false;Tick();Check(Native.Flags["WEED_KILLER_BAG_ITEM"],"ownership reconciles against each stable selected save");
Start();CassetteReceiptRandomization.Ready=true;Tick();
int beforeConsume=Native.Writes;
Native.Flags["WEED_KILLER_BAG_ITEM"]=false;
typeof(WeedKillerRandomization).GetMethod("RecordConsumption",BindingFlags.Static|BindingFlags.NonPublic)?.Invoke(null,new object[]{new {FlagWasSet=true,FlagIsSet=false},"WEED_KILLER_BAG_ITEM"});
Tick();Check(Native.Writes==beforeConsume&&!Native.Flags["WEED_KILLER_BAG_ITEM"],"observed consumption blocks refill before delayed door consequence");
Native.Flags["ROOTS_HUB_LEVEL_07_DOOR_REVEALED"]=true;Tick();Check(Native.Writes==beforeConsume,"later door consequence preserves consumption");
CassetteReceiptRandomization.Identity="new-save-after-consumption";Native.Flags["WEED_KILLER_BAG_ITEM"]=false;Native.Flags["ROOTS_HUB_LEVEL_07_DOOR_REVEALED"]=false;Tick();
Check(Native.Flags["WEED_KILLER_BAG_ITEM"],"consumption observation cannot suppress another stable save epoch");
var history = new WeedKillerSessionHistory(20);
history.Configure();
history.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket {Index=2,Items=new[]{new Archipelago.MultiClient.Net.Packets.NetworkItem{Item=187256116}}});
Check(!WeedKillerRandomization.History.Owned,"incomplete history cannot authorize delivery");
history.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket {Index=0,Items=new[]{new Archipelago.MultiClient.Net.Packets.NetworkItem{Item=187256116}}});
Check(WeedKillerRandomization.History.Owned,"complete receipt history authorizes delivery");
history.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket {Index=3,Items=Array.Empty<Archipelago.MultiClient.Net.Packets.NetworkItem>()});
Check(!WeedKillerRandomization.History.Owned,"gapped history revokes prior delivery authority");
history.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket {Index=0,Items=new[]{new Archipelago.MultiClient.Net.Packets.NetworkItem{Item=187256116}}});
WeedKillerRandomization.History.End(20);
history.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket {Index=0,Items=new[]{new Archipelago.MultiClient.Net.Packets.NetworkItem{Item=187256116}}});
Check(!WeedKillerRandomization.History.Owned,"ended session cannot republish receipts");
var next = new WeedKillerSessionHistory(21);next.Configure();
history.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket {Index=0,Items=new[]{new Archipelago.MultiClient.Net.Packets.NetworkItem{Item=187256116}}});
Check(!WeedKillerRandomization.History.Owned,"stale prior seed callback cannot grant new save");
next.HandlePacket(new Archipelago.MultiClient.Net.Packets.ReceivedItemsPacket {Index=0,Items=Array.Empty<Archipelago.MultiClient.Net.Packets.NetworkItem>()});
WeedKillerRandomization.TryApplyItem("Weed Killer");
Check(!WeedKillerRandomization.History.Owned,"receipt notification alone cannot bypass authoritative history");
Environment.ExitCode=failed==0?0:1;
namespace RhythmCastleAP {
internal static class Native {internal static Dictionary<string,bool> Flags=new();internal static int Writes,Reads;internal static bool Drop;}
internal static class RootsBucketRandomization {internal static bool TryReadProgressionFlag(string flag,out bool value){Native.Reads++;return Native.Flags.TryGetValue(flag,out value);}}
internal static class CassetteReceiptRandomization {internal static bool Ready;internal static string Identity="save-a";internal static void WithStableQuestItemSave(Action<object,string>a){if(Ready)a(new PlayerSaveRequestProcessor(),Identity);}}
internal static class Plugin {internal static Log? LoggerInstance=null;internal static Ap? AP=null;}
internal sealed class Log {public void LogWarning(string s){}public void LogInfo(string s){}}
internal sealed class Ap {public void QueueLocation(string s){}}
internal static class DeveloperHarness {internal static string CurrentRoomId="GameRoom_Hub2";}
internal static class ReflectionUtil {internal static Assembly GameAssembly=>typeof(RecordGameProgressionInSaveDataRequest).Assembly;internal static bool? ReadBool(object o,string n)=>(bool?)o.GetType().GetProperty(n)?.GetValue(o);}
}
public enum eGameProgressionFlag {WEED_KILLER_BAG_ITEM,ROOTS_HUB_LEVEL_07_DOOR_REVEALED}
public sealed class RecordGameProgressionInSaveDataRequest {public eGameProgressionFlag Flag{get;set;}public bool Value{get;set;}public RecordGameProgressionInSaveDataRequest(eGameProgressionFlag flag,bool value){Flag=flag;Value=value;}}
public sealed class PlayerSaveRequestProcessor {public void ProcessRequest(RecordGameProgressionInSaveDataRequest request){Native.Writes++;if(!Native.Drop)Native.Flags[request.Flag.ToString()]=request.Value;}}

namespace Archipelago.MultiClient.Net.Packets {
internal sealed class ReceivedItemsPacket {public int Index{get;set;}public NetworkItem[]? Items{get;set;}}
internal sealed class NetworkItem {public long Item{get;set;}}
}