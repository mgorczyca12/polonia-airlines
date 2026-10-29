namespace Shared.Integration.Contracts;

/// <summary>Asks seating to convert the workflow's held capacity into confirmed allocations.</summary>
public sealed record ConfirmReservationInventory(Guid CorrelationId) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
