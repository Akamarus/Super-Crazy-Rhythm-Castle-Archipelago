namespace RhythmCastleAP;

internal static class ConnectionEndpointPolicy
{
    // A local AP server uses plaintext WebSockets. Avoid an unnecessary TLS probe
    // when a loopback address is entered without an explicit transport.
    internal static string Normalize(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Contains("://", StringComparison.Ordinal))
            return endpoint;
        return Uri.TryCreate("ws://" + endpoint, UriKind.Absolute, out Uri? candidate) && candidate.IsLoopback
            ? "ws://" + endpoint
            : endpoint;
    }
}
