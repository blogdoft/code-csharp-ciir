namespace Ciir.Core.Symbols;

/// <summary>
/// The <c>method</c>-specific details of a CIIR document whose <c>kind</c> is
/// <see cref="CiirKind.Method"/> or <see cref="CiirKind.Constructor"/>.
/// </summary>
public sealed record CiirMethodInfo
{
    /// <summary>The declared accessibility.</summary>
    public required CiirAccessibility Accessibility { get; init; }

    /// <summary>Modifiers in canonical order (see <see cref="CiirModifierOrder"/>).</summary>
    public IReadOnlyList<CiirModifier> Modifiers { get; init; } = [];

    /// <summary>Parameters, in declaration order.</summary>
    public IReadOnlyList<CiirParameter> Parameters { get; init; } = [];

    /// <summary>
    /// The fully qualified return type. <see langword="null"/> for constructors, which have no
    /// return type.
    /// </summary>
    public string? ReturnType { get; init; }

    /// <summary>
    /// The semantic return type used for the <c>embeddingText</c> "Returns" section (e.g. the
    /// unwrapped <c>T</c> of a <c>Task&lt;T&gt;</c> return type). Falls back to
    /// <see cref="ReturnType"/> when not supplied by the analyzer.
    /// </summary>
    public string? EmbeddingReturnType { get; init; }
}
