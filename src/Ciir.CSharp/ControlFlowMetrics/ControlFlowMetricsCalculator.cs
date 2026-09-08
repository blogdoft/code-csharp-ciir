using Ciir.Core.ControlFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Ciir.CSharp.ControlFlowMetrics;

/// <summary>
/// Computes aggregate control-flow metrics for a method-like declaration using Roslyn's control
/// flow graph API. CIIR v1 never serializes the graph itself, only these summary numbers.
/// </summary>
internal static class ControlFlowMetricsCalculator
{
    public static CiirControlFlow Calculate(SyntaxNode declarationNode, SyntaxNode bodyNode, SemanticModel semanticModel)
    {
        var hasBranches = bodyNode.DescendantNodesAndSelf().Any(node =>
            node is IfStatementSyntax or SwitchStatementSyntax or SwitchExpressionSyntax or ConditionalExpressionSyntax);
        var hasLoops = bodyNode.DescendantNodesAndSelf().Any(node =>
            node is WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax or ForEachStatementSyntax);

        var (basicBlockCount, cyclomaticComplexity) = ComputeFromControlFlowGraph(declarationNode, semanticModel);

        return new CiirControlFlow
        {
            BasicBlockCount = basicBlockCount,
            CyclomaticComplexity = cyclomaticComplexity,
            HasBranches = hasBranches,
            HasLoops = hasLoops,
        };
    }

    private static (int BasicBlockCount, int CyclomaticComplexity) ComputeFromControlFlowGraph(SyntaxNode declarationNode, SemanticModel semanticModel)
    {
        var operation = semanticModel.GetOperation(declarationNode);

        var cfg = operation switch
        {
            IMethodBodyOperation methodBody => ControlFlowGraph.Create(methodBody),
            IConstructorBodyOperation constructorBody => ControlFlowGraph.Create(constructorBody),
            IBlockOperation block => ControlFlowGraph.Create(block),
            _ => null,
        };

        if (cfg is null)
        {
            return (1, 1);
        }

        var nodeCount = cfg.Blocks.Length;
        var edgeCount = 0;

        foreach (var block in cfg.Blocks)
        {
            if (block.FallThroughSuccessor?.Destination is not null)
            {
                edgeCount++;
            }

            if (block.ConditionalSuccessor?.Destination is not null)
            {
                edgeCount++;
            }
        }

        return (nodeCount, Math.Max(1, edgeCount - nodeCount + 2));
    }
}
