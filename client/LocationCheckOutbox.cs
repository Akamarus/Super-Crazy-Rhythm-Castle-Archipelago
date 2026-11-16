using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RhythmCastleAP;

// Keep locally earned checks across process restarts. Each login compares these
// with the server's checked set, which also recovers a room restored from backup.
internal sealed class LocationCheckOutbox(string directory)
{
    private sealed record Record(int Schema, string Identity, string[] Earned);

    private string FileFor(string identity) => Path.Combine(directory,
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))) + ".json");

    internal string[] Load(string identity)
    {
        string path = FileFor(identity);
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            var record = JsonSerializer.Deserialize<Record>(file);
            if (record is not { Schema: 1 } || record.Identity != identity || record.Earned == null ||
                record.Earned.Any(string.IsNullOrWhiteSpace) ||
                record.Earned.Distinct(StringComparer.OrdinalIgnoreCase).Count() != record.Earned.Length)
                throw new InvalidDataException("Invalid check history or wrong room.");
            return record.Earned;
        }
        catch (FileNotFoundException) { return Array.Empty<string>(); }
        catch (DirectoryNotFoundException) when (!File.Exists(directory)) { return Array.Empty<string>(); }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            throw new InvalidDataException($"Cannot read check-outbox/{Path.GetFileName(path)}. Check access or restore this room's file.", ex);
        }
    }

    internal void Save(string identity, IEnumerable<string> earned)
    {
        JournalPublication.Write(FileFor(identity), new Record(1, identity, earned.ToArray()));
    }
}
