namespace Ciir.Application.Model;

/// <summary>The classified, normalized form of the <c>&lt;path&gt;</c> CLI argument.</summary>
public sealed record AnalysisInput
{
    /// <summary>Whether the path is a solution, a single project, or a directory to scan.</summary>
    public required AnalysisInputType Type { get; init; }

    /// <summary>The full, normalized filesystem path.</summary>
    public required string Path { get; init; }
}
