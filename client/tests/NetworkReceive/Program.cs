using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using Archipelago.MultiClient.Net;

RhythmCastleAP.NetworkReceiveGuard.EnsureInstalled();

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

