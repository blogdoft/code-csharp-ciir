namespace Ciir.Core.Symbols;

/// <summary>The <c>type</c>-specific details of a CIIR document whose <c>kind</c> is <see cref="CiirKind.Type"/>.</summary>
public sealed record CiirTypeInfo
{
    /// <summary>The declaration shape (class, interface, struct, record, enum, delegate).</summary>
    public required CiirTypeKind TypeKind { get; init; }

    /// <summary>The declared accessibility.</summary>
    public required CiirAccessibility Accessibility { get; init; }

    /// <summary>Modifiers in canonical order (see <see cref="CiirModifierOrder"/>).</summary>
    public IReadOnlyList<CiirModifier> Modifiers { get; init; } = [];

    /// <summary>Generic type parameter names, in declaration order.</summary>
    public IReadOnlyList<string> GenericParameters { get; init; } = [];
}
