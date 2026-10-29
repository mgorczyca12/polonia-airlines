using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using booking_service.Application.Abstractions;
using booking_service.Application.Contracts;
using booking_service.Application.Validation;
using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure.Persistence;
using booking_service.Infrastructure.Sagas;
using FluentResults;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Integration.Contracts;

namespace booking_service.Infrastructure;

public sealed class BookingService(
    BookingDbContext database,
    IPublishEndpoint publisher,
    TimeProvider clock,
    OfferRequestValidator offerValidator,
    ReservationRequestValidator reservationValidator,
    BookingAccessValidator accessValidator) : IBookingService, IBookingQueries
{
    public async Task<CheckoutResponse> CreateCheckout(CancellationToken cancellationToken)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        database.GuestCheckouts.Add(new GuestCheckout(Hash(token), clock.GetUtcNow()));
        await database.SaveChangesAsync(cancellationToken);
        return new CheckoutResponse(token);
    }

    public async Task<Result<ProductResponse[]>> Catalog(CancellationToken cancellationToken)
    {
        var products = await database.FareProducts.AsNoTracking()
            .Where(x => x.Active)
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Version)
            .ToListAsync(cancellationToken);

        return Result.Ok(products.Select(x => new ProductResponse(
            x.Id, x.Code, x.Version, x.CabinClass, x.Brand, Snapshot(x.BaggageAllowance))).ToArray());
    }

    public async Task<Result<OfferResponse>> CreateOffer(
        string guestToken,
        OfferRequest request,
        CancellationToken cancellationToken)
    {
        await offerValidator.ValidateAndThrowAsync(request, cancellationToken);
        await accessValidator.ValidateAndThrowAsync(new BookingAccessRequest(guestToken), cancellationToken);
        var tokenHash = Hash(guestToken);
        if (!await database.GuestCheckouts.AnyAsync(x => x.TokenHash == tokenHash, cancellationToken))
        {
            return Failure<OfferResponse>("not_found", "Checkout was not found.");
        }

        var fares = new List<FareSnapshot>();
        foreach (var selection in request.Selections)
        {
            var price = await database.FlightPrices.AsNoTracking()
                .SingleOrDefaultAsync(x => x.FlightId == selection.FlightId &&
                    x.ProductId == selection.ProductId && x.Currency == request.Currency, cancellationToken);
            var product = await database.FareProducts.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == selection.ProductId && x.Active, cancellationToken);
            if (price is null || product is null)
            {
                return Failure<OfferResponse>("price_unavailable", "No active fare price exists for the selection.");
            }

            for (var index = 0; index < request.PassengerCount; index++)
            {
                if (selection.PassengerIndex.HasValue && selection.PassengerIndex != index)
                {
                    continue;
                }

                fares.Add(new FareSnapshot(
                    index,
                    selection.FlightId,
                    product.Id,
                    product.Code,
                    product.Version,
                    price.CabinId,
                    product.CabinClass,
                    product.Brand,
                    price.Amount,
                    price.Currency,
                    Snapshot(product.BaggageAllowance)));
            }
        }

        var expiresAt = clock.GetUtcNow().AddMinutes(15);
        var offer = new CheckoutOffer(tokenHash, JsonSerializer.Serialize(fares.ToArray()), expiresAt);
        database.Offers.Add(offer);
        await database.SaveChangesAsync(cancellationToken);
        return Result.Ok(ToOffer(offer));
    }

    public async Task<Result<OfferResponse>> Offer(Guid id, string guestToken, CancellationToken cancellationToken)
    {
        await accessValidator.ValidateAndThrowAsync(new BookingAccessRequest(guestToken, OfferId: id), cancellationToken);
        var tokenHash = Hash(guestToken);
        var offer = await database.Offers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.GuestTokenHash == tokenHash, cancellationToken);
        if (offer is null)
        {
            return Failure<OfferResponse>("not_found", "Offer was not found.");
        }

        if (offer.ExpiresAt <= clock.GetUtcNow())
        {
            return Failure<OfferResponse>("offer_expired", "Offer has expired.");
        }

        return Result.Ok(ToOffer(offer));
    }

    public async Task<Result<ReservationResponse>> CreateReservation(
        string guestToken,
        ReservationRequest request,
        CancellationToken cancellationToken)
    {
        await reservationValidator.ValidateAndThrowAsync(request, cancellationToken);
        await accessValidator.ValidateAndThrowAsync(new BookingAccessRequest(guestToken), cancellationToken);
        var tokenHash = Hash(guestToken);
        var payloadHash = Hash(JsonSerializer.Serialize(request));

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            var prior = await database.ReservationCheckouts.AsNoTracking()
                .SingleOrDefaultAsync(x => x.GuestTokenHash == tokenHash && x.RequestId == request.RequestId, cancellationToken);
            if (prior is not null)
            {
                return await Replay(prior, payloadHash, guestToken, cancellationToken);
            }

            var offer = await database.Offers.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == request.OfferId && x.GuestTokenHash == tokenHash, cancellationToken);
            if (offer is null)
            {
                return Failure<ReservationResponse>("not_found", "Offer was not found.");
            }

            var now = clock.GetUtcNow();
            if (offer.ExpiresAt <= now)
            {
                return Failure<ReservationResponse>("offer_expired", "Offer has expired.");
            }

            var fares = ReadFares(offer);
            if (fares.Select(x => x.PassengerIndex).Distinct().Count() != request.Passengers.Length)
            {
                return Failure<ReservationResponse>("passenger_mismatch", "Passenger count must match the offer.");
            }

            var claimed = await database.Offers
                .Where(x => x.Id == offer.Id && !x.Consumed && x.ExpiresAt > now)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Consumed, true), cancellationToken);
            if (claimed == 0)
            {
                prior = await database.ReservationCheckouts.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.GuestTokenHash == tokenHash && x.RequestId == request.RequestId, cancellationToken);
                if (prior is not null)
                {
                    return await Replay(prior, payloadHash, guestToken, cancellationToken);
                }

                return Failure<ReservationResponse>("offer_consumed", "Offer has already been consumed.");
            }

            var reservation = Domain.Entities.Reservation.Create(
                PnrCode.Generate(), null, request.ContactName, request.ContactEmail,
                fares.Select(x => x.FlightId).Distinct(StringComparer.Ordinal), fares[0].Currency, now);
            for (var index = 0; index < request.Passengers.Length; index++)
            {
                var passenger = request.Passengers[index];
                reservation.AddPassenger(
                    passenger.FirstName,
                    passenger.LastName,
                    passenger.DateOfBirth,
                    fares.Where(x => x.PassengerIndex == index).Select(ToQuote));
            }

            database.Reservations.Add(reservation);
            try
            {
                // Detect PNR insert collisions before publishing anything into the scoped outbox.
                await database.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsPnrCollision(exception))
            {
                await transaction.RollbackAsync(cancellationToken);
                database.ChangeTracker.Clear();
                continue;
            }

            var correlationId = Guid.NewGuid();
            database.ReservationCheckouts.Add(new ReservationCheckout(
                tokenHash, request.RequestId, payloadHash, offer.Id, reservation.Id, correlationId));
            var selections = reservation.Passengers.SelectMany(passenger => passenger.Fares.Select(fare =>
                new InventorySelection(
                    passenger.Id.ToString(CultureInfo.InvariantCulture),
                    fare.ReservationFlight.FlightId,
                    fare.CabinId ?? throw new InvalidOperationException("An offer fare requires a physical cabin mapping."),
                    null))).ToArray();
            await publisher.Publish(new ReservationRequested(
                correlationId,
                reservation.Id.ToString(CultureInfo.InvariantCulture),
                now.AddMinutes(10),
                selections), cancellationToken);

            try
            {
                await database.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "UX_ReservationCheckout_Request" })
            {
                await transaction.RollbackAsync(cancellationToken);
                database.ChangeTracker.Clear();
                return Failure<ReservationResponse>("idempotency_conflict", "Request ID is already in use.");
            }

            return Result.Ok(ToReservation(reservation, guestToken, fares, correlationId, null));
        }

        return Failure<ReservationResponse>("pnr_unavailable", "Unable to allocate a reservation code. Retry the request.");
    }

    public async Task<Result<ReservationResponse>> Reservation(int id, string guestToken, CancellationToken cancellationToken)
    {
        await accessValidator.ValidateAndThrowAsync(new BookingAccessRequest(guestToken, ReservationId: id), cancellationToken);
        var tokenHash = Hash(guestToken);
        var access = await database.ReservationCheckouts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReservationId == id && x.GuestTokenHash == tokenHash, cancellationToken);
        if (access is null)
        {
            return Failure<ReservationResponse>("not_found", "Reservation was not found.");
        }

        var reservation = await database.Reservations.AsNoTracking()
            .Include(x => x.Passengers).ThenInclude(x => x.Fares)
            .Include(x => x.Passengers).ThenInclude(x => x.Baggage)
            .SingleAsync(x => x.Id == id, cancellationToken);
        var offer = await database.Offers.AsNoTracking().SingleAsync(x => x.Id == access.OfferId, cancellationToken);
        var workflow = await database.Set<ReservationSagaState>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.CorrelationId == access.CorrelationId, cancellationToken);
        return Result.Ok(ToReservation(reservation, guestToken, ReadFares(offer), access.CorrelationId, workflow));
    }

    private async Task<Result<ReservationResponse>> Replay(
        ReservationCheckout prior,
        string payloadHash,
        string guestToken,
        CancellationToken cancellationToken)
    {
        if (prior.PayloadHash != payloadHash)
        {
            return Failure<ReservationResponse>("idempotency_conflict", "Request ID was used with a different payload.");
        }

        return await Reservation(prior.ReservationId, guestToken, cancellationToken);
    }

    private static bool IsPnrCollision(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "UX_Reservation_Pnr" };
    }

    private static Result<T> Failure<T>(string code, string message)
    {
        return Result.Fail<T>(new BookingError(code, message));
    }

    private static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static FareSnapshot[] ReadFares(CheckoutOffer offer)
    {
        return JsonSerializer.Deserialize<FareSnapshot[]>(offer.SnapshotJson)
            ?? throw new InvalidOperationException("Persisted offer is invalid.");
    }

    private static OfferResponse ToOffer(CheckoutOffer offer)
    {
        var fares = ReadFares(offer);
        return new OfferResponse(offer.Id, offer.ExpiresAt, fares[0].Currency, fares.Sum(x => x.Amount), fares);
    }

    private static ReservationResponse ToReservation(
        Reservation reservation,
        string token,
        FareSnapshot[] fares,
        Guid correlationId,
        ReservationSagaState? workflow)
    {
        return new ReservationResponse(
            reservation.Id, reservation.Code.Value, reservation.Status.Status,
            reservation.ReservationDate, reservation.Currency, reservation.TotalAmount, token, fares,
            correlationId, workflow?.CurrentState ?? "Pending", workflow?.FailureCode);
    }

    private static BaggageSnapshot Snapshot(BaggageAllowance baggage)
    {
        return new BaggageSnapshot(baggage.IncludedPieces, baggage.MaxWeightPerPieceKg, baggage.MaxExtraPieces, baggage.ExtraPiecePrice);
    }

    private static FareQuote ToQuote(FareSnapshot fare)
    {
        return FareQuote.Create(
            fare.FlightId, fare.ProductCode, fare.CabinClass, fare.Amount, fare.Currency,
            BaggageAllowance.Create(fare.Baggage.IncludedPieces, fare.Baggage.MaxWeightPerPieceKg,
                fare.Baggage.MaxExtraPieces, fare.Baggage.ExtraPiecePrice),
            fare.CabinId, fare.ProductVersion, fare.Brand);
    }
}
