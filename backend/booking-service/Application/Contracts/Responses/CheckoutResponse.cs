namespace booking_service.Application.Contracts.Responses;

/// <summary>The guest's capability token; it is returned once and only its hash is stored.</summary>
public sealed record CheckoutResponse(string GuestToken);
