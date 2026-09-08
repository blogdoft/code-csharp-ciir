namespace Ciir.Application.Model;

/// <summary>Options controlling how an analysis run behaves, independent of how it was triggered.</summary>
public sealed record AnalysisOptions
{
    /// <summary>The directory CIIR output artifacts are written to.</summary>
    public required string OutputPath { get; init; }

    /// <summary>Whether to embed the literal source text of each entity's declaration.</summary>
    public bool IncludeSource { get; init; }

    /// <summary>Whether a project analysis failure should cause a non-zero exit code.</summary>
    public bool FailOnError { get; init; }

    /// <summary>Whether to emit verbose diagnostic logging.</summary>
    public bool Verbose { get; init; }

    /// <summary>Whether to suppress progress reporting.</summary>
    public bool NoProgress { get; init; }
}
