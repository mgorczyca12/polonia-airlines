using System.Globalization;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Integration.Contracts;

namespace booking_service.Infrastructure.Sagas;

public sealed class CancelReservationActivity(BookingDbContext database)
    : IStateMachineActivity<ReservationSagaState, ReservationInventoryReleased>
{
    public void Probe(ProbeContext context)
    {
        context.CreateScope("cancel-reservation");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task Execute(
        BehaviorContext<ReservationSagaState, ReservationInventoryReleased> context,
        IBehavior<ReservationSagaState, ReservationInventoryReleased> next)
    {
        var reservationId = int.Parse(context.Saga.ReservationId, CultureInfo.InvariantCulture);
        var reservation = await database.Reservations
            .SingleAsync(reservation => reservation.Id == reservationId, context.CancellationToken);

        if (reservation.Status != ReservationStatus.Cancelled)
        {
            reservation.Cancel();
            await database.SaveChangesAsync(context.CancellationToken);
        }

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<ReservationSagaState, ReservationInventoryReleased, TException> context,
        IBehavior<ReservationSagaState, ReservationInventoryReleased> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }
}
