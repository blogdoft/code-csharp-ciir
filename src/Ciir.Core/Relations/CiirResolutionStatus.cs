namespace Ciir.Core.Relations;

/// <summary>Whether a relation's target symbol could be statically resolved.</summary>
public enum CiirResolutionStatus
{
    /// <summary>The target symbol was resolved to a single, unambiguous symbol.</summary>
    Resolved,

    /// <summary>The target symbol could not be resolved.</summary>
    Unresolved,

    /// <summary>The target symbol resolved to more than one candidate.</summary>
    Ambiguous,

    /// <summary>The target symbol is outside the analyzed project (framework/dependency).</summary>
    External,

    /// <summary>The target is only determinable at runtime (e.g. dynamic dispatch).</summary>
    Dynamic,
}
