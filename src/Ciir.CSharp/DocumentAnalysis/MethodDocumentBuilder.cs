using Ciir.Application.Model;
using Ciir.Core;
using Ciir.Core.Identity;
using Ciir.Core.Symbols;
using Ciir.CSharp.Comments;
using Ciir.CSharp.Conditions;
using Ciir.CSharp.ControlFlowMetrics;
using Ciir.CSharp.Documentation;
using Ciir.CSharp.EmbeddingText;
using Ciir.CSharp.Relations;
using Ciir.CSharp.SourceEvidence;
using Ciir.CSharp.SymbolMapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ciir.CSharp.DocumentAnalysis;

/// <summary>Builds the <see cref="CiirDocument"/> for a method or constructor declaration.</summary>
internal static class MethodDocumentBuilder
{
    public static CiirDocument? Build(
        IMethodSymbol method,
        Compilation compilation,
        string projectName,
        string projectDirectory,
        string assemblyName,
        AnalysisOptions options)
    {
        if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not BaseMethodDeclarationSyntax node)
        {
            return null;
        }

        var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
        var body = (SyntaxNode?)node.Body ?? node.ExpressionBody?.Expression;
        var kind = method.MethodKind == MethodKind.Constructor ? CiirKind.Constructor : CiirKind.Method;
        var canonicalName = SymbolNaming.CanonicalName(method);

        var document = new CiirDocument
        {
            Id = CiirIdentity.ComputeId("csharp", projectName, kind, canonicalName),
            Kind = kind,
            Language = "csharp",
            Project = projectName,
            Symbol = new CiirSymbol
            {
                Name = method.Name,
                QualifiedName = SymbolNaming.QualifiedName(method),
                CanonicalName = canonicalName,
                Container = SymbolNaming.Container(method),
            },
            Source = SourceLocationFactory.Create(node, projectDirectory, options.IncludeSource),
            Documentation = XmlDocCommentExtractor.Extract(method),
            Comments = TriviaCommentExtractor.Extract(node),
            Relations = body is null ? [] : MethodBodyRelationExtractor.Extract(body, semanticModel, assemblyName),
            Conditions = body is null ? [] : ConditionExtractor.Extract(body, semanticModel),
            ControlFlow = body is null ? null : ControlFlowMetricsCalculator.Calculate(node, body, semanticModel),
            Method = new CiirMethodInfo
            {
                Accessibility = AccessibilityMapper.Map(method.DeclaredAccessibility),
                Modifiers = ModifierMapper.Map(node.Modifiers),
                Parameters = [.. method.Parameters.Select(p => new CiirParameter { Name = p.Name, Type = SymbolNaming.TypeName(p.Type) })],
                ReturnType = method.MethodKind == MethodKind.Constructor ? null : SymbolNaming.TypeName(method.ReturnType),
                EmbeddingReturnType = method.MethodKind == MethodKind.Constructor ? null : ComputeEmbeddingReturnType(method.ReturnType),
            },
        };

        return EmbeddingTextAttacher.Attach(document);
    }

    private static string? ComputeEmbeddingReturnType(ITypeSymbol returnType)
    {
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return null;
        }

        if (returnType is INamedTypeSymbol { IsGenericType: true } named)
        {
            var openGenericName = named.ConstructedFrom.ToDisplayString();
            if (openGenericName is "System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.ValueTask<TResult>")
            {
                return SymbolNaming.TypeName(named.TypeArguments[0]);
            }
        }

        var displayName = returnType.ToDisplayString();
        if (displayName is "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask")
        {
            return null;
        }

        return SymbolNaming.TypeName(returnType);
    }
}
