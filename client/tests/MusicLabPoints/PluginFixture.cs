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
    internal void LogWarning(object message) => Messages.Add(message.ToString()!);
}

// Game-side inputs and native-score observation for the extracted real postfix.
internal static class DeveloperHarness
{
    internal static bool Enabled { get; set; }
    internal static string CurrentRoomId { get; set; } = "";
}

internal static class ScoreFixture
{
    internal static void SetOverride(int? score) => typeof(MusicLabPointOverride)
        .GetField("_overrideScore", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.SetValue(null, score);
    internal static int LastNativeScore => (int)typeof(MusicLabPointOverride)
        .GetField("_lastNativeScore", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
    internal static int Reads { get; set; }
    public static int NativeGetter()
    {
        Reads++;
        int result = 37;
        MusicLabPointOverridePatches.GetMedalScorePostfix(ref result);
        return result;
    }
}
