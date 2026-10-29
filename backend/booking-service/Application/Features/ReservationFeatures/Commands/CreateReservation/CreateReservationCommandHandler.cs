using System.Globalization;
using booking_service.Application.Abstractions;
using booking_service.Application.Contracts.Requests;
using booking_service.Application.Errors;
using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using DispatchR.Abstractions.Send;
using FluentResults;
using Shared.Application.Abstractions;
using Shared.Infrastructure.Messaging;
using Shared.Integration.Contracts;

namespace booking_service.Application.Features.ReservationFeatures.Commands.CreateReservation;

public sealed class CreateReservationCommandHandler(
    IReservationCheckoutRepository checkouts,
    ICheckoutOfferRepository offers,
    IReservationRepository reservations,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher events,
    TimeProvider clock
) : IRequestHandler<CreateReservationCommand, ValueTask<Result<int>>>
{
    private const int MaxPnrAttempts = 5;
    private static readonly TimeSpan InventoryHoldLifetime = TimeSpan.FromMinutes(10);

    public async ValueTask<Result<int>> Handle(
        CreateReservationCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var tokenHash = GuestToken.From(command.GuestToken).Hash;
        var payloadHash = ReservationRequestFingerprint.Of(request);

        var prior = await checkouts.GetByRequestAsync(tokenHash, request.RequestId, cancellationToken);
        if (prior is not null)
        {
            return Replay(prior, payloadHash);
        }

        var offer = await offers.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.GuestTokenHash != tokenHash)
        {
            return Result.Fail<int>(BookingError.NotFound("Offer"));
        }

        var now = clock.GetUtcNow();
        if (offer.IsExpiredAt(now))
        {
            return Result.Fail<int>(BookingError.OfferExpired());
        }

        if (offer.Fares.Select(fare => fare.PassengerIndex).Distinct().Count() != request.Passengers.Length)
        {
            return Result.Fail<int>(BookingError.PassengerMismatch());
        }

        if (!await offers.TryConsumeAsync(offer.Id, now, cancellationToken))
        {
            prior = await checkouts.GetByRequestAsync(tokenHash, request.RequestId, cancellationToken);

            return prior is null
                ? Result.Fail<int>(BookingError.OfferConsumed())
                : Replay(prior, payloadHash);
        }

        var pnrCode = await AllocatePnrCode(cancellationToken);
        if (pnrCode is null)
        {
            return Result.Fail<int>(BookingError.PnrUnavailable());
        }

        var reservation = CreateReservation(pnrCode, offer, request, now);
        reservations.Add(reservation);

        // Passenger identities are database-generated and the workflow message needs them.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var correlationId = Guid.NewGuid();
        checkouts.Add(ReservationCheckout.Create(
            tokenHash,
            request.RequestId,
            payloadHash,
            offer.Id,
            reservation.Id,
            correlationId));

        await events.PublishAsync(
            new ReservationRequested(
                correlationId,
                reservation.Id.ToString(CultureInfo.InvariantCulture),
                now.Add(InventoryHoldLifetime),
                SelectionsOf(reservation)),
            cancellationToken);

        return Result.Ok(reservation.Id);
    }

    private async Task<PnrCode?> AllocatePnrCode(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxPnrAttempts; attempt++)
        {
            var candidate = PnrCode.Generate();
            if (!await reservations.PnrCodeExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return null;
    }

    private static Reservation CreateReservation(
        PnrCode pnrCode,
        CheckoutOffer offer,
        ReservationRequest request,
        DateTimeOffset now)
    {
        var flightIds = offer.Fares
            .Select(fare => fare.FlightId)
            .Distinct(StringComparer.Ordinal);

        var reservation = Reservation.Create(
            pnrCode,
            null,
            request.ContactName,
            request.ContactEmail,
            flightIds,
            offer.Currency,
            now);

        for (var index = 0; index < request.Passengers.Length; index++)
        {
            var passenger = request.Passengers[index];
            reservation.AddPassenger(
                passenger.FirstName,
                passenger.LastName,
                passenger.DateOfBirth,
                offer.Fares
                    .Where(fare => fare.PassengerIndex == index)
                    .Select(fare => fare.ToQuote()));
        }

        return reservation;
    }

    private static InventorySelection[] SelectionsOf(Reservation reservation)
    {
        return reservation.Passengers
            .SelectMany(passenger => passenger.Fares.Select(fare => new InventorySelection(
                passenger.Id.ToString(CultureInfo.InvariantCulture),
                fare.ReservationFlight.FlightId,
                fare.CabinId,
                null)))
            .ToArray();
    }

    private static Result<int> Replay(ReservationCheckout prior, string payloadHash)
    {
        return prior.IsReplayOf(payloadHash)
            ? Result.Ok(prior.ReservationId)
            : Result.Fail<int>(BookingError.IdempotencyConflict());
    }
}
