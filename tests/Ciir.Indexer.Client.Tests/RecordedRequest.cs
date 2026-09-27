namespace Ciir.Indexer.Client.Tests;

/// <summary>One request captured by <see cref="StubHttpMessageHandler"/>.</summary>
internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Authorization, string? Body);
