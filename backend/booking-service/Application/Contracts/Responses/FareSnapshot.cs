namespace booking_service.Application.Contracts.Responses;

/// <summary>The exact fare promised to one passenger on one flight.</summary>
public sealed record FareSnapshot(
    int PassengerIndex,
    string FlightId,
    Guid ProductId,
    string ProductCode,
    int ProductVersion,
    string CabinId,
    string CabinClass,
    string Brand,
    decimal Amount,
    string Currency,
    BaggageSnapshot Baggage);
