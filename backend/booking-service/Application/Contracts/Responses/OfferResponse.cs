namespace booking_service.Application.Contracts.Responses;

public sealed record OfferResponse(
    Guid Id,
    DateTimeOffset ExpiresAt,
    string Currency,
    decimal TotalAmount,
    FareSnapshot[] Fares);
