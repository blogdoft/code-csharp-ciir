using Ciir.Core.Relations;
using Ciir.CSharp.SymbolMapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ciir.CSharp.Relations;

/// <summary>
/// Extracts <c>calls</c>/<c>constructs</c>/<c>reads</c>/<c>writes</c>/<c>throws</c>/<c>catches</c>
/// relations from a method-like body, always resolving targets through the semantic model rather
/// than syntax text.
/// </summary>
internal static class MethodBodyRelationExtractor
{
    public static IReadOnlyList<CiirRelation> Extract(SyntaxNode body, SemanticModel semanticModel, RelationResolutionContext context)
    {
        var walker = new Walker(semanticModel, context);
        walker.Visit(body);
        return walker.Relations;
    }

    private sealed class Walker(SemanticModel semanticModel, RelationResolutionContext context) : CSharpSyntaxWalker
    {
        public List<CiirRelation> Relations { get; } = [];

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);

            var symbolInfo = semanticModel.GetSymbolInfo(node);
            if (symbolInfo.Symbol is not IMethodSymbol && symbolInfo.CandidateSymbols is not [IMethodSymbol, ..])
            {
                return;
            }

            AddRelation(
                CiirRelationKind.Calls,
                symbolInfo,
                symbol => symbol is IMethodSymbol method ? SymbolNaming.CanonicalName(method) : SymbolNaming.QualifiedName(symbol),
                node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            base.VisitObjectCreationExpression(node);
            AddConstructsRelation(node);
        }

        public override void VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
        {
            base.VisitImplicitObjectCreationExpression(node);
            AddConstructsRelation(node);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            base.VisitAssignmentExpression(node);
            TryAddMemberRelation(CiirRelationKind.Writes, node.Left);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            base.VisitPostfixUnaryExpression(node);
            if (node.IsKind(SyntaxKind.PostIncrementExpression) || node.IsKind(SyntaxKind.PostDecrementExpression))
            {
                TryAddMemberRelation(CiirRelationKind.Writes, node.Operand);
            }
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            base.VisitPrefixUnaryExpression(node);
            if (node.IsKind(SyntaxKind.PreIncrementExpression) || node.IsKind(SyntaxKind.PreDecrementExpression))
            {
                TryAddMemberRelation(CiirRelationKind.Writes, node.Operand);
            }
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            base.VisitMemberAccessExpression(node);

            if (IsWriteTarget(node))
            {
                return;
            }

            TryAddMemberRelation(CiirRelationKind.Reads, node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            base.VisitIdentifierName(node);

            if (node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node)
            {
                return;
            }

            if (IsWriteTarget(node))
            {
                return;
            }

            TryAddMemberRelation(CiirRelationKind.Reads, node);
        }

        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            base.VisitThrowStatement(node);
            if (node.Expression is { } expression)
            {
                AddThrowRelation(expression);
            }
        }

        public override void VisitThrowExpression(ThrowExpressionSyntax node)
        {
            base.VisitThrowExpression(node);
            AddThrowRelation(node.Expression);
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            base.VisitCatchClause(node);

            if (node.Declaration?.Type is not { } exceptionType)
            {
                return;
            }

            var symbol = semanticModel.GetSymbolInfo(exceptionType).Symbol as ITypeSymbol;
            if (symbol is null)
            {
                return;
            }

            var origin = RelationResolutionClassifier.ClassifyOrigin(symbol, context);
            var status = origin is CiirResolutionOrigin.Project or CiirResolutionOrigin.Solution
                ? CiirResolutionStatus.Resolved
                : CiirResolutionStatus.External;
            var id = RelationTargetIdResolver.ResolveId(symbol, status, origin, context);

            Relations.Add(new CiirRelation
            {
                Kind = CiirRelationKind.Catches,
                Target = new CiirRelationTarget { Symbol = SymbolNaming.QualifiedName(symbol), Id = id },
                Resolution = new CiirRelationResolution { Status = status, Origin = origin },
            });
        }

        private static bool IsWriteTarget(SyntaxNode node) => node.Parent switch
        {
            AssignmentExpressionSyntax assignment => assignment.Left == node,
            PostfixUnaryExpressionSyntax postfix => postfix.Operand == node,
            PrefixUnaryExpressionSyntax prefix => prefix.Operand == node,
            _ => false,
        };

        private void AddConstructsRelation(BaseObjectCreationExpressionSyntax node)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(node);
            var constructor = symbolInfo.Symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
            var constructedType = constructor?.ContainingType ?? semanticModel.GetTypeInfo(node).Type;

            if (constructedType is null)
            {
                return;
            }

            var origin = RelationResolutionClassifier.ClassifyOrigin(constructedType, context);
            var status = symbolInfo.Symbol is not null
                ? (origin is CiirResolutionOrigin.Project or CiirResolutionOrigin.Solution ? CiirResolutionStatus.Resolved : CiirResolutionStatus.External)
                : CiirResolutionStatus.Unresolved;
            var id = RelationTargetIdResolver.ResolveId(constructedType, status, origin, context);

            Relations.Add(new CiirRelation
            {
                Kind = CiirRelationKind.Constructs,
                Target = new CiirRelationTarget { Symbol = SymbolNaming.QualifiedName(constructedType), Id = id },
                Resolution = new CiirRelationResolution { Status = status, Origin = origin },
            });
        }

        private void AddThrowRelation(ExpressionSyntax expression)
        {
            var type = semanticModel.GetTypeInfo(expression).Type;
            if (type is null)
            {
                return;
            }

            var origin = RelationResolutionClassifier.ClassifyOrigin(type, context);
            var status = origin is CiirResolutionOrigin.Project or CiirResolutionOrigin.Solution
                ? CiirResolutionStatus.Resolved
                : CiirResolutionStatus.External;
            var id = RelationTargetIdResolver.ResolveId(type, status, origin, context);

            Relations.Add(new CiirRelation
            {
                Kind = CiirRelationKind.Throws,
                Target = new CiirRelationTarget { Symbol = SymbolNaming.QualifiedName(type), Id = id },
                Resolution = new CiirRelationResolution { Status = status, Origin = origin },
            });
        }

        private void TryAddMemberRelation(CiirRelationKind kind, SyntaxNode node)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(node);
            if (symbolInfo.Symbol is not (IPropertySymbol or IFieldSymbol))
            {
                return;
            }

            AddRelation(kind, symbolInfo, SymbolNaming.QualifiedName, node);
        }

        private void AddRelation(CiirRelationKind kind, SymbolInfo symbolInfo, Func<ISymbol, string> displayText, SyntaxNode node)
        {
            var resolution = RelationResolutionClassifier.Classify(symbolInfo, context);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            var symbolText = symbol is not null ? displayText(symbol) : node.ToString();
            var id = RelationTargetIdResolver.ResolveId(symbol, resolution.Status, resolution.Origin, context);

            Relations.Add(new CiirRelation
            {
                Kind = kind,
                Target = new CiirRelationTarget { Symbol = symbolText, Id = id },
                Resolution = resolution,
            });
        }
    }
}
