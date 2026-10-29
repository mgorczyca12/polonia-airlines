namespace booking_service.Application.Contracts.Requests;

public sealed record GetOfferRequest(Guid OfferId, string GuestToken);
