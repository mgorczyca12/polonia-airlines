namespace booking_service.Domain.Entities;

public sealed class GuestCheckout
{
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private GuestCheckout()
    {
    }

    public GuestCheckout(string tokenHash, DateTimeOffset createdAt)
    {
        TokenHash = tokenHash;
        CreatedAt = createdAt;
    }
}

public sealed class CheckoutOffer
{
    public Guid Id { get; private set; }
    public string GuestTokenHash { get; private set; } = null!;
    public string SnapshotJson { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool Consumed { get; private set; }

    private CheckoutOffer()
    {
    }

    public CheckoutOffer(string guestTokenHash, string snapshotJson, DateTimeOffset expiresAt)
    {
        Id = Guid.NewGuid();
        GuestTokenHash = guestTokenHash;
        SnapshotJson = snapshotJson;
        ExpiresAt = expiresAt;
    }
}

public sealed class ReservationCheckout
{
    public Guid Id { get; private set; }
    public string GuestTokenHash { get; private set; } = null!;
    public string RequestId { get; private set; } = null!;
    public string PayloadHash { get; private set; } = null!;
    public Guid OfferId { get; private set; }
    public int ReservationId { get; private set; }
    public Guid CorrelationId { get; private set; }

    private ReservationCheckout()
    {
    }

    public ReservationCheckout(
        string guestTokenHash,
        string requestId,
        string payloadHash,
        Guid offerId,
        int reservationId,
        Guid correlationId)
    {
        Id = Guid.NewGuid();
        GuestTokenHash = guestTokenHash;
        RequestId = requestId;
        PayloadHash = payloadHash;
        OfferId = offerId;
        ReservationId = reservationId;
        CorrelationId = correlationId;
    }
}
