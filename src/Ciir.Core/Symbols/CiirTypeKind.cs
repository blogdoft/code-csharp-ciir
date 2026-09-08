namespace Ciir.Core.Symbols;

/// <summary>The kind of type declaration a <c>type</c> CIIR document represents.</summary>
public enum CiirTypeKind
{
    /// <summary>The type kind could not be determined.</summary>
    Unknown,

    /// <summary>A <c>class</c> (including <c>record class</c>; see <see cref="Record"/>).</summary>
    Class,

    /// <summary>An <c>interface</c>.</summary>
    Interface,

    /// <summary>A <c>struct</c> (including <c>record struct</c>; see <see cref="Record"/>).</summary>
    Struct,

    /// <summary>
    /// A <c>record</c> (either <c>record class</c> or <c>record struct</c>). Whether the
    /// underlying shape is a class or a struct is a C#-specific detail preserved under
    /// <c>extensions.csharp</c> rather than as a distinct universal type kind.
    /// </summary>
    Record,

    /// <summary>An <c>enum</c>.</summary>
    Enum,

    /// <summary>A <c>delegate</c>.</summary>
    Delegate,
}
