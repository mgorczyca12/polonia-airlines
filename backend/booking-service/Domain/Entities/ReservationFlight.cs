using Shared.Domain.Entities;

namespace booking_service.Domain.Entities;

public class ReservationFlight : Entity<int>
{
    public int ReservationId { get; private set; }
    public Reservation Reservation { get; private set; } = null!;
    public string FlightId { get; private set; } = null!;
    public int SequenceNumber { get; private set; }

    private ReservationFlight() { }

    internal static ReservationFlight Create(Reservation reservation, string flightId, int sequenceNumber)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentException.ThrowIfNullOrWhiteSpace(flightId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequenceNumber);
        return new ReservationFlight
        {
            ReservationId = reservation.Id,
            Reservation = reservation,
            FlightId = flightId,
            SequenceNumber = sequenceNumber
        };
    }
}
