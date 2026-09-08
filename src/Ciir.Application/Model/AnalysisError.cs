namespace Ciir.Application.Model;

/// <summary>A single error encountered while analyzing a project.</summary>
public sealed record AnalysisError
{
    /// <summary>The project the error occurred in.</summary>
    public required string Project { get; init; }

    /// <summary>A human-readable description of the error.</summary>
    public required string Message { get; init; }

    /// <summary>A short machine-readable category, e.g. <c>"project_load"</c>.</summary>
    public required string Category { get; init; }
}
