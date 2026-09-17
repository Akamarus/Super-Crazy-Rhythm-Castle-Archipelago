namespace RhythmCastleAP;
internal static class QuestChecks {
 internal static readonly QuestChecksState State = new();
 internal static readonly QuestChecksJournal Journal = new(Path.Combine(Path.GetTempPath(), "scrc-quest-session-tests-"+Guid.NewGuid()));
 internal static bool Handles(string name) => QuestChecksPolicy.Items.ContainsKey(name);
 internal static void ReplayJournal(IEnumerable<long> ids) { }
}
internal static class QuestCheckHooks { internal static bool Ready => true; }
