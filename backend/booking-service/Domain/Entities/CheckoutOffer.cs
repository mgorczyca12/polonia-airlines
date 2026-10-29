using Shared.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.Domain.Entities;

/// <summary>An expiring, server-priced selection of fares that a guest can turn into a reservation once.</summary>
public sealed class CheckoutOffer : AggregateRoot<Guid>
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    public string GuestTokenHash { get; private set; } = null!;
    public IReadOnlyList<OfferedFare> Fares { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }

    // Changed only by the atomic claim in ICheckoutOfferRepository.TryConsumeAsync, never in memory.
    public bool Consumed { get; private set; }

    public string Currency => Fares[0].Currency;
    public decimal TotalAmount => Fares.Sum(fare => fare.Amount);

    private CheckoutOffer()
    {
    }

    private CheckoutOffer(Guid id) : base(id)
    {
    }

    public static CheckoutOffer Create(
        string guestTokenHash,
        IEnumerable<OfferedFare> fares,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guestTokenHash);
        ArgumentNullException.ThrowIfNull(fares);

        if (createdAt == default)
        {
            throw new ArgumentException("Creation time is required.", nameof(createdAt));
        }

        var snapshot = fares.ToArray();
        if (snapshot.Length == 0 || snapshot.Any(fare => fare is null))
        {
            throw new ArgumentException("An offer requires at least one fare.", nameof(fares));
        }

        if (snapshot.Select(fare => fare.Currency).Distinct(StringComparer.Ordinal).Count() != 1)
        {
            throw new ArgumentException("An offer must be priced in a single currency.", nameof(fares));
        }

        return new CheckoutOffer(Guid.NewGuid())
        {
            GuestTokenHash = guestTokenHash,
            Fares = snapshot,
            ExpiresAt = createdAt.Add(Lifetime)
        };
    }

    public bool IsExpiredAt(DateTimeOffset moment)
    {
        return ExpiresAt <= moment;
    }
}
