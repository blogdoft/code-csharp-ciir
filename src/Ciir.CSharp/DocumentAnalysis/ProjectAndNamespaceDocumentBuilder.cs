using Ciir.Core;
using Ciir.Core.Identity;
using Ciir.Core.Symbols;
using Ciir.CSharp.EmbeddingText;

namespace Ciir.CSharp.DocumentAnalysis;

/// <summary>Builds the top-level <c>project</c> document and <c>namespace</c> documents.</summary>
internal static class ProjectAndNamespaceDocumentBuilder
{
    public static CiirDocument BuildProject(string projectName)
    {
        var document = new CiirDocument
        {
            Id = CiirIdentity.ComputeId("csharp", projectName, CiirKind.Project, projectName),
            Kind = CiirKind.Project,
            Language = "csharp",
            Project = projectName,
            Symbol = new CiirSymbol { Name = projectName, QualifiedName = projectName, CanonicalName = projectName },
        };

        return EmbeddingTextAttacher.Attach(document);
    }

    public static CiirDocument BuildNamespace(string qualifiedNamespaceName, string projectName)
    {
        var document = new CiirDocument
        {
            Id = CiirIdentity.ComputeId("csharp", projectName, CiirKind.Namespace, qualifiedNamespaceName),
            Kind = CiirKind.Namespace,
            Language = "csharp",
            Project = projectName,
            Symbol = new CiirSymbol
            {
                Name = qualifiedNamespaceName.Contains('.', StringComparison.Ordinal)
                    ? qualifiedNamespaceName[(qualifiedNamespaceName.LastIndexOf('.') + 1)..]
                    : qualifiedNamespaceName,
                QualifiedName = qualifiedNamespaceName,
                CanonicalName = qualifiedNamespaceName,
            },
        };

        return EmbeddingTextAttacher.Attach(document);
    }
}
