namespace Ciir.Application.Model;

/// <summary>A single analyzed project, as recorded in the manifest.</summary>
public sealed record AnalysisManifestProject
{
    /// <summary>The project's logical name.</summary>
    public required string Name { get; init; }

    /// <summary>The full path to the project file.</summary>
    public required string Path { get; init; }
}
