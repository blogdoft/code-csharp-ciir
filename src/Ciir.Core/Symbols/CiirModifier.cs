namespace Ciir.Core.Symbols;

/// <summary>
/// A declaration modifier preserved on a CIIR symbol when semantically relevant. Accessibility
/// (<c>public</c>/<c>private</c>/...) is modeled separately via <see cref="CiirAccessibility"/>
/// and is not part of this enum.
/// </summary>
public enum CiirModifier
{
    /// <summary><c>const</c>.</summary>
    Const,

    /// <summary><c>static</c>.</summary>
    Static,

    /// <summary><c>readonly</c>.</summary>
    Readonly,

    /// <summary><c>volatile</c>.</summary>
    Volatile,

    /// <summary><c>extern</c>.</summary>
    Extern,

    /// <summary><c>virtual</c>.</summary>
    Virtual,

    /// <summary><c>abstract</c>.</summary>
    Abstract,

    /// <summary><c>sealed</c>.</summary>
    Sealed,

    /// <summary><c>override</c>.</summary>
    Override,

    /// <summary><c>unsafe</c>.</summary>
    Unsafe,

    /// <summary><c>partial</c>.</summary>
    Partial,

    /// <summary><c>required</c>.</summary>
    Required,

    /// <summary><c>async</c>.</summary>
    Async,
}
