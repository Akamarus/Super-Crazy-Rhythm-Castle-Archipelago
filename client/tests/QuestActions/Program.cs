using RhythmCastleAP;

var tests = new (string Name, Action Run)[]
{
    ("Missing discovery candidate loses one of the 17 requested actions", AllCandidates),
    ("Wrong room or unrelated token must not consume diagnostic capacity", RejectNoise),
    ("Duplicate flag callbacks must log once while retaining distinct transitions", TransitionDedup),
    ("Unbounded unique event flood must stop at the per-channel limit", BoundFlood),
    ("A request must not suppress the corresponding observed event", SeparateChannels),
    ("Wrong-room scene objects must not leak into a hub capture", SceneScope),
    ("Repeated manual snapshots must not repeat native reads", SnapshotOnce),
    ("Dispatch must preserve unknown and true-to-false event values", DispatchFlags),
    ("Request dispatch must read Value without claiming a transition", DispatchRequest),
    ("Result dispatch must keep native variant identity and reject unrelated rooms", DispatchResult),
    ("Snapshot dispatch must preserve unavailable reads and never submit state", DispatchSnapshot),
};
int failed = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine("PASS " + test.Name); }
    catch (Exception ex) { failed++; Console.WriteLine("FAIL " + test.Name + ": " + ex.Message); }
}
Console.WriteLine($"QuestActions: {tests.Length - failed}/{tests.Length} passed.");
return failed == 0 ? 0 : 1;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static void AllCandidates()
{
    // Literal evidence fixtures; no expectation is built from the production catalog.
    var fixtures = new[]
    {
        ("GameRoom_09", "LEVEL_09_COMBO_ABILITY_EARNED", "roots_combo_bucket_conversion"),
        ("GameRoom_Hub1A", "ImportantLetters", "lobby_important_letters_delivery"),
        ("GameRoom_Hub1B", "PLUNGER_BAG_ITEM", "lobby_plunger_hand_in"),
        ("GameRoom_Hub1A", "LOBBY_HUB_FISH_TEARS_DEPOSITED", "lobby_fish_tears_delivery"),
        ("GameRoom_Hub4", "PIED_PIPER_ABILITY", "meat_hypno_pan_creation"),
        ("GameRoom_Hub4", "MEAT_HUB_ACT_ONE_MUSIC_DONE", "meat_act_1_music_delivery"),
        ("GameRoom_Hub4", "MEAT_HUB_ACT_THREE_MUSIC_DONE", "meat_act_3_music_delivery"),
        ("GameRoom_Hub4", "MEAT_HUB_ACT_FOUR_MUSIC_DONE", "meat_act_4_music_delivery"),
        ("GameRoom_Hub4", "MEAT_HUB_ACT_TWO_BOUNCER_REQUIREMENT_DONE", "meat_cat_return"),
        ("GameRoom_Hub4", "MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE", "meat_scruffy_return"),
        ("GameRoom_Hub4", "MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED", "meat_mouse_revolution"),
        ("GameRoom_Hub5B", "PRISON_HUB_BEES_ESCAPED", "cell_super_nectar_delivery"),
        ("GameRoom_Hub3", "MADNESS_HUB_DARKNESS_AREA_COMPLETED", "tower_minim_eye_restoration"),
        ("GameRoom_Hub3", "MADNESS_HUB_COMPLEXITY_AREA_COMPLETED", "tower_minim_mind_restoration"),
        ("GameRoom_Hub3", "MADNESS_HUB_LONELINESS_AREA_COMPLETED", "tower_minim_heart_restoration"),
        ("GameRoom_Hub3", "OVERALL_PROGRESS_HELPED_SCARED_MINION", "tower_totem_completion"),
        ("GameRoom_Hub7", "KING_CORRIDOR_STAR_EATER_FED", "royal_star_eater_feed"),
    };
    var policy = new QuestActionDiagnosticPolicy();
    foreach (var (room, token, candidate) in fixtures)
        Check(policy.Observe("event", room, token, false, true)?.Contains(candidate) == true, candidate);
}
static void RejectNoise()
{
    var p = new QuestActionDiagnosticPolicy();
    foreach (string room in new[] { "", "GameRoom_Hub40", "GameRoom_Hub6", "GameRoom_27", "GameRoom_Hub7" })
        Check(p.Observe("event", room, "MEAT_HUB_ACT_ONE_MUSIC_DONE", false, true) == null, room);
    for (int i = 0; i < 1000; i++) Check(p.Observe("event", "GameRoom_Hub4", "Unrelated" + i) == null, "noise");
    Check(p.Observe("event", "GameRoom_Hub4", "MEAT_HUB_ACT_ONE_MUSIC_DONE") != null, "noise used budget");
    Check(p.Observe("unknown-channel", "GameRoom_Hub4", "MEAT_HUB_ACT_ONE_MUSIC_DONE") == null, "channel");
    Check(p.Observe("event", "GameRoom_Hub4", "MEAT_HUB_ACT_ONE_MUSIC_DONE\nsecret") == null, "multiline token");
    Check(p.Observe("event", "GameRoom_Hub4", new string('x', 5000)) == null, "oversized input");
}
static void TransitionDedup()
{
    var p = new QuestActionDiagnosticPolicy();
    Check(p.Observe("event", "GameRoom_09", "LEVEL_09_COMBO_ABILITY_EARNED", false, true) != null, "first");
    for (int i = 0; i < 1000; i++) Check(p.Observe("event", "GameRoom_09", "LEVEL_09_COMBO_ABILITY_EARNED", false, true) == null, "duplicate");
    string reset = p.Observe("event", "GameRoom_09", "LEVEL_09_COMBO_ABILITY_EARNED", true, false)!;
    Check(reset.Contains("before=True after=False"), "transition lost");
    Check(p.Observe("event", "GameRoom_09", "LEVEL_09_COMBO_ABILITY_EARNED", null, null)!.Contains("before=? after=?"), "unknown conflated with false");
}
static void BoundFlood()
{
    var p = new QuestActionDiagnosticPolicy(); int emitted = 0;
    for (int i = 0; i < 1000; i++) if (p.Observe("event", "GameRoom_Hub4", "MEAT_HUB_ACT_ONE_MUSIC_DONE_" + i) != null) emitted++;
    Check(emitted == 64, "expected 64 bounded observations, got " + emitted);
    Check(p.Observe("request", "GameRoom_Hub4", "MEAT_HUB_ACT_ONE_MUSIC_DONE") != null, "event flood starved requests");
}
static void SeparateChannels()
{
    var p = new QuestActionDiagnosticPolicy();
    foreach (string channel in new[] { "request", "event", "snapshot" })
        Check(p.Observe(channel, "GameRoom_Hub4", "MEAT_HUB_ACT_ONE_MUSIC_DONE", null, true) != null, channel);
}
static void SceneScope()
{
    var p = new QuestActionDiagnosticPolicy();
    Check(p.ObserveScene("GameRoom_Hub4", "Root/GameRoom_Hub7_Logic/StarEater", "", true, true) == null, "foreign room");
    string path = "Root/GameRoom_Hub4_Logic/Objects/Scruffy";
    Check(p.ObserveScene("GameRoom_Hub4", path, "UnityEngine.Component", true, false)?.Contains("activeInHierarchy=False") == true, "inactive object lost");
    Check(p.ObserveScene("GameRoom_Hub4", path, "UnityEngine.Component", true, false) == null, "scene duplicate");
    Check(p.ObserveScene("GameRoom_Hub4", path, "UnityEngine.Component", true, true) != null, "activation change lost");
    Check(p.ObserveScene("GameRoom_Hub4", "Root/GameRoom_Hub4_Logic/Objects/LevelEntranceDoor_03", "LevelEntranceDoor", true, true) != null, "gate candidate");
}
static void SnapshotOnce()
{
    var p = new QuestActionDiagnosticPolicy();
    Check(p.BeginSnapshot("GameRoom_Hub40").Count == 0, "room prefix leak");
    Check(p.BeginSnapshot("GameRoom_09").SequenceEqual(new[] { "LEVEL_09_COMBO_ABILITY_EARNED" }), "wrong conversion snapshot");
    Check(p.BeginSnapshot("GameRoom_09").Count == 0, "repeat snapshot");
}
static void DispatchFlags()
{
    DeveloperHarness.CurrentRoomId = "GameRoom_Hub4";
    int start = Plugin.LoggerInstance.Messages.Count;
    QuestActionDiscovery.RecordProgressionFlagUpdated(new FlagEvent { FlagWasSet = true, FlagIsSet = false }, "MEAT_HUB_ACT_THREE_MUSIC_DONE");
    QuestActionDiscovery.RecordProgressionFlagUpdated(new object(), "MEAT_HUB_ACT_THREE_MUSIC_DONE");
    QuestActionDiscovery.RecordProgressionFlagUpdated(new object(), "MEAT_HUB_ACT_THREE_MUSIC_DONE");
    var rows = Plugin.LoggerInstance.Messages.Skip(start).ToArray();
    Check(rows.Length == 2 && rows[0].Contains("before=True after=False") && rows[1].Contains("before=? after=?"), "event payload mapping");
}
static void DispatchRequest()
{
    DeveloperHarness.CurrentRoomId = "GameRoom_Hub4";
    QuestActionDiscovery.RecordProgressionRequest(new FlagRequest { Value = true }, "MEAT_HUB_ACT_FOUR_MUSIC_DONE");
    Check(Plugin.LoggerInstance.Messages.Last().Contains("channel=request") && Plugin.LoggerInstance.Messages.Last().Contains("before=? after=True"), "request misrepresented");
}
static void DispatchResult()
{
    DeveloperHarness.CurrentRoomId = "GameRoom_09";
    int start = Plugin.LoggerInstance.Messages.Count;
    QuestActionDiscovery.RecordResult("Level_09", "LevelVariant_Default", true);
    QuestActionDiscovery.RecordResult("Level_09", "LevelVariant_Default", true);
    QuestActionDiscovery.RecordResult("Level_09", "LevelVariant_BeeMode", false);
    DeveloperHarness.CurrentRoomId = "GameRoom_27";
    QuestActionDiscovery.RecordResult("Level_09", "LevelVariant_Default", true);
    var rows = Plugin.LoggerInstance.Messages.Skip(start).ToArray();
    Check(rows.Length == 2 && rows[0].Contains("Level_09|LevelVariant_Default") && rows[1].Contains("LevelVariant_BeeMode"), "result identity/dedup");
}
static void DispatchSnapshot()
{
    DeveloperHarness.CurrentRoomId = "GameRoom_09";
    QuestActionDiscovery.SnapshotKnownFlags();
    QuestActionDiscovery.SnapshotKnownFlags();
    Check(RootsBucketRandomization.Reads.SequenceEqual(new[] { "LEVEL_09_COMBO_ABILITY_EARNED" }), "unbounded or wrong reads");
    Check(Plugin.LoggerInstance.Messages.Last().Contains("channel=snapshot") && Plugin.LoggerInstance.Messages.Last().Contains("after=?"), "unreadable flag fabricated");
}

namespace RhythmCastleAP
{
    // Only external inputs/sinks are substituted; the production dispatch is extracted and executed.
    internal static class Plugin { internal static TestLogger LoggerInstance { get; } = new(); }
    internal sealed class TestLogger { internal List<string> Messages { get; } = new(); internal void LogInfo(object message) => Messages.Add(message.ToString()!); }
    internal static class DeveloperHarness { internal static string CurrentRoomId { get; set; } = ""; }
    internal static class ReflectionUtil { internal static bool? ReadBool(object obj, string name) => obj.GetType().GetProperty(name)?.GetValue(obj) as bool?; }
    internal static class RootsBucketRandomization
    {
        internal static List<string> Reads { get; } = new();
        internal static bool TryReadProgressionFlag(string flag, out bool value) { Reads.Add(flag); value = false; return false; }
    }
    internal sealed class FlagEvent { public bool FlagWasSet { get; init; } public bool FlagIsSet { get; init; } }
    internal sealed class FlagRequest { public bool Value { get; init; } }
}
