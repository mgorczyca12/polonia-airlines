using Shared.Domain.Entities;

namespace booking_service.Domain.Entities;

/// <summary>
/// Durable record of one guest's create-reservation request. It makes the request idempotent and ties the
/// reservation and its workflow to the guest who may read it back.
/// </summary>
public sealed class ReservationCheckout : AggregateRoot<Guid>
{
    public string GuestTokenHash { get; private set; } = null!;
    public string RequestId { get; private set; } = null!;
    public string PayloadHash { get; private set; } = null!;
    public Guid OfferId { get; private set; }
    public int ReservationId { get; private set; }
    public Guid CorrelationId { get; private set; }

    private ReservationCheckout()
    {
    }

    private ReservationCheckout(Guid id) : base(id)
    {
    }

    public static ReservationCheckout Create(
        string guestTokenHash,
        string requestId,
        string payloadHash,
        Guid offerId,
        int reservationId,
        Guid correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guestTokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadHash);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(reservationId);

        if (offerId == Guid.Empty)
        {
            throw new ArgumentException("Offer is required.", nameof(offerId));
        }

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException("Workflow correlation is required.", nameof(correlationId));
        }

        return new ReservationCheckout(Guid.NewGuid())
        {
            GuestTokenHash = guestTokenHash,
            RequestId = requestId,
            PayloadHash = payloadHash,
            OfferId = offerId,
            ReservationId = reservationId,
            CorrelationId = correlationId
        };
    }

    public bool IsReplayOf(string payloadHash)
    {
        return string.Equals(PayloadHash, payloadHash, StringComparison.Ordinal);
    }
}
