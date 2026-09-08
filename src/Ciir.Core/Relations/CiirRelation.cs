using Ciir.Core.Source;

namespace Ciir.Core.Relations;

/// <summary>A single statically observable relationship from a CIIR entity to a target symbol.</summary>
public sealed record CiirRelation
{
    /// <summary>The relationship kind.</summary>
    public required CiirRelationKind Kind { get; init; }

    /// <summary>The target symbol.</summary>
    public required CiirRelationTarget Target { get; init; }

    /// <summary>How the target was resolved.</summary>
    public required CiirRelationResolution Resolution { get; init; }

    /// <summary>The location of the reference within the owning entity's source, when applicable.</summary>
    public CiirRange? Location { get; init; }
}
