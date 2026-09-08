namespace Ciir.Core.Comments;

/// <summary>The classification of an ordinary (non-formal-documentation) source comment.</summary>
public enum CiirCommentKind
{
    /// <summary>A single-line (<c>//</c>) comment with no recognized special marker.</summary>
    Line,

    /// <summary>A block (<c>/* */</c>) comment with no recognized special marker.</summary>
    Block,

    /// <summary>A comment marked as a TODO.</summary>
    Todo,

    /// <summary>A comment marked as a FIXME.</summary>
    Fixme,

    /// <summary>A comment marked as a warning.</summary>
    Warning,

    /// <summary>A comment marked as a note.</summary>
    Note,
}
