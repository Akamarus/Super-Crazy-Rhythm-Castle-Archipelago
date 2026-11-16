using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using Archipelago.MultiClient.Net;

var budget = new RhythmCastleAP.ReceiveTrafficBudget();
int maximum = RhythmCastleAP.BoundedWebSocketReceive.MaximumMessageBytes;
if (budget.Reserve(maximum, 100) != 0 || budget.Reserve(maximum, 100) != 0 ||
    budget.Reserve(maximum, 100) != 2 || budget.Reserve(1, 120) != 0)
    throw new Exception("A normal burst must pass, sustained bytes must wait, and idle time must restore credit.");
var smallMessages = new RhythmCastleAP.ReceiveTrafficBudget();
for (int i = 0; i < 2 * RhythmCastleAP.ReceiveTrafficBudget.MessagesPerSecond; i++)
    if (smallMessages.Reserve(1, 0) != 0) throw new Exception("Initial message burst was throttled early.");
if (smallMessages.Reserve(1, 0) <= 0 || new RhythmCastleAP.ReceiveTrafficBudget().Reserve(1, 0) != 0)
    throw new Exception("Receipt budgets must count tiny messages and remain independent per connection.");

// Exercise the parser shipped in the plugin without a server or any game state.
var parser = ArchipelagoSessionFactory.CreateSession("ws://localhost:38281").Socket;
var parse = parser.GetType().BaseType!.GetMethod("OnMessageReceived", BindingFlags.Instance | BindingFlags.NonPublic)!;
int packets = 0;
Exception? parseError = null;
parser.PacketReceived += _ => packets++;
parser.ErrorReceived += (error, _) => parseError = error;
string SamplePacket(int levels) => "[{\"cmd\":\"PrintJSON\",\"data\":[],\"sample\":" +
    new string('[', levels - 2) + "0" + new string(']', levels - 2) + "}]";
parse.Invoke(parser, new object[] { SamplePacket(64) });
if (packets != 1 || parseError != null) throw new Exception("Expected a normal packet at the supported depth.", parseError);
parse.Invoke(parser, new object[] { SamplePacket(65) });
if (packets != 1 || parseError is not Newtonsoft.Json.JsonReaderException ||
    !parseError.Message.Contains("MaxDepth", StringComparison.OrdinalIgnoreCase))
    throw new Exception("Expected bounded parser error without packet dispatch.", parseError);
Console.WriteLine("PASS: shipped MultiClient/Newtonsoft dispatches depth 64 and rejects 65.");
RhythmCastleAP.NetworkReceiveGuard.VerifyDependencies(typeof(ArchipelagoSessionFactory).Assembly, typeof(Newtonsoft.Json.JsonReader).Assembly);
foreach (bool substituteParser in new[] { false, true })
{
    var sdk = substituteParser ? typeof(ArchipelagoSessionFactory).Assembly : typeof(Program).Assembly;
    var json = substituteParser ? typeof(Program).Assembly : typeof(Newtonsoft.Json.JsonReader).Assembly;
    try { RhythmCastleAP.NetworkReceiveGuard.VerifyDependencies(sdk, json); }
    catch (InvalidOperationException) { continue; }
    throw new Exception("Unexpected dependency module was accepted.");
}

RhythmCastleAP.NetworkReceiveGuard.EnsureInstalled();
if (args.Contains("--room-smoke"))
{
    await TransportIntegrationTests.Run();
    return;
}
if (args.Contains("--offline"))
{
    await ReceiverTests.Run();
    return;
}

var probe = new TcpListener(IPAddress.Loopback, 0);
probe.Start();
int port = ((IPEndPoint)probe.LocalEndpoint).Port;
probe.Stop();
using var listener = new HttpListener();
listener.Prefixes.Add($"http://localhost:{port}/");
listener.Start();
var accepted = listener.GetContextAsync();
var session = ArchipelagoSessionFactory.CreateSession($"ws://localhost:{port}");
var helper = session.Socket;
var type = helper.GetType().BaseType!;
using var socket = (ClientWebSocket)type.GetField("Socket", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(helper)!;
var connected = socket.ConnectAsync(new Uri($"ws://localhost:{port}"), CancellationToken.None);
var context = await accepted.WaitAsync(TimeSpan.FromSeconds(10));
using var server = (await context.AcceptWebSocketAsync(null)).WebSocket;
await connected;
var method = type.GetMethod("ReadMessageAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
var receiving = (Task<string>)method.Invoke(helper, new object[] { new byte[8192] })!;
var sending = Task.Run(async () => {
    var chunk = new byte[1024 * 1024];
    Array.Fill(chunk, (byte)' ');
    for (int i=0;i<33;i++)
        await server.SendAsync(new ArraySegment<byte>(chunk), WebSocketMessageType.Text, i==32, CancellationToken.None);
});
try {
    var message = await receiving.WaitAsync(TimeSpan.FromSeconds(20));
    throw new Exception("SECURITY REGRESSION: oversized message returned successfully (including possible truncation).");
} catch (InvalidDataException) {
    if (socket.State != WebSocketState.Aborted) throw new Exception("Oversized message did not abort transport.");
    Console.WriteLine("PASS: actual dependency reader rejects oversized message and aborts.");
} finally {
    socket.Abort(); server.Abort();
    try { await sending; } catch (WebSocketException) { }
}


await ReceiverTests.Run();
await TransportIntegrationTests.Run();

