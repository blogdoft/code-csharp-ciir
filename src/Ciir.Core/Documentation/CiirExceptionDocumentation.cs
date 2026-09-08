namespace Ciir.Core.Documentation;

/// <summary>A documented exception (<c>&lt;exception&gt;</c>) that a symbol declares it may throw.</summary>
public sealed record CiirExceptionDocumentation
{
    /// <summary>The fully qualified exception type name.</summary>
    public required string Type { get; init; }

    /// <summary>The documented description, when provided.</summary>
    public string? Description { get; init; }
}
