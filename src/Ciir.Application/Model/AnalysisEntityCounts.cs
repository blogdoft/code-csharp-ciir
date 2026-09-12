namespace Ciir.Application.Model;

/// <summary>Per-kind entity counts.</summary>
public sealed record AnalysisEntityCounts
{
    /// <summary>Number of <c>type</c> documents.</summary>
    public required int Types { get; init; }

    /// <summary>Number of <c>method</c>/<c>constructor</c> documents.</summary>
    public required int Methods { get; init; }

    /// <summary>Number of <c>configuration_key</c> documents.</summary>
    public required int ConfigurationKeys { get; init; }

    /// <summary>Number of <c>file</c> documents.</summary>
    public required int Files { get; init; }
}
