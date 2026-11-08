using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Archipelago.MultiClient.Net;
using RhythmCastleAP;

internal static class TransportIntegrationTests
{
    internal static async Task Run()
    {
        NetworkReceiveGuard.EnsureInstalled(); // repeated connection setup is safe
        await NormalPackets();
        await NoTlsFallback();
        new HarmonyLib.Harmony("jack.rhythmcastle.archipelago.receive-security").UnpatchSelf();
        NetworkReceiveGuard.EnsureInstalled();
        await NormalPackets();
        Console.WriteLine("PASS: guard can reinstall after unpatch and real packet delivery still works.");
    }
    static async Task NormalPackets()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0); probe.Start();
        int port=((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
        using var listener=new HttpListener(); listener.Prefixes.Add($"http://localhost:{port}/"); listener.Start();
        var accept=listener.GetContextAsync();
        var session=ArchipelagoSessionFactory.CreateSession(ConnectionEndpointPolicy.Normalize($"localhost:{port}"));
        var received=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var closed=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.Socket.PacketReceived += packet => received.TrySetResult(packet.GetType().Name);
        session.Socket.SocketClosed += _ => closed.TrySetResult(true);
        var connect=session.Socket.ConnectAsync();
        var context=await accept.WaitAsync(TimeSpan.FromSeconds(5));
        using var server=(await context.AcceptWebSocketAsync(null)).WebSocket;
        await connect.WaitAsync(TimeSpan.FromSeconds(5));
        // Large valid message exercises the real parser after the guarded reader.
        byte[] message=Encoding.UTF8.GetBytes("[{\"cmd\":\"PrintJSON\",\"data\":[{\"text\":\""+new string('a', 1024*1024)+"\"}]}]");
        for(int offset=0;offset<message.Length;)
        {
            int n=Math.Min(8192,message.Length-offset);
            await server.SendAsync(new ArraySegment<byte>(message,offset,n),WebSocketMessageType.Text,offset+n==message.Length,CancellationToken.None);
            offset+=n;
        }
        if(await received.Task.WaitAsync(TimeSpan.FromSeconds(10)) != "PrintJsonPacket") throw new Exception("Real packet parser returned: " + await received.Task);
        await server.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,"test complete",CancellationToken.None);
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Console.WriteLine("PASS: actual session receives fragmented 1 MiB JSON through guard/parser and reports close.");
    }
    static async Task NoTlsFallback()
    {
        var listener=new TcpListener(IPAddress.Loopback,0); listener.Start();
        try
        {
            int port=((IPEndPoint)listener.LocalEndpoint).Port;
            var session=ArchipelagoSessionFactory.CreateSession(ConnectionEndpointPolicy.Normalize($"wss://localhost:{port}"));
            var connect=session.Socket.ConnectAsync();
            using(var peer=await listener.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(5)))
            {
                var data=new byte[1];
                if(await peer.GetStream().ReadAsync(data).AsTask().WaitAsync(TimeSpan.FromSeconds(5)) != 1 || data[0] != 22)
                    throw new Exception("Expected TLS handshake, not plaintext HTTP.");
            }
            try { await connect.WaitAsync(TimeSpan.FromSeconds(5)); throw new Exception("TLS unexpectedly succeeded."); }
            catch(System.Net.WebSockets.WebSocketException) { }
            using var timeout=new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
            try { using var second=await listener.AcceptTcpClientAsync(timeout.Token); throw new Exception("TLS failure retried another connection."); }
            catch(OperationCanceledException) { }
            Console.WriteLine("PASS: failed TLS handshake produces no plaintext retry.");
        }
        finally { listener.Stop(); }
    }
}



