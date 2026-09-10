namespace RhythmCastleAP;

// The adapter's only game-side dependency is the logging sink.
internal static class Plugin
{
    internal static TestLogger LoggerInstance { get; } = new();
}

internal sealed class TestLogger
{
    internal List<string> Messages { get; } = new();
    internal void LogInfo(object message) => Messages.Add(message.ToString()!);
}

// Game-side inputs and native-score observation for the extracted real postfix.
internal static class DeveloperHarness
{
    internal static bool Enabled { get; set; }
    internal static string CurrentRoomId { get; set; } = "";
}

internal static class MusicLabPointOverride
{
    internal static int? OverrideScore { get; set; }
    internal static int LastNativeScore { get; private set; }
    internal static void RecordNativeScore(int score) => LastNativeScore = score;
}
