using Shared.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.Domain.Entities;

/// <summary>A guest's anonymous checkout session, identified by the hash of their capability token.</summary>
public sealed class GuestCheckout : AggregateRoot<string>
{
    public DateTimeOffset CreatedAt { get; private set; }

    private GuestCheckout()
    {
    }

    private GuestCheckout(string tokenHash, DateTimeOffset createdAt) : base(tokenHash)
    {
        CreatedAt = createdAt;
    }

    public static GuestCheckout Start(GuestToken token, DateTimeOffset startedAt)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (startedAt == default)
        {
            throw new ArgumentException("Start time is required.", nameof(startedAt));
        }

        return new GuestCheckout(token.Hash, startedAt);
    }
}
