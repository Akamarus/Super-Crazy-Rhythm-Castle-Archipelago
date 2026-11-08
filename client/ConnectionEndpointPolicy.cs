namespace RhythmCastleAP;

internal static class ConnectionEndpointPolicy
{
    // Always provide a scheme: the dependency's unspecified scheme silently
    // retries plaintext after TLS failure. Explicit ws:// is the user's opt-in.
    internal static string Normalize(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) return endpoint;
        endpoint = endpoint.Trim();
        bool explicitScheme = endpoint.Contains("://", StringComparison.Ordinal);
        if (!Uri.TryCreate(explicitScheme ? endpoint : "wss://" + endpoint,
                UriKind.Absolute, out Uri? candidate) ||
            (candidate.Scheme != "ws" && candidate.Scheme != "wss") ||
            !string.IsNullOrEmpty(candidate.UserInfo) || !string.IsNullOrEmpty(candidate.Fragment))
            throw new ArgumentException("Use a server hostname and port, or an explicit ws:// or wss:// address.", nameof(endpoint));
        if (explicitScheme)
            return candidate.Scheme + endpoint.Substring(endpoint.IndexOf("://", StringComparison.Ordinal));
        return (candidate.IsLoopback ? "ws://" : "wss://") + endpoint;
    }
}
