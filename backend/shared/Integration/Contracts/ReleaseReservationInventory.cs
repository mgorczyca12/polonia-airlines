namespace Shared.Integration.Contracts;

/// <summary>Asks seating to release any held or confirmed capacity and fence the workflow against late requests.</summary>
public sealed record ReleaseReservationInventory(Guid CorrelationId) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
