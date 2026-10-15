using Shared.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.Domain.Entities;

public class PassengerFare : Entity<int>
{
    public int PassengerId { get; private set; }
    public Passenger Passenger { get; private set; } = null!;
    public int ReservationFlightId { get; private set; }
    public ReservationFlight ReservationFlight { get; private set; } = null!;
    public string ProductCode { get; private set; } = null!;
    public string CabinClass { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public BaggageAllowance BaggageAllowance { get; private set; } = null!;

    private PassengerFare()
    {
    }

    internal static PassengerFare Create(Passenger passenger, ReservationFlight flight, FareQuote quote)
    {
        ArgumentNullException.ThrowIfNull(passenger);
        ArgumentNullException.ThrowIfNull(flight);
        ArgumentNullException.ThrowIfNull(quote);

        return new PassengerFare
        {
            PassengerId = passenger.Id,
            Passenger = passenger,
            ReservationFlightId = flight.Id,
            ReservationFlight = flight,
            ProductCode = quote.ProductCode,
            CabinClass = quote.CabinClass,
            Amount = quote.Amount,
            Currency = quote.Currency,
            BaggageAllowance = quote.BaggageAllowance
        };
    }
}
