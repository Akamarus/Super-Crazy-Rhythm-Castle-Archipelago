namespace BepInEx { internal static class Paths { internal static string ConfigPath = Path.Combine(AppContext.BaseDirectory, "runtime-fixture-" + Guid.NewGuid()); } }
namespace RhythmCastleAP {
internal static class Plugin { internal static TestLog LoggerInstance = new(); internal static TestAP AP = new(); }
internal sealed class TestLog { internal void LogError(string message) { throw new Exception(message); } }
internal sealed class TestAP { internal HashSet<string> Queued = new(); internal bool QueueExpandedLocation(string name, string identity) { Queued.Add(name); return true; } }
internal static class ReflectionUtil { internal static bool? ReadBool(object value, string name) => value.GetType().GetProperty(name)?.GetValue(value) as bool?; }
internal static class CassetteReceiptRandomization {
 internal static bool Stable = true; internal static int Slot = 2;
 internal static void WithStableSelectedQuestSave(Action<object, string, int> action) { if (Stable) action(new object(), "epoch1", Slot); }
}
internal static class RootsBucketRandomization {
 internal static Dictionary<string, bool> Flags = new();
 internal static bool TryReadProgressionFlag(string flag, out bool value) => Flags.TryGetValue(flag, out value);
}
internal sealed class Request { public bool Value { get; init; } }
internal static class WeedKillerRandomization {
 internal static int Grants;
 internal static bool TrySubmitProgressionFlag(object processor, string flag, bool value, out string detail) {
  if (!BunkerRuntime.BeforeProgressionRequest(new Request {Value = value}, flag)) throw new Exception("Receipt was suppressed");
  Grants++; RootsBucketRandomization.Flags[flag] = value; detail = "fixture"; return true;
 }
}
}
namespace RhythmCastleAP { internal static class DeveloperHarness { internal static string CurrentRoomId = "GameRoom_Hub8"; } }
