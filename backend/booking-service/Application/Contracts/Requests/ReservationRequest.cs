namespace booking_service.Application.Contracts.Requests;

public sealed record PassengerRequest(string FirstName, string LastName, DateOnly DateOfBirth);

/// <summary>Turns an offer into a pending reservation; the request ID makes retries idempotent.</summary>
public sealed record ReservationRequest(
    Guid OfferId,
    string RequestId,
    string ContactName,
    string ContactEmail,
    PassengerRequest[] Passengers);
