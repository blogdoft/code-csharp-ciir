namespace Payments.Domain;

/// <summary>The outcome of a payment authorization attempt.</summary>
public record PaymentResult
{
    /// <summary>Whether the authorization succeeded.</summary>
    public bool Success { get; init; }
}
