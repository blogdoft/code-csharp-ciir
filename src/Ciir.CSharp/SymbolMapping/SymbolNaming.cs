using Microsoft.CodeAnalysis;

namespace Ciir.CSharp.SymbolMapping;

/// <summary>Builds the deterministic qualified/canonical name strings used across the CIIR symbol model.</summary>
internal static class SymbolNaming
{
    private static readonly SymbolDisplayFormat QualifiedFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

    private static readonly SymbolDisplayFormat CanonicalMethodFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType);

    private static readonly SymbolDisplayFormat TypeNameFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    /// <summary>The fully qualified, human-readable name of <paramref name="symbol"/>.</summary>
    /// <param name="symbol">The symbol to name.</param>
    public static string QualifiedName(ISymbol symbol) => symbol.ToDisplayString(QualifiedFormat);

    /// <summary>
    /// The overload-disambiguating canonical name of <paramref name="symbol"/> (includes parameter types).
    /// </summary>
    /// <param name="symbol">The method or constructor symbol to name.</param>
    public static string CanonicalName(IMethodSymbol symbol) => symbol.ToDisplayString(CanonicalMethodFormat);

    /// <summary>The canonical name of a non-method symbol, which is simply its qualified name.</summary>
    /// <param name="symbol">The symbol to name.</param>
    public static string CanonicalName(ISymbol symbol) => QualifiedName(symbol);

    /// <summary>The fully qualified name of a type, as used for parameter/return/field/property types.</summary>
    /// <param name="type">The type to name.</param>
    public static string TypeName(ITypeSymbol type) => type.ToDisplayString(TypeNameFormat);

    /// <summary>The qualified name of the semantically owning entity, when applicable.</summary>
    /// <param name="symbol">The symbol whose container is requested.</param>
    public static string? Container(ISymbol symbol)
    {
        if (symbol.ContainingType is { } containingType)
        {
            return TypeName(containingType);
        }

        if (symbol.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace)
        {
            return containingNamespace.ToDisplayString();
        }

        return null;
    }
}
