using Ciir.Core.Source;

namespace Ciir.Core.Comments;

/// <summary>An ordinary source comment, preserved separately from formal documentation.</summary>
public sealed record CiirComment
{
    /// <summary>The comment classification.</summary>
    public required CiirCommentKind Kind { get; init; }

    /// <summary>The comment text, with comment markers stripped.</summary>
    public required string Text { get; init; }

    /// <summary>The comment's location within the owning entity's source file.</summary>
    public required CiirRange Location { get; init; }
}
