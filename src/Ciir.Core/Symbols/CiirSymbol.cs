namespace Ciir.Core.Symbols;

/// <summary>Identity information for the software entity a CIIR document represents.</summary>
public sealed record CiirSymbol
{
    /// <summary>The simple name (e.g. <c>AuthorizeAsync</c>).</summary>
    public required string Name { get; init; }

    /// <summary>The fully qualified, human-readable name (e.g. <c>Payments.Application.PaymentService.AuthorizeAsync</c>).</summary>
    public required string QualifiedName { get; init; }

    /// <summary>
    /// The unambiguous identity of the symbol, distinguishing overloads (e.g. including parameter
    /// types for methods). Used as the canonical key input for <see cref="Identity.CiirIdentity"/>.
    /// </summary>
    public required string CanonicalName { get; init; }

    /// <summary>The qualified name of the semantically owning entity, when applicable.</summary>
    public string? Container { get; init; }
}
