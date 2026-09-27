namespace Ciir.Indexer.Client;

/// <summary>Builds the <see cref="HttpClient"/> used to talk to the indexer.</summary>
public static class IndexerHttpClientFactory
{
    /// <summary>Creates the client.</summary>
    /// <param name="timeout">Bounds each request, including the (potentially large) upload.</param>
    /// <param name="insecure">When <see langword="true"/>, the server's TLS certificate is not validated (self-signed, private CA, hostname mismatch).</param>
    public static HttpClient Create(TimeSpan timeout, bool insecure)
    {
        var handler = new HttpClientHandler();
        if (insecure)
        {
            // Deliberate: this is the user's explicit --insecure opt-in (the CLI warns about it on stderr).
#pragma warning disable MA0039
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#pragma warning restore MA0039
        }

        return new HttpClient(handler, disposeHandler: true) { Timeout = timeout };
    }
}
