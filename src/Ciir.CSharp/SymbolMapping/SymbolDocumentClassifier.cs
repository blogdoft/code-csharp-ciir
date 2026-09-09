using Ciir.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ciir.CSharp.SymbolMapping;

/// <summary>
/// Determines, for an arbitrary resolved <see cref="ISymbol"/>, whether this pipeline emits its
/// own <see cref="Ciir.Core.CiirDocument"/> for it — and if so, the <see cref="CiirKind"/> and the
/// exact canonical identity string (via <see cref="SymbolNaming"/>) that entity's own
/// document-emission pass would use to compute <see cref="Ciir.Core.CiirDocument.Id"/> via
/// <see cref="Ciir.Core.Identity.CiirIdentity.ComputeId"/>.
/// </summary>
/// <remarks>
/// Kept in sync by hand with the gating logic in <c>CompilationAnalyzer.CollectTypes</c>/
/// <c>IsRelevantMember</c> and each <c>*DocumentBuilder.Build</c>'s own syntax-shape check — this
/// is the single place that answers "does a document exist for this symbol" without needing that
/// symbol's own project's document-emission pass to have actually run.
/// </remarks>
internal static class SymbolDocumentClassifier
{
    public static bool TryClassify(ISymbol symbol, out CiirKind kind, out string canonicalIdentity)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var normalized = symbol.OriginalDefinition;

        switch (normalized)
        {
            case INamedTypeSymbol type when HasTypeDeclaration(type):
                kind = CiirKind.Type;
                canonicalIdentity = SymbolNaming.QualifiedName(type);
                return true;

            case IMethodSymbol { MethodKind: MethodKind.Constructor } ctor when HasMethodDeclaration(ctor):
                kind = CiirKind.Constructor;
                canonicalIdentity = SymbolNaming.CanonicalName(ctor);
                return true;

            case IMethodSymbol { MethodKind: MethodKind.Ordinary } method when HasMethodDeclaration(method):
                kind = CiirKind.Method;
                canonicalIdentity = SymbolNaming.CanonicalName(method);
                return true;

            case IPropertySymbol property when property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is PropertyDeclarationSyntax:
                kind = CiirKind.Property;
                canonicalIdentity = SymbolNaming.QualifiedName(property);
                return true;

            case IFieldSymbol { IsImplicitlyDeclared: false } field when !field.DeclaringSyntaxReferences.IsEmpty:
                kind = CiirKind.Field;
                canonicalIdentity = SymbolNaming.QualifiedName(field);
                return true;

            case IEventSymbol { IsImplicitlyDeclared: false } eventSymbol when !eventSymbol.DeclaringSyntaxReferences.IsEmpty:
                kind = CiirKind.Event;
                canonicalIdentity = SymbolNaming.QualifiedName(eventSymbol);
                return true;

            default:
                kind = default;
                canonicalIdentity = string.Empty;
                return false;
        }
    }

    private static bool HasTypeDeclaration(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<BaseTypeDeclarationSyntax>().Any();

    private static bool HasMethodDeclaration(IMethodSymbol method) =>
        method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is BaseMethodDeclarationSyntax;
}
