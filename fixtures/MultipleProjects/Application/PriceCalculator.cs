using MultipleProjects.Domain;

namespace MultipleProjects.Application;

/// <summary>Computes prices in the domain's <see cref="Money"/> type.</summary>
public class PriceCalculator
{
    /// <summary>Applies a discount to <paramref name="basePrice"/>.</summary>
    /// <param name="basePrice">The price before discount.</param>
    /// <param name="discountPercentage">The discount percentage (0-100).</param>
    public Money ApplyDiscount(Money basePrice, decimal discountPercentage)
    {
        var discounted = basePrice.Amount * (1 - (discountPercentage / 100));
        return new Money(discounted, basePrice.Currency);
    }
}
