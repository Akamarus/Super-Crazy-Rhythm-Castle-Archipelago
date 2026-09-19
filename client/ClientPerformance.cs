using System.Diagnostics;

namespace RhythmCastleAP;

// Opt-in main-thread measurements; fixed call-site labels, no per-frame log writes.
internal static class ClientPerformance
{
    internal sealed class Sample { internal long Count, Ticks, Max; }
    private static readonly Dictionary<string, Sample> Samples = new(StringComparer.Ordinal);
    private static Action<string>? _report;
    private static bool _enabled;
    private static string _room = "";
    private static double _seconds, _maxFrame;
    private static int _frames, _slowFrames;
    internal static void Configure(bool enabled, Action<string> report)
    { _enabled = enabled; _report = report; _room = ""; Reset(); }
    internal static Scope Measure(string label)
    {
        if (!_enabled) return default;
        if (!Samples.TryGetValue(label, out var sample)) Samples.Add(label, sample = new());
        return new(sample);
    }
    internal readonly struct Scope : IDisposable
    {
        private readonly Sample? _sample;
        private readonly long _start;
        internal Scope(Sample sample) { _sample = sample; _start = Stopwatch.GetTimestamp(); }
        public void Dispose()
        {
            if (_sample == null) return;
            long ticks = Stopwatch.GetTimestamp() - _start;
            _sample.Count++; _sample.Ticks += ticks; _sample.Max = Math.Max(_sample.Max, ticks);
        }
    }
    internal static void Frame(double seconds, string room)
    {
        if (!_enabled) return;
        if (_room != room) {
            if (_seconds >= 5) Report();
            Reset(); _room = room;
            return; // Scene transition intervals are not steady gameplay frames.
        }
        if (seconds <= 0 || !double.IsFinite(seconds)) return;
        _seconds += seconds; _frames++; _maxFrame = Math.Max(_maxFrame, seconds);
        if (seconds > 1.0 / 30) _slowFrames++;
        if (_seconds >= 30) { Report(); Reset(); }
    }
    private static void Report()
    {
        double ms = 1000.0 / Stopwatch.Frequency;
        string work = string.Join("; ", Samples.Where(p => p.Value.Count > 0).OrderByDescending(p => p.Value.Ticks)
            .Select(p => $"{p.Key}:calls={p.Value.Count},totalMs={p.Value.Ticks*ms:F1},maxMs={p.Value.Max*ms:F2}"));
        _report?.Invoke($"[SCRC-AP] PERFORMANCE room='{_room}' frames={_frames} meanFrameMs={_seconds*1000/Math.Max(1,_frames):F2} maxFrameMs={_maxFrame*1000:F2} over33ms={_slowFrames}; {work}");
    }
    private static void Reset()
    {
        _seconds = _maxFrame = 0; _frames = _slowFrames = 0;
        foreach (var sample in Samples.Values) sample.Count = sample.Ticks = sample.Max = 0;
    }
}
