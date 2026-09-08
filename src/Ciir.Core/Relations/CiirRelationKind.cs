namespace Ciir.Core.Relations;

/// <summary>
/// The kind of statically observable relationship between a CIIR entity and a target symbol.
/// Only the forward direction is ever recorded (e.g. <c>A CALLS B</c>); the inverse relation is
/// left to downstream graph construction.
/// </summary>
public enum CiirRelationKind
{
    /// <summary>The source entity structurally contains the target (e.g. a type containing a method).</summary>
    Contains,

    /// <summary>The source type inherits from the target base type.</summary>
    Inherits,

    /// <summary>The source type implements the target interface.</summary>
    Implements,

    /// <summary>The source member overrides the target virtual/abstract member.</summary>
    Overrides,

    /// <summary>The source member statically invokes the target callable.</summary>
    Calls,

    /// <summary>The source member constructs an instance of the target type.</summary>
    Constructs,

    /// <summary>The source member reads the target field/property.</summary>
    Reads,

    /// <summary>The source member writes the target field/property.</summary>
    Writes,

    /// <summary>The source member may throw the target exception type.</summary>
    Throws,

    /// <summary>The source member catches the target exception type.</summary>
    Catches,
}
