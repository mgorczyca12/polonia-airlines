namespace booking_service.Application.Contracts.Responses;

/// <summary>A current flight price, not an offer or a guarantee of available inventory.</summary>
public sealed record FlightFareResponse(
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
