using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace RhythmCastleAP;
internal sealed record BunkerRecord(int? Slot, long[] Consumed, long[] Completed);
internal sealed class BunkerJournal(string directory)
{
    private string FileFor(string identity) => Path.Combine(directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))) + ".json");
    internal static void Validate(BunkerRecord record)
    {
        if (record.Slot is < 1 or > 4 || record.Consumed == null || record.Completed == null ||
            record.Consumed.Length != record.Consumed.Distinct().Count() || record.Completed.Length != record.Completed.Distinct().Count() ||
            record.Consumed.Any(id => !BunkerCatalog.Items.Any(i => i.Id == id)) ||
            record.Completed.Any(id => !BunkerCatalog.Checks.Any(i => i.Id == id)) ||
            (!record.Slot.HasValue && (record.Consumed.Length + record.Completed.Length > 0)))
            throw new InvalidDataException("Invalid Bunker journal");
    }
    internal BunkerRecord Load(string identity)
    {
        var path = FileFor(identity);
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var document = JsonDocument.Parse(file);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Expected a Bunker journal object.");
            bool versioned = root.TryGetProperty("Schema", out var schema);
            if (!root.TryGetProperty("Slot", out _) || !root.TryGetProperty("Consumed", out _) ||
                !root.TryGetProperty("Completed", out _) || root.EnumerateObject().Count() != (versioned ? 5 : 3) ||
                (versioned && (schema.ValueKind != JsonValueKind.Number || !schema.TryGetInt32(out int version) || version != 1 ||
                    !root.TryGetProperty("Identity", out var owner) || owner.ValueKind != JsonValueKind.String || !owner.ValueEquals(identity))))
                throw new InvalidDataException("Unexpected Bunker journal schema or identity.");
            var record = root.Deserialize<BunkerRecord>()!;
            Validate(record);
            return record;
        }
        catch (FileNotFoundException) { return new(null, Array.Empty<long>(), Array.Empty<long>()); }
        catch (DirectoryNotFoundException) when (!File.Exists(directory)) { return new(null, Array.Empty<long>(), Array.Empty<long>()); }
        catch (Exception ex) when (ex is InvalidDataException or JsonException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"Cannot load bunker/{Path.GetFileName(path)}. Check file access or restore its matching .bak before reconnecting.", ex);
        }
    }
    internal void Save(string identity, BunkerRecord record)
    {
        Validate(record);
        JournalPublication.Write(FileFor(identity), new { Schema = 1, Identity = identity, record.Slot, record.Consumed, record.Completed });
    }
}
