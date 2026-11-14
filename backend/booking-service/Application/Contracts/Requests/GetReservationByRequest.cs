namespace booking_service.Application.Contracts.Requests;

/// <summary>Recovers a reservation using its guest-scoped submission identity.</summary>
public sealed record GetReservationByRequest(string RequestId, string GuestToken);
