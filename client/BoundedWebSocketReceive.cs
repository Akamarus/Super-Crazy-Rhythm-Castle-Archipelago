using System.Net.WebSockets;
using System.Text;
using System.Diagnostics;

namespace RhythmCastleAP;

// The pinned SDK awaits one complete message before reading again. The budget belongs
// to that poller, so waiting here applies backpressure without dropping room packets.
internal sealed class ReceiveTrafficBudget
{
    internal const int BytesPerSecond = 16 * 1024 * 1024;
    internal const int MessagesPerSecond = 2048;
    private double _byteTime = double.NegativeInfinity, _messageTime = double.NegativeInfinity;

    internal double Reserve(int bytes, double now)
    {
        _byteTime = Math.Max(_byteTime, now - 4) + (double)bytes / BytesPerSecond;
        _messageTime = Math.Max(_messageTime, now - 2) + 1.0 / MessagesPerSecond;
        return Math.Max(0, Math.Max(_byteTime, _messageTime) - now);
    }
}

internal static class BoundedWebSocketReceive
{
    internal const int MaximumMessageBytes = 32 * 1024 * 1024;
    internal static readonly TimeSpan FragmentDeadline = TimeSpan.FromSeconds(60);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    // ReceiveAsync returns decompressed bytes, so this also bounds compressed
    // messages. The deadline starts after the first fragment, not while idle.
    internal static async Task<string> ReadAsync(WebSocket socket, byte[] buffer,
        Action closed, int maximumBytes = MaximumMessageBytes, TimeSpan? fragmentDeadline = null,
        ReceiveTrafficBudget? budget = null)
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
                {
                    int length = checked((int)message.Length);
                    if (budget != null)
                    {
                        double wait = budget.Reserve(length, (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency);
                        // Fractional milliseconds round up; repeated tiny messages also pay their share.
                        if (wait > 0) await Task.Delay((int)Math.Ceiling(wait * 1000)).ConfigureAwait(false);
                    }
                    return StrictUtf8.GetString(message.GetBuffer(), 0, length);
                }
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
