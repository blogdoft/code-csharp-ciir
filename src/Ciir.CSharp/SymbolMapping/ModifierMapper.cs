using Ciir.Core.Symbols;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ciir.CSharp.SymbolMapping;

/// <summary>
/// Maps a declaration's syntactic modifier tokens to <see cref="CiirModifier"/> values, sorted
/// into canonical order. Reading directly from the syntax token list (rather than symbol
/// booleans) works uniformly across every declaration kind, since they all expose a
/// <c>Modifiers</c> token list.
/// </summary>
internal static class ModifierMapper
{
    public static IReadOnlyList<CiirModifier> Map(SyntaxTokenList modifiers)
    {
        var mapped = new List<CiirModifier>();

        foreach (var token in modifiers)
        {
            var modifier = token.Kind() switch
            {
                SyntaxKind.ConstKeyword => CiirModifier.Const,
                SyntaxKind.StaticKeyword => CiirModifier.Static,
                SyntaxKind.ReadOnlyKeyword => CiirModifier.Readonly,
                SyntaxKind.VolatileKeyword => CiirModifier.Volatile,
                SyntaxKind.ExternKeyword => CiirModifier.Extern,
                SyntaxKind.VirtualKeyword => CiirModifier.Virtual,
                SyntaxKind.AbstractKeyword => CiirModifier.Abstract,
                SyntaxKind.SealedKeyword => CiirModifier.Sealed,
                SyntaxKind.OverrideKeyword => CiirModifier.Override,
                SyntaxKind.UnsafeKeyword => CiirModifier.Unsafe,
                SyntaxKind.PartialKeyword => CiirModifier.Partial,
                SyntaxKind.RequiredKeyword => CiirModifier.Required,
                SyntaxKind.AsyncKeyword => CiirModifier.Async,
                _ => (CiirModifier?)null,
            };

            if (modifier is { } value)
            {
                mapped.Add(value);
            }
        }

        return CiirModifierOrder.Sort(mapped);
    }
}
