namespace Payments.Domain;

/// <summary>An order placed by a customer.</summary>
public class Order
{
    /// <summary>The order identifier.</summary>
    public int Id { get; set; }

    /// <summary>The total amount to be authorized.</summary>
    public decimal Total { get; set; }
}
