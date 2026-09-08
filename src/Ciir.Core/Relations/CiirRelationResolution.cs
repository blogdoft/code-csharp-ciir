namespace Ciir.Core.Relations;

/// <summary>Describes how confidently and from where a relation's target was resolved.</summary>
public sealed record CiirRelationResolution
{
    /// <summary>Whether the target was statically resolved.</summary>
    public required CiirResolutionStatus Status { get; init; }

    /// <summary>Where the target originates from.</summary>
    public required CiirResolutionOrigin Origin { get; init; }

    /// <summary>A human-readable explanation, populated for unresolved/ambiguous/dynamic relations.</summary>
    public string? Reason { get; init; }
}
