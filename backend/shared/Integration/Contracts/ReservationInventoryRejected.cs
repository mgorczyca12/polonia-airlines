namespace Shared.Integration.Contracts;

/// <summary>Seating refused the hold or confirmation; <see cref="Code"/> is a stable machine-readable reason.</summary>
public sealed record ReservationInventoryRejected(Guid CorrelationId, string Code) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
