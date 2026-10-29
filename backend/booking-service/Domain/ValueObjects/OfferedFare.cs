namespace booking_service.Domain.ValueObjects;

/// <summary>
/// The exact priced fare an offer promises one passenger on one flight. The physical cabin identifier is an
/// opaque reference used for inventory and is separate from the product's cabin class.
/// </summary>
public sealed record OfferedFare
{
    public int PassengerIndex { get; private set; }
    public string FlightId { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public string ProductCode { get; private set; } = null!;
    public int ProductVersion { get; private set; }
    public string CabinId { get; private set; } = null!;
    public string CabinClass { get; private set; } = null!;
    public string Brand { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public BaggageAllowance BaggageAllowance { get; private set; } = null!;

    private OfferedFare()
    {
    }

    public static OfferedFare Create(
        int passengerIndex,
        string flightId,
        Guid productId,
        string productCode,
        int productVersion,
        string cabinId,
        string cabinClass,
        string brand,
        decimal amount,
        string currency,
        BaggageAllowance baggageAllowance)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(passengerIndex);
        ArgumentException.ThrowIfNullOrWhiteSpace(flightId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(cabinId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cabinClass);
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentNullException.ThrowIfNull(baggageAllowance);

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product is required.", nameof(productId));
        }

        return new OfferedFare
        {
            PassengerIndex = passengerIndex,
            FlightId = flightId,
            ProductId = productId,
            ProductCode = productCode,
            ProductVersion = productVersion,
            CabinId = cabinId,
            CabinClass = cabinClass,
            Brand = brand,
            Amount = amount,
            Currency = CurrencyCode.Normalize(currency),
            BaggageAllowance = baggageAllowance
        };
    }

    public FareQuote ToQuote()
    {
        return FareQuote.Create(
            FlightId,
            ProductCode,
            CabinClass,
            Amount,
            Currency,
            BaggageAllowance,
            CabinId,
            ProductVersion,
            Brand);
    }
}
