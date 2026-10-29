using System.Globalization;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Integration.Contracts;

namespace booking_service.Infrastructure.Integrations.Sagas;

public sealed class CompleteReservationActivity(BookingDbContext database)
    : IStateMachineActivity<ReservationSagaState, ReservationInventoryConfirmed>
{
    public void Probe(ProbeContext context)
    {
        context.CreateScope("complete-reservation");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task Execute(
        BehaviorContext<ReservationSagaState, ReservationInventoryConfirmed> context,
        IBehavior<ReservationSagaState, ReservationInventoryConfirmed> next)
    {
        var reservationId = int.Parse(context.Saga.ReservationId, CultureInfo.InvariantCulture);
        var reservation = await database.Reservations
            .Include(reservation => reservation.Passengers)
            .SingleAsync(reservation => reservation.Id == reservationId, context.CancellationToken);

        if (reservation.Status != ReservationStatus.Reserved)
        {
            reservation.Reserve();
            await database.SaveChangesAsync(context.CancellationToken);
        }

        await next.Execute(context);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<ReservationSagaState, ReservationInventoryConfirmed, TException> context,
        IBehavior<ReservationSagaState, ReservationInventoryConfirmed> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }
}
