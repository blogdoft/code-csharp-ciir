namespace Ciir.Application.Model;

/// <summary>Operational summary of an analysis run (not CIIR data). Maps to <c>analysis-report.json</c>.</summary>
public sealed record AnalysisReport
{
    /// <summary>Whether the run completed without a fatal failure.</summary>
    public required bool Success { get; init; }

    /// <summary>Project-level counts.</summary>
    public required AnalysisProjectStats Projects { get; init; }

    /// <summary>Document-level counts.</summary>
    public required AnalysisDocumentStats Documents { get; init; }

    /// <summary>Relation resolution counts.</summary>
    public required AnalysisRelationStats Relations { get; init; }

    /// <summary>Per-kind entity counts, reused to populate <c>manifest.json</c>'s statistics.</summary>
    public required AnalysisEntityCounts Entities { get; init; }

    /// <summary>Errors encountered during the run.</summary>
    public IReadOnlyList<AnalysisError> Errors { get; init; } = [];
}
