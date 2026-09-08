namespace Ciir.Core.Documentation;

/// <summary>Formal documentation extracted for a declared symbol.</summary>
public sealed record CiirDocumentation
{
    /// <summary>The documentation comment format.</summary>
    public required CiirDocumentationFormat Format { get; init; }

    /// <summary>How the documentation was obtained.</summary>
    public required CiirDocumentationSource Source { get; init; }

    /// <summary>The summary description, when present.</summary>
    public string? Summary { get; init; }

    /// <summary>Additional remarks, when present.</summary>
    public string? Remarks { get; init; }

    /// <summary>Per-parameter documented descriptions.</summary>
    public IReadOnlyList<CiirDocumentationParameter> Parameters { get; init; } = [];

    /// <summary>The documented return value description, when present.</summary>
    public string? Returns { get; init; }

    /// <summary>Documented exceptions.</summary>
    public IReadOnlyList<CiirExceptionDocumentation> Exceptions { get; init; } = [];
}
