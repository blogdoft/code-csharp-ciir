using Ciir.Core.Relations;
using Microsoft.CodeAnalysis;

namespace Ciir.CSharp.Relations;

/// <summary>Classifies how a statically resolved (or unresolved) symbol reference maps to <see cref="CiirRelationResolution"/>.</summary>
internal static class RelationResolutionClassifier
{
    public static CiirRelationResolution Classify(SymbolInfo symbolInfo, RelationResolutionContext context)
    {
        if (symbolInfo.Symbol is { } symbol)
        {
            var origin = ClassifyOrigin(symbol, context);
            var status = origin is CiirResolutionOrigin.Project or CiirResolutionOrigin.Solution
                ? CiirResolutionStatus.Resolved
                : CiirResolutionStatus.External;
            return new CiirRelationResolution { Status = status, Origin = origin };
        }

        if (symbolInfo.CandidateReason == CandidateReason.LateBound)
        {
            return new CiirRelationResolution
            {
                Status = CiirResolutionStatus.Dynamic,
                Origin = CiirResolutionOrigin.Runtime,
                Reason = "Invocation target could not be determined statically because it is dynamically dispatched.",
            };
        }

        if (symbolInfo.CandidateSymbols.Length > 1)
        {
            return new CiirRelationResolution
            {
                Status = CiirResolutionStatus.Ambiguous,
                Origin = CiirResolutionOrigin.Unknown,
                Reason = $"{symbolInfo.CandidateSymbols.Length} candidate symbols were found and none could be selected statically.",
            };
        }

        return new CiirRelationResolution
        {
            Status = CiirResolutionStatus.Unresolved,
            Origin = CiirResolutionOrigin.Unknown,
            Reason = "The reference could not be resolved to a symbol.",
        };
    }

    public static CiirResolutionOrigin ClassifyOrigin(ISymbol symbol, RelationResolutionContext context)
    {
        var containingAssembly = symbol.ContainingAssembly?.Name;

        if (containingAssembly is null)
        {
            return CiirResolutionOrigin.Unknown;
        }

        if (string.Equals(containingAssembly, context.CurrentAssemblyName, StringComparison.Ordinal))
        {
            return CiirResolutionOrigin.Project;
        }

        if (context.TryGetProjectName(containingAssembly, out _))
        {
            return CiirResolutionOrigin.Solution;
        }

        return IsFrameworkAssembly(containingAssembly) ? CiirResolutionOrigin.Framework : CiirResolutionOrigin.Dependency;
    }

    private static bool IsFrameworkAssembly(string assemblyName) =>
        assemblyName.StartsWith("System", StringComparison.Ordinal) ||
        assemblyName.StartsWith("Microsoft.NETCore", StringComparison.Ordinal) ||
        assemblyName.StartsWith("Microsoft.CSharp", StringComparison.Ordinal) ||
        assemblyName.StartsWith("mscorlib", StringComparison.Ordinal) ||
        assemblyName.StartsWith("netstandard", StringComparison.Ordinal);
}
