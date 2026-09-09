using Ciir.Core.Identity;
using Ciir.Core.Relations;
using Ciir.CSharp.SymbolMapping;
using Microsoft.CodeAnalysis;

namespace Ciir.CSharp.Relations;

/// <summary>Computes <see cref="CiirRelationTarget.Id"/> for a classified relation target, when possible.</summary>
internal static class RelationTargetIdResolver
{
    public static string? ResolveId(ISymbol? symbol, CiirResolutionStatus status, CiirResolutionOrigin origin, RelationResolutionContext context)
    {
        if (symbol is null || status != CiirResolutionStatus.Resolved)
        {
            return null;
        }

        if (!SymbolDocumentClassifier.TryClassify(symbol, out var kind, out var canonicalIdentity))
        {
            return null;
        }

        var projectName = origin switch
        {
            CiirResolutionOrigin.Project => context.CurrentProjectName,
            CiirResolutionOrigin.Solution when context.TryGetProjectName(symbol.ContainingAssembly!.Name, out var name) => name,
            _ => null,
        };

        return projectName is null ? null : CiirIdentity.ComputeId("csharp", projectName, kind, canonicalIdentity);
    }
}
