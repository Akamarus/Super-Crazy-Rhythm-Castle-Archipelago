namespace RhythmCastleAP {
internal static class DeveloperHarness { internal static string CurrentRoomId = ""; }
internal static class ReflectionUtil {internal static System.Reflection.Assembly GameAssembly=typeof(CurrentPlayerSaveEnquiries).Assembly;internal static int Reads; internal static bool? ReadBool(object value,string name) { Reads++; return value.GetType().GetProperty(name)?.GetValue(value) as bool?; }}
internal static class QuestChecks {internal static readonly Dictionary<string,bool?> Flags=new(); internal static int Reads; internal static bool? ReadFlag(string flag) { Reads++; return Flags.TryGetValue(flag,out var value)?value:false; }}
internal static class CassetteReceiptRandomization {internal static bool Stable=true;internal static int Slot=1;internal static void WithStableSelectedQuestSave(Action<object,string,int> action) {if(Stable)action(new object(),"native",Slot);}}
internal static class Plugin {internal static Log? LoggerInstance=new();internal static Client? AP=new();}
internal class Log {internal readonly List<string> Errors=new(); internal void LogError(string value) {Errors.Add(value);}}
internal class Client {internal string? AcceptedIdentity; internal bool QueueExpandedLocation(string name,string identity) {if(AcceptedIdentity!=null && AcceptedIdentity!=identity)return false;QueueLocation(name);return true;}internal readonly List<string> Queued=new();internal void QueueLocation(string value) {Queued.Add(value);}}
}
namespace BepInEx {internal static class Paths {internal static string ConfigPath = Path.Combine(Path.GetTempPath(),"expanded-runtime-tests-"+Guid.NewGuid().ToString("N"));}}
namespace RhythmCastleAP {internal static class ExpandedCheckResults {internal static bool? ReadFlag(string flag) => QuestChecks.ReadFlag(flag);internal static int Reads; internal static bool? ReadCompleted(string level,string variant) {Reads++;return false;}}}




internal enum ePlayableCharacter { KING=6 }
internal static class CurrentPlayerSaveEnquiries {
 internal static bool? KingUnlocked;
 public static bool IsCharacterUnlocked(ePlayableCharacter character) => KingUnlocked ?? throw new IOException("unreadable character");
}
