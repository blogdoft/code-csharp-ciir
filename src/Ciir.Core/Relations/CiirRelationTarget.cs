namespace Ciir.Core.Relations;

/// <summary>The symbol a <see cref="CiirRelation"/> points to.</summary>
public sealed record CiirRelationTarget
{
    /// <summary>
    /// The target's CIIR document id, populated whenever <see cref="CiirRelationResolution.Status"/>
    /// is <see cref="CiirResolutionStatus.Resolved"/> (whether <see cref="CiirRelationResolution.Origin"/>
    /// is <see cref="CiirResolutionOrigin.Project"/> or <see cref="CiirResolutionOrigin.Solution"/>) and
    /// the target symbol's kind is one this pipeline emits its own document for.
    /// <see langword="null"/> for external symbols, and for resolved symbols of a kind that never
    /// gets its own CIIR document (e.g. a record's positional property, an indexer, or a local variable).
    /// </summary>
    public string? Id { get; init; }

    /// <summary>The target's fully qualified symbol name.</summary>
    public required string Symbol { get; init; }
}
