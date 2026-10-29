namespace booking_service.Application.Contracts;

public sealed record CheckoutResponse(string GuestToken);
public sealed record ProductResponse(
    Guid Id,
    string Code,
    int Version,
    string CabinClass,
    string Brand,
    BaggageSnapshot Baggage);
public sealed record BaggageSnapshot(int IncludedPieces, decimal MaxWeightPerPieceKg, int MaxExtraPieces, decimal ExtraPiecePrice);
public sealed record OfferSelection(string FlightId, Guid ProductId, int? PassengerIndex = null);
public sealed record OfferRequest(string Currency, int PassengerCount, OfferSelection[] Selections);
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
public sealed record OfferResponse(Guid Id, DateTimeOffset ExpiresAt, string Currency, decimal TotalAmount, FareSnapshot[] Fares);
public sealed record PassengerRequest(string FirstName, string LastName, DateOnly DateOfBirth);
public sealed record ReservationRequest(
    Guid OfferId,
    string RequestId,
    string ContactName,
    string ContactEmail,
    PassengerRequest[] Passengers);
public sealed record ReservationResponse(
    int Id,
    string Code,
    string Status,
    DateTimeOffset ReservationDate,
    string Currency,
    decimal TotalAmount,
    string GuestAccessToken,
    FareSnapshot[] Fares,
    Guid WorkflowId,
    string WorkflowStatus,
    string? FailureCode);
