namespace Ciir.Core.Documentation;

/// <summary>The documented description of a single parameter.</summary>
public sealed record CiirDocumentationParameter
{
    /// <summary>The parameter name.</summary>
    public required string Name { get; init; }

    /// <summary>The documented description.</summary>
    public required string Description { get; init; }
}
