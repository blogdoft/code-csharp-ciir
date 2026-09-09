using Ciir.Core.Relations;
using Ciir.CSharp.SymbolMapping;
using Microsoft.CodeAnalysis;

namespace Ciir.CSharp.Relations;

/// <summary>Extracts <c>inherits</c>/<c>implements</c>/<c>overrides</c> relations for a type declaration.</summary>
internal static class InheritanceRelationExtractor
{
    public static IReadOnlyList<CiirRelation> Extract(INamedTypeSymbol type, RelationResolutionContext context)
    {
        ArgumentNullException.ThrowIfNull(type);

        var relations = new List<CiirRelation>();

        if (type.TypeKind == TypeKind.Class && type.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
        {
            relations.Add(CreateRelation(CiirRelationKind.Inherits, baseType, context));
        }

        foreach (var implementedInterface in type.Interfaces)
        {
            relations.Add(CreateRelation(CiirRelationKind.Implements, implementedInterface, context));
        }

        foreach (var member in type.GetMembers())
        {
            if (GetOverriddenSymbol(member) is { } overridden)
            {
                relations.Add(CreateRelation(CiirRelationKind.Overrides, overridden, context));
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

    private static CiirRelation CreateRelation(CiirRelationKind kind, ISymbol target, RelationResolutionContext context)
    {
        var symbolText = target is IMethodSymbol method ? SymbolNaming.CanonicalName(method) : SymbolNaming.QualifiedName(target);
        var origin = RelationResolutionClassifier.ClassifyOrigin(target, context);
        var status = origin is CiirResolutionOrigin.Project or CiirResolutionOrigin.Solution
            ? CiirResolutionStatus.Resolved
            : CiirResolutionStatus.External;
        var id = RelationTargetIdResolver.ResolveId(target, status, origin, context);

        return new CiirRelation
        {
            Kind = kind,
            Target = new CiirRelationTarget { Symbol = symbolText, Id = id },
            Resolution = new CiirRelationResolution { Status = status, Origin = origin },
        };
    }
}
