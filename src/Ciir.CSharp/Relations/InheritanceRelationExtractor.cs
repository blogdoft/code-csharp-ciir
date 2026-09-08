using Ciir.Core.Relations;
using Ciir.CSharp.SymbolMapping;
using Microsoft.CodeAnalysis;

namespace Ciir.CSharp.Relations;

/// <summary>Extracts <c>inherits</c>/<c>implements</c>/<c>overrides</c> relations for a type declaration.</summary>
internal static class InheritanceRelationExtractor
{
    public static IReadOnlyList<CiirRelation> Extract(INamedTypeSymbol type, string currentAssemblyName)
    {
        ArgumentNullException.ThrowIfNull(type);

        var relations = new List<CiirRelation>();

        if (type.TypeKind == TypeKind.Class && type.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
        {
            relations.Add(CreateRelation(CiirRelationKind.Inherits, baseType, currentAssemblyName));
        }

        foreach (var implementedInterface in type.Interfaces)
        {
            relations.Add(CreateRelation(CiirRelationKind.Implements, implementedInterface, currentAssemblyName));
        }

        foreach (var member in type.GetMembers())
        {
            if (GetOverriddenSymbol(member) is { } overridden)
            {
                relations.Add(CreateRelation(CiirRelationKind.Overrides, overridden, currentAssemblyName));
            }
        }

        return relations;
    }

    private static ISymbol? GetOverriddenSymbol(ISymbol member) => member switch
    {
        IMethodSymbol { IsOverride: true } method => method.OverriddenMethod,
        IPropertySymbol { IsOverride: true } property => property.OverriddenProperty,
        IEventSymbol { IsOverride: true } eventSymbol => eventSymbol.OverriddenEvent,
        _ => null,
    };

    private static CiirRelation CreateRelation(CiirRelationKind kind, ISymbol target, string currentAssemblyName)
    {
        var symbolText = target is IMethodSymbol method ? SymbolNaming.CanonicalName(method) : SymbolNaming.QualifiedName(target);
        var origin = RelationResolutionClassifier.ClassifyOrigin(target, currentAssemblyName);
        var status = origin == CiirResolutionOrigin.Project ? CiirResolutionStatus.Resolved : CiirResolutionStatus.External;

        return new CiirRelation
        {
            Kind = kind,
            Target = new CiirRelationTarget { Symbol = symbolText },
            Resolution = new CiirRelationResolution { Status = status, Origin = origin },
        };
    }
}
