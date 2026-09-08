namespace Ciir.Core.Symbols;

/// <summary>The declared accessibility of a symbol.</summary>
public enum CiirAccessibility
{
    /// <summary>Accessibility could not be determined or does not apply.</summary>
    Unknown,

    /// <summary><c>private</c>.</summary>
    Private,

    /// <summary><c>private protected</c>.</summary>
    PrivateProtected,

    /// <summary><c>protected</c>.</summary>
    Protected,

    /// <summary><c>internal</c>.</summary>
    Internal,

    /// <summary><c>protected internal</c>.</summary>
    ProtectedInternal,

    /// <summary><c>public</c>.</summary>
    Public,
}
