namespace Ciir.Core.Relations;

/// <summary>The symbol a <see cref="CiirRelation"/> points to.</summary>
public sealed record CiirRelationTarget
{
    /// <summary>
    /// The target's CIIR document id, when the target is itself represented as a CIIR document
    /// in this analysis. <see langword="null"/> for external symbols (see <see cref="CiirRelationResolution"/>).
    /// </summary>
    public string? Id { get; init; }

    /// <summary>The target's fully qualified symbol name.</summary>
    public required string Symbol { get; init; }
}
