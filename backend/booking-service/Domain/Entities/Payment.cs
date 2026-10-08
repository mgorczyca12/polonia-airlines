using Shared.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.Domain.Entities;

public class Payment : Entity<int>
{
    public int ReservationId { get; private set; }
    public Reservation Reservation { get; private set; } = null!;
    public string Reference { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public DateTimeOffset RecordedAt { get; private set; }

    private Payment() { }

    internal static Payment Create(Reservation reservation, string reference, decimal amount, string currency, DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (recordedAt == default)
            throw new ArgumentException("Payment recording date is required.", nameof(recordedAt));
        return new Payment
        {
            ReservationId = reservation.Id,
            Reservation = reservation,
            Reference = reference.Trim(),
            Amount = amount,
            Currency = CurrencyCode.Normalize(currency),
            RecordedAt = recordedAt
        };
    }
}