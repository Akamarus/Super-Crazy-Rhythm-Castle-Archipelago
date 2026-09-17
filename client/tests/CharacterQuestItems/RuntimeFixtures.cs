using System.Reflection;
namespace RhythmCastleAP;
internal static class Plugin { internal static TestLog LoggerInstance = new(); }
internal sealed class TestLog { internal void LogInfo(string value) { } }
internal static class DeveloperHarness { internal static string CurrentRoomId = "GameRoom_Hub6"; }
internal static class ReflectionUtil {
 internal static Assembly GameAssembly => typeof(CurrentPlayerSaveEnquiries).Assembly;
 internal static IEnumerable<Type> SafeGetTypes(Assembly assembly) => assembly.GetTypes();
 internal static bool? ReadBool(object obj,string name) => obj.GetType().GetProperty(name)?.GetValue(obj) as bool?;
}
internal enum ePlayableCharacter { GUITAR_MANIAC = 7 }
internal static class CurrentPlayerSaveEnquiries {
 internal static bool Maniac, Fail;
 public static bool IsCharacterUnlocked(ePlayableCharacter character) { if(Fail) throw new Exception(); return Maniac; }
}
internal static class CassetteReceiptRandomization {
 internal static bool Stable;
 internal static string Identity = "slot4:epoch1";
 internal static void WithStableQuestItemSave(Action<object,string> action) { if(Stable) action(new object(),Identity); }
}
internal static class RootsBucketRandomization {
 internal static Dictionary<string,bool> Flags = new();
 internal static int Reads;
 internal static bool TryReadProgressionFlag(string name,out bool value) { Reads++; return Flags.TryGetValue(name,out value); }
}
internal sealed class Request { public bool Value {get;init;} }
internal static class WeedKillerRandomization {
 internal static int Grants;
 internal static bool TrySubmitProgressionFlag(object processor,string name,bool value,out string detail) {
  if(CharacterQuestItems.ShouldSuppress(new Request { Value=value },name)) throw new Exception("AP receipt suppressed");
  Grants++; RootsBucketRandomization.Flags[name]=value; detail="fixture"; return true;
 }
}
internal sealed class ConsumptionEvent { public bool FlagWasSet => true; public bool FlagIsSet => false; }

internal static class QuestChecks { internal static bool Enabled => false; internal static bool? HandInConsumed(string name) => null; }
