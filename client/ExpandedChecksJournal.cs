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
        if (!File.Exists(path)) return new(null, Array.Empty<long>());
        try {
            var root = JObject.Parse(File.ReadAllText(path), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
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
        } catch (Exception ex) when (ex is JsonException or OverflowException or FormatException or ArgumentException) {
            throw new InvalidDataException("Corrupt expanded check journal.", ex);
        }
    }
    internal void Save(string identity, ExpandedJournalRecord record)
    {
        Validate(record);
        Directory.CreateDirectory(_directory);
        string path = FileFor(identity), temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            var value = new { Schema = 2, Identity = identity, record.Slot, record.Completed, PendingCharacters = record.PendingCharacters ?? Array.Empty<long>() };
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
                stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            File.Move(temporary, path, true);
        } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
