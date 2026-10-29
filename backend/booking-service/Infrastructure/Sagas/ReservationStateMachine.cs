using MassTransit;
using Shared.Integration.Contracts;

namespace booking_service.Infrastructure.Sagas;

public sealed class ReservationStateMachine : MassTransitStateMachine<ReservationSagaState>
{
    public State AwaitingCapacity { get; private set; } = null!;
    public State ConfirmingAllocations { get; private set; } = null!;
    public State Compensating { get; private set; } = null!;
    public State RequiresIntervention { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;

    public Event<ReservationRequested> Requested { get; private set; } = null!;
    public Event<ReservationInventoryHeld> Held { get; private set; } = null!;
    public Event<ReservationInventoryRejected> Rejected { get; private set; } = null!;
    public Event<ReservationInventoryConfirmed> Confirmed { get; private set; } = null!;
    public Event<ReservationInventoryReleased> Released { get; private set; } = null!;
    public Event<ReservationWorkflowExpired> Expired { get; private set; } = null!;

    public ReservationStateMachine(TimeProvider clock, ILogger<ReservationStateMachine> logger)
    {
        InstanceState(state => state.CurrentState);
        Event(() => Requested, options => options.CorrelateById(context => context.Message.CorrelationId));
        Event(() => Held, options => options.CorrelateById(context => context.Message.CorrelationId));
        Event(() => Rejected, options => options.CorrelateById(context => context.Message.CorrelationId));
        Event(() => Confirmed, options => options.CorrelateById(context => context.Message.CorrelationId));
        Event(() => Released, options => options.CorrelateById(context => context.Message.CorrelationId));
        Event(() => Expired, options => options.CorrelateById(context => context.Message.CorrelationId));

        Initially(
            When(Requested)
                .Then(context =>
                {
                    context.Saga.ReservationId = context.Message.ReservationId;
                    context.Saga.HoldUntil = context.Message.HoldUntil;
                    context.Saga.NextCheckAt = context.Message.HoldUntil;
                })
                .Publish(context => new HoldReservationInventory(
                    context.Saga.CorrelationId,
                    context.Message.ReservationId,
                    context.Message.HoldUntil,
                    context.Message.Selections))
                .TransitionTo(AwaitingCapacity));

        During(AwaitingCapacity,
            Ignore(Requested),
            When(Held)
                .IfElse(context => clock.GetUtcNow() < context.Saga.HoldUntil,
                    valid => valid
                        .Publish(context => new ConfirmReservationInventory(context.Saga.CorrelationId))
                        .TransitionTo(ConfirmingAllocations),
                    expired => expired
                        .Then(context => BeginCompensation(context.Saga, "inventory.hold_expired", clock.GetUtcNow()))
                        .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId))
                        .TransitionTo(Compensating)),
            When(Rejected)
                .Then(context => BeginCompensation(context.Saga, context.Message.Code, clock.GetUtcNow()))
                .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId))
                .TransitionTo(Compensating),
            When(Expired)
                .If(context => clock.GetUtcNow() >= context.Saga.HoldUntil,
                    expired => expired
                        .Then(context => BeginCompensation(context.Saga, "inventory.timeout", clock.GetUtcNow()))
                        .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId))
                        .TransitionTo(Compensating)));

        During(ConfirmingAllocations,
            Ignore(Requested),
            Ignore(Held),
            When(Confirmed)
                .Activity(activity => activity.OfType<CompleteReservationActivity>())
                .TransitionTo(Completed),
            When(Rejected)
                .Then(context => BeginCompensation(context.Saga, context.Message.Code, clock.GetUtcNow()))
                .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId))
                .TransitionTo(Compensating),
            When(Expired)
                .If(context => clock.GetUtcNow() >= context.Saga.HoldUntil,
                    expired => expired
                        .Then(context => BeginCompensation(context.Saga, "inventory.confirmation_timeout", clock.GetUtcNow()))
                        .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId))
                        .TransitionTo(Compensating)));

        During(Compensating,
            Ignore(Requested),
            Ignore(Rejected),
            When(Held)
                .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId)),
            When(Confirmed)
                .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId)),
            When(Released)
                .Activity(activity => activity.OfType<CancelReservationActivity>())
                .TransitionTo(Cancelled),
            When(Expired)
                .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId))
                .If(context => context.Saga.CompensationStartedAt <= clock.GetUtcNow().AddMinutes(-30),
                    stuck => stuck
                        .Then(context => logger.LogError(
                            "Reservation workflow {CorrelationId} requires intervention: inventory release has not completed.",
                            context.Saga.CorrelationId))
                        .TransitionTo(RequiresIntervention)));

        During(RequiresIntervention,
            Ignore(Requested),
            Ignore(Rejected),
            When(Held)
                .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId)),
            When(Confirmed)
                .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId)),
            When(Expired)
                .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId)),
            When(Released)
                .Activity(activity => activity.OfType<CancelReservationActivity>())
                .TransitionTo(Cancelled));

        During(Completed,
            Ignore(Requested),
            Ignore(Held),
            Ignore(Confirmed),
            Ignore(Rejected),
            Ignore(Released),
            Ignore(Expired));

        During(Cancelled,
            Ignore(Requested),
            Ignore(Rejected),
            Ignore(Released),
            Ignore(Expired),
            When(Held)
                .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId)),
            When(Confirmed)
                .Publish(context => new ReleaseReservationInventory(context.Saga.CorrelationId)));
    }

    private static void BeginCompensation(
        ReservationSagaState state,
        string failureCode,
        DateTimeOffset now)
    {
        state.FailureCode = failureCode;
        state.CompensationStartedAt = now;
        state.NextCheckAt = now.AddSeconds(15);
    }
}
