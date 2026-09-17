using RhythmCastleAP;

var tests = new (string Name, Action Run)[]
{
    ("Missing discovery candidate loses a requested action", AllCandidates),
    ("Character item discovery keeps inventory, source and hand-in states distinct", CharacterItemDiscovery),
    ("Lobby plunger and Star Eater remain distinct observations", LobbyActionsRemainDistinct),
    ("Roots Star Eater feed is distinct from blockade and dialogue flags", RootsStarEaterFeedIsDistinct),
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
        ("GameRoom_Hub1A", "LOBBY_HUB_STAR_EATER_FED", "lobby_star_eater_feed"),
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
        ("GameRoom_Hub2", "ROOTS_HUB_STAR_EATER_FED", "roots_star_eater_feed"),
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
static void LobbyActionsRemainDistinct()
{
    var p = new QuestActionDiagnosticPolicy();
    var plunger = p.Observe("event", "GameRoom_Hub1A", "LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED", false, true);
    var starEater = p.Observe("event", "GameRoom_Hub1A", "LOBBY_HUB_STAR_EATER_FED", false, true);
    Check(plunger?.Contains("lobby_plunger_hand_in") == true, "missing plunger observation");
    Check(plunger?.Contains("lobby_star_eater_feed") != true, "plunger mislabeled as Star Eater");
    Check(starEater?.Contains("lobby_star_eater_feed") == true, "missing Star Eater observation");
    Check(starEater?.Contains("lobby_plunger_hand_in") != true, "Star Eater mislabeled as plunger");
    var flags = new QuestActionDiagnosticPolicy().BeginSnapshot("GameRoom_Hub1A");
    Check(flags.Contains("LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED"), "missing plunger snapshot");
    Check(flags.Contains("LOBBY_HUB_STAR_EATER_FED"), "missing Star Eater snapshot");
}
static void RootsStarEaterFeedIsDistinct()
{
    var p = new QuestActionDiagnosticPolicy();
    var feed = p.Observe("event", "GameRoom_Hub2", "ROOTS_HUB_STAR_EATER_FED", false, true);
    Check(feed?.Contains("roots_star_eater_feed") == true, "missing Roots feed observation");
    Check(p.Observe("event", "GameRoom_Hub2", "ROOTS_HUB_STAR_EATER_BLOCKADE_REMOVED", false, true)?.Contains("roots_star_eater_feed") != true, "blockade mislabeled as feeding");
    Check(p.Observe("event", "GameRoom_Hub2", "ROOTS_HUB_STAR_EATER_DIALOGUE_INTRODUCED", false, true)?.Contains("roots_star_eater_feed") != true, "dialogue mislabeled as feeding");
    Check(p.Observe("event", "GameRoom_Hub1A", "ROOTS_HUB_STAR_EATER_FED", false, true) == null, "Roots feed leaked to Lobby");
    Check(new QuestActionDiagnosticPolicy().BeginSnapshot("GameRoom_Hub2").Contains("ROOTS_HUB_STAR_EATER_FED"), "missing Roots feed snapshot");
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

static void CharacterItemDiscovery()
{
    string[] memory = { "CLEAN_HUB_MEMORY_CARD_TAKEN", "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM", "LEVEL_27_MEMORY_CARD_SCGMD_COLLECTED" };
    string[] battery = { "CLEAN_HUB_GHOST_CAT_BATTERY_REWARDED", "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM", "PLAYABLE_CHARACTER_UNLOCKED_MEOO" };
    var p = new QuestActionDiagnosticPolicy();
    foreach (string room in new[] { "GameRoom_Hub6", "GameRoom_27" })
    {
        foreach (string flag in memory)
            Check(p.Observe("event", room, flag, true, false)?.Contains("music_lab_old_game_data") == true, "missing memory event " + room + flag);
        foreach (string flag in battery)
            Check(p.Observe("event", room, flag, false, true)?.Contains("music_lab_car_battery") == true, "missing battery event " + room + flag);
        var flags = p.BeginSnapshot(room);
        Check(flags.Count == 6 && memory.Concat(battery).All(flags.Contains), "exact six metadata-backed snapshots " + room);
        Check(p.BeginSnapshot(room).Count == 0, "repeated snapshot native reads");
    }
    foreach (string room in new[] { "GameRoom_Hub1A", "GameRoom_Hub1B" })
    {
        var lobby = new QuestActionDiagnosticPolicy();
        foreach (string flag in battery)
            Check(lobby.Observe("event", room, flag, true, false)?.Contains("music_lab_car_battery") == true, "Lobby battery hand-in " + room + flag);
        var flags = lobby.BeginSnapshot(room);
        Check(battery.All(flags.Contains), "Lobby battery baseline missing");
        Check(!memory.Any(flags.Contains), "memory-card snapshot leaked into Lobby");
        Check(lobby.BeginSnapshot(room).Count == 0, "Lobby snapshot repeated");
        foreach (string flag in memory)
            Check(lobby.Observe("event", room, flag, false, true) == null, "memory-card event leaked into Lobby");
    }
    foreach (string flag in memory.Concat(battery))
        Check(p.Observe("event", "GameRoom_Hub2", flag, false, true) == null, "wrong room");
    Check(p.Observe("event", "GameRoom_Hub6", "UNRELATED_BATTERY", false, true) == null, "broad battery match");
    Check(p.Observe("event", "GameRoom_Hub6", "PLAYABLE_CHARACTER_UNLOCKED_GECKO", false, true) == null, "unrelated character");
    var consumed = new QuestActionDiagnosticPolicy().Observe("event", "GameRoom_Hub6", battery[1], true, false);
    Check(consumed?.Contains("before=True after=False") == true, "consumption retained");
    Check(consumed?.Contains("music_lab_old_game_data") == false, "item identities mixed");
    Check(consumed?.Contains("no check dispatched") == true, "read-only labeling");
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
