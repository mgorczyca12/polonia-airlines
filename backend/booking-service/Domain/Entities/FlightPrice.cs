using Shared.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.Domain.Entities;

/// <summary>
/// The price of a fare product on one flight and currency. It also maps the product's cabin class to the
/// flight's physical cabin identifier used for inventory.
/// </summary>
public sealed class FlightPrice : AggregateRoot<Guid>
{
    public string FlightId { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public string Currency { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string CabinId { get; private set; } = null!;

    private FlightPrice()
    {
    }

    private FlightPrice(Guid id) : base(id)
    {
    }

    public static FlightPrice Create(
        string flightId,
        Guid productId,
        string currency,
        decimal amount,
        string cabinId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flightId);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(cabinId);

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product is required.", nameof(productId));
        }

        return new FlightPrice(Guid.NewGuid())
        {
            FlightId = flightId,
            ProductId = productId,
            Currency = CurrencyCode.Normalize(currency),
            Amount = amount,
            CabinId = cabinId
        };
    }

    public void Reprice(decimal amount, string cabinId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(cabinId);

        Amount = amount;
        CabinId = cabinId;
    }
}
