using RhythmCastleAP;

(string Input, string Expected)[] cases =
{
    ("127.0.0.1:38281", "ws://127.0.0.1:38281"),
    ("127.0.0.2:38281", "ws://127.0.0.2:38281"),
    ("localhost:38281", "ws://localhost:38281"),
    ("[::1]:38281", "ws://[::1]:38281"),
    ("ws://127.0.0.1:38281", "ws://127.0.0.1:38281"),
    ("wss://127.0.0.1:38281", "wss://127.0.0.1:38281"),
    ("archipelago.gg:38281", "archipelago.gg:38281"),
    ("192.168.1.1:38281", "192.168.1.1:38281"),
    ("", ""),
};
foreach (var test in cases)
    if (ConnectionEndpointPolicy.Normalize(test.Input) != test.Expected)
        throw new Exception("Endpoint normalization failed: " + test.Input);
Console.WriteLine("Passed all loopback endpoint transport cases.");
var diagnostic = new ConnectionAttemptDiagnostics();
var reports = new System.Collections.Concurrent.ConcurrentQueue<string>();
Parallel.For(0, 40, _ => diagnostic.ReportFirstError(new IOException("offline"), "fallback", reports.Enqueue));
if (reports.Count != 1) throw new Exception("Expected one report per attempt.");
var nested = new AggregateException(new IOException("wrapper", new InvalidOperationException("root\r\nfailure")));
if (ConnectionAttemptDiagnostics.Summarize(nested, "fallback") != "InvalidOperationException: root  failure")
    throw new Exception("Expected compact underlying cause.");
if (ConnectionAttemptDiagnostics.Summarize(new Exception(new string('x', 1000)), "fallback").Length != 320)
    throw new Exception("Expected bounded error length.");
await ConnectionAttemptDiagnostics.ObserveDisconnectAsync(() => Task.FromException(new IOException("failed cleanup")), reports.Enqueue);
await ConnectionAttemptDiagnostics.ObserveDisconnectAsync(() => throw new IOException("synchronous cleanup"), reports.Enqueue);
await ConnectionAttemptDiagnostics.ObserveDisconnectAsync(() => Task.CompletedTask, reports.Enqueue);
if (reports.Count != 3) throw new Exception("Expected cleanup faults observed, success silent.");
Console.WriteLine("Passed bounded per-attempt errors and cleanup observation.");
