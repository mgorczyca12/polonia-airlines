namespace booking_service.Domain.ValueObjects;

/// <summary>
/// A priced fare for one passenger on one flight, supplied by the fare catalog owner.
/// Identifiers are opaque external references and are preserved as supplied.
/// </summary>
public sealed record FareQuote
{
    public string FlightId { get; private set; } = null!;
    public string ProductCode { get; private set; } = null!;
    public string CabinClass { get; private set; } = null!;
    public string? CabinId { get; private set; }
    public int ProductVersion { get; private set; }
    public string Brand { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public BaggageAllowance BaggageAllowance { get; private set; } = null!;

    private FareQuote()
    {
    }

    public static FareQuote Create(
        string flightId,
        string productCode,
        string cabinClass,
        decimal amount,
        string currency,
        BaggageAllowance baggageAllowance,
        string? cabinId = null,
        int productVersion = 1,
        string? brand = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flightId);
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(cabinClass);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentNullException.ThrowIfNull(baggageAllowance);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productVersion);
        if (cabinId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cabinId);
        }

        return new FareQuote
        {
            FlightId = flightId,
            ProductCode = productCode,
            CabinClass = cabinClass,
            CabinId = cabinId,
            ProductVersion = productVersion,
            Brand = brand ?? productCode,
            Amount = amount,
            Currency = CurrencyCode.Normalize(currency),
            BaggageAllowance = baggageAllowance
        };
    }
}
