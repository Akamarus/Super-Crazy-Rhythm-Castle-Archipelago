using System.Reflection;
using RhythmCastleAP;
var type = Assembly.GetExecutingAssembly().GetType("RhythmCastleAP.DifficultySelectionAvailability");
void Tick(double seconds = 1) => type?.GetMethod("TickUnity", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { TimeSpan.FromSeconds(seconds) });
int failures = 0;
void Check(string name, bool pass) { if (!pass) { failures++; Console.WriteLine("FAIL " + name); } }
Tick(); Check("unstable save cannot read or write", RootsBucketRandomization.Reads == 0 && WeedKillerRandomization.Writes == 0);
CassetteReceiptRandomization.Stable = true;
AreaAccessPrototype.Enabled = false; Tick(); Check("disabled preserves native state", WeedKillerRandomization.Writes == 0);
AreaAccessPrototype.Enabled = true; IntroHubSkip.Compatible = false; Tick(); Check("incompatible preserves native state", WeedKillerRandomization.Writes == 0);
IntroHubSkip.Compatible = true; AreaAccessPrototype.AuthenticatedCompatibleSession = false; Tick(); Check("disconnected preserves native state", WeedKillerRandomization.Writes == 0);
AreaAccessPrototype.AuthenticatedCompatibleSession = true;
RootsBucketRandomization.Readable = false; Tick(); Check("unreadable flag is not false", WeedKillerRandomization.Writes == 0);
RootsBucketRandomization.Readable = true; Tick(); Check("stable fresh save writes flag", WeedKillerRandomization.Writes == 1);
Check("write verified by readback", RootsBucketRandomization.Reads >= 3);
Tick(0.01); Check("poll interval prevents immediate retry", WeedKillerRandomization.Writes == 1);
Tick(); Check("unverified successful submission retries", WeedKillerRandomization.Writes == 2);
WeedKillerRandomization.Apply = true; Tick(); Check("readback confirms native flag", RootsBucketRandomization.Owned && WeedKillerRandomization.Writes == 3);
Tick(); Check("owned flag is not resubmitted", WeedKillerRandomization.Writes == 3);
CassetteReceiptRandomization.Identity = "save-b"; RootsBucketRandomization.Owned = false; WeedKillerRandomization.Apply = false; WeedKillerRandomization.Submit = false;
for (int i = 0; i < 9; i++) Tick();
Check("failed submissions bounded per save", WeedKillerRandomization.Writes == 7);
CassetteReceiptRandomization.Identity = "save-c"; WeedKillerRandomization.Apply = true; WeedKillerRandomization.Submit = true; Tick();
Check("new save gets independent retry budget", RootsBucketRandomization.Owned && WeedKillerRandomization.Writes == 8);
Check("only assignment flag true through stable processor", WeedKillerRandomization.Exact);
Console.WriteLine($"Difficulty selection: {failures} failures"); return failures == 0 ? 0 : 1;
namespace RhythmCastleAP
{
 internal static class AreaAccessPrototype { internal static bool Enabled = true, AuthenticatedCompatibleSession = true; }
 internal static class IntroHubSkip { internal static bool Compatible = true; }
 internal static class CassetteReceiptRandomization { internal static bool Stable; internal static string Identity = "save-a"; internal static readonly object Processor = new(); internal static void WithStableQuestItemSave(Action<object,string> a) { if (Stable) a(Processor,Identity); } }
 internal static class RootsBucketRandomization { internal static bool Readable = true, Owned; internal static int Reads; internal static bool TryReadProgressionFlag(string flag, out bool value) { Reads++; if(flag != "ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE") throw new Exception("wrong read"); value=Owned; return Readable; } }
 internal static class WeedKillerRandomization { internal static bool Apply, Submit = true, Exact = true; internal static int Writes; internal static bool TrySubmitProgressionFlag(object processor,string flag,bool value,out string detail) { Writes++; Exact &= ReferenceEquals(processor,CassetteReceiptRandomization.Processor) && flag == "ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE" && value; if (Apply && Submit) RootsBucketRandomization.Owned=true; detail="fixture"; return Submit; } }
 internal static class Plugin { internal static readonly Log? LoggerInstance = new(); }
 internal sealed class Log { public void LogInfo(string text) { } public void LogWarning(string text) { } }
}
