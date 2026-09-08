namespace Ciir.Core.Symbols;

/// <summary>The <c>field</c>-specific details of a CIIR document whose <c>kind</c> is <see cref="CiirKind.Field"/>.</summary>
public sealed record CiirFieldInfo
{
    /// <summary>The declared accessibility.</summary>
    public required CiirAccessibility Accessibility { get; init; }

    /// <summary>Modifiers in canonical order (see <see cref="CiirModifierOrder"/>).</summary>
    public IReadOnlyList<CiirModifier> Modifiers { get; init; } = [];

    /// <summary>The fully qualified field type.</summary>
    public required string Type { get; init; }
}
