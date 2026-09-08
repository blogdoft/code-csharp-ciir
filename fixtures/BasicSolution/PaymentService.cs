namespace Payments.Application;

using Payments.Domain;

/// <summary>Orchestrates payment authorization for orders.</summary>
public class PaymentService
{
    private readonly IPaymentGateway _gateway;

    /// <summary>Creates a new <see cref="PaymentService"/>.</summary>
    /// <param name="gateway">The payment gateway to use.</param>
    public PaymentService(IPaymentGateway gateway)
    {
        _gateway = gateway;
    }

    /// <summary>
    /// Authorizes a payment for the given order.
    /// </summary>
    /// <param name="order">Order being authorized.</param>
    /// <returns>Authorization result.</returns>
    public async Task<PaymentResult> AuthorizeAsync(Order order)
    {
        if (order.Total <= 0)
        {
            throw new InvalidOrderException();
        }

        return await _gateway.AuthorizeAsync(order);
    }
}
