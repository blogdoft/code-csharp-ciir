using Ciir.Core.Conditions;
using Ciir.Core.Source;
using Ciir.CSharp.SymbolMapping;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ciir.CSharp.Conditions;

/// <summary>Extracts conditional/branching constructs from a method-like body as static facts.</summary>
internal static class ConditionExtractor
{
    public static IReadOnlyList<CiirCondition> Extract(SyntaxNode body, SemanticModel semanticModel)
    {
        var walker = new Walker(semanticModel);
        walker.Visit(body);
        return walker.Conditions;
    }

    private sealed class Walker(SemanticModel semanticModel) : Microsoft.CodeAnalysis.CSharp.CSharpSyntaxWalker
    {
        public List<CiirCondition> Conditions { get; } = [];

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            base.VisitIfStatement(node);
            Add(ClassifyIfKind(node), node.Condition);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            base.VisitSwitchStatement(node);
            Add(CiirConditionKind.Switch, node.Expression);
        }

        public override void VisitSwitchExpression(SwitchExpressionSyntax node)
        {
            base.VisitSwitchExpression(node);
            Add(CiirConditionKind.SwitchExpression, node.GoverningExpression);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            base.VisitWhileStatement(node);
            Add(CiirConditionKind.While, node.Condition);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            base.VisitDoStatement(node);
            Add(CiirConditionKind.DoWhile, node.Condition);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            base.VisitForStatement(node);
            if (node.Condition is { } condition)
            {
                Add(CiirConditionKind.For, condition);
            }
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            base.VisitForEachStatement(node);
            Add(CiirConditionKind.Foreach, node.Expression);
        }

        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            base.VisitConditionalExpression(node);
            Add(CiirConditionKind.ConditionalExpression, node.Condition);
        }

        private static CiirConditionKind ClassifyIfKind(IfStatementSyntax node)
        {
            if (node.Parent is ElseClauseSyntax)
            {
                return CiirConditionKind.ElseIf;
            }

            return node.Else is null && IsGuardBody(node.Statement) ? CiirConditionKind.Guard : CiirConditionKind.If;
        }

        private static bool IsGuardBody(StatementSyntax statement)
        {
            // block.Statements.SingleOrDefault() would throw (not return null) for a block with
            // two or more statements, which is a common shape ("log, then throw") — a guard
            // clause is only recognized when the body is exactly one statement.
            var effective = statement is BlockSyntax block
                ? (block.Statements.Count == 1 ? block.Statements[0] : null)
                : statement;

            return effective is ReturnStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax;
        }

        private void Add(CiirConditionKind kind, SyntaxNode expression)
        {
            var lineSpan = expression.SyntaxTree.GetLineSpan(expression.Span);

            Conditions.Add(new CiirCondition
            {
                Kind = kind,
                Expression = expression.ToString(),
                Location = new CiirRange
                {
                    StartLine = lineSpan.StartLinePosition.Line + 1,
                    StartColumn = lineSpan.StartLinePosition.Character + 1,
                    EndLine = lineSpan.EndLinePosition.Line + 1,
                    EndColumn = lineSpan.EndLinePosition.Character + 1,
                },
                Reads = CollectReads(expression),
            });
        }

        private IReadOnlyList<string> CollectReads(SyntaxNode expression)
        {
            var reads = new List<string>();

            foreach (var node in expression.DescendantNodesAndSelf())
            {
                if (node is IdentifierNameSyntax identifier &&
                    identifier.Parent is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name == identifier)
                {
                    continue;
                }

                if (node is not (MemberAccessExpressionSyntax or IdentifierNameSyntax))
                {
                    continue;
                }

                var symbol = semanticModel.GetSymbolInfo(node).Symbol;
                if (symbol is IPropertySymbol or IFieldSymbol)
                {
                    reads.Add(SymbolNaming.QualifiedName(symbol));
                }
            }

            return [.. reads.Distinct(StringComparer.Ordinal)];
        }
    }
}
