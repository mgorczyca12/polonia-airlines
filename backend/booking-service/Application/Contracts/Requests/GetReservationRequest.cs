namespace booking_service.Application.Contracts.Requests;

public sealed record GetReservationRequest(int ReservationId, string GuestToken);
