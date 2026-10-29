using booking_service.Domain.Entities;
using Shared.Application.Abstractions;

namespace booking_service.Application.Abstractions;

public interface ICheckoutOfferRepository : IRepository<CheckoutOffer, Guid>
{
    /// <summary>
    /// Atomically marks an unconsumed, unexpired offer as consumed. Concurrent callers are serialized so that
    /// exactly one succeeds; the others return false.
    /// </summary>
    Task<bool> TryConsumeAsync(Guid offerId, DateTimeOffset now, CancellationToken cancellationToken);
}
