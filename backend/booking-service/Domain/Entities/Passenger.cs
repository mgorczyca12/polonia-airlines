using Shared.Domain.Entities;

namespace booking_service.Domain.Entities;

public class Passenger : Entity<int>
{
    private readonly List<Baggage> baggage = new();

    public int ReservationId { get; private set; }
    public Reservation Reservation { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public DateOnly DateOfBirth { get; private set; }
    public IReadOnlyCollection<Baggage> Baggage => baggage.AsReadOnly();

    private Passenger() { }

    internal static Passenger Create(Reservation reservation, string firstName, string lastName, DateOnly dateOfBirth)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        if (dateOfBirth == default)
            throw new ArgumentException("Date of birth is required.", nameof(dateOfBirth));
        return new Passenger
        {
            ReservationId = reservation.Id,
            Reservation = reservation,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DateOfBirth = dateOfBirth
        };
    }

    internal Baggage AddBaggage(string name, decimal weightKg)
    {
        var item = global::booking_service.Domain.Entities.Baggage.Create(this, name, weightKg);
        baggage.Add(item);
        return item;
    }
}