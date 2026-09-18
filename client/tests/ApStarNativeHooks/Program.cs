using System.Reflection;
using RhythmCastleAP;
using HarmonyLib;
int checks = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); checks++; }
object? Call(string name, params object?[] args) { var m=typeof(ApStarNativeHooks).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic)!; var result=m.Invoke(null,args); return result; }
var h=new Harmony(); int blocked=0; int admitted=0; int observed=0; string seen="";
int n=ApStarNativeHooks.Install(h,x=>17,(l,v)=>{seen=l+"/"+v; return l=="Level_28" && v=="LevelVariant_Default";},(l,v)=>blocked++,(l,v)=>admitted++,(l,v)=>observed++);
Check(n==3 && ApStarNativeHooks.Ready,"exact three native boundaries found");
Check(h.Methods.Single(x=>x.DeclaringType==typeof(GameFlowRequestProcessor)).GetParameters()[0].ParameterType==typeof(StartLevelRequest),"same-name overload not selected");
Check((bool)Call("StartPrefix",(object)new object[]{new StartLevelRequest("Level_28","LevelVariant_Default")})!,"normal final entry admitted");
Check(!(bool)Call("StartPrefix",(object)new object[]{new StartLevelRequest("Level_14","LevelVariant_Default")})! && blocked==1,"blocked entry reported once");
Check(!(bool)Call("StartPrefix",(object)new object[]{new OtherRequest()})!,"unreadable request fails policy closed");
object?[] preview={new LevelPreviewUI(),false}; typeof(ApStarNativeHooks).GetMethod("PreviewPostfix",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,preview);
Check(preview[1] is false,"native preview restriction preserved");
preview=new object?[]{new LevelPreviewUI(),true};typeof(ApStarNativeHooks).GetMethod("PreviewPostfix",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,preview);
Check(preview[1] is true && seen=="Level_28/LevelVariant_Default","preview reads exact state identity");
object?[] total={66};typeof(ApStarNativeHooks).GetMethod("TotalPostfix",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,total);Check((int)total[0]! ==17,"total uses authoritative delegate");
Check(admitted==1,"only admitted start captures attempt context");
Check(observed==2,"preview observation includes native-blocked preview");
Console.WriteLine($"{checks} native hook fixture checks passed");
public class StartLevelRequest {public string LevelIdentifier {get;} public string Variant {get;} public StartLevelRequest(string l,string v){LevelIdentifier=l;Variant=v;}}
public class OtherRequest {}
public class GameFlowRequestProcessor {public void ProcessRequest(StartLevelRequest x){} public void ProcessRequest(OtherRequest x){}}
public static class CurrentPlayerSaveEnquiries {public static int GetTotalNumStars()=>66;public static int GetTotalNumStars(int x)=>x;}
public class LevelPreviewUI {public bool CanEnterDisplayedLevelVariant(bool x)=>true;public object ObtainState()=>new {LevelIdentifier="Level_28",LevelVariantIdentifier="LevelVariant_Default"};}
namespace HarmonyLib {public class Harmony {public List<MethodInfo> Methods=new();public void Patch(MethodInfo m,HarmonyMethod? prefix=null,HarmonyMethod? postfix=null)=>Methods.Add(m);} public class HarmonyMethod {public HarmonyMethod(MethodInfo m){}}}
namespace RhythmCastleAP { public static class ReflectionUtil {public static Assembly GameAssembly=>typeof(ReflectionUtil).Assembly;public static IEnumerable<Type> SafeGetTypes(Assembly a)=>a.GetTypes();public static object? FindArg(object[]? a,string n)=>a?.FirstOrDefault(x=>x.GetType().Name==n);public static object? ReadMember(object? x,string n)=>x?.GetType().GetProperty(n)?.GetValue(x);public static string? ExtractIdentifier(object? x)=>x?.ToString();} public static class Plugin {public static Logger? LoggerInstance;} public class Logger {public void LogInfo(string x){} public void LogError(string x){} public void LogWarning(string x){}} }
