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
 Console.WriteLine($"{passed} runtime scenarios passed");
 }
}
public enum eGameProgressionFlag {HIP_GLASSES_BAG_ITEM,ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES,CHICKEN_BUCKET_BAG_ITEM,LEVEL_09_COMBO_ABILITY_EARNED}
public static class CurrentPlayerSaveEnquiries { public static Dictionary<eGameProgressionFlag,bool> Flags=new(); public static bool GetProgressionFlagValue(eGameProgressionFlag flag)=>Flags.GetValueOrDefault(flag); }
public class PlayerSaveRequestProcessor {}
namespace RhythmCastleAP {
 static class CassetteReceiptRandomization {internal static bool Ready;internal static string Identity="save-a";internal static void WithStableQuestItemSave(Action<object,string> action){if(Ready)action(new PlayerSaveRequestProcessor(),Identity);} }
 static class WeedKillerRandomization {internal static int Grants;internal static void CapturePlayerSaveRequestProcessor(object p){}internal static void TryFlushPendingNativeGrant(){} internal static bool TrySubmitProgressionFlag(object p,string flag,bool value,out string detail){Grants++;CurrentPlayerSaveEnquiries.Flags[Enum.Parse<eGameProgressionFlag>(flag)]=value;detail="test";return true;} }
 static class ReflectionUtil {internal static Assembly GameAssembly=>Assembly.GetExecutingAssembly();internal static Type[] SafeGetTypes(Assembly a)=>a.GetTypes();internal static bool? ReadBool(object o,string n)=>false;internal static object? ReadMember(object o,string n)=>null;}
 static class DeveloperHarness {internal static string CurrentRoomId="GameRoom_Hub2";}
 static class Plugin {internal static Logger? LoggerInstance=new();internal static Api? AP=new();}
 class Logger {internal void LogInfo(string s){}internal void LogWarning(string s){} }
 class Api {internal void QueueLocation(string s){} }
}
