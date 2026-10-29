using booking_service.Domain.ValueObjects;

namespace booking_service.Domain.Entities;

public sealed class FareProduct
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public int Version { get; private set; }
    public string CabinClass { get; private set; } = null!;
    public string Brand { get; private set; } = null!;
    public bool Active { get; private set; }
    public BaggageAllowance BaggageAllowance { get; private set; } = null!;

    private FareProduct()
    {
    }

    public static FareProduct Create(
        string code,
        int version,
        string cabinClass,
        string brand,
        BaggageAllowance baggageAllowance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(cabinClass);
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentNullException.ThrowIfNull(baggageAllowance);

        return new FareProduct
        {
            Id = Guid.NewGuid(),
            Code = code,
            Version = version,
            CabinClass = cabinClass,
            Brand = brand,
            Active = true,
            BaggageAllowance = BaggageAllowance.Create(
                baggageAllowance.IncludedPieces,
                baggageAllowance.MaxWeightPerPieceKg,
                baggageAllowance.MaxExtraPieces,
                baggageAllowance.ExtraPiecePrice)
        };
    }

    public void Retire()
    {
        Active = false;
    }
}

public sealed class FlightPrice
{
    public Guid Id { get; private set; }
    public string FlightId { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public string Currency { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string CabinId { get; private set; } = null!;

    private FlightPrice()
    {
    }

    public static FlightPrice Create(string flightId, Guid productId, string currency, decimal amount, string cabinId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flightId);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(cabinId);
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product is required.", nameof(productId));
        }

        return new FlightPrice
        {
            Id = Guid.NewGuid(),
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
