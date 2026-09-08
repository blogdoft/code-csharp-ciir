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

/// <summary>Builds the <see cref="CiirDocument"/> for an event declaration.</summary>
internal static class EventDocumentBuilder
{
    public static CiirDocument? Build(IEventSymbol eventSymbol, string projectName, string projectDirectory, AnalysisOptions options)
    {
        if (eventSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not SyntaxNode declarationNode)
        {
            return null;
        }

        var modifiers = declarationNode switch
        {
            EventDeclarationSyntax eventDeclaration => eventDeclaration.Modifiers,
            _ => declarationNode.Ancestors().OfType<EventFieldDeclarationSyntax>().FirstOrDefault()?.Modifiers ?? default,
        };

        var qualifiedName = SymbolNaming.QualifiedName(eventSymbol);

        var document = new CiirDocument
        {
            Id = CiirIdentity.ComputeId("csharp", projectName, CiirKind.Event, qualifiedName),
            Kind = CiirKind.Event,
            Language = "csharp",
            Project = projectName,
            Symbol = new CiirSymbol
            {
                Name = eventSymbol.Name,
                QualifiedName = qualifiedName,
                CanonicalName = qualifiedName,
                Container = SymbolNaming.Container(eventSymbol),
            },
            Source = SourceLocationFactory.Create(declarationNode, projectDirectory, options.IncludeSource),
            Documentation = XmlDocCommentExtractor.Extract(eventSymbol),
            Comments = TriviaCommentExtractor.Extract(declarationNode),
            Event = new CiirEventInfo
            {
                Accessibility = AccessibilityMapper.Map(eventSymbol.DeclaredAccessibility),
                Modifiers = ModifierMapper.Map(modifiers),
                Type = eventSymbol.Type is null ? string.Empty : SymbolNaming.TypeName(eventSymbol.Type),
            },
        };

        return EmbeddingTextAttacher.Attach(document);
    }
}
