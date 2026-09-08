namespace Ciir.Core.Source;

/// <summary>The primary source evidence for a declared CIIR entity.</summary>
public sealed record CiirSourceLocation
{
    /// <summary>Path relative to the analysis root. Never an absolute machine path.</summary>
    public required string Path { get; init; }

    /// <summary>1-based start line.</summary>
    public required int StartLine { get; init; }

    /// <summary>1-based start column.</summary>
    public required int StartColumn { get; init; }

    /// <summary>1-based end line.</summary>
    public required int EndLine { get; init; }

    /// <summary>1-based end column.</summary>
    public required int EndColumn { get; init; }

    /// <summary>SHA-256 hash (<c>sha256:&lt;hex&gt;</c>) of the exact source span.</summary>
    public string? Hash { get; init; }

    /// <summary>The literal source text of the span. Only populated with <c>--include-source</c>.</summary>
    public string? Text { get; init; }
}
