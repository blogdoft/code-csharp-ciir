namespace Ciir.Application.Model;

/// <summary>The input to the main analysis use case, independent of how it was triggered (CLI, API, ...).</summary>
public sealed record AnalyzeInputCommand
{
    /// <summary>The raw <c>&lt;path&gt;</c> argument: a solution, project, or directory to analyze.</summary>
    public required string Path { get; init; }

    /// <summary>Options controlling the run.</summary>
    public required AnalysisOptions Options { get; init; }
}
