namespace seating_service.Domain.ValueObjects;

public sealed record HeldSelection(
    string PassengerId,
    string FlightId,
    string CabinId,
    string? SeatId);
