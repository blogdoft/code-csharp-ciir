using Ciir.Application.Model;
using Ciir.Core;
using Ciir.Core.Identity;
using Ciir.Core.Symbols;
using Ciir.CSharp.Comments;
using Ciir.CSharp.Conditions;
using Ciir.CSharp.Documentation;
using Ciir.CSharp.EmbeddingText;
using Ciir.CSharp.Relations;
using Ciir.CSharp.SourceEvidence;
using Ciir.CSharp.SymbolMapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ciir.CSharp.DocumentAnalysis;

/// <summary>Builds the <see cref="CiirDocument"/> for a property declaration.</summary>
internal static class PropertyDocumentBuilder
{
    public static CiirDocument? Build(
        IPropertySymbol property,
        Compilation compilation,
        string projectName,
        string rootDirectory,
        RelationResolutionContext context,
        AnalysisOptions options)
    {
        if (property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not PropertyDeclarationSyntax node)
        {
            return null;
        }

        var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
        var qualifiedName = SymbolNaming.QualifiedName(property);
        var body = (SyntaxNode?)node.ExpressionBody?.Expression;

        var document = new CiirDocument
        {
            Id = CiirIdentity.ComputeId("csharp", projectName, CiirKind.Property, qualifiedName),
            Kind = CiirKind.Property,
            Language = "csharp",
            Project = projectName,
            Symbol = new CiirSymbol
            {
                Name = property.Name,
                QualifiedName = qualifiedName,
                CanonicalName = qualifiedName,
                Container = SymbolNaming.Container(property),
            },
            Source = SourceLocationFactory.Create(node, rootDirectory, options.IncludeSource),
            Documentation = XmlDocCommentExtractor.Extract(property),
            Comments = TriviaCommentExtractor.Extract(node),
            Relations = body is null ? [] : MethodBodyRelationExtractor.Extract(body, semanticModel, context),
            Conditions = body is null ? [] : ConditionExtractor.Extract(body, semanticModel),
            Property = new CiirPropertyInfo
            {
                Accessibility = AccessibilityMapper.Map(property.DeclaredAccessibility),
                Modifiers = ModifierMapper.Map(node.Modifiers),
                Type = SymbolNaming.TypeName(property.Type),
                HasGetter = property.GetMethod is not null,
                HasSetter = property.SetMethod is not null,
            },
        };

        return EmbeddingTextAttacher.Attach(document);
    }
}
