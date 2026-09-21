using RhythmCastleAP;
static void Check(bool condition, string label) { if (!condition) throw new Exception(label); }
const string root = "Root/GameRoom_Hub6_Logic/GameRoom_Hub6_Script/MiscSequences/ChestRewards/";
string[] songs = { "Quicksand", "Flamenco", "TenFourGoodBuddy", "Zen", "Wiggle" };
foreach (string song in songs)
{
    string path = root + "SongRewardSequence_" + song;
    Check(CassetteChestRewardPolicy.ShouldSuppress(true, true, path + "/AwardCassette", false), song + " chest reward bypass is blocked at source");
    Check(CassetteChestRewardPolicy.ShouldSuppress(true, true, path + "/CassettePopup", true), song + " false vanilla reward popup is suppressed");
    Check(!CassetteChestRewardPolicy.ShouldSuppress(false, true, path + "/AwardCassette", false), "vanilla/incompatible slot remains native");
    Check(!CassetteChestRewardPolicy.ShouldSuppress(true, false, path + "/AwardCassette", false), "unbound save remains native");
    Check(!CassetteChestRewardPolicy.ShouldSuppress(true, true, path + "/Persist", false), "native chest persistence preserved");
    Check(!CassetteChestRewardPolicy.ShouldSuppress(true, true, path + "/AwardCassette/Other", false), "descendant is not a reward");
    Check(!CassetteChestRewardPolicy.ShouldSuppress(true, true, path + "/AwardCassette", true), "popup hook cannot suppress grant component");
    Check(!CassetteChestRewardPolicy.ShouldSuppress(true, true, path + "/CassettePopup", false), "grant hook cannot suppress popup component");

}
Check(!CassetteChestRewardPolicy.ShouldSuppress(true, true, root + "SongRewardSequence_Gold/AwardCassette", false), "unknown source preserved");
Check(!CassetteChestRewardPolicy.ShouldSuppress(true, true, "Root/GameRoom_Hub6_Logic/Objects/CassetteLord", false), "player insertion untouched");
Check(!CassetteChestRewardPolicy.ShouldSuppress(true, true, root + "GarageCartridgeRewardSequence_Gradius/AwardBagItem", false), "other chest reward untouched");
Check(!CassetteChestRewardPolicy.ShouldSuppress(true, true, null, false), "unreadable path native");
int nativeCalls = 0, saveReads = 0, notices = 0, errors = 0;
string currentPath = root + "SongRewardSequence_Quicksand/AwardCassette";
bool bound = true;
void Invoke(bool enabled = true, bool popup = false) => CassetteChestRewardAdmission.Run(enabled, popup,
    () => currentPath, () => { saveReads++; return bound; }, () => nativeCalls++, _ => notices++, _ => errors++);
Invoke();
Check(nativeCalls == 0 && notices == 1 && saveReads == 1, "actual grant admission intercepts before native dispatch");
bound = false; Invoke();
Check(nativeCalls == 1 && notices == 1, "boundary or foreign save is native");
bound = true; Invoke(false);
Check(nativeCalls == 2 && saveReads == 2, "disabled slot does no save reads");
currentPath = "Root/OtherReward"; Invoke();
Check(nativeCalls == 3 && saveReads == 2, "unrelated reward performs no save probe");
CassetteChestRewardAdmission.Run(true, false, () => throw new Exception("path"), () => true,
    () => nativeCalls++, _ => notices++, _ => errors++);
Check(nativeCalls == 4 && errors == 1, "unreadable path preserves original exactly once");
currentPath = root + "SongRewardSequence_Quicksand/AwardCassette";
CassetteChestRewardAdmission.Run(true, false, () => currentPath, () => throw new Exception("identity"),
    () => nativeCalls++, _ => notices++, _ => errors++);
Check(nativeCalls == 5 && errors == 2, "unreadable selected-save identity preserves native behavior");
CassetteChestRewardAdmission.Run(true, false, () => currentPath, () => true,
    () => nativeCalls++, _ => throw new Exception("logger"), _ => errors++);
Check(nativeCalls == 5 && errors == 3, "logging failure cannot allow confirmed randomized reward");
try
{
    CassetteChestRewardAdmission.Run(false, false, () => "", () => false,
        () => { nativeCalls++; throw new Exception("native"); }, _ => {}, _ => errors++);
}
catch (Exception ex) when (ex.Message == "native") { }
Check(nativeCalls == 6, "original exception never invokes native code twice");
// global-metadata.dat v29, native token 0x06007B56: explicit interface name
// differs from the generated interop wrapper's underscore spelling.
Check(CassetteChestHookInstallation.Target(false) ==
    ("ObtainSongCassetteOnTrigger", "TriggerReactor.OnTrigger", false), "native explicit-interface reward method uses dotted name");
Check(CassetteChestHookInstallation.Target(true) ==
    ("PopupNewSongCassetteDetailsSequenceStep", "Trigger", true), "popup target remains the native Trigger method");
var installCalls = new List<bool>();
CassetteChestHookInstallation.Run(popup => { installCalls.Add(popup); return false; });
Check(installCalls.SequenceEqual(new[] { false }), "failed reward hook cannot install popup suppression");
installCalls.Clear();
CassetteChestHookInstallation.Run(popup => { installCalls.Add(popup); return true; });
Check(installCalls.SequenceEqual(new[] { false, true }), "popup installs only after successful reward hook");
installCalls.Clear();
CassetteChestHookInstallation.Run(popup => { installCalls.Add(popup); return !popup; });
Check(installCalls.SequenceEqual(new[] { false, true }), "popup failure does not retry or remove reward hook");
Console.WriteLine("Cassette chest reward regressions passed.");
