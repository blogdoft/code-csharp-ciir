namespace Ciir.Core.Documentation;

/// <summary>How a symbol's documentation was obtained.</summary>
public enum CiirDocumentationSource
{
    /// <summary>Origin could not be determined.</summary>
    Unknown,

    /// <summary>Documentation was explicitly declared on the symbol itself.</summary>
    Declared,
}
