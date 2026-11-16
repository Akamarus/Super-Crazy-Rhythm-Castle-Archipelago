using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace RhythmCastleAP;
internal sealed record ExpandedJournalRecord(int? Slot, long[] Completed, long[]? PendingCharacters = null);
internal sealed class ExpandedChecksJournal
{
    private readonly string _directory;
    internal ExpandedChecksJournal(string directory) { _directory = directory; }
    private string FileFor(string identity) => Path.Combine(_directory,
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))) + ".json");
    internal static void Validate(ExpandedJournalRecord record)
    {
        var pending = record.PendingCharacters ?? Array.Empty<long>();
        if (pending.Distinct().Count() != pending.Length || pending.Any(id => !ExpandedChecksPolicy.ById.TryGetValue(id, out var e) || e.Character == null) ||
            pending.Any(id => record.Completed?.Contains(id) == true) || (pending.Length > 0 && record.Slot == null))
            throw new InvalidDataException("Invalid pending native character source.");
        if (record.Completed == null || record.Slot is < 1 or > 4 ||
            (record.Completed.Length > 0 && record.Slot == null) ||
            record.Completed.Length != record.Completed.Distinct().Count() ||
            record.Completed.Any(id => !ExpandedChecksPolicy.ById.ContainsKey(id)))
            throw new InvalidDataException("Invalid expanded check journal.");
    }
    internal ExpandedJournalRecord Load(string identity)
    {
        string path = FileFor(identity);
        try {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var reader = new StreamReader(file);
            var root = JObject.Parse(reader.ReadToEnd(), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            bool current = root["Schema"]?.Type == JTokenType.Integer && (long?)root["Schema"] == 2;
            if (root.Count != (current ? 5 : 4) || root["Schema"]?.Type != JTokenType.Integer || (long?)root["Schema"] != (current ? 2 : 1) ||
                root["Identity"]?.Type != JTokenType.String || (string?)root["Identity"] != identity ||
                root["Slot"] == null || (root["Slot"]!.Type != JTokenType.Null && root["Slot"]!.Type != JTokenType.Integer) ||
                root["Completed"] is not JArray completed || completed.Any(v => v.Type != JTokenType.Integer))
                throw new InvalidDataException("Malformed expanded check journal.");
            long[] pending = Array.Empty<long>();
            if (current) {
                if (root["PendingCharacters"] is not JArray values || values.Any(v => v.Type != JTokenType.Integer))
                    throw new InvalidDataException("Malformed pending native character sources.");
                pending = values.Select(v => (long)v).ToArray();
            }
            var record = new ExpandedJournalRecord((int?)root["Slot"], completed.Select(v => (long)v).ToArray(), pending);
            Validate(record); return record;
        } catch (FileNotFoundException) { return new(null, Array.Empty<long>()); }
        catch (DirectoryNotFoundException) when (!File.Exists(_directory)) { return new(null, Array.Empty<long>()); }
        catch (Exception ex) when (ex is JsonException or OverflowException or FormatException or ArgumentException or
            InvalidDataException or IOException or UnauthorizedAccessException) {
            throw new InvalidDataException($"Cannot load expanded-checks/{Path.GetFileName(path)}. Check file access or restore this room's matching backup.", ex);
        }
    }
    internal void Save(string identity, ExpandedJournalRecord record)
    {
        Validate(record);
        JournalPublication.Write(FileFor(identity), new { Schema = 2, Identity = identity, record.Slot, record.Completed, PendingCharacters = record.PendingCharacters ?? Array.Empty<long>() });
    }
}
