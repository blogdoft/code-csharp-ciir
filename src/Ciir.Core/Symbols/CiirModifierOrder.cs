namespace Ciir.Core.Symbols;

/// <summary>
/// Defines the canonical, deterministic ordering for <c>modifiers</c> arrays. Language analyzers
/// must sort modifiers through <see cref="Sort"/> rather than preserving source-declaration order,
/// so that two analyses of semantically equivalent code produce byte-identical output.
/// </summary>
public static class CiirModifierOrder
{
    /// <summary>The canonical modifier order.</summary>
    public static readonly IReadOnlyList<CiirModifier> Canonical =
    [
        CiirModifier.Const,
        CiirModifier.Static,
        CiirModifier.Readonly,
        CiirModifier.Volatile,
        CiirModifier.Extern,
        CiirModifier.Virtual,
        CiirModifier.Abstract,
        CiirModifier.Sealed,
        CiirModifier.Override,
        CiirModifier.Unsafe,
        CiirModifier.Partial,
        CiirModifier.Required,
        CiirModifier.Async,
    ];

    /// <summary>Sorts <paramref name="modifiers"/> into canonical order, removing duplicates.</summary>
    /// <param name="modifiers">The modifiers to sort.</param>
    public static IReadOnlyList<CiirModifier> Sort(IEnumerable<CiirModifier> modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);

        var present = new HashSet<CiirModifier>(modifiers);
        return [.. Canonical.Where(present.Contains)];
    }
}
