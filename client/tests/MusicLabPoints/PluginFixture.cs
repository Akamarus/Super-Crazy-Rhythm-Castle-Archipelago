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
