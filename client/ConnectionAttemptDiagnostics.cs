namespace RhythmCastleAP;

internal sealed class ConnectionAttemptDiagnostics
{
    private int _reported;

    internal void ReportFirstError(Exception exception, string message, Action<string> report)
    {
        if (Interlocked.Exchange(ref _reported, 1) == 0)
            report(Summarize(exception, message));
    }

    internal static string Summarize(Exception exception, string message)
    {
        Exception detail = exception is AggregateException aggregate
            ? aggregate.Flatten().InnerExceptions.FirstOrDefault()?.GetBaseException() ?? exception
            : exception.GetBaseException();
        string text = $"{detail.GetType().Name}: {detail.Message}";
        if (string.IsNullOrWhiteSpace(detail.Message))
            text = $"{detail.GetType().Name}: {message}";
        text = text.Replace('\r', ' ').Replace('\n', ' ');
        return text.Length <= 320 ? text : text.Substring(0, 317) + "...";
    }

    internal static async Task ObserveDisconnectAsync(Func<Task> disconnect, Action<string> report)
    {
        try
        {
            await disconnect().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            report(Summarize(exception, "disconnect failed"));
        }
    }
}
