using Shared.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.Domain.Entities;

public class Passenger : Entity<int>
{
    private readonly List<Baggage> baggage = new();
    private readonly List<PassengerFare> fares = new();

    public int ReservationId { get; private set; }
    public Reservation Reservation { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public IReadOnlyCollection<PassengerFare> Fares => fares.AsReadOnly();
    public IReadOnlyCollection<Baggage> Baggage => baggage.AsReadOnly();
    public decimal TotalAmount => fares.Sum(fare => fare.Amount) + baggage.Sum(item => item.Charge);

    private Passenger()
    {
    }

    internal static Passenger Create(
        Reservation reservation,
        string firstName,
        string lastName,
        DateOnly dateOfBirth)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        if (dateOfBirth == default)
        {
            throw new ArgumentException("Date of birth is required.", nameof(dateOfBirth));
        }

        return new Passenger
        {
            ReservationId = reservation.Id,
            Reservation = reservation,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DateOfBirth = dateOfBirth
        };
    }

    internal PassengerFare AddFare(ReservationFlight flight, FareQuote quote)
    {
        var fare = PassengerFare.Create(this, flight, quote);
        fares.Add(fare);
        return fare;
    }

    internal Baggage AddBaggage(string flightId, string name, decimal weightKg)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flightId);
        var fare = fares
            .SingleOrDefault(existing => existing.ReservationFlight.FlightId == flightId)
            ?? throw new InvalidOperationException("Passenger has no fare for this flight.");
        var existingPieces = baggage.Count(item => ReferenceEquals(item.PassengerFare, fare));
        var charge = fare.BaggageAllowance.ChargeForNextPiece(existingPieces, weightKg);
        var item = global::booking_service.Domain.Entities.Baggage.Create(this, fare, name, weightKg, charge);
        baggage.Add(item);
        return item;
    }
}
