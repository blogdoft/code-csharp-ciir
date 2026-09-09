using Ciir.Application.Model;
using Ciir.Core;
using Ciir.CSharp.Discovery;
using Ciir.CSharp.DocumentAnalysis;
using Ciir.CSharp.Relations;
using Ciir.CSharp.SymbolMapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ciir.CSharp.Workspace;

/// <summary>
/// Produces CIIR documents from an already-loaded <see cref="Compilation"/>. Kept separate from
/// <see cref="CSharpCodeAnalyzer"/> so the semantic analysis itself (type/member discovery,
/// relation/condition extraction) can be exercised directly against a hand-built compilation,
/// independent of how that compilation was obtained (MSBuildWorkspace, or otherwise).
/// </summary>
internal static class CompilationAnalyzer
{
    public static IEnumerable<CiirDocument> Analyze(
        Compilation compilation,
        string projectName,
        string rootDirectory,
        RelationResolutionContext context,
        AnalysisOptions options,
        CancellationToken cancellationToken)
    {
        yield return ProjectAndNamespaceDocumentBuilder.BuildProject(projectName);

        var emittedNamespaces = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in CollectTypes(compilation, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var namespaceName in NamespaceChain(type.ContainingNamespace))
            {
                if (emittedNamespaces.Add(namespaceName))
                {
                    yield return ProjectAndNamespaceDocumentBuilder.BuildNamespace(namespaceName, projectName);
                }
            }

            yield return TypeDocumentBuilder.Build(type, projectName, rootDirectory, context, options);

            foreach (var member in OrderMembers(type))
            {
                var document = BuildMemberDocument(member, compilation, projectName, rootDirectory, context, options);
                if (document is not null)
                {
                    yield return document;
                }
            }
        }
    }

    private static CiirDocument? BuildMemberDocument(
        ISymbol member,
        Compilation compilation,
        string projectName,
        string rootDirectory,
        RelationResolutionContext context,
        AnalysisOptions options) => member switch
        {
            IMethodSymbol method => MethodDocumentBuilder.Build(method, compilation, projectName, rootDirectory, context, options),
            IPropertySymbol property => PropertyDocumentBuilder.Build(property, compilation, projectName, rootDirectory, context, options),
            IFieldSymbol field => FieldDocumentBuilder.Build(field, projectName, rootDirectory, options),
            IEventSymbol eventSymbol => EventDocumentBuilder.Build(eventSymbol, projectName, rootDirectory, options),
            _ => null,
        };

    private static IEnumerable<INamedTypeSymbol> CollectTypes(Compilation compilation, CancellationToken cancellationToken)
    {
        var types = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (GeneratedCodeDetector.IsGenerated(syntaxTree))
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot(cancellationToken);

            foreach (var declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is INamedTypeSymbol symbol)
                {
                    types.Add(symbol);
                }
            }
        }

        return types.OrderBy(SymbolNaming.QualifiedName, StringComparer.Ordinal);
    }

    private static IEnumerable<string> NamespaceChain(INamespaceSymbol? containingNamespace)
    {
        if (containingNamespace is null || containingNamespace.IsGlobalNamespace)
        {
            yield break;
        }

        var format = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);
        var fullName = containingNamespace.ToDisplayString(format);
        var segments = fullName.Split('.');

        for (var i = 0; i < segments.Length; i++)
        {
            yield return string.Join('.', segments[..(i + 1)]);
        }
    }

    private static IEnumerable<ISymbol> OrderMembers(INamedTypeSymbol type) => type.GetMembers()
        .Where(IsRelevantMember)
        .OrderBy(MemberOrder)
        .ThenBy(member => member.Name, StringComparer.Ordinal)
        .ThenBy(member => member is IMethodSymbol method ? SymbolNaming.CanonicalName(method) : string.Empty, StringComparer.Ordinal);

    private static bool IsRelevantMember(ISymbol member)
    {
        if (member.IsImplicitlyDeclared)
        {
            return false;
        }

        return member switch
        {
            IMethodSymbol method => method.MethodKind is MethodKind.Ordinary or MethodKind.Constructor,
            IPropertySymbol or IFieldSymbol or IEventSymbol => true,
            _ => false,
        };
    }

    private static int MemberOrder(ISymbol member) => member switch
    {
        IMethodSymbol { MethodKind: MethodKind.Constructor } => 0,
        IMethodSymbol { MethodKind: MethodKind.Ordinary } => 1,
        IPropertySymbol => 2,
        IFieldSymbol => 3,
        IEventSymbol => 4,
        _ => 5,
    };
}
