namespace Ciir.Application.Model;

/// <summary>Project discovery/analysis outcome counts.</summary>
public sealed record AnalysisProjectStats
{
    /// <summary>Number of unique projects discovered.</summary>
    public required int Discovered { get; init; }

    /// <summary>Number of projects successfully analyzed.</summary>
    public required int Analyzed { get; init; }

    /// <summary>Number of projects that failed to load or analyze.</summary>
    public required int Failed { get; init; }
}
