namespace Ciir.Application.Model;

/// <summary>Aggregate statistics recorded in the manifest.</summary>
public sealed record AnalysisManifestStatistics
{
    /// <summary>Number of projects analyzed.</summary>
    public required int Projects { get; init; }

    /// <summary>Number of source files analyzed.</summary>
    public required int FilesAnalyzed { get; init; }

    /// <summary>Number of <c>type</c> documents.</summary>
    public required int Types { get; init; }

    /// <summary>Number of <c>method</c>/<c>constructor</c> documents.</summary>
    public required int Methods { get; init; }

    /// <summary>Number of <c>configuration_key</c> documents.</summary>
    public required int ConfigurationKeys { get; init; }

    /// <summary>Number of <c>file</c> documents.</summary>
    public required int Files { get; init; }

    /// <summary>Total number of relations extracted.</summary>
    public required int Relations { get; init; }

    /// <summary>Number of relations that could not be confidently resolved.</summary>
    public required int UnresolvedRelations { get; init; }
}
