namespace Payments.Domain;

/// <summary>A gateway capable of authorizing payments.</summary>
public interface IPaymentGateway
{
    /// <summary>Authorizes a payment for the given order.</summary>
    /// <param name="order">The order to authorize.</param>
    Task<PaymentResult> AuthorizeAsync(Order order);
}
