using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RhythmCastleAP;

internal sealed record QuestJournalRecord(int? Slot, long[] Completed, long[] Pending);

internal sealed class QuestJournalLoadException : IOException
{
    internal QuestJournalLoadException(string path, string advice, Exception cause)
        : base($"Quest journal quest-checks/{Path.GetFileName(path)} could not be loaded. {advice}", cause) { }
}

internal sealed class QuestChecksJournal
{
    private readonly string _directory;
    internal QuestChecksJournal(string directory) { _directory = directory; }
    private string FileFor(string identity) => Path.Combine(_directory,
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))) + ".json");
    internal QuestJournalRecord Load(string identity)
    {
        string path = FileFor(identity);
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var document = JsonDocument.Parse(stream);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("Slot", out _) ||
                !root.TryGetProperty("Completed", out _) || !root.TryGetProperty("Pending", out _))
                throw new InvalidDataException("Missing quest journal fields.");

            // The original writer emitted exactly Slot, Completed and Pending under the same identity hash.
            bool versioned = root.TryGetProperty("Schema", out var schema);
            if (root.EnumerateObject().Count() != (versioned ? 5 : 3) ||
                (versioned && (schema.ValueKind != JsonValueKind.Number || !schema.TryGetInt32(out int version) || version != 1 ||
                    !root.TryGetProperty("Identity", out var owner) || owner.ValueKind != JsonValueKind.String || !owner.ValueEquals(identity))))
                throw new InvalidDataException("Unexpected quest journal schema or identity.");

            var record = root.Deserialize<QuestJournalRecord>()!;
            Validate(record);
            return record;
        }
        catch (FileNotFoundException) { return new(null, Array.Empty<long>(), Array.Empty<long>()); }
        catch (DirectoryNotFoundException) when (!File.Exists(_directory)) { return new(null, Array.Empty<long>(), Array.Empty<long>()); }
        catch (Exception ex) when (ex is InvalidDataException or JsonException or InvalidOperationException)
        {
            throw new QuestJournalLoadException(path, "Back up the file, then repair it or restore a valid backup for this seed and slot before reconnecting.", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new QuestJournalLoadException(path, "Check access to the quest-checks folder and file, then reconnect.", ex);
        }
    }

    private static void Validate(QuestJournalRecord record)
    {
        if (record.Slot is < 1 or > 4 || record.Completed == null || record.Pending == null ||
            record.Completed.Length > QuestChecksPolicy.Locations.Count || record.Pending.Length > 2 ||
            (!record.Slot.HasValue && (record.Completed.Length != 0 || record.Pending.Length != 0)) ||
            record.Completed.Distinct().Count() != record.Completed.Length || record.Pending.Distinct().Count() != record.Pending.Length ||
            record.Completed.Any(id => !QuestChecksPolicy.Locations.ContainsValue(id)) ||
            record.Pending.Any(id => id != QuestChecksPolicy.BatteryHandIn && id != QuestChecksPolicy.MemoryHandIn) ||
            record.Pending.Intersect(record.Completed).Any())
            throw new InvalidDataException("Invalid quest source journal data.");
    }

    internal void Save(string identity, QuestJournalRecord value)
    {
        Validate(value);
        Directory.CreateDirectory(_directory);
        string path = FileFor(identity);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var staging = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        try
        {
            using (staging)
            {
                JsonSerializer.Serialize(staging, new { Schema = 1, Identity = identity, value.Slot, value.Completed, value.Pending });
                staging.Flush(true);
            }
            File.Move(temporary, path, true);
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
    }
}
