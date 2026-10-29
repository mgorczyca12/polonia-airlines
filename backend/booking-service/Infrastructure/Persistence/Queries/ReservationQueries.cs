using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using booking_service.Application.Errors;
using booking_service.Application.Features.ReservationFeatures.Queries;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure.Integrations.Sagas;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace booking_service.Infrastructure.Persistence.Queries;

public sealed class ReservationQueries(BookingDbContext context) : IReservationQueries
{
    private const string PendingWorkflowStatus = "Pending";

    public async Task<Result<ReservationResponse>> GetReservation(
        GetReservationRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = GuestToken.From(request.GuestToken).Hash;
        var access = await context.ReservationCheckouts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                checkout => checkout.ReservationId == request.ReservationId
                    && checkout.GuestTokenHash == tokenHash,
                cancellationToken);

        if (access is null)
        {
            return Result.Fail<ReservationResponse>(BookingError.NotFound("Reservation"));
        }

        var reservation = await context.Reservations
            .AsNoTracking()
            .Include(value => value.Passengers).ThenInclude(passenger => passenger.Fares)
            .Include(value => value.Passengers).ThenInclude(passenger => passenger.Baggage)
            .SingleAsync(value => value.Id == request.ReservationId, cancellationToken);
        var offer = await context.CheckoutOffers
            .AsNoTracking()
            .SingleAsync(value => value.Id == access.OfferId, cancellationToken);
        var workflow = await context.Set<ReservationSagaState>()
            .AsNoTracking()
            .SingleOrDefaultAsync(state => state.CorrelationId == access.CorrelationId, cancellationToken);

        return Result.Ok(new ReservationResponse(
            reservation.Id,
            reservation.Code.Value,
            reservation.Status.Status,
            reservation.ReservationDate,
            reservation.Currency,
            reservation.TotalAmount,
            request.GuestToken,
            offer.Fares.Select(ResponseMapper.ToSnapshot).ToArray(),
            access.CorrelationId,
            workflow?.CurrentState ?? PendingWorkflowStatus,
            workflow?.FailureCode));
    }
}
