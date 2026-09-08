namespace Ciir.Application.Model;

/// <summary>A single generated output file, as recorded in the manifest.</summary>
public sealed record AnalysisManifestFile
{
    /// <summary>The file's path, relative to the output directory.</summary>
    public required string Path { get; init; }

    /// <summary>The number of JSONL records, when applicable.</summary>
    public int? Records { get; init; }

    /// <summary>The SHA-256 hash (<c>sha256:&lt;hex&gt;</c>) of the file's contents.</summary>
    public required string Sha256 { get; init; }
}
