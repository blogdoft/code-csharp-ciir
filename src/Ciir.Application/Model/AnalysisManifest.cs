namespace Ciir.Application.Model;

/// <summary>The content of <c>manifest.json</c>: information needed to correlate the CIIR output back to its source.</summary>
public sealed record AnalysisManifest
{
    /// <summary>The generator's version (this application's own version).</summary>
    public required string GeneratorVersion { get; init; }

    /// <summary>The kind of input that was analyzed.</summary>
    public required AnalysisInputType InputType { get; init; }

    /// <summary>The original, resolved input path.</summary>
    public required string InputPath { get; init; }

    /// <summary>When the run completed.</summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>The analyzed projects.</summary>
    public required IReadOnlyList<AnalysisManifestProject> Projects { get; init; }

    /// <summary>The generated output files, with content hashes.</summary>
    public required IReadOnlyList<AnalysisManifestFile> Files { get; init; }

    /// <summary>Aggregate statistics for the run.</summary>
    public required AnalysisManifestStatistics Statistics { get; init; }
}
