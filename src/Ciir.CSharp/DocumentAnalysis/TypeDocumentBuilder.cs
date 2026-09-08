using Ciir.Application.Model;
using Ciir.Core;
using Ciir.Core.Identity;
using Ciir.Core.Symbols;
using Ciir.CSharp.Comments;
using Ciir.CSharp.Documentation;
using Ciir.CSharp.EmbeddingText;
using Ciir.CSharp.Relations;
using Ciir.CSharp.SourceEvidence;
using Ciir.CSharp.SymbolMapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ciir.CSharp.DocumentAnalysis;

/// <summary>Builds the <see cref="CiirDocument"/> for a <c>type</c> declaration, merging partial declarations into one entity.</summary>
internal static class TypeDocumentBuilder
{
    public static CiirDocument Build(
        INamedTypeSymbol type,
        string projectName,
        string projectDirectory,
        string assemblyName,
        AnalysisOptions options)
    {
        var qualifiedName = SymbolNaming.QualifiedName(type);
        var declaringNodes = type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<BaseTypeDeclarationSyntax>()
            .OrderBy(node => node.SyntaxTree.FilePath, StringComparer.Ordinal)
            .ThenBy(node => node.SpanStart)
            .ToArray();

        var primaryNode = declaringNodes[0];
        var sourceLocations = declaringNodes
            .Select(node => SourceLocationFactory.Create(node, projectDirectory, options.IncludeSource))
            .ToArray();

        var comments = declaringNodes.SelectMany(TriviaCommentExtractor.Extract).ToArray();

        var document = new CiirDocument
        {
            Id = CiirIdentity.ComputeId("csharp", projectName, CiirKind.Type, qualifiedName),
            Kind = CiirKind.Type,
            Language = "csharp",
            Project = projectName,
            Symbol = new CiirSymbol
            {
                Name = type.Name,
                QualifiedName = qualifiedName,
                CanonicalName = qualifiedName,
                Container = SymbolNaming.Container(type),
            },
            Source = sourceLocations[0],
            AdditionalSourceLocations = sourceLocations.Length > 1 ? sourceLocations[1..] : [],
            Documentation = XmlDocCommentExtractor.Extract(type),
            Comments = comments,
            Relations = InheritanceRelationExtractor.Extract(type, assemblyName),
            Type = new CiirTypeInfo
            {
                TypeKind = TypeKindMapper.Map(type),
                Accessibility = AccessibilityMapper.Map(type.DeclaredAccessibility),
                Modifiers = ModifierMapper.Map(primaryNode.Modifiers),
                GenericParameters = [.. type.TypeParameters.Select(parameter => parameter.Name)],
            },
        };

        return EmbeddingTextAttacher.Attach(document);
    }
}
