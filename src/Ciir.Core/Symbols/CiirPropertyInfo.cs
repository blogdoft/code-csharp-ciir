namespace Ciir.Core.Symbols;

/// <summary>The <c>property</c>-specific details of a CIIR document whose <c>kind</c> is <see cref="CiirKind.Property"/>.</summary>
public sealed record CiirPropertyInfo
{
    /// <summary>The declared accessibility.</summary>
    public required CiirAccessibility Accessibility { get; init; }

    /// <summary>Modifiers in canonical order (see <see cref="CiirModifierOrder"/>).</summary>
    public IReadOnlyList<CiirModifier> Modifiers { get; init; } = [];

    /// <summary>The fully qualified property type.</summary>
    public required string Type { get; init; }

    /// <summary>Whether the property declares a getter.</summary>
    public required bool HasGetter { get; init; }

    /// <summary>Whether the property declares a setter (including <c>init</c>).</summary>
    public required bool HasSetter { get; init; }
}
