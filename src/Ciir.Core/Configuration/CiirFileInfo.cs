namespace Ciir.Core.Configuration;

/// <summary>The <c>file</c>-specific details of a CIIR document whose <c>kind</c> is <see cref="CiirKind.File"/>: metadata for a file captured without structural parsing.</summary>
public sealed record CiirFileInfo
{
    /// <summary>The file's size in bytes.</summary>
    public required long SizeBytes { get; init; }
}
