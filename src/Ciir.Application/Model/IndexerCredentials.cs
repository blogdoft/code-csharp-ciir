namespace Ciir.Application.Model;

/// <summary>How a run authenticates against the code-ciir-indexer. A run with no credentials sends unauthenticated.</summary>
public abstract record IndexerCredentials;
