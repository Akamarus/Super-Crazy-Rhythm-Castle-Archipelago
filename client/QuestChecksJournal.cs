using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace RhythmCastleAP;

internal sealed record QuestJournalRecord(int? Slot, long[] Completed, long[] Pending);
internal sealed class QuestChecksJournal
{
    private readonly string _directory;
    internal QuestChecksJournal(string directory) { _directory = directory; }
    private string FileFor(string identity) => Path.Combine(_directory,
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))) + ".json");
    internal QuestJournalRecord Load(string identity)
    {
        string path = FileFor(identity);
        if (!File.Exists(path)) return new(null, Array.Empty<long>(), Array.Empty<long>());
        var value = JsonConvert.DeserializeObject<QuestJournalRecord>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty quest source journal");
        if (value.Completed == null || value.Pending == null || value.Completed.Length > 4 || value.Pending.Length > 2 ||
            value.Completed.Any(id => !QuestChecksPolicy.Locations.ContainsValue(id)) ||
            value.Pending.Any(id => id != QuestChecksPolicy.BatteryHandIn && id != QuestChecksPolicy.MemoryHandIn) ||
            ((value.Completed.Length > 0 || value.Pending.Length > 0) && !value.Slot.HasValue))
            throw new InvalidDataException("Unknown source in quest journal");
        return value;
    }
    internal void Save(string identity, QuestJournalRecord value)
    {
        Directory.CreateDirectory(_directory);
        string path = FileFor(identity), temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonConvert.SerializeObject(value));
        File.Move(temporary, path, true);
    }
}
