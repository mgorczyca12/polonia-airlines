using MassTransit;

namespace booking_service.Infrastructure.Sagas;

public sealed class ReservationSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public string ReservationId { get; set; } = string.Empty;
    public DateTimeOffset HoldUntil { get; set; }
    public DateTimeOffset NextCheckAt { get; set; }
    public DateTimeOffset? CompensationStartedAt { get; set; }
    public string? FailureCode { get; set; }
}
