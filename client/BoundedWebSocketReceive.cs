using System.Net.WebSockets;
using System.Text;

namespace RhythmCastleAP;

internal static class BoundedWebSocketReceive
{
    internal const int MaximumMessageBytes = 32 * 1024 * 1024;
    internal static readonly TimeSpan FragmentDeadline = TimeSpan.FromSeconds(60);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    // ReceiveAsync returns decompressed bytes, so this also bounds compressed
    // messages. The deadline starts after the first fragment, not while idle.
    internal static async Task<string> ReadAsync(WebSocket socket, byte[] buffer,
        Action closed, int maximumBytes = MaximumMessageBytes, TimeSpan? fragmentDeadline = null)
    {
        using var deadline = new CancellationTokenSource();
        using var message = new MemoryStream(Math.Min(buffer.Length, maximumBytes));
        bool fragmented = false;
        try
        {
            while (true)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), deadline.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // Do not wait indefinitely for a peer that never completes a close handshake.
                    using var closeDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    try { await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, closeDeadline.Token).ConfigureAwait(false); }
                    finally { socket.Abort(); closed(); }
                    return string.Empty; // discard any partial message
                }
                if (result.MessageType != WebSocketMessageType.Text)
                    throw new InvalidDataException("Archipelago sent a non-text WebSocket message.");
                if (result.Count > maximumBytes - message.Length)
                    throw new InvalidDataException("Archipelago message exceeded the receive-size limit; connection closed.");
                message.Write(buffer, 0, result.Count);
                if (result.EndOfMessage)
                    return StrictUtf8.GetString(message.GetBuffer(), 0, checked((int)message.Length));
                if (!fragmented)
                {
                    fragmented = true;
                    deadline.CancelAfter(fragmentDeadline ?? FragmentDeadline);
                }
            }
        }
        catch
        {
            socket.Abort(); // stop the dependency polling loop before propagating the failure
            throw;
        }
    }
}
