namespace Ciir.Application.Model;

/// <summary>Relation resolution outcome counts.</summary>
public sealed record AnalysisRelationStats
{
    /// <summary>Relations resolved to a known symbol (project or external).</summary>
    public required int Resolved { get; init; }

    /// <summary>Relations that could not be confidently resolved (unresolved, ambiguous, or dynamic).</summary>
    public required int Unresolved { get; init; }
}
