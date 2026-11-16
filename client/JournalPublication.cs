using System.Text.Json;

namespace RhythmCastleAP;

internal static class JournalPublication
{
    internal static void Write<T>(string path, T record)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string staging = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, record);
                file.Flush(true);
            }
            if (File.Exists(path)) File.Replace(staging, path, path + ".bak");
            else File.Move(staging, path);
        }
        finally { if (File.Exists(staging)) File.Delete(staging); }
    }
}
