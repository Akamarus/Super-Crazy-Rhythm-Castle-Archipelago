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
        return LogField(text);
    }

    internal static string LogField(string? value)
    {
        if (value == null) return string.Empty;
        string bounded = value.Length <= 320 ? value : value.Substring(0, 317) + "...";
        return string.Create(bounded.Length, bounded, (output, input) =>
        {
            for (int i = 0; i < output.Length; i++)
                output[i] = char.IsControl(input[i]) || input[i] is '\u2028' or '\u2029' ? ' ' : input[i];
        });
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
