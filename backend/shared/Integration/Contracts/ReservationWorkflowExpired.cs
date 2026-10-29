namespace Shared.Integration.Contracts;

/// <summary>Published by booking's deadline dispatcher when a workflow's deadline or retry time is due.</summary>
public sealed record ReservationWorkflowExpired(Guid CorrelationId) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
