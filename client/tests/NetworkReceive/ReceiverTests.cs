using System.Net.WebSockets;
using System.Text;
using RhythmCastleAP;

internal static class ReceiverTests
{
    internal static async Task Run()
    {
        await Success(new[] { "abc", "def" }, 6, "abcdef", "exact limit");
        await Success(new[] { "", "" }, 1, "", "empty fragments");
        using (var split = new ScriptedSocket(new byte[][] { new byte[] { 0xc3 }, new byte[] { 0xa9 } }))
            Equal("é", await BoundedWebSocketReceive.ReadAsync(split, new byte[16], () => { }), "split UTF8");
        using (var huge = new ScriptedSocket(new[] { Encoding.UTF8.GetBytes("abcd"), Encoding.UTF8.GetBytes("efg") }))
        {
            await Throws<InvalidDataException>(() => BoundedWebSocketReceive.ReadAsync(huge, new byte[16], () => { }, 6));
            Equal(WebSocketState.Aborted, huge.State, "oversize abort");
        }
        using (var binary = new ScriptedSocket(new[] { new byte[] { 1 } }, WebSocketMessageType.Binary))
            await Throws<InvalidDataException>(() => BoundedWebSocketReceive.ReadAsync(binary, new byte[16], () => { }));
        using (var invalid = new ScriptedSocket(new[] { new byte[] { 0xff } }))
            await Throws<DecoderFallbackException>(() => BoundedWebSocketReceive.ReadAsync(invalid, new byte[16], () => { }));
        using (var stalled = new ScriptedSocket(new[] { new byte[] { 32 }, new byte[] { 32 } }, delaySecond: true))
        {
            await Throws<OperationCanceledException>(() => BoundedWebSocketReceive.ReadAsync(stalled, new byte[16], () => { }, 16, TimeSpan.FromMilliseconds(50)));
            Equal(WebSocketState.Aborted, stalled.State, "deadline abort");
        }
        using (var idle = new ScriptedSocket(new[] { Encoding.UTF8.GetBytes("ok") }, delayFirst: true))
            Equal("ok", await BoundedWebSocketReceive.ReadAsync(idle, new byte[16], () => { }, 16, TimeSpan.FromMilliseconds(20)), "idle is not partial-message timeout");
        int closed = 0;
        using (var closing = new ScriptedSocket(new[] { new byte[] { 32 }, Array.Empty<byte>() }, closeLast: true))
        {
            Equal("", await BoundedWebSocketReceive.ReadAsync(closing, new byte[16], () => closed++), "partial close discarded");
            Equal(1, closed, "close event once");
        }
        Console.WriteLine("PASS: receiver boundaries, fragments, UTF8, binary rejection, partial timeout, idle and close.");
    }
    static async Task Success(string[] pieces, int limit, string expected, string label)
    {
        using var socket = new ScriptedSocket(pieces.Select(Encoding.UTF8.GetBytes).ToArray());
        Equal(expected, await BoundedWebSocketReceive.ReadAsync(socket, new byte[64], () => { }, limit), label);
        Equal(WebSocketState.Open, socket.State, label + " remains connected");
    }
    static void Equal<T>(T expected, T actual, string label)
    { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception(label); }
    static async Task Throws<T>(Func<Task<string>> run) where T : Exception
    { try { await run(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }

    sealed class ScriptedSocket : WebSocket
    {
        readonly byte[][] pieces;
        readonly WebSocketMessageType kind;
        readonly bool delaySecond, delayFirst, closeLast;
        int next;
        WebSocketState state = WebSocketState.Open;
        internal ScriptedSocket(byte[][] pieces, WebSocketMessageType kind = WebSocketMessageType.Text,
            bool delaySecond = false, bool delayFirst = false, bool closeLast = false)
        { this.pieces=pieces;this.kind=kind;this.delaySecond=delaySecond;this.delayFirst=delayFirst;this.closeLast=closeLast; }
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override string? SubProtocol => null;
        public override WebSocketState State => state;
        public override void Abort() => state = WebSocketState.Aborted;
        public override void Dispose() { }
        public override Task CloseAsync(WebSocketCloseStatus s, string? d, CancellationToken c) { state=WebSocketState.Closed; return Task.CompletedTask; }
        public override Task CloseOutputAsync(WebSocketCloseStatus s, string? d, CancellationToken c) => CloseAsync(s,d,c);
        public override Task SendAsync(ArraySegment<byte> b, WebSocketMessageType t, bool e, CancellationToken c) => throw new NotSupportedException();
        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken token)
        {
            if ((next==0 && delayFirst) || (next==1 && delaySecond)) await Task.Delay(150, token);
            var piece=pieces[next++];
            piece.CopyTo(buffer.Array!, buffer.Offset);
            return new WebSocketReceiveResult(piece.Length, closeLast && next==pieces.Length ? WebSocketMessageType.Close : kind, next==pieces.Length);
        }
    }
}
