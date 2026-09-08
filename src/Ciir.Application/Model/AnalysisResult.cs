namespace Ciir.Application.Model;

/// <summary>The outcome of running the analysis use case.</summary>
public sealed record AnalysisResult
{
    /// <summary>The exit code the caller (e.g. the CLI) should surface.</summary>
    public required AnalysisExitCode ExitCode { get; init; }

    /// <summary>A human-readable error description, populated when <see cref="ExitCode"/> is not <see cref="AnalysisExitCode.Success"/>.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>The operational report, populated once the pipeline has run (even partially).</summary>
    public AnalysisReport? Report { get; init; }
}
