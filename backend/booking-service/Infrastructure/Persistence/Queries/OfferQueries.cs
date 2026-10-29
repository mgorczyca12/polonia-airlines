using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using booking_service.Application.Errors;
using booking_service.Application.Features.CheckoutFeatures.Queries;
using booking_service.Domain.ValueObjects;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace booking_service.Infrastructure.Persistence.Queries;

public sealed class OfferQueries(BookingDbContext context, TimeProvider clock) : IOfferQueries
{
    public async Task<Result<OfferResponse>> GetOffer(GetOfferRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = GuestToken.From(request.GuestToken).Hash;
        var offer = await context.CheckoutOffers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.Id == request.OfferId && value.GuestTokenHash == tokenHash,
                cancellationToken);

        if (offer is null)
        {
            return Result.Fail<OfferResponse>(BookingError.NotFound("Offer"));
        }

        if (offer.IsExpiredAt(clock.GetUtcNow()))
        {
            return Result.Fail<OfferResponse>(BookingError.OfferExpired());
        }

        return Result.Ok(new OfferResponse(
            offer.Id,
            offer.ExpiresAt,
            offer.Currency,
            offer.TotalAmount,
            offer.Fares.Select(ResponseMapper.ToSnapshot).ToArray()));
    }
}
