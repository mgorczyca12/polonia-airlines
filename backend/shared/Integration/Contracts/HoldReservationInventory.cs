namespace Shared.Integration.Contracts;

/// <summary>Asks seating to hold capacity for every selection until the absolute deadline.</summary>
public sealed record HoldReservationInventory(
    Guid CorrelationId,
    string ReservationId,
    DateTimeOffset HoldUntil,
    InventorySelection[] Selections) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
