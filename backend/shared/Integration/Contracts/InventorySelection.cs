namespace Shared.Integration.Contracts;

/// <summary>One passenger's requested capacity on a flight cabin, with an optional physical seat.</summary>
public sealed record InventorySelection(
    string PassengerId,
    string FlightId,
    string CabinId,
    string? SeatId);
