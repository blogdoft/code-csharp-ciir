namespace Ciir.Application.Model;

/// <summary>Document processing counts.</summary>
public sealed record AnalysisDocumentStats
{
    /// <summary>Number of CIIR documents written.</summary>
    public required int Analyzed { get; init; }

    /// <summary>Number of source files ignored (e.g. detected as generated code).</summary>
    public required int Ignored { get; init; }

    /// <summary>Number of distinct source files that contributed at least one analyzed document.</summary>
    public required int FilesAnalyzed { get; init; }
}
