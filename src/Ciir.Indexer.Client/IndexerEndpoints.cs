namespace Ciir.Indexer.Client;

/// <summary>The code-ciir-indexer's routes, relative to the configured base URL (which may carry a path prefix).</summary>
internal static class IndexerEndpoints
{
    public static Uri Upload(Uri baseUrl) => new(baseUrl, "api/indexer/ciir-uploads");

    public static Uri Token(Uri baseUrl) => new(baseUrl, "api/indexer/auth/token");
}
