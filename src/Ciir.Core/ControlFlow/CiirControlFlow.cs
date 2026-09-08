namespace Ciir.Core.ControlFlow;

/// <summary>
/// Aggregate control-flow metrics for a method-like entity. CIIR v1 intentionally does not
/// serialize a full control-flow graph; this shape is designed so a future, more detailed
/// representation can be added without breaking v1 consumers.
/// </summary>
public sealed record CiirControlFlow
{
    /// <summary>The number of basic blocks in the method body.</summary>
    public required int BasicBlockCount { get; init; }

    /// <summary>The cyclomatic complexity of the method body.</summary>
    public required int CyclomaticComplexity { get; init; }

    /// <summary>Whether the method body contains any branching construct.</summary>
    public required bool HasBranches { get; init; }

    /// <summary>Whether the method body contains any loop construct.</summary>
    public required bool HasLoops { get; init; }
}
