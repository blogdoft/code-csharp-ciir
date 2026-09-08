namespace Ciir.Core.Documentation;

/// <summary>
/// The formal documentation comment format a symbol's documentation was extracted from. Only
/// <see cref="XmlDoc"/> is produced by the C# v1 generator; the remaining values are reserved for
/// future language generators.
/// </summary>
public enum CiirDocumentationFormat
{
    /// <summary>Format could not be determined.</summary>
    Unknown,

    /// <summary>C# XML documentation comments (<c>///</c>).</summary>
    XmlDoc,

    /// <summary>Java Javadoc.</summary>
    Javadoc,

    /// <summary>JavaScript JSDoc.</summary>
    Jsdoc,

    /// <summary>TypeScript TSDoc.</summary>
    Tsdoc,

    /// <summary>Python docstrings.</summary>
    Docstring,

    /// <summary>Free-form Markdown documentation.</summary>
    Markdown,

    /// <summary>Plain-text documentation.</summary>
    Plain,
}
