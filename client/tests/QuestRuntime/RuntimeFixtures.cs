using System.Reflection;
public enum ePlayableCharacter { GHOST_CAT=5, GUITAR_MANIAC=7 }
public sealed class RecordCharacterUnlockedValueInSaveDataRequest {
 public ePlayableCharacter Character {get;set;}
 public bool Unlocked {get;set;}
}
public static class CurrentPlayerSaveEnquiries {
 public static readonly HashSet<ePlayableCharacter> Unlocked = new();
 public static bool IsCharacterUnlocked(ePlayableCharacter character) => Unlocked.Contains(character);
}
public sealed class NativeProcessor {
 public int Grants;
 public void ProcessRequest(RecordCharacterUnlockedValueInSaveDataRequest request) {
  if (RhythmCastleAP.QuestChecks.SuppressCharacter(request)) throw new Exception("AP native character grant was suppressed");
  Grants++; if(request.Unlocked) CurrentPlayerSaveEnquiries.Unlocked.Add(request.Character);
 }
}
namespace BepInEx {
 internal static class Paths { internal static string ConfigPath = Path.Combine(Path.GetTempPath(),"QuestRuntime-"+Guid.NewGuid().ToString("N")); }
}
namespace RhythmCastleAP {
 internal static class Plugin { internal static TestLog LoggerInstance=new(); internal static TestAP AP=new(); }
 internal sealed class TestLog { internal void LogInfo(string text){} internal void LogError(string text){} }
 internal sealed class TestAP { internal readonly List<string> Queued=new(); internal void QueueLocation(string name)=>Queued.Add(name); }
 internal static class DeveloperHarness { internal static string CurrentRoomId="GameRoom_Hub6"; }
 internal static class ReflectionUtil {
  internal static Assembly GameAssembly=>typeof(CurrentPlayerSaveEnquiries).Assembly;
  internal static IEnumerable<Type> SafeGetTypes(Assembly assembly)=>assembly.GetTypes();
  internal static object? ReadMember(object obj,string name)=>obj.GetType().GetProperty(name)?.GetValue(obj);
  internal static bool? ReadBool(object obj,string name)=>ReadMember(obj,name) as bool?;
 }
 internal static class CassetteReceiptRandomization {
  internal static bool Stable=true;
  internal static int Slot=4;
  internal static string Identity="slot4:epoch1";
  internal static NativeProcessor Processor=new();
  internal static void WithStableSelectedQuestSave(Action<object,string,int> action) {if(Stable)action(Processor,Identity,Slot);}
  internal static void WithStableQuestItemSave(Action<object,string> action) {if(Stable)action(Processor,Identity);}
 }
 internal static class RootsBucketRandomization {
  internal static readonly Dictionary<string,bool> Flags=new();
  internal static bool TryReadProgressionFlag(string name,out bool value)=>Flags.TryGetValue(name,out value);
 }
 internal sealed class Request {public bool Value {get;init;}}
 internal sealed class Changed {public bool FlagWasSet {get;init;} public bool FlagIsSet {get;init;}}
 internal static class WeedKillerRandomization {
  internal static int Grants;
  internal static bool TrySubmitProgressionFlag(object processor,string name,bool value,out string detail) {
   var request=new Request{Value=value};
   if(CharacterQuestItems.ShouldSuppress(request,name)||QuestChecks.SuppressPlunger(request,name))throw new Exception("AP inventory receipt was suppressed");
   Grants++;RootsBucketRandomization.Flags[name]=value;detail="fixture";return true;
  }
 }
}
