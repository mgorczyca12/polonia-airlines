namespace Shared.Integration.Contracts;

/// <summary>Published by booking when a pending reservation has been persisted and its workflow should start.</summary>
public sealed record ReservationRequested(
    Guid CorrelationId,
    string ReservationId,
    DateTimeOffset HoldUntil,
    InventorySelection[] Selections) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
