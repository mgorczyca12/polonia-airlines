namespace Shared.Integration.Contracts;

/// <summary>Seating released the workflow's capacity and durably fenced it.</summary>
public sealed record ReservationInventoryReleased(Guid CorrelationId) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
