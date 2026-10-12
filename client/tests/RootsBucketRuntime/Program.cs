using System.Reflection;
using RhythmCastleAP;
static class Program {
 static int passed;
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);passed++;}
 static void Tick(){var method=typeof(RootsBucketRandomization).GetMethod("TickUnity",BindingFlags.Static|BindingFlags.NonPublic);method?.Invoke(null,new object[]{TimeSpan.FromSeconds(2)});}
 static void Main(){
 RootsBucketRandomization.Configure();
 var data=new Dictionary<string,object>{["implementation_version"]=RootsBucketRandomizationPolicy.CompatibleVersionPrefix,["randomize_hip_glasses_chicken_bucket"]=true,
 ["hip_glasses_item"]="Hip Glasses",["chicken_bucket_item"]="Chicken Bucket",["hip_glasses_source_location"]="Roots - Level 4 - Hip Glasses",["bucket_minion_trade_location"]="Roots - Bucket Minion Trade",["hip_glasses_source_flag"]="LEVEL_08_GLASSES_COLLECTED",["hip_glasses_native_flag"]="HIP_GLASSES_BAG_ITEM",["bucket_trade_flag"]="ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES",["chicken_bucket_native_flag"]="CHICKEN_BUCKET_BAG_ITEM",["combo_bucket_conversion_flag"]="LEVEL_09_COMBO_ABILITY_EARNED"};
 RootsBucketRandomization.ApplySlotData(data);
 RootsBucketRandomization.TryApplyItem("Hip Glasses");RootsBucketRandomization.TryApplyItem("Chicken Bucket");
 Tick();Check(WeedKillerRandomization.Grants==0,"defer until selected save ready");
 CassetteReceiptRandomization.Ready=true;Tick();
 Check(WeedKillerRandomization.Grants==2,"deliver starting items without progression request");
 Tick();Check(WeedKillerRandomization.Grants==2,"held items not duplicated");
 CurrentPlayerSaveEnquiries.Flags.Clear();CassetteReceiptRandomization.Identity="save-b";Tick();
 Check(WeedKillerRandomization.Grants==4,"new save gets owned items despite prior grant");
 CurrentPlayerSaveEnquiries.Flags.Clear();CurrentPlayerSaveEnquiries.Flags[eGameProgressionFlag.ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES]=true;CurrentPlayerSaveEnquiries.Flags[eGameProgressionFlag.LEVEL_09_COMBO_ABILITY_EARNED]=true;CassetteReceiptRandomization.Identity="save-c";Tick();
 Check(WeedKillerRandomization.Grants==4,"consumed items not restored after save switch");
 RootsBucketRandomization.Configure();RootsBucketRandomization.ApplySlotData(data);
 CurrentPlayerSaveEnquiries.Flags.Clear();WeedKillerRandomization.Grants=0;WeedKillerRandomization.Drop=true;CurrentPlayerSaveEnquiries.Reads=0;
 RootsBucketRandomization.TryApplyItem("Hip Glasses");RootsBucketRandomization.TryApplyItem("Chicken Bucket");
 Check(WeedKillerRandomization.Grants==0&&CurrentPlayerSaveEnquiries.Reads==0,"receipt callbacks do not access native state");
 Tick();Check(WeedKillerRandomization.Grants==2,"both grants attempted when native state is ready");
 WeedKillerRandomization.Drop=false;Tick();
 Check(WeedKillerRandomization.Grants==4&&CurrentPlayerSaveEnquiries.Flags.GetValueOrDefault(eGameProgressionFlag.HIP_GLASSES_BAG_ITEM)&&CurrentPlayerSaveEnquiries.Flags.GetValueOrDefault(eGameProgressionFlag.CHICKEN_BUCKET_BAG_ITEM),"silently dropped grants retry without restart");
 CurrentPlayerSaveEnquiries.Flags.Clear();Tick();
 Check(WeedKillerRandomization.Grants==6,"native scene overwrite reconciles without stale process latch");
 CurrentPlayerSaveEnquiries.Flags.Clear();CassetteReceiptRandomization.Ready=false;Tick();
 Check(WeedKillerRandomization.Grants==6,"previous ready save cannot bypass current selected-save gate");
 CassetteReceiptRandomization.Ready=true;CurrentPlayerSaveEnquiries.Flags[eGameProgressionFlag.ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES]=true;CurrentPlayerSaveEnquiries.Flags[eGameProgressionFlag.LEVEL_09_COMBO_ABILITY_EARNED]=true;Tick();
 Check(WeedKillerRandomization.Grants==6,"consumed markers override missing inventory after successful grants");
 RootsBucketRandomization.Configure();RootsBucketRandomization.ApplySlotData(data);CurrentPlayerSaveEnquiries.Flags.Clear();WeedKillerRandomization.Grants=0;
 RootsBucketRandomization.TryApplyItem("Hip Glasses");RootsBucketRandomization.TryApplyItem("Chicken Bucket");Tick();
 CurrentPlayerSaveEnquiries.Flags.Clear();
 foreach(string bag in new[]{"HIP_GLASSES_BAG_ITEM","CHICKEN_BUCKET_BAG_ITEM"}) typeof(RootsBucketRandomization).GetMethod("RecordConsumption",BindingFlags.Static|BindingFlags.NonPublic)?.Invoke(null,new object[]{new {FlagWasSet=true,FlagIsSet=false},bag});
 Tick();Check(WeedKillerRandomization.Grants==2,"observed bag consumption blocks refill before delayed trade/conversion markers");
 CurrentPlayerSaveEnquiries.Flags[eGameProgressionFlag.ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES]=true;CurrentPlayerSaveEnquiries.Flags[eGameProgressionFlag.LEVEL_09_COMBO_ABILITY_EARNED]=true;Tick();
 Check(WeedKillerRandomization.Grants==2,"later trade/conversion markers preserve consumption");
 CassetteReceiptRandomization.Identity="new-save-after-consumption";CurrentPlayerSaveEnquiries.Flags.Clear();Tick();
 Check(WeedKillerRandomization.Grants==4,"consumption observations are scoped to selected-save epoch");
 Console.WriteLine($"{passed} runtime scenarios passed");
 }
}
public enum eGameProgressionFlag {HIP_GLASSES_BAG_ITEM,ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES,CHICKEN_BUCKET_BAG_ITEM,LEVEL_09_COMBO_ABILITY_EARNED}
public static class CurrentPlayerSaveEnquiries { public static Dictionary<eGameProgressionFlag,bool> Flags=new(); public static int Reads; public static bool GetProgressionFlagValue(eGameProgressionFlag flag){Reads++;return Flags.GetValueOrDefault(flag);} }
public class PlayerSaveRequestProcessor {}
namespace RhythmCastleAP {
 static class CassetteReceiptRandomization {internal static bool Ready;internal static string Identity="save-a";internal static void WithStableQuestItemSave(Action<object,string> action){if(Ready)action(new PlayerSaveRequestProcessor(),Identity);} }
 static class WeedKillerRandomization {internal static int Grants;internal static bool Drop;internal static void CapturePlayerSaveRequestProcessor(object p){}internal static void TryFlushPendingNativeGrant(){} internal static bool TrySubmitProgressionFlag(object p,string flag,bool value,out string detail){Grants++;if(!Drop)CurrentPlayerSaveEnquiries.Flags[Enum.Parse<eGameProgressionFlag>(flag)]=value;detail="test";return true;} }
 static class ReflectionUtil {internal static Assembly GameAssembly=>Assembly.GetExecutingAssembly();internal static Type[] SafeGetTypes(Assembly a)=>a.GetTypes();internal static bool? ReadBool(object o,string n)=>(bool?)o.GetType().GetProperty(n)?.GetValue(o);internal static object? ReadMember(object o,string n)=>null;}
 static class DeveloperHarness {internal static string CurrentRoomId="GameRoom_Hub2";}
 static class Plugin {internal static Logger? LoggerInstance=new();internal static Api? AP=new();}
 class Logger {internal void LogInfo(string s){}internal void LogWarning(string s){} }
 class Api {internal void QueueLocation(string s){} }
}
