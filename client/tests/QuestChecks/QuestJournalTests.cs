using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using RhythmCastleAP;

internal static class QuestJournalTests
{
    internal static void Run(string directory, Action<bool, string> check)
    {
        const string identity = "test seed|team0|slot1";
        const string legacy = """{"Slot":1,"Completed":[187256294],"Pending":[187256295]}""";
        string name = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))) + ".json";
        string path = Path.Combine(directory, name);
        var journal = new QuestChecksJournal(directory);
        check(journal.Load(identity).Slot == null, "missing journal starts unbound");
        Directory.CreateDirectory(directory);
        try
        {
            for (int slot = 1; slot <= 4; slot++)
            {
                string original = legacy.Replace("\"Slot\":1", $"\"Slot\":{slot}");
                File.WriteAllText(path, original);
                var restored = journal.Load(identity);
                check(restored.Slot == slot && restored.Completed.SequenceEqual(new long[] { 187256294 }) &&
                    restored.Pending.SequenceEqual(new long[] { 187256295 }), "legacy regular save binding and both source lists restored");
                check(File.ReadAllText(path) == original, "opening a legacy journal preserves its contents");
            }

            var record = journal.Load(identity);
            File.WriteAllText(path + ".tmp", "another writer");
            journal.Save(identity, record);
            check(File.Exists(path + ".tmp") && File.ReadAllText(path + ".tmp") == "another writer",
                "publishing must leave another writer's temporary file intact");
            var versioned = JObject.Parse(File.ReadAllText(path));
            check((int?)versioned["Schema"] == 1 && (string?)versioned["Identity"] == identity, "saved journal identifies its schema and seed");
            var current = journal.Load(identity);
            check(current.Slot == 4 && current.Completed.SequenceEqual(record.Completed) && current.Pending.SequenceEqual(record.Pending),
                "current schema retains the save, completed checks and pending hand-in");

            foreach (string invalid in new[]
            {
                "", "null", "[]", "{", legacy + " {}",
                legacy.Replace("\"Slot\":1", "\"Slot\":0"), legacy.Replace("\"Slot\":1", "\"Slot\":5"),
                legacy.Replace("\"Slot\":1", "\"Slot\":null"), legacy.Replace("\"Slot\":1", "\"Slot\":\"1\""),
                legacy.Replace("\"Slot\":1", "\"Slot\":1,\"Slot\":1"), legacy.Replace("\"Slot\":1,", ""),
                legacy.Replace("\"Slot\":1", "\"Slot\":1,\"Extra\":0"), legacy.Replace("187256294", "187256294,187256294"),
                legacy.Replace("187256295", "187256295,187256295"), legacy.Replace("187256294", "187256295"),
                legacy.Replace("187256294", "42"), legacy.Replace("187256295", "187256297"),
                legacy.Replace("187256294", "\"187256294\""), legacy.Replace("187256295", "187256295.0"),
                legacy.Replace("187256294", "9999999999999999999999"),
                legacy.Replace("[187256295]", "null"), legacy.Replace("\"Pending\"", "\"pending\""),
                legacy.Insert(1, "\"Schema\":1,"), legacy.Insert(1, "\"Identity\":\"other\","),
                legacy.Insert(1, "\"Schema\":1,\"Identity\":\"other\","),
                legacy.Insert(1, $"\"Schema\":2,\"Identity\":\"{identity}\","),
                legacy.Insert(1, $"\"Schema\":\"1\",\"Identity\":\"{identity}\",")
            })
            {
                File.WriteAllText(path, invalid);
                Exception? failure = null;
                try { journal.Load(identity); } catch (Exception ex) { failure = ex; }
                check(failure is QuestJournalLoadException && failure.Message.Contains(name) && !failure.Message.Contains(identity) &&
                    !failure.Message.Contains(directory) && failure.Message.Contains("backup", StringComparison.OrdinalIgnoreCase),
                    "invalid journal needs a filename-specific repair diagnostic for " + invalid);
                check(File.ReadAllText(path) == invalid, "failed load preserves the original");
            }

            journal.Save(identity, record);
            string previous = File.ReadAllText(path);
            try { journal.Save(identity, new(0, Array.Empty<long>(), Array.Empty<long>())); }
            catch (InvalidDataException) { }
            check(File.ReadAllText(path) == previous, "invalid replacement cannot overwrite prior progress");
            if (OperatingSystem.IsWindows())
            {
                using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    try { journal.Save(identity, new(2, Array.Empty<long>(), Array.Empty<long>())); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                }
                check(File.ReadAllText(path) == previous && Directory.GetFiles(directory).Length == 2,
                    "failed publication preserves prior progress and cleans up its staging file");
            }
            File.WriteAllText(path, """{"Slot":null,"Completed":[],"Pending":[]}""");
            var empty = journal.Load(identity);
            check(empty.Slot == null && empty.Completed.Length == 0 && empty.Pending.Length == 0, "empty unbound legacy record still loads");
            File.Delete(path);
            Directory.CreateDirectory(path);
            Exception? unreadable = null;
            try { journal.Load(identity); } catch (Exception ex) { unreadable = ex; }
            check(unreadable is QuestJournalLoadException && unreadable.Message.Contains(name) &&
                unreadable.Message.Contains("access", StringComparison.OrdinalIgnoreCase), "unreadable journal cannot start an empty session");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
