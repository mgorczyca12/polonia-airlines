namespace Shared.Integration.Contracts;

/// <summary>Seating committed the workflow's confirmed allocations.</summary>
public sealed record ReservationInventoryConfirmed(Guid CorrelationId) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
