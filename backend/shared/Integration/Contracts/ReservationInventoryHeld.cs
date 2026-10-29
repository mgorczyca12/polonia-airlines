namespace Shared.Integration.Contracts;

/// <summary>Seating has held every requested selection for the workflow.</summary>
public sealed record ReservationInventoryHeld(Guid CorrelationId) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
