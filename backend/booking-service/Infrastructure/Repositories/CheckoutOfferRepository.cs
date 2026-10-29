using booking_service.Application.Abstractions;
using booking_service.Domain.Entities;
using booking_service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Repositories;

namespace booking_service.Infrastructure.Repositories;

public sealed class CheckoutOfferRepository(BookingDbContext dbContext)
    : RepositoryBase<CheckoutOffer, Guid>(dbContext), ICheckoutOfferRepository
{
    public async Task<bool> TryConsumeAsync(Guid offerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // A conditional update takes the row lock, so concurrent claims on one offer are serialized.
        var claimed = await dbContext.CheckoutOffers
            .Where(offer => offer.Id == offerId && !offer.Consumed && offer.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(offer => offer.Consumed, true),
                cancellationToken);

        return claimed == 1;
    }
}
