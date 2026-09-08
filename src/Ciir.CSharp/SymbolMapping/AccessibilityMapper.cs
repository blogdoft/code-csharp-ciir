using Ciir.Core.Symbols;
using Microsoft.CodeAnalysis;

namespace Ciir.CSharp.SymbolMapping;

/// <summary>Maps Roslyn's <see cref="Accessibility"/> to <see cref="CiirAccessibility"/>.</summary>
internal static class AccessibilityMapper
{
    public static CiirAccessibility Map(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Private => CiirAccessibility.Private,
        Accessibility.ProtectedAndInternal => CiirAccessibility.PrivateProtected,
        Accessibility.Protected => CiirAccessibility.Protected,
        Accessibility.Internal => CiirAccessibility.Internal,
        Accessibility.ProtectedOrInternal => CiirAccessibility.ProtectedInternal,
        Accessibility.Public => CiirAccessibility.Public,
        _ => CiirAccessibility.Unknown,
    };
}
