using Ciir.Application.Model;
using Ciir.Core;
using Ciir.Core.Identity;
using Ciir.Core.Symbols;
using Ciir.CSharp.Comments;
using Ciir.CSharp.Documentation;
using Ciir.CSharp.EmbeddingText;
using Ciir.CSharp.SourceEvidence;
using Ciir.CSharp.SymbolMapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ciir.CSharp.DocumentAnalysis;

/// <summary>Builds the <see cref="CiirDocument"/> for a field (or enum member) declaration.</summary>
internal static class FieldDocumentBuilder
{
    public static CiirDocument? Build(IFieldSymbol field, string projectName, string rootDirectory, AnalysisOptions options)
    {
        if (field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not SyntaxNode declaratorNode)
        {
            return null;
        }

        var modifiersNode = declaratorNode.Ancestors().OfType<BaseFieldDeclarationSyntax>().FirstOrDefault();
        var qualifiedName = SymbolNaming.QualifiedName(field);

        var document = new CiirDocument
        {
            Id = CiirIdentity.ComputeId("csharp", projectName, CiirKind.Field, qualifiedName),
            Kind = CiirKind.Field,
            Language = "csharp",
            Project = projectName,
            Symbol = new CiirSymbol
            {
                Name = field.Name,
                QualifiedName = qualifiedName,
                CanonicalName = qualifiedName,
                Container = SymbolNaming.Container(field),
            },
            Source = SourceLocationFactory.Create(declaratorNode, rootDirectory, options.IncludeSource),
            Documentation = XmlDocCommentExtractor.Extract(field),
            Comments = TriviaCommentExtractor.Extract(declaratorNode),
            Field = new CiirFieldInfo
            {
                Accessibility = AccessibilityMapper.Map(field.DeclaredAccessibility),
                Modifiers = modifiersNode is null ? [] : ModifierMapper.Map(modifiersNode.Modifiers),
                Type = SymbolNaming.TypeName(field.Type),
            },
        };

        return EmbeddingTextAttacher.Attach(document);
    }
}
