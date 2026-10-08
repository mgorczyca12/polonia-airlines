using Shared.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.Domain.Entities;

public class Fare : Entity<int>
{
    public int ReservationId { get; private set; }
    public Reservation Reservation { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;

    private Fare() { }

    public static Fare Create(string code, decimal amount, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        return new Fare { Code = code.Trim(), Amount = amount, Currency = CurrencyCode.Normalize(currency) };
    }

    internal static Fare CreateForReservation(Reservation reservation, Fare quote)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(quote);
        var fare = Create(quote.Code, quote.Amount, quote.Currency);
        fare.ReservationId = reservation.Id;
        fare.Reservation = reservation;
        return fare;
    }
}