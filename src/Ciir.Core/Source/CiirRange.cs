namespace Ciir.Core.Source;

/// <summary>
/// A 1-based line/column range within the same file as its owning entity's
/// <see cref="CiirSourceLocation"/>. Used for nested locations (comments, relations, conditions)
/// where repeating the file path and content hash would be redundant.
/// </summary>
public sealed record CiirRange
{
    /// <summary>1-based start line.</summary>
    public required int StartLine { get; init; }

    /// <summary>1-based start column.</summary>
    public int? StartColumn { get; init; }

    /// <summary>1-based end line.</summary>
    public required int EndLine { get; init; }

    /// <summary>1-based end column.</summary>
    public int? EndColumn { get; init; }
}
