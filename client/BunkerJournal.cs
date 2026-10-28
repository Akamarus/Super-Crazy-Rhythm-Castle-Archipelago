using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
namespace RhythmCastleAP;
internal sealed record BunkerRecord(int? Slot, long[] Consumed, long[] Completed);
internal sealed class BunkerJournal(string directory)
{
    private string FileFor(string identity) => Path.Combine(directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))) + ".json");
    internal static void Validate(BunkerRecord record)
    {
        if (record.Slot is < 1 or > 4 || record.Consumed == null || record.Completed == null ||
            record.Consumed.Length > 5 || record.Completed.Length > 6 ||
            record.Consumed.Any(id => !BunkerCatalog.Items.Any(i => i.Id == id)) ||
            record.Completed.Any(id => !BunkerCatalog.Checks.Any(i => i.Id == id)) ||
            (!record.Slot.HasValue && (record.Consumed.Length + record.Completed.Length > 0)))
            throw new InvalidDataException("Invalid Bunker journal");
    }
    internal BunkerRecord Load(string identity)
    {
        var path = FileFor(identity);
        if (!File.Exists(path)) return new(null, Array.Empty<long>(), Array.Empty<long>());
        var record = JsonConvert.DeserializeObject<BunkerRecord>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty Bunker journal");
        Validate(record); return record;
    }
    internal void Save(string identity, BunkerRecord record)
    {
        Validate(record); Directory.CreateDirectory(directory);
        var path = FileFor(identity); File.WriteAllText(path + ".tmp", JsonConvert.SerializeObject(record)); File.Move(path + ".tmp", path, true);
    }
}
