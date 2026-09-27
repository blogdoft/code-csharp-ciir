namespace Ciir.Application.Model;

/// <summary>Where and how to send the generated <c>ciir.jsonl</c> to the code-ciir-indexer.</summary>
public sealed record SendOptions
{
    /// <summary>The indexer's base URL (absolute http/https, with a trailing slash so relative endpoints resolve under any path prefix).</summary>
    public required Uri BaseUrl { get; init; }

    /// <summary>The id of the project, already registered in the indexer, the file is uploaded to.</summary>
    public required Guid ProjectId { get; init; }

    /// <summary>How to authenticate, or <see langword="null"/> to send without authentication.</summary>
    public IndexerCredentials? Credentials { get; init; }
}
